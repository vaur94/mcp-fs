# Usage Guide

## Install and Run

### Option A: Build locally
```bash
git clone https://github.com/<your-org>/mcp-fs.git
cd mcp-fs
dotnet build -c Release
dotnet run --project src/McpFs/McpFs.csproj -c Release
```

### Option B: Use release binary
1. Download the correct binary from GitHub Releases (`linux-x64`, `win-x64`, `osx-x64`, `osx-arm64`).
2. Mark executable if needed:
```bash
chmod +x mcp-fs
```
3. Run:
```bash
./mcp-fs
```

## Configuration

By default, the server loads `mcp-fs.json` from current directory.
You can also set `MCP_FS_CONFIG=/path/to/file.json`.

### Example config
```json
{
  "workspaceRoot": "/path/to/your/workspace",
  "followSymlinks": false,
  "searchMaxResults": 100,
  "searchSnippetBytes": 220,
  "openMaxBytes": 65536,
  "openMaxLines": 200,
  "scanLimit": 500,
  "scanMaxDepth": 16,
  "logLevel": "info"
}
```

A full sample exists at [samples/example-config/mcp-fs.json](../samples/example-config/mcp-fs.json).

## Ignore behavior
- Built-in ignores: `.git`, `bin`, `obj`, `node_modules`, `dist`, `.idea`, `.vs`.
- Root `.gitignore` is respected.
- Search and scan also accept extra `glob` / `excludeGlob` style filters.

## MCP host wiring (generic stdio)
Your host should spawn `mcp-fs` and communicate via stdio JSON-RPC with `Content-Length` framing.

High-level sequence:
1. Send `initialize`.
2. Send `tools/list`.
3. Send `tools/call` with a tool name and arguments.

## Example calls (minimal)

### 1) capabilities
Request:
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/call",
  "params": {
    "name": "fs.capabilities",
    "arguments": {}
  }
}
```

Response (shape):
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "content": [{"type": "text", "text": "..."}],
    "structuredContent": {
      "ok": true,
      "data": {
        "os": "...",
        "arch": "...",
        "toolAvailability": {"ripgrep": true}
      }
    },
    "isError": false
  }
}
```

### 2) scan
Request:
```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "method": "tools/call",
  "params": {
    "name": "fs.scan",
    "arguments": {
      "maxDepth": 2,
      "limit": 50
    }
  }
}
```

Response data fields:
```json
{
  "ok": true,
  "data": {
    "root": "/abs/path",
    "items": [
      {"path": "src/McpFs/Program.cs", "kind": "file", "size": 1200, "quickHash8": "ab12cd34"}
    ],
    "truncated": false
  }
}
```

### 3) search
Request:
```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "method": "tools/call",
  "params": {
    "name": "fs.search",
    "arguments": {
      "query": "HASH_MISMATCH",
      "maxResults": 20,
      "snippetBytes": 220
    }
  }
}
```

Response data fields:
```json
{
  "ok": true,
  "data": {
    "engine": "rg",
    "matches": [
      {
        "path": "src/McpFs/Tools/PatchTool.cs",
        "line": 64,
        "col": 47,
        "snippet": "...preHash mismatch...",
        "range": {"startLine": 64, "startCol": 47, "endLine": 64, "endCol": 60},
        "contextHash": "..."
      }
    ],
    "truncated": false
  }
}
```

### 4) open
Request:
```json
{
  "jsonrpc": "2.0",
  "id": 4,
  "method": "tools/call",
  "params": {
    "name": "fs.open",
    "arguments": {
      "path": "README.md",
      "startLine": 1,
      "endLine": 30,
      "maxBytes": 4096
    }
  }
}
```

Response data fields:
```json
{
  "ok": true,
  "data": {
    "path": "README.md",
    "startLine": 1,
    "endLine": 30,
    "text": "# mcp-fs\n...",
    "contextHash": "..."
  }
}
```

### 5) patch
Request:
```json
{
  "jsonrpc": "2.0",
  "id": 5,
  "method": "tools/call",
  "params": {
    "name": "fs.patch",
    "arguments": {
      "path": "sample.txt",
      "preHash": "<contextHash>",
      "mode": "strict",
      "edits": [
        {
          "op": "replace",
          "range": {"startLine": 1, "startCol": 1, "endLine": 1, "endCol": 6},
          "text": "hello"
        }
      ]
    }
  }
}
```

Response data fields:
```json
{
  "ok": true,
  "data": {
    "postHash": "...",
    "appliedEditsCount": 1,
    "summary": "Applied 1 edit(s) to sample.txt."
  }
}
```

## Why output is minimal
- Agent context window is limited and expensive.
- Returning full-file content for every search result increases hallucination risk and token waste.
- `mcp-fs` intentionally returns concise snippets + range/hash references so clients can fetch only needed ranges.
