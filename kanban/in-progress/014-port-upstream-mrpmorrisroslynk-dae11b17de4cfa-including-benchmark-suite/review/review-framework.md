# Review framework

## Budget (by-diff)

- Lines changed: 8162
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Scope (task 014)

**Date:** 2026-11-09
**Host task:** kanban/in-progress/014-port-upstream-mrpmorrisroslynk-dae11b17de4cfa-including-benchmark-suite/
**Diff scope:** branch `task/014-port-upstream-mrpmorrisroslynk-dae11b17de4cfa-incl` vs `master`: merge `f80a0f5d` (upstream `dae11b1..8e14794`, conflict resolutions read with `git show --remerge-diff`) and fork follow-up `44aa307b`.
**Plan / brief:** task.md Requirements and `## Goal (implementer restatement)`.
**Effort:** 3 (by-diff budget). **Reviewer roster:** general (reviewer-claude, Opus 5.5, single pass).
**Session IDs:** reviewer session not exposed in headless print mode.

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
