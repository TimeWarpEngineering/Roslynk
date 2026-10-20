# Round 1 — general
**Date:** 2026-10-20
**Scope reviewed:** branch `task/005-run-roslynk-on-the-nuru-served-app-daemon-stdio-vi` vs `master` — Nuru served app, handlers, forwarder, stdio MCP host, exit codes, engine cancellation plumbing, AppHost, packaging, docs

## Summary

`roslynk` (`Morris.Roslynk.Cli`) is now the CLI, the served daemon, and the MCP server. `AddServeRoute(idleTimeoutSeconds: 3600, autoStart: true)` runs the 29 handlers in `roslynk serve`. `RoslynkForwarder` forces every operation call onto that server and maps a start or answer failure to one line and exit 69. `roslynk stdio` is a local-only Nuru McpHost route whose tool calls use the same forwarder. The engine has no TimeWarp.Nuru reference. Idle eviction stays beside Nuru's process idle stop (30 minutes per solution, hour-long process idle), and the AppHost runs `roslynk serve` on `roslynk-apphost.sock`.

The implementer restatement matches the original task: same daemon, MCP, CLI, schema, cancellation, self-start, timings, packaging, exit 70, and docs. No misread.

Checked against the source and against TimeWarp.Nuru 4.0.0-beta.4 (daemon client, server cancel-on-disconnect, McpHost invoker). The built `roslynk.runtimeconfig.json` asks only for `Microsoft.NETCore.App`. `ProblemRenderer` is what the generated interceptor writes for `SharedProblemDetails`. Handler copy, exit-code mapping (including 70), budget cut, and an already-cancelled token reaching `get_symbol`, `get_diagnostics`, and `multi_query` were re-run (`HandlerTests`, 42 passed). The full 283-test CLI suite was not re-run in this round; the new tests assert the requirement checks (shared server, non-blocking `get_symbol`, schema, exit 69, outlines).

## Issues
