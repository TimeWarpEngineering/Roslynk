# Review framework — task 012

**Date:** 2026-11-01
**Host task:** kanban/in-progress/012-find-why-18-razor-tests-fail-locally-but-pass-in-ci-and-upstream/
**Diff scope:** product commit `0ebd960` on `task/012-find-why-18-razor-tests-fail-locally-but-pass-in-c` (merge-base `42666a3`). 18 product files, 453 insertions, 60 deletions. Kanban move/land commits are out of scope.
**Plan / brief:** Find why 18 Razor tests fail on this machine while CI and upstream pass, and fix the right layer. The implementer treats it as two layers: the engine did not recognize the .NET 11 `X_razor.decl.g.cs` document, and the fixture BuildHost was evaluating with the newest SDK.
**Effort:** 2 (general only)
**Reviewer roster:** general
**Session IDs:** grok 01a1967d-1c9f-7552-bf56-d4f8c809a086

## Budget (by-diff)

- Lines changed: 750
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
