# Review framework — task 007

**Date:** 2026-10-23
**Host task:** kanban/to-do/007-publish-the-fork-as-timewarproslynk-to-private-github-packages-only/
**Diff scope:** branch `task/007-publish-the-fork-as-timewarproslynk-to-private-git` vs `master` (`c28b35d`, 213 lines). Product paths: `.github/workflows/workflow.yml`, `Source/App/Morris.Roslynk.Cli/Morris.Roslynk.Cli.csproj`, `README.md`, `AGENTS.md`, `releases.md`. Kitchen `task.md` is uncommitted and out of product scope.
**Plan / brief:** Fork issue #11. Package id `TimeWarp.Roslynk` (command stays `roslynk`). Tag workflow packs and pushes only to `https://nuget.pkg.github.com/TimeWarpEngineering/index.json` with `GITHUB_TOKEN`. No nuget.org login or push. Docs and `releases.md` updated. Tagging stays a human gate.
**Effort:** 2 (by-diff, 213 lines). Roster axis is general only; the budget note names no specialist.
**Reviewer roster:** general
**Session IDs:** review oracle Grok, session `01a168bc-b74d-7882-bab3-af456b9da72d` (2026-10-23)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Budget (by-diff)

- Lines changed: 213
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120
