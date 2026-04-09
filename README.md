# Nulmesis

[中文说明 / Chinese README](./README.zh-CN.md)

Nulmesis is a Windows-only desktop and command-line tool for finding and deleting reserved-name `nul` files that are difficult to handle through normal path semantics.

## What it does

- launches a WPF GUI when started without arguments
- runs as a CLI when started with arguments
- shares one core implementation for scanning and deletion
- supports two matching modes:
  - `strict`: base name is exactly `nul` (case-insensitive) and file size is `0`
  - `loose`: base name is exactly `nul` (case-insensitive), regardless of file size
- only treats files whose base name is exactly `nul` as matches
- does not treat `nul.txt`, `nul.log`, `nul.backup`, or other extended names as matches
- skips reparse points instead of following them

## Project status

Nulmesis is currently in the pre-`1.0.0` stage.

- release line: `0.x`
- intended first formal release: `0.1.0`
- official release assets are produced by CI for tagged releases
- official release filenames include platform, architecture, and version
- local manual publish outputs are validation artifacts, not formal release assets

## Repository layout

```text
src/
  Nulmesis.Core/   shared domain models, matching rules, scanner, deleter
  Nulmesis.App/    WPF shell, CLI entry, dialogs, view models
tests/
  Nulmesis.Core.Tests/
  Nulmesis.App.Tests/
  Nulmesis.IntegrationTests/
```

## Requirements

- Windows
- .NET 8 SDK for local build and test

## Build and test

```powershell
dotnet test .\Nulmesis.slnx -c Release
```

## Run from source

GUI:

```powershell
dotnet run --project .\src\Nulmesis.App
```

CLI examples:

```powershell
dotnet run --project .\src\Nulmesis.App -- scan --root C:\path\to\target --json
dotnet run --project .\src\Nulmesis.App -- list --root C:\path\to\target
dotnet run --project .\src\Nulmesis.App -- delete --root C:\path\to\target
```

## Safety model

- deletion is limited to detected delete targets
- the tool is intentionally scoped to the `nul` reserved name only
- tests are designed to use isolated temporary directories rather than real working folders

## Documentation for coding agents

If you are an automated coding agent, read [AGENTS.md](./AGENTS.md) first.
