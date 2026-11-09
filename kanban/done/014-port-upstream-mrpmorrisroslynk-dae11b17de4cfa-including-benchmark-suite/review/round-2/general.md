# Round 2 — general
**Date:** 2026-11-09
**Scope reviewed:** fix delta since round 1 (`a8d3bdc0..ee3efb02`): `fb81e1c3` (`AGENTS.md`, `source/App/Morris.Roslynk.Cli.Tests/HandlerTests.cs`) and `ee3efb02` (kitchen only). Re-verified M1 and M2.

## Summary

The fix pass is test and documentation only; no product code changed. The new `HandlerTests.WhenACapturedReadIsCancelledOrSuperseded_ThenTheHandlerReturnsStaleWithTheSnapshot` calls the protected `OperationHandler.ReadAsync` by reflection on a real `GetSymbolHandler` over a scratch Simple fixture. One delegate cancels after capture, and the other publishes a new solution with `RoslynInstance.AdvanceTo`. Each case checks `Type` Stale, `Status` 6 (`ExitCodes.Stale`), the `snapshot` header on the problem extensions, and `snapshot=` in the `ProblemRenderer` outline with no body. The `AGENTS.md` diagnostics sentence now matches `RoslynInstance.RequestDiagnosticsAsync` (line 272: `completion.Task.WaitAsync(cancellationToken)`) and the consumer's token check (lines 343-344). `dotnet test … --filter HandlerTests` was run in this worktree: 44 passed, 0 failed. Risk is low.

## Prior findings

- M1 (suggestion): **fixed**. Both stale paths of the handler round trip are covered, as the round-1 suggestion asked.
- M2 (nit): **fixed**. The wording matches the source.

## Issues

None new.
