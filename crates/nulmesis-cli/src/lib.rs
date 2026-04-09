use std::io::{BufRead, Write};
use std::path::PathBuf;

use anyhow::Result;
use chrono::Utc;
use clap::{CommandFactory, Parser, Subcommand, ValueEnum};
use nulmesis_core::{
    DeleteResult, NulFileDeleter, NulFileScanner, ScanError, ScanMode, ScanResult,
};
use serde::Serialize;
use serde_json::{json, Value};

#[derive(Debug, Parser)]
#[command(name = "nulmesis")]
#[command(about = "Detect and delete reserved-name nul files.")]
pub struct Cli {
    #[command(subcommand)]
    pub command: Commands,
}

#[derive(Debug, Subcommand)]
pub enum Commands {
    Scan(CommonArgs),
    List(CommonArgs),
    Delete(CommonArgs),
}

#[derive(Debug, Clone, clap::Args)]
pub struct CommonArgs {
    #[arg(long)]
    pub root: Option<String>,

    #[arg(long, value_enum, default_value_t = CliScanMode::Strict)]
    pub mode: CliScanMode,

    #[arg(long)]
    pub json: bool,
}

#[derive(Debug, Clone, Copy, ValueEnum)]
pub enum CliScanMode {
    Strict,
    Loose,
}

impl From<CliScanMode> for ScanMode {
    fn from(value: CliScanMode) -> Self {
        match value {
            CliScanMode::Strict => ScanMode::Strict,
            CliScanMode::Loose => ScanMode::Loose,
        }
    }
}

#[derive(Debug, Serialize)]
pub struct CliEnvelope {
    pub version: String,
    pub mode: String,
    pub root: String,
    pub summary: CliSummary,
}

#[derive(Debug, Serialize)]
pub struct CliSummary {
    pub command: String,
    pub exit_code: i32,
    pub matched_count: usize,
    pub error_count: usize,
}

pub struct CliExitCode;

impl CliExitCode {
    pub const SUCCESS: i32 = 0;
    pub const PARTIAL_FAILURE: i32 = 1;
    pub const INVALID_ARGUMENTS: i32 = 2;
    pub const USER_CANCELLED_DELETE: i32 = 3;
    pub const UNHANDLED_EXCEPTION: i32 = 4;
}

pub fn sample_scan_envelope(root: &str, mode: CliScanMode) -> CliEnvelope {
    CliEnvelope {
        version: env!("CARGO_PKG_VERSION").to_string(),
        mode: cli_mode_label(mode).to_string(),
        root: root.to_string(),
        summary: CliSummary {
            command: "scan".to_string(),
            exit_code: 0,
            matched_count: 0,
            error_count: 0,
        },
    }
}

pub fn render_help() -> String {
    let mut command = Cli::command();
    let mut output = Vec::new();
    command
        .write_long_help(&mut output)
        .expect("help should render");
    String::from_utf8(output).expect("help should be utf8")
}

pub fn run_with_io(
    cli: Cli,
    input: &mut dyn BufRead,
    output: &mut dyn Write,
    error: &mut dyn Write,
) -> Result<i32> {
    let scanner = NulFileScanner;
    let deleter = NulFileDeleter;

    match cli.command {
        Commands::Scan(args) => execute_scan_like("scan", false, args, &scanner, output, error),
        Commands::List(args) => execute_scan_like("list", true, args, &scanner, output, error),
        Commands::Delete(args) => execute_delete(args, input, &scanner, &deleter, output, error),
    }
}

fn execute_scan_like(
    command_name: &str,
    list_only: bool,
    args: CommonArgs,
    scanner: &NulFileScanner,
    output: &mut dyn Write,
    error: &mut dyn Write,
) -> Result<i32> {
    let root = resolve_root(args.root)?;
    let mode: ScanMode = args.mode.into();

    match scanner.scan(&root, mode) {
        Ok(scan_result) => {
            let exit_code = if scan_result.errors.is_empty() {
                CliExitCode::SUCCESS
            } else {
                CliExitCode::PARTIAL_FAILURE
            };

            if args.json {
                write_json(
                    output,
                    create_scan_payload(command_name, &scan_result, &root, mode, exit_code),
                )?;
            } else if list_only {
                write_list_human(output, error, &scan_result, &root, mode)?;
            } else {
                write_scan_human(output, error, &scan_result, &root, mode)?;
            }

            Ok(exit_code)
        }
        Err(err) => {
            writeln!(error, "{err}")?;
            Ok(CliExitCode::UNHANDLED_EXCEPTION)
        }
    }
}

fn execute_delete(
    args: CommonArgs,
    input: &mut dyn BufRead,
    scanner: &NulFileScanner,
    deleter: &NulFileDeleter,
    output: &mut dyn Write,
    error: &mut dyn Write,
) -> Result<i32> {
    let root = resolve_root(args.root)?;
    let mode: ScanMode = args.mode.into();

    match scanner.scan(&root, mode) {
        Ok(scan_result) => {
            if scan_result.delete_targets.is_empty() {
                let exit_code = if scan_result.errors.is_empty() {
                    CliExitCode::SUCCESS
                } else {
                    CliExitCode::PARTIAL_FAILURE
                };

                if args.json {
                    write_json(
                        output,
                        create_delete_payload(&scan_result, None, &root, mode, exit_code),
                    )?;
                } else {
                    writeln!(
                        output,
                        "No delete targets found under '{}'.",
                        scan_result.summary.root
                    )?;
                    write_scan_errors(error, &scan_result.errors)?;
                    writeln!(
                        output,
                        "Summary: {} matches, {} delete targets, {} scan errors.",
                        scan_result.summary.matched_count,
                        scan_result.delete_targets.len(),
                        scan_result.summary.error_count
                    )?;
                }

                return Ok(exit_code);
            }

            if !args.json {
                writeln!(output, "Pending delete targets:")?;
                for item in &scan_result.delete_targets {
                    writeln!(output, "- {}", item.absolute_path)?;
                }
                write!(
                    output,
                    "Delete these {} item(s)? [y/N]: ",
                    scan_result.delete_targets.len()
                )?;
                output.flush()?;
            }

            let mut confirmation = String::new();
            let _ = input.read_line(&mut confirmation)?;
            if !confirmation.trim().eq_ignore_ascii_case("y") {
                let cancelled = DeleteResult {
                    summary: nulmesis_core::DeleteSummary {
                        requested_count: scan_result.delete_targets.len(),
                        deleted_count: 0,
                        failed_count: 0,
                        cancelled: true,
                    },
                    errors: vec![],
                };

                if args.json {
                    write_json(
                        output,
                        create_delete_payload(
                            &scan_result,
                            Some(&cancelled),
                            &root,
                            mode,
                            CliExitCode::USER_CANCELLED_DELETE,
                        ),
                    )?;
                } else {
                    writeln!(output, "Delete cancelled.")?;
                }

                return Ok(CliExitCode::USER_CANCELLED_DELETE);
            }

            let delete_result = deleter.delete(&scan_result.delete_targets);
            let exit_code = if scan_result.errors.is_empty() && delete_result.errors.is_empty() {
                CliExitCode::SUCCESS
            } else {
                CliExitCode::PARTIAL_FAILURE
            };

            if args.json {
                write_json(
                    output,
                    create_delete_payload(
                        &scan_result,
                        Some(&delete_result),
                        &root,
                        mode,
                        exit_code,
                    ),
                )?;
            } else {
                writeln!(
                    output,
                    "Deleted {} of {} target(s).",
                    delete_result.summary.deleted_count, delete_result.summary.requested_count
                )?;
                write_scan_errors(error, &scan_result.errors)?;
                write_delete_errors(error, &delete_result)?;
                writeln!(output, "Summary: {} matches, {} delete targets, {} deleted, {} delete failures, {} scan errors.", scan_result.summary.matched_count, scan_result.delete_targets.len(), delete_result.summary.deleted_count, delete_result.summary.failed_count, scan_result.summary.error_count)?;
            }

            Ok(exit_code)
        }
        Err(err) => {
            writeln!(error, "{err}")?;
            Ok(CliExitCode::UNHANDLED_EXCEPTION)
        }
    }
}

fn resolve_root(root: Option<String>) -> Result<String> {
    let root = root.map(PathBuf::from).unwrap_or(std::env::current_dir()?);
    Ok(root.to_string_lossy().to_string())
}

fn create_scan_payload(
    command_name: &str,
    scan_result: &ScanResult,
    root: &str,
    mode: ScanMode,
    exit_code: i32,
) -> Value {
    json!({
        "version": env!("CARGO_PKG_VERSION"),
        "timestampUtc": Utc::now(),
        "root": root,
        "mode": scan_mode_label(mode),
        "matches": scan_result.matches,
        "errors": scan_result.errors.iter().map(cli_error_from_scan).collect::<Vec<_>>(),
        "summary": {
            "command": command_name,
            "exitCode": exit_code,
            "matchedCount": scan_result.summary.matched_count,
            "errorCount": scan_result.summary.error_count,
            "durationMs": scan_result.summary.duration_ms,
        }
    })
}

fn create_delete_payload(
    scan_result: &ScanResult,
    delete_result: Option<&DeleteResult>,
    root: &str,
    mode: ScanMode,
    exit_code: i32,
) -> Value {
    let mut errors = scan_result
        .errors
        .iter()
        .map(cli_error_from_scan)
        .collect::<Vec<_>>();
    if let Some(delete_result) = delete_result {
        errors.extend(delete_result.errors.iter().map(|item| {
            json!({
                "kind": "DeleteError",
                "path": item.path,
                "message": item.message,
            })
        }));
    }

    json!({
        "version": env!("CARGO_PKG_VERSION"),
        "timestampUtc": Utc::now(),
        "root": root,
        "mode": scan_mode_label(mode),
        "matches": scan_result.delete_targets,
        "errors": errors,
        "summary": {
            "command": "delete",
            "exitCode": exit_code,
            "matchedCount": scan_result.summary.matched_count,
            "scanErrorCount": scan_result.summary.error_count,
            "requestedCount": delete_result.map(|item| item.summary.requested_count).unwrap_or(0),
            "deletedCount": delete_result.map(|item| item.summary.deleted_count).unwrap_or(0),
            "failedCount": delete_result.map(|item| item.summary.failed_count).unwrap_or(0),
            "cancelled": delete_result.map(|item| item.summary.cancelled).unwrap_or(false),
        }
    })
}

fn cli_error_from_scan(error: &ScanError) -> Value {
    json!({
        "kind": format!("{:?}", error.kind),
        "path": error.path,
        "message": error.message,
    })
}

fn cli_mode_label(mode: CliScanMode) -> &'static str {
    match mode {
        CliScanMode::Strict => "strict",
        CliScanMode::Loose => "loose",
    }
}

fn scan_mode_label(mode: ScanMode) -> &'static str {
    match mode {
        ScanMode::Strict => "strict",
        ScanMode::Loose => "loose",
    }
}

fn write_json(output: &mut dyn Write, payload: Value) -> Result<()> {
    writeln!(output, "{}", serde_json::to_string_pretty(&payload)?)?;
    Ok(())
}

fn write_scan_human(
    output: &mut dyn Write,
    error: &mut dyn Write,
    scan_result: &ScanResult,
    root: &str,
    mode: ScanMode,
) -> Result<()> {
    writeln!(output, "Root: {}", root)?;
    writeln!(output, "Mode: {}", scan_mode_label(mode))?;
    writeln!(output, "Matches: {}", scan_result.summary.matched_count)?;

    for item in &scan_result.matches {
        writeln!(
            output,
            "- {} ({} bytes, {})",
            item.absolute_path,
            item.size_bytes,
            item.last_write_time_utc.to_rfc3339()
        )?;
    }

    if scan_result.matches.is_empty() {
        writeln!(output, "No matches found.")?;
    }

    write_scan_errors(error, &scan_result.errors)?;
    writeln!(
        output,
        "Summary: {} matches, {} errors, {} ms.",
        scan_result.summary.matched_count,
        scan_result.summary.error_count,
        scan_result.summary.duration_ms
    )?;
    Ok(())
}

fn write_list_human(
    output: &mut dyn Write,
    error: &mut dyn Write,
    scan_result: &ScanResult,
    root: &str,
    mode: ScanMode,
) -> Result<()> {
    if scan_result.matches.is_empty() {
        writeln!(
            output,
            "No matches found under '{}' (mode: {}).",
            root,
            scan_mode_label(mode)
        )?;
    } else {
        for item in &scan_result.matches {
            writeln!(output, "{}", item.absolute_path)?;
        }
    }

    write_scan_errors(error, &scan_result.errors)?;
    writeln!(
        output,
        "Summary: {} matches, {} errors.",
        scan_result.summary.matched_count, scan_result.summary.error_count
    )?;
    Ok(())
}

fn write_scan_errors(error: &mut dyn Write, errors: &[ScanError]) -> Result<()> {
    for item in errors {
        writeln!(error, "[{:?}] {}: {}", item.kind, item.path, item.message)?;
    }
    Ok(())
}

fn write_delete_errors(error: &mut dyn Write, delete_result: &DeleteResult) -> Result<()> {
    for item in &delete_result.errors {
        writeln!(error, "[DeleteError] {}: {}", item.path, item.message)?;
    }
    Ok(())
}

#[cfg(test)]
mod tests {
    use std::io::{BufReader, Cursor};

    use clap::Parser;
    use nulmesis_core::ReservedNameFixtureBuilder;
    use tempfile::tempdir;

    use super::{
        render_help, run_with_io, sample_scan_envelope, Cli, CliExitCode, CliScanMode, Commands,
    };

    #[test]
    fn parses_scan_command_with_json_flag() {
        let cli = Cli::parse_from(["nulmesis", "scan", "--root", "C:\\temp", "--json"]);

        match cli.command {
            Commands::Scan(args) => {
                assert_eq!(args.root.as_deref(), Some("C:\\temp"));
                assert!(args.json);
            }
            _ => panic!("expected scan command"),
        }
    }

    #[test]
    fn emits_stable_sample_json_contract() {
        let envelope = sample_scan_envelope("C:\\temp", CliScanMode::Strict);
        let json = serde_json::to_string_pretty(&envelope).expect("json serialization should work");

        assert!(json.contains("\"version\""));
        assert!(json.contains("\"command\": \"scan\""));
        assert!(json.contains("\"mode\": \"strict\""));
    }

    #[test]
    fn renders_help_text() {
        let help = render_help();
        assert!(help.contains("scan"));
        assert!(help.contains("delete"));
    }

    #[test]
    fn scan_command_emits_real_json() {
        let temp_dir = tempdir().expect("temp dir should exist");
        let builder =
            ReservedNameFixtureBuilder::new(temp_dir.path()).expect("builder should initialize");
        builder
            .create_regular_file("nul", "")
            .expect("fixture should be created");

        let cli = Cli::parse_from([
            "nulmesis",
            "scan",
            "--root",
            temp_dir.path().to_str().expect("utf8 path"),
            "--json",
        ]);
        let mut input = BufReader::new(Cursor::new(Vec::<u8>::new()));
        let mut stdout = Vec::new();
        let mut stderr = Vec::new();

        let exit_code =
            run_with_io(cli, &mut input, &mut stdout, &mut stderr).expect("cli run should succeed");
        let output = String::from_utf8(stdout).expect("utf8 output");

        assert_eq!(exit_code, CliExitCode::SUCCESS);
        assert!(output.contains("\"command\": \"scan\""));
        assert!(output.contains("\"matchedCount\": 1"));
        assert!(stderr.is_empty());
    }

    #[test]
    fn delete_command_deletes_real_target() {
        let temp_dir = tempdir().expect("temp dir should exist");
        let builder =
            ReservedNameFixtureBuilder::new(temp_dir.path()).expect("builder should initialize");
        let target = builder
            .create_read_only_nul_file("readonly\\nul")
            .expect("fixture should be created");

        let cli = Cli::parse_from([
            "nulmesis",
            "delete",
            "--root",
            temp_dir.path().to_str().expect("utf8 path"),
        ]);
        let mut input = BufReader::new(Cursor::new(b"y\n".to_vec()));
        let mut stdout = Vec::new();
        let mut stderr = Vec::new();

        let exit_code =
            run_with_io(cli, &mut input, &mut stdout, &mut stderr).expect("cli run should succeed");

        assert_eq!(exit_code, CliExitCode::SUCCESS);
        assert!(!target.exists());
        assert!(stderr.is_empty());
    }
}
