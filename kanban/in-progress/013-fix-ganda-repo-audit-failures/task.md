# Fix ganda repo audit failures

## Goal (implementer restatement)

`ganda repo audit` must exit 0 in this worktree so later PRs can clear the pre-PR gate. The line-cap check stays off. The written reason must name the shrink-only baseline and say why that baseline does not fit a fork that merges upstream growth.

Acceptance:

- The audit's current failure list is re-checked here (the master `bin/dev` may have been stale).
- `ganda repo audit --fix` changes are committed, including untracking `.vscode/settings.json`.
- `bin/dev` exposes `check-version`, `verify-samples`, and `workflow`. If the rebuilt binary already does, no product change is required for that check.
- `tools/dev-cli/endpoints/workflow-command.cs` is split by responsibility so every fork-owned file stays at or under 500 lines, and `bin/dev workflow` still runs.
- Upstream files under `source/` that exceed 500 lines stay unsplit. The root `.editorconfig` keeps `source-file-line-cap.severity = off` under `[ganda.audit]`, with a reason and task 013.
- That reason names `.timewarp/line-cap-baseline.json` as the only per-path mechanism, says a recorded count may only shrink, and says an upstream merge that grows a `source/` file would fail the audit. The same correction is in the AGENTS.md fork-workflow sentence and in this kitchen's Session and Results.
- The comment cites the standard as `timewarp-ganda documentation/developer/standards/repo-audit.md (source-file-line-cap)`.
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
- [x] Round 1 M1 and M2: the waiver reason names the shrink-only baseline, and the doc citation names timewarp-ganda
- [x] `bin/dev build` and `bin/dev test` pass
- [x] `ganda repo audit` exits 0

## Session

- Created: 93b68595-4648-49ee-a58b-6a1ec1f5dab9 (2026-11-08)
- Implementation: 01a1bc05-18b3-7571-a4bd-ce015eb18695 (2026-11-08)

`ganda repo audit` in this worktree: dev-cli-capabilities already passed (the rebuilt `bin/dev` lists `check-version`, `verify-samples`, and `workflow`). Failures were `source-file-line-cap` (13 files) and `vscode-settings-gitignore`. `--fix` ran `git rm --cached` on `.vscode/settings.json`. `.gitignore` already ignored `.vscode/`.

The first pass said `source-file-line-cap` has no path exclude. That is wrong. The check's only per-path mechanism is the shrink-only `.timewarp/line-cap-baseline.json` ratchet: once origin's default branch records a file, that count may only shrink. An upstream merge that grows a `source/` file over 500 lines would fail the audit, and the recorded count could not be raised. The waiver stays `source-file-line-cap.severity = off`, with that reason and task 013. Fork-owned `workflow-command.cs` is still split so `tools/` stays under 500 lines.
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1bc05-18b3-7571-a4bd-ce015eb18695, max-turns 500, Done — 2026-11-08T16:56:15Z
- Review oracle round 1: Claude Opus 5.5 (general reviewer; a Sonnet helper subagent compared the split method by method), effort 2, turn cap 120. Result: 0 bug, 1 suggestion, 1 nit, all open. Fix pass requested — 2026-11-08
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5), session not reported, max-turns 120, artifacts general.md, merged.md — 2026-11-08T16:59:25Z
- Fix pass (review round 1 M1, M2): reworded the line-cap waiver so it names the shrink-only baseline and cites the timewarp-ganda standard. Severity stays `off`. — 2026-11-08

## Results

`ganda repo audit` exits 0 (34 passed, 0 failed, 2 skipped).

- `dev-cli-capabilities` already passed on the worktree `bin/dev`. No command was missing.
- `ganda repo audit --fix` untracked `.vscode/settings.json`. The ignore was already in `.gitignore`.
- Release-mode git and GitHub Actions steps moved from `WorkflowCommand` into `WorkflowReleaseSteps`. `workflow-command.cs` is 390 lines. `workflow-release-steps.cs` is 350 lines.
- `source-file-line-cap` stays `off` under `[ganda.audit]`. Its only per-path mechanism is the shrink-only `.timewarp/line-cap-baseline.json` ratchet. An upstream merge that grows a `source/` file would fail that ratchet, so there is no baseline in this fork. The AGENTS.md fork-workflow sentence says the same thing. Upstream files under `source/` were not split. Fork-owned `tools/` was split and stays under 500 lines. The comment cites `timewarp-ganda documentation/developer/standards/repo-audit.md (source-file-line-cap)`.

### Review

- Round 1 (effort 2, roster general): 0 bug, 1 suggestion, 1 nit. Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`.
- M1 (suggestion): fixed in this pass. The waiver comment, the AGENTS.md sentence, and the Session and Results wording name `.timewarp/line-cap-baseline.json` and say the ratchet only shrinks, so an upstream merge that grows a `source/` file would fail the audit. Severity stays `off`.
- M2 (nit): fixed in this pass. The comment cites `timewarp-ganda documentation/developer/standards/repo-audit.md (source-file-line-cap)`.

### How to validate

Smoke: from this worktree, run `ganda repo audit`. Then read the `source-file-line-cap` block in the root `.editorconfig` and the last sentence of the AGENTS.md "TimeWarp fork workflow" section.

Expect: `ganda repo audit` exits 0 and prints `Repository passes all audit checks.` `source-file-line-cap.severity` is still `off`. The comment names `.timewarp/line-cap-baseline.json`, says a recorded count may only shrink, and cites `timewarp-ganda documentation/developer/standards/repo-audit.md (source-file-line-cap)`. The AGENTS.md sentence says the only per-path waiver is that shrink-only baseline and that an upstream merge would break it. No C# file changed in this pass, so `bin/dev build` and `bin/dev test` were not re-run. The earlier run still stands: build exits 0 with 0 warnings and 0 errors, Morris.Roslynk.Cli.Tests 336 passed, Morris.Roslynk.Tests 1006 passed. `bin/dev workflow --mode nope` prints `Unknown --mode 'nope'. Valid values: pr, merge, release.` and exits 1.

A local `workflow --mode release` reaches the tag-pin step (`v1.0.0-beta.1 not yet tagged`) and then aborts because this task commit is not an ancestor of master. That abort is the existing release gate.

## Review

- Round 1: Blocked — open M1, M2
- Fix pass: M1 and M2 addressed in `.editorconfig`, `AGENTS.md`, and this kitchen. Severity stays `off`.

## Notes

No `releases.md` entry needed (tooling/audit only, no feature change).
