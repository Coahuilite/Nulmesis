use std::ffi::OsStr;
use std::os::windows::ffi::OsStrExt;
use std::path::Path;

use crate::match_policy::is_disqualified;
use crate::models::{DeleteError, DeleteResult, DeleteSummary, NulMatch};
use crate::path::normalize_reserved_path;
use windows::core::{Error as WinError, PCWSTR};
use windows::Win32::Storage::FileSystem::{
    DeleteFileW, GetFileAttributesW, SetFileAttributesW, FILE_ATTRIBUTE_READONLY,
    FILE_FLAGS_AND_ATTRIBUTES, INVALID_FILE_ATTRIBUTES,
};

pub struct NulFileDeleter;

impl Default for NulFileDeleter {
    fn default() -> Self {
        Self
    }
}

impl NulFileDeleter {
    pub fn delete(&self, targets: &[NulMatch]) -> DeleteResult {
        let mut deleted_count = 0usize;
        let mut errors = Vec::new();

        for target in targets {
            match delete_core(target) {
                Ok(()) => deleted_count += 1,
                Err(error) => errors.push(DeleteError {
                    path: target.absolute_path.clone(),
                    message: error.to_string(),
                }),
            }
        }

        DeleteResult {
            summary: DeleteSummary {
                requested_count: targets.len(),
                deleted_count,
                failed_count: errors.len(),
                cancelled: false,
            },
            errors,
        }
    }
}

fn delete_core(target: &NulMatch) -> std::io::Result<()> {
    validate_delete_target(target)?;

    let normalized =
        normalize_reserved_path(Some(&target.absolute_path)).expect("absolute path should exist");

    unsafe {
        let wide = encode_wide_path(&normalized);
        let attributes = GetFileAttributesW(PCWSTR(wide.as_ptr()));

        if attributes == INVALID_FILE_ATTRIBUTES {
            return Err(std::io::Error::from_raw_os_error(
                WinError::from_win32().code().0,
            ));
        }

        if attributes & FILE_ATTRIBUTE_READONLY.0 != 0 {
            let next_attributes = attributes & !FILE_ATTRIBUTE_READONLY.0;
            SetFileAttributesW(
                PCWSTR(wide.as_ptr()),
                FILE_FLAGS_AND_ATTRIBUTES(next_attributes),
            )
            .map_err(|_| std::io::Error::from_raw_os_error(WinError::from_win32().code().0))?;
        }

        DeleteFileW(PCWSTR(wide.as_ptr()))
            .map_err(|_| std::io::Error::from_raw_os_error(WinError::from_win32().code().0))?;
    }

    Ok(())
}

fn validate_delete_target(target: &NulMatch) -> std::io::Result<()> {
    if !target.file_name.eq_ignore_ascii_case("nul") || is_disqualified(&target.file_name) {
        return Err(std::io::Error::new(
            std::io::ErrorKind::InvalidInput,
            "Delete target must be a literal file named 'nul'.",
        ));
    }

    let path = Path::new(&target.absolute_path);
    let path_file_name = path
        .file_name()
        .and_then(|name| name.to_str())
        .unwrap_or_default();
    if !path_file_name.eq_ignore_ascii_case("nul") {
        return Err(std::io::Error::new(
            std::io::ErrorKind::InvalidInput,
            "Delete target path must end with a literal 'nul' filename.",
        ));
    }

    let normalized = normalize_reserved_path(Some(&target.absolute_path)).unwrap_or_default();
    let has_volume_prefix = normalized.starts_with(r"\\?\UNC\")
        || normalized
            .strip_prefix(r"\\?\")
            .map(|rest| rest.len() >= 2 && rest.as_bytes()[1] == b':')
            .unwrap_or(false);

    if !has_volume_prefix {
        return Err(std::io::Error::new(
            std::io::ErrorKind::InvalidInput,
            "Delete target must resolve to a filesystem path, not a device namespace.",
        ));
    }

    Ok(())
}

fn encode_wide_path(path: &str) -> Vec<u16> {
    OsStr::new(path)
        .encode_wide()
        .chain(std::iter::once(0))
        .collect()
}

#[cfg(test)]
mod tests {
    use chrono::Utc;
    use tempfile::tempdir;

    use crate::{NulMatch, ReservedNameFixtureBuilder};

    use super::NulFileDeleter;

    #[test]
    fn deletes_read_only_file() {
        let temp_dir = tempdir().expect("temp dir should exist");
        let builder =
            ReservedNameFixtureBuilder::new(temp_dir.path()).expect("builder should initialize");
        let path = builder
            .create_read_only_nul_file(r"readonly\nul")
            .expect("fixture should be created");
        let deleter = NulFileDeleter;
        let target = NulMatch {
            absolute_path: path.to_string_lossy().to_string(),
            relative_path: String::from(r"readonly\nul"),
            file_name: String::from("nul"),
            size_bytes: 0,
            last_write_time_utc: Utc::now(),
        };

        let result = deleter.delete(&[target.clone()]);

        assert_eq!(result.summary.deleted_count, 1);
        assert!(result.errors.is_empty());
        assert!(!std::path::Path::new(&target.absolute_path).exists());
    }

    #[test]
    fn rejects_device_like_target() {
        let deleter = NulFileDeleter;
        let target = NulMatch {
            absolute_path: String::from("NUL"),
            relative_path: String::from("NUL"),
            file_name: String::from("nul"),
            size_bytes: 0,
            last_write_time_utc: Utc::now(),
        };

        let result = deleter.delete(&[target]);

        assert_eq!(result.summary.deleted_count, 0);
        assert_eq!(result.summary.failed_count, 1);
        assert!(result.errors[0].message.contains("filesystem path"));
    }
}
