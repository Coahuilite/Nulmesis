#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

use std::sync::Arc;
use std::sync::atomic::{AtomicBool, Ordering};

use nulmesis_core::{DeleteResult, NulFileDeleter, NulFileScanner, NulMatch, ScanMode, ScanResult};
use rfd::FileDialog;
use serde::Deserialize;
use serde::Serialize;
use tauri::Manager;
use tauri::State;
use tauri::WindowEvent;

#[derive(Debug, Serialize)]
#[serde(rename_all = "camelCase")]
struct ShellStatus {
    app_name: &'static str,
    shell: &'static str,
    mode: &'static str,
    scope: &'static str,
    current_dir: String,
    version: &'static str,
    build_channel: &'static str,
    project_url: &'static str,
    engine: &'static str,
    os: &'static str,
    arch: &'static str,
    authors: Vec<&'static str>,
}

#[derive(Default)]
struct ScanControl {
    is_scanning: Arc<AtomicBool>,
    cancel_requested: Arc<AtomicBool>,
}

#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
struct ScanRequest {
    root: String,
    mode: String,
}

#[tauri::command]
async fn scan(request: ScanRequest, state: State<'_, ScanControl>) -> Result<ScanResult, String> {
    if state.is_scanning.swap(true, Ordering::SeqCst) {
        return Err("scan-in-progress".to_string());
    }

    state.cancel_requested.store(false, Ordering::SeqCst);
    let cancel_flag = state.cancel_requested.clone();
    let is_scanning = state.is_scanning.clone();

    let result = tauri::async_runtime::spawn_blocking(move || {
        let scanner = NulFileScanner;
        scanner
            .scan_with_cancel(&request.root, parse_mode(&request.mode), Some(&cancel_flag))
    })
    .await
    .map_err(|error| error.to_string());

    is_scanning.store(false, Ordering::SeqCst);

    match result {
        Ok(Ok(scan_result)) => Ok(scan_result),
        Ok(Err(error)) if error.kind() == std::io::ErrorKind::Interrupted => {
            Err("scan-cancelled".to_string())
        }
        Ok(Err(error)) => Err(error.to_string()),
        Err(error) => Err(error),
    }
}

#[tauri::command]
async fn delete(matches: Vec<NulMatch>) -> Result<DeleteResult, String> {
    tauri::async_runtime::spawn_blocking(move || {
        let deleter = NulFileDeleter;
        Ok::<DeleteResult, String>(deleter.delete(&matches))
    })
    .await
    .map_err(|error| error.to_string())?
}

#[tauri::command]
fn cancel_scan(state: State<'_, ScanControl>) -> bool {
    if state.is_scanning.load(Ordering::SeqCst) {
        state.cancel_requested.store(true, Ordering::SeqCst);
        true
    } else {
        false
    }
}

#[tauri::command]
fn pick_root_dir() -> Option<String> {
    FileDialog::new()
        .pick_folder()
        .map(|path| path.to_string_lossy().to_string())
}

#[tauri::command]
fn shell_status() -> ShellStatus {
    ShellStatus {
        app_name: "Nulmesis",
        shell: "tauri",
        mode: "rust-tauri",
        scope: "nul-only",
        current_dir: std::env::current_dir()
            .map(|path| path.to_string_lossy().to_string())
            .unwrap_or_default(),
        version: env!("CARGO_PKG_VERSION"),
        build_channel: "dirty-local",
        project_url: "https://github.com/Coahuilite/Nulmesis",
        engine: "Rust + Tauri 2",
        os: std::env::consts::OS,
        arch: std::env::consts::ARCH,
        authors: vec!["Fe (@Coahuilite)", "OpenCode", "GPT-5.4 · Sisyphus"],
    }
}

fn parse_mode(value: &str) -> ScanMode {
    match value.to_ascii_lowercase().as_str() {
        "loose" => ScanMode::Loose,
        _ => ScanMode::Strict,
    }
}

fn main() {
    tauri::Builder::default()
        .manage(ScanControl::default())
        .on_window_event(|window, event| {
            if window.label() == "main" && matches!(event, WindowEvent::CloseRequested { .. }) {
                window.app_handle().exit(0);
            }
        })
        .invoke_handler(tauri::generate_handler![
            shell_status,
            scan,
            delete,
            cancel_scan,
            pick_root_dir
        ])
        .run(tauri::generate_context!())
        .expect("failed to run tauri desktop shell");
}
