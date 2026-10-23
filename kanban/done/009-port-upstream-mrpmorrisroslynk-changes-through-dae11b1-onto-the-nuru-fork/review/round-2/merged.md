# Round 2 — merged findings
**Date:** 2026-10-23
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 1 | 0 |

## Resolved prior

### M1 — Severity: bug — Status: fixed
- File: Source/App/Morris.Roslynk.Cli/Handlers/OperationHandlers.cs:71
- Description: Round 1: `change_signature` dropped the call token, so a cancelled call kept waiting on the load. Re-checked on `87a9afd`: `ChangeSignatureHandler` passes `cancellationToken` into `ChangeSignatureTool.HandleAsync`, matching the other write handlers. `WaitForInitialLoadAsync` observes that token via `Task.WaitAsync`. `HandlerTests.WhenTheCallerCancels_ThenTheCancellationReachesTheEngine` now includes `change_signature`. Release filter of that test: 1 passed (52 ms).
- Suggestion: Pass `cancellationToken` into `ChangeSignatureTool.HandleAsync`.
- Source: general
- Disposition notes: fixed in `87a9afd`. Confirmed in round 2.

### M2 — Severity: nit — Status: fixed
- File: README.md:361
- Description: Round 1: three settings bullets repeated the new table and omitted `LoadWaitSeconds`. Re-checked: those bullets are gone. The table at README.md:340 still lists `MaxResponseChars`, `LoadWaitSeconds`, `IdleEvictionMinutes`, and `EvictionCheckMinutes`.
- Suggestion: Delete the leftover bullets.
- Source: general
- Disposition notes: fixed in `87a9afd`. Confirmed in round 2.

## Issues

<!-- No new findings. -->

## Duplicates / conflicts

- No overlapping findings. One reviewer. Prior IDs M1 and M2 kept.
