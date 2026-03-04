# mcp-fs Copilot Instructions

## Repository non-negotiables
- `stdout` must emit JSON-RPC/MCP frames only. Never write logs or debug text to `stdout`.
- Tool responses must use the envelope contract: success `{ ok:true, data:{...} }` and failure `{ ok:false, errorCode, message, data? }`.
- Hard caps are mandatory; do not bypass open/search/patch/scan caps.
- `fs.patch` must require strict `preHash`. Hash mismatch returns `HASH_MISMATCH` and must not write.
- PathGuard is mandatory: reject absolute/UNC/drive-letter paths and block root escape (`OUTSIDE_ROOT`).
- Keep repository free from unresolved debt markers.
- Quality gates are mandatory before merge: format, tests, debt scan.

## Code style and performance
- Keep `System.Text.Json` source-generation based serialization patterns intact.
- Minimize allocations in hot paths.
- Prefer streaming IO for large file processing.
- Keep output bounded and context-minimal.

## PR discipline
- If behavior, schema, limits, or error handling changes, update `docs/protocol.md`.
- If usage changes, update `docs/usage.md` and `README.md`.
- Keep GitHub workflow permissions minimal (least privilege).
- Include verification evidence in PR description.
