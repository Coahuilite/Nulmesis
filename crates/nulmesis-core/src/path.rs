pub fn normalize_reserved_path(path: Option<&str>) -> Option<String> {
    let path = path?;

    if path.is_empty() {
        return Some(String::new());
    }

    if path.starts_with(r"\\?\") {
        return Some(path.to_string());
    }

    if let Some(rest) = path.strip_prefix(r"\\") {
        return Some(format!(r"\\?\UNC\{rest}"));
    }

    if path.len() >= 2 && path.as_bytes()[1] == b':' {
        return Some(format!(r"\\?\{path}"));
    }

    Some(path.to_string())
}

pub fn display_path_from_reserved(path: &str) -> String {
    const EXTENDED_PREFIX: &str = r"\\?\";
    const UNC_EXTENDED_PREFIX: &str = r"\\?\UNC\";

    if let Some(rest) = path.strip_prefix(UNC_EXTENDED_PREFIX) {
        return format!(r"\\{rest}");
    }

    if let Some(rest) = path.strip_prefix(EXTENDED_PREFIX) {
        return rest.to_string();
    }

    path.to_string()
}

#[cfg(test)]
mod tests {
    use super::{display_path_from_reserved, normalize_reserved_path};

    #[test]
    fn local_paths_get_extended_prefix() {
        assert_eq!(
            Some(String::from(r"\\?\C:\file.txt")),
            normalize_reserved_path(Some(r"C:\file.txt"))
        );
        assert_eq!(
            Some(String::from(r"\\?\D:\data\file.txt")),
            normalize_reserved_path(Some(r"D:\data\file.txt"))
        );
    }

    #[test]
    fn unc_paths_get_extended_unc_prefix() {
        assert_eq!(
            Some(String::from(r"\\?\UNC\server\share\file.txt")),
            normalize_reserved_path(Some(r"\\server\share\file.txt"))
        );
    }

    #[test]
    fn already_extended_paths_stay_unchanged() {
        assert_eq!(
            Some(String::from(r"\\?\C:\file.txt")),
            normalize_reserved_path(Some(r"\\?\C:\file.txt"))
        );
    }

    #[test]
    fn empty_and_null_are_preserved() {
        assert_eq!(Some(String::new()), normalize_reserved_path(Some("")));
        assert_eq!(None, normalize_reserved_path(None));
    }

    #[test]
    fn relative_paths_stay_unchanged() {
        assert_eq!(
            Some(String::from(r"relative\path\file.txt")),
            normalize_reserved_path(Some(r"relative\path\file.txt"))
        );
        assert_eq!(
            Some(String::from("justfilename.txt")),
            normalize_reserved_path(Some("justfilename.txt"))
        );
    }

    #[test]
    fn display_paths_drop_extended_prefixes() {
        assert_eq!(
            String::from(r"C:\file.txt"),
            display_path_from_reserved(r"\\?\C:\file.txt")
        );
        assert_eq!(
            String::from(r"\\server\share\file.txt"),
            display_path_from_reserved(r"\\?\UNC\server\share\file.txt")
        );
    }
}
