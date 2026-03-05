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

One-command install (release binary + SHA256 verification):
```bash
curl -fsSL https://raw.githubusercontent.com/vaur94/mcp-fs/main/install.sh | bash
```

See full guide: [docs/install.md](./install.md)

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

## MCPHub integration (`vaur94/mcphub`)
Reference: https://github.com/vaur94/mcphub

Sample settings file: [samples/workers/mcp-fs.worker.json](../samples/workers/mcp-fs.worker.json)

### Mode 1: Installed binary
Use this server block in `mcp_settings.json`:
```json
{
  "mcpServers": {
    "mcp-fs": {
      "type": "stdio",
      "command": "/home/you/.local/bin/mcp-fs",
      "args": [
        "--config",
        "/home/you/Projects/mcp-fs/samples/mcp-fs.config.json.sample"
      ],
      "env": {
        "MCP_FS_ROOT": "/home/you/Projects"
      }
    }
  }
}
```

### Mode 2: Run from source (`dotnet run`)
Use this server block in `mcp_settings.json`:
```json
{
  "mcpServers": {
    "mcp-fs-source": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/home/you/Projects/mcp-fs/src/McpFs/McpFs.csproj",
        "-c",
        "Release",
        "--",
        "--config",
        "/home/you/Projects/mcp-fs/samples/mcp-fs.config.json.sample"
      ],
      "env": {
        "MCP_FS_ROOT": "/home/you/Projects"
      }
    }
  }
}
```

Important points:
- Use absolute paths for `command`, `args`, and `MCP_FS_ROOT`.
- MCPHub stdio transport starts processes with `cwd` under home directory; relative paths can fail.
- `MCP_FS_ROOT` defines the sandbox root seen by `mcp-fs`.

Verification:
1. Start MCPHub and open dashboard (`http://localhost:3000`).
2. Confirm server status is `connected`.
3. Confirm tools list includes `fs.capabilities`, `fs.open`, `fs.patch`, and other `fs.*` tools.

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
