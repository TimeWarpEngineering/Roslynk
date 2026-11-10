# Review framework

## Budget (by-diff)

- Lines changed: 683
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Framework — task 015

**Date:** 2026-11-11
**Host task:** kanban/in-progress/015-fix-new-ganda-repo-audit-failures-nuru-beta12-pwsh-shell-suppression-baseline/
**Diff scope:** branch `task/015-fix-new-ganda-repo-audit-failures-nuru-beta12-pwsh` vs `master` (merge base), commit `eebe2cf2` (build!: target net11 because Nuru 4.0.0-beta.12 is net11-only) plus kanban landing commits
**Plan / brief:** make `ganda repo audit` exit 0 after the ganda update: commit `--fix` output (suppression baseline, githooks, pwsh default shell, tools pins), Nuru family to 4.0.0-beta.12 (net11-only, Mediator 15 handler shape), coverage upload, vscode window icon
**Effort:** 2 (general only)
**Reviewer roster:** general
**Session IDs:** reviewer-claude (claude-opus-5-5), headless review oracle, 2026-11-11

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
