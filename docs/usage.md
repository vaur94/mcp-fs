# Usage Guide

## Build and run

From source:
```bash
dotnet restore
dotnet build -c Release
dotnet run --project src/McpFs/McpFs.csproj -c Release -- --config ./mcp-fs.config.json
```

Binary:
```bash
./mcp-fs --config ./mcp-fs.config.json
```

## CLI arguments
- `--root <path>`: force workspace root
- `--config <path>`: config file path

## Config file
Default lookup order:
1. `--config`
2. `MCP_FS_CONFIG`
3. `./mcp-fs.config.json`
4. legacy `./mcp-fs.json`

Sample: [samples/mcp-fs.config.json.sample](../samples/mcp-fs.config.json.sample)

## mcp-hub worker example
Use [samples/workers/mcp-fs.worker.json](../samples/workers/mcp-fs.worker.json).

Important points:
- Keep `toolPrefix` as `fs` to expose tools as `fs.*`.
- Set `callToolMs` to at least search timeout hard cap (`15000`).
- Do not enable verbose stdout logging in worker wrappers.

## Minimal MCP flow
1. `initialize`
2. `tools/list`
3. `tools/call`

Example (`fs.health`):
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/call",
  "params": {
    "name": "fs.health",
    "arguments": {}
  }
}
```

## Context-minimal usage patterns
- Use `fs.scan` or `fs.readDir` before opening files.
- Use `fs.search` snippets, then `fs.open` narrow ranges.
- Use `fs.patchPreview` before `fs.patch` on risky edits.
- On `HASH_MISMATCH`, refresh with `fs.open` and re-generate patch.

## Common troubleshooting
- `INVALID_PATH`: send workspace-relative path only.
- `OUTSIDE_ROOT`: requested path escapes root after normalization.
- `PERMISSION_DENIED`: permission issue or symlink blocked by policy.
- `TOO_LARGE`: reduce request scope or tune config (up to hard caps).
