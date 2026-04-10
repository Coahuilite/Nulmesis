# Contributing

## Version policy

- Current development stays in the `0.x` phase.
- The initial release line starts at `0.1.0`.
- `1.0.0` remains blocked until the project explicitly leaves the pre-1.0 stage.

## Commit message format

Follow Conventional Commits:

```text
<type>(<scope>): <subject>
```

Allowed `type` values:

- `feat`
- `fix`
- `docs`
- `refactor`
- `test`
- `chore`
- `build`
- `ci`
- `perf`
- `release`

Suggested `scope` values:

- `core`
- `cli`
- `desktop`
- `release`
- `docs`

## Release tagging

Only `v0.x.y` tags should trigger the release pipeline.

## Release asset naming

- Formal release assets must include product surface, platform, architecture, and version in the filename.
- Current target naming pattern:
  - `Nulmesis-cli-windows-x64-v0.x.y.exe`
  - `Nulmesis-gui-windows-x64-v0.x.y.exe`
- Local manual builds are dirty validation artifacts and must not be treated as formal release assets.

## Pull requests

- Target the repository default branch.
- Make sure commits pass commitlint, Rust tests, and relevant desktop build checks.
- Describe the change, its risk, and the validation you performed.
