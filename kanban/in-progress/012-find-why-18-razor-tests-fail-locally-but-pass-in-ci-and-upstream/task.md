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

## Goal (implementer restatement)

Find out why 18 Razor tests in `Morris.Roslynk.Tests` fail on Steven's WSL2 machine while CI (ubuntu-latest)
and upstream pass them all, and fix it at the right layer.

Acceptance criteria:
- The failures are reproduced locally with at least one failure message per group.
- The local environment is compared with CI's (SDK, MSBuildLocator's choice, Razor generator loaded).
- The root cause is proven with evidence, testing one hypothesis at a time.
- An engine defect gets an engine fix plus a test that fails without it. A machine-only cause gets
  documentation in AGENTS.md "Testing", plus cheap test-setup hardening. No test is skipped, deleted or weakened.
- The full engine suite passes locally (999/999), or Results names the exact machine-side step.
- `releases.md` changes only if product behavior changes (Fixes #21).

## Checklist

- [x] Reproduced, with failure messages per group
- [x] Local vs CI environment diff (SDK, MSBuildLocator choice, Razor generator loaded)
- [x] Root cause identified with evidence
- [x] Fix at the right layer (engine fix plus test, or documented machine requirement)
- [x] Local full engine suite: 1006/1006 (the 999 plus 7 new)
- [ ] CI green on the PR (the PR is opened by the host's open-pr node after this one)

## Session

- Created: 1287ec9b-6d3e-4805-9dd9-5ef6634981c8 (2026-11-01)
- Implemented under ganda task work (implementer, Claude Opus 5.5), 2026-11-01

## Notes

- Known since task 009. Task 011 counted 18 failures, all Razor. Task 008 ran with SDK 10.0.403 pinned and 14
  still failed.

## Results

### Reproduction (Release, default SDK 11.0.100-rc.3, before the fix)

The Razor filter failed 18 of 106 tests. One message per group:

- `RazorRenameTests` (8), e.g. `WhenRenamingAFieldDeclaredInARazorCodeBlock_ThenTheRazorFileIsRewrittenOnDisk`: `Assert.Contains() Failure: Sub-string not found ... Not found: "private int Total;"`. The Razor file was not rewritten.
- `RazorSourceToolTests` (5), e.g. `WhenFixingACompilerDiagnosticInARazorFile_...`: `error=NotFound errorMessage=No CS0219 dia...`. `WhenListingActionsInsideARazorCodeBlock_...`: `error=NotSupported errorMessage=21:7 in '...` (an `@code` position did not map).
- `RazorSymbolMappingTests` (2), e.g. `WhenGettingTheBodyOfAnInjectProperty_...`: `Not found: "path=RazorInjectLib/Panel.razor\n"`. The body was reported in a `.g.cs` file.
- `ChangeSignatureTests` (2), e.g. `WhenTheMethodIsDeclaredInARazorCodeBlock_...`: `Not found: "private string Label(int value, string pr..."`.
- `GetExpressionInfoTests` (1): `error=NotFound errorMessage=No expression...`.
- `RazorGenerationTests` (1), `WhenTheProjectReferencesALoadableRazorCompiler_ThenItsOwnGeneratorInstanceIsUsed`: `Assert.NotNull() Failure: Value is null`.

### Local vs CI environment

| | CI (run 41291172938) | This machine |
|---|---|---|
| SDKs | 10.0.403 only (`setup-dotnet 10.0.x`) | 10.0.301 to 10.0.403, plus 11.0.100 preview.5, preview.6, rc.1, rc.2 and rc.3 |
| MSBuildLocator (in the test host) | 10.0.403 | 10.0.403, because it lists only the 10.0.x SDKs the net10 host can run |
| SDK the fixtures are evaluated with (out-of-process BuildHost, no global.json) | 10.0.403 | **11.0.100-rc.3** (analyzer refs under `sdk/11.0.100-rc.3.26522.148/...`) |
| Razor compiler the project references | 10.0.403 `Microsoft.CodeAnalysis.Razor.Compiler` 10.4.0.0 | rc.3 `Microsoft.CodeAnalysis.Razor.Compiler` 11.1.0.0 |
| Own reference loads? | yes | **no**: `AnalyzerLoadFailed ReferencesNewerCompiler, ReferencedCompilerVersion=5.12.0.0` (Roslynk ships Roslyn 5.9) |
| Generator Roslynk runs | the project's own instance | the rc.3 DLL, loaded in isolation by `RazorGeneratorLoader` (11.1.0.0) |

There are no `DOTNET_*`/`MSBUILD*` surprises (only `DOTNET_ROOT` and `MSBUILDDISABLENODEREUSE=1`). No global.json exists above the worktree. Fixture `bin/obj` started empty (fresh worktree). The `.razor` files check out CRLF on both machines, because `* text=auto eol=crlf` applies on Linux too.

### Root cause (two layers, each with evidence)

1. **Engine defect.** The .NET 11 Razor compiler emits each component as two documents:
   `X_razor.g.cs` (the render tree) and **`X_razor.decl.g.cs`** (the declarations, including every `@code` member).
   A real `dotnet build -p:EmitCompilerGeneratedFiles=true` of the RazorSolution fixture with SDK 11 rc.3 writes
   both, so this is legitimate compiler output and not an artifact of the isolated load. Roslynk recognized
   only `_razor.g.cs`/`_cshtml.g.cs` (`RazorMapping.IsRazorGeneratedPath`). It resolved a `.razor` file to one
   generated document (`RazorSourceDocument`), so `@code` members were treated as ordinary C#: unmapped,
   never folded back into the `.razor` file, and their positions unreachable. This explains the 17 behavioral
   failures. With only that fix and the fixtures still on SDK 11, all 17 pass.
2. **Environment premise of one test.** `WhenTheProjectReferencesALoadableRazorCompiler_...` asserts that the
   project's own Razor compiler loads. That holds only when the fixtures are evaluated with the SDK band whose
   Razor compiler targets Roslynk's Roslyn (10.0.x for Roslyn 5.9). The out-of-process BuildHost picked the
   newest SDK (11 rc.3). This also explains task 008's "14 still fail with SDK 10.0.403 pinned": that pin was a
   repo-root global.json, and scratch copies live under `/tmp/roslynk-tests/...`, out of its reach. Only the 4
   read-only-fixture tests were fixed by it.

### Fix

- **Engine** (`source/App/Morris.Roslynk`):
  - `RazorMapping.IsRazorOutputFileName`/`RazorOutputStem` recognize `X_razor.decl.g.cs`/`X_cshtml.decl.g.cs`, used by `IsRazorGeneratedPath` and by `RazorSnapshot` orphan matching.
  - `RazorSourceDocument.Documents` lists every generated document of a Razor file, render tree first, as `Document`. `MapToDocumentAsync` now returns `RazorMappedSpan(Document, Span)`, the document holding the span.
  - `ExtractMethodTool`, `GetCodeActionsTool`, `GetExpressionInfoTool` and `ApplyCodeFixTool` take the document from the mapping. The fix tool searches every generated document for the diagnostic.
  - A Razor code-action id carries the generated file name (`ActionRef.GeneratedDocument`, omitted when null, so old ids still decode). `ApplyCodeActionTool` re-discovers in that document.
  - `RazorUnusedUsings` probes every generated document. A `@using` is removed only when every document carrying it finds it unused. The render tree is fully qualified, so probing it alone would have removed `@using Microsoft.AspNetCore.Components`, which `[Parameter]` needs.
- **Tests**:
  - `RazorSplitDeclarationTests` (3) rewrites the fixture's Counter into the split .NET 11 shape, so CI (SDK 10 only) covers it. `WhenAUsingIsNeededOnlyByTheDeclarations_...` fails if only the first document is probed (mutation-checked).
  - `RazorSnapshotTests.WhenADeclarationFileHasNoSource_ThenItIsDroppedLikeItsRenderTreeFile`.
  - 3 new `IsRazorGeneratedPath` cases.
  - No existing test was skipped, removed or weakened.
- **Test setup**: `source/TestFixtures/global.json` pins the fixtures to the 10.0 SDK band (`10.0.100`, `latestFeature`, no prerelease). `TestSolutions.CreateScratch` copies it beside each scratch copy. This makes the local fixture environment match CI.
- **Docs**: AGENTS.md (Razor section: the split; Testing: the fixture SDK pin and why), `releases.md` under Unreleased (Fixes #21).

### Verification

- `dotnet test source/App/Morris.Roslynk.Tests -c Release`: **1006/1006 passed** (the 999 plus 7 new).
- `dotnet test source/App/Morris.Roslynk.Cli.Tests -c Release`: 336/336 passed.
- With the fixture pin temporarily set to SDK 11 rc.3, the Razor, ChangeSignature, GetExpressionInfo, CodeAction and Using filter passed 160/162. All 16 behavioral tests from the original failure list passed. One failure was the own-compiler test, whose premise needs the 10.0 band. The other was the new split-test helper adding a second decl document where SDK 11 already emits one. The helper now replaces an existing decl document, and the 3 split tests then passed under SDK 11 too (separate run).
- `ganda repo audit`: passes.
- No machine-side step is needed from Steven. Any 10.0.x SDK must be installed, which building the repo already requires.

### How to validate

Smoke:
```bash
dotnet test source/App/Morris.Roslynk.Tests/Morris.Roslynk.Tests.csproj -c Release \
  --filter "FullyQualifiedName~Razor|FullyQualifiedName~ChangeSignature|FullyQualifiedName~GetExpressionInfo"
dotnet test source/App/Morris.Roslynk.Tests/Morris.Roslynk.Tests.csproj -c Release
```

Expect:
- On a machine with SDK 11 previews/RCs installed beside 10.0, the first command passes every test, including `RazorSplitDeclarationTests` (3) and `RazorGenerationTests.WhenTheProjectReferencesALoadableRazorCompiler_ThenItsOwnGeneratorInstanceIsUsed`.
- The second command reports `Passed: 1006, Failed: 0`.
- CI (SDK 10 only) stays green.
