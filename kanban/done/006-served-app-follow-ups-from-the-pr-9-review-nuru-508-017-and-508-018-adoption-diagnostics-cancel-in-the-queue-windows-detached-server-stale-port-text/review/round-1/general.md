# Round 1 — general
**Date:** 2026-10-21
**Scope reviewed:** `task/006-served-app-follow-ups-from-the-pr-9-review-nuru-50` vs `master`, product commit `7e33df5`. Compared with the pre-implement task (checklist at `ddaa1ff`) and with `## Goal (implementer restatement)`.

## Summary

The branch pins TimeWarp.Nuru, Nuru.Daemon, Nuru.McpHost, and Nuru.DevCli to 4.0.0-beta.5, takes the 29 routes from the engine assembly via `[assembly: NuruEndpointAssembly]`, and drops the linked contract sources and the `engine` alias. Binding failures go through `UseProblemRendererForErrors()` with MCP and CLI tests, and CLI binding stays exit 1. `RequestDiagnosticsAsync` cancels the returned task from the caller's token and skips a queued build whose completion is already done. README, AGENTS.md, `releases.md`, and `Benchmarks.md` cover the socket, Windows, and stale-port items the task asked for. The one gap is a leftover `OperationHandler.ToEngine` sentence in AGENTS.md that describes a copy path this change removed. Re-ran `WhenADiagnosticsBuildIsCancelledBehindALongWrite_ThenTheCallerStopsWaitingAndTheBuildIsSkipped`: passed (1/1). Did not re-run the full CLI or engine suites.

## Issues

### Issue 1 — Severity: suggestion
- File: AGENTS.md:182
- Description: The MCP-contracts paragraph still says a new member type must round-trip through JSON "because a request crosses from the app's copy of the contract to the engine's as JSON (`OperationHandler.ToEngine`)". That method and the linked app copy are gone. Handlers call `FromCaller`, which resolves `solutionId` / `solutionPath` on the engine request Nuru already bound. The same file's handlers bullet (AGENTS.md:249) already says that. The JSON requirement itself is still right for `--json-args` and MCP; the reason and the method name are not, so the next change can be steered back toward a second contract copy.
- Suggestion: Keep the System.Text.Json round-trip rule, and tie it to Nuru binding `--json-args` and MCP arguments. Remove `OperationHandler.ToEngine` and the claim that the app still has its own copy of the contract.
- Status: open
