# Review framework — task 006

**Date:** 2026-10-21
**Host task:** kanban/to-do/006-served-app-follow-ups-from-the-pr-9-review-nuru-508-017-and-508-018-adoption-diagnostics-cancel-in-the-queue-windows-detached-server-stale-port-text/
**Diff scope:** branch `task/006-served-app-follow-ups-from-the-pr-9-review-nuru-50` vs `master` (product commit `7e33df5`; 48 product files, 317 insertions / 233 deletions). Kanban task edits are not product scope.
**Plan / brief:** PR #9 served-app follow-ups. Pin TimeWarp.Nuru 4.0.0-beta.5 and adopt 508-017 (per-build socket), 508-018 (problem renderer for binding errors), 508-013 (routes from the engine assembly), and 508-007 (first-use services). Cancel a queued `get_diagnostics` without waiting on a long write. Record the Windows detached-server check. Fix `Benchmarks.md`. Extend the `(Fixes #8)` bullet. `ganda repo audit --fix` for `workflow-file`.
**Effort:** 3
**Reviewer roster:** general
**Session IDs:** Grok review oracle `01a15ccb-c347-7af3-8e6e-a781428ab9cc` (2026-10-21). Implementer session was not reported.

## Budget (by-diff)

- Lines changed: 19
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Round 2

**Date:** 2026-10-21
**Diff scope:** `4b55243` since round 1 (`dcfd8fe`). Product delta is the AGENTS.md JSON round-trip sentence (M1). The same commit’s `review/round-1/merged.md` and `task.md` edits are kitchen status, not product.
**Effort:** 1 (general), as in Budget (by-diff) above
**Session IDs:** Grok review oracle `01a15cd5-bede-7a62-a2eb-d2ba55f25646` (2026-10-21)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
