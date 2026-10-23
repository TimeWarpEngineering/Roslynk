# Round 1 — merged findings
**Date:** 2026-10-23
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: Source/App/Morris.Roslynk.Cli/Handlers/OperationHandlers.cs:71
- Description: `change_signature` no longer cancels during the load wait that #119 added. `ChangeSignatureTool.HandleAsync` now takes a `CancellationToken` and passes it to `InstanceRegistry.GetOrBeginAsync`, which waits up to `Roslynk:LoadWaitSeconds` (default 30s). `ChangeSignatureHandler` still calls `HandleAsync(FromCaller(command))`, so the token stays at its default and a cancelled CLI or MCP call keeps waiting. Every other write handler passes `cancellationToken`. Before this merge the method had no token parameter and `GetOrBeginAsync` did not wait (`fac24b5` has no `WaitForInitialLoad`), so the old call site still compiles. The task requires that cancellation still work once the wait is on the served path, and `AGENTS.md` says write handlers pass the call's token.
- Suggestion: Pass `cancellationToken` into `ChangeSignatureTool.HandleAsync`, as `RenameSymbolHandler` and `ExtractMethodHandler` do.
- Source: general
- Disposition notes: fixed — `ChangeSignatureHandler` passes `cancellationToken`; `HandlerTests.WhenTheCallerCancels_ThenTheCancellationReachesTheEngine` now covers change_signature (fails without the fix).

### M2 — Severity: nit — Status: fixed
- File: README.md:363
- Description: The new Settings table lists `MaxResponseChars`, `LoadWaitSeconds`, `IdleEvictionMinutes`, and `EvictionCheckMinutes`. The three bullets under "A shared server serves every client…" repeat the first, third, and fourth keys and omit `LoadWaitSeconds`.
- Suggestion: Delete those three leftover bullets. The table and the paragraph above them already say what they say.
- Source: general
- Disposition notes: fixed — the three leftover bullets are deleted; the Settings table is the one list.

## Duplicates / conflicts

- No overlapping findings. One reviewer.
