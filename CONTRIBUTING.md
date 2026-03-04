# Contributing

Thanks for contributing to `mcp-fs`.

## Development setup
1. Install .NET 10 SDK.
2. Clone repository.
3. Run:
```bash
dotnet restore
dotnet build -c Release
dotnet test -c Release
dotnet format mcp-fs.sln --verify-no-changes
rg -n "TECH_DEBT_MARKER" .
```

## Branch and PR flow
- Branch from `main`.
- Keep PRs focused and small.
- Add tests for behavior changes.
- Update docs if protocol or tool behavior changes.
- Update `CHANGELOG.md` under `Unreleased`.

## Coding standards
- Use workspace-relative, security-first path handling.
- Keep tool responses deterministic: `{ ok, errorCode?, message?, data? }`.
- Avoid unbounded output and context-heavy responses.
- Prefer minimal allocations in hot paths.

## Commit style
Use clear, imperative messages. Example:
- `Implement strict hash-guarded patch pipeline`
- `Add fallback search line/column tests`

## Pull request checklist
- [ ] `dotnet build -c Release` passes
- [ ] `dotnet test -c Release` passes
- [ ] `dotnet format mcp-fs.sln --verify-no-changes` passes
- [ ] `rg -n "TECH_DEBT_MARKER" .` returns empty
- [ ] docs updated (`README`, `docs/*`) when needed
- [ ] changelog updated
- [ ] no unresolved debt markers in production code

## PR template
See [.github/PULL_REQUEST_TEMPLATE.md](.github/PULL_REQUEST_TEMPLATE.md).
