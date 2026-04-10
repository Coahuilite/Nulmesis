# AGENTS.md

Primary repository instructions for coding agents.

## Scope

- Project: `Nulmesis`
- Platform: Windows-only
- Stack: Rust, Tauri 2, TypeScript, Vite
- Reserved-name scope: `nul` only

## Source of truth

1. direct user instructions
2. this file
3. `README.md`, `CONTRIBUTING.md`, and CI workflows
4. current code and tests

## Architecture that changes how you work

- `crates/nulmesis-core/` is the shared behavior source of truth for scan/delete logic.
- `crates/nulmesis-cli/` is the CLI entrypoint.
- `apps/desktop/` is the Tauri desktop shell.
- `apps/desktop/src-tauri/` is **excluded from the Rust workspace**. `cargo test` and `cargo build` do **not** validate the desktop crate by themselves.

## Product constraints

- Match only files whose basename is exactly `nul`.
- Do not treat `nul.txt`, `nul.log`, `nul.backup`, or similar names as matches.
- Do not follow reparse points.
- Keep GUI and CLI aligned through the same Rust core.
- Prefer targeted changes over broad refactors.

## Verification commands

- Rust workspace: `cargo test`
- Desktop deps: `npm ci --prefix .\apps\desktop`
- Desktop frontend build: `npm run frontend:build --prefix .\apps\desktop`
- Desktop dirty package: `npx tauri build --no-bundle --config .\apps\desktop\src-tauri\tauri.conf.json`
- CLI smoke: `cargo run -p nulmesis-cli -- --help`

Run the smallest relevant set, but do not claim desktop behavior without actually building or launching the desktop app.

## Release/build workflow facts

- CI validates commit messages with commitlint before build/test.
- Release tags must match `v0.x.y`.
- Release workflow currently produces separate CLI and GUI Windows x64 artifacts.
- Local builds are dirty validation artifacts only.

## Repo hygiene

- Generated non-source files are expected under `artifacts/`, `target/`, `apps/desktop/node_modules/`, `apps/desktop/dist/`, and `apps/desktop/src-tauri/target/`.
- Use `pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\clean-non-source.ps1` to clean non-source files for this repo.

## Documentation constraints

- Keep user-facing docs in English unless the file is explicitly localized.
- Keep localized docs linked to the English version.
- Do not commit local machine details, private paths, or environment-specific state.

## Companion instruction files

If you entered through `CLAUDE.md`, `CODEX.md`, or `GEMINI.md`, return here and treat this file as the shared instruction set.
