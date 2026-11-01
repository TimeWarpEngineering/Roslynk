# Review framework — task 008

**Date:** 2026-11-01
**Host task:** kanban/in-progress/008-adopt-the-dev-cli-release-flow-ci-on-prs-and-master-dev-release-to-private-github-packages/
**Diff scope:** branch `task/008-adopt-the-dev-cli-release-flow-ci-on-prs-and-maste` vs `master` (25 files, +1373/−201). Product scope is the release flow, package pins, audit waivers, dev CLI, and docs. Kanban `task.md` is in the same diff and was read as the spec and the implementer record.
**Plan / brief:** Task 008. Put the fork on the TimeWarp release flow: version typed once in `source/Directory.Build.props`, CI on every PR to master and every master push, `dev release` promotes the CI artifact to GitHub Packages. Package bumps and audit waivers ride along. This task does not cut the release.
**Effort:** 3
**Reviewer roster:** general
**Session IDs:** grok 01a19649-5bb8-7aa2-9f31-c519adb4e273 (review oracle). Implementer: implementer-claude, session not reported.

## Budget (by-diff)

- Lines changed: 1574
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
