# Performance Notes

`mcp-fs` is optimized for bounded outputs and predictable latency.

## Default limits
- `fs.search.maxResults = 100`
- `fs.search.snippetBytes = 220`
- `fs.search.maxFilesScanned = 5000`
- `fs.search.maxFileSizeBytes = 2097152`
- `fs.search.timeoutMs = 5000`
- `fs.open.maxBytes = 65536`
- `fs.open.maxLines = 200`
- `fs.scan.limit = 500`
- `fs.scan.maxDepth = 16`

## Hard caps
Hard caps are enforced even if config is higher:
- `search.maxResults <= 500`
- `search.snippetBytes <= 2000`
- `search.timeoutMs <= 15000`
- `open.maxBytes <= 131072`
- `open.maxLines <= 1000`
- `scan.limit <= 5000`
- `scan.maxDepth <= 64`

## Search engine behavior
- Preferred engine: `rg` (`ripgrep`).
- Fallback engine: streaming line-by-line search.
- Binary heuristic: null-byte detection for fallback skip.
- Both engines support truncation via caps and timeouts.

## Context minimal principle
- `fs.search` returns snippets, ranges, and hashes.
- `fs.open` returns only requested line range, bounded by caps.
- `fs.patchPreview` gives summary instead of full diff dump.
