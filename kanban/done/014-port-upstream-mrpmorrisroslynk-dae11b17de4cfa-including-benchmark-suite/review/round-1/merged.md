# Round 1 — merged findings
**Date:** 2026-11-09
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 1 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: open
- File: source/App/Morris.Roslynk.Cli/Handlers/OperationHandler.cs:74
- Description: `ReadAsync` now runs `QueryExecution`, so a mid-query cancellation or a superseded snapshot becomes `error=Stale` (exit 6) with a `snapshot=<id>` header. No fork test covers the handler path: the round trip through `ProblemOutline.TryParse`/`FromText` into `SharedProblemDetails` and the stderr outline. The only handler cancellation test uses an already-cancelled token, which still throws before the query runs.
- Suggestion: Add a `HandlerTests` unit (or a CLI process test) that cancels mid-query or supersedes the snapshot. Assert `Status == 6`, `Type == "Stale"`, and `snapshot=` in the rendered problem.
- Source: general
- Disposition notes:

### M2 — Severity: nit — Status: open
- File: AGENTS.md:63
- Description: The diagnostics cancellation sentence ("the consumer drops an item whose completion is already done") describes the fork's pre-merge mechanism. The merge resolution took upstream's `WaitAsync` plus the token check in `RunDiagnosticsAsync`.
- Suggestion: Reword to say that `RequestDiagnosticsAsync` returns `completion.Task.WaitAsync(token)` and the consumer skips an item whose token is already cancelled.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None (single reviewer).
