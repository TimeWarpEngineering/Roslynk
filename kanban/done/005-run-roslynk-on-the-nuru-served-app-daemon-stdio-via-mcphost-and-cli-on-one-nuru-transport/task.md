# Run Roslynk on the Nuru served app: daemon, stdio via McpHost and CLI on one Nuru transport

## Description

Fork issue #8 (release note bullet ends `(Fixes #8)`).

Decision (Steven, 2026-10-20): the fork aims for the cleanest architecture and is the proving ground for
TimeWarp.Nuru's served-app design. Byte-identical MCP schema and text are **no longer a constraint**; MCP
clients are models that read `tools/list` on every connect. Upstream (Pete) cherry-picks later.

Today (after 003, 003-001, 004): `roslynk` is one Nuru app from `Morris.Roslynk.Cli`, packed as the `Roslynk`
tool. Its 29 operation routes forward to a hand-written ASP.NET MCP-over-HTTP daemon (`RoslynkDaemon.Run` in
`Morris.Roslynk.Mcp`, `WithRoslynkTools`, loopback port 6502) through `ForwardingHandler` + `DaemonForwarder` +
`DaemonStarter`; `roslynk stdio` is the old stdio-to-HTTP bridge; `VerbatimTerminal` rewrites Nuru's output.

Target:

- **Daemon:** the Nuru served app (`TimeWarp.Nuru.Daemon`, `AddServeRoute`) runs the real handlers in the
  daemon process: 29 thin handlers in a host project (not the engine) that adapt each operation request to its
  engine core (pinned `SolutionModel`, `RoslynInstance`, `ResponseBudget` where the core takes one), registered
  through the generated mediator. The engine stays free of Nuru. Instance idle eviction maps onto (or stays
  beside) Nuru's idle timeout; record which.
- **MCP:** `roslynk stdio` is `TimeWarp.Nuru.McpHost`, whose tool calls forward to the daemon through the
  app's `RouteForwarder` (508-004/005 already do this), so all MCP sessions and CLI calls share one warm
  workspace.
- **CLI:** operation routes forward through the Nuru forwarder. Delete `ForwardingHandler`, `DaemonForwarder`,
  `DaemonStarter`/`DaemonStartInfo`, `VerbatimTerminal` (508-011), the ModelContextProtocol client reference,
  and the ASP.NET host (`Morris.Roslynk.Mcp`, `WithRoslynkTools`, `LoopbackOnlyExtensions`, port config), unless
  the Aspire AppHost needs a shim; record what the AppHost runs now.
- **MCP surface:** whatever McpHost derives from the contracts. Server instructions (`McpServerRegistration
  .Configure`) and tool descriptions carry over through McpHost's options. Update the tool descriptions and
  skill where the text or error format changed.

## Requirements

1. Concurrency: two MCP sessions and a CLI call run concurrently against one warm solution; a long
   `get_diagnostics` does not block a concurrent `get_symbol`; `multi_query` still runs its operations in
   parallel. (Needs nuru 508-016.)
2. Output: success text and the `error=` outline for problems are what the tool descriptions document
   (needs nuru 508-011); exit-code table unchanged for the CLI.
3. Schema: `multi_query.operations` and `apply_patch.baseVersions` publish real JSON schema; write tools carry
   `destructiveHint`, all tools `openWorldHint: false` and a title (needs nuru 508-015). Replace the pinned MCP
   schema tests with tests on names, descriptions, required members, annotations and behaviour; regenerate
   any snapshot in the same PR.
4. Cancellation: an MCP cancel or Ctrl-C stops a long call.
5. Self-start parity: `roslynk stdio` or any CLI command with no daemon starts one; one line + exit 69 when it
   cannot.
6. Timings against 003/004: cold `--capabilities` (~184-191 ms), warm forwarded `get_symbol` (~359 ms median of
   five), plus 8 parallel forwarded calls. The Nuru socket transport should beat MCP-over-HTTP; record it.
7. Packaging: the `Roslynk` tool package no longer carries ASP.NET or the Mcp launcher leftovers flagged in the
   PR #6 review; record the size against 16.7 MB.
8. Exit code 1 ambiguity from the PR #5 review: give an unknown daemon `error=` code its own exit code, distinct
   from a usage error; document it.
9. Docs: README (install, MCP config is still `roslynk stdio`), AGENTS.md (hosting and CLI sections, repository
   map, "Hosting and release touchpoints"), SKILL.md, `releases.md` Unreleased bullet `(Fixes #8)`.

## Goal (implementer restatement)

Replace Roslynk's hand-written transport with TimeWarp.Nuru's served app, so one `roslynk` Nuru app is the
CLI, the daemon and the MCP stdio server, and all three share one warm engine through Nuru's Unix-socket
forwarder:

- `roslynk serve` (Nuru Daemon, `AddServeRoute(autoStart: true)`) runs the real operation handlers. The 29
  handlers live in the `roslynk` app project and call the engine's handler/core with the pinned model,
  instance and budget. The engine gains no Nuru reference.
- `roslynk stdio` is a local-only route that runs `NuruMcpServer.RunStdioAsync` with Roslynk's server
  instructions and per-tool annotations (commands destructive, every tool `openWorldHint: false`, a title
  from `[DisplayName]`). Its tool calls forward to the daemon.
- Every operation route forwards; a missing daemon is started; when it cannot be started or stops
  answering, the call prints one line and exits 69.
- Deleted: `ForwardingHandler`/`DaemonForwarder`/`DaemonStarter`/`DaemonStartInfo`/`CliSettings`,
  `VerbatimTerminal` (replaced by `UseVerbatimRendererOutput()` + a `SharedProblemDetails` renderer),
  `Morris.Roslynk.Mcp` (ASP.NET host, loopback port, `WithRoslynkTools`), and the MCP client reference.

Acceptance:

1. Two MCP sessions and a CLI call share one warm solution concurrently; a long `get_diagnostics` does not
   block `get_symbol`; `multi_query` still runs in parallel (tests).
2. Success text and the `error=` outline are what the descriptions document; CLI exit codes keep the table,
   and an unknown error code gets its own exit code distinct from usage (1).
3. MCP schema: `multi_query.operations` and `apply_patch.baseVersions` publish real JSON schema; write tools
   have `destructiveHint: true`, all tools `openWorldHint: false` and a title. Pinned schema tests replaced by
   tests on names, descriptions, required members, annotations and behaviour.
4. An MCP cancel or a client going away cancels the call's token in the daemon.
5. Self-start: `roslynk stdio` or any operation with no daemon starts one; one line + exit 69 when it cannot.
6. Timings (cold `--capabilities`, warm forwarded `get_symbol` median of five, 8 parallel) and the
   package size against 16.7 MB recorded in Results.
7. README, AGENTS.md, SKILL.md, tool descriptions and a `releases.md` Unreleased bullet `(Fixes #8)`.
8. `ganda repo audit` passes; build and tests pass.

## Non-goals

- Engine behaviour changes; new tools.
- Upstream PR to Pete.
- Releasing a package (the `Roslynk` NuGet id is Pete's; a fork release is a separate decision).

## Checklist

- [x] Pins bumped to TimeWarp.Nuru 4.0.0-beta.4 (carries 508-011, 508-015, 508-016); Daemon and McpHost packages added
- [x] Host-project handlers adapting the 29 operations to their engine cores
- [x] Daemon on the Nuru served app; idle eviction decision recorded
- [x] `stdio` on McpHost forwarding to the daemon; server instructions carried over
- [x] CLI on the Nuru forwarder; hand-written forwarding, daemon start, verbatim terminal and ASP.NET host deleted
- [x] Tests per requirements 1-5 and 8; MCP schema tests rewritten
- [x] Timings and package size in Results
- [x] Docs and `(Fixes #8)` bullet

## Session

- Created: 2532391 (2026-10-20) by the Roslynk cockpit after Steven's decision to move onto the Nuru served app.
- 2026-10-20 cockpit (2532391): **unblocked**. TimeWarp.Nuru **4.0.0-beta.4** (all six packages, indexed) ships
  508-011, 508-015, 508-016 (and 508-009, nested enums keep allowed values). Contracts stays 2.0.0-beta.23. APIs to use
  (from the nuru `kanban/done/508-01x` Results):
  - **508-011:** `UseVerbatimRendererOutput()` on the builder writes renderer text with `Write` (no added newline)
    and problem-renderer text to stderr. The problem renderer is `IResponseRenderer<SharedProblemDetails>` (the
    contracts interface): implement one that emits today's `error=` outline (`ProblemOutline.Format`) and register it.
    Exit code still follows `SharedProblemDetails.Status`; map the error codes to the documented table there.
    Then delete `VerbatimTerminal`.
  - **508-016:** the daemon and McpHost run calls concurrently by default with a per-call `NuruCallContext`
    (terminal, working directory, exit code). Use `NuruCallContext.GetFullPath` / `CurrentDirectory` for any
    client-relative path (e.g. `open_solution` with a relative path), never `Environment.CurrentDirectory`.
    `[Exclusive]` / `.Exclusive()` exists for routes that must serialize; Roslynk should not need it (the engine
    serializes writes itself). The DI-injected `ITerminal` follows the current call.
  - **508-015:** non-scalar members publish real JSON schema from the binder's type info; defaults appear as both
    JSON `default` and a `Default: <json>.` description sentence. Annotations: McpHost reads `[DisplayName]` (BCL,
    fine on engine requests) for `title`, and Nuru's `[Destructive]` / `[OpenWorld]` from the request class. **Do not
    put Nuru attributes on the engine's request classes** (the engine stays free of Nuru); set `destructiveHint` /
    `openWorldHint` through `NuruMcpHostOptions.ToolAnnotations[toolName]` in the host, derived from the operation
    kind (commands destructive, all tools `openWorld: false`). timewarp-architecture 391 asks Contracts for a
    neutral home for these hints; switch to it when it ships.
  - Pins: `TimeWarp.Nuru` 4.0.0-beta.4 in both props files, `TimeWarp.Nuru.DevCli` 4.0.0-beta.4 in the root, add
    `TimeWarp.Nuru.Daemon` and `TimeWarp.Nuru.McpHost` 4.0.0-beta.4 to `Source/Directory.Packages.props`.
- 2026-10-20 implementer (ganda task work, claude): implemented on the task branch; see Results. Boy-scout:
  `ganda repo audit --fix` completed the VS Code window-icon config (`.vscode/` is gitignored, so nothing to commit).
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-20T14:51:14Z
- Review oracle: general (grok, session 01a159cb-9046-7b22-8142-6a8616635df2), effort 3, round 1, clean — 2026-10-20
- Review oracle confirming pass: general (grok, session 01a15a51-c032-7242-b7a3-08bfbe9e16d4), effort 3, round 1 left in place, clean — 2026-10-21. Re-read the handlers, forwarder, stdio host, exit codes, and TimeWarp.Nuru 4.0.0-beta.4 daemon client and McpHost against the original task and the implementer restatement. No misread. The CLI suite was not re-run on this pass.
- Review oracle: review by reviewer-grok (grok, model grok-4.7), session not reported, max-turns 200 — 2026-10-20T17:29:06Z

## Review

- Round 1: Done

## Notes

- Was blocked on timewarp-nuru 508-011, 508-015 and 508-016; all released in TimeWarp.Nuru 4.0.0-beta.4 (2026-10-20).
- Supersedes the "revisit McpHost" note in Roslynk 004 (PR #6) and the transport section of AGENTS.md "CLI
  front end".
- Related: Roslynk 003 (PR #5), 003-001 (PR #7), 004 (PR #6); nuru 508-004, 508-005, 508-010, 508-013.

## Results

### What changed

- **One Nuru app.** `roslynk` (`Morris.Roslynk.Cli`) is the CLI, the served daemon and the MCP server:
  `UseVerbatimRendererOutput()`, `DiscoverEndpoints()`, `AddServeRoute(idleTimeoutSeconds: 3600, autoStart: true)`,
  and a local-only `stdio` endpoint class. The engine stays free of Nuru (no Nuru attribute on its contracts).
- **Handlers.** `Handlers/OperationHandlers.cs`: 29 thin handlers. Each copies its request onto the engine's copy
  of the contract type (`OperationHandler.ToEngine`, JSON; a relative `solutionId`/`solutionPath` is resolved
  with `NuruCallContext.GetFullPath`) and calls the engine tool's `HandleAsync`: reads with the pinned
  `SolutionModel`/`RoslynInstance`/`ResponseBudget`, the rest with the request and the call's token. The outline
  comes back as the outcome: `error=` becomes `SharedProblemDetails` with the exit code as `Status`; success over
  the budget is cut with `ResponseBudget.Fit` (not multi_query). Escaped exceptions are `Invalid`/`Faulted`
  (`ToolErrors.ToError`, moved out of the deleted `RoslynkTool`). Each call is a `Morris.Roslynk` span named after
  its operation. The engine grants `InternalsVisibleTo roslynk`; the app references it under the `engine` alias.
- **Engine host.** `Hosting/RoslynkEngine.cs` (Nuru singleton) composes `AddRoslynk` plus the 29 tools on first
  use, so a forwarded call or `--capabilities` never loads Roslyn (`ProcessIsolationTests`).
  **Idle eviction decision:** kept beside Nuru's idle stop, not mapped onto it. Solutions idle for
  `Roslynk:IdleEvictionMinutes` (30) close, checked every `Roslynk:EvictionCheckMinutes` (5); the process stops
  after 3600 s with no call. One busy solution no longer keeps the others alive, and the process stop never
  drops anything eviction would have kept. OTLP export of the engine's traces/metrics when
  `OTEL_EXPORTER_OTLP_ENDPOINT` is set (replaces the ASP.NET host's observability).
- **Forwarding.** `Hosting/RoslynkForwarder.cs` wraps Nuru's `NuruDaemonClient`: every call is forced to the
  server (Nuru alone runs an operation locally when an auto start fails), and a failure with no server of this
  build answering becomes exit 69 with one line. `--no-daemon` still runs a call in-process.
- **MCP.** `Hosting/StdioCommand.cs`: probes (starting the server, warming its engine), then serves Nuru McpHost
  over the catalog minus `stdio`, with `McpServerRegistration.Instructions` (moved from the deleted host; the
  failures paragraph now says error results carry the outline) and per-tool `ToolAnnotations`
  (`destructiveHint` for commands, `openWorldHint: false` for all). Titles are `[DisplayName]` on the 29 request
  classes (equal to the engine's `[McpServerTool(Title)]`, pinned by `OperationContractTests`).
  `multi_query`'s `tool`/`arguments` and `apply_patch`'s `path`/`version` keep lowercase JSON names.
- **Exit codes.** New `70` for an `error=` code `ExitCodes` does not know; `1` is only Nuru's usage/`--json-args`
  errors (requirement 8). `69` is server unavailable. README and AGENTS.md tables updated.
- **Deleted.** `Morris.Roslynk.Mcp` (ASP.NET host, `LoopbackOnlyExtensions`, port 6502, `StdioBridge`,
  `DaemonLauncher`, `IdleEvictionService`, Windows-service hook), `Morris.Roslynk.McpTests`, `WithRoslynkTools`
  and `RoslynkTool`, the CLI's `Forwarding/` (`ForwardingHandler`, `DaemonForwarder`, `DaemonStarter`,
  `DaemonStartInfo`, `CliSettings`, `ForwardedCall`), `Output/VerbatimTerminal`, `Hosting/HostEntry`, the
  `daemon` route and bare-command daemon, and the CLI's ModelContextProtocol client reference. Pins for
  ModelContextProtocol.AspNetCore, Hosting.WindowsServices and OpenTelemetry ASP.NET instrumentation removed.
- **Engine plumbing (no behaviour change):** `get_diagnostics` and `multi_query` now pass the caller's
  cancellation token to the engine (requirement 4).
- **AppHost** runs `roslynk serve` (`AddProject<Morris_Roslynk_Cli>().WithArgs("serve")`) with
  `NURU_DAEMON_SOCKET=roslynk-apphost.sock`, so an installed roslynk's server (another build) is left alone.
  `installer/run.sh` runs `dotnet run ... -- serve`.

### Tests

- `Morris.Roslynk.Cli.Tests`: 283/283 (Release, three runs). New: `McpSurfaceTests` (tool names, descriptions,
  titles, annotations, properties/required, defaults, `multi_query`/`apply_patch` JSON schema, instructions,
  impact recipe, success/problem/bind-error/enum-rejection behaviour), `ServedCliTests` (stdout/stderr bytes,
  exit 0/1/2/3, `--json-args` batch, caller-relative paths, `--no-daemon`), `ServerLifecycleTests` (CLI and
  stdio self-start; one line + exit 69 for CLI and stdio when no server can start), `ConcurrencyTests` (two
  sessions + a CLI call on one server; a cold `get_diagnostics` does not block `get_symbol`), `CancellationTests`
  (MCP cancel and a killed CLI client leave the server serving), `HandlerTests` (all 29 requests copy member for
  member, exit-code mapping incl. 70, budget cut, cancellation reaches the engine for read/diagnostics/batch,
  problem renderer), rewritten `CapabilitiesContractTests` and `ProcessIsolationTests` (no ASP.NET in deps or
  runtimeconfig). Old pinned schema tests (McpTests) and stub/HTTP-daemon tests are gone.
- `Morris.Roslynk.Tests`: 839 passed, 15 failed: the same 15 pre-existing Razor failures 003/004 recorded on
  SDK 11 (`RazorSourceToolTests`, `RazorRenameTests`, Razor `ChangeSignature`, Razor `GetExpressionInfo`,
  `RazorGenerationTests`). `ToolCompositionTests` now composes the engine as the app does;
  `OperationContractTests` also pins title = `[DisplayName]`.
- `./bin/dev build`: 0 warnings, 0 errors. `ganda repo audit`: passes all checks.

### Timings

Release apphost, Linux/WSL2 (32 cores, load ~3), interleaved A/B against a Release build of `master` (98812f4)
on the same machine, seven runs after one warm-up each, warm server with SimpleSolution loaded:

| Command | 005 (Nuru socket) | master (MCP over HTTP) |
| --- | --- | --- |
| `roslynk --capabilities` | 258 ms (231, 232, 248, 258, 273, 285, 306) | 229 ms (206, 210, 229, 226, 265, 296, 288) |
| `roslynk --help` | 267 ms | 299 ms |
| `roslynk get_symbol ...` (forwarded) | **250 ms** (300, 250, 225, 247, 236, 254, 291) | 399 ms (393, 362, 399, 403, 407, 381, 413) |
| `roslynk get_solution_status` (forwarded) | **291 ms** | 575 ms |
| 8 parallel forwarded `get_symbol` (wall) | **462 ms** (478, 444, 462) | 628 ms (671, 628, 496) |

The Nuru socket transport beats MCP-over-HTTP by ~150 ms per forwarded call (~37%) and ~27% for eight in
parallel. Cold `--capabilities` is within noise of master on this loaded machine (004 measured 191 ms on a quiet
one); it loads no engine or Roslyn assembly. A cold first call that starts the server: 0.72 s.

### Packaging

`dotnet pack -c Release`: `Roslynk.0.0.5-local.nupkg` is **17,177,470 bytes (17.2 MB)** against master's
16,687,988 (16.7 MB). Gone: `Morris.Roslynk.Mcp` (dll, apphost, deps.json, runtimeconfig, appsettings.json),
ModelContextProtocol.AspNetCore, OpenTelemetry ASP.NET instrumentation, Hosting.WindowsServices,
ServiceController. Added: the Microsoft.Extensions.* assemblies the ASP.NET shared framework used to supply
(~0.5 MB), Nuru Daemon and McpHost. The tool's runtimeconfig now needs only `Microsoft.NETCore.App`, not
`Microsoft.AspNetCore.App`. Installed with `dotnet tool install --tool-path`: `--capabilities` lists 30
endpoints, a forwarded call started the server, `roslynk stdio` listed 29 tools and answered `tools/call`.

### Decisions and follow-ups

- The engine keeps its `[McpServerToolType]`/`[McpServerTool]` methods and its ModelContextProtocol reference:
  engine tests and `MultiQueryCatalog` use them, and removing them is an engine refactor outside this task.
- Linked contract sources stay until timewarp-nuru 508-013 (referenced-assembly discovery).
- Hints come from `NuruMcpHostOptions.ToolAnnotations` until timewarp-architecture 391 gives them a neutral home.
- No Windows service host any more (the ASP.NET host's `AddWindowsService` went with it).

### How to validate

Smoke:

```bash
cd Source/App
dotnet build Morris.Roslynk.Cli/Morris.Roslynk.Cli.csproj
export NURU_DAEMON_SOCKET=/tmp/rk-smoke/roslynk.sock; mkdir -p -m 700 /tmp/rk-smoke
R=Morris.Roslynk.Cli/bin/Debug/net10.0/roslynk
(cd ../TestFixtures/SimpleSolution && dotnet restore -v q && ../../App/$R open_solution --solutionPath SimpleSolution.slnx)
sleep 5; $R get_symbol --solutionId $PWD/../TestFixtures/SimpleSolution/SimpleSolution.slnx --symbolName SimpleLibrary.Calculator; echo "exit=$?"
$R get_symbol --solutionId $PWD/../TestFixtures/SimpleSolution/SimpleSolution.slnx --symbolName Nope; echo "exit=$?"
$R serve --status; $R serve --stop
chmod 777 /tmp/rk-smoke; $R get_solution_status; echo "exit=$?"
dotnet test Morris.Roslynk.Cli.Tests/Morris.Roslynk.Cli.Tests.csproj
```

Expect:

- `open_solution` prints `solutionId=<absolute path>` (the relative path resolved against the caller's directory)
  and starts the server; `serve --status` reports `roslynk server is running: pid ...`.
- `get_symbol` prints `project=SimpleLibrary`, `path=SimpleLibrary/Calculator.cs`, `loc=...`, a blank line and
  `public class Calculator`, then `exit=0`; `Nope` writes `error=NotFound` / `errorMessage=...` to stderr and
  `exit=3`.
- With the socket directory world-writable, `get_solution_status` prints one `Error: ...` line and `exit=69`.
- The test run reports `Passed!  - Failed: 0, Passed: 283`.

### Review

- Round 1, roster general, effort 3. Disposition: **clean** (0 open; no wontfix).
- The implementer restatement matches the original task. No misread.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.
- This round re-ran `HandlerTests` (42 passed). The full CLI suite was not re-run here.
- 2026-10-21 confirming pass (session 01a15a51-c032-7242-b7a3-08bfbe9e16d4) left round 1 and the clean disposition in place. The implementer restatement matches the original task (no misread). Checked again: one `roslynk` app forwards every operation through `RoslynkForwarder` onto `roslynk serve`, `stdio` publishes the 29 tools through McpHost onto that same forwarder, exit 69 and 70 are distinct from usage, and the built runtimeconfig asks only for `Microsoft.NETCore.App`. The suite was not re-run on this pass.
