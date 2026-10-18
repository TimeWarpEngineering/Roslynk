# Round 1 — general
**Date:** 2026-10-18
**Scope reviewed:** `master...task/002-typed-operation-contracts-requests-responses-and-p` (34a766d), all 74 files; shared infrastructure (`Infrastructure/Operations/*`, `_ServicesRegistration.cs`), all 29 operation/tool pairs (read in full: GetSymbol, FindReferences, GetDiagnostics, RenameSymbol, ApplyPatch, MultiQuery; the rest checked by diff pattern), `MultiQueryCatalog` binder, test changes, `AGENTS.md`, `releases.md`.

## Summary

Each of the 29 MCP tools now has a typed `Query`/`Command` with `[Operation]` metadata, a `Response`, a renderer, and a `HandleAsync` that returns `OneOf<Response, SharedProblemDetails>`. The public MCP method only fills the request and renders. `multi_query` binds onto request properties and still keeps unknown-key, required and `solutionId` handling. The published surface is unchanged: McpTests 30/30 pass, McpTests and `ToolCompositionTests` are not edited, and no expected output string was changed. The risk is low. The main design point is that each response is an opaque outline string, and the problem outcome is recovered by parsing that rendered text back. This matches the task's byte-identity constraint and is a deliberate staged step, so it is recorded and not sent back.

Verification run by the reviewer:

- `dotnet build Source/Morris.Roslynk.slnx -c Release`: 0 warnings, 0 errors.
- McpTests: 30 passed.
- Engine tests: 840 passed, 15 failed, all Razor (SDK 11.0.100-rc.2).
- A `git archive master` export in /tmp, with only the branch's `TestSolutions.cs` harness fix copied in, fails **the same 15 Razor tests** (98 run by Razor/GetExpressionInfo/ChangeSignature filter). Unmodified master hangs in the test harness, which is the deadlock the implementer fixed. So the failures came before this refactor and are environmental (SDK/Razor generator).
- `ganda repo audit`: exit 0 (pre-existing `vscode-window-icon` advisory).
- GitHub issue #2 exists and is open. `releases.md` has the `(Fixes #2)` bullet.
- `[DefaultValue]` attribute values agree with property initializers in every operation file (manual scan). See Issue 2 for the missing guard.

## Issues

### Issue 1 — Severity: nit
- File: Source/App/Morris.Roslynk/Features/References/RenameSymbol/RenameSymbolTool.cs:160 (and 13 more tool files)
- Description: When the old public method body was split into `Execute`, its closing brace was re-indented from one tab to two (`+		}` replacing `-	}`). This happens in ApplyCodeActionTool, ApplyCodeFixTool, GetCodeActionsTool, GetDiagnosticsTool, MultiQueryTool, ApplyPatchTool, ExtractMethodTool, RenameSymbolTool, ChangeSignatureTool, RenameParameterTool, GetSolutionStatusTool, OpenSolutionTool, ReloadSolutionTool and RemoveUnusedUsingsTool. It is formatting churn against `Source/.editorconfig` (tabs, Allman) and the AGENTS.md rule "without unrelated formatting churn".
- Suggestion: Restore the one-tab indentation of each method's closing brace. `git diff master...HEAD -U0 -- Source/App/Morris.Roslynk/Features | grep -B3 '^+\t\t}$'` lists them.
- Status: open

### Issue 2 — Severity: suggestion
- File: Source/App/Morris.Roslynk.Tests/Infrastructure/Operations/OperationContractTests.cs:63 (also Features/MultiQuery/MultiQueryCatalogTests.cs:131)
- Description: When an argument is omitted, `MultiQueryCatalog.BindRequest` leaves the property at its **initializer** value. Both contract tests compare the public parameter default only with the property's `[DefaultValue]` attribute, so drift between the initializer and the attribute is not caught. For example, `[DefaultValue(true)] public bool includeMethods { get; set; } = false;` would pass both tests, but a batched `get_members` would then silently differ from a single call. Before this change, the old test compared the public default with the core parameter's real default, which is the value the binder used. The values agree today, but nothing keeps them in step.
- Suggestion: In `OperationContractTests` (or the catalog test), build the request with `Activator.CreateInstance` and assert that each optional property's value equals `parameter.DefaultValue`, as well as the `[DefaultValue]` attribute.
- Status: open

### Issue 3 — Severity: suggestion
- File: Source/App/Morris.Roslynk/Infrastructure/Operations/OperationResults.cs:15, Infrastructure/Operations/Problems.cs:28
- Description: The typed outcome is derived from rendered text. Each handler calls the existing core, gets the outline string, and `Split` re-parses it (`ProblemOutline.TryParse`) into a `SharedProblemDetails` or a `Response { Text }`. `Problems.FormatStale` builds a problem, formats it to a string inside the core, and the handler then parses that string again. Bytes stay identical (all headers come from `OutlineBuilder` and end in `\n`; `ProblemOutlineTests` covers the round trip). But the `OneOf` discriminant depends on text shape. An error outline with a header line that has no `=` would quietly become `IsT0`, and `Response` carries no typed fields that a Nuru CLI (step 7) could render differently.
- Suggestion: In a later plan step, have cores return `Error`/`SharedProblemDetails` directly and give responses typed fields. Keep `Split` only as a migration shim.
- Status: open

### Issue 4 — Severity: suggestion
- File: Source/App/Morris.Roslynk/Features/References/RenameSymbol/RenameSymbolTool.cs:77 (all 8 write handlers)
- Description: The task says "a write handler takes the `RoslynInstance` and a request". The write handlers (`HandleAsync(Command, CancellationToken)`) instead resolve the instance themselves from `request.solutionId`. The updated AGENTS.md text does not claim otherwise, so the docs and code agree with each other but not with the task wording.
- Suggestion: Either pass the resolved `RoslynInstance` into write handlers, or accept the request-only shape (writes are not batched, and a request-carried `solutionId` keeps the handler transport-neutral).
- Status: open
