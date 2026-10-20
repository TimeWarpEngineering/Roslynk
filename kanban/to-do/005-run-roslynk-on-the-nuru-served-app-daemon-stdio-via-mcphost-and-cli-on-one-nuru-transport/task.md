# Run Roslynk on the Nuru served app: daemon, stdio via McpHost and CLI on one Nuru transport

## Description

Fork issue #8 (release note bullet ends `(Fixes #8)`).

Decision (Steven, 2026-10-20): the fork aims for the cleanest architecture and is the proving ground for
TimeWarp.Nuru's served-app design. Byte-identical MCP schema and text are **no longer a constraint**; MCP
clients are models that read `tools/list` on every connect. Upstream (Pete) cherry-picks later.

Today (after 003, 003-001, 004): `roslynk` is one Nuru app from `Morris.Roslynk.Cli`, packed as the `Roslynk`
tool. Its 29 operation routes forward to a hand-written ASP.NET MCP-over-HTTP daemon (`RoslynkDaemon.Run` in
`Morris.Roslynk.Mcp`, `WithRoslynkTools`, loopback port 6502) through `ForwardingHandler` + `DaemonForwarder` +
`DaemonStarter`; `roslynk stdio` is the old stdio-to-HTTP bridge; `VerbatimTerminal` rewrites Nuru's output.

Target:

- **Daemon:** the Nuru served app (`TimeWarp.Nuru.Daemon`, `AddServeRoute`) runs the real handlers in the
  daemon process: 29 thin handlers in a host project (not the engine) that adapt each operation request to its
  engine core (pinned `SolutionModel`, `RoslynInstance`, `ResponseBudget` where the core takes one), registered
  through the generated mediator. The engine stays free of Nuru. Instance idle eviction maps onto (or stays
  beside) Nuru's idle timeout; record which.
- **MCP:** `roslynk stdio` is `TimeWarp.Nuru.McpHost`, whose tool calls forward to the daemon through the
  app's `RouteForwarder` (508-004/005 already do this), so all MCP sessions and CLI calls share one warm
  workspace.
- **CLI:** operation routes forward through the Nuru forwarder. Delete `ForwardingHandler`, `DaemonForwarder`,
  `DaemonStarter`/`DaemonStartInfo`, `VerbatimTerminal` (508-011), the ModelContextProtocol client reference,
  and the ASP.NET host (`Morris.Roslynk.Mcp`, `WithRoslynkTools`, `LoopbackOnlyExtensions`, port config), unless
  the Aspire AppHost needs a shim; record what the AppHost runs now.
- **MCP surface:** whatever McpHost derives from the contracts. Server instructions (`McpServerRegistration
  .Configure`) and tool descriptions carry over through McpHost's options. Update the tool descriptions and
  skill where the text or error format changed.

## Requirements

1. Concurrency: two MCP sessions and a CLI call run concurrently against one warm solution; a long
   `get_diagnostics` does not block a concurrent `get_symbol`; `multi_query` still runs its operations in
   parallel. (Needs nuru 508-016.)
2. Output: success text and the `error=` outline for problems are what the tool descriptions document
   (needs nuru 508-011); exit-code table unchanged for the CLI.
3. Schema: `multi_query.operations` and `apply_patch.baseVersions` publish real JSON schema; write tools carry
   `destructiveHint`, all tools `openWorldHint: false` and a title (needs nuru 508-015). Replace the pinned MCP
   schema tests with tests on names, descriptions, required members, annotations and behaviour; regenerate
   any snapshot in the same PR.
4. Cancellation: an MCP cancel or Ctrl-C stops a long call.
5. Self-start parity: `roslynk stdio` or any CLI command with no daemon starts one; one line + exit 69 when it
   cannot.
6. Timings against 003/004: cold `--capabilities` (~184-191 ms), warm forwarded `get_symbol` (~359 ms median of
   five), plus 8 parallel forwarded calls. The Nuru socket transport should beat MCP-over-HTTP; record it.
7. Packaging: the `Roslynk` tool package no longer carries ASP.NET or the Mcp launcher leftovers flagged in the
   PR #6 review; record the size against 16.7 MB.
8. Exit code 1 ambiguity from the PR #5 review: give an unknown daemon `error=` code its own exit code, distinct
   from a usage error; document it.
9. Docs: README (install, MCP config is still `roslynk stdio`), AGENTS.md (hosting and CLI sections, repository
   map, "Hosting and release touchpoints"), SKILL.md, `releases.md` Unreleased bullet `(Fixes #8)`.

## Non-goals

- Engine behaviour changes; new tools.
- Upstream PR to Pete.
- Releasing a package (the `Roslynk` NuGet id is Pete's; a fork release is a separate decision).

## Checklist

- [ ] Pins bumped to TimeWarp.Nuru 4.0.0-beta.4 (carries 508-011, 508-015, 508-016); Daemon and McpHost packages added
- [ ] Host-project handlers adapting the 29 operations to their engine cores
- [ ] Daemon on the Nuru served app; idle eviction decision recorded
- [ ] `stdio` on McpHost forwarding to the daemon; server instructions carried over
- [ ] CLI on the Nuru forwarder; hand-written forwarding, daemon start, verbatim terminal and ASP.NET host deleted
- [ ] Tests per requirements 1-5 and 8; MCP schema tests rewritten
- [ ] Timings and package size in Results
- [ ] Docs and `(Fixes #8)` bullet

## Session

- Created: 2532391 (2026-10-20) by the Roslynk cockpit after Steven's decision to move onto the Nuru served app.
- 2026-10-20 cockpit (2532391): **unblocked**. TimeWarp.Nuru **4.0.0-beta.4** (all six packages, indexed) ships
  508-011, 508-015, 508-016 (and 508-009, nested enums keep allowed values). Contracts stays 2.0.0-beta.23. APIs to use
  (from the nuru `kanban/done/508-01x` Results):
  - **508-011:** `UseVerbatimRendererOutput()` on the builder writes renderer text with `Write` (no added newline)
    and problem-renderer text to stderr. The problem renderer is `IResponseRenderer<SharedProblemDetails>` (the
    contracts interface): implement one that emits today's `error=` outline (`ProblemOutline.Format`) and register it.
    Exit code still follows `SharedProblemDetails.Status`; map the error codes to the documented table there.
    Then delete `VerbatimTerminal`.
  - **508-016:** the daemon and McpHost run calls concurrently by default with a per-call `NuruCallContext`
    (terminal, working directory, exit code). Use `NuruCallContext.GetFullPath` / `CurrentDirectory` for any
    client-relative path (e.g. `open_solution` with a relative path), never `Environment.CurrentDirectory`.
    `[Exclusive]` / `.Exclusive()` exists for routes that must serialize; Roslynk should not need it (the engine
    serializes writes itself). The DI-injected `ITerminal` follows the current call.
  - **508-015:** non-scalar members publish real JSON schema from the binder's type info; defaults appear as both
    JSON `default` and a `Default: <json>.` description sentence. Annotations: McpHost reads `[DisplayName]` (BCL,
    fine on engine requests) for `title`, and Nuru's `[Destructive]` / `[OpenWorld]` from the request class. **Do not
    put Nuru attributes on the engine's request classes** (the engine stays free of Nuru); set `destructiveHint` /
    `openWorldHint` through `NuruMcpHostOptions.ToolAnnotations[toolName]` in the host, derived from the operation
    kind (commands destructive, all tools `openWorld: false`). timewarp-architecture 391 asks Contracts for a
    neutral home for these hints; switch to it when it ships.
  - Pins: `TimeWarp.Nuru` 4.0.0-beta.4 in both props files, `TimeWarp.Nuru.DevCli` 4.0.0-beta.4 in the root, add
    `TimeWarp.Nuru.Daemon` and `TimeWarp.Nuru.McpHost` 4.0.0-beta.4 to `Source/Directory.Packages.props`.

## Notes

- Was blocked on timewarp-nuru 508-011, 508-015 and 508-016; all released in TimeWarp.Nuru 4.0.0-beta.4 (2026-10-20).
- Supersedes the "revisit McpHost" note in Roslynk 004 (PR #6) and the transport section of AGENTS.md "CLI
  front end".
- Related: Roslynk 003 (PR #5), 003-001 (PR #7), 004 (PR #6); nuru 508-004, 508-005, 508-010, 508-013.
