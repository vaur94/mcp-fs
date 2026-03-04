# Example Workspace

This folder is a minimal workspace for manual `mcp-fs` smoke tests.

Suggested checks:
1. Run `fs.scan` and confirm `node_modules` is ignored.
2. Run `fs.search` for `marker` and inspect snippet output.
3. Run `fs.open` on `src/app.txt` with a small range.
4. Run `fs.patchPreview` first, then `fs.patch` with strict `preHash`.
