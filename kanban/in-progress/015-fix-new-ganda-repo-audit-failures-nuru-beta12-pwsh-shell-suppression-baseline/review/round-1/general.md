# Round 1 — general
**Date:** 2026-11-11
**Scope reviewed:** branch vs master: workflow.yml, both Directory.Build.props / Directory.Packages.props, README.md, Mediator 15 handler migration (OperationHandlers.cs, StdioCommand.cs, HandlerTests.cs, tools/dev-cli endpoints), SymbolResolver.DedupeMetadataCopies, Multi.csproj fixture, Morris.Roslynk.csproj, test-command coverage, `--fix` output (.githooks, suppression baseline).

## Summary

The change does what the task asks. Nuru 4.0.0-beta.12 is net11-only, so the implementer retargeted product and tools to net11.0. They moved the handlers from the removed `ICommandHandler`/`IQueryHandler` to `IRequestHandler` returning `ValueTask`, added a coverage collection and upload path that matches, and committed the `--fix` output. The handler migration is mechanical and correct: `RunAsync`/`ReadAsync` still return `Task` and are wrapped in `new ValueTask(...)`, and the cancellation test uses `.AsTask()`.

Retargeting to net11.0 had a side effect. `Multi.csproj` now clears the inherited `TargetFramework`, so the committed MultiTarget fixture really multi-targets. The new `SymbolResolver.DedupeMetadataCopies` collapses the duplicate metadata symbols that follow from that, which matches the existing `seen` dedupe on the metadata-fallback path.

The open issues are stale user and agent docs that still say .NET 10, plus the Results' user-visible note.

Verified in this worktree:
- `./bin/dev build`: 0 warnings, 0 errors.
- Focused tests passed: Morris.Roslynk.Tests `GenericNameResolutionTests|GetDiagnosticsTests|CandidateRoundTripTests|GetSymbolTests` 83/83, and Morris.Roslynk.Cli.Tests `HandlerTests|CapabilitiesContractTests` 138/138.
- The full `./bin/dev test` did not finish inside the reviewer's 10-minute foreground window, so it was not re-run in full. The implementer's Results record 1115 + 338 passing.
- `ganda repo audit`: 39 passed, 1 failed, 2 skipped. All six target checks pass. The one failure is `kanban-landed` ("015 task.md differs from origin-home"). That check covers the host-owned kitchen landing of a claim that is still in progress, not product, so it is not raised as a finding.

## Issues

### Issue 1 — Severity: bug
- File: README.md:33
- Description: The connect section still says "Requires the [.NET 10 SDK]…". The `TimeWarp.Roslynk` tool package is now built for net11.0 (`source/Directory.Build.props`), so a user who follows the README with only .NET 10 installed gets a tool that cannot run. The task's Results say "Roslynk and the dev CLI now require the .NET 11 SDK", and the README's net10.0 → net11.0 path edits were made, but this install requirement was missed.
- Suggestion: Say .NET 11 SDK (or runtime, as the README prefers) at README.md:33, and check the rest of README for other version prerequisites.
- Status: open

### Issue 2 — Severity: suggestion
- File: AGENTS.md:35, AGENTS.md:217
- Description: AGENTS.md:35 still says "`source/Directory.Build.props` sets .NET 10". AGENTS.md:217 says "CI, with only 10.0 installed, would see neither". CI now installs 8.0.x, 10.0.x and 11.0.x, with 11 as the default. The fixture pin still protects fixture evaluation, but the stated reason no longer holds. Agents read this guide as authoritative, so it should now say that product and tools are net11 while fixtures stay on the SDK 10 band through `TestFixtures/global.json`, and that this is why CI installs 10.0.x and 8.0.x.
- Suggestion: Update both sentences. Mention that a developer machine needs the SDK 10 band beside 11 for fixture loads, as Results already note.
- Status: open

### Issue 3 — Severity: nit
- File: source/App/Morris.Roslynk.Tests/Features/Diagnostics/GetDiagnosticsTests/GetDiagnosticsTests.cs:191
- Description: The comment says "under source/, Directory.Build.props forces the committed fixture to a single net10.0 target framework". After the `Multi.csproj` change (`<TargetFramework></TargetFramework>`), the committed fixture is no longer collapsed, so the comment is false.
- Suggestion: Reword the comment to the real reason for the scratch copy, or drop it. Optionally use the committed fixture now that it multi-targets.
- Status: open

### Issue 4 — Severity: suggestion
- File: kanban/in-progress/015-…/task.md (## Results, "User-visible" paragraph)
- Description: The task asks that any user-visible effect of the Nuru bump be recorded in Results so that a `releases.md` bullet and an issue can follow. Results list the .NET 11 requirement but then say "No other user-facing behavior change was intended". The `SymbolResolver.DedupeMetadataCopies` change is user-visible: a BCL type such as `System.Collections.Generic.List<T>`, named in a genuinely multi-targeted user solution, now resolves once where it used to resolve once per framework. The fact is described in the next paragraph of Results but is not flagged as user-visible.
- Suggestion: Add the dedupe to the user-visible paragraph as a candidate `releases.md` bullet (fix), next to the .NET 11 requirement.
- Status: open

## Checked, no issue

- Mediator 15 migration: every operation handler implements `IRequestHandler<,>` and returns `ValueTask`. `RunStepAsync(string, ValueTask<Unit>)` awaits each ValueTask once.
- `Microsoft.Extensions.DependencyInjection.Abstractions` was removed. The build with warnings as errors is clean, and the package pin was removed with it.
- Coverage: both test projects reference `coverlet.collector`. `dev test` writes to `<repo>/artifacts/coverage` (gitignored), fails when no cobertura file is written, and the CI upload path is `artifacts/coverage`.
- pwsh default shell: the only `run:` steps are `dotnet run --file …` and simple invocations that parse the same under pwsh.
- `.vscode/settings.json` stays gitignored and untracked, and `vscode-window-icon` passes.
- `.githooks/*.cs`, `.timewarp/suppression-baseline.json` and the tools pins are `ganda repo audit --fix` output, which is policy (no finding).
- `DedupeMetadataCopies` only collapses symbols with no source location. Source copies are kept, so multi-targeted declarations stay reachable per framework.

## Goal restatement vs task

The `## Goal (implementer restatement)` matches the task description and Requirements. No misread. The net11 retarget is outside the literal six checks, but the task's "adapt the build to anything that release breaks" requires it.
