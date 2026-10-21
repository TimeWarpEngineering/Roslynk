# Served-app follow-ups from the PR 9 review: Nuru 508-017 and 508-018 adoption, diagnostics cancel in the queue, Windows detached server, stale port text

## Description

Follow-ups from the independent review of PR #9 (task 005, fork issue #8; verdict merge with noted issues).
No new GitHub issue: extend the existing Unreleased `(Fixes #8)` bullet in `releases.md`.

- **Build thrash (medium, Nuru).** Two builds of `roslynk` sharing one socket stop each other's server and cancel
  in-flight calls (Nuru daemon client stops a server whose build id differs). Fixed upstream by timewarp-nuru
  **508-017**; adopt its model here (pin, docs: README/AGENTS.md say what happens with `dnx Roslynk -- stdio` beside a
  global tool).
- **MCP binding errors (low-medium, Nuru).** A missing or wrong argument over MCP returns Nuru's plain
  `Error: ...` text, not the `error=Invalid` outline the server instructions promise. Fixed upstream by
  timewarp-nuru **508-018**; adopt it, and add an MCP test (missing `symbolName` on `get_symbol` gives
  `error=Invalid`).
- **Diagnostics cancel in the queue (low).** `RoslynInstance.RequestDiagnosticsAsync` returns the queued completion;
  the token is only observed when the single consumer runs the item, so a cancelled `get_diagnostics` behind a long
  write keeps its call open. Let the awaiting handler stop waiting on cancellation (and let the queued item skip if
  already cancelled). Test with a controlled long write.
- **Windows detached server (read-only finding).** The Nuru launcher uses `setsid` on Linux only; on Windows the
  detached server is a plain redirected child and may die with the client's console or job object. Verify on
  Windows or file it to Nuru; record which. Note in README that the old Windows-service hosting option is gone.
- **Stale text.** `Source/TestFixtures/Benchmarks/Benchmarks.md:5` still says JSON-RPC at `127.0.0.1:6502`.

## Goal (implementer restatement)

Finish the PR #9 review follow-ups on the served `roslynk` app by moving to TimeWarp.Nuru 4.0.0-beta.5 and
using what it ships, plus three small Roslynk fixes.

Acceptance criteria:

- Nuru, Nuru.Daemon, Nuru.McpHost and Nuru.DevCli are pinned to 4.0.0-beta.5 in both props files.
- 508-017: README and AGENTS.md say the default socket is per build (`roslynk-<8 hex>.sock`), so a `dnx Roslynk -- stdio`
  beside a global `roslynk` runs two warm servers and neither stops the other; `NURU_DAEMON_SOCKET` (the Aspire
  AppHost) still pins one socket; `roslynk serve --status` is shown.
- 508-018: the builder calls `UseProblemRendererForErrors()`. An MCP call with a missing required argument
  (`get_symbol` without `symbolName`) returns the `error=Invalid` outline, with a test. The CLI exit code for a
  binding error is decided and documented.
- 508-013: the app takes its 29 operation routes from the engine assembly
  (`[assembly: NuruEndpointAssembly(...)]`) instead of linked source files. Routes, groups, descriptions,
  defaults and the MCP schema stay the same (existing contract tests pass), and a forwarded call or
  `--capabilities` still loads no Roslyn or MSBuild.
- 508-007: cold `--capabilities` and a forwarded call are re-measured; no NURU056 warning.
- `get_diagnostics` stops waiting when its call is cancelled while queued behind a long write, and a queued
  diagnostics item that is already cancelled is skipped. Tested with a controlled long write.
- Windows detached server: verified, or filed upstream; README notes the old Windows-service hosting is gone.
- `ganda repo audit` passes (`workflow-file` fixed with `--fix`).
- `Benchmarks.md` no longer mentions JSON-RPC at `127.0.0.1:6502`.
- The `(Fixes #8)` bullet in `releases.md` covers these changes.

PR #10 review fix pass (same task, same PR):

- The publish workflow keeps the `pull_request` trigger the audit wants, but a PR build only tests: deriving the
  version, packing, NuGet login and push run only on tag builds. `ganda repo audit` still passes `workflow-file`.
- `ProcessIsolationTests` covers every operation route (plus `--capabilities`), each call binding and being
  forwarded, and none loads `Microsoft.CodeAnalysis*`, `Microsoft.Build*`, `ModelContextProtocol*` or `OpenTelemetry*`.
- The diagnostics cancellation registration is disposed when the awaited result completes, not from a bare
  `ContinueWith`; the behavior and the cancel test stay the same.

## Checklist

- [x] **Start only after PR #9 (task 005) is merged**; merge origin/master into this branch first (PR #9 merged de1df13 2026-10-21 with --skip-audit; master merged in)
- [x] TimeWarp.Nuru (+ DevCli, Daemon, McpHost) 4.0.0-beta.5 pinned in both props files
- [x] 508-017: per-build socket (`<app>-<8 hex>.sock`) documented in README/AGENTS.md (what `dnx Roslynk -- stdio`
      beside a global `roslynk` does now: two warm servers, never a stop); the Aspire socket name still works;
      `roslynk serve --status` shown in docs
- [x] 508-018: `UseProblemRendererForErrors()` on the builder, so MCP and CLI binding errors and escaped exceptions
      render through Roslynk's problem renderer (`error=Invalid` / `error=Faulted`); decide whether CLI binding errors
      keep exit 1 (usage) or follow the problem's exit code, and document it (decided: keep exit 1)
- [x] 508-013: drop the linked-source workaround. The `roslynk` app opts in with
      `[assembly: NuruEndpointAssembly(typeof(GetSymbol.Query))]`; the linked `<Compile>` items and the `engine` alias are
      gone; routes, groups, descriptions and MCP schema unchanged; the process-isolation test still proves a forwarded call
      loads no Roslyn or MSBuild. **Not done as written:** the engine does *not* get `[assembly: MediatorAssembly]`
      (not needed, and it breaks the CLI test build with TWM001; see Results)
- [x] 508-007: services are built on first use; re-measured cold `--capabilities` and a forwarded call; no NURU056 warning
- [x] MCP binding-error test gives `error=Invalid`
- [x] Diagnostics cancellation does not wait on the queue; test
- [x] Windows detached server verified (on Windows, from WSL): survives the client and its console, dies with a kill-on-close
      job; upstream task handed to Steven to file (see Results)
- [x] `workflow-file` audit: applied `ganda repo audit --fix` (adds the `pull_request` trigger); audit passes
- [x] Benchmarks.md fixed
- [x] `releases.md` (Fixes #8) bullet extended
- [x] PR #10 review fix: in `.github/workflows/workflow.yml` (the NuGet publish workflow) run the `NuGet login` and `Push` steps only on tag builds (`if: startsWith(github.ref, 'refs/tags/')`), and the `Derive version`/`Pack` steps too if a PR build would otherwise fail on `VERSION=<n>/merge`. Keep the audit's `pull_request` trigger; `ganda repo audit` must still pass `workflow-file`. Update the workflow's header comment
- [x] PR #10 review fix: widen `ProcessIsolationTests` from `--capabilities` + `get_symbol` to every operation route (a theory over the catalog), or at minimum one write route and `multi_query`, asserting no `Microsoft.CodeAnalysis*`, `Microsoft.Build*`, `ModelContextProtocol*` or `OpenTelemetry*` loads
- [x] PR #10 review nit: dispose the diagnostics cancellation registration in `RoslynInstance.RequestDiagnosticsAsync` deterministically rather than from a bare `ContinueWith`, without changing behavior (the cancellation test must still pass)

## Session

- Created: 2532391 (2026-10-21) by the Roslynk cockpit from the PR #9 review.
- 2026-10-21 cockpit: unblocked by TimeWarp.Nuru 4.0.0-beta.5; scope widened to drop the linked sources (508-013)
  and adopt lazy services (508-007). Moved to to-do; dispatch after PR #9 merges.
- 2026-10-21 implementer-claude (headless `ganda task work`, implement oracle): pinned Nuru 4.0.0-beta.5; dropped the
  linked contract sources for `NuruEndpointAssembly`; `UseProblemRendererForErrors()`; diagnostics cancel in the queue;
  verified the detached server on Windows; docs; `ganda repo audit --fix` (`workflow-file`: `pull_request` trigger;
  `vscode-window-icon` wrote the git-ignored `.vscode/` files only). Audit passes.
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-21T04:50:19Z
- Review oracle: Grok 4.7, session `01a15ccb-c347-7af3-8e6e-a781428ab9cc` (2026-10-21). Round 1, effort 3, roster general. M1 open. Disposition not written.
- Review oracle: review by reviewer-grok (grok, model grok-4.7), session not reported, max-turns 200 — 2026-10-21T05:00:12Z
- 2026-10-21 implementer-claude (implement-fix-2 oracle): fixed round-1 M1 — AGENTS.md MCP-contracts paragraph no longer
  cites `OperationHandler.ToEngine` or an app-side contract copy; the JSON round-trip rule now rests on Nuru binding
  `--json-args` and MCP arguments. `merged.md` M1 marked fixed. Audit passes.
- Review oracle round 2: Grok 4.7, session `01a15cd5-bede-7a62-a2eb-d2ba55f25646` (2026-10-21). Effort 1, roster general. M1 re-verified fixed. No new findings. Disposition clean.
- Fix pass requested 2026-10-21 by Steven ("yes add the fixes to the current task") after the independent Sonnet review of PR #10: tag-gate the publish workflow's login/push, widen ProcessIsolationTests, dispose the diagnostics cancel registration. Same PR #10; do not open a new PR
- 2026-10-21 implementer-claude (implement oracle, PR #10 fix pass): tag-gated the workflow's version/pack/login/push
  steps; `ProcessIsolationTests` is a theory over all 29 operation routes plus `--capabilities`; the diagnostics
  registration is disposed by an awaiting wrapper. CLI tests 315 passed; audit passes.
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-21T05:53:42Z
- Review oracle round 3: Grok 4.7, session `01a15d05-c93d-7780-b83e-8ca4b792d782` (2026-10-21). Effort 3, roster general. M1 still fixed. No new findings. Disposition clean. Re-ran `ProcessIsolationTests` (31 passed) and the diagnostics-cancel test (1 passed). `ganda repo audit` passes, including `workflow-file`.

## Review

- Round 1: Blocked — open M1
- Round 2: Done
- Round 3: Done
## Notes

- TimeWarp.Nuru 4.0.0-beta.5 (2026-10-21) ships 508-007, 508-013, 508-017, 508-018; release warnings are in the
  nuru `kanban/done/514-release-400-beta5` task.
- Environment note from the review: CLI tests fail (143 of 283) when `/tmp/nuget.config` is a dangling symlink;
  run with `TMPDIR` elsewhere or remove the stray symlink.

## Results

### What changed

- **Nuru 4.0.0-beta.5** (Nuru, Daemon, McpHost in `Source/Directory.Packages.props`; Nuru, DevCli in the root props).
- **508-013, contracts from the engine assembly.** `Program.cs` has `[assembly: NuruEndpointAssembly(typeof(GetSymbol.Query))]`.
  The CLI csproj no longer compiles `Features/**/*.cs` and the five Infrastructure files as linked sources, and it references the
  engine without the `engine` alias. The handlers use the engine's contract types directly. `OperationHandler.ToEngine` (a JSON
  copy onto the engine's twin type) became `FromCaller`, which resolves a relative `solutionId`/`solutionPath` in place. The
  29 nested `Renderer` classes in the engine contracts are now `public`, because Nuru ignores an internal renderer in a
  referenced assembly. Every defaulted request member already had a matching `[DefaultValue]`.
  - **No `[assembly: MediatorAssembly]` on the engine.** The checklist asked for it, but it is not needed and it breaks the
    build. The handlers live in the app, and the app's generated mediator registers them. A local `--no-daemon` call and a
    served call both dispatch without the attribute. With it, `Morris.Roslynk.Cli.Tests` (a mediator generator host by
    transitive reference) failed with TWM001 for all 29 requests, because they would have joined its graph without handlers.
    Nuru's doc needs the attribute only when the handlers live in the contracts assembly. So the engine stays free of both
    Nuru and Mediator markers. AGENTS.md records this.
  - **Catalog diff** (old vs new `--capabilities`, same build otherwise): the 30 endpoints, groups, descriptions, titles,
    kinds, options, required flags and types are the same. The one difference: 18 nullable options with `[DefaultValue(null)]`
    no longer show `"defaultValue": "null"`, because Nuru's metadata path reads a null `[DefaultValue]` as no default. The MCP
    schema does not change. McpHost already left a `"null"` default out of the schema (`DefaultJson` and `JsonDefault` return
    null for it), and `McpSurfaceTests` (schema, required, defaults) pass.
  - **Process isolation.** A forwarded call and `--capabilities` now load `Morris.Roslynk.dll` for the request types.
    `ProcessIsolationTests` now forbids `Microsoft.CodeAnalysis*`, `Microsoft.Build*`, `ModelContextProtocol*` and `OpenTelemetry*`
    instead of the engine assembly, and passes. No Roslyn, MSBuild, MCP SDK or OTel loads in the client.
- **508-018.** `.UseProblemRendererForErrors()` is on the builder.
  - MCP: a missing, unknown or mistyped argument returns `isError` with `error=Invalid\nerrorMessage=...` (new tests).
  - CLI: an option value or `--json-args` member that does not bind prints the `error=Invalid` outline on stderr.
  - **Exit code decision: CLI binding errors keep exit 1.** That keeps "the call never reached a tool" apart from the engine's
    `Invalid` (2). A missing required argv option still does not match the route, so it gets Nuru's usage text, exit 1.
    Documented in the README and AGENTS.md exit tables. A new `ServedCliTests` theory covers both kinds.
- **508-017.** The default socket is `roslynk-<8 hex>.sock` per build. The README and AGENTS.md cover:
  - two builds run side by side (`dnx Roslynk -- stdio` beside a global `roslynk`);
  - `serve --status` shows the build and lists other builds' servers;
  - two builds on one shared `NURU_DAEMON_SOCKET` (the Aspire AppHost) are refused: one line, exit 69 through
    `RoslynkForwarder`, and the other build's server keeps running.

  Smoke check (Debug and Release builds of this branch): default sockets `roslynk-9ab7862d.sock` and `roslynk-7982b1c8.sock`.
  On a shared socket, the Release call printed Nuru's "is another build ... It was left running" line and exited 69, and the
  Debug server kept answering.
- **508-007 re-measure.** Release builds, interleaved A/B against a Release build of `origin/master` (beta.4, linked
  sources), on the same machine with load about 3–5. Medians of 7 runs after one warm-up:

  | Command | master | 006 |
  | --- | --- | --- |
  | `--capabilities` (cold) | 240 ms | 233 ms |
  | forwarded `get_symbol` | 218 ms | 215 ms |
  | forwarded `get_solution_status` | 385 ms | 373 ms |
  | 8 parallel forwarded `get_symbol` (wall) | 279, 282, 298 ms | 267, 293, 302 ms |

  The difference is within noise. `RoslynkEngine` was already lazy inside, so first-use DI does not change the
  numbers. The Release build has 0 warnings: no NURU056. The `RoslynkEngine` remarks and AGENTS.md now describe first-use
  construction.
- **Diagnostics cancel in the queue.** `RoslynInstance.RequestDiagnosticsAsync` registers the token to cancel the
  returned task. `RunDiagnosticsAsync` skips an item whose completion is already done, so it does not compile and does not
  set the model to Loading.
  - Test: `WhenADiagnosticsBuildIsCancelledBehindALongWrite_ThenTheCallerStopsWaitingAndTheBuildIsSkipped` holds a write open
    with a gate, cancels the queued build, sees `OperationCanceledException` while the write still runs, then sees no compile
    after the write drains.
  - Mutation check: without the registration, the test fails with `TimeoutException`.
- **Windows detached server, verified on Windows** (the Release build run by Windows `dotnet.exe` 10 from WSL via `powershell.exe`).
  - Self-start, forwarded calls and `serve --status`/`--stop` work over AF_UNIX on Windows.
  - The server keeps running after the client and the console that started it exit.
  - It does **not** survive a kill-on-close job object: with the client in such a job, the server was gone once the job's
    owner exited.
  - From the code (`nuru-daemon-launcher.cs`): on Windows the server is a plain redirected child, with no `DETACHED_PROCESS`,
    no `CREATE_NEW_PROCESS_GROUP` and no `CREATE_BREAKAWAY_FROM_JOB`. So it shares the client's job, and a Ctrl+C in the
    client's console probably reaches it too (not tested).

  README notes the job-object limit and that the Windows-service hosting is gone. Not filed: per the cockpit scope, Nuru
  tasks are Steven's to launch. Suggested filing (from the nuru origin-home):
  `ganda kanban create "Daemon launcher: detach the served-app server on Windows (new process group, breakaway from a kill-on-close job, no console)"`.
- **Stale text.** `Source/TestFixtures/Benchmarks/Benchmarks.md` describes `roslynk stdio` (JSON-RPC lines on stdin/stdout)
  instead of HTTP/SSE at `127.0.0.1:6502`.
- **Audit (boy-scout).** `ganda repo audit` failed `workflow-file` (no `pull_request` trigger) and `vscode-window-icon`.
  `ganda repo audit --fix` added `pull_request: branches: ['feature/**']` to `.github/workflows/workflow.yml` and wrote
  `.vscode/` (git-ignored). The audit passes all checks. Note: the workflow's pack and push steps are not gated to tags, so a
  PR into a `feature/**` branch would run them (pack fails first on a non-version ref name). PRs into `master` do not trigger it.
- `releases.md`: the Unreleased `(Fixes #8)` bullet now covers per-build sockets, binding errors as `error=Invalid`,
  diagnostics cancel, the dropped Windows-service hosting and linked sources.

- **PR #10 review fix pass.**
  - `.github/workflows/workflow.yml`: `Derive version from tag`, `Pack`, `NuGet login` and `Push` carry
    `if: startsWith(github.ref, 'refs/tags/')`, so a PR build into `feature/**` runs the tests only (no
    `VERSION=<n>/merge` pack, no OIDC login). The header comment says so. The `pull_request` trigger stays; the audit
    passes `workflow-file`.
  - `ProcessIsolationTests.WhenTheCliRuns_ThenNoRoslynOrMSBuildAssemblyIsLoaded` is a theory over `--capabilities` and
    all 29 operation routes. Each call sends its required members through `--json-args` against the served fixture
    (`checkOnly` set on writes), must bind and be forwarded (exit 0 or a problem 2 to 10, never 1 or 69), loads
    `TimeWarp.Nuru.Daemon`, and loads no `Microsoft.CodeAnalysis*`, `Microsoft.Build*`, `ModelContextProtocol*` or
    `OpenTelemetry*`. All 30 pass.
  - `RoslynInstance.RequestDiagnosticsAsync` registers the token before queueing as before; a cancellable call returns
    `AwaitThenUnregisterAsync`, which disposes the registration in a `using` once the completion finishes. The
    cancel-behind-a-long-write test passes.

### Tests

- PR #10 fix pass: `dotnet test Source/App/Morris.Roslynk.Cli.Tests`: 315 passed, 0 failed (the isolation theory grew
  from 2 to 30 cases). `Morris.Roslynk.Tests` Lifecycle and Diagnostics: 86 passed. Release build: 0 warnings. Audit passes.
- `dotnet test Source/App/Morris.Roslynk.Cli.Tests`: 287 passed, 0 failed. New: 2 MCP binding-error tests, the CLI
  binding-error theory (2 cases), and the reworked `HandlerTests` caller-path theory (29 cases).
- `dotnet test Source/App/Morris.Roslynk.Tests`: 840 passed, 15 failed. All 15 are Razor tests (`RazorSourceToolTests`,
  `RazorRenameTests`, Razor `ChangeSignatureTests`, `GetExpressionInfo` in Razor, `RazorGenerationTests`). The same 15 fail
  on an `origin/master` copy (`--filter Razor`: 15 failed, 62 passed), so this branch did not cause them. They are not
  waived or quarantined. If PR CI goes red, it is on these.
- `dotnet build Source/Morris.Roslynk.slnx -c Release`: 0 warnings, 0 errors.
- `ganda repo audit`: passes.
- Round-2 fix pass (docs only, AGENTS.md): `grep -n ToEngine AGENTS.md` prints nothing.

### How to validate

Smoke:

```bash
cd Source && dotnet build Morris.Roslynk.slnx && cd ..
R=Source/App/Morris.Roslynk.Cli/bin/Debug/net10.0/roslynk
S=$PWD/Source/TestFixtures/SimpleSolution/SimpleSolution.slnx
$R --capabilities | grep -c '"pattern"'
$R open_solution --solutionPath $S; sleep 8
$R get_symbol --solutionId $S --symbolName Calculator
$R get_members --solutionId $S --typeName Calculator --startItem first; echo "exit=$?"
$R get_symbol --json-args "{\"solutionId\":\"$S\",\"symbolName\":\"x\",\"bogus\":1}"; echo "exit=$?"
$R serve --status
grep -n "Compile Include\|Aliases" Source/App/Morris.Roslynk.Cli/Morris.Roslynk.Cli.csproj
dotnet test Source/App/Morris.Roslynk.Cli.Tests/Morris.Roslynk.Cli.Tests.csproj
dotnet test Source/App/Morris.Roslynk.Tests/Morris.Roslynk.Tests.csproj --filter "FullyQualifiedName~RoslynInstanceTests"
$R serve --stop
grep -n "if: startsWith" .github/workflows/workflow.yml
```

Expect:

- `--capabilities` lists 30 patterns (29 operations plus `stdio`).
- `get_symbol` prints `project=SimpleLibrary` / `path=SimpleLibrary/Calculator.cs`, exit 0.
- The `--startItem first` call writes `error=Invalid` / `errorMessage=Invalid value 'first' for option '--startItem'. Expected: int`
  to stderr, `exit=1`. The `--json-args` call writes `error=Invalid` / `errorMessage=Unknown key 'bogus' ...`, `exit=1`.
- `serve --status` names this build and a socket `.../roslynk-<8 hex>.sock`.
- The csproj grep prints nothing: no linked sources and no alias.
- The workflow grep prints four `if: startsWith(github.ref, 'refs/tags/')` lines (version, pack, login, push).
- CLI tests: 315 passed (includes 30 `ProcessIsolationTests` route cases). RoslynInstanceTests: 11 passed, including the diagnostics-cancel test.

### Review

Three rounds, roster general. Round 1 was effort 3 (`review/round-1/`). Round 2 was effort 1 (`review/round-2/`), scoped to `4b55243`. Round 3 was effort 3 (`review/round-3/`), scoped to the branch with the re-review focused on `61c7dc8`.

Final counts (round 3 ledger): bug 0 open / 0 fixed / 0 wontfix; suggestion 0 open / 1 fixed / 0 wontfix; nit 0 open / 0 fixed / 0 wontfix.

Disposition: clean (`review/disposition.md`). M1 (stale `OperationHandler.ToEngine` and an app contract copy in AGENTS.md) stays fixed. Round 3 found no new defects: the publish steps run only on tags, the isolation theory covers every operation route, and the diagnostics cancellation registration is disposed with the awaited completion.

Paths: `review/review-framework.md`, `review/round-3/merged.md`, `review/disposition.md`.

## Learnings

- Misread: the Goal restatement's 508-013 bullet only requires `[assembly: NuruEndpointAssembly]` and says routes, groups, descriptions, defaults, and the MCP schema stay the same. The inbound checklist (before the implementer rewrote it) also said the engine gets `[assembly: TimeWarp.Mediator.MediatorAssembly]`, with "confirm". The restatement drops that attribute. The implementation follows the restatement and records why the attribute was not applied (handlers live in the app; the CLI test project reported TWM001). The "confirm" wording makes that deviation the check the checklist asked for, so it is not filed as a defect.
- The restatement also says defaults stay the same. The original checklist asked for routes, groups, descriptions, and schema unchanged, not for the `--capabilities` `defaultValue` text. Results record that 18 nullable options no longer publish `"defaultValue": "null"` while the MCP schema is unchanged. That is inside the original schema requirement and outside the restatement's stricter "defaults" sentence. Not filed: the published schema is what the task required, and the null default string was Nuru reading `[DefaultValue(null)]` as no default.
