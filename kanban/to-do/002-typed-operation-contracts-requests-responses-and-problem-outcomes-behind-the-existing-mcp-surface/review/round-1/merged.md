# Round 1 — merged findings
**Date:** 2026-10-18
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 2 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: nit — Status: fixed
- File: Source/App/Morris.Roslynk/Features/References/RenameSymbol/RenameSymbolTool.cs:160 (+13 tool files listed in general.md Issue 1)
- Description: The closing brace of each split-out `Execute` method is re-indented to two tabs. This is formatting churn against `Source/.editorconfig` and AGENTS.md.
- Suggestion: Restore one-tab indentation of those closing braces.
- Source: general (Issue 1)
- Disposition notes: fixed on the fix pass (2026-10-18). Each `Execute` closing brace is one tab again, matching its opening brace. Round 2 has not re-verified this.

### M2 — Severity: suggestion — Status: fixed
- File: Source/App/Morris.Roslynk.Tests/Infrastructure/Operations/OperationContractTests.cs:63
- Description: The contract tests compare public defaults only with `[DefaultValue]`. The multi_query binder uses the property **initializer**, so a drift between the initializer and the attribute would change batched behavior and no test would catch it.
- Suggestion: Also assert that a freshly constructed request's property value equals the public parameter default.
- Source: general (Issue 2)
- Disposition notes: fixed on the fix pass (2026-10-18). `OperationContractTests` and `MultiQueryCatalogTests` construct the request and compare each optional property's value with the public parameter default. Round 2 has not re-verified this.

### M3 — Severity: suggestion — Status: wontfix
- File: Source/App/Morris.Roslynk/Infrastructure/Operations/OperationResults.cs:15
- Description: The `OneOf` outcome is recovered by re-parsing the core's rendered outline, and `Response` is an opaque `Text` carrier.
- Suggestion: Later step: cores return typed problems and responses; `Split` stays only as a migration shim.
- Source: general (Issue 3)
- Disposition notes: wontfix in this task (decided by review oracle, 2026-10-18). The task requires byte-identical output and "handlers are the existing internal cores", so carrying the existing outline is the conservative step. `ProblemOutlineTests` pins the round trip, and the implementer disclosed the design in Results. Typed response fields belong to the Nuru CLI derivation (plan step 7, timewarp-nuru 508-001) or a follow-up Roslynk task. An operator may reopen it.

### M4 — Severity: suggestion — Status: wontfix
- File: Source/App/Morris.Roslynk/Features/References/RenameSymbol/RenameSymbolTool.cs:77
- Description: The write handlers take only the request and resolve `RoslynInstance` from `request.solutionId`. The task wording was "takes the RoslynInstance and a request".
- Suggestion: Pass the instance in, or accept the request-only shape.
- Source: general (Issue 4)
- Disposition notes: wontfix (decided by review oracle, 2026-10-18). Writes are not in the multi_query catalog, so nothing has to supply a pinned instance, and a request-carried `solutionId` keeps the handler transport-neutral for the step-9 MCP binding. AGENTS.md describes the shape that was implemented. An operator may reopen it.

## Duplicates / conflicts

- None. Single reviewer.

## Verification evidence

- Release build clean; McpTests 30/30; engine 840 passed / 15 failed (all Razor).
- The same 15 Razor failures reproduce on a `git archive master` export with only the `TestSolutions.cs` harness fix, so they are pre-existing and not caused by this diff.
- `ganda repo audit` exit 0; issue #2 open; `releases.md` bullet present.
