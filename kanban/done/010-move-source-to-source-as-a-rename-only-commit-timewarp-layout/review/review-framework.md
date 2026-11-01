# Review framework — task 010

**Date:** 2026-10-24
**Host task:** kanban/to-do/010-move-source-to-source-as-a-rename-only-commit-timewarp-layout/
**Diff scope:** branch `task/010-move-source-to-source-as-a-rename-only-commit-time` vs `master` (product commits `b5aae02`, `dee2da8`, `34a0370`)
**Plan / brief:** `task.md` original Description / Requirements and `## Goal (implementer restatement)`. Rename top-level `Source/` to `source/` as a pure rename, repoint directory references outside `kanban/`, drop audit waivers that now pass, and do not start task 008.
**Effort:** 2 (general only; by-diff budget)
**Reviewer roster:** general
**Session IDs:** Grok `01a16d43-286e-7231-b464-e3038a89a0c8` (2026-10-24)

## Budget (by-diff)

- Lines changed: 277
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
