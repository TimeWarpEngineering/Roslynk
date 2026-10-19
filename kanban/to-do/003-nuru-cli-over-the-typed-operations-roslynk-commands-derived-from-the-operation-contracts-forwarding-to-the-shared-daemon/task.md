# Nuru CLI over the typed operations: roslynk commands derived from the [Operation] contracts, forwarding to the shared daemon

## Description

Give Roslynk a command-line front end whose commands are derived by TimeWarp.Nuru 4.0.0-beta.1 from the 29 `[Operation]` contracts that task 002 (PR #3, merged ed7282d) put behind every MCP tool. Each command forwards to the shared Roslynk daemon over its existing loopback MCP transport, with the same tool name and the same arguments an MCP `tools/call` would carry, so one warm workspace serves CLI and MCP alike and the text an agent reads is byte-identical to the MCP text.

This is the "Roslynk CLI with `[Operation]`" step of the contracts-first plan (timewarp-nuru 506 pitch, timewarp-architecture 384/387, Roslynk 002, timewarp-nuru 508-001/002/003, all merged). It comes before 508-004 (`TimeWarp.Nuru.McpHost`) and before 508-005 (the generic Nuru daemon and forwarding client), so it must not pre-empt either: the daemon stays the current `Morris.Roslynk.Mcp` host, and the forwarding is Roslynk-specific and small.

Fork GitHub issue: #4. The `releases.md` bullet ends with `(Fixes #4)`.

### Why forward instead of running the engine in the CLI process

Task 506 measured cold load plus first compile at about 6 s and an uncached warm call at about 26 ms. A process per command cannot keep the workspace warm. The daemon already exists, already self-starts (`DaemonLauncher`), already speaks MCP over streamable HTTP on `Roslynk:Port` (default 6502), and already scopes solution ref-counts per MCP session. The CLI is a second doorbell beside the stdio bridge, not a second engine.

### What Nuru 4.0.0-beta.1 gives us (read `documentation/user/features/neutral-contracts.md` and `agent-invocation.md` in timewarp-nuru)

- A class carrying `[Operation]` plus `IQuery<T>` / `ICommand<T>` is a complete endpoint: route literal = operation name (`get_symbol`), one option per public settable property named exactly as declared (`--solutionId`), description from `[Description]`, required from `required` or non-nullable-without-initializer, default from the initializer. A non-nullable `bool = true` becomes a value option (`--includeAnalyzers false`), so the six Roslynk bools that default to true keep their defaults without any CLR change. Scalar arrays are repeated options; anything else (`multi_query.operations`, `apply_patch.baseVersions`) is one JSON value that must come through `--json-args`.
- The handler is any registered `IQueryHandler<,>` / `ICommandHandler<,>` for the request; a nested `Handler` is optional, and the docs name the "CLI edition registers a forwarding handler, the daemon the real one" shape explicitly.
- Success is rendered by a registered or single visible `IResponseRenderer<TResponse>`; `OneOf<TSuccess, SharedProblemDetails>` writes the problem to stderr and exits with `Status` when it is 1..255, otherwise 1.
- `--capabilities` is the flat catalog; `--group-filter` matches `groupPath`, which comes from a containing `[NuruRouteGroup]` or `[NuruCliBinding(Group = "...")]`. `--json-args` (`-`, `@path`, inline) binds a JSON object by the same property names.

### Constraint the implementer must design around

The Nuru generator discovers `[Operation]` endpoints with `ForAttributeWithMetadataName` over the **compiling project's own syntax** (`source/timewarp-nuru-analyzers/generators/nuru-generator.cs`), while handlers and renderers are also collected from referenced assemblies. An operation class that lives only in a referenced `Morris.Roslynk.dll` is therefore not a route. The 29 operation files depend on `IOutlineResponse`, `ParameterDescriptions`, `ProblemOutline` and (two of them) small types under `Infrastructure/Outlines`; none of them touches Roslyn.

Recommended shape, open to a better one that meets the requirements: a new console project `Source/App/Morris.Roslynk.Cli` that does **not** reference `Morris.Roslynk` or `Morris.Roslynk.Mcp`, and instead compiles the operation files and their closure as linked sources (`<Compile Include="../Morris.Roslynk/Features/**/*.cs" Exclude="**/*Tool.cs;..." Link="..." />` or an explicit list). Upstream files are not moved. If the closure drags in Roslyn or the engine's DI, stop and split the offending type rather than widen the reference. The CLI references `TimeWarp.Nuru` (which brings `TimeWarp.Contracts` 2.0.0-beta.21 and the mediator contracts) and `ModelContextProtocol` (client only). Cold start of `roslynk-cli --capabilities` must be measured and reported.

Dispatch: one generic forwarding handler (`ForwardingHandler<TRequest, TResponse>`) closed per operation, registered however Nuru requires so that every one of the 29 routes resolves a handler at build time (no NURU_H008 left in the build, which treats warnings as errors). Whether that is source-generated DI with 29 explicit registrations, `UseMicrosoftDependencyInjection()` plus a TimeWarp.Mediator registration, or a nested `Handler` emitted per operation, is the implementer's call; record it in Results with the measured per-call overhead.

Forwarding: resolve the port the way `StdioBridge.ResolvePort` does (appsettings beside the executable, then environment, `Roslynk:Port`), ensure the daemon is up the way `DaemonLauncher.EnsureRunningAsync` does (the CLI is a different executable, so starting a missing daemon must find the installed `roslynk` tool or the sibling `Morris.Roslynk.Mcp` build; if neither is found, fail with one line that says how to start it), open one `McpClient` over streamable HTTP, call `tools/call` with the operation name and the request's properties serialized under their declared names (omit properties left at their initializer so the daemon applies its own defaults), and dispose the client on exit so the daemon's session ref-counts release. The text content of the result is the outcome: `ProblemOutline.TryParse` splits `error=` text into `SharedProblemDetails`, anything else is the response.

Output contract: stdout for a success is exactly the MCP text (no added newline, no colour, no banner). A problem goes to stderr. Decide and document the exit-code table per Roslynk error code (`Invalid`, `NotFound`, `Ambiguous`, `Indexing`, `Stale`, `Conflict`, `NotSupported`, `Faulted`, transport failure); problems must carry that code in `Status` so Nuru's own problem path produces it. Stdout for a problem is empty.

Groups: give the 29 routes group paths so `--group-filter` can cut the catalog the way 506 asked. Proposed groups: `symbols` (get_symbol, get_members, get_symbol_body, find_definition, search_symbols, get_expression_info, get_type_hierarchy, find_implementations), `references` (find_references, get_callers, get_callees, find_reads, find_writes, find_dead_code, find_dead_conditionals), `solution` (open_solution, get_solution_status, reload_solution, get_diagnostics, get_code_actions), `edit` (apply_code_action, apply_code_fix, extract_method, change_signature, rename_symbol, rename_parameter, remove_unused_usings, apply_patch), `batch` (multi_query). The group attribute is a Nuru attribute, so putting it on the request classes makes the engine reference TimeWarp.Nuru transitively. Decision for Steven, default if unanswered: keep the engine free of Nuru by declaring the groups in the CLI project only (a per-group `[NuruRouteGroup]` marker that the linked operation classes cannot inherit means this needs a CLI-side mapping; if Nuru offers no way to group a linked endpoint from outside its declaration, file a nuru follow-up asking for `Group` on `TimeWarp.Contracts.CliOperationBinding` and ship this task ungrouped with the follow-up id in Results).

## Requirements

1. `roslynk-cli --capabilities` lists exactly 29 endpoints whose patterns are the 29 MCP tool names, whose option names are the request property identifiers, and whose descriptions equal the MCP tool and parameter descriptions. A test asserts this against the published MCP schema the McpTests already pin (same names, same descriptions, same defaults, same required set).
2. Every one of the 29 routes resolves a handler: the build has no NURU_H008, and a test invokes each route against a fake daemon (an in-process MCP server over `WithRoslynkTools()`, or a stub `tools/call` server) and asserts the forwarded tool name and argument set equal what the MCP wrapper would receive. The two JSON-only members go through `--json-args` and arrive as the same JSON the MCP server accepts.
3. Byte-identical text: for a success the CLI stdout equals the MCP text content; for a problem the stderr carries the same `error=` outline text and the exit code follows the documented table. Tests cover one success, one `error=` problem, one `Stale` from a write tool, and a daemon-unreachable failure.
4. The daemon is not modified except where a minimal seam is unavoidable; if one is needed, it is additive, documented in `AGENTS.md`, and the McpTests still pass unchanged.
5. The CLI process does not load Roslyn. Verify by assembly list at exit (or a startup test) and report the cold start of `--capabilities` and of one forwarded call against a warm daemon, median of five.
6. `bin/dev build` and `bin/dev test` cover the new project; `ganda repo audit` passes; the solution file includes the project. CI (`.github/workflows/workflow.yml`) builds it but packaging of the CLI as a tool command is **out of scope** here (the `roslynk` command name stays the MCP host's). Record the packaging options in Results for the host-unification task that follows 508-004.
7. Documentation: `README.md` gains a short CLI section (install/run, `--capabilities`, `--group-filter`, `--json-args`); `skills/roslynk/SKILL.md` is **not** rewritten for the CLI yet (that is the one-line skill of the plan and lands with packaging); `AGENTS.md` gains a "CLI front end" subsection stating the linked-source rule, the forwarding handler, the exit-code table, and that new operations must be added to the CLI's compile list and group mapping. `releases.md` gets one Unreleased bullet `(Fixes #4)`.
8. Follow-ups filed, with ids recorded in Results: (a) timewarp-nuru: discover `[Operation]` endpoints in referenced assemblies, or an equivalent, so the linked-source workaround can go; (b) timewarp-architecture: `Group` on `CliOperationBinding` if the grouping decision above needed it; (c) Roslynk: host unification after 508-004 (`roslynk` command = Nuru app hosting McpHost; CLI forwarding moves to 508-005).

## Non-goals

- No change to the MCP surface, tool text, or daemon lifecycle. No `TimeWarp.Nuru.McpHost` (508-004) and no generic daemon client (508-005).
- No skill rewrite, no tool-package rename, no upstream restructuring under `Source/`.
- No typed response fields: responses stay the opaque outline carriers 002 introduced (review M3 there).

## Goal (implementer restatement)

Ship `Morris.Roslynk.Cli`, a Nuru 4.0.0-beta.2 console app (walk 1 targeted beta.1) whose 29 commands are generated from the
29 `[Operation]` request classes (compiled as linked sources, no engine reference, no Roslyn in the
process). Every command forwards to the running (or self-started) Roslynk daemon as one MCP
`tools/call` with the same tool name and the arguments the caller set, and prints the daemon's text
unchanged: success text to stdout byte for byte, an `error=` outline to stderr with an exit code from
a documented table. The daemon and MCP surface stay untouched.

Acceptance, in my words:

- `--capabilities` shows exactly the 29 tool names, option names = request property names, same
  descriptions / defaults / required set as the published MCP schema (asserted by a test).
- Every route has a visible handler (no NURU_H008) and a per-route test proves the forwarded name and
  argument set, including `multi_query.operations` and `apply_patch.baseVersions` via `--json-args`.
- Tests for byte-identical success, an `error=` problem, a `Stale` write, and daemon unreachable.
- No Roslyn assembly is loaded; cold start of `--capabilities` and of one forwarded call measured.
- Groups: wired if Nuru can group a linked endpoint from outside its declaration, otherwise shipped
  ungrouped with follow-ups filed.
- dev-CLI root pins TimeWarp.Nuru 4.0.0-beta.1; `bin/dev build`/`test`, `ganda repo audit` green;
  project in the slnx and CI builds it. README, AGENTS.md, releases.md `(Fixes #4)` updated.
- Follow-ups filed (nuru referenced-assembly discovery, contracts `Group`, Roslynk host unification).

Fix pass for review round 1 (M1), in my words: a daemon the CLI starts must listen on the port the CLI
resolved, on Windows as on Unix, including a port that exists only in the CLI's own appsettings.json.
Done when both launchers hand the resolved port to the daemon, the daemon's stdout/stderr stay out of the
CLI, a test that runs on Linux checks the Windows and Unix start info, and the existing suites still pass.

## Checklist

- [x] Root `Directory.Packages.props` (dev CLI) pins TimeWarp.Nuru 4.0.0-beta.1 and `bin/dev` still builds and runs; this is the audit failure that blocked `kanban publish` on 2026-10-19
- [x] `Source/Directory.Packages.props` and the root `Directory.Packages.props` pin TimeWarp.Nuru (and DevCli) 4.0.0-beta.2, the release that carries 508-006; `dotnet build Source/App/Morris.Roslynk.Cli` has no CS0117, CS9035 or SYSLIB1031
- [x] `Morris.Roslynk.Cli` project added to `Source/Morris.Roslynk.slnx`, linked operation sources compile without an engine reference
- [x] Forwarding handler and daemon client; port resolution and daemon start parity with `StdioBridge`/`DaemonLauncher` recorded
- [x] 29 routes in `--capabilities`, contract test against the pinned MCP schema passes
- [x] Per-route forwarding test (name + arguments), including the two `--json-args` members
- [x] Byte-identical success text, problem-to-stderr with exit-code table, Stale and daemon-unreachable cases tested
- [x] Groups decided (or follow-up filed) and `--group-filter` demonstrated (ungrouped; 508-012 + architecture 388)
- [x] No Roslyn assembly loaded in the CLI process; cold-start and forwarded-call timings recorded in Results
- [x] `bin/dev build`, `bin/dev test`, `ganda repo audit` green; CI workflow builds the project (`bin/dev test`: the 15 pre-existing Razor failures of `Morris.Roslynk.Tests` on SDK 11 remain, see Results)
- [x] README CLI section, AGENTS.md subsection, `releases.md` bullet `(Fixes #4)`
- [x] Follow-ups filed and ids recorded in Results

## Session

- Created: 2532391 (2026-10-19). `ganda kanban publish 003` was refused by the pre-sign audit (`FAIL nuru: TimeWarp.Nuru is outdated (current: 3.0.0, latest: 4.0.0-beta.1)` in the root `Directory.Packages.props`). The fix is a product change, so it is the first checklist item of this task and the walk was dispatched from the held claim without an inbox round trip.

- 2026-10-19 implementer walk 1 (Blocked):
  - Root `Directory.Packages.props`: TimeWarp.Nuru and TimeWarp.Nuru.DevCli 3.0.0 -> 4.0.0-beta.1.
    `dotnet run --file tools/dev-cli/dev.cs -- --capabilities` runs, `self-install` rebuilt `bin/dev`,
    `bin/dev build` passes. This clears the `nuru` audit failure that refused `kanban publish`.
    `ganda repo audit --fix` regenerated the (gitignored) `.vscode` window-icon files; audit passes.
  - Scaffolded `Source/App/Morris.Roslynk.Cli` (no engine reference; links `Features/**/*.cs` minus
    `*Tool.cs` and `MultiQueryCatalog.cs`, plus `IOutlineResponse`, `OutlineHeader`, `ProblemOutline`,
    `ParameterDescriptions`, `OutlineDescriptions`). The closure is Roslyn-free, as the spec expected.
    `Source/Directory.Packages.props` pins TimeWarp.Nuru 4.0.0-beta.1 and ModelContextProtocol.Core 2.2.0.
    The project is deliberately **not** in `Morris.Roslynk.slnx` yet: it does not compile (below), and
    adding it would break `bin/dev build` and CI.
  - **Blocker (upstream Nuru defect).** Nuru 4.0.0-beta.1 lowercases each `[Operation]` property name in
    the extractors and rebuilds it with `ToPascalCase` in `command-construction-emitter.cs`, so the
    generated request initializer is `new() { SolutionId = solutionid, ... }` for our camelCase
    `solutionId`: 834 CS0117 + 414 CS9035 across the argv, `--json-args` and `TryBind` paths. The
    camelCase names are a decision of this task (they are the MCP parameter names), and nothing on the
    CLI side can rename the members of linked contract classes. Filed and published as
    **timewarp-nuru 508-006** with the repro and the other findings below. Unblocks when a Nuru release
    carries that fix; bump `TimeWarp.Nuru` in `Source/Directory.Packages.props` and resume.
  - Other findings, recorded in 508-006 so one upstream pass covers them:
    - SYSLIB1031: Nuru's build task registers every `OneOf<X.Response, SharedProblemDetails>` in one
      JSON context; same-named nested `Response` types collide (58). Workaround if unfixed: `NoWarn`.
    - SYSLIB1034: `MultiQueryOp`'s non-generic `JsonStringEnumConverter` in `NuruJsonMemberContext`.
    - Output is not verbatim: renderer text goes through `WriteLine` (extra newline) and a problem prints
      `"{Type} {Title}"` + `Detail`, not the `error=` outline. Requirement 3 needs either a Nuru verbatim /
      problem-renderer hook or a CLI-side terminal that the forwarding handler writes through.
    - Groups: `groupPath` comes only from a base `[NuruRouteGroup]` or `[NuruCliBinding]` on the request
      itself, and the prefix is prepended to the pattern (`symbols get_symbol`). So the linked contracts
      cannot be grouped from the CLI, and grouping would change the route away from the tool name.
      Default from the spec applies: ship ungrouped; the follow-up asks for an app-side group mapping or
      `Group` on `CliOperationBinding` that sets `groupPath` without prefixing.
    - Nested `--json-args` members bind by declared, case-sensitive names (`Tool`, `Arguments`,
      `Path`, `Version`), while the MCP schema is camelCase (`tool`, `arguments`).
    - The handler shape that will satisfy the generator: 29 closed `sealed` subclasses of an abstract
      `ForwardingHandler<TRequest, TResponse>` (`ScanMediator` reads `AllInterfaces` of non-abstract
      classes), with `TimeWarpMediatorAssembly=true` so dispatch goes through the generated mediator.
      TWM001 / NURU_H008 in the scaffold build are only the missing handlers.
- 2026-10-19 cockpit (2532391), unblocked: timewarp-nuru 508-006 merged (PR #332) and **TimeWarp.Nuru 4.0.0-beta.2**
  is indexed on nuget.org (all six packages). It fixes the blocker (request members emitted as declared, so
  `solutionId = solutionid`), SYSLIB1031 (colliding nested `Response` types get full-name `TypeInfoPropertyName`s;
  nothing is registered when the endpoint has a renderer) and the nested `--json-args` names
  (`PropertyNameCaseInsensitive = true`, so `tool`/`arguments` bind). SYSLIB1034 is documented only: use
  `JsonStringEnumConverter<TEnum>`. The other findings are now their own nuru tasks, all to-do:
  508-011 verbatim renderer output and a problem renderer (finding 3), 508-012 groups assigned from the app
  without prefixing the route (finding 4), 508-013 discover `[Operation]` endpoints in referenced assemblies
  (finding 6). For this task that means: bump to beta.2 first; satisfy requirement 3 CLI-side (the forwarding
  handler writes the daemon text verbatim through its own terminal and renders problems to stderr itself)
  until 508-011 lands; ship ungrouped and record 508-012 as the group follow-up (the spec's nuru
  referenced-assembly follow-up is 508-013, already filed). The timewarp-architecture `Group` and Roslynk
  host-unification follow-ups are still to file.
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Blocked — 2026-10-18T19:44:08Z

- 2026-10-19 implementer walk 2 (implementer-claude, claude-opus-5-5):
  - Bumped TimeWarp.Nuru to 4.0.0-beta.2 in `Source/Directory.Packages.props` and the root (dev CLI, plus
    TimeWarp.Nuru.DevCli); `self-install` rebuilt `bin/dev` on beta.2. The CLI scaffold now compiles: no CS0117,
    CS9035 or SYSLIB1031. The remaining SYSLIB1034 came from `MultiQueryOp`'s non-generic
    `JsonStringEnumConverter`; changed to `JsonStringEnumConverter<MultiQueryOp>` in the shared contract file
    (same wire format; McpTests 30/30 and the engine's MultiQuery tests 43/43 pass unchanged).
  - Handler shape: 29 one-line `sealed` subclasses of `ForwardingQueryHandler<,>` / `ForwardingCommandHandler<,>`
    (both on an abstract `ForwardingHandler<TRequest, TResponse>`) in `Forwarding/OperationHandlers.cs`, with
    `TimeWarpMediatorAssembly=true`: the generated TimeWarp.Mediator registers each, Nuru dispatches through it,
    the handler gets the app `ITerminal` by constructor injection. No TWM001 / NURU_H008 left.
  - Verbatim output (508-011 not released): `Output/VerbatimTerminal` wraps the system terminal via
    `UseTerminal`; the handler announces the daemon text it returns and the one Nuru `WriteLine` (success) or
    `"{Type} {Title}"` + `Detail` pair (problem) that renders it is replaced by the exact bytes.
  - Forwarding and daemon start: `CliSettings` (appsettings beside the exe, then env, `Roslynk:Port`, plus
    `Roslynk:Cli:AutoStart` / `Roslynk:Cli:Daemon`), `DaemonStarter` (DaemonLauncher's probe, nohup start, 30 s
    window, log file; candidates `Roslynk:Cli:Daemon`, beside the CLI, this checkout's sibling
    `Morris.Roslynk.Mcp` build, `roslynk` on PATH / `~/.dotnet/tools`), `DaemonForwarder` (one `McpClient` over
    streamable HTTP, disposed per call). Verified live: with no daemon on 6611 the CLI started the sibling
    Release build and answered in 0.89 s.
  - Groups: shipped ungrouped (walk-1 finding stands; Nuru prefixes the route with a group and only reads it from
    the request's own declaration). `--capabilities --group-filter symbols` returns 0 endpoints.
  - Added `Morris.Roslynk.Cli.Tests` (143 tests) and both projects to `Morris.Roslynk.slnx`.
  - No daemon change was needed (requirement 4); the only engine-file edit is the generic enum converter above.
  - Follow-ups: timewarp-architecture 388 and Roslynk 004 reserved, claimed and specified; neither could be
    published (details in Results).
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-19T06:22:39Z
- Review oracle: grok session `01a152d3-9539-78c0-a381-63ff467e3db4` (2026-10-19), effort 3, roster general. Round 1 written. M1 open. Disposition not written.
- Review oracle: review by reviewer-grok (grok, model grok-4.7), session not reported, max-turns 200 — 2026-10-19T06:34:51Z
- Fix pass (implement-fix-2, claude-opus-5-5, 2026-10-19): fixed M1. The daemon start moved into the
  dependency-free `Forwarding/DaemonStartInfo.cs`. Both launchers now pass the resolved port as the
  command-line argument `--Roslynk:Port=<port>`, replacing the Unix-only `Roslynk__Port` environment entry.
  The daemon's `WebApplication.CreateBuilder(args)` ranks the command line above its appsettings and
  environment, and an argument survives Windows `UseShellExecute = true`, which cannot carry an environment
  block. The Windows start stays shell-executed and hidden, so it is still detached and the daemon's output
  stays out of the CLI. The Unix script is now `nohup "$@" >>"$0"` (log first, then the daemon command
  line). Added `DaemonStartInfoTests` (6 tests; `DaemonStartInfo.cs` is linked into the test project):
  Windows and Unix start info, the real `/bin/sh` start passing arguments that contain spaces verbatim into
  the log, and `WebApplication.CreateBuilder` reading the argument as `Roslynk:Port`.
  `Morris.Roslynk.Cli.Tests` 149/149; `ganda repo audit` passes.

## Results

`roslynk-cli` (`Source/App/Morris.Roslynk.Cli`) ships: 29 Nuru commands derived from the `[Operation]`
contracts (linked sources, no engine reference), each forwarding to the shared daemon as the same MCP
`tools/call`, printing the daemon's text unchanged.

- Requirement 1: `CapabilitiesContractTests` (59 tests) compares `--capabilities` with the published schema
  (`AddRoslynk` + `WithRoslynkTools`): exactly 29 endpoints = 29 tool names, option names = parameter names,
  tool descriptions equal, parameter descriptions equal once the `Default: ...` suffix `WithRoslynkTools` moves
  into MCP descriptions is split off, defaults equal, required sets equal. No positional parameters.
- Requirement 2: no TWM001 / NURU_H008 in the build. `ForwardingTests` (60 tests) runs every route against a
  stub MCP daemon twice: with every parameter set to a non-default value built from the MCP schema (spelled
  as the catalog says: flags, `--x false` value options for the six true-default bools, repeated `--ids`,
  `--json-args` for the JSON members) and with only the required ones; it asserts the forwarded tool name
  and argument JSON (deep-equal), and that defaults are omitted. `multi_query.operations` via stdin and
  `apply_patch.baseVersions` via `@file` arrive as the same JSON; the real-daemon tests prove the server binds
  them (a Stale `apply_patch` and a `multi_query` batch).
- Requirement 3: `OutputContractTests` (stub) and `RealDaemonTests` (in-process daemon composition on a
  scratch copy of SimpleSolution): success stdout = MCP text byte for byte (including no-trailing-newline
  and empty text), `error=` outline = stderr byte for byte with empty stdout, the exit-code table per code,
  a Stale write (stub `rename_symbol`, real `apply_patch` with an old `baseVersions` hash: exit 6, file
  untouched), daemon unreachable (exit 69, one line) and no daemon to start (exit 69, one line). The batch
  comparison masks only the per-call random `boundary=`.
- Requirement 4: daemon not modified; McpTests 30/30 unchanged.
- Requirement 5: `ProcessIsolationTests` loads the test assembly into the CLI as a `DOTNET_STARTUP_HOOKS`
  hook that lists loaded assemblies at exit: no `Microsoft.CodeAnalysis*`, `Microsoft.Build*`,
  `Morris.Roslynk` or `Morris.Roslynk.Mcp` for `--capabilities` or a forwarded call; the CLI's deps.json
  has none either.
- Requirement 6: in the slnx; `bin/dev build` succeeds (0 warnings); CI's `dotnet test Source/Morris.Roslynk.slnx
  --configuration Release` now builds and tests the CLI. `bin/dev test`: `Morris.Roslynk.Cli.Tests` 143/143,
  `Morris.Roslynk.McpTests` 30/30, `Morris.Roslynk.Tests` 840 passed / 15 failed. The 15 are the
  pre-existing Razor failures (RazorSourceToolTests, RazorRenameTests, ChangeSignature/GetExpressionInfo
  Razor cases, RazorGenerationTests) recorded on master since 002 (13 on SDK 10 / 15 on SDK 11; this
  machine runs SDK 11.0.100-rc.2); none touch the CLI. `ganda repo audit`: 24 passed, 0 failed, 3 skipped.
- Requirement 7: README "Command line (`roslynk-cli`, preview)", AGENTS.md "CLI front end" (linked-source rule,
  forwarding handler, verbatim terminal, exit-code table, groups, adding an operation) plus repository-map and
  test-table rows, `releases.md` Unreleased bullet `(Fixes #4)`. SKILL.md untouched.

Handler and dispatch decision: closed subclasses of one generic forwarding handler, registered by the
generated TimeWarp.Mediator (`TimeWarpMediatorAssembly=true`), dispatched by Nuru's source-generated DI. The
in-process mediator dispatch is not measurable next to the call itself; the whole forwarded call, process
start included, is below.

Timings (Release apphost, Linux/WSL2, median of five after one warm-up run, warm daemon from this checkout
with SimpleSolution loaded):

| Command | Median | Runs (ms) |
| --- | --- | --- |
| `roslynk-cli --capabilities` (cold process) | 184 ms | 187, 193, 184, 180, 179 |
| `roslynk-cli --help` | 176 ms | 164, 155, 180, 183, 176 |
| `roslynk-cli get_symbol ...` (forwarded) | 359 ms | 364, 396, 350, 359, 358 |
| `roslynk-cli get_solution_status` (forwarded) | 368 ms | 331, 370, 333, 370, 368 |
| `roslynk-cli multi_query --json-args ...` (2 ops) | 398 ms | 413, 404, 378, 386, 398 |

So a forwarded call costs about 175 ms over process start: MCP client JIT, HTTP connect, `initialize`, the
call and the session DELETE (506 measured ~26 ms for the call inside the daemon). ReadyToRun / AOT for the
CLI, or a lighter transport, is a packaging-task decision.

Exit-code table: 0 success; 1 usage or `--json-args` error (Nuru) or unknown error code; 2 Invalid;
3 NotFound; 4 Ambiguous; 5 Indexing; 6 Stale; 7 Conflict; 8 NotSupported; 9 Faulted; 10 Truncated;
69 daemon unavailable (sysexits EX_UNAVAILABLE).

Packaging options for the host-unification task (recorded in Roslynk 004): (1) one `Roslynk` tool package,
command `roslynk` = Nuru app with the CLI routes beside `stdio` and the daemon (needs 508-004; preferred);
(2) a second tool package `Roslynk.Cli`, command `roslynk-cli`, as built today (cheap; two installs, version
skew possible); (3) ship `roslynk-cli` inside the `Roslynk` tool folder and have `roslynk` hand unknown verbs
to it (no new package; two apps in one folder).

Follow-ups:

- (a) timewarp-nuru **508-013** (discover `[Operation]` endpoints in referenced assemblies), already filed by
  the cockpit. Also **508-011** (verbatim rendering and a problem renderer; removes `VerbatimTerminal`) and
  **508-012** (app-side groups without prefixing the route).
- (b) timewarp-architecture **388** "Group on CliOperationBinding so a neutral contract can carry its CLI group"
  (contract-side alternative to 508-012; carries the intended five-group mapping). Reserved, claimed, spec
  committed and pushed on `task/388-group-on-clioperationbinding-so-a-neutral-contract`, but **not
  published**: that repo's pre-sign audit fails on its root TimeWarp.Nuru 3.0.0 pin, and `kanban publish`
  refuses the boy-scout bump (also on that branch, dev CLI and agent-identity CLI verified on beta.2)
  because it is a product path. Unblock: merge the bump through a PR there (or publish after master has
  it), then `ganda kanban publish 388 --repo TimeWarpEngineering/timewarp-architecture`.
- (c) Roslynk **004** "Host unification: roslynk command is a Nuru app hosting McpHost, CLI forwarding moves
  to the Nuru daemon". Reserved, claimed, spec committed and pushed on
  `task/004-host-unification-roslynk-command-is-a-nuru-app-hos`, not published: master's root pin is still
  Nuru 3.0.0 (the bump is on this 003 branch). Publish 004 after 003 merges.

### Review

Round 1: effort 3, roster general (one reviewer). Counts: bug 1, now fixed; suggestion 0; nit 0.

- M1 (bug, fixed in the fix pass): `DaemonStarter.StartDetached` passed `Roslynk__Port` only on the non-Windows branch, so a Windows auto-start ignored a non-default port that came from the CLI's appsettings. Now `DaemonStartInfo.Create` passes `--Roslynk:Port=<port>` to the daemon on both launchers, and `DaemonStartInfoTests` cover it.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`.

### How to validate

Smoke:

```bash
./bin/dev build
dotnet test Source/App/Morris.Roslynk.Cli.Tests
dotnet test Source/App/Morris.Roslynk.Cli.Tests --filter FullyQualifiedName~DaemonStartInfoTests
dotnet test Source/App/Morris.Roslynk.McpTests
ganda repo audit
C=Source/App/Morris.Roslynk.Cli/bin/Release/net10.0/roslynk-cli
$C --capabilities | python3 -c "import json,sys; e=json.load(sys.stdin)['endpoints']; print(len(e), sorted(x['pattern'] for x in e)[:3])"
Roslynk__Port=6599 Roslynk__Cli__AutoStart=false $C get_symbol --solutionId x --symbolName y; echo "exit=$?"
$C get_solution_status; echo "exit=$?"   # against the daemon on 6502 (starts one if none listens)
```

Expect: build has 0 warnings and 0 errors; `Morris.Roslynk.Cli.Tests` passes 149/149 (the `DaemonStartInfoTests` filter passes 6/6: both launchers end
with `--Roslynk:Port=6599`, and the real Unix start passes its arguments verbatim) and McpTests 30/30; the
audit passes; `--capabilities` prints `29 ['apply_code_action', 'apply_code_fix', 'apply_patch']`; the
port-6599 call prints one `roslynk-cli: no Roslynk daemon is listening on port 6599 ...` line on stderr and
`exit=69`; `get_solution_status` prints the daemon's outline (one `<solutionPath>,<status>,...` line per open
solution, byte for byte what an MCP client reads) with `exit=0`.

## Review

- Round 1: Blocked — open M1

## Notes

- Tool and parameter descriptions are the contract: the derived CLI must show the same text; do not reword. Where a description shows `#project`/`#path` the output still emits unprefixed keys (see `AGENTS.md`).
- Request property names are camelCase on purpose (they are the MCP parameter names); Nuru keeps them as declared, so options are `--solutionId`, not `--solution-id`. Do not add `[Option]` overrides to rename them.
- 506 findings to carry: six bools default to true (Nuru renders them as value options now); two object-array inputs (`multi_query.operations`, `apply_patch.baseVersions`) are JSON-only; a flat catalog of 29 tools was about 16k tokens, hence groups.
- Related: Roslynk 002 (done), timewarp-nuru 506 (done), 508-001/002/003 and 508-006 (done, released as TimeWarp.Nuru 4.0.0-beta.1 and 4.0.0-beta.2), 508-011/012/013 (to-do, the non-blocking CLI findings), 508-004 (to-do), 508-005 (in beta.2 as served apps), timewarp-architecture 386 (to-do) and 388 (filed by this task), Roslynk 004 (filed by this task).

## Learnings

- Misread: the Goal restatement's acceptance line still says the dev-CLI root pins TimeWarp.Nuru 4.0.0-beta.1. The cockpit unblock in Session and checklist item 2 require 4.0.0-beta.2, and the restatement's opening sentence already names beta.2. The implementation pinned beta.2. The stale acceptance line did not change the build.
