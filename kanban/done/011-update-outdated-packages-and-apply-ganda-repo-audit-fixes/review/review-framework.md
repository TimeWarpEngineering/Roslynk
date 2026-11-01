# Review framework — task 011

**Date:** 2026-10-30
**Host task:** kanban/in-progress/011-update-outdated-packages-and-apply-ganda-repo-audit-fixes/
**Diff scope:** product commits `e0436b1..d81f867` on `task/011-update-outdated-packages-and-apply-ganda-repo-audi` (audit fixes, then Nuru 4.0.0-beta.9 / Contracts 2.0.0-beta.29 and the hint move). 43 files, 189 insertions, 110 deletions.
**Plan / brief:** Update both central package files to the latest versions `ganda nuget outdated` would pick, adapt to Nuru beta.6–beta.9 (titles and MCP hints), run `ganda repo audit --fix`, fill `glossary.md`, and leave the Release build and the post-010 test baseline green.
**Effort:** 2 (general only)
**Reviewer roster:** general
**Session IDs:** grok 01a18da2-8133-7410-8fed-c6b85a05cefd

## Budget (by-diff)

- Lines changed: 564
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
