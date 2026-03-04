# Troubleshooting

## INVALID_PATH
- Send workspace-relative paths only.
- Absolute paths, UNC paths, and drive-letter paths are rejected.
- Remove null bytes from path payloads.

## OUTSIDE_ROOT
- Path normalization resolved outside workspace root.
- Check `..` traversal and symlink targets.

## PERMISSION_DENIED
- File system ACL issue, or
- symlink access blocked when `followSymlinks=false`.

## HASH_MISMATCH (patch)
1. Read fresh context (`fs.open`)
2. Recompute patch with new `contextHash`
3. Retry `fs.patch`

## TOO_LARGE
- Reduce `fs.open` range/bytes.
- Reduce `fs.search` scope (`root`, `glob`, `maxResults`).
- Keep edits smaller for `fs.patch`.
- Tune config within hard caps.

## Search engine fallback
- If `rg` is unavailable or fails, `fs.search` uses fallback automatically.
- Verify with `fs.capabilities.data.toolAvailability.ripgrep`.
