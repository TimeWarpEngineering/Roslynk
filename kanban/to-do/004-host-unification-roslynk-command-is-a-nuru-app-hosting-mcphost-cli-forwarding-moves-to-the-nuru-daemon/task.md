# Host unification: roslynk command is a Nuru app hosting McpHost, CLI forwarding moves to the Nuru daemon

## Description

Follow-up (c) of task 003 (fork issue #4). 003 shipped `roslynk-cli` (`Source/App/Morris.Roslynk.Cli`): a
TimeWarp.Nuru 4.0.0-beta.2 app whose 29 commands are derived from the `[Operation]` contracts (linked
sources, no engine reference) and whose one generic forwarding handler sends each call to the existing
`Morris.Roslynk.Mcp` daemon as an MCP `tools/call` over loopback streamable HTTP. It is deliberately a second
executable that is not packaged, and its forwarding is Roslynk-specific, so it did not pre-empt
timewarp-nuru 508-004 (`TimeWarp.Nuru.McpHost`) or 508-005 (served apps: `TimeWarp.Nuru.Daemon`, already in
4.0.0-beta.2 as `AddServeRoute` / `INuruRouteForwarder`).

Unify the hosts once 508-004 is released:

- The `roslynk` tool command becomes one Nuru app: the CLI routes, an `mcp` / `stdio` route served by
  `TimeWarp.Nuru.McpHost` (tools derived from the same catalog, byte-identical to today's MCP text and
  schema, `WithRoslynkTools` behaviour preserved: defaults moved into descriptions, binding errors as
  `Invalid`, the response-budget backstop), and the daemon.
- Replace 003's `ForwardingHandler` + `DaemonStarter` + `VerbatimTerminal` with the generic Nuru daemon
  forwarding (508-005) and verbatim rendering (508-011), or record why Roslynk keeps MCP-over-HTTP as its
  daemon transport (warm workspaces must stay shared between MCP and CLI sessions; session ref-counts).
- Drop the linked-source workaround when timewarp-nuru 508-013 discovers `[Operation]` endpoints in
  referenced assemblies; apply groups when 508-012 (or timewarp-architecture 388) lands.
- Packaging: decide how `roslynk` ships the CLI. Options recorded by 003: (1) one `Roslynk` tool package,
  command `roslynk`, CLI routes beside `stdio`/daemon (needs this unification; preferred); (2) a second tool
  package (`Roslynk.Cli`, command `roslynk-cli`) beside the MCP host, as 003 builds it today (cheap, two
  commands to install, version skew possible); (3) ship `roslynk-cli` inside the `Roslynk` package's tool
  folder and have `roslynk` dispatch unknown verbs to it (no new package, but two apps in one folder and
  the daemon's dependency closure loaded for a CLI call unless dispatched by process).
- Then the one-line skill of the contracts-first plan (`skills/roslynk/SKILL.md` points at the CLI).

## Checklist

- [ ] 508-004 released (TimeWarp.Nuru.McpHost 4.0.0-beta.2, 2026-10-19); decide the transport question above with measurements (003: cold `--capabilities`
      ~184 ms, warm forwarded call ~359 ms median of five, against ~26 ms in-daemon per 506)
- [ ] `roslynk` = Nuru app hosting McpHost + CLI routes + daemon; MCP text and schema byte-identical
      (McpTests and `Morris.Roslynk.Cli.Tests` pass)
- [ ] Packaging option chosen and the CLI ships; README and AGENTS.md "CLI front end" updated
- [ ] Linked-source workaround and VerbatimTerminal removed when 508-013 / 508-011 allow
- [ ] Skill rewritten to the CLI one-liner; `releases.md` bullet
- [ ] Carry the three low findings of the PR #5 independent review (below) into whichever forwarding path
      survives: catch `TaskCanceledException` / `JsonException` as exit 69 (not a stack trace), give a
      forwarded call an overall timeout so a daemon that accepts then stalls cannot hang the CLI, and add exit
      code 10 (Truncated) to the README table (code and AGENTS.md already list it)

## Session

- Created: 563407 (2026-10-19) by the 003 implementer as that task's host-unification follow-up.
- 2026-10-19 cockpit (2532391): 003 merged as PR #5 (c359733, issue #4 closed), which brings the TimeWarp.Nuru
  4.0.0-beta.2 pin to master; merged master into this branch and published the kitchen.
- 2026-10-19 cockpit, dispatch: timewarp-nuru 508-004 (McpHost), 508-005 (Daemon: `AddServeRoute`,
  `INuruRouteForwarder`) and 508-006 are merged and all six TimeWarp.Nuru packages are 4.0.0-beta.2 on nuget.org
  (`TimeWarp.Nuru.McpHost`, `TimeWarp.Nuru.Daemon` included), so the first checklist item is met and this task is
  unblocked. timewarp-architecture released 2.0.0-beta.22 the same day: package-pin updates only (task 389), no
  operation group yet; architecture 388 ("Operation group on [Operation]", contract-only per Steven) is specified in
  to-do, not implemented, so ship ungrouped again and leave the group checklist item as a follow-up. Bumping
  `TimeWarp.Contracts` to 2.0.0-beta.22 in `Source/Directory.Packages.props` is optional housekeeping (no API change).
  The Nuru docs for McpHost / Daemon are in the timewarp-nuru repo `documentation/user/features/` (neutral-contracts,
  agent-invocation, and the 508-004/508-005 kitchens under `kanban/done/`).

## Notes

- PR #5 independent review (Sonnet, 2026-10-19), verdict merge with noted issues, none blocking:
  `DaemonForwarder.cs` catches `HttpRequestException` / `McpException` / `IOException` / `TimeoutException`
  only, so an `HttpClient` timeout (`TaskCanceledException`) or a malformed daemon reply (`JsonException`)
  would crash instead of exiting 69; no overall per-call timeout; README exit-code list stops at 9 and 69
  (10 Truncated missing); exit 1 covers both a usage error and an unknown `error=` code, so a script cannot
  tell a typo from a new daemon error code (documented in AGENTS.md; consider a distinct code here).
- Related: Roslynk 003 (CLI), timewarp-nuru 508-004 (McpHost), 508-005 (served apps), 508-011 (verbatim /
  problem renderer), 508-012 (app-side groups), 508-013 (referenced-assembly discovery),
  timewarp-architecture 388 (`Group` on `CliOperationBinding`).
