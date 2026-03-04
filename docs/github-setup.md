# GitHub Setup Checklist (Manual UI Steps)

## 1) Branch protection / ruleset
Path: `Settings -> Branches` (or `Settings -> Rulesets`)

Enable for `main`:
- Require a pull request before merging
- Require approvals:
  - Autonomous bot flow: `0`
  - Human review flow (optional stricter mode): `1+`
- Require status checks to pass before merging
- Required checks: CI workflow checks for all matrix platforms
- Optionally dismiss stale approvals when new commits are pushed
- Apply rules to administrators (disable admin bypass)
- Block force pushes
- Block branch deletion

## 2) Actions permissions
Path: `Settings -> Actions -> General`

Set:
- Workflow permissions: **Read repository contents permission** by default
- Allow GitHub Actions to create and approve pull requests: enable if needed for release-please maintenance flow

## 3) Dependabot
Path: `Security -> Dependabot`

Enable:
- Dependabot alerts
- Dependabot security updates

Note:
- Dependabot version updates are configured via `.github/dependabot.yml`.
- Dependabot PRs must pass CI before merge.
- Create label `dependencies` in `Issues -> Labels` so Dependabot PR labels resolve consistently.

## 4) Copilot
Path: repository/org Copilot settings

Enable:
- Copilot code review for pull requests (if available on plan)
- Repository custom instructions via `.github/copilot-instructions.md`

## 5) Required checks naming guidance
After first CI run, copy exact check names from PR UI into ruleset required checks.
Prefer matrix checks that represent all target platforms.

## 6) Recommended merge policy
- Squash merge enabled
- Auto-merge enabled at repository level
- Bot PR auto-merge is handled by `.github/workflows/auto-merge-bots.yml`
- No direct pushes to `main`

## 7) Release automation notes
- `Release Please` workflow runs on `main` pushes and opens/updates release PRs.
- Merging a release PR creates a release/tag and updates `CHANGELOG.md`.
- `.github/workflows/release.yml` publishes binaries and `SHA256SUMS` on:
  - tag push (`v*`)
  - release published event
  - manual dispatch (`workflow_dispatch`) with `tag` input
