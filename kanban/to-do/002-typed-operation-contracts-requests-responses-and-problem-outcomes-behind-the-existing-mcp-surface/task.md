# Typed operation contracts: requests, responses and problem outcomes behind the existing MCP surface

## Description

Step 3 of the serial contracts-first plan (timewarp-nuru 508, timewarp-architecture 384). Every Roslynk tool becomes a typed operation: a request class, a typed response, and an outcome of `OneOf<Response, SharedProblemDetails>`, with the current compact text produced by a renderer at the edge. The published MCP surface does not change: tool names, parameter names and defaults, descriptions, annotations, and the exact text of every response and error stay byte-identical, so the existing test suites are the regression harness. This is what lets step 7 generate a Nuru CLI from the same classes and step 9 host the MCP binding without a second schema.

Contract shape (timewarp-architecture style, Roslynk file conventions under `Source/`): per operation folder `Features/<Area>/<Operation>/`, an outer `public static partial class <Operation>` with a nested `Query` or `Command` (`IQuery<OneOf<Response, SharedProblemDetails>>` / `ICommand<...>` from `TimeWarp.Mediator.Contracts`, which `TimeWarp.Contracts` brings in), a `Response`, and `[Operation("<mcp tool name>", Description = ...)]` on the request carrying the text that is today the method `[Description]`. Members are public settable properties named exactly as the MCP parameters are today, with `System.ComponentModel.Description` per property (reuse `ParameterDescriptions` constants) and initializers for defaults. Writes keep their `checkOnly` member.

Handlers are the existing internal cores: a read handler takes the pinned `SolutionModel` and a request; a write handler takes the `RoslynInstance` and a request. The public MCP method becomes thin: bind parameters into the request, run the handler, render. Rendering goes through `IResponseRenderer<TResponse>` implementations that reproduce today's `OutlineBuilder` output exactly; a `SharedProblemDetails` renders to today's `error=`/`errorMessage=`/`candidates` headers (`Type` = error code such as Indexing, Stale, Ambiguous, Invalid, NotSupported, Conflict, Faulted; `Detail` = message; candidates in `Extensions`). `StaleWriteException` becomes a typed Stale problem at the handler boundary, so every write tool reports it the same way; the `RoslynkTool` wrapper keeps mapping escaped exceptions to `Invalid`/`Faulted`.

Constraints from `AGENTS.md`: cores stay pure against one captured snapshot; `multi_query` keeps pinning one model and now binds its operation arguments into the request classes (same string/bool/int coercions and unknown-key failure); `ResponseBudget`, `ItemPaging` and `ResponseBudget.Fit` behave as before; no change to lifecycle, writing, watching or Razor seams. Do not add FluentValidation or a mediator runtime in this step; handlers are invoked directly and binding errors remain `Invalid`.

## Requirements

- `TimeWarp.Contracts` 2.0.0-beta.20 added to `Source/Directory.Packages.props` and referenced by `Morris.Roslynk` (keep central versions; no local versions).
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
