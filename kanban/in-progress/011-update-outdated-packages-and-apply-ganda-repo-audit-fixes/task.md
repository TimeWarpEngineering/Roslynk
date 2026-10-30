# Update outdated packages and apply ganda repo audit fixes

## Description

Fork issue #18. Steven (2026-10-30): "nuru and ganda have both updated. So run `ganda nuget outdated` and
`ganda repo audit` and fix both in same task."

ganda is at 1.0.0-beta.38 or later, and `ganda repo audit` on master now fails `nuru`, `glossary`,
`routine-jo…` (.gitignore), `memsearch-…` and `vscode-win…`. Earlier, `kanban-landed` also failed, because of
a dead task 008 claim; the cockpit released it on 2026-10-30. `ganda nuget outdated` at the repo root lists:
- TimeWarp.Nuru and TimeWarp.Nuru.DevCli 4.0.0-beta.5 → 4.0.0-beta.9
- Roslynator.* 5.0.0 → 5.0.1
- Microsoft.CodeAnalysis.NetAnalyzers 10.0.401 → 10.0.403

The root run reads only the root `Directory.Packages.props` (tools). The product's central file,
`source/Directory.Packages.props`, pins more, including:
- TimeWarp.Nuru, Nuru.Daemon and Nuru.McpHost at 4.0.0-beta.5
- TimeWarp.Contracts at 2.0.0-beta.23 (latest is beta.28)
- Roslyn, Aspire, OpenTelemetry and test packages

## Requirements

- **Packages:** run `ganda nuget outdated` (`--update --force` to apply) for **both** central files, root
  and `source/`. If the command reads only the root file, run it from `source/` or update `source/` by hand to
  the latest versions it would choose, and say which in Results.
  - Keep the whole TimeWarp.Nuru family (Nuru, DevCli, Daemon, McpHost) on one version.
  - Prereleases stay prereleases where we already use them (Nuru 4.0 beta, Contracts 2.0 beta).
  - Do not jump stable packages onto prereleases. One example is NetAnalyzers `11.0.100-r…`: stay on 10.0.x.
  - A major-version bump of a stable package is **not** an exception to policy. Take it, and adapt the code if
    the build needs that.
  - Bump `Microsoft.CodeAnalysis.*` (the Roslyn workspace/features the engine runs on) only to versions the
    .NET 10 SDK's MSBuild/BuildHost supports. If a bump breaks solution loading in tests, pin back and say why.
- **Adapt to the new versions.** Fix whatever the bumps break under warnings-as-errors.
  - Nuru beta.6–beta.9 includes timewarp-nuru 518: McpHost reads the tool hints (title, destructive,
    open-world) from TimeWarp.Contracts 2.0.0-beta.24+ and retires Nuru's own `[Destructive]`/`[OpenWorld]`.
    Roslynk's `McpServerRegistration.CreateOptions` sets `destructiveHint` and `openWorldHint` from the
    operation kind. If the bump breaks that path, or the hints now duplicate or conflict, move them onto the
    `[Operation]` contracts the TimeWarp.Contracts way: write tools destructive, every tool closed-world. If
    the current path still works unchanged, leave it and note the move as a follow-up in Results.
  - Either way, the published MCP annotations must not change: `McpSurfaceTests` stays green.
  - Read the Nuru releases' notes (`gh release view v4.0.0-beta.N --repo TimeWarpEngineering/timewarp-nuru`)
    for breaking changes.
- **Audit:** run `ganda repo audit --fix` and commit every change (global policy: audit fixes are the
  baseline). That includes `glossary.md`, the `.gitignore` routine entry, the memsearch cleanup and the VS
  Code window icon. Where a fix creates content (for example `glossary.md`), fill it with real Roslynk terms
  from AGENTS.md (served app, pinned snapshot, publication generation, projection, outline, …) instead of
  leaving a stub, if the check allows it. Keep the existing `[ganda.audit]` waivers. Add a new waiver only for
  a check that cannot apply to this fork, with a reason, and say so in Results. `ganda repo audit` must pass.
- **Verify:**
  - `dotnet build source/Morris.Roslynk.slnx -c Release`: 0 warnings, 0 errors.
  - `dotnet test source/Morris.Roslynk.slnx -c Release` compared with the post-010 baseline: CLI 336/336, and
    engine with only the 18 known Razor failures. A new failure must be fixed or explained. Never skip,
    quarantine or delete a test.
  - `bin/dev build`: run `dev self-install` first, because DevCli changed.
- `releases.md`: under `# Unreleased`, add "Updated to TimeWarp.Nuru 4.0.0-beta.9 and TimeWarp.Contracts
  2.0.0-beta.N, plus analyzer and package updates (Fixes #18)".
- **Out of scope:** 008's release flow (no Version, no CI workflows, no DevCli check-version/release restore)
  and Pete's new upstream commits.

## Goal (implementer restatement)

Bring both central package files (root `Directory.Packages.props` for tools/, `source/Directory.Packages.props`
for the product) to the latest versions `ganda nuget outdated` picks, keeping the TimeWarp.Nuru family on one
version (4.0.0-beta.9), prereleases on prerelease lines, stable packages on stable lines (NetAnalyzers stays
10.0.x, Roslyn only as far as the .NET 10 SDK BuildHost loads). Fix whatever the bumps break under
warnings-as-errors; check whether Nuru's new contract-driven MCP hints (nuru 518) clash with
`McpServerRegistration.CreateOptions` and keep published annotations unchanged. Run `ganda repo audit --fix`,
commit its output, fill `glossary.md` with real Roslynk terms, and leave `ganda repo audit` passing.

Acceptance: Release build 0 warnings / 0 errors; tests match the post-010 baseline (CLI 336/336, engine only
the 18 known Razor failures); `bin/dev build` works after `dev self-install`; releases.md Unreleased bullet
(Fixes #18); Results with an old → new version table and How to validate.

## Checklist

- [x] Root and `source/` central package files updated; Nuru family on one version
- [x] Build fixes for the bumps; hint path checked (moved onto contracts, or noted)
- [x] `ganda repo audit --fix` committed; `ganda repo audit` clean
- [x] Release build clean; tests match the baseline; `bin/dev build` works
- [x] releases.md (Fixes #18); AGENTS.md wherever versions or the hint mechanism are described
- [x] Results: a table of old → new versions per package

## Session

- Created: 1287ec9b-6d3e-4805-9dd9-5ef6634981c8 (2026-10-30)
- Implemented 2026-10-30 (implementer-claude, ganda task work): package bumps, contract titles/hints, audit fixes, glossary.

## Notes

- After this task: 008 is respecced on the `source/` layout (only its already-published guard waits on nuru
  536), and Pete's 7 new upstream commits (456684a … 7de4cfa) go to their own merge task.

## Results

### Packages

`ganda nuget outdated --update --force` reads only the root `Directory.Packages.props` (running it from `source/`
still reports the root file), so the root file was updated by the command and `source/Directory.Packages.props`
plus the AppHost SDK were updated by hand to the latest version on the same line (checked against the nuget.org
flat-container index).

| Package | File | Old | New |
|---------|------|-----|-----|
| TimeWarp.Nuru | root, source | 4.0.0-beta.5 | 4.0.0-beta.9 |
| TimeWarp.Nuru.DevCli | root | 4.0.0-beta.5 | 4.0.0-beta.9 |
| TimeWarp.Nuru.Daemon | source | 4.0.0-beta.5 | 4.0.0-beta.9 |
| TimeWarp.Nuru.McpHost | source | 4.0.0-beta.5 | 4.0.0-beta.9 |
| TimeWarp.Contracts | source | 2.0.0-beta.23 | 2.0.0-beta.29 |
| Roslynator.Analyzers / .CodeAnalysis.Analyzers / .Formatting.Analyzers | root | 5.0.0 | 5.0.1 |
| Microsoft.CodeAnalysis.NetAnalyzers | root | 10.0.401 | 10.0.403 (not 11.0.100-rc) |
| Microsoft.Extensions.AI.Abstractions | source | 10.10.0 | 10.11.0 |
| ModelContextProtocol / ModelContextProtocol.Core | source | 2.2.0 | 2.3.0 |
| Microsoft.Extensions.DependencyInjection.Abstractions | source | 10.0.12 | 10.0.14 (not 11.0 rc) |
| OpenTelemetry.Exporter.OpenTelemetryProtocol | source | 1.19.1 | 1.20.1 |
| Microsoft.NET.Test.Sdk | source | 18.10.1 | 18.11.0 |
| xunit.runner.visualstudio | source | 4.0.0 | 4.0.1 |
| coverlet.collector | source | 10.0.1 | 10.1.0 |
| Aspire.AppHost.Sdk (AppHost `Sdk=`) | AppHost csproj | 13.4.4 | 13.6.4 |
| Microsoft.CodeAnalysis.* (Workspaces, Features, CodeStyle) | both | 5.9.0 | 5.9.0 (already latest) |
| Microsoft.Build.Locator | source | 1.11.2 | 1.11.2 (already latest) |
| Microsoft.Build.Framework / Microsoft.NET.StringTools | source | 18.9.6 | 18.9.6 (pinned) |
| xunit | source | 2.9.3 | 2.9.3 (already latest) |

Microsoft.Build.Framework/StringTools stay on 18.9.x: they are compile-only (`ExcludeAssets=runtime`) and the
runtime assembly comes from the SDK MSBuildLocator resolves; the .NET 10 SDK (10.0.403) ships MSBuild 18.9.15,
so 18.10/18.11 would compile against API newer than the BuildHost Roslynk runs on.

### Adaptations

- **Titles (breaking in Nuru beta.6, timewarp-nuru 518):** McpHost and `--capabilities` read an operation's
  title only from `[Operation(Title)]`; `[DisplayName]` no longer sets it, so all 29 titles disappeared (58 CLI
  test failures). Each request's `[DisplayName("…")]` moved verbatim to `Title = "…"` on its `[Operation]`.
- **Hints moved onto the contracts:** every command has `[Destructive]`, every query `[Destructive(false)]`, and
  every request `[OpenWorld(false)]` (TimeWarp.Contracts). `McpServerRegistration.CreateOptions()` now sets only
  the instructions (its per-endpoint `ToolAnnotations` loop is gone). Published annotations are unchanged:
  `McpSurfaceTests` (title, destructiveHint == IsCommand, readOnlyHint, openWorldHint false) stays green.
- Tests: `OperationContractTests` now checks `[Operation].Title`, `[Destructive]` and `[OpenWorld]` against
  each method's `[McpServerTool]`; the CLI test `Operations` helper reads the title from `[Operation]`.
- ModelContextProtocol 2.3.0 made `McpClient.ServerInfo` nullable: `McpSurfaceTests` uses `ServerInfo?.Name`.
- Aspire 13.6 defaults `AspireUseCliBundle=false` and then warns (ASPIRE010) about its own default; the AppHost
  suppresses ASPIRE010 with a comment, keeping the package-restored dashboard/DCP.
- AGENTS.md: title and hint mechanism (Documentation ownership, MCP contracts, Adding an operation) rewritten.
- releases.md: Unreleased bullet (Fixes #18).

### Audit

`ganda repo audit --fix` (all fixes committed):
- `glossary`: scaffolded `glossary.md`, then replaced the example rows with real Roslynk terms (served app,
  engine, operation, instance, solutionId, model, publication generation, pinned snapshot, write queue, fold,
  dirty rebuild, rebase, stale, projection, outline, leaf, response budget, retry line, tracked analyzer,
  RazorGenerationState, group) and acronyms (MCP, CPM, TFM, OTLP). `glossary-stale` required the terms to
  appear verbatim in the repo, hence `solutionId` and `RazorGenerationState` spellings.
- `memsearch-scaffold`: refreshed `.githooks/pre-push.cs` (empty dest list allowed).
- `routine-journal`: `.gitignore` gains `stacked-task-set*.resume.json`.
- `vscode-window-icon`: wrote `.vscode/settings.json` and `.vscode/tasks.json`; `.vscode/` is gitignored in
  this repo, so they stay local and nothing is committed for that check (it passes).
- `nuru`: fixed by the package bump.
No new waivers; existing `[ganda.audit]` waivers kept. `ganda repo audit`: Passed 30, Failed 0, Skipped 2.

### Verification

- `dotnet build source/Morris.Roslynk.slnx -c Release`: 0 warnings, 0 errors.
- `dotnet test source/Morris.Roslynk.slnx -c Release`: CLI 336/336; engine 981/999, the 18 failures are the
  known Razor baseline (RazorRenameTests, RazorSourceToolTests, RazorSymbolMappingTests,
  RazorGenerationTests, the Razor cases of ChangeSignature and GetExpressionInfo). No new failures.
- `dotnet run --file tools/dev-cli/dev.cs -- self-install` then `bin/dev build`: success, 0 warnings.

### Follow-ups

- None required. The 18 Razor failures predate this task (post-010 baseline).

### How to validate

Smoke:

```bash
dotnet build source/Morris.Roslynk.slnx -c Release
dotnet test source/Morris.Roslynk.slnx -c Release
dotnet run --project source/App/Morris.Roslynk.Cli -c Release -- --capabilities | grep -c '"title"'
ganda repo audit
```

Expect:

- Build: 0 warnings, 0 errors.
- Tests: Morris.Roslynk.Cli.Tests 336/336; Morris.Roslynk.Tests fails only the 18 known Razor tests.
- `--capabilities` lists a title on all 29 operations (titles come from `[Operation(Title)]`).
- `ganda repo audit`: "Repository passes all audit checks."
