# Roslynk

**Roslyn + link** — the link between AI test harnesses and Roslyn.

> **TimeWarp fork.** This repository is TimeWarp Engineering's fork of
> [Peter Morris's Roslynk](https://github.com/mrpmorris/Roslynk). The fork ships its own tool package,
> `TimeWarp.Roslynk`, only to TimeWarpEngineering's private GitHub Packages feed. The `Roslynk` package on
> nuget.org is Pete's upstream release, not this fork. Both install the same `roslynk` command.

Add the MCP to your AI harness, then type "Open (solution file name)" - that's it!

The biggest time saver you will see is checking for compiler errors and warnings; with
Roslynk it is practically instant, no need to wait minutes for the solution to build.

Roslynk gives an MCP client (e.g. Claude) semantic intelligence over C# code via Roslyn:
* diagnostics
* symbol navigation
* find-references
* semantic rename
* code actions
* dead-code detection
* and more

All operating directly on the projects compiled in a loaded solution!

- **Transport:** MCP over stdio (`roslynk stdio`) and a command line, both forwarding to one shared local server over a per-user Unix domain socket. No TCP port.
- **Host:** one `roslynk` command (a [TimeWarp.Nuru](https://github.com/TimeWarpEngineering/timewarp-nuru) app) is the CLI, the MCP server and the shared server. No UI.
- **Observability:** OpenTelemetry, exported via OTLP to a backend you configure.


## Connect an MCP client (all platforms)

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download) on `PATH`. The MCP client spawns
`roslynk stdio`; it starts the shared Roslynk server (`roslynk serve`) if one isn't already running, and
every tool call runs there. Nothing to launch or babysit by hand.

First install the fork's global .NET tool, `TimeWarp.Roslynk`, from TimeWarpEngineering's GitHub Packages feed
(no clone, no build needed). The package is private, so add the feed once with a GitHub personal access token
(classic) that has the `read:packages` scope and access to the TimeWarpEngineering organization:

```bash
dotnet nuget add source https://nuget.pkg.github.com/TimeWarpEngineering/index.json \
  --name timewarp-github --username <github-user> --password <PAT> --store-password-in-clear-text
dotnet tool install -g TimeWarp.Roslynk --prerelease
```

(Or put the same source and its `packageSourceCredentials` in your user `NuGet.config`.) Releases are
prereleases (`1.0.0-beta.1` onwards), so `--prerelease` is required, for `dnx` as well. `dnx` reads the same
authenticated source from your NuGet configuration.

Pete's upstream package installs the same `roslynk` command (`dotnet tool install -g Roslynk` from nuget.org),
so the two global tools conflict: uninstall one before installing the other
(`dotnet tool uninstall -g Roslynk` or `dotnet tool uninstall -g TimeWarp.Roslynk`).

### Claude Desktop

```bash
claude mcp add roslynk -- dnx TimeWarp.Roslynk --prerelease --yes -- stdio
```

### Antigravity (`agy`)

Using the CLI:

```bash
agy mcp add roslynk roslynk stdio
```

(Or with `dnx`: `agy mcp add roslynk dnx TimeWarp.Roslynk --prerelease --yes -- stdio`)

Or manually in `~/.gemini/config/mcp_config.json`:

```json
{
  "mcpServers": {
    "roslynk": {
      "command": "roslynk",
      "args": ["stdio"]
    }
  }
}
```

### Manual `mcp.json` (VS Code, Cursor, Windsurf, and others)

Add the following entry to your MCP configuration file (e.g. `.vscode/mcp.json`,
`~/.cursor/mcp.json`, or your AI tool's equivalent):

```json
{
  "mcpServers": {
    "roslynk": {
      "type": "stdio",
      "command": "dnx",
      "args": ["TimeWarp.Roslynk", "--prerelease", "--yes", "--", "stdio"]
    }
  }
}
```

> **Important:** the `stdio` argument at the end is required. Without it the command prints its help
> and exits, so the client never gets an MCP `initialize` answer.

Tagged releases of the fork (e.g. `1.0.0-beta.1`, no `v` prefix) are packed as `TimeWarp.Roslynk` and pushed to
TimeWarpEngineering's private GitHub Packages feed by CI. Nothing from this fork is published to nuget.org.

From a source checkout:

```bash
claude mcp add roslynk -- dotnet run --project /path/to/Roslynk/Source/App/Morris.Roslynk.Cli -- stdio
```

Or in `mcp.json` from a source checkout:

```json
{
  "mcpServers": {
    "roslynk": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/Roslynk/Source/App/Morris.Roslynk.Cli", "--", "stdio"]
    }
  }
}
```

(With a built checkout, run `roslynk stdio` from `Source/App/Morris.Roslynk.Cli/bin/<Configuration>/net10.0` instead. It starts faster.)

### DeepSeek Harness (`dsh`)

DeepSeek Harness bridges MCP servers through `@deepseek-ai/dsh-mcp-client` — one plugin row per
server. Add Roslynk to your user patch layer at `$DSH_HOME/cordis.patch.yml` (defaults to
`~/.dsh/cordis.patch.yml` or `%USERPROFILE\.dsh\cordis.patch.yml`), which is applied to every profile:

```yaml
- insert:
    - id: mcp-roslynk
      name: '@deepseek-ai/dsh-mcp-client'
      config:
        serverName: roslynk
        transport: stdio
        command: dnx
        args: ['TimeWarp.Roslynk', '--prerelease', '--yes', '--', 'stdio']
```

> **Important:** a new plugin has to go inside an `insert:` list. A bare top-level `- id: mcp-roslynk`
> row is an *id-targeted config override* of a row that already exists in the composed tree, so `dsh`
> prints `patch: entry "mcp-roslynk" not found` to stderr and boots with no bridge and no tools.

The tools reach the model as `mcp__roslynk__*`. Check the composition without booting a session:

```bash
dsh --profile web --dump-config
```

The row should be listed under a `# == <path>/cordis.patch.yml` comment, with no `not found` warning
above it. `dsh` watches the patch file, so edits reconnect the server without restarting the process.

The server outlives individual sessions, so Roslyn workspaces stay warm across MCP clients and CLI calls.
Calls run concurrently: a long `get_diagnostics` does not hold up other tools. It listens on a per-user
Unix domain socket named for its build, `roslynk-<8 hex>.sock` (in `$XDG_RUNTIME_DIR/nuru` on Linux,
`<temp>/nuru-<user>` otherwise, `%LOCALAPPDATA%\TimeWarp.Nuru\daemon` on Windows; `NURU_DAEMON_SOCKET`
overrides it), stops after an hour without calls, and closes a solution nobody used for 30 minutes. A server
started in the background writes nothing; run `roslynk serve` in a terminal to watch it. `roslynk serve --status`
reports its build (version, path and write time), lists the servers of other `roslynk` builds that are running,
and says how to stop each; `roslynk serve --stop` stops this build's server.

Each build gets its own server, and a `roslynk` never stops another build's server. So `dnx TimeWarp.Roslynk -- stdio`
in an MCP config (the package cache copy) beside a globally installed `roslynk` used from the shell or a hook
runs two warm servers side by side, one per build, each with its own loaded solutions; neither cancels the
other's calls. A server you no longer use stops on its idle timeout. Two builds made to share one socket
(the same `NURU_DAEMON_SOCKET`, as the Aspire AppHost sets) are refused rather than replaced: the call fails
with exit 69 and one line naming both builds and `serve --stop`, and the running server keeps serving.

On Windows the server keeps running after the client and its console exit, but it is started as an ordinary
child process: a host that runs its children in a kill-on-close job object takes the server down with it, and
the next call starts a new one (with cold solutions). The Windows-service hosting of earlier versions is gone;
`roslynk serve` in a terminal, or the self-started server, is the only way to run it.

## 🤖 Configure your agent

Connecting the MCP server is only half the job. Left to its own habits an agent will still `grep`
for call sites, read whole files to find a member, and run `dotnet build` to check for errors — all
slower and less correct than the equivalent Roslynk call. Teach it the tools once and it stops.

### Claude Code: install the skill

This repo ships a ready-made skill in [`skills/roslynk`](skills/roslynk). Copy it into your skills
folder and Claude will load it automatically whenever a task touches C#:

```bash
# user-wide (all projects)
cp -r skills/roslynk ~/.claude/skills/

# or per-project
cp -r skills/roslynk .claude/skills/
```

### Antigravity: install the skill

Antigravity discovers skills in `~/.gemini/config/skills` for user-wide skills, and
`<projectRoot>/.agents/skills` for project ones:

```bash
# user-wide (all projects)
cp -r skills/roslynk ~/.gemini/config/skills/

# or per-project
cp -r skills/roslynk .agents/skills/
```

### DeepSeek Harness: install the skill

`dsh` scans `$DSH_HOME/skills` (defaults to `~/.dsh/skills`) for user-wide skills, and
`<projectRoot>/.dsh/skills` for project ones — the project root being the nearest ancestor
containing `.git`:

```bash
# user-wide (all profiles, all projects)
cp -r skills/roslynk ~/.dsh/skills/

# or per-project
cp -r skills/roslynk .dsh/skills/
```

The skill directory is watched, so a fresh copy is picked up without restarting `dsh`.

### Everything else: example system prompt for LLMs

> [!IMPORTANT]
> **⚡ Pro Tip:** Add this system prompt to your `AGENTS.md` / `CLAUDE.md`, Cursor Rules, Windsurf
> rules, or any AI agent configuration file. It dramatically improves how often and how well your
> LLM reaches for Roslynk instead of grepping, reading files, or rebuilding the solution.

> You have access to `roslynk`, an MCP server holding a live Roslyn compilation of a C# solution.
> Prefer it over grep, file reads, hand-written edits, and `dotnet build` for anything semantic:
> it answers from the compiler's symbol model, so it sees partial classes, generated code, inactive
> `#if` branches and Razor, and it never matches names inside comments or strings.
>
> **Getting started:** call `open_solution` with the absolute path to the `.sln`/`.slnx`; the
> `solutionId` it returns is the handle every other tool needs. It loads in the background — while
> it does, other calls return `error=Indexing`; retry shortly or poll `get_solution_status`. Never
> fall back to reading or editing files directly. Never call `reload_solution` on your own
> initiative — a file watcher picks up edits automatically; suggest a reload instead.
>
> **Lifecycle:**
> - `open_solution`: Load a solution. Parameters: `solutionPath` (absolute). Idempotent, cheap to repeat.
> - `get_solution_status`: List every loaded solution and its progress, with the messages behind `loadDiagnostics` (skipped analyzers, failed project loads) indented under each Ready solution. A missing generator DLL names the project to build; once built, the next call picks it up without a reload. Optional parameter: `allLoadDiagnostics` (every message instead of the first 20 per solution).
> - `reload_solution`: Force a from-disk re-evaluation. Parameter: `solutionId`. Only when the user explicitly asks.
>
> **Navigation:**
> - `find_definition`: Go to definition from a cursor position. Parameters: `solutionId`, `filePath`, `line`, `column` (1-based).
> - `get_expression_info`: Compiler facts about the expression at a position: type and converted type, bound symbol and selected overload, nullability, constant value, implicit conversion, origin and doc summary; on a declared name, the declared symbol and its type. Parameters: `solutionId`, `filePath` (`.cs`/`.razor`/`.cshtml`), `line`, `column` (1-based).
> - `get_symbol`: Identify a symbol and get its declaration. Parameters: `solutionId`, `symbolName` (fully-qualified).
> - `get_symbol_body`: Read a symbol's source text, body included. Parameters: `solutionId`, `symbolName` (fully-qualified), `includeLeadingTrivia`, `startItem` (page a long declaration; the response reports `truncated=Y`/`count`/`nextStartItem`), `maxSize` (page size in characters; default the response budget), `part` (fetch one declaration of a partial type; oversized parts are listed with `omitted=Y` and line counts).
> - `get_members`: List a type's members with their locations. Parameters: `solutionId`, `typeName`, `includeInherited`, `nameFilter`, `includeMethods`/`includeFields`/`includeProperties`/`includeEvents`/`includeNestedTypes`.
> - `search_symbols`: Find symbols by partial name. Parameters: `solutionId`, `query`, `maxResults`.
> - `multi_query`: Run several of the read-only queries above in one call, against a single snapshot of the solution. Parameters: `solutionId`, `operations` (each names a tool and carries that tool's own parameters), optional `expectSnapshot`. The whole response stays within the server's response budget (80,000 characters by default): every operation runs, whole results are delivered smallest-first until the budget is filled (in request order), and the rest are named once in the header as `retry=<index>:<size>` (size = the result's cost as a ratio of the budget, rounded up to 0.1). Sizes ≥ 1.0 must be re-sent alone (they page); sizes summing to ≤ 1.0 fit one continuation batch together. See the skill's multi_query section. For impact questions ("what uses X?", "what breaks if I change X?"), batch `find_references`, `get_callers`, `get_callees`, `find_implementations` and `get_type_hierarchy` in one call.
>
> **Relationships:**
> - `find_references`: Every usage of a symbol. Parameters: `solutionId`, `symbolName`, `maxResults` (omit for every reference; an oversized result pages with `startItem`).
> - `find_reads` / `find_writes`: Where a field, property or parameter is read or written, each location tagged `read`, `assign`, `compound`, `increment`, `ref`, `out` or `init`; `compound`, `increment` and `ref` appear in both. Parameters: `solutionId`, `symbolName` (a parameter is `Namespace.Type.Method:parameter`), `maxResults`.
> - `get_callers`: Who calls a member (method, constructor, operator, property, indexer or event), overloads resolved, including calls the compiler inserts (`foreach`/`await` plumbing, `using` disposal, operators, conversions). Parameters: `solutionId`, `methodName`.
> - `get_callees`: What a member calls - the inverse of `get_callers`; methods, constructors, property/event accessors and operators, overload-exact, with referenced-assembly callees grouped per assembly. Parameters: `solutionId`, `memberName`, `excludeExternal`.
> - `find_implementations`: Implementors/overrides of an interface, abstract or virtual member. Parameters: `solutionId`, `symbolName`.
> - `get_type_hierarchy`: Base types, interfaces and derived types. Parameters: `solutionId`, `typeName`.
>
> **Diagnostics:**
> - `get_diagnostics`: Compile check — this replaces `dotnet build`. Parameters: `solutionId`, `includeErrors`, `includeWarnings`, `includeInfo`, `includeHidden` (**all default false**), `includeAnalyzers`, and optional filters `projectName`, `filePath`, `ids` (array of strings), `maxResults`, `summaryOnly`. The header always reports `errors=`/`warnings=`/`infos=`/`hidden=` counts — for the filtered set, with each active filter echoed as a `filter=` header — so an empty body with `filter=` lines means the filter matched nothing, not a clean solution; opt into detail only when they are non-zero. `maxResults` caps the listed entries with `count=`/`truncated=Y`; `summaryOnly=true` returns one line per id (count, one example location and message) instead. Filters apply after compilation, so a filtered call reuses the cached result. Costly per call (it compiles and analyzes everything the edits affect), so run it once after a task's edits, not after each one. Razor compiler (`RZ*`) errors are included, reported against the `.razor`/`.cshtml` file.
>
> **Code actions:**
> - `get_code_actions`: List fixes and refactorings at a position. Parameters: `solutionId`, `documentPath`, `line`, `column`, `endLine`, `endColumn`.
> - `apply_code_action`: Apply one by its opaque `actionId` (pass it back verbatim). Parameters: `solutionId`, `actionId`, `checkOnly`.
> - `apply_code_fix`: Fix the diagnostic (compiler or analyzer, e.g. `CS0219` or `IDE0005`) at a position `get_diagnostics` reported, no round-trip. Parameters: `solutionId`, `documentPath`, `diagnosticId`, `line`, `column` (required), `checkOnly`. When the diagnostic has several fixes it writes nothing and returns `error=Conflict` with a `candidate` actionId per fix; apply the chosen one with `apply_code_action`.
>
> **Editing:**
> - `apply_patch`: Edit text files with a git unified diff. Parameters: `solutionId`, `patch`, `baseVersions`, `checkOnly`. Hunks are content-anchored, not line-number-anchored — include enough context that each matches exactly one place. Paths are relative to the solution folder (a unique suffix also resolves); a missing file is `NotFound`, an ambiguous suffix `Ambiguous`.
> - `rename_parameter`: Rename one parameter of a method, constructor or indexer overload with Roslyn: declaration, body, named arguments and `<paramref>` docs, cascading across the override/interface family. Parameters: `solutionId`, `methodId`, `parameterName`, `newName`, `checkOnly`.
> - `rename_symbol`: Compiler-correct rename across partial classes, every `#if` branch, every target framework, and `.razor`/`.cshtml`. Parameters: `solutionId`, `symbolName`, `newName`, `checkOnly`.
> - `change_signature`: Append one optional parameter to an ordinary method and thread an argument into every call site. Parameters: `solutionId`, `methodId`, `parameterType`, `parameterName`, `defaultValue`, `callSiteArgument`, `checkOnly`.
> - `extract_method`: Extract a selection of statements or an expression into a new method or local function with Roslyn's own Extract Method refactoring; writes nothing when Roslyn cannot extract it safely or the result would not compile. Parameters: `solutionId`, `documentPath`, `startLine`, `startColumn`, `endLine`, `endColumn`, `methodName`, `asLocalFunction`, `checkOnly`.
> - `remove_unused_usings`: Strip unused `using` directives (CS8019), preserving surrounding trivia. Parameters: `solutionId`, `documentPath` (omit for the whole solution), `checkOnly`.
>
> **Dead code:**
> - `find_dead_code`: Unreferenced members with a confidence and a reason — candidates, never verdicts; it deletes nothing. Parameters: `solutionId`, `scope` (FQN prefix; use it on large solutions), `includePublic`, `maxResults`.
> - `find_dead_conditionals`: `#if` branches never compiled under any loaded configuration. Parameter: `solutionId`.
>
> **Conventions:** most tools take a fully-qualified `Namespace.Type.Member` name (no `global::`); a generic
> type may be written without its type parameters (`Namespace.Box`) when only one arity exists — several
> arities are `error=Ambiguous` with one candidate per arity, and spellings like ``Namespace.Box`1`` or
> `Namespace.Box<int>` resolve too; a local
> function is named as a member of the method declaring it (`Namespace.Type.Method.local`, nesting further
> for one inside another);
> `find_definition`, `get_expression_info` and `get_code_actions` are position-based instead. Every tool that takes a
> `documentPath`, and every write tool, works on `.razor` and `.cshtml` files as well as `.cs`: positions
> are given in the Razor file, and edits are written back to it in its own indentation. Responses are a compact
> `key=value` header block, a blank line, then a tab-indented outline; booleans are `Y`/`N`. Errors
> are header-only: `error=Indexing` (retry), `Ambiguous`/`NotFound` (pick from the `candidate=`
> lines), `Stale` (re-read and recompute), `Conflict` (re-run the discovery step). Watch for
> `truncated=Y` and continue with `startItem` (or raise `maxResults`). Leave defaulted parameters alone unless you need the
> non-default behaviour, and re-query rather than reusing an earlier response — the solution is
> edited live.
>
> **Writing safely:** every write tool accepts `checkOnly=true` to preview the changed-file list
> without writing — use it before a broad rename. Writes are atomic and stale-guarded: if a file
> changed on disk since Roslynk read it the whole batch is rejected with `error=Stale` rather than
> clobbering the other edit. Successful writes advance the in-memory model immediately, so
> `get_diagnostics` straight afterwards reflects the change — no reload, no rebuild.
>
> **The edit loop:** make the task's changes → `get_diagnostics` once at the end (bare call, read the counts) → if errors
> appeared, re-call with `includeErrors=true` → fix with `apply_code_fix` at each entry's `line`/`column`
> (or `apply_code_action` with a `candidate` it offers) or `apply_patch` → repeat.

## Run the server manually

For a foreground server with visible output, from a source checkout:

```bash
./installer/run.sh              # Linux / WSL / macOS: dotnet run ... -- serve
dotnet run --project Source/App/Morris.Roslynk.Cli -- serve   # any OS
```

or, with the tool installed, `roslynk serve`. MCP clients (`roslynk stdio`) and CLI calls from the same build
use it. Ctrl+C stops it.

The server reads these settings once, when its engine starts, from any configuration source of the app
(environment variables such as `Roslynk__MaxResponseChars`, `appsettings.json` beside the tool, or
`--Roslynk:MaxResponseChars=40000` after `serve`). A server started in the background has the environment of
the client that started it. Stop the server (`roslynk serve --stop`) to change them.

- `Roslynk:MaxResponseChars`: the response budget, the character cap every tool result is held under (default 80,000). One server serves every client with one value, so set the smallest cap your clients need.
- `Roslynk:IdleEvictionMinutes`: close a solution nobody used for this long (default 30; 0 keeps them loaded).
- `Roslynk:EvictionCheckMinutes`: how often to check (default 5).

## Command line

The `roslynk` command is also a command line over the same 29 tools. Each tool is a command with the tool's name, and each parameter is an option with the parameter's name (`--solutionId`, not `--solution-id`). A command does not load Roslyn. It forwards the call to the shared server, starting it when none runs, and prints the text an MCP client would read. A relative `--solutionPath` or `--solutionId` is resolved against your current directory. Success text goes to stdout unchanged. An `error=` result goes to stderr with a non-zero exit code:

| Exit code | Meaning |
| --- | --- |
| 0 | Success |
| 1 | Usage or binding error: Nuru's usage text, or an `error=Invalid` outline for an option value or `--json-args` member that does not bind (the call never reached the server's tool) |
| 2 | `Invalid` |
| 3 | `NotFound` |
| 4 | `Ambiguous` |
| 5 | `Indexing` |
| 6 | `Stale` |
| 7 | `Conflict` |
| 8 | `NotSupported` |
| 9 | `Faulted` |
| 10 | `Truncated` |
| 69 | No server of this build: none runs and none could be started, another build's server holds the socket, or the server stopped answering during the call (one line on stderr) |
| 70 | An `error=` code this version does not know |

```bash
dotnet tool install -g TimeWarp.Roslynk --prerelease   # the same tool the MCP clients above run (feed setup above)

roslynk --capabilities                         # every command, option, description and default, as JSON
roslynk open_solution --solutionPath /abs/path/to/App.slnx
roslynk get_symbol --solutionId /abs/path/to/App.slnx --symbolName MyApp.OrderService
roslynk get_diagnostics --solutionId /abs/path/to/App.slnx --includeErrors --includeAnalyzers false
```

From a source checkout, build `Source/App/Morris.Roslynk.Cli` and run `bin/Debug/net10.0/roslynk` there.

Every option can also come from one JSON object with `--json-args`. `-` reads stdin, `@path` reads a file, or you can pass the object inline. The keys are the option names. `multi_query`'s `operations` and `apply_patch`'s `baseVersions` are JSON values, so they can only be passed this way:

```bash
roslynk multi_query --json-args - <<'EOF'
{"solutionId":"/abs/path/to/App.slnx","operations":[{"tool":"get_symbol","arguments":{"symbolName":"MyApp.OrderService"}}]}
EOF
```

Beside the 29 tools, `roslynk stdio` is the MCP server above, and `roslynk serve` (with `--status` and `--stop`) runs or manages the shared server. `--no-daemon` before a command runs it in the CLI process instead of the server (it loads Roslyn and the solution there, so it is slow). `--capabilities` lists `stdio` too, marked local-only.

Each command belongs to one of five groups, so a caller can list one slice of the catalog instead of all of it. The group does not change the command name:

| Group | Commands |
| --- | --- |
| `symbols` | `get_symbol`, `get_members`, `get_symbol_body`, `find_definition`, `search_symbols`, `get_expression_info`, `get_type_hierarchy`, `find_implementations` |
| `references` | `find_references`, `get_callers`, `get_callees`, `find_reads`, `find_writes`, `find_dead_code`, `find_dead_conditionals` |
| `solution` | `open_solution`, `get_solution_status`, `reload_solution`, `get_diagnostics`, `get_code_actions` |
| `edit` | `apply_code_action`, `apply_code_fix`, `extract_method`, `change_signature`, `rename_symbol`, `rename_parameter`, `remove_unused_usings`, `apply_patch` |
| `batch` | `multi_query` |

```bash
roslynk --capabilities --group-filter symbols  # only the symbols commands (about a quarter of the full catalog)
```

`stdio` has no group, so a filtered catalog leaves it out. Ctrl+C on a command stops its call in the server too.

# Observing Roslynk
1. Stop the running Roslynk server (`roslynk serve --stop`).

2. Open PowerShell:
```
  dotnet tool install -g Aspire.Cli
  dotnet tool install -g TimeWarp.Roslynk --prerelease
  aspire dashboard run --allow-anonymous
```
3. CTRL Click Dashboard link in output to open it in your browser.
4. Then click the `Traces` Icon in the page that opens.
5. Leave PowerShell window open.
6. Open second PowerShell window:
```
  $env:OTEL_EXPORTER_OTLP_ENDPOINT = "http://localhost:4317"
  $env:OTEL_EXPORTER_OTLP_PROTOCOL = "grpc"
  roslynk serve
```
You will then see all the Roslynk traces in Aspire.
