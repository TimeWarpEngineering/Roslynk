# Fix ganda repo audit failures

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

- [ ] Re-run `ganda repo audit` in this worktree to get the current failure list
- [ ] `ganda repo audit --fix`; commit its changes
- [ ] Resolve dev-cli-capabilities (stale binary vs. real gap)
- [ ] Split `tools/dev-cli/endpoints/workflow-command.cs` under 500 lines; `bin/dev workflow` still works
- [ ] Waive line cap for upstream `source/` files in `.editorconfig` `[ganda.audit]` with reason + task 013
- [ ] Update the AGENTS.md "TimeWarp fork workflow" waiver sentence if the waiver list changes
- [ ] `bin/dev build` and `bin/dev test` pass
- [ ] `ganda repo audit` exits 0

## Session

- Created: 93b68595-4648-49ee-a58b-6a1ec1f5dab9 (2026-11-08)

## Notes

No `releases.md` entry needed (tooling/audit only, no feature change).
