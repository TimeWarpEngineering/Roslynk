# Port upstream mrpmorris/Roslynk changes through dae11b1 onto the Nuru fork

## Description

Fork issue #14. Steven (2026-10-23): "Yes do 1 first. then we restructure". Pete's commits are wanted, but they
will conflict.

The fork split from `mrpmorris/Roslynk` at merge base `788735a`. Since then upstream has added 28 commits
(27 PRs plus one direct commit), up to **`dae11b148c3b79cd1c9b2cfb3067247df3a1e386`**. That commit is the pin; do
not take anything newer. The fork has replaced the upstream host with one TimeWarp.Nuru app (tasks 003–007).
`Source/App/Morris.Roslynk.Mcp` and its stdio bridge, HTTP daemon and appsettings are gone. The CLI, the
served daemon and McpHost now live in `Source/App/Morris.Roslynk.Cli`. The engine is still Pete's engine, with
operation contracts added (`[Operation]` request/response/renderer beside each tool).

This task comes **before** the `source/` restructure. Our paths still match upstream now, so git can merge
them. Doing the port by a **real merge** moves the merge base forward, so later upstream changes merge from
`dae11b1` and not from `788735a`.

## Requirements

- **Merge, do not cherry-pick or squash.** Add the remote in the worktree:
  `git remote add upstream https://github.com/mrpmorris/Roslynk.git` (fetch only; never push to it). Then run
  `git merge dae11b1` to make one merge commit, resolve the conflicts, and put follow-up fixes in separate
  commits after it. The merge base afterwards must be `dae11b1` (check with `git merge-base HEAD dae11b1`).
  Never push anything to `mrpmorris/Roslynk`, and never open PRs or issues there.
- **Engine changes port as-is**, then reach the fork's surfaces:
  - When a tool gains, loses or renames a parameter, change its `[Operation]` request property too. The
    property must keep the same name, `[Description]`, `[DefaultValue]` and initializer. The method's
    `[Description]` must match the operation description word for word. That covers #121 standardised names,
    #152 `query`→`symbolNameFilter`, #114 the `kinds` filter, #148 `solutionId` on `get_solution_status`, #153
    `countOnly`, #137 `includeHidden`, and anything else in the range.
    `OperationContractTests`, `CapabilitiesContractTests`, `McpSurfaceTests` and `HandlerTests.SampleOf` must
    agree.
  - A new error code, such as #122 `UnknownArgument`, gets an exit code in `Output/ExitCodes`, and the
    AGENTS.md and README exit-code tables are updated in the same change.
  - Changes to server instructions (#120, d7c570d "call open_solution in parallel") go into
    `McpServerRegistration.Instructions`.
  - **#119 (wait server-side for the solution load instead of returning `Indexing`):** port it into the engine
    path the served app's handlers use. Then check that a CLI call against a cold solution waits, and that
    cancellation still works (task 006 queued-cancel tests).
- **Upstream host changes** (the deleted `Morris.Roslynk.Mcp`). Adapt each one to the Nuru app, or drop it when
  the fork already covers it. Do not restore the project. Expected outcomes, which you may change with reasons:
  - #103 install-dir content root: adapt if the served app reads `appsettings.json`, otherwise drop.
  - #113 daemon version match: drop, because per-build sockets (nuru 508-017) already cover it.
  - #142 typed settings object: adapt into `RoslynkEngine`'s settings read.
  - #143 `appsettings.json.example`: adapt if the package ships settings.
  - #144 `/restart` endpoint: drop, because there is no HTTP daemon. If restart matters, note a follow-up.
  - #105 watcher errors and #108 binding errors: engine parts port; MCP binding errors go through Nuru's
    `ProblemRenderer` path.
- **Behavior Nuru must provide.** An example is #114, parsing JSON-array strings for array arguments, which
  happens in McpHost binding. Port what the engine can do. Do **not** patch around Nuru inside Roslynk. List
  each gap in Results with the exact timewarp-nuru change it needs. Steven launches nuru work himself.
- **Keep the fork's decisions.** Keep the package id `TimeWarp.Roslynk` and the command `roslynk`. Keep
  publishing to GitHub Packages only, with no nuget.org and no `mrpmorris` push in any workflow. Keep Nuru-first
  hosting. Keep the `[ganda.audit]` waivers. Keep the fork's README install section. When upstream workflow or
  README changes conflict with these, the fork wins. Take upstream's README tool documentation where it is
  still true.
- **releases.md:** keep upstream's release-history entries for the merged range, with their upstream issue
  numbers written as `mrpmorris/Roslynk#NNN`. Add one fork bullet under `# Unreleased`: "Ported upstream
  mrpmorris/Roslynk through dae11b1 (Fixes #14)".
- **Tests:** run `dotnet test Source/Morris.Roslynk.slnx --configuration Release`. List failures against the
  pre-merge baseline from origin/master: about 13–15 Razor tests already fail locally. A new failure must be
  fixed or explained. Never skip, quarantine or delete a test. `ganda repo audit` must pass, and every `--fix`
  it makes is committed.
- **Docs:** update AGENTS.md wherever the engine contract it describes changed (for example #119 Indexing,
  #153 countOnly paging, #107 solutionId slashes). Update `skills/roslynk/SKILL.md` if routing changed.

## Goal (implementer restatement)

Bring Pete's 28 upstream commits (`788735a..dae11b1`) into the fork with one real `git merge dae11b1`, so the
merge base moves to `dae11b1`. Upstream engine code wins; upstream host code (the deleted `Morris.Roslynk.Mcp`)
is adapted into the Nuru app (`Morris.Roslynk.Cli`) or dropped with a reason. Fork decisions win on packaging,
publishing, install docs and audit waivers.

Acceptance:

- `git merge-base HEAD dae11b1` prints `dae11b1…`; the merge is one merge commit with follow-ups after it.
- Every tool parameter/description change upstream is mirrored on its `[Operation]` request (name,
  `[Description]`, `[DefaultValue]`, initializer) and the method/operation descriptions match word for word;
  contract, capabilities, MCP-surface and handler tests agree.
- New error codes have exit codes, and the AGENTS.md and README tables list them.
- Server-instruction changes live in `McpServerRegistration.Instructions`.
- #119 (wait for the load instead of `Indexing`) works through the served handlers; cancellation still works.
- Release test run compared with the pre-merge baseline; no new unexplained failures; `ganda repo audit` clean.
- AGENTS.md, SKILL.md, README, releases.md (`mrpmorris/Roslynk#NNN` refs plus the fork's Fixes #14 bullet).
- Results: a table of all 28 commits marked ported / adapted / dropped, plus Nuru gaps.

## Checklist

- [x] `upstream` remote added in the worktree; `git merge dae11b1` produces one merge commit; merge base is
      `dae11b1`
- [x] Conflicts resolved: engine as upstream, hosting adapted to Nuru
- [x] Contracts, CLI options, MCP schema and tests follow every parameter/error change (one schema gap: N3)
- [x] `ExitCodes` plus the AGENTS.md/README tables for new error codes
- [x] Server instructions updated
- [x] Full Release test run compared with the pre-merge baseline
- [x] `ganda repo audit` clean, with fixes committed
- [x] AGENTS.md / SKILL.md / README / releases.md (Fixes #14)
- [x] Results: a table of all 28 upstream commits (sha, PR, title), each marked **ported**, **adapted**
      (where it went) or **dropped** (why), plus any nuru follow-ups

## Session

- Created: 1287ec9b-6d3e-4805-9dd9-5ef6634981c8 (2026-10-23)
- Implement (claude, 2026-10-23): `upstream` remote added (push URL disabled); `git merge dae11b1` →
  merge commit 2eaee8f (parents fac24b5, dae11b1); `git merge-base HEAD dae11b1` = dae11b1. Follow-ups:
  e48647d (Nuru app adaptation), 5302710 (CLI test renames + ported name/prose guards), 4dd16d1 (cold-load
  CLI test). `ganda repo audit` failed one advisory check (`vscode-window-icon`); `--fix` wrote
  `.vscode/settings.json`/`tasks.json`, which `.gitignore` excludes, so the fix has nothing to commit and the
  audit now passes. SKILL.md needed no change (routing is unchanged).
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-23T16:00:49Z
- Review round 1 (grok, 2026-10-23): session `01a1697e-58f8-7f11-a64e-930e306bb975`, effort 3, roster general. Open findings in `review/round-1/merged.md` (M1 bug, M2 nit). Disposition waits on the fix pass.

## Results

Merged by a real merge: 2eaee8f `Merge upstream mrpmorris/Roslynk through dae11b1 (#14)`, merge base now
`dae11b1`. Nothing was pushed to `mrpmorris/Roslynk` (the remote's push URL is set to `DISABLED`).

### Upstream commits

| # | sha | PR | Title | Outcome |
| --- | --- | --- | --- | --- |
| 1 | dbe9aa2 | #104 | Root the daemon's host content at its install directory (Fixes #103) | **dropped**: Nuru's generated configuration already uses `AppContext.BaseDirectory` with `reloadOnChange: false`, so there is no recursive content-root watch |
| 2 | cb64432 | #123 | UnknownArgument error (Fixes #122) | **ported** engine (`ErrorCode.UnknownArgument`, `UnknownArgument.Reject`, multi_query binder) + **adapted**: `ToolErrors` maps it, exit code 11; a call's own unknown key is Nuru's `error=Invalid` (gap N1) |
| 3 | 0973ab3 | #124 | Standardise parameter names (Fixes #121) | **ported**; every `[Operation]` request renamed (`symbolName`, `filePath`, `line`/`column`, `maxSize`) |
| 4 | 4ec00df | #125 | Ensure server instructions are correct (Fixes #120) | **adapted**: guards ported as `Cli.Tests/PublishedNamesTests` against the real `roslynk stdio` surface |
| 5 | 2030305 | #126 | Wait server-side for solution load instead of returning Indexing (Fixes #119) | **ported** engine + **adapted**: `OperationHandler.ReadAsync` passes the call token, `Roslynk:LoadWaitSeconds` in `RoslynkSettings`, `open_solution` handler async with `noWait`; new CLI cold-load test |
| 6 | f598cb1 | #127 | Auto-resolve a file path when exactly one solution file matches (Fixes #117) | **ported** |
| 7 | 6f11180 | #128 | Accept uniquely-matching partially-qualified symbol names by suffix (Fixes #118) | **ported** |
| 8 | d7c570d | (direct) | Tell harnesses to call open_solution in parallel | **adapted** into `McpServerRegistration.Instructions` |
| 9 | 55237c2 | #129 | Use System.Threading.Lock for the private gate fields (Fixes #112) | **ported** |
| 10 | 74b53f4 | #130 | Reuse the daemon only when its version matches the bridge (Fixes #113) | **dropped**: per-build sockets (timewarp-nuru 508-017) already keep builds apart |
| 11 | 4cae895 | #132 | Accept container-qualified dotted queries in search_symbols (Fixes #116) | **ported** |
| 12 | 5c4cf06 | #131 | Parse JSON-array strings for array arguments and add a search_symbols kinds filter (Fixes #114) | **ported** `kinds` (contract member) and the multi_query `JsonArrayArgument` path; the call-level string form needs Nuru (gap N2) |
| 13 | 4f3d945 | #133 | Echo solutionId with forward slashes so it can be reused verbatim in JSON (Fixes #107) | **ported** |
| 14 | 09d0f78 | #134 | find_references and find_dead_code ignore plain disposable locals as Dispose references (Fixes #88) | **ported** |
| 15 | 7be07af | #135 | Report precise invalid-JSON and multi_query binding errors (Fixes #108) | **ported** engine (`MultiQueryOperationConverter`, per-slot Invalid naming the index; works over CLI and MCP through Nuru's binder); `McpBodyValidation` **dropped** (JSON-RPC parsing is the MCP SDK's under McpHost); schema restore is gap N3 |
| 16 | 0058664 | #106 | Handle file watcher errors instead of ignoring them (Fixes #105) | **ported** (`WatchDiagnostics`, `watchDiagnostic=` in get_solution_status) |
| 17 | cca0002 | #139 | Report ambiguous filePath matches as Ambiguous instead of NotFound (Fixes #136) | **ported** |
| 18 | 4651b1d | #140 | get_diagnostics summaryOnly excludes hidden diagnostics unless includeHidden is set (Fixes #137) | **ported** |
| 19 | d294fc2 | #141 | Map Razor @inject members and BuildRenderTree markup back to the .razor source (Fixes #138) | **ported** (its two new Razor tests fail locally exactly as on pristine upstream; see Tests) |
| 20 | 0ea95a9 | #146 | Read all Roslynk settings from one typed startup object with nullable defaults (Fixes #142) | **adapted**: `Cli/Hosting/RoslynkSettings` (no `Port`), read once in `RoslynkEngine`; `Cli.Tests/RoslynkSettingsTests` |
| 21 | ed1623e | #147 | Add a loopback POST /restart endpoint and use it in the stdio bridge handshake (Fixes #144) | **dropped**: no HTTP daemon; `roslynk serve --stop` and the next call's auto-start cover a restart, so no follow-up |
| 22 | 1e2e338 | #145 | Ship a commented appsettings.json.example instead of appsettings.json (Fixes #143) | **adapted**: `Cli/appsettings.json.example` (no `Port`) copied beside `roslynk`; the example test lives in `RoslynkSettingsTests` |
| 23 | fabbb7d | #155 | Clamp startItem=0 to page 1 (Fixes #150) | **ported** |
| 24 | dd2d7ab | #156 | Rename search_symbols query to symbolNameFilter (Fixes #152) | **ported** + contract |
| 25 | 0e43e43 | #157 | Add optional solutionId to get_solution_status (Fixes #148) | **ported** + contract (optional member; a relative id resolves against the caller's directory like every `solutionId`) |
| 26 | 89e43b6 | #158 | get_members: exclude property/event accessors from methods (Fixes #149) | **ported** |
| 27 | 0ab3b07 | #159 | Add countOnly to every paging tool for header-only counts (Fixes #153) | **ported** + contracts |
| 28 | dae11b1 | #161 | Preserve each file's BOM in semantic write tools (Fixes #162) | **ported** |

Upstream's deleted host tests (`Morris.Roslynk.McpTests`) are not restored. What still applies was ported:
the name and prose guards (#120/#121), the settings tests (#142/#143), and the per-slot and optional-solutionId
expectations in `McpSurfaceTests`. Tests of the HTTP daemon, handshake, `/restart` and body validation have no
fork counterpart.

### Nuru gaps (Steven launches these)

- **N1, unknown argument code.** A call (`--json-args` or MCP `tools/call`) with an undeclared key gets Nuru's
  bind error `error=Invalid` ("Unknown key 'bogus' ... Known names: ..."), exit 1, where upstream returns
  `error=UnknownArgument`. timewarp-nuru change: give `NuruBindError` a kind for an unknown key and let
  `NuruProblems.FromBindError` emit a distinct problem `Type` (e.g. `UnknownArgument`) that an app's
  `IResponseRenderer<SharedProblemDetails>` and exit code can tell apart from other bind failures.
- **N2, JSON-array strings for array members.** `"kinds":"[\"Class\"]"` is refused ("wrong JSON type. Expected
  string array.") by Nuru's binder; upstream parses it. timewarp-nuru change: in the `--json-args`/McpHost
  binder, accept a JSON string whose content parses as a JSON array for an array-typed member, refusing a
  malformed one by key. Until then the `kinds`/`ids` descriptions, kept word for word from upstream, promise
  the string form, which works only inside `multi_query`. README says so.
- **N3, schema for a converter-bound JSON member.** `MultiQueryOperation` now carries
  `[JsonConverter(typeof(MultiQueryOperationConverter))]` (#108), and `NuruJsonMemberSchema` publishes
  `operations` as `{"type":"array"}` with no `items` (the exporter cannot describe a custom converter), so the
  tool enum and the arguments shape are gone from the MCP schema. Upstream restores the item shape in its
  host (`MultiQuerySchema.RestoreOperationsItem`; the class is kept in the engine, unused, for this).
  timewarp-nuru change: let a contract supply the published schema for a converter-bound type (for example an
  attribute naming a converter-free shape type to export, or a schema-override hook in the capabilities
  generator / `NuruMcpHostOptions`). Failing test until then:
  `McpSurfaceTests.WhenMultiQueryIsListed_ThenOperationsIsAnArrayOfToolEnumAndArgumentsObjects`.
  Alternative if Steven prefers the schema now: drop the type-level converter attribute (malformed operations
  then fail the whole call, as before the merge) and pass the converter explicitly in the two upstream engine
  tests that deserialize with it.

### Tests

`dotnet test Source/Morris.Roslynk.slnx --configuration Release`:

| Run | Morris.Roslynk.Tests | Morris.Roslynk.Cli.Tests |
| --- | --- | --- |
| Baseline (fac24b5, pre-merge) | 15 failed / 840 passed / 855 | 0 failed / 315 passed |
| This branch (4dd16d1) | 18 failed / 981 passed / 999 | 1 failed / 335 passed / 336 |

All 15 baseline failures (Razor: `RazorSourceToolTests` ×5, `RazorRenameTests` ×6, `ChangeSignatureTests`
Razor ×2, `GetExpressionInfoTests` Razor ×1, `RazorGenerationTests` ×1) still fail, unchanged. New failures:

- `RazorSymbolMappingTests.WhenGettingTheBodyOfAnInjectProperty_ThenTheDirectiveLineIsReturned`,
  `RazorSymbolMappingTests.WhenListingTheMembersOfARazorComponent_ThenTheInjectPropertyIsUnderTheRazorFile`
  (#141) and `RazorRenameTests.WhenARenamedRazorFileHasAUtf8Bom_ThenTheBomIsPreserved` (#161): new upstream
  Razor tests that fail on this machine the same way on pristine upstream `dae11b1` (checked: `git archive
  dae11b1`, Razor filter, 15 failed including all three). Same local Razor-generator cause as the baseline 15.
- `McpSurfaceTests.WhenMultiQueryIsListed_ThenOperationsIsAnArrayOfToolEnumAndArgumentsObjects`: gap N3.

### How to validate

Smoke:

```bash
git merge-base HEAD dae11b1                       # dae11b148c3b79cd1c9b2cfb3067247df3a1e386
dotnet build Source/Morris.Roslynk.slnx -c Release
B=Source/App/Morris.Roslynk.Cli/bin/Release/net10.0/roslynk
S=$PWD/Source/TestFixtures/SimpleSolution/SimpleSolution.slnx
$B search_symbols --solutionId "$S" --symbolNameFilter Calc --countOnly
$B search_symbols --json-args "{\"solutionId\":\"$S\",\"symbolNameFilter\":\"Calc\",\"kinds\":[\"Class\"]}"
$B multi_query --json-args "{\"solutionId\":\"$S\",\"operations\":[{\"tool\":\"get_symbol\",\"arguments\":{\"symbolName\":\"Calculator\",\"bogus\":1}},{\"tool\":\"nope\"}]}"
$B get_solution_status --solutionId "$S"
$B serve --stop
dotnet test Source/Morris.Roslynk.slnx --configuration Release
```

Expect:

- The merge base is `dae11b1`, and the build has 0 warnings and 0 errors.
- The first call on a cold server returns results, not `error=Indexing`, exit 0. `--countOnly` prints
  `count=1` and `count.class=1`.
- `kinds` as a real array lists `class,Calculator`.
- In multi_query, slot 1 is `error=UnknownArgument` ("Unknown argument 'bogus' for get_symbol. Valid:
  symbolName."), slot 2 is `error=Invalid` naming operations[1], and the exit code is 0.
- `get_solution_status --solutionId` lists only that solution, with forward slashes.
- Tests: exactly the failures listed above (15 baseline Razor + 3 new upstream Razor + N3).

## Notes

- Upstream range, oldest first: dbe9aa2 #104, cb64432 #123, 0973ab3 #124, 4ec00df #125, 2030305 #126,
  f598cb1 #127, 6f11180 #128, d7c570d (direct), 55237c2 #129, 74b53f4 #130, 4cae895 #132, 5c4cf06 #131,
  4f3d945 #133, 09d0f78 #134, 7be07af #135, 0058664 #106, cca0002 #139, 4651b1d #140, d294fc2 #141,
  0ea95a9 #146, ed1623e #147, 1e2e338 #145, fabbb7d #155, dd2d7ab #156, 0e43e43 #157, 89e43b6 #158,
  0ab3b07 #159, dae11b1 #161.
- A read-only clone of upstream is at `/home/steve/worktrees/github.com/mrpmorris/Roslynk/master`. It is
  useful for `git show`. Do not work, commit or branch in it.
- Next steps after this task: the `source/` restructure task, then 008 (release flow), then `dev release`
  1.0.0-beta.1.
- Review round 1 is open: `review/round-1/merged.md` (M1 `change_signature` drops the load-wait token; M2 leftover README settings bullets). No disposition until those are fixed or marked wontfix.
