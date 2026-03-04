# Protocol

## Transport
- Channel: stdio
- Framing: `Content-Length: <bytes>\r\n\r\n<json>`
- `stdout`: JSON-RPC only
- `stderr`: diagnostics/logs only

## JSON-RPC envelope
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

Response:
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "content": [{"type": "text", "text": "..."}],
    "structuredContent": {
      "ok": true,
      "data": {}
    },
    "isError": false
  }
}
```

Tool response standard (deterministic):
```json
{
  "ok": true,
  "errorCode": null,
  "message": null,
  "data": {}
}
```

## MCP methods
- `initialize`
- `initialized` (notification)
- `tools/list`
- `tools/call`

Direct method calls (`fs.*`) are also accepted for diagnostics/testing.

## Tool schemas and examples

### fs.capabilities
Arguments:
```json
{}
```

Data:
```json
{
  "os": "Linux 6.13.4...",
  "arch": "X64",
  "pathSeparator": "/",
  "version": "0.1.0",
  "toolAvailability": {"ripgrep": true},
  "defaults": {
    "searchMaxResults": 100,
    "searchSnippetBytes": 220,
    "openMaxBytes": 65536,
    "scanLimit": 500,
    "followSymlinks": false
  }
}
```

### fs.root_detect
Arguments:
```json
{}
```

Data:
```json
{
  "rootPath": "/abs/workspace",
  "detectionReason": "upward:.git"
}
```

### fs.scan
Arguments:
```json
{
  "root": "src",
  "maxDepth": 3,
  "includeGlobs": ["**/*.cs"],
  "excludeGlobs": ["**/*Generated*.cs"],
  "limit": 200
}
```

Data:
```json
{
  "root": "/abs/workspace/src",
  "items": [
    {
      "path": "src/McpFs/Program.cs",
      "kind": "file",
      "size": 1240,
      "mtimeUtc": "2026-03-04T18:00:00+00:00",
      "quickHash8": "1a2b3c4d"
    },
    {
      "path": "src/McpFs/Core",
      "kind": "dir",
      "mtimeUtc": "2026-03-04T18:00:00+00:00"
    }
  ],
  "truncated": false
}
```

### fs.search
Arguments:
```json
{
  "query": "PathPolicy",
  "root": "src",
  "regex": false,
  "caseSensitive": false,
  "glob": ["**/*.cs"],
  "excludeGlob": ["**/obj/**"],
  "maxResults": 100,
  "snippetBytes": 220
}
```

Data:
```json
{
  "engine": "rg",
  "matches": [
    {
      "path": "src/McpFs/Core/PathPolicy.cs",
      "line": 8,
      "col": 21,
      "snippet": "...public sealed class PathPolicy...",
      "range": {
        "startLine": 8,
        "startCol": 21,
        "endLine": 8,
        "endCol": 31
      },
      "contextHash": "9d7e..."
    }
  ],
  "truncated": false
}
```

### fs.open
Arguments:
```json
{
  "path": "src/McpFs/Program.cs",
  "startLine": 1,
  "endLine": 120,
  "maxBytes": 65536
}
```

Data:
```json
{
  "path": "src/McpFs/Program.cs",
  "startLine": 1,
  "endLine": 60,
  "text": "using McpFs.Config;\n...",
  "contextHash": "d0ab..."
}
```

### fs.patch
Arguments:
```json
{
  "path": "sample.txt",
  "preHash": "8a4f...",
  "mode": "strict",
  "edits": [
    {
      "op": "insert",
      "at": {"line": 1, "col": 1},
      "text": "prefix "
    },
    {
      "op": "replace",
      "range": {"startLine": 2, "startCol": 1, "endLine": 2, "endCol": 5},
      "text": "done"
    },
    {
      "op": "delete",
      "range": {"startLine": 3, "startCol": 1, "endLine": 3, "endCol": 2}
    }
  ]
}
```

Data:
```json
{
  "postHash": "70e3...",
  "appliedEditsCount": 3,
  "summary": "Applied 3 edit(s) to sample.txt."
}
```

Note: `mode=best_effort` exists in schema for forward compatibility. In `v0.1.0` it intentionally returns `HASH_MISMATCH` on hash mismatch and does not write.

## Error codes
Tool-level `errorCode` values:
- `NOT_FOUND`
- `PERMISSION_DENIED`
- `INVALID_PATH`
- `OUTSIDE_ROOT`
- `INVALID_RANGE`
- `HASH_MISMATCH`
- `TOO_LARGE`
- `RATE_LIMITED`
- `INTERNAL_ERROR`
