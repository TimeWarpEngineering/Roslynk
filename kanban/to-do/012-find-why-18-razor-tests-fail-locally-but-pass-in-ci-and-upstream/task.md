# Find why 18 Razor tests fail locally but pass in CI and upstream

## Description

Fork issue #21. Steven (2026-11-01): "the Razor test pass for Pete, so we need to investigate that more".

On ubuntu-latest CI (run 41291172938, PR #20), `Morris.Roslynk.Tests` passes **999/999**, and upstream
(mrpmorris/Roslynk) passes the same tests. On Steven's machine (WSL2, Linux 6.18) the same commit fails **18**
Razor tests. It fails with the default SDK 11.0.100-rc.3, and 14 of them still fail with a temporary
global.json pinned to SDK 10.0.403 (task 008's Results).

Failing groups:
- `RazorRenameTests` ×8
- `RazorSourceToolTests` ×5
- `RazorSymbolMappingTests` ×2
- `ChangeSignatureTests` Razor cases ×2
- `GetExpressionInfoTests` Razor ×1
- `RazorGenerationTests` ×1

Something in this environment differs from CI. Candidates (unverified):
- The installed SDK/workload set and which Razor compiler `RazorGeneratorLoader` loads. That covers the
  project's shadow-loaded analyzer reference vs the SDK DLL, and SDK 11 rc beside SDK 10 when MSBuildLocator
  picks the newest.
- Fixture restore state: stale `bin/obj` in `source/TestFixtures/*` from earlier SDKs, or the
  `RazorSnapshot` fallback picking up old build output.
- `DOTNET_*` / `MSBUILD*` environment variables, a NuGet config, or global.json lookup from parent
  directories (`/home/steve/worktrees/...`).
- Line endings: the CRLF policy for C#, and `.razor` fixtures checked out with different endings.
- Case-sensitive paths, or a leftover path from before the `Source/` → `source/` move.
- Parallel test runs sharing a fixture.

## Requirements

- **Reproduce first.** Run the Razor filter in Release and record the exact failure messages and stack
  traces, at least one per group.
- **Compare with CI.** CI's environment is in the run log (`gh run view 41291172938 --repo
  TimeWarpEngineering/Roslynk --log`): SDK version, `dotnet --info`, restore output. Compare it with
  `dotnet --info` here, MSBuildLocator's choice (log it from a test if needed), and the Razor generator
  assembly actually loaded (its path and version).
- Find the **root cause** by testing hypotheses one at a time, each with evidence. Examples: a clean clone in
  a fresh directory, `git clean -xdf source/TestFixtures`, pinning the SDK, unsetting env vars, a container
  run of ubuntu with the CI SDK (`mcr.microsoft.com/dotnet/sdk:10.0`) if docker is available.
- **Fix the right layer:**
  - If the engine mishandles a legitimate environment (for example multiple SDKs installed, or loading the
    wrong Razor compiler), fix the engine. Add a test that fails without the fix, if one can be written.
  - If it is purely this machine's state (stale files, a stray env var), do not change product code.
    Document the requirement or cleanup in AGENTS.md "Testing", and make the test setup robust if cheap. For
    example, the fixture restore could detect stale output.
  - Never skip, quarantine or delete the tests, and never weaken their assertions.
- **Verify:** after the fix, `dotnet test source/Morris.Roslynk.slnx -c Release` locally passes 999/999,
  or Results explains exactly which machine-side step Steven must take. CI stays green on the PR.
- `releases.md` under `# Unreleased` only if product behavior changes (Fixes #21).

## Checklist

- [ ] Reproduced, with failure messages per group
- [ ] Local vs CI environment diff (SDK, MSBuildLocator choice, Razor generator loaded)
- [ ] Root cause identified with evidence
- [ ] Fix at the right layer (engine fix plus test, or documented machine requirement)
- [ ] Local full engine suite 999/999, or the exact remaining step for Steven
- [ ] CI green on the PR

## Session

- Created: 1287ec9b-6d3e-4805-9dd9-5ef6634981c8 (2026-11-01)

## Notes

- Known since task 009. Task 011 counted 18 failures, all Razor. Task 008 ran with SDK 10.0.403 pinned and 14
  still failed.
