# mcp-fs

Language: [English](#english-en) | [Türkçe](#turkce-tr)

## English (EN)

### What is mcp-fs?
`mcp-fs` is a local-first MCP stdio server for safe and minimal file operations in coding-agent workflows.

### v1.0 P0 tools
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

### Key guarantees
- `stdout` is JSON-RPC only; logs are `stderr` only.
- Workspace sandbox enforcement for every path.
- Absolute/UNC/drive-letter paths rejected.
- Symlink follow disabled by default.
- Strict pre-hash patching (`HASH_MISMATCH` blocks write).
- Atomic writes (`temp + replace`).
- Bounded outputs and hard caps to prevent context bloat.

### Quick start
```bash
dotnet restore
dotnet build -c Release
dotnet run --project src/McpFs/McpFs.csproj -c Release -- --config ./mcp-fs.config.json
```

### Config and MCPHub samples
- Config sample: [samples/mcp-fs.config.json.sample](samples/mcp-fs.config.json.sample)
- MCPHub settings sample (`mcp_settings.json` format): [samples/workers/mcp-fs.worker.json](samples/workers/mcp-fs.worker.json)

### MCPHub (vaur94/mcphub) integration notes
- Prefer absolute paths for `command`, `args`, and `env` values.
- Set `MCP_FS_ROOT` to the workspace directory you want `mcp-fs` to sandbox.
- MCPHub repository: [vaur94/mcphub](https://github.com/vaur94/mcphub)

### Docs
- Protocol (normative): [docs/protocol.md](docs/protocol.md)
- Usage: [docs/usage.md](docs/usage.md)
- Security model: [docs/security.md](docs/security.md)
- Troubleshooting: [docs/troubleshooting.md](docs/troubleshooting.md)
- Install guide (EN/TR): [docs/install.md](docs/install.md)
- GitHub setup checklist: [docs/github-setup.md](docs/github-setup.md)
- Release notes: [CHANGELOG.md](CHANGELOG.md)
- GitHub Releases: [releases](https://github.com/vaur94/mcp-fs/releases)

### Build and test
```bash
dotnet build mcp-fs.sln -c Release
dotnet test mcp-fs.sln -c Release
dotnet format mcp-fs.sln --verify-no-changes
```

### License
MIT ([LICENSE](LICENSE)).

---

## Turkce (TR)

### mcp-fs nedir?
`mcp-fs`, coding-agent akışları için güvenli ve context-minimal dosya işlemleri sunan lokal MCP stdio sunucusudur.

### v1.0 P0 tool seti
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

### Temel garantiler
- `stdout` sadece JSON-RPC; loglar sadece `stderr`.
- Tüm path girişlerinde workspace sandbox kontrolü.
- absolute/UNC/drive-letter path reddi.
- Varsayılan `followSymlinks=false`.
- Strict `preHash` olmadan patch yok.
- Atomik yazım (`temp + replace`).
- Context şişmesini engelleyen hard cap limitleri.

### Hızlı başlangıç
```bash
dotnet restore
dotnet build -c Release
dotnet run --project src/McpFs/McpFs.csproj -c Release -- --config ./mcp-fs.config.json
```

### Örnek dosyalar
- Config: [samples/mcp-fs.config.json.sample](samples/mcp-fs.config.json.sample)
- MCPHub ayar örneği (`mcp_settings.json` formatı): [samples/workers/mcp-fs.worker.json](samples/workers/mcp-fs.worker.json)

### MCPHub (vaur94/mcphub) entegrasyon notları
- `command`, `args` ve `env` değerlerinde mutlak path kullanın.
- `MCP_FS_ROOT` ile `mcp-fs` çalışma alanı sınırını belirleyin.
- MCPHub deposu: [vaur94/mcphub](https://github.com/vaur94/mcphub)

### Dokümanlar
- Protokol (normatif): [docs/protocol.md](docs/protocol.md)
- Kullanım: [docs/usage.md](docs/usage.md)
- Güvenlik modeli: [docs/security.md](docs/security.md)
- Kurulum rehberi (EN/TR): [docs/install.md](docs/install.md)
- GitHub kurulum checklist: [docs/github-setup.md](docs/github-setup.md)
- Sürüm notları: [CHANGELOG.md](CHANGELOG.md)
- GitHub Release sayfası: [releases](https://github.com/vaur94/mcp-fs/releases)

### Lisans
MIT ([LICENSE](LICENSE)).
