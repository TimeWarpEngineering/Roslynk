# Round 1 — general
**Date:** 2026-11-08
**Scope reviewed:** branch 3266f4f vs merge-base 32b9352 (see review-framework.md)

## Summary

The change does what the task asks. `ganda repo audit` exits 0 here (re-run by the reviewer), `--fix` untracked `.vscode/settings.json` (`.gitignore` already has `.vscode/`), and `dev-cli-capabilities` already passed on the rebuilt `bin/dev`. The `workflow-command.cs` split is a real split by responsibility: a standalone `internal sealed class WorkflowReleaseSteps`, not a partial-class shard. A line-set comparison against the 705-line original shows only access changes (private to internal), the new `steps.`/`WorkflowReleaseSteps.` qualifiers, the constructor and the aliases. `RunReleaseAsync` keeps its step order and passes `Ct` through. Both files are at most 390 lines, `dotnet build tools/dev-cli/dev.cs` succeeds, `bin/dev` was rebuilt after the split, `bin/dev build` passes with 0 warnings, and `bin/dev workflow --mode nope` exits 1 with the expected message.

The one theme is the line-cap waiver's stated reason. Setting `severity = off` is the right outcome, but the comment and AGENTS.md give a factually wrong reason for it.

The restatement matches the task. The task allowed "exclude/prune for source/ or a per-path exclude, whichever the check supports". Turning the check off for the whole repo is broader than that, but the restatement already covers it and the reason is documented, so this is not a misread.

## Issues

### Issue 1 — Severity: suggestion
- File: .editorconfig:317 (comment 318–322); AGENTS.md:283; task.md `## Session` / `## Results`
- Description: The waiver says the check "has no path exclude" and "cannot waive source/ alone". The installed ganda (1.0.0-beta.38+61e4dc919, which includes fa3b129e6 "ratchet C# files over 500 lines" and 08ff73676) supports a per-path baseline, `.timewarp/line-cap-baseline.json`. That baseline is a ratchet: once origin's default branch has it, a recorded file may only shrink, and raising a recorded count is an Error (timewarp-ganda `documentation/developer/standards/repo-audit.md`, `source-file-line-cap`). That is why `severity = off` is still correct for this fork. An upstream merge that grows any of the 12 upstream files would fail the audit, and the count could not be raised. The comment is the only record of why the cap is off. It should name the baseline it rejected and give the real reason. Otherwise a later agent will "fix" the waiver by adding a baseline that breaks the next upstream merge, or will trust a false claim about the tool.
- Suggestion: Reword the `.editorconfig` comment to say that the check's only per-path mechanism is the `.timewarp/line-cap-baseline.json` ratchet, which only lets recorded files shrink. Upstream merges grow these `source/` files, so a baseline would fail the audit on the next upstream merge, and the check is therefore off. Keep the note that fork-owned `tools/` was split and task 013. Make the same correction to the AGENTS.md sentence ("has no path exclude" → "its only per-path waiver is a shrink-only baseline that upstream merges would break") and to the task.md Session/Results wording.
- Status: open

### Issue 2 — Severity: nit
- File: .editorconfig:319
- Description: The comment cites `documentation/developer/standards/repo-audit.md` as if it were in this repo. That path does not exist in Roslynk; the file is in TimeWarpEngineering/timewarp-ganda.
- Suggestion: Write it as `timewarp-ganda documentation/developer/standards/repo-audit.md (source-file-line-cap)`.
- Status: open

<!-- Considered and not raised: the eight `using X = WorkflowReleaseSteps.X;` aliases in workflow-command.cs:20-27 keep the moved code verbatim and are a reasonable minimal-diff choice. -->
