## Summary
- What changed:
- Why:
- How verified:

## Changes
- 

## Validation
- [ ] `dotnet format mcp-fs.sln --verify-no-changes`
- [ ] `dotnet build mcp-fs.sln -c Release`
- [ ] `dotnet test mcp-fs.sln -c Release`
- [ ] `rg -n "TO[D]O|FI[X]ME|HA[C]K" --glob '!docs/answer.md' .` returns empty
- [ ] Release smoke validated (if applicable)
- [ ] Manual protocol check (if tool behavior changed)

## Security checklist
- [ ] No root-escape regression
- [ ] No stdout logging
- [ ] Output remains bounded/minimal
- [ ] Patch guard unchanged (`preHash` mismatch => no write)

## Documentation
- [ ] Updated README/docs if needed
- [ ] Updated `docs/protocol.md` when tool schema/limits/errors changed
- [ ] Updated CHANGELOG (if release-facing)
