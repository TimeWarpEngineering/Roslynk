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

## Goal (implementer restatement)

Make the installed `roslynk` command one Nuru app. It carries the 29 operation commands of
`roslynk-cli`, the `stdio` MCP bridge and the HTTP daemon. It ships as the one `Roslynk` tool package
(packaging option 1). The MCP surface stays byte-identical.

The task says to adopt `TimeWarp.Nuru.McpHost` and the Nuru daemon (508-004/005), or to record why
Roslynk keeps MCP-over-HTTP as its daemon transport. Decide this with measurements: the schema McpHost
would publish against the pinned schema, call concurrency, MCP session ref-counts, and the forwarded-call
cost. Then implement the transport that survives.

Acceptance, in my words:

- `roslynk --capabilities` lists the 29 operations. `roslynk <operation> ...` forwards to the daemon as
  003's `roslynk-cli` did: same bytes, same exit codes. `roslynk stdio` is today's bridge. `roslynk`
  with no arguments (or `roslynk daemon`) is today's daemon, so existing MCP configs and the daemon's
  self-start keep working.
- The MCP text and schema do not change. McpTests and the CLI tests pass. A CLI call loads no Roslyn
  assembly and no engine assembly.
- The `Roslynk` package is packed from the unified app. README, AGENTS.md "CLI front end" and the CI
  pack step are updated.
- `VerbatimTerminal` and the linked sources stay until 508-011 / 508-013 ship. Record that, and record
  that groups stay deferred (architecture 388 is not released).
- The PR #5 low findings: `TaskCanceledException` / `JsonException` exit 69 with one line, a forwarded
  call has an overall timeout, and the README exit-code table lists 10 (Truncated).
- `skills/roslynk/SKILL.md` gets the CLI one-liner, and `releases.md` gets an Unreleased bullet.
- `bin/dev build`, the tests and `ganda repo audit` are green.

## Checklist

- [x] 508-004 released (TimeWarp.Nuru.McpHost 4.0.0-beta.2, 2026-10-19); decide the transport question above with measurements (003: cold `--capabilities`
      ~184 ms, warm forwarded call ~359 ms median of five, against ~26 ms in-daemon per 506). Decided: keep
      MCP-over-HTTP and `WithRoslynkTools`, and do not adopt McpHost or the Nuru daemon (measurements in Results)
- [x] `roslynk` = Nuru app with the CLI routes, `stdio` and the daemon. The MCP surface is still served by
      `WithRoslynkTools`, not McpHost, so MCP text and schema stay byte-identical (McpTests 30/30,
      `Morris.Roslynk.Cli.Tests` 154/154, and a stdio client lists the published tools byte for byte)
- [x] Packaging option chosen and the CLI ships (option 1: one `Roslynk` tool, command `roslynk`); README and AGENTS.md "CLI front end" updated
- [x] Linked-source workaround and VerbatimTerminal removed when 508-013 / 508-011 allow. They do not allow it
      yet (both unreleased), so both stay and are recorded in AGENTS.md. Groups stay deferred to
      architecture 388 / nuru 508-014 (task 003-001)
- [x] Skill rewritten to the CLI one-liner; `releases.md` bullet
- [x] Carry the three low findings of the PR #5 independent review (below) into whichever forwarding path
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

- 2026-10-19 implementer walk 1 (implementer-claude, claude-opus-5-5):
  - Measured the McpHost route with a throwaway probe outside the repo:
    `NuruMcpToolCatalog.CreateTools` over `roslynk-cli --capabilities`, compared with
    `AddRoslynk` + `WithRoslynkTools`. The 29 names and 29 tool descriptions match. These do not match:
    0/29 input schemas, 0/29 annotation sets, and 70/139 property descriptions (52 defaults move to
    `default`). 11/139 property types differ. The Nuru daemon serializes calls (`CallGate`), and it would
    be a second warm process beside the HTTP daemon. Decision: keep the transport (Results).
  - Moved the daemon composition out of `Morris.Roslynk.Mcp/Program.cs` into `Hosting/RoslynkDaemon.Run`.
    `Program.cs` keeps `stdio`, accepts `daemon`, and stays the AppHost / `dotnet run` entry point. The Mcp
    project is no longer packable.
  - `Morris.Roslynk.Cli` is now assembly `roslynk` and the `Roslynk` tool package (PackAsTool, command
    `roslynk`). It references `Morris.Roslynk.Mcp` / `Morris.Roslynk` under the extern aliases `host` /
    `engine`, so the linked contract types never clash with the engine's copies. `Hosting/HostEntry.cs` is
    the only type that names the host, through `NoInlining` methods. Routes: `""` and `daemon {*settings}`
    run the daemon, and `stdio` runs the bridge. Nuru strips `--Section:Key=value` overrides from route
    arguments, so the daemon reads `Environment.GetCommandLineArgs()` itself.
  - `DaemonStarter` now starts this same command as the daemon (apphost, or `dotnet roslynk.dll`) unless
    `Roslynk:Cli:Daemon` is set. The sibling-build and PATH discovery are gone. Messages say `roslynk:`.
  - PR #5 findings: added `Roslynk:Cli:TimeoutSeconds` (default 300, 0 = none) as one window over connect
    and call. Unrequested `OperationCanceledException` (an HttpClient timeout) and `JsonException` now
    exit 69 with one line. The README has the full exit-code table, including 10.
  - Tests: `HostRoutesTests` (new: daemon and bare routes on a given port, a stdio client sees the
    published tools byte for byte, auto-start of itself), a timeout test in `OutputContractTests`, the
    capabilities test (29 operations plus three host routes), and the process-isolation test (still no
    Roslyn, engine or host loaded for an operation call; the deps now carry them by design).
  - CI packs `Morris.Roslynk.Cli`. README "Command line", AGENTS.md "CLI front end" / repo map / release
    line, the SKILL.md one-liner, and the `releases.md` #4 bullet are rewritten for the shipped command.
  - Boy-scout: `ganda repo audit` warned about `vscode-window-icon` (gitignored `.vscode` files);
    `ganda repo audit --fix` regenerated them, and the audit passes all checks.

## Results

`roslynk` is now one Nuru app, and it ships as the one `Roslynk` tool package (packaging option 1). It
has the 29 operation commands that 003 built as `roslynk-cli`, `roslynk stdio` (the MCP bridge, unchanged),
and `roslynk` / `roslynk daemon` (the HTTP daemon, unchanged). A command with no daemon listening starts
this same tool as the daemon. The MCP surface is still `WithRoslynkTools`, so its text and schema are
byte-identical by construction. A test checks this through the stdio route.

### Transport decision: keep MCP-over-HTTP, do not adopt McpHost or the Nuru daemon

The probe derived McpHost's tools from the command's own catalog (`NuruMcpToolCatalog.CreateTools`) and
compared them with the published tools:

| Published field | McpHost equal | Difference |
| --- | --- | --- |
| Tool name | 29/29 | none |
| Tool description | 29/29 | none |
| Input schema | 0/29 | `additionalProperties: false`, Nuru property order |
| Annotations | 0/29 | `title`, `destructiveHint: false/true`, `openWorldHint` dropped |
| Property description | 69/139 | 52 defaults move to `default`; `WithRoslynkTools` puts them in the description |
| Property type | 128/139 | `multi_query.operations` becomes a string, and nullable members lose `null` |

Text: McpHost returns Nuru's rendering. Nuru 4.0.0-beta.2 adds a newline to a response and writes a
problem as `"{Type} {Title}"` + `Detail`, not the `error=` outline (508-011 is not released).

Daemon: `TimeWarp.Nuru.Daemon` runs one call at a time behind a process-wide `CallGate` over a Unix socket.
It would be a second warm process beside the HTTP daemon that MCP clients use, unless MCP also went
through it and was serialized. Its server also needs the real handlers in the same app. Roslynk's engine
handlers take the pinned `SolutionModel` and `RoslynInstance`, not a mediator request.

Correction to the description: the daemon has no per-session solution ref-counts. Eviction is
idle-time based (`IdleEvictionService`), so ref-counts were not a factor. The deciding factors are the
schema and annotation divergence, the text rendering, and serialized calls.

Revisit this when McpHost can publish a host-supplied tool list or when 508-011 ships. The
`ForwardingHandler`, `DaemonStarter` and `VerbatimTerminal` stay, and AGENTS.md records why.

### Timings

Release apphost, Linux/WSL2, median of five after one warm-up, warm daemon with SimpleSolution loaded:

| Command | Median | Runs (ms) | 003 (`roslynk-cli`) |
| --- | --- | --- | --- |
| `roslynk --capabilities` | 191 ms | 194, 190, 189, 192, 191 | 184 ms |
| `roslynk --help` | 168 ms | 167, 168, 172, 176, 167 | 176 ms |
| `roslynk get_symbol ...` (forwarded) | 359 ms | 368, 359, 368, 347, 347 | 359 ms |
| `roslynk get_solution_status` (forwarded) | 338 ms | 334, 338, 354, 339, 332 | 368 ms |

Carrying the host and the engine in the same app costs nothing measurable, because neither loads for an
operation call (`ProcessIsolationTests`).

### Packaging

`dotnet pack Source/App/Morris.Roslynk.Cli` produces `Roslynk.<version>.nupkg` with command `roslynk`. I
installed it with `dotnet tool install --tool-path` and checked it on Linux:

- `--capabilities` lists 32 endpoints (29 operations plus `""`, `daemon {*settings}` and `stdio`).
- A forwarded call on a free port started the installed tool as the daemon (`roslynk --Roslynk:Port=6641`)
  and exited 0.
- `roslynk stdio` on another free port started the daemon (bare `roslynk`), listed 29 tools and answered
  `tools/call`.

The package also carries the `Morris.Roslynk.Mcp` apphost, deps.json and runtimeconfig. These are
leftovers of referencing an executable project. They are unused and harmless.

### Requirements

- Tests: `Morris.Roslynk.Cli.Tests` 154/154 (was 149: +5 `HostRoutesTests` cases and +1 timeout test; the
  deps test was replaced by its unified-package counterpart). `Morris.Roslynk.McpTests` 30/30.
  `Morris.Roslynk.Tests` 840 passed / 15 failed: the same 15 pre-existing Razor failures 003 recorded on
  SDK 11 (the engine is untouched).
- `bin/dev build` passes with 0 warnings and `ganda repo audit` passes all checks.
- Deferred, as the checklist allows: the linked sources (508-013) and `VerbatimTerminal` (508-011).
  Groups wait on architecture 388 and nuru 508-014 (task 003-001).
- No new GitHub issue: the `releases.md` bullet for #4 (still Unreleased) now describes the shipped `roslynk`
  command instead of `roslynk-cli`.

### How to validate

Smoke:

```bash
./bin/dev build
dotnet test Source/App/Morris.Roslynk.Cli.Tests
dotnet test Source/App/Morris.Roslynk.McpTests
ganda repo audit
R=Source/App/Morris.Roslynk.Cli/bin/Release/net10.0/roslynk
dotnet build Source/App/Morris.Roslynk.Cli -c Release
$R --capabilities | python3 -c "import json,sys; e=json.load(sys.stdin)['endpoints']; print(len(e), sorted(x['pattern'] for x in e)[:4])"
Roslynk__Port=6599 Roslynk__Cli__AutoStart=false $R get_symbol --solutionId x --symbolName y; echo "exit=$?"
Roslynk__Port=6598 $R get_solution_status; echo "exit=$?"   # starts this roslynk as the daemon on 6598
ss -ltnp | grep 6598                                         # the started daemon; stop it with kill <pid>
dotnet pack Source/App/Morris.Roslynk.Cli -c Release -p:Version=0.0.4-local -o /tmp/roslynk-pack
unzip -p /tmp/roslynk-pack/Roslynk.0.0.4-local.nupkg tools/net10.0/any/DotnetToolSettings.xml
```

Expect:

- The build has 0 warnings and 0 errors. `Morris.Roslynk.Cli.Tests` passes 154/154 and McpTests pass
  30/30. The audit passes.
- `--capabilities` prints `32 ['', 'apply_code_action', 'apply_code_fix', 'apply_patch']`.
- The port-6599 call prints one `roslynk: no Roslynk daemon is listening on port 6599 ... Start it with
  'roslynk daemon' ...` line on stderr and `exit=69`.
- The port-6598 call prints nothing on stdout (no solution is open) with `exit=0`, and a `roslynk` process
  now listens on 6598.
- The tool settings name the command `roslynk` with entry point `roslynk.dll`.

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
