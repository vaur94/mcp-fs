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

### mcp-hub worker wiring
Use [samples/workers/mcp-fs.worker.json](../samples/workers/mcp-fs.worker.json) and set command path to the installed binary:
```json
{
  "command": "/home/you/.local/bin/mcp-fs"
}
```

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

### mcp-hub worker bağlantısı
[samples/workers/mcp-fs.worker.json](../samples/workers/mcp-fs.worker.json) kullanın ve `command` yolunu kurulan binary’ye yöneltin:
```json
{
  "command": "/home/you/.local/bin/mcp-fs"
}
```
