# Protocol (Normative)

## Transport
- Channel: stdio
- Framing: `Content-Length: <bytes>\r\n\r\n<json>`
- `stdout`: JSON-RPC/MCP only
- `stderr`: logs only

## JSON-RPC + MCP methods
- `initialize`
- `initialized` (notification)
- `tools/list`
- `tools/call`

Direct `fs.*` method calls are accepted for diagnostics, but clients should prefer `tools/call`.

## Tool envelope (normative)
All tools return this structure in `result.structuredContent` (and mirrored in `content[0].text`):

```json
{
  "ok": true,
  "data": {}
}
```

or

```json
{
  "ok": false,
  "errorCode": "INVALID_RANGE",
  "message": "...",
  "data": {}
}
```

JSON-RPC `error` is reserved for protocol-level failures (`invalid request`, `method not found`, `invalid params`).

## Tool list (P0)
- `fs.capabilities`
- `fs.root_detect`
- `fs.health`
- `fs.scan`
- `fs.readDir`
- `fs.stat`
- `fs.search`
- `fs.open`
- `fs.patch`
- `fs.patchPreview`

## Limits and hard caps (P0)

| Area | Config defaults | Hard cap |
|---|---:|---:|
| `open.maxBytes` | 65536 | 131072 |
| `open.maxLines` | 200 | 1000 |
| `search.maxResults` | 100 | 500 |
| `search.snippetBytes` | 220 | 2000 |
| `search.maxFilesScanned` | 5000 | 20000 |
| `search.maxFileSizeBytes` | 2097152 | 16777216 |
| `search.timeoutMs` | 5000 | 15000 |
| `patch.maxBytes` | 262144 | 1048576 |
| `patch.maxEdits` | 50 | 200 |
| `patch.maxFileSizeBytes` | 2097152 | 16777216 |
| `scan.limit` | 500 | 5000 |
| `scan.maxDepth` | 16 | 64 |

Effective limits are `min(request, config, hardCap)` when request-side override exists.

## Path and sandbox rules (normative)
All path-based tools (`scan/search/open/patch/patchPreview/stat/readDir`) enforce:
1. Empty/null path rejected (`INVALID_PATH`)
2. Null-byte rejected (`INVALID_PATH`)
3. Absolute/UNC/drive-letter rejected (`INVALID_PATH`)
4. Resolve with workspace root + normalize
5. Outside-root rejected (`OUTSIDE_ROOT`)
6. Symlink policy:
- `followSymlinks=false` (default): symlink access denied (`PERMISSION_DENIED`), symlink entries skipped in traversal tools
- `followSymlinks=true`: symlink target must still be inside root, otherwise `OUTSIDE_ROOT`

## Root detection order (normative)
1. `MCP_FS_ROOT`
2. config `workspaceRoot`
3. upward `.git`
4. upward `*.sln` or `global.json`
5. upward `package.json` or `pnpm-workspace.yaml`
6. upward `pyproject.toml`
7. fallback `cwd`

`fs.root_detect.data.reason` values: `env | config | git | sln | node | python | cwd`.

## `.gitignore` subset (normative)
Implemented behavior is best-effort and intentionally not byte-for-byte git parity.

Supported tokens:
- `*`
- `?`
- `**` (globstar)
- root anchor `/`
- negation `!`
- trailing `/` directory pattern

Built-in default ignores:
- `.git`
- `bin`
- `obj`
- `node_modules`
- `dist`
- `.idea`
- `.vs`

Scope in v1.0 P0: root `.gitignore` only.

## Tool schemas and examples

### `fs.capabilities`
Arguments:
```json
{}
```
Data fields include runtime info, defaults, hard caps, and features.

### `fs.root_detect`
Arguments:
```json
{}
```
Data:
```json
{
  "root": "/abs/workspace",
  "reason": "git"
}
```

### `fs.health`
Arguments:
```json
{}
```
Data:
```json
{
  "status": "ok",
  "version": "1.0.0",
  "uptimeMs": 1234,
  "root": "/abs/workspace",
  "followSymlinks": false,
  "limits": { "openMaxBytes": 65536 }
}
```

### `fs.scan`
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
  "entries": [
    {
      "path": "src/McpFs/Program.cs",
      "kind": "file",
      "size": 1240,
      "mtimeUtc": "2026-03-04T18:00:00+00:00",
      "quickHash8": "1a2b3c4d"
    }
  ],
  "truncated": false
}
```

### `fs.readDir`
Arguments:
```json
{
  "path": "src",
  "includeFiles": true,
  "includeDirs": true,
  "limit": 100
}
```
Data:
```json
{
  "entries": [
    {
      "name": "Program.cs",
      "path": "src/McpFs/Program.cs",
      "kind": "file",
      "size": 1024,
      "mtimeUtc": "2026-03-04T18:00:00+00:00",
      "isSymlink": false
    }
  ],
  "truncated": false
}
```

### `fs.stat`
Arguments:
```json
{
  "path": "src/McpFs/Program.cs"
}
```
File data:
```json
{
  "path": "src/McpFs/Program.cs",
  "kind": "file",
  "size": 1024,
  "mtimeUtc": "2026-03-04T18:00:00+00:00",
  "contextHash": "...",
  "quickHash8": "...",
  "isSymlink": false
}
```

### `fs.search`
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
  "snippetBytes": 220,
  "maxFilesScanned": 5000,
  "maxFileSizeBytes": 2097152,
  "timeoutMs": 5000
}
```
Data:
```json
{
  "engine": "rg",
  "results": [
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
      "contextHash": "..."
    }
  ],
  "truncated": false
}
```

### `fs.open`
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
  "endLine": 120,
  "text": "using ...",
  "contextHash": "...",
  "fileSize": 4096,
  "truncated": false
}
```

### `fs.patch`
Arguments:
```json
{
  "path": "sample.txt",
  "preHash": "...",
  "mode": "strict",
  "edits": [
    { "op": "insert", "line": 1, "col": 1, "text": "prefix " },
    { "op": "replace", "startLine": 2, "startCol": 1, "endLine": 2, "endCol": 5, "text": "done" },
    { "op": "delete", "startLine": 3, "startCol": 1, "endLine": 3, "endCol": 2 }
  ]
}
```
Data:
```json
{
  "postHash": "...",
  "appliedEditsCount": 3,
  "bytesChanged": 14,
  "lineDelta": 0,
  "summary": "Applied 3 edit(s) to sample.txt."
}
```

### `fs.patchPreview`
Arguments: same as `fs.patch`

Data:
```json
{
  "wouldApply": true,
  "postHash": "...",
  "bytesChanged": 14,
  "lineDelta": 0,
  "diffSummary": {
    "path": "sample.txt",
    "editCount": 3,
    "bytesChanged": 14,
    "lineDelta": 0,
    "editSummaries": ["insert:0-0", "replace:...", "delete:..."]
  }
}
```

## Error code dictionary
- `NOT_FOUND`: file/dir/path not found
- `PERMISSION_DENIED`: ACL denied or symlink forbidden by policy
- `INVALID_PATH`: malformed path, null-char, absolute/UNC/drive input
- `OUTSIDE_ROOT`: normalized path or resolved symlink target escapes root
- `INVALID_RANGE`: invalid line/column/range/edit payload
- `HASH_MISMATCH`: `preHash` differs from current file hash
- `TOO_LARGE`: caps exceeded (file size, patch size, edits)
- `RATE_LIMITED`: reserved for host-level policies
- `INTERNAL_ERROR`: unexpected tool-side failure
