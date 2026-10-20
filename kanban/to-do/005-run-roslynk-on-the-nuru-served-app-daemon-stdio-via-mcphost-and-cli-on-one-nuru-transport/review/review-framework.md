# Review framework — task 005

**Date:** 2026-10-20
**Host task:** kanban/to-do/005-run-roslynk-on-the-nuru-served-app-daemon-stdio-via-mcphost-and-cli-on-one-nuru-transport/
**Diff scope:** branch `task/005-run-roslynk-on-the-nuru-served-app-daemon-stdio-vi` vs `master` (feature commit `449ff1e`; 116 files, +2439/−3915)
**Plan / brief:** Run Roslynk on the Nuru served app so CLI, MCP stdio, and the daemon share one warm engine. Checked against the original task and `## Goal (implementer restatement)`.
**Effort:** 3
**Reviewer roster:** general
**Session IDs:** grok 01a159cb-9046-7b22-8142-6a8616635df2

## Budget (by-diff)

- Lines changed: 6354
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
