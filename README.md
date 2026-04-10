# Nulmesis

[中文说明 / Chinese README](./README.zh-CN.md)

Nulmesis is a Windows-only Rust toolset for finding and deleting blocked files whose basename is exactly `nul`.

## What it does

- runs as a dedicated CLI executable through `nulmesis-cli`
- runs as a dedicated Tauri desktop executable through `nulmesis-desktop`
- keeps GUI and CLI aligned through the same shared Rust core while shipping them as separate binaries
- matches only files whose basename is exactly `nul`
- does not treat `nul.txt`, `nul.log`, `nul.backup`, or similar names as matches
- skips reparse points instead of following them
- supports two scan modes:
  - `strict`: exact `nul` with zero-byte size
  - `loose`: exact `nul` regardless of size

## Repository layout

```text
crates/
  nulmesis-core/   shared domain models, scan/delete services, path handling
  nulmesis-cli/    command-line entrypoint and JSON/human output
apps/
  desktop/         Tauri desktop shell, frontend, packaging config
```

## Requirements

- Windows
- Rust toolchain
- Node.js 20+

## Build and test

```powershell
cargo test
```

Desktop frontend build:

```powershell
npm ci --prefix .\apps\desktop
npm run frontend:build --prefix .\apps\desktop
```

Desktop dirty package:

```powershell
npm run build --prefix .\apps\desktop -- --no-bundle --config .\src-tauri\tauri.conf.json
```

## Run from source

CLI examples:

```powershell
cargo run -p nulmesis-cli -- scan --root C:\path\to\target --json
cargo run -p nulmesis-cli -- list --root C:\path\to\target --mode loose
cargo run -p nulmesis-cli -- delete --root C:\path\to\target --mode loose
```

Desktop dev mode:

```powershell
npm run dev --prefix .\apps\desktop
```

Desktop scope note:

- `nulmesis-desktop` is GUI-only and calls the shared Rust core directly
- CLI behavior lives in `nulmesis-cli`; the desktop executable is no longer a CLI fallback host

## Release policy

- CI-produced tagged assets are the authoritative release outputs
- local builds are dirty validation artifacts only
- release filenames must include product surface, platform, architecture, and version
- current formal release assets are separate Windows x64 `.exe` files for CLI and GUI

## Safety model

- current reserved-name scope is `nul` only
- deletion is limited to exact `nul` file targets
- reparse points are skipped
- high-risk roots should require deliberate user confirmation in the GUI

## Documentation for coding agents

If you are an automated coding agent, read [AGENTS.md](./AGENTS.md) first.
