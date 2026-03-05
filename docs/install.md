# Install Guide (EN + TR)

## English (EN)

### Quick install (latest release)
```bash
curl -fsSL https://raw.githubusercontent.com/vaur94/mcp-fs/main/install.sh | bash
```

### Install from cloned repo
```bash
./install.sh
```

### Install specific version
```bash
./install.sh --version 1.0.0
```

### Custom install prefix
```bash
./install.sh --prefix /usr/local/bin
```

### Uninstall
```bash
./uninstall.sh
```

### Windows install (PowerShell)
```powershell
./install.ps1
```

### Security model
- Installer always downloads release archive plus `SHA256SUMS`.
- Installation is aborted if SHA256 verification fails.
- Default install path is user-local (`~/.local/bin`) and does not require root.

### Local test mode for installer
Use local release assets instead of downloading from GitHub:
```bash
./install.sh --local-release-dir ./artifacts/release-local --prefix ./tmp-bin
```

### MCPHub wiring (`vaur94/mcphub`)
MCPHub repository: https://github.com/vaur94/mcphub

Use [samples/workers/mcp-fs.worker.json](../samples/workers/mcp-fs.worker.json) as `mcp_settings.json`.

Startup matrix:

| Mode | `command` | `args` |
| --- | --- | --- |
| Installed binary | `/home/you/.local/bin/mcp-fs` | `["--config","/home/you/Projects/mcp-fs/samples/mcp-fs.config.json.sample"]` |
| Source (`dotnet run`) | `dotnet` | `["run","--project","/home/you/Projects/mcp-fs/src/McpFs/McpFs.csproj","-c","Release","--","--config","/home/you/Projects/mcp-fs/samples/mcp-fs.config.json.sample"]` |

Recommended `mcp_settings.json` block:
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

Notes:
- Use absolute paths for `command`, `args`, and `MCP_FS_ROOT`.
- `MCP_FS_ROOT` sets the workspace sandbox boundary for `mcp-fs`.

## Türkçe (TR)

### Hızlı kurulum (son release)
```bash
curl -fsSL https://raw.githubusercontent.com/vaur94/mcp-fs/main/install.sh | bash
```

### Clone sonrası kurulum
```bash
./install.sh
```

### Belirli sürüm kurulumu
```bash
./install.sh --version 1.0.0
```

### Özel kurulum dizini
```bash
./install.sh --prefix /usr/local/bin
```

### Kaldırma
```bash
./uninstall.sh
```

### Windows (PowerShell)
```powershell
./install.ps1
```

### Güvenlik modeli
- Installer her zaman release arşivi + `SHA256SUMS` indirir.
- SHA256 doğrulaması başarısız olursa kurulum iptal edilir.
- Varsayılan kurulum yolu kullanıcı dizinidir (`~/.local/bin`), root gerektirmez.

### Installer local test modu
GitHub yerine local release asset kullanımı:
```bash
./install.sh --local-release-dir ./artifacts/release-local --prefix ./tmp-bin
```

### MCPHub bağlantısı (`vaur94/mcphub`)
MCPHub deposu: https://github.com/vaur94/mcphub

[samples/workers/mcp-fs.worker.json](../samples/workers/mcp-fs.worker.json) dosyasını `mcp_settings.json` örneği olarak kullanın.

Başlatma matrisi:

| Mod | `command` | `args` |
| --- | --- | --- |
| Kurulu binary | `/home/you/.local/bin/mcp-fs` | `["--config","/home/you/Projects/mcp-fs/samples/mcp-fs.config.json.sample"]` |
| Kaynaktan (`dotnet run`) | `dotnet` | `["run","--project","/home/you/Projects/mcp-fs/src/McpFs/McpFs.csproj","-c","Release","--","--config","/home/you/Projects/mcp-fs/samples/mcp-fs.config.json.sample"]` |

Önerilen `mcp_settings.json` bloğu:
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

Notlar:
- `command`, `args` ve `MCP_FS_ROOT` için mutlak path kullanın.
- `MCP_FS_ROOT`, `mcp-fs` için workspace sandbox sınırını belirler.
