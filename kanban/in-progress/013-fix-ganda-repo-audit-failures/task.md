# Fix ganda repo audit failures

## Goal (implementer restatement)

`ganda repo audit` must exit 0 in this worktree so later PRs can clear the pre-PR gate.

Acceptance:

- The audit's current failure list is re-checked here (the master `bin/dev` may have been stale).
- `ganda repo audit --fix` changes are committed, including untracking `.vscode/settings.json`.
- `bin/dev` exposes `check-version`, `verify-samples`, and `workflow`. If the rebuilt binary already does, no product change is required for that check.
- `tools/dev-cli/endpoints/workflow-command.cs` is split by responsibility so every fork-owned file stays at or under 500 lines, and `bin/dev workflow` still runs.
- Upstream files under `source/` that exceed 500 lines are waived in the root `.editorconfig` under `[ganda.audit]`, with a reason comment and task 013. They are not split.
- The AGENTS.md "TimeWarp fork workflow" sentence lists the waiver set if that set changes.
- `bin/dev build` and `bin/dev test` pass, then `ganda repo audit` exits 0.

## Description

`ganda repo audit` on master (32b9352, 2026-11-08) reports blocking failures. Make it pass
(exit 0) so PRs can clear the pre-PR gate.

Failures seen from the master checkout:

1. **dev-cli-capabilities** (Error): missing required commands `check-version`,
   `verify-samples`, `workflow`. AGENTS.md says these exist, so the master `bin/dev` is likely
   stale. This worktree's on-create rebuilt `bin/dev`; re-run the audit here first and only
   change code if it still fails.
2. **source-file-line-cap** (Error): 13 C# files exceed 500 lines:
   - Fork-owned: `tools/dev-cli/endpoints/workflow-command.cs` (705) — **split it by
     responsibility**.
   - Upstream-owned (everything under `source/`): GetCallersCoverageTests, GetDiagnosticsTests,
     MultiQueryLimitTests, ApplyPatchTests, RazorSourceToolTests, SolutionFileSyncTests,
     GetDiagnosticsTool, GetSymbolBodyTool, RoslynInstance, SymbolResolver, SymbolSignature,
     SolutionFileSync. Do **not** split these: splitting upstream files breaks upstream merges.
     Waive them in the root `.editorconfig` under `[ganda.audit]` (exclude/prune for `source/`
     or a per-path exclude, whichever the check supports), with a reason comment and the task
     id, following the existing waivers for `kebab-path-names`, `slnx`, `cpm-consistency`.
3. **vscode-settings-gitignore** (Error): `.vscode/settings.json` is tracked. Run
   `ganda repo audit --fix` (it does `git rm --cached` and adds the ignore). Commit whatever
   `--fix` changes; that is policy.

## Checklist

- [x] Re-run `ganda repo audit` in this worktree to get the current failure list
- [x] `ganda repo audit --fix`; commit its changes
- [x] Resolve dev-cli-capabilities (stale binary vs. real gap)
- [x] Split `tools/dev-cli/endpoints/workflow-command.cs` under 500 lines; `bin/dev workflow` still works
- [x] Waive line cap for upstream `source/` files in `.editorconfig` `[ganda.audit]` with reason + task 013
- [x] Update the AGENTS.md "TimeWarp fork workflow" waiver sentence if the waiver list changes
- [x] `bin/dev build` and `bin/dev test` pass
- [x] `ganda repo audit` exits 0

## Session

- Created: 93b68595-4648-49ee-a58b-6a1ec1f5dab9 (2026-11-08)
- Implementation: 01a1bc05-18b3-7571-a4bd-ce015eb18695 (2026-11-08)

`ganda repo audit` in this worktree: dev-cli-capabilities already passed (the rebuilt `bin/dev` lists `check-version`, `verify-samples`, and `workflow`). Failures were `source-file-line-cap` (13 files) and `vscode-settings-gitignore`. `--fix` ran `git rm --cached` on `.vscode/settings.json`. `.gitignore` already ignored `.vscode/`.

`source-file-line-cap` has no path exclude. The cap is a constant, and the check refuses a path allowlist. The waiver is `source-file-line-cap.severity = off` with a reason and task 013. Fork-owned `workflow-command.cs` is still split so `tools/` stays under 500 lines.

## Results

`ganda repo audit` exits 0 (34 passed, 0 failed, 2 skipped).

- `dev-cli-capabilities` already passed on the worktree `bin/dev`. No command was missing.
- `ganda repo audit --fix` untracked `.vscode/settings.json`. The ignore was already in `.gitignore`.
- Release-mode git and GitHub Actions steps moved from `WorkflowCommand` into `WorkflowReleaseSteps`. `workflow-command.cs` is 390 lines. `workflow-release-steps.cs` is 350 lines.
- `source-file-line-cap` has no path exclude, so the waiver is `source-file-line-cap.severity = off` under `[ganda.audit]`, with the reason and task 013. The AGENTS.md fork-workflow sentence names that check. Upstream files under `source/` were not split.

### How to validate

Smoke: from this worktree, run `ganda repo audit`, then `bin/dev workflow --mode nope`, then `bin/dev build`, then `bin/dev test`.

Expect: `ganda repo audit` exits 0 and prints `Repository passes all audit checks.` `bin/dev workflow --mode nope` prints `Unknown --mode 'nope'. Valid values: pr, merge, release.` and exits 1. `bin/dev build` exits 0 with 0 warnings and 0 errors. `bin/dev test` exits 0 with Morris.Roslynk.Cli.Tests 336 passed and Morris.Roslynk.Tests 1006 passed.

A local `workflow --mode release` reaches the tag-pin step (`v1.0.0-beta.1 not yet tagged`) and then aborts because this task commit is not an ancestor of master. That abort is the existing release gate.

## Notes

No `releases.md` entry needed (tooling/audit only, no feature change).
