# Round 1 — general
**Date:** 2026-10-30
**Scope reviewed:** product commits `e0436b1..d81f867` against the task requirements and the implementer restatement

## Summary

The branch pins the TimeWarp.Nuru family at 4.0.0-beta.9 in both central package files, TimeWarp.Contracts at 2.0.0-beta.29, and the other public packages on the latest stable line the task allows. NetAnalyzers stays on 10.0.403. Microsoft.CodeAnalysis.* stays on 5.9.0, which is the newest stable release. Microsoft.Build.Framework and Microsoft.NET.StringTools stay on 18.9.6; the .NET 10 SDK 10.0.403 ships Microsoft.Build 18.9.15, and those references are compile-only so the BuildHost assembly wins at runtime.

All 29 request titles match the previous `[DisplayName]` strings. Commands carry `[Destructive]`, queries `[Destructive(false)]`, and every request `[OpenWorld(false)]`. `McpServerRegistration.CreateOptions` now publishes only the server instructions. TimeWarp.Nuru.McpHost 4.0.0-beta.9 maps `destructiveHint`, `openWorldHint`, and `title` from those contract members, and `ToolAnnotations` remains an override the app no longer fills. Nuru beta.6 also removed `UseLogging` / `UseConsoleLogging`; this repo does not call them. The AppHost SDK is 13.6.4. `NoWarn` includes ASPIRE010, which is the remedy Aspire.Hosting.AppHost 13.6.4 documents for keeping `AspireUseCliBundle` false. The Release AppHost build is 0 warnings, 0 errors.

`ganda repo audit` on this tree: Passed 30, Failed 0, Skipped 2. `ganda nuget outdated` on the root file: 13 packages, all up to date. The root command does not read `source/Directory.Packages.props`; the source pins were checked against the nuget.org index and the Nuru and Contracts GitHub releases (beta.9 and beta.29 are the current prereleases). Existing `[ganda.audit]` waivers are unchanged. Glossary groups match the five `[Operation(Group)]` values in the tree.

Release contract tests, filtered to `OperationContractTests`, `CapabilitiesContractTests`, and `McpSurfaceTests`: engine 1 passed (the contract fact covers every tool), CLI 224 passed, 0 failed. The full CLI 336 and engine 999 runs were not repeated in this round.

## Issues

None.
