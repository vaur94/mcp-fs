# Troubleshooting

## Windows path issues
- Use workspace-relative paths in tool requests.
- Do not send drive-qualified absolute paths (`C:\...`).
- If you get `INVALID_PATH`, convert to relative path under workspace root.
- If you get `OUTSIDE_ROOT`, the path resolves outside detected root.

## Permission denied
If a tool returns `PERMISSION_DENIED`:
- Verify file/folder ACLs.
- Ensure the process user has read/write rights.
- On Linux/macOS, check `chmod` and ownership.

## ripgrep not found
`fs.search` automatically falls back to internal streaming search when `rg` is unavailable.

Check availability with `fs.capabilities`:
```json
{"toolAvailability":{"ripgrep":false}}
```

Install ripgrep for best search performance:
- macOS: `brew install ripgrep`
- Ubuntu/Debian: `sudo apt-get install ripgrep`
- Windows: `winget install BurntSushi.ripgrep.MSVC`

## HASH_MISMATCH on patch
`fs.patch` strict mode requires exact `preHash`.

Recommended flow:
1. Re-read target range with `fs.open`.
2. Regenerate patch against current content.
3. Retry `fs.patch` with current `contextHash`.

## TOO_LARGE from fs.open
If content exceeds `maxBytes`:
- Reduce line range (`startLine`, `endLine`).
- Increase `maxBytes` (within your host policy).
- Prefer multiple small `fs.open` calls over one large call.

## Performance tuning
- Add `excludeGlob` filters to search/scan large repos.
- Keep `maxResults` low and focused.
- Exclude generated folders aggressively.

See [performance.md](performance.md) for tuning details.
