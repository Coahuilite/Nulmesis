use crate::models::{NulMatch, ScanMode};

const NUL_BASE_NAME: &str = "nul";

pub fn is_match(candidate: &NulMatch, mode: ScanMode) -> bool {
    if !candidate.file_name.eq_ignore_ascii_case(NUL_BASE_NAME) {
        return false;
    }

    match mode {
        ScanMode::Strict => candidate.size_bytes == 0,
        ScanMode::Loose => true,
    }
}

pub fn is_disqualified(file_name: &str) -> bool {
    file_name.eq_ignore_ascii_case("nul.txt")
        || file_name.eq_ignore_ascii_case("nul.")
        || file_name.eq_ignore_ascii_case("nul ")
}

#[cfg(test)]
mod tests {
    use super::*;

    fn make(file_name: &str, size_bytes: u64) -> NulMatch {
        NulMatch {
            absolute_path: format!(r"C:\test\{file_name}"),
            relative_path: file_name.to_string(),
            file_name: file_name.to_string(),
            size_bytes,
            last_write_time_utc: chrono::Utc::now(),
        }
    }

    #[test]
    fn strict_zero_size_matches_exact_nul_names() {
        for name in ["nul", "NUL", "Nul"] {
            assert!(is_match(&make(name, 0), ScanMode::Strict));
        }
    }

    #[test]
    fn strict_non_zero_size_does_not_match() {
        for name in ["nul", "NUL", "Nul", "NUL.TXT", "nul.txt"] {
            assert!(!is_match(&make(name, 1), ScanMode::Strict));
        }
    }

    #[test]
    fn loose_any_size_matches_exact_nul_names() {
        for name in ["nul", "NUL", "Nul"] {
            assert!(is_match(&make(name, 100), ScanMode::Loose));
        }
    }

    #[test]
    fn disqualified_names_never_match() {
        for name in ["nul.txt", "nul.", "nul ", "NUL.TXT", "NUL.", "NUL "] {
            assert!(is_disqualified(name));
        }
    }

    #[test]
    fn unrelated_names_do_not_match() {
        for name in ["other", "com1", "aux", "prn"] {
            assert!(!is_match(&make(name, 0), ScanMode::Strict));
            assert!(!is_match(&make(name, 0), ScanMode::Loose));
        }
    }
}
