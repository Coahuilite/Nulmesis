# AGENTS.md

This file is the primary instruction entry for coding agents working in this repository.

## Scope

- Project: `Nulmesis`
- Platform: Windows-only
- Stack: .NET 8, WPF, CLI, xUnit
- Current reserved-name scope: `nul` only

## Source of truth

When working in this repository, use this priority order:

1. direct user instructions
2. this `AGENTS.md`
3. repository documentation such as `README.md` and `CONTRIBUTING.md`
4. existing code and tests

## Architecture summary

- `src/Nulmesis.Core/`: shared domain models, matching rules, path normalization, scan and delete services
- `src/Nulmesis.App/`: WPF shell, CLI entry, dialogs, view models
- `tests/`: unit, app, and integration tests

## Behavioral constraints

- Keep the tool focused on the reserved name `nul` unless the user explicitly asks to expand scope.
- Match only files whose base name is exactly `nul`.
- Do not treat `nul.txt`, `nul.log`, `nul.backup`, or similar names as matches.
- Do not follow reparse points.
- Keep GUI and CLI behavior aligned through shared core logic.
- Prefer minimal, targeted changes over broad refactors.

## Build and verification

Before reporting completion for code changes, do the smallest relevant verification set:

- run targeted or full tests as appropriate
- verify changed behavior manually when possible
- do not claim publish or release behavior without actually testing it

Baseline command:

```powershell
dotnet test .\Nulmesis.slnx -c Release
```

## Documentation rules

- Write user-facing documentation in clear English unless the file is explicitly localized.
- Keep localized documents linked to the English version.
- Do not include local machine details, private paths, machine-specific identifiers, or personal environment state in committed docs.
- Keep provider-specific guidance out of `AGENTS.md`; this file must remain tool-agnostic.

## Release and packaging guidance

- Treat CI-produced tagged artifacts as the authoritative release outputs.
- Treat local publish outputs as validation artifacts unless the user explicitly asks to change that policy.
- Include platform, architecture, and version information in formal release artifacts when release packaging is being adjusted.

## Multi-agent companion files

If you were invoked through another assistant-specific entry file such as `CLAUDE.md`, `CODEX.md`, or `GEMINI.md`, return here and treat this file as the shared project instruction set.
