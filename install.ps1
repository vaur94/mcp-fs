param(
  [string]$Version = "latest",
  [string]$Repo = "vaur94/mcp-fs",
  [string]$InstallDir = "$env:LOCALAPPDATA\\mcp-fs\\bin",
  [string]$LocalReleaseDir = "",
  [switch]$Uninstall
)

$ErrorActionPreference = "Stop"

if ($Uninstall) {
  Write-Host "Windows uninstall is manual for now. Remove: $InstallDir\\mcp-fs.exe"
  exit 0
}

$assetName = "mcp-fs-win-x64.zip"
$checksumName = "SHA256SUMS"

$tmpDir = New-Item -ItemType Directory -Path ([System.IO.Path]::GetTempPath()) -Name ("mcpfs-install-" + [System.Guid]::NewGuid().ToString("N"))
try {
  $assetPath = Join-Path $tmpDir.FullName $assetName
  $checksumPath = Join-Path $tmpDir.FullName $checksumName

  if ($LocalReleaseDir) {
    Copy-Item (Join-Path $LocalReleaseDir $assetName) $assetPath
    Copy-Item (Join-Path $LocalReleaseDir $checksumName) $checksumPath
  }
  else {
    if ($Version -eq "latest") {
      $baseUrl = "https://github.com/$Repo/releases/latest/download"
    }
    else {
      if (-not $Version.StartsWith("v")) {
        $Version = "v$Version"
      }
      $baseUrl = "https://github.com/$Repo/releases/download/$Version"
    }

    Write-Host "Downloading $assetName and $checksumName from $Repo ($Version)..."
    Invoke-WebRequest -Uri "$baseUrl/$assetName" -OutFile $assetPath
    Invoke-WebRequest -Uri "$baseUrl/$checksumName" -OutFile $checksumPath
  }

  $expectedLine = Get-Content $checksumPath | Where-Object { $_ -match "\s+$assetName$" } | Select-Object -First 1
  if (-not $expectedLine) {
    throw "Could not find checksum entry for $assetName"
  }

  $expectedHash = ($expectedLine -split "\s+")[0].ToLowerInvariant()
  $actualHash = (Get-FileHash -Algorithm SHA256 -Path $assetPath).Hash.ToLowerInvariant()
  if ($expectedHash -ne $actualHash) {
    throw "Checksum verification failed for $assetName"
  }

  Write-Host "Checksum verified."

  $extractDir = Join-Path $tmpDir.FullName "extract"
  Expand-Archive -Path $assetPath -DestinationPath $extractDir

  $binary = Get-ChildItem -Path $extractDir -Recurse -File -Filter "McpFs.exe" | Select-Object -First 1
  if (-not $binary) {
    throw "Could not locate McpFs.exe in archive"
  }

  New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
  $target = Join-Path $InstallDir "mcp-fs.exe"
  Copy-Item $binary.FullName $target -Force

  Write-Host "Installed: $target"
  Write-Host "Add to PATH if needed."
}
finally {
  Remove-Item -Recurse -Force $tmpDir.FullName
}
