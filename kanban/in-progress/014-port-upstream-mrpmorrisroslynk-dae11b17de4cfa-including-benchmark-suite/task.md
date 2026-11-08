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

## Goal (implementer restatement)

Bring Pete's 16 commits after `dae11b1` into this fork with one real merge, and push nothing to `mrpmorris/Roslynk`. The brief names `7de4cfa` as the pin, but that commit is the first of the 16 (its parent is `dae11b1`). The other 15, including the benchmark suite, sit on top of it, and upstream `master` at this walk is their tip, `8e14794`. Merging that tip takes every listed commit and nothing newer. `git merge-base HEAD 7de4cfa` is still `7de4cfa`, because that commit is an ancestor of the tip.

Upstream engine code lands as Pete wrote it. Because this fork renamed `Source/` to `source/` after task 009, the merge follows that rename and any file that still arrives under `Source/` is moved under `source/`. The deleted `Morris.Roslynk.Mcp` host is not restored: daemon, Aspire, and OpenTelemetry host edits are carried into the Nuru app (`Morris.Roslynk.Cli`) or dropped with a reason. Fork choices stay: package `TimeWarp.Roslynk`, command `roslynk`, GitHub Packages only, Nuru hosting, the task 013 audit waivers, and the fork install section.

Acceptance:

- `git merge-base HEAD 7de4cfa` prints `7de4cfa`. The merge is one merge commit; fixes after it are separate commits.
- A tool parameter or description that upstream changed is the same on its `[Operation]` request: member name, `[Description]`, `[DefaultValue]`, initializer, and the method description word for word. `OperationContractTests`, `CapabilitiesContractTests`, `McpSurfaceTests`, and `HandlerTests.SampleOf` agree.
- New error codes have exit codes in `Output/ExitCodes` and rows in the AGENTS.md and README tables. Server-instruction edits live in `McpServerRegistration.Instructions`.
- New settings (analyzer budget, compiler-argument cache, OpenTelemetry knobs) are read through `RoslynkSettings` / `RoslynkEngine`, and `appsettings.json.example` when the setting is one a user ships.
- `Benchmarks/` is in the tree and drives `roslynk stdio` (MCP over stdio, per-build Unix socket), not Pete's HTTP daemon. `scripts/reset.sh` can reset `TestSolution`. Audit stays green; upstream-shaped fixture files are waived under `[ganda.audit]` with a reason when a check would otherwise restyle them. Results say whether `source/TestFixtures/Benchmarks/Benchmarks.md` stays, and how to run the suite against upstream's build and ours. The full AI benchmark is not run here.
- `releases.md` keeps upstream's history lines for this range with `mrpmorris/Roslynk#NNN`, plus one `# Unreleased` fork bullet: ported through `7de4cfa`, including the benchmark suite (Fixes #24). AGENTS.md picks up upstream guidance where it changed; hosting text stays the fork's.
- `bin/dev build` and `bin/dev test` pass for both test projects. `ganda repo audit` exits 0.
- Results name each Nuru gap and the timewarp-nuru change it needs. Roslynk does not paper over those gaps.
- Results list each of the 16 commits as ported, adapted (where), or dropped (why), and include a non-empty How to validate (Smoke + Expect).

This walk is the fix pass for review round 1. The merge and the Nuru follow-up are already on the branch. The remaining work is the two open findings in `review/round-1/merged.md`.

Fix-pass acceptance:

- `HandlerTests` drives `OperationHandler.ReadAsync` with the tool replaced by a delegate. One delegate cancels its token after the snapshot is captured. The other publishes a new solution before it returns. Each result is a problem whose `Type` is `Stale` and whose `Status` is 6. The `snapshot=` header is present on the problem extensions, and the rendered problem outline contains `snapshot=`.
- The diagnostics sentence in `AGENTS.md` matches `RoslynInstance`: `RequestDiagnosticsAsync` returns `completion.Task.WaitAsync(token)`, and the consumer skips an item whose token is already cancelled.
- The new handler cases pass, and `ganda repo audit` exits 0.

## Checklist

- [x] Upstream remote added; `git merge 7de4cfa` merge commit; merge-base verified
- [x] Conflicts resolved; `Source/` paths land under `source/`
- [x] Engine changes reach `[Operation]` contracts, exit codes, server instructions, settings
- [x] Host changes adapted or dropped with reasons
- [x] `Benchmarks/` ported and driving `roslynk stdio`; comparison how-to in Results
- [x] releases.md and AGENTS.md updated
- [x] `bin/dev build` and `bin/dev test` pass (both test projects)
- [x] `ganda repo audit` exits 0
- [x] Nuru gaps listed in Results
- [x] M1: `HandlerTests` covers a captured read that is cancelled or superseded, exit 6, `snapshot=` kept
- [x] M2: `AGENTS.md` diagnostics cancellation matches `WaitAsync`

## Session

- Created: 93b68595-4648-49ee-a58b-6a1ec1f5dab9 (2026-11-09)
- 2026-11-09 implementer (01a1bc70-aead-73d2-86f4-4aa02bf10639): merged tip `8e14794` as `f80a0f5` (the kitchen pin `7de4cfa` is the first of the sixteen and an ancestor). Follow-up `44aa307` carries the Nuru surface, the binlog reader, and the fixture restore. Audit 35 passed. Tests: CLI 337, engine 1115. The AI benchmark was not run. Nothing was pushed to `mrpmorris/Roslynk`.
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1bc70-aead-73d2-86f4-4aa02bf10639, max-turns 500, Done — 2026-11-08T19:59:03Z
- Review oracle round 1: reviewer-claude (Opus 5.5, claude-opus-5-5), effort 3, roster general, max-turns 200, 2026-11-09. Findings: 0 bug, 1 suggestion (M1), 1 nit (M2), all open. Fix pass requested. Artifacts: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`.
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5), session not reported, max-turns 200, artifacts general.md, merged.md — 2026-11-08T20:05:24Z
- 2026-11-09 implement-fix-2: closed the two open round-1 findings on this branch. `HandlerTests` now drives `ReadAsync` for a mid-query cancellation and a superseded snapshot, and `AGENTS.md` describes `WaitAsync`. `./bin/dev build` 0 warnings / 0 errors. `./bin/dev test` CLI 338, engine 1115. `ganda repo audit` exit 0 (35 passed, 2 skipped). Round 2 has not been run.
- Review oracle round 2: reviewer-claude (Opus 5.5, claude-opus-5-5), effort 2, roster general, max-turns 120, session not reported (headless print mode), 2026-11-09. Delta `a8d3bdc0..ee3efb02`. M1 and M2 are fixed, with no new findings. HandlerTests 44/44 in this worktree. Disposition: clean. Artifacts: `review/round-2/general.md`, `review/round-2/merged.md`, `review/disposition.md`.

## Results

One real merge, `f80a0f5` `Merge upstream mrpmorris/Roslynk through 8e14794 (Fixes #24)`, parents `28fe36ed` and `8e147947b93b00b0674858a32a698338b397945b`. `git merge-base HEAD 7de4cfa` is `7de4cfaa16179213a5072f1ae17313148c70ba47`. `git merge-base HEAD 8e14794` is `8e14794`. The kitchen names `7de4cfa` as the pin; that commit is the oldest of the sixteen (its parent is `dae11b1`). Upstream `master` at this walk is their tip, `8e14794`, and nothing newer exists, so the merge takes every listed commit. The upstream remote is `https://github.com/mrpmorris/Roslynk.git` with push URL `DISABLED`. There is no `Source/` directory and no `Morris.Roslynk.Mcp` project; rename detection landed the engine under `source/`.

Follow-up commit `44aa307` is separate from the merge. It wires the new surface through the Nuru app, lets the SDK 10 binlog reader accept a log the newer build host writes, and restores every in-place fixture so package-free projects still have a targeting pack.

### Upstream commits

| # | sha | PR | Title | Outcome |
| --- | --- | --- | --- | --- |
| 1 | 7de4cfa | #167 | Place Razor-declared symbols under the .razor file, not the generated .g.cs path (#164) | **ported** |
| 2 | 0470ab2 | #169 | get_solution_status reports NuGet restore warnings as loadWarning (#163) | **ported**, and the `[Operation]` description matches the tool. `loadWarning` and `loadWarningsTruncated` are in `PublishedNamesTests` |
| 3 | b633239 | #168 | Guard read-only queries against a superseded snapshot (#165) | **ported** and **adapted**: `OperationHandler.ReadAsync` runs `QueryExecution`. Instructions keep the fork sentence (a call is `error=Invalid`, a `multi_query` slot is `error=UnknownArgument`) and add the snapshot-guard sentences. `ExitCodes.Stale` is already 6. This range adds no error code |
| 4 | 01b8d20 | (direct) | Update Aspire version | **dropped** as a host change: the AppHost was already `Aspire.AppHost.Sdk/13.6.4` and still runs `roslynk serve`. The deleted `Morris.Roslynk.Mcp` project stays deleted, and with it the dev-daemon version handshake (`0.0.1-local` refusing a remote shutdown). This host has no HTTP endpoint |
| 5 | 5a6aec8 | #173 | Record OTEL span and metric tag values in full (#172) | **ported** in the engine. `AddOtlpExporter` still reads `OTEL_EXPORTER_OTLP_*` |
| 6 | bd87e36 | #171 | Cache compiler arguments captured during the MSBuild load (#170) | **ported**. There is no `Roslynk:` key; every load uses `Roslynk/CompilerArguments` under local application data. Fork follow-up in `CompilerArgumentCapture.Read`: `AllowForwardCompatibility` and an empty `RecoverableReadError` handler. On this machine the out-of-process build host is SDK `11.0.100-rc.3` and writes binlog format 28; `MsBuildRegistrar` loads SDK `10.0.403`, whose reader stops at 26 unless that flag is set. The reader also refuses the log when nothing subscribes to `RecoverableReadError`. Loading SDK 11 `Microsoft.Build` into this net10 process does not work: that assembly needs `System.Runtime` 11.0.0.0. A format-28 design-time log replayed with the flag produced the same `CscCommandLineArgs` as an SDK 11 reader. `CompilerArgumentCacheTests` 40/40 |
| 7 | 456684a | #175 | Add reproducible AI benchmark suite for Roslynk (#174) | **adapted**: `Benchmarks/` is in the tree. `Benchmarks/run.md` drives `roslynk stdio` (MCP over stdio, this build's Unix-socket server) and does not call `POST /restart`. `Benchmarks/TestSolution/scripts/reset.sh` still restores that directory only (`bash -n` passed). `Benchmarks/TestSolution` did not trip `ganda repo audit`, so no new `[ganda.audit]` waiver. The full AI benchmark was not run |
| 8 | 9fcf739 | #180 | Resolve positional record properties by name (#176) | **ported** |
| 9 | a05fc09 | #181 | Return whole declaration for a bodiless type in get_symbol (#179) | **ported**, and the `[Operation]` description matches the tool method |
| 10 | c957312 | #184 | Reduce test times (#160) | **ported**. Fork follow-up: `TestSolutions.Prepare` always calls `EnsureSourceRestored`. A package-free net8 library still resolves its targeting pack from `obj/project.assets.json`. Without that file the design-time build loads the project with no metadata references (constants, source generators, and `using` disposal disappear). Scratch copies already restored |
| 11 | fc9a0b1 | #185 | Output fewer OTEL traces (#182) | **dropped**: `McpTransportNoiseFilterExporter`. The Nuru host emits neither those MCP protocol spans nor ASP.NET server spans, so there is nothing for that filter to remove |
| 12 | 4216b1b | #194 | Fix stale code-action diagnostics after a method-body edit (#187) | **ported**. The diagnostics cache key is the project's dependent version, so a method-body edit misses |
| 13 | 9020fc4 | #197 | apply_patch applies several sections for the same file in order (#186) | **ported**, and the `[Operation]` description matches the tool method |
| 14 | 84a165b | #196 | Keep unrestored backups when a batch-write rollback fails (#188) | **ported** |
| 15 | 8910f92 | #198 | get_diagnostics and change_signature honour the caller's cancellation token (#189) | **ported** |
| 16 | 8e14794 | #195 | Make the code-action analyzer budget configurable and raise it for tests (#190) | **adapted**. The engine reads `ROSLYNK_ANALYZER_BUDGET_SECONDS` once (AppContext, then the environment; default 5; the test project sets 60 through its runtimeconfig). `Roslynk:AnalyzerBudgetSeconds` is kept as text and, when set, written to that AppContext key before `AddRoslynkEngine`, so a present key wins over the environment the way the engine's own order does. Shipped in `RoslynkSettings`, `appsettings.json.example`, README, and AGENTS.md |

### Host changes

- HTTP daemon, `DaemonEndpoints`, `DaemonHandshake`, and `POST /restart`: dropped. `roslynk serve` is the per-build Unix socket, and `roslynk stdio` is MCP over stdio.
- Aspire 13.6.4: already the fork's AppHost SDK. Not a second bump, and not a reason to restore `Morris.Roslynk.Mcp`.
- OpenTelemetry full tag values (#172): kept in the engine.
- OpenTelemetry transport-noise filter (#182): dropped, reason in row 11.
- Analyzer budget (#190) and the compiler-argument cache (#170): wired as rows 16 and 6. The cache is not a user setting.

### `source/TestFixtures/Benchmarks/Benchmarks.md`

Kept. It is an older latency procedure against a private solution, and it already drives `roslynk stdio`. It is not the #174 suite under `Benchmarks/`.

### Comparing this build with upstream

Run the suite in this worktree by following `Benchmarks/run.md`. The harness's Roslynk command is `roslynk stdio`. That starts this build's Unix-socket server when none is running. Reset the fixture with `Benchmarks/TestSolution/scripts/reset.sh` before a measured session, and do not leave a gap long enough for `Roslynk:IdleEvictionMinutes` (default 30) to evict it.

To measure upstream, check out `mrpmorris/Roslynk` at `8e14794` and follow that checkout's `Benchmarks/run.md`, which drives Pete's HTTP daemon. Do not point that daemon at this fork's socket, and do not point this fork's `roslynk stdio` at that daemon. This task did not run a measured session on either side.

### Nuru gaps

This range adds none. Roslynk does not patch around these. They are the same gaps task 009 recorded, and Steven launches them on timewarp-nuru:

- **N1, unknown argument code.** A call (`--json-args` or MCP `tools/call`) with an undeclared key gets Nuru's bind error `error=Invalid`, exit 1, where upstream returns `error=UnknownArgument`. `multi_query` still reports `UnknownArgument` per slot, exit 11. timewarp-nuru change: give `NuruBindError` a kind for an unknown key and let `NuruProblems.FromBindError` emit a distinct problem `Type` that an app's problem renderer and exit code can tell apart from other bind failures.
- **N2, JSON-array strings for array members.** A JSON string whose content is a JSON array is refused by Nuru's binder for an array-typed member. It works only inside `multi_query`. timewarp-nuru change: in the `--json-args` / McpHost binder, accept that string form and refuse a malformed one by key.
- **N3, schema for a converter-bound JSON member.** Resolved in task 009 by dropping the type-level `MultiQueryOperation` converter attribute. No further change in this range.

### Gates

| Gate | Result |
| --- | --- |
| `./bin/dev build` | Succeeded. 0 warnings, 0 errors. About 4 s on the fix-pass rebuild |
| `./bin/dev test` | CLI 338 passed, 0 failed (337 plus the stale-snapshot handler test). Engine 1115 passed, 0 failed, about 2 m 42 s |
| `ganda repo audit` | Exit 0. Passed 35, failed 0, skipped 2. No new waiver |
| `bash -n Benchmarks/TestSolution/scripts/reset.sh` | Exit 0 |
| AI benchmark | Not run |

### How to validate

Smoke:

```bash
git merge-base HEAD 7de4cfaa16179213a5072f1ae17313148c70ba47
git merge-base HEAD 8e147947b93b00b0674858a32a698338b397945b
./bin/dev build
./bin/dev test
dotnet test source/App/Morris.Roslynk.Cli.Tests/Morris.Roslynk.Cli.Tests.csproj --filter FullyQualifiedName~WhenACapturedReadIsCancelledOrSuperseded
ganda repo audit
bash -n Benchmarks/TestSolution/scripts/reset.sh
```

Expect:

- The first merge-base is `7de4cfaa16179213a5072f1ae17313148c70ba47`. The second is `8e147947b93b00b0674858a32a698338b397945b`.
- `./bin/dev build` reports 0 warnings and 0 errors.
- `./bin/dev test` reports 338 passed in `Morris.Roslynk.Cli.Tests` and 1115 passed in `Morris.Roslynk.Tests`, with 0 failed in each. The extra CLI test is `WhenACapturedReadIsCancelledOrSuperseded_ThenTheHandlerReturnsStaleWithTheSnapshot`.
- That filtered test passes 1. It is the handler round trip: a query cancelled after capture, and a query that publishes a new solution before it returns, each come back with `Type` `Stale`, `Status` 6, and `snapshot=` in the rendered outline.
- `ganda repo audit` exits 0.
- `reset.sh` is valid bash. It restores `Benchmarks/TestSolution` only. Do not treat a syntax check as a measured benchmark run.
- `AGENTS.md` says `RequestDiagnosticsAsync` returns `completion.Task.WaitAsync(token)` and the consumer skips an item whose token is already cancelled.

### Review

Round 1 (general, effort 3) found one suggestion (M1) and one nit (M2). The fix pass on this branch addresses both. `HandlerTests.WhenACapturedReadIsCancelledOrSuperseded_ThenTheHandlerReturnsStaleWithTheSnapshot` drives `OperationHandler.ReadAsync` with a delegate: cancelling after the snapshot is captured, and publishing a new solution before the query returns, each yield `Type` `Stale` and `Status` 6, and the `snapshot=` header is on the problem extensions and in the `ProblemRenderer` outline. `AGENTS.md` now matches `RoslynInstance`: `RequestDiagnosticsAsync` returns `completion.Task.WaitAsync(token)`, and the consumer skips an item whose token is already cancelled. Round 1 files were not edited. See `review/round-1/merged.md`.

Round 2 (general, effort 2, delta `a8d3bdc0..ee3efb02`) re-checked M1 and M2 against the source and the test run (HandlerTests 44/44) and marked both **fixed**. It raised no new findings.

**Review disposition: clean.** 2 rounds, roster general (effort 3, then 2). Final counts: bug 0/0/0, suggestion 0 open / 1 fixed / 0 wontfix, nit 0 open / 1 fixed / 0 wontfix. Paths: `review/review-framework.md`, `review/round-2/merged.md`, `review/disposition.md`.

## Learnings

- Misread in the brief, not by the implementer: the task names `7de4cfa` as the pin and says `git merge 7de4cfa`, but that commit is the oldest of the sixteen (its parent is `dae11b1`). The brief listed them oldest-first, not in the newest-first order `git log` uses. Merging `7de4cfa` alone would have left out the benchmark suite and 14 other listed commits. The restatement merges the tip `8e14794` instead. The reviewer checked this with `git log dae11b1..8e14794` and judged it the correct reading of the intent: all 16 commits, nothing newer. why: review oracle round 1, 2026-11-09

## Review

- Round 1: M1 and M2 addressed on this branch by the fix pass.
- Round 2: Done
## Notes

- Prior port: task 009 / PR #14 (through dae11b1).
- `dev check-version` takes ~2 min silently (nuru task 569); it is not hung.
