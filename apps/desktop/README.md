# Desktop shell plan

This directory contains the Tauri desktop shell for Nulmesis.

Layout:

- `apps/desktop/src-tauri/` for the Rust Tauri backend
- `apps/desktop/src/` for the web UI shell

The desktop shell calls shared Rust core services directly and is now intentionally GUI-only.

Current status:

- backend command bridge exists
- frontend shell exists
- GUI and CLI ship as separate binaries
- desktop release builds are optimized for a small Windows GUI executable
