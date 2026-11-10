# Round 2 — general
**Date:** 2026-11-11
**Scope reviewed:** fix delta `e8421088..f4cdbf9f` (round-1 base to HEAD). The product changes are in `README.md`, `AGENTS.md`, and `source/App/Morris.Roslynk.Tests/Features/Diagnostics/GetDiagnosticsTests/GetDiagnosticsTests.cs`, and `task.md` Results changed too.

## Summary

The fix pass only changes docs and a comment, and it closes all four round-1 findings. I checked each new claim against the repo. The README now requires the .NET 11 SDK. `.github/workflows/workflow.yml` installs 8.0.x, 10.0.x, and 11.0.x in both jobs. `Multi.csproj` clears the inherited `TargetFramework` before it sets `net8.0;netstandard2.0`. Results name `SymbolResolver.DedupeMetadataCopies` as user-visible. The delta has no behavior change. The build, the focused test, and the audit are all green.

## Prior findings re-verified

- **M1** (bug) — fixed. `README.md:33` says .NET 11 SDK, and no other line in README.md names .NET 10 as a prerequisite.
- **M2** (suggestion) — fixed. `AGENTS.md:35` says net11.0 for the product and the tools. `AGENTS.md:217` says CI installs 8/10/11 and that a developer needs the SDK 10 band beside 11. `AGENTS.md:167` no longer says CI has only SDK 10.
- **M3** (nit) — fixed. `GetDiagnosticsTests.cs:191-192` now gives the right reason: `Multi.csproj` clears the inherited `TargetFramework`.
- **M4** (suggestion) — fixed. The `task.md` Results "User-visible" paragraph lists `SymbolResolver.DedupeMetadataCopies` as a second candidate `releases.md` bullet.

## Issues

None new on the fix delta.

## Checks run

- `./bin/dev build` exited 0 with 0 warnings and 0 errors.
- `dotnet test source/App/Morris.Roslynk.Tests/Morris.Roslynk.Tests.csproj --configuration Release --filter FullyQualifiedName~WhenFilteredByProjectName_ThenEveryTargetFrameworkIsIncluded`: 1 passed.
- `ganda repo audit`: `Passed: 40 | Failed: 0 | Skipped: 2`.

## Goal alignment

The second paragraph of `## Goal (implementer restatement)` describes the four round-1 findings and the acceptance checks for this pass. It matches the round-1 `merged.md`, so I found no misread.
