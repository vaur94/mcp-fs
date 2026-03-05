# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0](https://github.com/vaur94/mcp-fs/compare/v1.0.0...v1.1.0) (2026-03-04)


### Features

* initial mcp-fs v0.1.0 ([a07dec0](https://github.com/vaur94/mcp-fs/commit/a07dec0eee6fc04d6d558fce79f1d53aaf4e10e0))

## [Unreleased]
### Changed
- Replaced legacy hub wording with MCPHub (`vaur94/mcphub`) in docs.
- Added MCPHub startup guidance for both installed binary and `dotnet run` modes.
- Converted `samples/workers/mcp-fs.worker.json` to MCPHub `mcp_settings.json` format.

## [1.0.0] - 2026-03-04
### Added
- New P0 tools: `fs.health`, `fs.readDir`, `fs.stat`, `fs.patchPreview`.
- Search caps: `maxFilesScanned`, `maxFileSizeBytes`, `timeoutMs` with hard caps.
- Patch caps: `patchMaxBytes`, `patchMaxEdits`, `patchMaxFileSizeBytes` with hard caps.
- Open hard caps for line/byte limits and `fileSize`/`truncated` response fields.
- EOL normalization and UTF-8 BOM preservation in patch pipeline.
- Root detection reason normalization: `env|config|git|sln|node|python|cwd`.
- New normative security document (`docs/security.md`).
- MCPHub worker sample and v1 config sample.

### Changed
- Version bumped to `1.0.0`.
- Router and tool schemas now expose full P0 tool set.
- Config loader now supports CLI arguments (`--root`, `--config`) and `mcp-fs.config.json`.
- CI and release coverage expanded for v1.0 packaging and quality gates.

### Removed
- `best_effort` patch behavior is no longer accepted in runtime mode validation.

## [0.1.0] - 2026-03-04
### Added
- Initial product-grade MVP for local MCP filesystem server.
- stdio JSON-RPC host with MCP-compatible `initialize`, `tools/list`, `tools/call`.
- Tools: `fs.capabilities`, `fs.root_detect`, `fs.scan`, `fs.search`, `fs.open`, `fs.patch`.
- Workspace root detection with deterministic heuristics.
- Root sandbox path policy and outside-root protection.
- Ignore system with built-in ignores and root `.gitignore` support.
- Hash standard: `SHA256(size + first64KB + last64KB)` with strict pre-hash patching.
- Atomic patch writer.
- Search engines: ripgrep integration + streaming fallback.
- Cross-platform CI and release workflows.
- Documentation set, templates, and public-repo governance files.
