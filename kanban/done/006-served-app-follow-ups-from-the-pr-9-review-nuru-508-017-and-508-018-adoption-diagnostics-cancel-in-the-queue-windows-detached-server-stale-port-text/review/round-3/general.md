# Round 3 — general
**Date:** 2026-10-21
**Scope reviewed:** branch `task/006-served-app-follow-ups-from-the-pr-9-review-nuru-50` vs `master`, with the re-review focused on `61c7dc8` and on M1. Compared with `## Goal (implementer restatement)` and with the original task checklist.

## Summary

The PR #10 fix pass tag-gates derive-version, pack, NuGet login, and push with `startsWith(github.ref, 'refs/tags/')` and leaves the `pull_request` trigger in place. GitHub's expression docs apply an implicit `success()` to an `if` that does not name a status function, so a failed test still skips publish. `ganda repo audit` passes, including `workflow-file`. `ProcessIsolationTests` is a theory over `--capabilities` and all 29 `[Operation]` routes; required members are strings, ints, or `IReadOnlyList<MultiQueryOperation>`, writes send `checkOnly`, and the run was 31 passed (30 routes plus the deps fact). `RequestDiagnosticsAsync` disposes the cancellation registration in `AwaitThenUnregisterAsync` after the completion finishes, and the cancel-behind-a-long-write test passed. M1 stays fixed. The Goal restatement's differences from the inbound checklist are already under `## Learnings`. No new findings.

## Issues

<!-- None. -->

## Resolved prior

### M1 — Severity: suggestion — Status: fixed
- File: AGENTS.md:182
- Description: Round 1 asked the MCP-contracts paragraph to drop `OperationHandler.ToEngine` and the app-side contract copy, and to tie the JSON round-trip rule to Nuru binding.
- Re-check: `ToEngine` is absent from `AGENTS.md`, `README.md`, and `skills/roslynk/SKILL.md`. AGENTS.md:182 names System.Text.Json binding of `--json-args` and MCP arguments onto the engine request type, and says the app has no contract copy. `FromCaller` still mutates that request in place.
- Status: fixed
