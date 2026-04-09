use std::fs;
use std::os::windows::fs::MetadataExt;
use std::path::{Path, PathBuf};
use std::sync::atomic::{AtomicBool, Ordering};
use std::time::Instant;

use chrono::{DateTime, Utc};

use crate::match_policy::{is_disqualified, is_match};
use crate::models::{NulMatch, ScanError, ScanErrorKind, ScanMode, ScanResult, ScanSummary};
use crate::path::{display_path_from_reserved, normalize_reserved_path};

pub struct NulFileScanner;

impl Default for NulFileScanner {
    fn default() -> Self {
        Self
    }
}

impl NulFileScanner {
    pub fn scan(&self, root_path: &str, mode: ScanMode) -> Result<ScanResult, std::io::Error> {
        self.scan_with_cancel(root_path, mode, None)
    }

    pub fn scan_with_cancel(
        &self,
        root_path: &str,
        mode: ScanMode,
        cancel_flag: Option<&AtomicBool>,
    ) -> Result<ScanResult, std::io::Error> {
        if root_path.trim().is_empty() {
            return Err(std::io::Error::new(
                std::io::ErrorKind::InvalidInput,
                "Root path is required.",
            ));
        }

        if is_cancelled(cancel_flag) {
            return Err(cancelled_error());
        }

        let stopwatch = Instant::now();
        let full_root_path = PathBuf::from(root_path)
            .canonicalize()
            .unwrap_or_else(|_| PathBuf::from(root_path));
        let full_root_path = display_path_from_reserved(&full_root_path.to_string_lossy());
        let normalized_root_path =
            normalize_reserved_path(Some(&full_root_path)).expect("root path should exist");
        let mut matches = Vec::new();
        let mut delete_targets = Vec::new();
        let mut errors = Vec::new();

        if !Path::new(&normalized_root_path).exists() {
            errors.push(ScanError {
                kind: ScanErrorKind::DirectoryNotFound,
                path: full_root_path.clone(),
                message: format!("Root directory '{full_root_path}' was not found."),
            });

            return Ok(Self::build_result(
                full_root_path,
                mode,
                matches,
                delete_targets,
                errors,
                stopwatch.elapsed().as_millis(),
            ));
        }

        self.scan_directory(
            &full_root_path,
            &normalized_root_path,
            &full_root_path,
            mode,
            &mut matches,
            &mut delete_targets,
            &mut errors,
            cancel_flag,
        )?;

        Ok(Self::build_result(
            full_root_path,
            mode,
            matches,
            delete_targets,
            errors,
            stopwatch.elapsed().as_millis(),
        ))
    }

    fn scan_directory(
        &self,
        root_path: &str,
        normalized_directory_path: &str,
        display_directory_path: &str,
        mode: ScanMode,
        matches: &mut Vec<NulMatch>,
        delete_targets: &mut Vec<NulMatch>,
        errors: &mut Vec<ScanError>,
        cancel_flag: Option<&AtomicBool>,
    ) -> Result<(), std::io::Error> {
        if is_cancelled(cancel_flag) {
            return Err(cancelled_error());
        }

        let entries = match fs::read_dir(normalized_directory_path) {
            Ok(entries) => entries,
            Err(error) => {
                errors.push(create_directory_error(display_directory_path, &error));
                return Ok(());
            }
        };

        for entry in entries {
            if is_cancelled(cancel_flag) {
                return Err(cancelled_error());
            }

            let entry = match entry {
                Ok(entry) => entry,
                Err(error) => {
                    errors.push(ScanError {
                        kind: map_io_error_kind(&error),
                        path: display_directory_path.to_string(),
                        message: error.to_string(),
                    });
                    continue;
                }
            };

            let path = entry.path();
            let normalized_child_path = path.to_string_lossy().to_string();
            let display_child_path = display_path_from_reserved(&normalized_child_path);
            let metadata = match entry.metadata() {
                Ok(metadata) => metadata,
                Err(error) => {
                    errors.push(create_directory_error(&display_child_path, &error));
                    continue;
                }
            };

            let file_attributes = metadata.file_attributes();
            let is_reparse_point = (file_attributes & 0x400) == 0x400;

            if metadata.is_dir() {
                if is_reparse_point {
                    let message =
                        format!("Skipped reparse point directory '{}'.", display_child_path);
                    errors.push(ScanError {
                        kind: ScanErrorKind::ReparsePointSkipped,
                        path: display_child_path,
                        message,
                    });
                    continue;
                }

                self.scan_directory(
                    root_path,
                    &normalized_child_path,
                    &display_child_path,
                    mode,
                    matches,
                    delete_targets,
                    errors,
                    cancel_flag,
                )?;
                continue;
            }

            let file_name = match path.file_name().and_then(|name| name.to_str()) {
                Some(file_name) => file_name.to_string(),
                None => continue,
            };

            if is_disqualified(&file_name) {
                continue;
            }

            let absolute_path = display_child_path;
            let relative_path = build_relative_path(root_path, display_directory_path, &file_name);
            let size_bytes = metadata.len();
            let last_write_time_utc: DateTime<Utc> = metadata
                .modified()
                .map(DateTime::<Utc>::from)
                .unwrap_or_else(|_| Utc::now());

            let candidate = NulMatch {
                absolute_path: absolute_path.clone(),
                relative_path,
                file_name,
                size_bytes,
                last_write_time_utc,
            };

            if is_match(&candidate, mode) {
                matches.push(candidate.clone());

                if is_blocked_reserved_nul(&candidate) {
                    delete_targets.push(candidate);
                }
            }
        }

        Ok(())
    }

    fn build_result(
        root_path: String,
        mode: ScanMode,
        matches: Vec<NulMatch>,
        delete_targets: Vec<NulMatch>,
        errors: Vec<ScanError>,
        duration_ms: u128,
    ) -> ScanResult {
        ScanResult {
            summary: ScanSummary {
                root: root_path,
                mode,
                matched_count: matches.len(),
                error_count: errors.len(),
                duration_ms,
            },
            matches,
            delete_targets,
            errors,
        }
    }
}

fn is_cancelled(cancel_flag: Option<&AtomicBool>) -> bool {
    cancel_flag
        .map(|flag| flag.load(Ordering::Relaxed))
        .unwrap_or(false)
}

fn cancelled_error() -> std::io::Error {
    std::io::Error::new(std::io::ErrorKind::Interrupted, "Scan cancelled.")
}

fn is_blocked_reserved_nul(candidate: &NulMatch) -> bool {
    match fs::metadata(&candidate.absolute_path) {
        Ok(metadata) => {
            let modified_matches = metadata
                .modified()
                .map(DateTime::<Utc>::from)
                .map(|modified| modified == candidate.last_write_time_utc)
                .unwrap_or(false);

            metadata.len() != candidate.size_bytes || !modified_matches
        }
        Err(_) => true,
    }
}

fn create_directory_error(display_path: &str, error: &std::io::Error) -> ScanError {
    ScanError {
        kind: map_io_error_kind(error),
        path: display_path.to_string(),
        message: error.to_string(),
    }
}

fn map_io_error_kind(error: &std::io::Error) -> ScanErrorKind {
    match error.kind() {
        std::io::ErrorKind::NotFound => ScanErrorKind::DirectoryNotFound,
        std::io::ErrorKind::PermissionDenied => ScanErrorKind::AccessDenied,
        _ => ScanErrorKind::IoFailure,
    }
}

fn build_relative_path(root_path: &str, display_directory_path: &str, file_name: &str) -> String {
    let normalized_root = root_path
        .replace('/', "\\")
        .trim_end_matches(['\\', '/'])
        .to_string();
    let normalized_display = display_directory_path.replace('/', "\\");

    if normalized_root.eq_ignore_ascii_case(&normalized_display) {
        return file_name.to_string();
    }

    let relative_dir = if normalized_display.len() >= normalized_root.len()
        && normalized_display[..normalized_root.len()].eq_ignore_ascii_case(&normalized_root)
    {
        normalized_display[normalized_root.len()..]
            .trim_start_matches(['\\', '/'])
            .to_string()
    } else {
        normalized_display
    };

    if relative_dir.is_empty() {
        file_name.to_string()
    } else {
        Path::new(&relative_dir)
            .join(file_name)
            .to_string_lossy()
            .to_string()
    }
}

#[cfg(test)]
mod tests {
    use std::sync::atomic::AtomicBool;

    use tempfile::tempdir;

    use crate::{ReservedNameFixtureBuilder, ScanErrorKind};

    use super::NulFileScanner;
    use crate::models::ScanMode;

    #[test]
    fn returns_missing_root_error_for_missing_directory() {
        let scanner = NulFileScanner;
        let result = scanner
            .scan(
                r"C:\definitely-missing-directory-for-nulmesis",
                ScanMode::Strict,
            )
            .expect("scan should complete");

        assert_eq!(result.summary.matched_count, 0);
        assert_eq!(result.errors.len(), 1);
        assert_eq!(result.errors[0].kind, ScanErrorKind::DirectoryNotFound);
    }

    #[test]
    fn finds_zero_length_nul_in_strict_mode() {
        let temp_dir = tempdir().expect("temp dir should exist");
        let builder =
            ReservedNameFixtureBuilder::new(temp_dir.path()).expect("builder should initialize");
        builder
            .create_regular_file(r"nested\nul", "")
            .expect("fixture should be created");
        let scanner = NulFileScanner;

        let result = scanner
            .scan(&temp_dir.path().to_string_lossy(), ScanMode::Strict)
            .expect("scan should succeed");

        assert_eq!(result.summary.matched_count, 1);
        assert_eq!(result.matches.len(), 1);
    }

    #[test]
    fn stops_when_cancel_requested() {
        let temp_dir = tempdir().expect("temp dir should exist");
        let builder =
            ReservedNameFixtureBuilder::new(temp_dir.path()).expect("builder should initialize");
        builder
            .create_regular_file(r"nested\nul", "")
            .expect("fixture should be created");
        let scanner = NulFileScanner;
        let cancel_flag = AtomicBool::new(true);

        let error = scanner
            .scan_with_cancel(
                &temp_dir.path().to_string_lossy(),
                ScanMode::Strict,
                Some(&cancel_flag),
            )
            .expect_err("scan should be cancelled");

        assert_eq!(error.kind(), std::io::ErrorKind::Interrupted);
    }
}
