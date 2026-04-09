# Contributing

## Version policy

- Current development stays in the `0.x` phase.
- The initial release line starts at `0.1.0`.
- `0.0.1` is not an accepted formal starting version.
- `1.0.0` is currently blocked until the project explicitly leaves the pre-1.0 stage.

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

Suggested `scope` values:

- `core`
- `cli`
- `wpf`
- `release`
- `tests`
- `docs`

Examples:

```text
feat(core): add note aggregation pipeline
fix(wpf): keep sidebar selection after refresh
ci(release): add github actions and semver governance for 0.x
docs(docs): clarify 0.x support policy
```

## Release tagging

Only `v0.x.y` tags should trigger the release pipeline.

Examples:

- `v0.1.0`
- `v0.2.0`
- `v0.2.1`

Do not create `v1.0.0` yet.

## Release asset naming

- Formal release assets must include platform, architecture, and version in the filename.
- Current release naming pattern:
  - `Nulmesis-windows-x64-v0.x.y.exe`
  - `Nulmesis-windows-x64-v0.x.y.zip`
  - `Nulmesis-windows-x86-v0.x.y.exe`
  - `Nulmesis-windows-x86-v0.x.y.zip`
- Local manual publishes are validation artifacts and must not be treated as formal release assets.

## Pull requests

- Target the repository default branch.
- Make sure commits pass commitlint, build, and test checks.
- Describe the change, its risk, and the validation you performed.
