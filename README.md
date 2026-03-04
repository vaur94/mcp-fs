# mcp-fs

Language: [English](#english-en) | [Türkçe](#turkce-tr)

## English (EN)

### What is mcp-fs?
`mcp-fs` is a local-first MCP server for high-signal file operations during agentic coding. It runs over stdio JSON-RPC and focuses on fast file discovery, minimal context search results, range-based reads, and hash-guarded patching.

The server is built for offline use. No network calls, no telemetry, no usage tracking.

### Features (MVP)
- `fs.capabilities`: Runtime, defaults, and tool availability.
- `fs.root_detect`: Deterministic workspace root detection.
- `fs.scan`: Ignore-aware file and directory listing with depth and result limits.
- `fs.search`: Fast content search with `rg` fallback, short snippets, bounded results.
- `fs.open`: Range-based file reads with strict byte limits.
- `fs.patch`: Atomic, strict pre-hash guarded text edits.

### Non-goals
- No remote service calls, update checks, telemetry, or analytics.
- No full repository content dump APIs.
- No delete/rename/recursive destructive file operations in MVP.
- No IDE/editor integration in this repository.
- No heavyweight indexing database in MVP.

### Quick Start
#### 1) Build
```bash
dotnet restore
dotnet build -c Release
```

#### 2) Run from source
```bash
dotnet run --project src/McpFs/McpFs.csproj -c Release
```

#### 3) Publish single-file binary (self-contained)
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

#### 4) Verify in 5 minutes
- Start server process.
- Send `initialize`, then `tools/list`, then `tools/call` with `fs.capabilities`.
- See JSON-RPC response on `stdout`; logs only on `stderr`.

### NativeAOT (optional target)
NativeAOT is optional and not the default release artifact in `v0.1.0`.

```bash
dotnet publish src/McpFs/McpFs.csproj -c Release -r linux-x64 -p:PublishAot=true
```

### How to connect to an MCP host
`mcp-fs` is a stdio MCP server. Any MCP-compatible host can launch it as a subprocess and speak JSON-RPC over stdio with `Content-Length` framing.

Host-specific snippets and examples are in [docs/usage.md](docs/usage.md).

### Tool reference
Tool schemas, JSON-RPC framing, and examples are in [docs/protocol.md](docs/protocol.md).

### Configuration
Configuration file examples and runtime options are in [docs/usage.md](docs/usage.md).

### Security model
- Workspace sandbox root is mandatory; all paths are resolved relative to it.
- Traversal and outside-root access are blocked.
- Absolute input paths are rejected.
- Symlink follow is disabled by default (`followSymlinks=false`).
- `fs.patch` requires `preHash` and writes atomically (`temp + replace`).
- No destructive file APIs in MVP.

### Performance notes
Why outputs are minimal:
- Agent context is expensive and noisy when payloads are large.
- `fs.search` returns small snippets (default `220` bytes), not full files.
- `fs.open` is range + byte bounded (default `64KB`).
- Scan/search stop early when `limit`/`maxResults` is reached.

Details and tuning: [docs/performance.md](docs/performance.md).

### Usage scenarios
#### Let the agent find files
1. Call `fs.root_detect` to confirm workspace root.
2. Use `fs.scan` for ignore-aware listing (`limit`, `maxDepth`).
3. Use `fs.search` for short snippet-based matches.

#### Let the agent search and apply a small patch
1. Use `fs.search` to locate target lines.
2. Use `fs.open` to read a small range.
3. Call `fs.patch` with `preHash` and minimal `edits`.

#### What to do on hash mismatch
1. If `HASH_MISMATCH` is returned, the file changed.
2. Re-read fresh context with `fs.open`.
3. Regenerate patch using current `contextHash`.
4. Retry `fs.patch`.

### Roadmap
- File watcher + incremental in-memory index.
- Deterministic `best_effort` patch mode with anchor strategy.
- NativeAOT publishing profile (documented, optional target).

### Contributing
See [CONTRIBUTING.md](CONTRIBUTING.md).

### License
MIT. See [LICENSE](LICENSE).

---

## Turkce (TR)

### mcp-fs nedir?
`mcp-fs`, agentic coding akışında yüksek sinyal veren dosya işlemleri için geliştirilmiş, lokal-öncelikli bir MCP sunucusudur. Stdio üzerinden JSON-RPC çalışır; hızlı dosya keşfi, minimal içerik çıktısı, range bazlı okuma ve hash korumalı patch uygulamaya odaklanır.

Sunucu tamamen offline kullanım için tasarlanmıştır. Ağ çağrısı, telemetri ve kullanım takibi yoktur.

### Ozellikler (MVP)
- `fs.capabilities`: Çalışma ortamı, varsayılanlar ve araç erişilebilirliği.
- `fs.root_detect`: Deterministik workspace root tespiti.
- `fs.scan`: Ignore kurallarını dikkate alan dosya/klasör listeleme.
- `fs.search`: `rg` varsa hızlı arama, yoksa fallback; kısa snippet ve limitli sonuç.
- `fs.open`: Byte limiti ile range bazlı dosya okuma.
- `fs.patch`: Atomik ve strict pre-hash korumalı metin düzenleme.

### Non-goal'ler
- Uzak servis çağrısı, güncelleme kontrolü, telemetri veya analitik yok.
- Tüm repo içeriğini döken API yok.
- MVP'de silme/yeniden adlandırma/recursive destructive işlem yok.
- Bu repoda IDE/editor entegrasyonu yok.
- MVP'de ağır indeks/veritabanı altyapısı yok.

### Hizli Baslangic
#### 1) Build
```bash
dotnet restore
dotnet build -c Release
```

#### 2) Kaynaktan calistir
```bash
dotnet run --project src/McpFs/McpFs.csproj -c Release
```

#### 3) Self-contained single-file publish
Linux:
```bash
dotnet publish src/McpFs/McpFs.csproj -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -o ./artifacts/linux-x64
```

Windows:
```bash
dotnet publish src/McpFs/McpFs.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./artifacts/win-x64
```

macOS:
```bash
dotnet publish src/McpFs/McpFs.csproj -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -o ./artifacts/osx-arm64
```

#### 4) 5 dakikada dogrulama
- Sunucuyu başlat.
- `initialize`, sonra `tools/list`, ardından `fs.capabilities` için `tools/call` gönder.
- JSON-RPC cevabı `stdout` üzerinde, loglar yalnızca `stderr` üzerinde görünmelidir.

### NativeAOT (opsiyonel)
NativeAOT opsiyoneldir ve `v0.1.0` için varsayılan release çıktısı değildir.

```bash
dotnet publish src/McpFs/McpFs.csproj -c Release -r linux-x64 -p:PublishAot=true
```

### MCP host'a baglama
`mcp-fs` bir stdio MCP server'dır. MCP uyumlu herhangi bir host, süreci subprocess olarak başlatıp stdio üzerinden `Content-Length` framed JSON-RPC konuşabilir.

Host özel örnekler için: [docs/usage.md](docs/usage.md)

### Tool referansi
Tool şemaları, JSON-RPC formatı ve örnekler: [docs/protocol.md](docs/protocol.md)

### Konfigurasyon
Config örnekleri ve çalışma seçenekleri: [docs/usage.md](docs/usage.md)

### Guvenlik modeli
- Workspace sandbox root zorunludur; tüm path'ler buna göre resolve edilir.
- Traversal ve root dışına çıkış engellenir.
- Absolute path girişi reddedilir.
- Varsayılan olarak symlink takibi kapalıdır (`followSymlinks=false`).
- `fs.patch` için `preHash` zorunludur ve yazma atomiktir (`temp + replace`).
- MVP'de destructive dosya API'si yoktur.

### Performans notlari
Çıktıların minimal olmasının nedeni:
- Agent context pahalıdır ve büyük payload'larda gürültü artar.
- `fs.search` tam dosya yerine kısa snippet döndürür (varsayılan `220` byte).
- `fs.open` range + byte limiti ile çalışır (varsayılan `64KB`).
- `scan/search`, `limit` veya `maxResults` dolunca erken durur.

Ayrıntılar: [docs/performance.md](docs/performance.md)

### Kullanim senaryolari
#### Agent dosya bulsun
1. Workspace root için `fs.root_detect` çağır.
2. Ignore-aware listeleme için `fs.scan` kullan (`limit`, `maxDepth`).
3. Kısa snippet eşleşmeleri için `fs.search` kullan.

#### Agent arama yapip kucuk patch uygulasin
1. Hedef satırları `fs.search` ile bul.
2. Küçük bir aralığı `fs.open` ile oku.
3. `preHash` ve minimal `edits` ile `fs.patch` çağır.

#### Hash mismatch durumunda ilerleme
1. `HASH_MISMATCH` geldiyse dosya değişmiştir.
2. Güncel context'i `fs.open` ile tekrar oku.
3. Patch'i güncel `contextHash` ile yeniden üret.
4. `fs.patch` çağrısını tekrar dene.

### Yol haritasi
- File watcher + incremental in-memory index.
- Anchor stratejili deterministik `best_effort` patch modu.
- NativeAOT publish profili (dokümante, opsiyonel hedef).

### Katki
Bkz. [CONTRIBUTING.md](CONTRIBUTING.md)

### Lisans
MIT. Bkz. [LICENSE](LICENSE)
