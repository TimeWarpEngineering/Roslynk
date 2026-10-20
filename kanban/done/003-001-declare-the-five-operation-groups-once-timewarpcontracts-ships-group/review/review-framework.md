# Review framework — task 003-001

**Date:** 2026-10-20
**Host task:** kanban/to-do/003-001-declare-the-five-operation-groups-once-timewarpcontracts-ships-group/
**Diff scope:** branch `task/003-001-declare-the-five-operation-groups-once-timewarpcon` vs `master` (pins, 29 `[Operation]` groups, `CapabilitiesContractTests`, README, AGENTS.md, releases.md, `VerbatimTerminal` comment)
**Plan / brief:** Declare each of the 29 operations' purpose group once on `[Operation]` through `OperationAttribute.Group` (Contracts 2.0.0-beta.23). Nuru 4.0.0-beta.3 exposes that as `groupPath`. No Nuru attribute and no app-side map.
**Effort:** 2
**Reviewer roster:** general
**Session IDs:** grok 01a15749-6275-7da2-8dbe-a49ecf133a60

## Budget (by-diff)

- Lines changed: 275
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
