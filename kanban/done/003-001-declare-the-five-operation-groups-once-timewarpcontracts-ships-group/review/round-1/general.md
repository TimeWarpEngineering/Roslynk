# Round 1 — general
**Date:** 2026-10-20
**Scope reviewed:** branch `task/003-001-declare-the-five-operation-groups-once-timewarpcon` vs `master` — pins, the 29 `[Operation]` attributes under `Source/App/Morris.Roslynk/Features/`, `CapabilitiesContractTests`, README, AGENTS.md, releases.md, and the `VerbatimTerminal` version comment. Checked against the original task (description and checklist at `cd698f5`) and against `## Goal (implementer restatement)`.

## Summary

Each of the 29 request types now sets `Group` once on its existing `[Operation]`. The assignments match the task list: 8 `symbols`, 7 `references`, 5 `solution`, 8 `edit`, 1 `batch`. Route patterns stay the tool names. The three host routes stay ungrouped. The engine project still references only `TimeWarp.Contracts`, and `ForwardedCall` still sends `operation.Name`. Pins are Contracts 2.0.0-beta.23 and Nuru / Nuru.DevCli 4.0.0-beta.3. Docs drop the ungrouped wording and name the five groups plus `--group-filter`.

The implementer restatement matches that original description and checklist. There is no misread.

`roslynk --capabilities` on the Release build lists 32 routes (29 grouped + 3 host) with one-segment `groupPath` values matching the task. Each `--group-filter` slice lists exactly that group's patterns. `dotnet test` of `CapabilitiesContractTests` (Release) passed 95. No issues raised.

## Issues
