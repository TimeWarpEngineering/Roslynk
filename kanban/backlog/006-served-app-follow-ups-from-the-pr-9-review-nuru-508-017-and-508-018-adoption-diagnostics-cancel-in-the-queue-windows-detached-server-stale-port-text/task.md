# Served-app follow-ups from the PR 9 review: Nuru 508-017 and 508-018 adoption, diagnostics cancel in the queue, Windows detached server, stale port text

## Description

Follow-ups from the independent review of PR #9 (task 005, fork issue #8; verdict merge with noted issues).
No new GitHub issue: extend the existing Unreleased `(Fixes #8)` bullet in `releases.md`.

- **Build thrash (medium, Nuru).** Two builds of `roslynk` sharing one socket stop each other's server and cancel
  in-flight calls (Nuru daemon client stops a server whose build id differs). Fixed upstream by timewarp-nuru
  **508-017**; adopt its model here (pin, docs: README/AGENTS.md say what happens with `dnx Roslynk -- stdio` beside a
  global tool).
- **MCP binding errors (low-medium, Nuru).** A missing or wrong argument over MCP returns Nuru's plain
  `Error: ...` text, not the `error=Invalid` outline the server instructions promise. Fixed upstream by
  timewarp-nuru **508-018**; adopt it, and add an MCP test (missing `symbolName` on `get_symbol` gives
  `error=Invalid`).
- **Diagnostics cancel in the queue (low).** `RoslynInstance.RequestDiagnosticsAsync` returns the queued completion;
  the token is only observed when the single consumer runs the item, so a cancelled `get_diagnostics` behind a long
  write keeps its call open. Let the awaiting handler stop waiting on cancellation (and let the queued item skip if
  already cancelled). Test with a controlled long write.
- **Windows detached server (read-only finding).** The Nuru launcher uses `setsid` on Linux only; on Windows the
  detached server is a plain redirected child and may die with the client's console or job object. Verify on
  Windows or file it to Nuru; record which. Note in README that the old Windows-service hosting option is gone.
- **Stale text.** `Source/TestFixtures/Benchmarks/Benchmarks.md:5` still says JSON-RPC at `127.0.0.1:6502`.

## Checklist

- [ ] TimeWarp.Nuru release with 508-017 and 508-018 pinned; behaviours adopted and documented
- [ ] MCP binding-error test gives `error=Invalid`
- [ ] Diagnostics cancellation does not wait on the queue; test
- [ ] Windows detached server verified or filed upstream
- [ ] Benchmarks.md fixed
- [ ] `releases.md` (Fixes #8) bullet extended

## Session

- Created: 2532391 (2026-10-21) by the Roslynk cockpit from the PR #9 review.

## Notes

- Blocked on a TimeWarp.Nuru release carrying timewarp-nuru 508-017 and 508-018 for the first two items; the
  other three can be done first if useful.
- Environment note from the review: CLI tests fail (143 of 283) when `/tmp/nuget.config` is a dangling symlink;
  run with `TMPDIR` elsewhere or remove the stray symlink.
