# Round 1 — merged findings
**Date:** 2026-10-19
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: Source/App/Morris.Roslynk.Cli/Forwarding/DaemonStarter.cs:159
- Description: `StartDetached` passes `Roslynk__Port` only in the non-Windows branch (`DaemonStarter.cs:189`). The Windows branch uses `UseShellExecute = true`, which cannot set `ProcessStartInfo.Environment`, and it never records the port anywhere else. `CliSettings` resolves the port from appsettings beside the CLI, then the environment. The daemon is a different executable with its own appsettings and reads `Roslynk:Port` in `LoopbackOnlyExtensions`. On Windows, a non-default port that exists only in the CLI's appsettings makes the CLI wait on that port while the child binds 6502, then the 30s startup message fires and an orphan daemon is left on the default port. A port that is already in the parent environment still works, because shell execute inherits the parent block. Unix covers both sources; the comment at line 188 says the appsettings case is why the port is handed over. `DaemonLauncher` does not need this hand-off (it re-runs itself). This launcher does.
- Suggestion: Start the Windows daemon with a launcher that both detaches (the child must outlive the CLI) and accepts an environment block, and set `Roslynk__Port` to the resolved port the same way the Unix branch does. Keep stdout/stderr out of the CLI. Add a test that a non-default resolved port is present on the start info for both launchers (a pure builder test can run on Linux).
- Source: general
- Disposition notes:

## Duplicates / conflicts

- One reviewer. Nothing to collapse.
