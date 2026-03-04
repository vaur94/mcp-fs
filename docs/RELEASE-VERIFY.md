# Release Verification

## Local publish
```bash
dotnet publish src/McpFs/McpFs.csproj -c Release -r linux-x64 -p:PublishSingleFile=true -p:SelfContained=true -p:PublishTrimmed=false -o artifacts/linux-x64
```

Output binary:
- `artifacts/linux-x64/McpFs`

## Archive and checksums
Created release-local assets:
- `mcp-fs-linux-x64.tar.gz`
- `mcp-fs.config.json.sample`
- `mcp-fs.worker.json`
- `SHA256SUMS`

`sha256sum -c SHA256SUMS` result:
```text
mcp-fs-linux-x64.tar.gz: OK
mcp-fs.config.json.sample: OK
mcp-fs.worker.json: OK
```

SHA256SUMS:
```text
fc23a975bdea65e77b2fb2e10eb5b2026dbf306ca3a00e63b6611ec407b9d71d  mcp-fs-linux-x64.tar.gz
19da0f475ba8614383d34c25bae0194f4ec317775541251c391675d31c754801  mcp-fs.config.json.sample
af773f44a680fdebb40f96957ae467f466ed965d3a4cd97d2d8c494a3600f5bc  mcp-fs.worker.json
```

## Extracted binary run check
- archive extracted under temporary directory
- binary executed with `--root` and `--config`
- `initialize` request returned valid framed JSON-RPC response

## Stdio smoke timing snapshot
- initialize: 102.88 ms
- tools/list: 7.41 ms
- security matrix pass: true
