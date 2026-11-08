# Round 2 — merged findings
**Date:** 2026-11-09
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/App/Morris.Roslynk.Cli.Tests/HandlerTests.cs:123
- Description: No fork test covered `OperationHandler.ReadAsync` turning a mid-query cancellation or a superseded snapshot into `error=Stale` with `snapshot=`.
- Source: general (round 1)
- Disposition notes: Fixed in `fb81e1c3`. `WhenACapturedReadIsCancelledOrSuperseded_ThenTheHandlerReturnsStaleWithTheSnapshot` asserts Stale, status 6, the `snapshot` extension header and the rendered outline. It passes in this worktree (HandlerTests 44/44).

### M2 — Severity: nit — Status: fixed
- File: AGENTS.md:63
- Description: The diagnostics cancellation sentence described the pre-merge mechanism.
- Source: general (round 1)
- Disposition notes: Fixed in `fb81e1c3`. The sentence now names `completion.Task.WaitAsync(token)` and the consumer's skip of a cancelled item. Both were checked against `RoslynInstance.cs:272` and `:343-344`.

## Duplicates / conflicts

- None. No new findings in round 2.
