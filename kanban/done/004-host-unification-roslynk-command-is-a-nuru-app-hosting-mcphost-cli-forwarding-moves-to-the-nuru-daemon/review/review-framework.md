# Review framework — task 004

**Date:** 2026-10-19
**Host task:** kanban/to-do/004-host-unification-roslynk-command-is-a-nuru-app-hosting-mcphost-cli-forwarding-moves-to-the-nuru-daemon/
**Diff scope:** branch `task/004-host-unification-roslynk-command-is-a-nuru-app-hos` vs `master` (873 lines, 25 files). Product commit `04c2f34`.
**Plan / brief:** `task.md` Goal (implementer restatement) and the original Description. One `roslynk` Nuru app (29 operation routes, `stdio`, daemon) shipped as the `Roslynk` tool. McpHost and the Nuru daemon are adopted only when the MCP text and schema stay byte-identical.
**Effort:** 3 (by-diff). Roster axes: general only.
**Reviewer roster:** general
**Session IDs:** grok `01a15503-fb91-7b80-baab-6a4fe3cfdabc` (round 1)

## Budget (by-diff)

- Lines changed: 873
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
