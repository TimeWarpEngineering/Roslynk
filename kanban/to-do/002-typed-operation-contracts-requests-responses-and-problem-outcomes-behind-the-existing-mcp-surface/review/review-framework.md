# Review framework

## Budget (by-diff)

- Lines changed: 3227
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

# Review framework — task 002

**Date:** 2026-10-18
**Host task:** kanban/to-do/002-typed-operation-contracts-requests-responses-and-problem-outcomes-behind-the-existing-mcp-surface/
**Diff scope:** branch `task/002-typed-operation-contracts-requests-responses-and-p` vs `master` (34a766d; 74 files, +3110/−117)
**Plan / brief:** typed operation (Query/Command + Response + `OneOf<Response, SharedProblemDetails>`) behind all 29 MCP tools; renderers keep today's bytes; multi_query binds into request classes; TimeWarp.Contracts 2.0.0-beta.21
**Effort:** 3 (by-diff budget), roster axis general
**Reviewer roster:** general (review oracle, claude-opus-5-5)
**Session IDs:** review oracle under `ganda task work` (headless print mode; session id not exposed)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
