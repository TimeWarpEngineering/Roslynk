# Round 2 — general
**Date:** 2026-10-23
**Scope reviewed:** `bfab33f..87a9afd` only (the round 1 fix). Product delta: `ChangeSignatureHandler` passes `cancellationToken`, `HandlerTests.WhenTheCallerCancels_ThenTheCancellationReachesTheEngine` covers `change_signature`, and the three leftover README settings bullets are gone.

## Summary

The fix does what round 1 asked. `ChangeSignatureHandler` now passes the call token into `ChangeSignatureTool.HandleAsync`, which forwards it to `InstanceRegistry.GetOrBeginAsync` and `WaitForInitialLoadAsync`. A token that is already cancelled makes `Task.WaitAsync` throw `OperationCanceledException`; the default token does not, so the new assertion fails if the argument is dropped again. The settings table is the only list of the four `Roslynk:*` keys. The implementer restatement matches the original task. No new defect on this delta.

## Issues

<!-- None. Prior M1 and M2 are fixed. -->
