# Review framework

## Budget (by-diff)

- Lines changed: 797
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Framework — task 013

**Date:** 2026-11-08
**Host task:** kanban/in-progress/013-fix-ganda-repo-audit-failures/
**Diff scope:** branch `task/013-fix-ganda-repo-audit-failures` at 3266f4f vs merge-base 32b9352 (master): `.editorconfig`, `.vscode/settings.json` (untracked), `AGENTS.md`, `tools/dev-cli/endpoints/workflow-command.cs`, `tools/dev-cli/endpoints/workflow-release-steps.cs`, task.md
**Plan / brief:** task.md `## Goal (implementer restatement)`: make `ganda repo audit` exit 0; commit `--fix`; split the fork-owned workflow command; waive the line cap for upstream `source/` files
**Effort:** 2 (by-diff budget), roster general
**Reviewer roster:** general (review oracle, Claude Opus 5.5; helper subagent Sonnet did the method-by-method split comparison)
**Session IDs:** review oracle (Claude, ganda task work headless)

### Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit. Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
