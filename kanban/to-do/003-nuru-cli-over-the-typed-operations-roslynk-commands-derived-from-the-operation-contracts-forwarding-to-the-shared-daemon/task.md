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

## Checklist

- [ ] `Morris.Roslynk.Cli` project added to `Source/Morris.Roslynk.slnx`, linked operation sources compile without an engine reference
- [ ] Forwarding handler and daemon client; port resolution and daemon start parity with `StdioBridge`/`DaemonLauncher` recorded
- [ ] 29 routes in `--capabilities`, contract test against the pinned MCP schema passes
- [ ] Per-route forwarding test (name + arguments), including the two `--json-args` members
- [ ] Byte-identical success text, problem-to-stderr with exit-code table, Stale and daemon-unreachable cases tested
- [ ] Groups decided (or follow-up filed) and `--group-filter` demonstrated
- [ ] No Roslyn assembly loaded in the CLI process; cold-start and forwarded-call timings recorded in Results
- [ ] `bin/dev build`, `bin/dev test`, `ganda repo audit` green; CI workflow builds the project
- [ ] README CLI section, AGENTS.md subsection, `releases.md` bullet `(Fixes #4)`
- [ ] Follow-ups filed and ids recorded in Results

## Session

- Created: 2532391 (2026-10-19)

## Notes

- Tool and parameter descriptions are the contract: the derived CLI must show the same text; do not reword. Where a description shows `#project`/`#path` the output still emits unprefixed keys (see `AGENTS.md`).
- Request property names are camelCase on purpose (they are the MCP parameter names); Nuru keeps them as declared, so options are `--solutionId`, not `--solution-id`. Do not add `[Option]` overrides to rename them.
- 506 findings to carry: six bools default to true (Nuru renders them as value options now); two object-array inputs (`multi_query.operations`, `apply_patch.baseVersions`) are JSON-only; a flat catalog of 29 tools was about 16k tokens, hence groups.
- Related: Roslynk 002 (done), timewarp-nuru 506 (done), 508-001/002/003 (done, released as TimeWarp.Nuru 4.0.0-beta.1), 508-004 (to-do), 508-005 (backlog), timewarp-architecture 386 (to-do).
