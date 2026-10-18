# Typed operation contracts: requests, responses and problem outcomes behind the existing MCP surface

## Goal (implementer restatement)

Give every Roslynk tool a transport-neutral operation: a request (`Query` or `Command`), a typed `Response`, and an outcome of `OneOf<Response, SharedProblemDetails>`. The public MCP method only binds arguments, calls the existing core, and renders. Renderers emit the same compact text the tools emit today, including `error=` / `errorMessage=` / `candidate` / `stale` headers. Tool names, parameter names, defaults, descriptions, annotations, and JSON schemas stay byte-identical, and the current test suite remains the regression harness. `multi_query` keeps one pinned snapshot and binds operation arguments into those request classes. No mediator runtime and no FluentValidation in this step.

This walk cannot start that conversion. `TimeWarp.Contracts` 2.0.0-beta.21, the package the request types must come from, publishes only `lib/net11.0`. `Morris.Roslynk` targets `net10.0`, and `.github/workflows/workflow.yml` restores and tests with `dotnet-version: 10.0.x`. Restore fails with NU1202 before any operation class can compile.

## Description

Step 3 of the serial contracts-first plan (timewarp-nuru 508, timewarp-architecture 384). Every Roslynk tool becomes a typed operation: a request class, a typed response, and an outcome of `OneOf<Response, SharedProblemDetails>`, with the current compact text produced by a renderer at the edge. The published MCP surface does not change: tool names, parameter names and defaults, descriptions, annotations, and the exact text of every response and error stay byte-identical, so the existing test suites are the regression harness. This is what lets step 7 generate a Nuru CLI from the same classes and step 9 host the MCP binding without a second schema.

Contract shape (timewarp-architecture style, Roslynk file conventions under `Source/`): per operation folder `Features/<Area>/<Operation>/`, an outer `public static partial class <Operation>` with a nested `Query` or `Command` (`IQuery<OneOf<Response, SharedProblemDetails>>` / `ICommand<...>` from `TimeWarp.Mediator.Contracts`, which `TimeWarp.Contracts` brings in), a `Response`, and `[Operation("<mcp tool name>", Description = ...)]` on the request carrying the text that is today the method `[Description]`. Members are public settable properties named exactly as the MCP parameters are today, with `System.ComponentModel.Description` per property (reuse `ParameterDescriptions` constants) and initializers for defaults. Writes keep their `checkOnly` member.

Handlers are the existing internal cores: a read handler takes the pinned `SolutionModel` and a request; a write handler takes the `RoslynInstance` and a request. The public MCP method becomes thin: bind parameters into the request, run the handler, render. Rendering goes through `IResponseRenderer<TResponse>` implementations that reproduce today's `OutlineBuilder` output exactly; a `SharedProblemDetails` renders to today's `error=`/`errorMessage=`/`candidates` headers (`Type` = error code such as Indexing, Stale, Ambiguous, Invalid, NotSupported, Conflict, Faulted; `Detail` = message; candidates in `Extensions`). `StaleWriteException` becomes a typed Stale problem at the handler boundary, so every write tool reports it the same way; the `RoslynkTool` wrapper keeps mapping escaped exceptions to `Invalid`/`Faulted`.

Constraints from `AGENTS.md`: cores stay pure against one captured snapshot; `multi_query` keeps pinning one model and now binds its operation arguments into the request classes (same string/bool/int coercions and unknown-key failure); `ResponseBudget`, `ItemPaging` and `ResponseBudget.Fit` behave as before; no change to lifecycle, writing, watching or Razor seams. Do not add FluentValidation or a mediator runtime in this step; handlers are invoked directly and binding errors remain `Invalid`.

## Requirements

- `TimeWarp.Contracts` 2.0.0-beta.21 added to `Source/Directory.Packages.props` and referenced by `Morris.Roslynk` (keep central versions; no local versions).
- Shared infrastructure first: problem-to-header renderer, `IResponseRenderer` registration, request binding helper used by the MCP methods and by `MultiQueryCatalog`, Stale mapping. Then convert tools in groups, each group green before the next: (1) read-only symbol/navigation tools (`get_symbol`, `get_members`, `get_symbol_body`, `find_definition`, `search_symbols`, `get_expression_info`, `get_type_hierarchy`, `find_implementations`); (2) references, callers, callees, reads, writes, dead code, dead conditionals; (3) diagnostics, code actions discovery, solution lifecycle (`open_solution`, `get_solution_status`, `reload_solution`); (4) writes (`apply_code_action`, `apply_code_fix`, `extract_method`, `change_signature`, `rename_symbol`, `rename_parameter`, `remove_unused_usings`, `apply_patch`); (5) `multi_query`.
- Every tool's MCP metadata is unchanged: name, title, annotations, description text, parameter names, descriptions, defaults and JSON schema. `ToolCompositionTests` and the `McpTests` schema tests pass without edits to their expectations.
- Every response and error text is unchanged: the full existing test suite passes without changing expected strings. Where a test asserts on an internal type rather than output, adapt the test, not the output.
- `[Operation]` names equal the MCP tool names; member names equal the MCP parameter names. Complex members (`multi_query.operations`, `apply_patch.baseVersions`) stay typed objects; they bind from JSON as today.
- `releases.md` gets an `# Unreleased` bullet; open a GitHub issue on this fork for the refactor and reference it as `(Fixes #nn)`.
- `AGENTS.md`: update the "MCP contracts and adding tools" and "read-only feature" guidance to describe the request/response/renderer split, and the multi_query section for binding into request classes. Keep the rest of Pete's guide intact.

## Checklist

- [ ] Package reference and shared infrastructure (problem renderer, response renderer plumbing, binder, Stale mapping)
- [ ] Group 1 converted, tests green
- [ ] Group 2 converted, tests green
- [ ] Group 3 converted, tests green
- [ ] Group 4 converted, tests green
- [ ] Group 5 (`multi_query`) binds into request classes, tests green
- [ ] Schema and composition tests pass unchanged; `bin/dev test` green
- [ ] GitHub issue opened; `releases.md` Unreleased entry; `AGENTS.md` guidance updated
- [ ] Results: list of operations, any output you could not keep byte-identical (expected: none)

## Notes

- Tool and parameter descriptions are the contract: move the text, do not reword it. Where the description shows `#project`/`#path` while output emits unprefixed keys, leave both as they are (recorded in `AGENTS.md`).
- Task 506 on timewarp-nuru found six boolean parameters defaulting to `true` and two object-array parameters; keep their CLR shapes here, the CLI derivation rules handle them in 508-001.
- If the work needs splitting for parallel implementers, file children of this task per group; group 1 must land first because it carries the shared infrastructure.
- Related: 001 (fork tooling, done); timewarp-nuru 508/508-001; timewarp-architecture 384 (package), 386 (template adoption).

## Session

- Created: 2909955 (2026-10-18)
- Implementation: blocked (2026-10-18). `dotnet restore` of `PackageReference Include="TimeWarp.Contracts" Version="2.0.0-beta.20"` on `Morris.Roslynk` (`net10.0`) failed: NU1202, package supports only `net11.0`. NuGet listing `https://api.nuget.org/v3-flatcontainer/timewarp.contracts/index.json` contains only `2.0.0-beta.20`. The nupkg's sole lib folder is `lib/net11.0/timewarp-contracts.dll` (dependency group `net11.0`: OneOf 3.0.271, TimeWarp.Mediator.Contracts 14.0.0-beta.5). The package reference was reverted so this branch still builds. No tool conversion, issue, or release note landed.
- Unblocked: 2026-10-18, beta.21 live; rerun of `ganda task work 002` launched
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Blocked — 2026-10-18T05:20:55Z

## Blocked

Cleared 2026-10-18: TimeWarp.Contracts 2.0.0-beta.21 is on nuget.org with `lib/net10.0` and `lib/net11.0` (architecture task 387, PR #537, release v2.0.0-beta.21). Reference `TimeWarp.Contracts` **2.0.0-beta.21** in `Source/Directory.Packages.props`; `Morris.Roslynk` stays net10.0. https://www.nuget.org/packages/TimeWarp.Contracts/2.0.0-beta.21

Earlier block (2026-10-18): 2.0.0-beta.20 shipped only `lib/net11.0`, so restore on net10.0 failed with NU1202; tracked as timewarp-architecture 387.