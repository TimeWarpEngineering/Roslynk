# Port upstream mrpmorris/Roslynk dae11b1..7de4cfa including benchmark suite

## Description

Fork issue #24. Steven (2026-11-09) asked how to compare the fork to Pete's and whether upstream has
benchmarks; answer "yes" to porting the 16 new upstream commits including the benchmark suite.

Task 009 merged upstream through **`dae11b1`** (merge commit 2eaee8f, PR #14). Since then upstream master has
16 commits, pinned at **`7de4cfa`** ("Place Razor-declared symbols under the .razor file…", #167). That is the
pin; do not take anything newer.

```
7de4cfa Place Razor-declared symbols under the .razor file, not the generated .g.cs path (#164/#167)
0470ab2 get_solution_status reports NuGet restore warnings as loadWarning (#163/#169)
b633239 Guard read-only queries against a superseded snapshot (#165/#168)
01b8d20 Update Aspire version
5a6aec8 Record OTEL span and metric tag values in full (#172/#173)
bd87e36 Cache compiler arguments captured during the MSBuild load (#170/#171)
456684a Add reproducible AI benchmark suite for Roslynk (#174/#175)
9fcf739 Resolve positional record properties by name (#176/#180)
a05fc09 Return whole declaration for a bodiless type in get_symbol (#179/#181)
c957312 Reduce test times (#160/#184)
fc9a0b1 Output fewer OTEL traces (#182/#185)
4216b1b Fix stale code-action diagnostics after a method-body edit (#187/#194)
9020fc4 apply_patch applies several sections for the same file in order (#186/#197)
84a165b Keep unrestored backups when a batch-write rollback fails (#188/#196)
8910f92 get_diagnostics and change_signature honour the caller's cancellation token (#189/#198)
8e14794 Make the code-action analyzer budget configurable and raise it for tests (#190/#195)
```

Follow task 009's approach (`kanban/done/009-…/task.md`); its rules still apply. Differences now: the fork
renamed `Source/` → `source/` (pure rename, task after 009), so rely on git rename detection; and the
repo-audit waivers from task 013 are in place.

## Requirements

- **Real merge, not cherry-pick or squash.** `git remote add upstream https://github.com/mrpmorris/Roslynk.git`
  in the worktree (fetch only; never push, never open PRs/issues there). `git merge 7de4cfa` → one merge commit,
  follow-up fixes as separate commits. Afterwards `git merge-base HEAD 7de4cfa` must be `7de4cfa`.
  Upstream files landing under `Source/` must end up under `source/`.
- **Engine changes port as-is**, then reach the fork's surfaces: any changed tool parameter/description also
  changes its `[Operation]` request (same name, `[Description]`, `[DefaultValue]`, initializer; method
  description word-for-word). `OperationContractTests`, `CapabilitiesContractTests`, `McpSurfaceTests`,
  `HandlerTests.SampleOf` must agree. New error codes get exit codes in `Output/ExitCodes` + AGENTS.md/README
  tables. Server-instruction changes go into `McpServerRegistration.Instructions`.
- **New settings** (#190 analyzer budget, #170 compiler-args cache, any OTEL knobs): wire through
  `Hosting/RoslynkSettings` / `RoslynkEngine` the way task 009 did #142, and the shipped
  `appsettings.json.example` if applicable.
- **Upstream host changes** in the deleted `Morris.Roslynk.Mcp` (e.g. `Hosting/DaemonEndpoints.cs`,
  `DaemonHandshake.cs` from #174, Aspire bump 01b8d20, OTEL #172/#182): adapt to the Nuru app or drop with a
  reason. Do not restore the project.
- **Benchmark suite (#174, `Benchmarks/` at repo root):** bring it in. Adapt `Benchmarks/run.md` and anything
  that talks to Pete's HTTP daemon so it drives `roslynk stdio` (MCP over stdio, per-build Unix socket server).
  Keep its `TestSolution` runnable from a reset (`scripts/reset.sh`). Make sure `ganda repo audit` still
  passes (the TestSolution may trip checks; waive under `[ganda.audit]` with reasons rather than restyle
  upstream files). Decide whether `source/TestFixtures/Benchmarks/Benchmarks.md` (old private-solution
  instructions) stays, and say why in Results. Note in Results how to run the suite against upstream's build
  vs. ours for a side-by-side comparison. Do not run a full AI benchmark as part of this task.
- **Nuru gaps:** do not patch around Nuru inside Roslynk; list each gap in Results with the needed
  timewarp-nuru change.
- **Keep the fork's decisions:** package `TimeWarp.Roslynk`, command `roslynk`, GitHub Packages only (no
  nuget.org, no mrpmorris push), Nuru-first hosting, `[ganda.audit]` waivers, fork README install section.
- **releases.md:** keep upstream's release-history entries for the range with `mrpmorris/Roslynk#NNN`; add one
  fork bullet under `# Unreleased`: "Ported upstream mrpmorris/Roslynk through 7de4cfa, including its benchmark
  suite (Fixes #24)".
- Update AGENTS.md where upstream changed its guidance (merge carefully; the fork's AGENTS.md wins on hosting).

## Checklist

- [ ] Upstream remote added; `git merge 7de4cfa` merge commit; merge-base verified
- [ ] Conflicts resolved; `Source/` paths land under `source/`
- [ ] Engine changes reach `[Operation]` contracts, exit codes, server instructions, settings
- [ ] Host changes adapted or dropped with reasons
- [ ] `Benchmarks/` ported and driving `roslynk stdio`; comparison how-to in Results
- [ ] releases.md and AGENTS.md updated
- [ ] `bin/dev build` and `bin/dev test` pass (both test projects)
- [ ] `ganda repo audit` exits 0
- [ ] Nuru gaps listed in Results

## Session

- Created: 93b68595-4648-49ee-a58b-6a1ec1f5dab9 (2026-11-09)

## Notes

- Prior port: task 009 / PR #14 (through dae11b1).
- `dev check-version` takes ~2 min silently (nuru task 569); it is not hung.
