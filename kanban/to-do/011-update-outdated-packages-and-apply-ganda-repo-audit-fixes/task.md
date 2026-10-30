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

## Checklist

- [ ] Root and `source/` central package files updated; Nuru family on one version
- [ ] Build fixes for the bumps; hint path checked (moved onto contracts, or noted)
- [ ] `ganda repo audit --fix` committed; `ganda repo audit` clean
- [ ] Release build clean; tests match the baseline; `bin/dev build` works
- [ ] releases.md (Fixes #18); AGENTS.md wherever versions or the hint mechanism are described
- [ ] Results: a table of old → new versions per package

## Session

- Created: 1287ec9b-6d3e-4805-9dd9-5ef6634981c8 (2026-10-30)

## Notes

- After this task: 008 is respecced on the `source/` layout (only its already-published guard waits on nuru
  536), and Pete's 7 new upstream commits (456684a … 7de4cfa) go to their own merge task.
