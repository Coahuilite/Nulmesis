use chrono::{DateTime, Utc};
use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Copy, PartialEq, Eq, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub enum ScanMode {
    Strict,
    Loose,
}

#[derive(Debug, Clone, PartialEq, Eq, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct NulMatch {
    pub absolute_path: String,
    pub relative_path: String,
    pub file_name: String,
    pub size_bytes: u64,
    pub last_write_time_utc: DateTime<Utc>,
}

#[derive(Debug, Clone, Copy, PartialEq, Eq, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub enum ScanErrorKind {
    DirectoryNotFound,
    AccessDenied,
    IoFailure,
    ReparsePointSkipped,
}

#[derive(Debug, Clone, PartialEq, Eq, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct ScanError {
    pub kind: ScanErrorKind,
    pub path: String,
    pub message: String,
}

#[derive(Debug, Clone, PartialEq, Eq, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct ScanSummary {
    pub root: String,
    pub mode: ScanMode,
    pub matched_count: usize,
    pub error_count: usize,
    pub duration_ms: u128,
}

#[derive(Debug, Clone, PartialEq, Eq, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct ScanResult {
    pub matches: Vec<NulMatch>,
    pub delete_targets: Vec<NulMatch>,
    pub errors: Vec<ScanError>,
    pub summary: ScanSummary,
}

#[derive(Debug, Clone, PartialEq, Eq, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct DeleteError {
    pub path: String,
    pub message: String,
}

#[derive(Debug, Clone, PartialEq, Eq, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct DeleteSummary {
    pub requested_count: usize,
    pub deleted_count: usize,
    pub failed_count: usize,
    pub cancelled: bool,
}

#[derive(Debug, Clone, PartialEq, Eq, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct DeleteResult {
    pub summary: DeleteSummary,
    pub errors: Vec<DeleteError>,
}
