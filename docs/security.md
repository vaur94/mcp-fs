# Security Model

## Threat model
`mcp-fs` is a local stdio worker. Primary risks:
- path traversal and root escape
- symlink/junction escape
- stale context writes
- partial file writes on interruption
- context overexposure in tool outputs

## Sandbox guarantees
- All tool paths are workspace-relative.
- Absolute/UNC/drive-letter inputs are rejected.
- Normalized path must remain under workspace root.
- `followSymlinks=false` by default.
- If symlink following is enabled, resolved target must still stay inside root.

## Write integrity (`fs.patch`)
- Writes only through `fs.patch`.
- Strict `preHash` required.
- Hash mismatch returns `HASH_MISMATCH` and writes nothing.
- EOL style is preserved (`\n` vs `\r\n`).
- UTF-8 BOM is preserved if present.
- Atomic write strategy:
  - same-directory temp file
  - flush to disk
  - replace target (`File.Replace` on Windows, `File.Move(overwrite:true)` elsewhere)

## Context minimization
- `fs.search` returns snippets, never full-file dumps.
- `fs.open` uses line ranges plus byte/line caps.
- traversal tools enforce limit/depth caps and `truncated` indicators.

## Logging
- JSON-RPC messages only on `stdout`.
- Operational logs only on `stderr`.

## Known boundaries
- Root `.gitignore` subset is best-effort, not full git parity.
- No delete/rename/destructive bulk file APIs.
- Host-level rate limiting/timeouts remain authoritative; worker caps are defense-in-depth.
