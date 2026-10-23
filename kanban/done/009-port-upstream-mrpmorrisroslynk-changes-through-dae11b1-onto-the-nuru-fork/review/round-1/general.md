# Round 1 — general
**Date:** 2026-10-23
**Scope reviewed:** branch `task/009-port-upstream-mrpmorrisroslynk-changes-through-dae` vs `master`. Merge `2eaee8f` of `dae11b1`, then the Nuru adaptation in `e48647d`, `5302710`, and `4dd16d1`. Engine bodies were compared with `dae11b1`. The goal restatement matches the original task.

## Summary

The port is a real merge: `git merge-base HEAD dae11b1` is `dae11b148c3b79cd1c9b2cfb3067247df3a1e386`, the upstream remote push URL is `DISABLED`, and the engine delta against `dae11b1` is the fork's operation contracts plus the pre-existing queued-cancel path. Host items land where the task asked: settings and the example file in the Nuru app, instructions in `McpServerRegistration`, exit code 11 for `UnknownArgument`, and documented drops for the HTTP restart and the daemon version handshake. Nuru's generated configuration already uses `AppContext.BaseDirectory` with `reloadOnChange: false`, so dropping the install-dir content root is sound. One served write handler does not pass the new load-wait token, and the README settings section keeps a stale bullet list under the new table.

## Issues

### Issue 1 — Severity: bug
- File: Source/App/Morris.Roslynk.Cli/Handlers/OperationHandlers.cs:71
- Description: `change_signature` no longer cancels during the load wait that #119 added. `ChangeSignatureTool.HandleAsync` now takes a `CancellationToken` and passes it to `InstanceRegistry.GetOrBeginAsync`, which waits up to `Roslynk:LoadWaitSeconds` (default 30s). `ChangeSignatureHandler` still calls `HandleAsync(FromCaller(command))`, so the token stays at its default and a cancelled CLI or MCP call keeps waiting. Every other write handler passes `cancellationToken`. Before this merge the method had no token parameter and `GetOrBeginAsync` did not wait (`fac24b5` has no `WaitForInitialLoad`), so the old call site still compiles. The task requires that cancellation still work once the wait is on the served path, and `AGENTS.md` says write handlers pass the call's token.
- Suggestion: Pass `cancellationToken` into `ChangeSignatureTool.HandleAsync`, as `RenameSymbolHandler` and `ExtractMethodHandler` do.
- Status: open

### Issue 2 — Severity: nit
- File: README.md:363
- Description: The new Settings table lists `MaxResponseChars`, `LoadWaitSeconds`, `IdleEvictionMinutes`, and `EvictionCheckMinutes`. The three bullets under "A shared server serves every client…" repeat the first, third, and fourth keys and omit `LoadWaitSeconds`.
- Suggestion: Delete those three leftover bullets. The table and the paragraph above them already say what they say.
- Status: open
