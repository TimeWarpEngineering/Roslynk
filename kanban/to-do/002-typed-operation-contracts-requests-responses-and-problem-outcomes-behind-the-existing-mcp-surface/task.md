# Typed operation contracts: requests, responses and problem outcomes behind the existing MCP surface

## Goal (implementer restatement)

Put a transport-neutral operation behind every Roslynk tool without changing what clients see. Each operation is a `Query` or `Command` plus a `Response`, and the handler returns `OneOf<Response, SharedProblemDetails>`. The MCP method only fills that request, calls the handler, and renders. Renderers reproduce today's compact text, including `error=`, `errorMessage=`, `candidate` and `stale` headers. Tool names, parameter names, defaults, descriptions, annotations and JSON schemas stay the same, and the existing tests stay the regression harness. `multi_query` still pins one snapshot and binds each operation's arguments into those request classes. This step does not add a mediator runtime or FluentValidation.

`TimeWarp.Contracts` 2.0.0-beta.21 is the package to reference. It ships `lib/net10.0`, so `Morris.Roslynk` stays on `net10.0`.

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

- [x] Package reference and shared infrastructure (problem renderer, response renderer plumbing, binder, Stale mapping)
- [x] Group 1 converted, tests green
- [x] Group 2 converted, tests green
- [x] Group 3 converted, tests green
- [x] Group 4 converted, tests green
- [x] Group 5 (`multi_query`) binds into request classes, tests green
- [x] Schema and composition tests pass unchanged. `bin/dev test` is not fully green: see Results
- [x] GitHub issue opened; `releases.md` Unreleased entry; `AGENTS.md` guidance updated
- [x] Results: list of operations, any output you could not keep byte-identical (expected: none)

## Results

Twenty-nine operations, one per MCP tool. Writes are `Command`; every other tool, including `open_solution` and `reload_solution`, is `Query`. Success text is the existing outline carried on `Response.Text` and returned by `IResponseRenderer<TResponse>`. Problems are parsed from today's `error=` outline and written back from the ordered headers, so `candidate`, `stale`, `rejected` and `snapshot` stay in the same order. No response or error text was intentionally changed.

`StaleWriteException` is mapped once, in `Problems.FormatStale`, at the seven write handlers that catch it. `apply_patch` already built its Stale outline itself. The `RoslynkTool` wrapper still maps escaped exceptions to `Invalid` / `Faulted`.

Operations:

- Group 1: `get_symbol`, `get_members`, `get_symbol_body`, `find_definition`, `search_symbols`, `get_expression_info`, `get_type_hierarchy`, `find_implementations`
- Group 2: `find_references`, `get_callers`, `get_callees`, `find_reads`, `find_writes`, `find_dead_code`, `find_dead_conditionals`
- Group 3: `get_diagnostics`, `get_code_actions`, `open_solution`, `get_solution_status`, `reload_solution`
- Group 4: `apply_code_action`, `apply_code_fix`, `extract_method`, `change_signature`, `rename_symbol`, `rename_parameter`, `remove_unused_usings`, `apply_patch`
- Group 5: `multi_query` (binds each slot's arguments onto that operation's request; `solutionId` stays on the batch)

`multi_query` catalog entries point at `HandleAsync`. Cores stay on the tools so direct core tests and the pinning stubs still bind parameter-by-parameter. `MultiQueryCatalogTests` now compares request properties with the public MCP parameters (an internal-type assertion, not an output string).

Output that is not byte-identical: none.

`TestSolutions.RunDotnet` now restores and builds fixtures in-process (`--disable-build-servers`) and reads both streams before it inspects the exit code. Otherwise an MSBuild node inherits the redirected pipes and `ReadToEnd` never returns, which deadlocked `bin/dev test` on this machine.

`bin/dev test` on the machine default SDK 11.0.100-rc.2: McpTests 30 passed; engine tests 840 passed, 15 failed, all Razor. On SDK 10.0.402: McpTests 30 passed; engine tests 842 passed, 13 failed, all Razor writes (rename, change signature, extract, code action). `WhenRenamingAFieldDeclaredInARazorCodeBlock_ThenTheRazorFileIsRewrittenOnDisk` fails the same way with the pre-refactor `RenameSymbolTool` (applied=Y, markup renamed, `@code` declaration left as `CurrentCount`). Those failures are not this refactor. SDK 11 adds two more (`WhenTheProjectReferencesALoadableRazorCompiler` and the Razor `get_expression_info` case) because that SDK's Razor compiler reference yields no generators to Microsoft.CodeAnalysis 5.9.0. This task does not change the Razor seams.

`ganda repo audit` exits 0. Two advisory warnings were already there and were left: `memsearch-scaffold` (`core.hooksPath=.githooks` in local git config) and `vscode-window-icon` (missing avatar and `.vscode` files).

### Review

Round 1 (general reviewer, effort 3) is in progress: 0 bugs, 1 open suggestion (M2), 1 open nit (M1), 2 wontfix suggestions (M3, M4). A fix pass is requested for M1 and M2, and disposition is pending. The reviewer confirmed that the 15 Razor test failures also occur on master with only the harness fix applied. See `review/round-1/merged.md`.

### How to validate

Smoke: `dotnet test Source/App/Morris.Roslynk.McpTests/Morris.Roslynk.McpTests.csproj -c Release` and `dotnet test Source/App/Morris.Roslynk.Tests/Morris.Roslynk.Tests.csproj -c Release --filter 'FullyQualifiedName~OperationContractTests|FullyQualifiedName~ProblemOutlineTests|FullyQualifiedName~MultiQueryCatalogTests'`

Expect: McpTests reports 30 passed with the existing schema expectations. The three engine filters pass: every tool's `[Operation]` name and description match the MCP method, problem outlines round-trip, and catalog binding matches the public parameters.

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
- Implement oracle resumed 2026-10-18. Typed operations landed for all 29 tools (Fixes #2). `ganda repo audit` exit 0. `bin/dev test` schema suite green; 13 pre-existing Razor write failures remain and reproduce with the original `RenameSymbolTool`. Audit advisories `memsearch-scaffold` and `vscode-window-icon` left as pre-existing.
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Done — 2026-10-18T11:19:10Z
- Review oracle round 1 (2026-10-18): general reviewer (claude, model claude-opus-5-5), effort 3, turn cap 200. Findings in `review/round-1/` (`general.md`, `merged.md`). 0 bugs. M1 (nit, brace indentation) and M2 (suggestion, default-initializer guard) are open, so a fix pass is requested. M3 and M4 are wontfix with rationale. No misread: the Goal restatement matches the task.

## Blocked

Cleared 2026-10-18: TimeWarp.Contracts 2.0.0-beta.21 is on nuget.org with `lib/net10.0` and `lib/net11.0` (architecture task 387, PR #537, release v2.0.0-beta.21). Reference `TimeWarp.Contracts` **2.0.0-beta.21** in `Source/Directory.Packages.props`; `Morris.Roslynk` stays net10.0. https://www.nuget.org/packages/TimeWarp.Contracts/2.0.0-beta.21

Earlier block (2026-10-18): 2.0.0-beta.20 shipped only `lib/net11.0`, so restore on net10.0 failed with NU1202; tracked as timewarp-architecture 387.