# mcp-fs

## What is mcp-fs?
`mcp-fs` is a local-first MCP server for high-signal file operations during agentic coding. It runs over stdio JSON-RPC and focuses on fast file discovery, minimal context search results, range-based reads, and hash-guarded patching.

The server is built for offline use. No network calls, no telemetry, no usage tracking.

## Features (MVP)
- `fs.capabilities`: Runtime, defaults, and tool availability.
- `fs.root_detect`: Deterministic workspace root detection.
- `fs.scan`: Ignore-aware file and directory listing with depth and result limits.
- `fs.search`: Fast content search with `rg` fallback, short snippets, bounded results.
- `fs.open`: Range-based file reads with strict byte limits.
- `fs.patch`: Atomic, strict pre-hash guarded text edits.

## Non-goals
- No remote service calls, update checks, telemetry, or analytics.
- No full repository content dump APIs.
- No delete/rename/recursive destructive file operations in MVP.
- No IDE/editor integration in this repository.
- No heavyweight indexing database in MVP.

## Quick Start
### 1) Build
```bash
dotnet restore
dotnet build -c Release
```

### 2) Run from source
```bash
dotnet run --project src/McpFs/McpFs.csproj -c Release
```

### 3) Publish single-file binary (self-contained)
Linux example:
```bash
dotnet publish src/McpFs/McpFs.csproj -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -o ./artifacts/linux-x64
```

Windows example:
```bash
dotnet publish src/McpFs/McpFs.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./artifacts/win-x64
```

macOS example:
```bash
dotnet publish src/McpFs/McpFs.csproj -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -o ./artifacts/osx-arm64
```

### 4) Verify in 5 minutes
- Start server process.
- Send `initialize`, then `tools/list`, then `tools/call` with `fs.capabilities`.
- See JSON-RPC response on `stdout`; logs only on `stderr`.

### NativeAOT (optional target)
NativeAOT is optional and not the default release artifact in `v0.1.0`.  
If you want to experiment locally:
```bash
dotnet publish src/McpFs/McpFs.csproj -c Release -r linux-x64 -p:PublishAot=true
```

## How to connect to an MCP host
`mcp-fs` is a stdio MCP server. Any MCP-compatible host can launch it as a subprocess and speak JSON-RPC over stdio with `Content-Length` framing.

Host-specific snippets and examples are in [docs/usage.md](docs/usage.md).

## Tool reference
Tool schemas, JSON-RPC framing, and examples are in [docs/protocol.md](docs/protocol.md).

## Configuration
Configuration file examples and runtime options are in [docs/usage.md](docs/usage.md).

## Security model
- Workspace sandbox root is mandatory; all paths are resolved relative to it.
- Traversal and outside-root access are blocked.
- Absolute input paths are rejected.
- Symlink follow is disabled by default (`followSymlinks=false`).
- `fs.patch` requires `preHash` and writes atomically (`temp + replace`).
- No destructive file APIs in MVP.

## Performance notes
Why outputs are minimal:
- Agent context is expensive and noisy when payloads are large.
- `fs.search` returns small snippets (default `220` bytes), not full files.
- `fs.open` is range + byte bounded (default `64KB`).
- Scan/search stop early when `limit`/`maxResults` is reached.

Details and tuning: [docs/performance.md](docs/performance.md).

## Usage scenarios
### Agent dosya bulsun
1. `fs.root_detect` ile çalışma kökünü doğrula.
2. `fs.scan` ile ignore-aware liste al (`limit`, `maxDepth` ver).
3. `fs.search` ile dar snippet tabanlı eşleşmeleri topla.

### Agent pattern arayıp küçük patch uygulasın
1. `fs.search` ile hedef satırları bul.
2. `fs.open` ile küçük range oku.
3. `fs.patch` çağrısında `preHash` + minimal `edits` gönder.

### Hash mismatch olduğunda nasıl ilerlenir?
1. `HASH_MISMATCH` alınırsa dosya değişmiştir.
2. `fs.open` ile güncel context al.
3. Güncel `contextHash` ile yeni patch üret.
4. Tekrar `fs.patch` çağır.

## Roadmap
- File watcher + incremental in-memory index.
- Deterministic `best_effort` patch mode with anchor strategy.
- NativeAOT publishing profile (documented, optional target).

## Contributing
See [CONTRIBUTING.md](CONTRIBUTING.md).

## License
MIT. See [LICENSE](LICENSE).
