use std::fs;
use std::path::{Path, PathBuf};

use crate::path::normalize_reserved_path;

pub struct ReservedNameFixtureBuilder {
    root_path: PathBuf,
}

impl ReservedNameFixtureBuilder {
    pub fn new(root_path: impl AsRef<Path>) -> std::io::Result<Self> {
        let root_path = root_path
            .as_ref()
            .canonicalize()
            .unwrap_or_else(|_| root_path.as_ref().to_path_buf());
        let normalized = normalize_reserved_path(root_path.to_str())
            .expect("root path should convert to string");
        fs::create_dir_all(normalized)?;
        Ok(Self { root_path })
    }

    pub fn root_path(&self) -> &Path {
        &self.root_path
    }

    pub fn create_regular_file(
        &self,
        relative_path: impl AsRef<Path>,
        content: &str,
    ) -> std::io::Result<PathBuf> {
        let full_path = self.full_path(relative_path);
        self.create_parent_directory(&full_path)?;
        let normalized = normalize_reserved_path(full_path.to_str())
            .expect("full path should convert to string");
        fs::write(normalized, content.as_bytes())?;
        Ok(full_path)
    }

    pub fn create_read_only_nul_file(
        &self,
        relative_path: impl AsRef<Path>,
    ) -> std::io::Result<PathBuf> {
        let full_path = self.full_path(relative_path);
        self.create_parent_directory(&full_path)?;
        let normalized = normalize_reserved_path(full_path.to_str())
            .expect("full path should convert to string");
        fs::write(&normalized, [])?;
        let mut permissions = fs::metadata(&normalized)?.permissions();
        permissions.set_readonly(true);
        fs::set_permissions(normalized, permissions)?;
        Ok(full_path)
    }

    fn create_parent_directory(&self, full_path: &Path) -> std::io::Result<()> {
        if let Some(parent) = full_path.parent() {
            let normalized = normalize_reserved_path(parent.to_str())
                .expect("parent path should convert to string");
            fs::create_dir_all(normalized)?;
        }
        Ok(())
    }

    fn full_path(&self, path: impl AsRef<Path>) -> PathBuf {
        let path = path.as_ref();
        if path.is_absolute() {
            path.to_path_buf()
        } else {
            self.root_path.join(path)
        }
    }
}

#[cfg(test)]
mod tests {
    use tempfile::tempdir;

    use super::ReservedNameFixtureBuilder;

    #[test]
    fn creates_read_only_nul_fixture() {
        let temp_dir = tempdir().expect("temp dir should exist");
        let builder =
            ReservedNameFixtureBuilder::new(temp_dir.path()).expect("builder should initialize");
        let path = builder
            .create_read_only_nul_file(r"nested\nul")
            .expect("fixture should be created");

        assert!(path.ends_with(r"nested\nul"));
        assert!(path.exists());
        assert!(std::fs::metadata(path)
            .expect("metadata")
            .permissions()
            .readonly());
    }
}
