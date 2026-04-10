# Desktop shell plan

This directory now contains the initial Tauri shell scaffold.

Layout:

- `apps/desktop/src-tauri/` for the Rust Tauri backend
- `apps/desktop/src/` for the web UI shell

The desktop shell will call shared Rust core services directly instead of spawning the CLI by default.

Current scaffold status:

- backend command bridge exists
- frontend placeholder exists
- workspace build still prioritizes `nulmesis-core` and `nulmesis-cli`
- Tauri packaging and richer UI come after core/CLI parity
