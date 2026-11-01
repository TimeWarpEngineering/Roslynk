# Round 1 — general
**Date:** 2026-11-01
**Scope reviewed:** product commit `0ebd960` against the original task and `## Goal (implementer restatement)`

## Summary

The Goal restatement matches the original task. Both ask for a reproduced failure, a local-versus-CI comparison, a proven root cause, an engine fix plus a test when the engine is wrong, documentation and cheap setup hardening when the cause is the machine, no skipped or weakened tests, and a `releases.md` line only because product behavior changed.

The engine change is the right layer. `RazorMapping.IsRazorOutputFileName` and `RazorOutputStem` treat `X_razor.decl.g.cs` and `X_cshtml.decl.g.cs` as the same component as `X_razor.g.cs`. `RazorSourceDocument` lists every generated document, maps a span into the document that holds it, and `GetCodeActions`, `ApplyCodeAction`, `ApplyCodeFix`, `ExtractMethod`, and `GetExpressionInfo` use that document. Rename and change signature already fold through `IsRazorGeneratedPath`, so declaration-file edits now map back onto the `.razor` file. `RazorGeneratedChangeFolder` unions edits from both generated files by Razor span before writing. `RazorUnusedUsings` drops an `@using` only when every generated document that carries it reports it unused, which keeps `@using Microsoft.AspNetCore.Components` when only the declaration document needs it. `RazorSnapshot` claims a declaration file from the same source as its render-tree file.

The fixture pin matches the second layer. `source/TestFixtures/global.json` is `10.0.100` with `rollForward: latestFeature` and `allowPrerelease: false`, so a machine that also has SDK 11 still evaluates fixtures on the 10.0 band. `CreateScratch` copies that file beside the scratch copy, because scratch directories live outside `TestFixtures/`. In-place fixtures sit under `TestFixtures/` and see the same file. No test is skipped, deleted, or weakened. `releases.md` and AGENTS.md record the split and the pin.

Release filter on this tree (`RazorSplitDeclaration`, `RazorSnapshotTests`, `RazorGenerationTests`, `RazorRenameTests`, `RazorSymbolMapping`, `RazorSourceToolTests`, `ChangeSignatureTests`, `GetExpressionInfoTests`): 92 passed, 0 failed, 0 skipped. The test host printed SDK 11.0.100-rc.3; the previously failing groups are in that 92, so the fixture pin is holding on this machine. The full 1006 and CLI 336 runs were not repeated. CI on the PR is still the later open-pr node.

## Issues

None.
