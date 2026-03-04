# Performance Notes

## Why limits exist
`mcp-fs` is optimized for agent workflows where context is scarce and expensive.

Default limits:
- `fs.search.maxResults`: `100`
- `fs.search.snippetBytes`: `220`
- `fs.open.maxBytes`: `65536` (64KB)
- `fs.scan.limit`: `500`

These defaults reduce noisy payloads and keep round-trip latency predictable.

## ripgrep advantage
When available, `rg` is used for `fs.search` because it:
- Scans large trees faster than managed fallback.
- Supports robust regex/literal modes.
- Returns line-oriented match events suitable for snippet extraction.

If `rg` is absent, `mcp-fs` falls back automatically to streaming search.

## Binary file behavior
Fallback search skips likely binary files by probing early bytes for null-byte markers. This avoids expensive scans and prevents unreadable snippets.

## Ignore strategy
Performance and relevance improve with ignore rules:
- Built-in ignores: `.git`, `bin`, `obj`, `node_modules`, `dist`, `.idea`, `.vs`
- `.gitignore` support at workspace root
- Optional include/exclude globs per request

In large monorepos, excluding `node_modules` and generated output is critical.

## Early stop behavior
- `fs.scan`: stops once `limit` is reached and marks `truncated=true`.
- `fs.search`: stops at `maxResults`, marks `truncated=true`, and avoids full output floods.

This is a hard requirement for predictable latency and bounded token usage.
