# Round 1 — general
**Date:** 2026-11-09
**Scope reviewed:** merge `f80a0f5d` (upstream `dae11b1..8e14794`) and fork follow-up `44aa307b`, against task.md Requirements and `## Goal (implementer restatement)`.

## Summary

The port is a real merge: `git merge-base HEAD 7de4cfa` = `7de4cfaa…`, `git merge-base HEAD 8e14794` = `8e147947…`, and the upstream push URL is `DISABLED`. All 16 commits are accounted for in Results. Conflict resolutions (`--remerge-diff`) take upstream's `QueryExecution` guard in every read tool, upstream's `WaitAsync` diagnostics cancellation in `RoslynInstance`, and keep the fork's DI/handler shape. The deleted `Morris.Roslynk.Mcp` and McpTests projects stay deleted. The fork carries the snapshot guard into `OperationHandler.ReadAsync`, the instructions sentence into `McpServerRegistration.Instructions`, the analyzer budget through `RoslynkSettings`/`RoslynkEngine`/`appsettings.json.example`/README/AGENTS.md, and adapts `Benchmarks/run.md` to `roslynk stdio`.

Verified on this worktree: `./bin/dev build` 0 warnings / 0 errors; `./bin/dev test` CLI 337/337, engine 1115/1115; `ganda repo audit` exit 0; `reset.sh` restores and cleans only `Benchmarks/TestSolution`. `OperationContractTests` passing confirms the `[Operation]` descriptions match the method descriptions for the changed tools (get_symbol, get_solution_status, apply_patch).

Overall risk is low. Two small findings, both on fork-owned text or tests.

## Issues

### Issue 1 — Severity: suggestion
- File: source/App/Morris.Roslynk.Cli/Handlers/OperationHandler.cs:74
- Description: The fork's own adaptation of #165 changes `ReadAsync` behaviour. A query cancelled after the model was captured, or superseded by a newer solution, now returns an `error=Stale` problem (exit 6) that carries a `snapshot=<id>` header, where before it rethrew cancellation or returned the result. No fork-side test covers this path. `HandlerTests.WhenTheCallerCancels_ThenTheCancellationReachesTheEngine` covers only an already-cancelled token, which still throws from `ReadModelAsync`. Upstream's `QueryExecutionTests` cover the engine helper, but not the handler's `OneOf` → outline → `ProblemOutline.TryParse` → `FromText` round trip. The `snapshot=` header must survive that round trip into `SharedProblemDetails.Extensions` and the rendered stderr outline, and the status must be 6.
- Suggestion: Add a `HandlerTests` unit (or a CLI process test) that drives a read handler's `ReadAsync` with a `handle` delegate which cancels its token mid-query, or which publishes a new solution before it returns. Assert `Status == 6`, `Type == "Stale"`, and that the rendered problem outline contains `snapshot=`.
- Status: open

### Issue 2 — Severity: nit
- File: AGENTS.md:63
- Description: The merge took upstream's diagnostics cancellation (`completion.Task.WaitAsync(cancellationToken)`, and `RunDiagnosticsAsync` skips the build when `cancellationToken.IsCancellationRequested`). The fork's earlier registration/`AwaitThenUnregisterAsync` and `completion.Task.IsCompleted` check were dropped. AGENTS.md still says "the consumer drops an item whose completion is already done". With `WaitAsync` the completion source is not completed by the caller's cancellation, so that clause describes the removed mechanism.
- Suggestion: Reword to match the merged code, for example: "`RequestDiagnosticsAsync` returns `completion.Task.WaitAsync(token)`; the consumer skips an item whose token is already cancelled."
- Status: open
