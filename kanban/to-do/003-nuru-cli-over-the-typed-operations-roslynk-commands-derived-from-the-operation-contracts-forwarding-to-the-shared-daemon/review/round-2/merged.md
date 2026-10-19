# Round 2 — merged findings
**Date:** 2026-10-19
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: Source/App/Morris.Roslynk.Cli/Forwarding/DaemonStartInfo.cs:18
- Description: Round 1: `DaemonStarter.StartDetached` passed `Roslynk__Port` only on the non-Windows branch, so a Windows auto-start ignored a non-default port that came from the CLI's appsettings. The fix builds both starts in `DaemonStartInfo.Create` and passes `--Roslynk:Port=<port>` on each. The daemon's `WebApplication.CreateBuilder(args)` ranks that argument above its appsettings and environment. The Unix start still redirects the daemon to the log and backgrounds it. The Windows start stays shell-executed and hidden. Verified here: `DaemonStartInfoTests` 6/6 and `Morris.Roslynk.Cli.Tests` 149/149.
- Suggestion: none remaining.
- Source: general
- Disposition notes: Fixed in `3d2a3ba`. Round 2 re-checked the delta and the tests, including the resumed pass (`DaemonStartInfoTests` 6/6, `Morris.Roslynk.Cli.Tests` 149/149). No regression on the fix.

## Duplicates / conflicts

- One reviewer. M1 is carried from round 1. No new findings to collapse.
