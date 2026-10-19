# Round 2 — general
**Date:** 2026-10-19
**Scope reviewed:** `7e1203b..3d2a3ba` (M1 fix). `Forwarding/DaemonStartInfo.cs`, `Forwarding/DaemonStarter.cs`, `Morris.Roslynk.Cli.Tests/DaemonStartInfoTests.cs` and its csproj link, the AGENTS.md forwarding paragraph, the README port sentence.

## Summary

The fix moves the detached start into `DaemonStartInfo.Create` and passes the resolved port as `--Roslynk:Port=<port>` on both launchers. That argument is a separate argv entry, so a port that exists only in the CLI's appsettings reaches a daemon whose own appsettings and environment would otherwise keep 6502. `WebApplication.CreateBuilder(args)` in `Morris.Roslynk.Mcp/Program.cs` already ranks command-line configuration above appsettings and environment, and `AddLoopbackOnlyKestrel` reads `Roslynk:Port` from that builder. The Unix branch still detaches with `nohup` and appends the daemon's stdout and stderr to the log. The Windows branch still uses shell execute with a hidden window, which keeps the daemon's output off the CLI, and now puts the same port argument on `ArgumentList` the way `DaemonLauncher` already passes a framework-dependent assembly.

Re-checked on this Linux host: `DaemonStartInfoTests` 6/6 (both start infos, the real `/bin/sh` start with a path that contains spaces, and `CreateBuilder` reading the argument as 6599) and `Morris.Roslynk.Cli.Tests` 149/149. The Windows process was not launched here. The agreed check for that launcher is the start-info builder, and it matches the port argument, shell execute, and hidden window.

The fix-pass restatement matches M1. The earlier beta.1 acceptance-line misread is unchanged and already recorded under Learnings. No new findings.

## Resolved prior

### M1 — Severity: bug — Status: fixed
- File: Source/App/Morris.Roslynk.Cli/Forwarding/DaemonStartInfo.cs:18
- Description: Round 1: `StartDetached` passed `Roslynk__Port` only on the non-Windows branch. Both launchers now add `PortArgument(port)` (`--Roslynk:Port=<port>`). The Unix script is `nohup "$@" >>"$0"`, with the log as `$0` and the executable, optional assembly, and port as `"$@"`. The Windows start keeps `UseShellExecute` and `WindowStyle.Hidden` and appends the assembly (when present) and the port to `ArgumentList`.
- Suggestion: none remaining.
- Status: fixed
