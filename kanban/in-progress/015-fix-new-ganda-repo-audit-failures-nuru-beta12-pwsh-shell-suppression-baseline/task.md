# Fix new ganda repo audit failures (Nuru beta.12, pwsh shell, suppression baseline)

## Goal (implementer restatement)

`ganda repo audit` on this tree must exit 0. Six checks that appeared after the ganda update are the work:
seed the analyzer suppression baseline, move the TimeWarp.Nuru family (Nuru, DevCli, Daemon, McpHost) to
4.0.0-beta.12 and adapt the build to anything that release breaks, give `workflow.yml` a top-level pwsh
default shell without breaking its existing `run:` steps, upload the coverage files that `dev workflow` /
tests actually write, refresh the memsearch githooks, and resolve the vscode window-icon check (commit
`--fix` output when it is meant to be tracked; do not force a gitignored local settings file into the
repo). Commit whatever `ganda repo audit --fix` writes. Waive a check in `.editorconfig` only when it
truly does not apply, with a reason and this task id. Do not add a `releases.md` Unreleased bullet unless
the Nuru bump changes something a user of Roslynk would see; if it does, record that in Results instead
of inventing a fork issue here. Acceptance is a green `bin/dev build`, a green `bin/dev test`, and a
clean `ganda repo audit`, with the Nuru beta.12 notes (including whether task 569's slow `check-version`
fix is in that release) written under Results.

Round 1 left four documentation findings. This pass closes them. The README must require the .NET 11 SDK,
and no other prerequisite in that file may still say .NET 10. AGENTS.md must say the product and the tools
target net11.0, that CI installs the 8.0, 10.0, and 11.0 SDK bands with 11 last, and that a developer still
needs the SDK 10 band beside 11 because fixture loads stay pinned to that band. The GetDiagnosticsTests
comment must not claim that Directory.Build.props forces the committed MultiTarget fixture onto a single
net10.0 target. Results must list `SymbolResolver.DedupeMetadataCopies` next to the .NET 11 requirement as
a candidate `releases.md` bullet: in a multi-targeted solution, a BCL type with no source location resolves
once instead of once per framework. Acceptance for this pass is those four texts, a green `./bin/dev build`,
the Multi target-framework diagnostic test still passing, and a clean `ganda repo audit`.

## Description

After a ganda update, `ganda repo audit` on master (2026-11-11, after task 014 merged) fails 6 checks that
were not there when task 013 passed. All six are marked fixable. Make the audit exit 0.

| Check | Severity | Detail |
| --- | --- | --- |
| `analyzer-suppressions` | Error | `.timewarp/suppression-baseline.json` missing; `--fix` seeds it from the current tree |
| `nuru` | Error | TimeWarp.Nuru 4.0.0-beta.11 < 4.0.0-beta.12 in `Directory.Packages.props` |
| `workflow-pwsh-shell` | Error | `.github/workflows/workflow.yml` lacks top-level `defaults: run: shell: pwsh` |
| `ci-code-coverage` | Warning | CI collects coverage but has no `actions/upload-artifact` step for it |
| `memsearch-scaffold` | Warning | `.githooks/{post-commit,post-merge,post-checkout,pre-commit,pre-push}.cs` outdated |
| `vscode-window-icon` | Warning | `.vscode/settings.json` missing (it is git-ignored since task 013; check whether `--fix` only writes a local file or wants something tracked) |

## Requirements

- Run `ganda repo audit --fix` and commit everything it changes. That is policy: do not revert or second-guess
  `--fix` output; mention it as fact, and act only if it breaks the build or tests.
- Nuru beta.12: bump the whole TimeWarp.Nuru family together (Nuru, DevCli, Daemon, McpHost) if `--fix` bumps
  only one. Read the Nuru beta.12 release notes / changes since beta.11 and adapt Roslynk to any breaking change.
  Check whether beta.12 contains timewarp-nuru task 569 (slow `check-version`); note in Results.
- pwsh default shell: make sure every existing `run:` step in `workflow.yml` still works under pwsh (the
  workflow is a thin trigger for `dev workflow`); fix any bash-only step.
- Coverage upload: the artifact path must match where `dev workflow` / tests write coverage.
- Anything `--fix` cannot fix: fix by hand, or waive under `[ganda.audit]` in `.editorconfig` with a reason and
  task id (only if the check truly does not apply).
- `releases.md` `# Unreleased`: no new bullet (audit/tooling task, no fork issue). If the Nuru bump changes
  user-visible behavior, say so in Results so a bullet and issue can be added.

## Checklist

- [x] `ganda repo audit --fix`; commit its changes
- [x] Nuru family at 4.0.0-beta.12; build adapted
- [x] workflow.yml steps verified under pwsh; coverage uploaded
- [x] `bin/dev build` and `bin/dev test` pass
- [x] `ganda repo audit` exits 0
- [x] Round-1 M1–M4: README .NET 11, AGENTS.md SDK notes, MultiTarget comment, user-visible dedupe

## Session

- Created: 93b68595-4648-49ee-a58b-6a1ec1f5dab9 (2026-11-11)
- Implementation: 01a1c6bc-1c3e-70e2-a96a-d6499a3237c7 (2026-11-11)
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1c6bc-1c3e-70e2-a96a-d6499a3237c7, max-turns 500, Done — 2026-11-10T20:29:49Z
- Review oracle round 1: reviewer-claude (claude, model claude-opus-5-5), headless, effort 2, roster general, turn cap 120. Result: fix pass needed for open M1–M4 (2026-11-11)
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 63da1d62-3a50-4e1d-a5a1-95dccb784d3a, max-turns 120, artifacts general.md, merged.md — 2026-11-10T20:44:11Z
- Fix pass: implement-fix-2 for review round 1 findings M1–M4 (2026-11-11)
- Review oracle round 2: reviewer-claude (claude, model claude-opus-5-5), headless, effort 1, roster general, turn cap 80. Result: disposition clean, 0 open (2026-11-11)
- Installed ganda changed during this walk: was 51756808 bytes a18f3dc25f245e28614a218db4f6294c4e02bfabf5529c0b4a7a94f7b048c6ec; now 51823784 bytes b912c9a5b66c92609a48f010f69a502abc0bd7425b1696a59cc6caf30d8eaccf

## Results

`ganda repo audit` exits 0 (40 passed, 0 failed, 2 skipped) and prints `Repository passes all audit checks.` No `[ganda.audit]` waiver. No `releases.md` Unreleased bullet.

`ganda repo audit --fix` seeded `.timewarp/suppression-baseline.json` (54 analyzer suppressions, none above baseline), refreshed the five `.githooks/*.cs` scripts, set top-level `defaults.run.shell: pwsh` in `.github/workflows/workflow.yml`, and bumped the tools pins that beta.12 pulls in (TimeWarp.Nuru and TimeWarp.Nuru.DevCli 4.0.0-beta.12, TimeWarp.Amuru and TimeWarp.Amuru.Tools 3.0.0-beta.2, TimeWarp.Terminal 2.0.0-beta.1). It also wrote the window title into gitignored `.vscode/settings.json`. That file stays untracked, and `vscode-window-icon` passes. `--fix` did not insert a coverage upload: the CI job owns `dev workflow`, so the check's patcher skips it. The upload was added by hand on the `ci` job only (`actions/upload-artifact@v4`, name `coverage`, path `artifacts/coverage`, `if: success()`, `if-no-files-found: error`).

The Nuru family is at 4.0.0-beta.12 together: product pins TimeWarp.Nuru, TimeWarp.Nuru.Daemon, and TimeWarp.Nuru.McpHost; tools pins TimeWarp.Nuru and TimeWarp.Nuru.DevCli. TimeWarp.Contracts moved to 2.0.0-beta.31 with them. Beta.12 (2026-11-09) is net11.0-only. Its PR 384 is timewarp-nuru task 569 (`fix(devcli): evaluate packable projects together so check-version is not silent`). That fix is in this pin. Roslynk did not change its `check-version` call. Mediator 15.0.0-beta.4, which beta.12 depends on, removed `ICommandHandler` and `IQueryHandler`. Handlers are `IRequestHandler<TRequest, TResponse>` and return `ValueTask`. `RunAsync` / `ReadAsync` still return `Task` so the reflection test that casts `ReadAsync` keeps working. Tools and product `TargetFramework` moved from net10.0 to net11.0. The net11 SDK then reported NU1510 for the explicit `Microsoft.Extensions.DependencyInjection.Abstractions` reference (shared framework), so that package reference was removed. README source-checkout output paths say net11.0.

User-visible: Roslynk and the dev CLI now require the .NET 11 SDK. A `releases.md` bullet and a fork issue can be added for that. `SymbolResolver.DedupeMetadataCopies` is also user-visible: in a multi-targeted solution, a BCL type with no source location resolves once instead of once per framework (`System.Collections.Generic.List<T>` used to arrive once from `System.Collections` and once from `netstandard`). That is a second candidate `releases.md` bullet. Source copies stay, so a multi-targeted declaration is still reachable in every framework. `check-version` is the beta.12 build, which includes task 569 (packable projects evaluated together).

This machine had only the .NET 11 SDK. `source/TestFixtures/global.json` pins SDK 10.0.100 with `rollForward: latestFeature`, and MSBuild design-time builds use the fixture directory, so solution loads faulted until SDK 10.0.404 and 8.0.428 were installed beside SDK 11 in the existing dotnetup root. That is a machine change, not a repo change. CI already installs 8.0.x, 10.0.x, and 11.0.x, with 11 last so it stays the default. Fixtures stay on the .NET 10 SDK band. `source/TestFixtures/MultiTargetSolution/src/Multi/Multi.csproj` clears the inherited `TargetFramework` before setting `TargetFrameworks` to `net8.0;netstandard2.0`. An unconditional net11.0 from `source/Directory.Build.props` otherwise makes that project a single-framework net11 build, which the fixture's SDK 10 pin cannot compile (NETSDK1045). The Razor multi-project fixture already used the same clear. Fully-qualified lookup now drops metadata copies that share a `FullyQualifiedWithRefKinds` signature (`System.Collections.Generic.List<T>` arrives once from `System.Collections` and once from `netstandard`). Source copies stay, so a multi-targeted declaration is still reachable in every framework.

Every existing `run:` step in `workflow.yml` parses as PowerShell. None was bash-only. `dev test` deletes `artifacts/coverage`, runs `dotnet test` with `--collect "XPlat Code Coverage"` and `--results-directory` pointed at that directory, and fails when no `coverage.cobertura.xml` is written.

### How to validate

Smoke: from this worktree, run `./bin/dev build`, then `./bin/dev test`, then `ganda repo audit`.

Expect: `./bin/dev build` exits 0 with 0 warnings and 0 errors. `./bin/dev test` exits 0, Morris.Roslynk.Tests passes 1115, Morris.Roslynk.Cli.Tests passes 338, and the command prints `All tests passed.` Coverlet writes `artifacts/coverage/<guid>/coverage.cobertura.xml` (this run: `93c2c234-9999-4dc4-ba2b-fe7e1a4cd70e` and `b603be69-8d13-4c15-9443-ce024012185c`). `ganda repo audit` exits 0, prints `Passed: 40 | Failed: 0 | Skipped: 2` and `Repository passes all audit checks.` A machine with only the .NET 11 SDK will fault fixture solution loads until SDK 10 is installed; `source/TestFixtures/global.json` is unchanged.

Smoke (round-1 fix): read the MCP prerequisite in `README.md`, the build-conventions sentence and the fixture-pin paragraph in `AGENTS.md`, the comment on `WhenFilteredByProjectName_ThenEveryTargetFrameworkIsIncluded`, and the User-visible paragraph above. Then run `./bin/dev build`, `dotnet test source/App/Morris.Roslynk.Tests/Morris.Roslynk.Tests.csproj --configuration Release --filter FullyQualifiedName~WhenFilteredByProjectName_ThenEveryTargetFrameworkIsIncluded`, and `ganda repo audit`.

Expect (round-1 fix): `README.md` requires the .NET 11 SDK and names no other .NET 10 prerequisite. `AGENTS.md` says product and tools are net11.0 and that a developer needs the SDK 10 band beside 11. The test comment says `Multi.csproj` clears the inherited `TargetFramework`, so the fixture keeps net8.0 and netstandard2.0. The User-visible paragraph names `SymbolResolver.DedupeMetadataCopies` as a candidate `releases.md` bullet. This pass: `./bin/dev build` exited 0 with 0 warnings and 0 errors in 8.44s. The filtered test passed 1. `ganda repo audit` exits 0 with `Passed: 40 | Failed: 0 | Skipped: 2` once this kitchen matches origin-home. Before that land, the only failure is `kanban-landed`.

### Review

Round 1 (effort 2, roster: general). Findings: 1 bug, 2 suggestions and 1 nit. The fix pass on 2026-11-11 addressed all four.

Round 2 (effort 1, roster: general) checked the fix delta `e8421088..f4cdbf9f`. It marked M1–M4 fixed and raised no new findings. Final counts: bug 0 open, 1 fixed. Suggestion 0 open, 2 fixed. Nit 0 open, 1 fixed. Nothing is wontfix. **Disposition: clean.** Round 2 checks: `./bin/dev build` was green with 0 warnings. The focused MultiTarget diagnostics test passed 1. `ganda repo audit` reported `Passed: 40 | Failed: 0 | Skipped: 2`. Open follow-up for a human: neither candidate `releases.md` bullet (the .NET 11 SDK requirement and `DedupeMetadataCopies`) has been added yet.

- **M1** (bug): README.md now requires the .NET 11 SDK. No other prerequisite in README.md named .NET 10. `Benchmarks/TestSolution` stays net10.0; that solution's own README still describes its `Directory.Build.props`.
- **M2**: AGENTS.md now says product and tools are net11.0, CI installs 8.0.x, 10.0.x, and 11.0.x, and fixture loads need the SDK 10 band beside 11. The Razor split-shape sentence no longer says CI has only SDK 10.
- **M3**: the GetDiagnosticsTests comment now says `Multi.csproj` clears the inherited `TargetFramework`, so the fixture keeps net8.0 and netstandard2.0.
- **M4**: Results lists `SymbolResolver.DedupeMetadataCopies` next to the .NET 11 requirement as a candidate `releases.md` bullet. No `releases.md` bullet was added; there is still no fork issue.

Reviewer checks:

- `./bin/dev build` is green.
- Focused tests passed: 83 in Morris.Roslynk.Tests and 138 in Morris.Roslynk.Cli.Tests.
- `ganda repo audit`: all six target checks pass. The only failure is `kanban-landed`, which is host landing state.

Paths:

- `review/review-framework.md`
- `review/round-1/general.md`
- `review/round-1/merged.md`
- `review/round-2/general.md`
- `review/round-2/merged.md`
- `review/disposition.md`

## Review

- Round 1: fix pass implemented M1–M4 (2026-11-11).
- Round 2: Done
## Notes

- Prior audit task: 013.
