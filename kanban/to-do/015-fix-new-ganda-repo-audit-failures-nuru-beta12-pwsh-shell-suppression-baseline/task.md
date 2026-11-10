# Fix new ganda repo audit failures (Nuru beta.12, pwsh shell, suppression baseline)

## Description

After a ganda update, `ganda repo audit` on master (2026-11-11, after task 014 merged) fails 6 checks that
were not there when task 013 passed. All six are marked fixable. Make the audit exit 0.

| Check | Severity | Detail |
| --- | --- | --- |
| `analyzer-suppressions` | Error | `.timewarp/suppression-baseline.json` missing; `--fix` seeds it from the current tree |
| `nuru` | Error | TimeWarp.Nuru 4.0.0-beta.11 < 4.0.0-beta.12 in `Directory.Packages.props` |
| `workflow-pwsh-shell` | Error | `.github/workflows/workflow.yml` lacks top-level `defaults: run: shell: pwsh` |
| `ci-code-coverage` | Warning | CI collects coverage but has no `actions/upload-artifact` step for it |
| `memsearch-scaffold` | Warning | `.githooks/{post-commit,post-merge,post-checkout,pre-commit,pre-push}.cs` outdated |
| `vscode-window-icon` | Warning | `.vscode/settings.json` missing (it is git-ignored since task 013; check whether `--fix` only writes a local file or wants something tracked) |

## Requirements

- Run `ganda repo audit --fix` and commit everything it changes. That is policy: do not revert or second-guess
  `--fix` output; mention it as fact, and act only if it breaks the build or tests.
- Nuru beta.12: bump the whole TimeWarp.Nuru family together (Nuru, DevCli, Daemon, McpHost) if `--fix` bumps
  only one. Read the Nuru beta.12 release notes / changes since beta.11 and adapt Roslynk to any breaking change.
  Check whether beta.12 contains timewarp-nuru task 569 (slow `check-version`); note in Results.
- pwsh default shell: make sure every existing `run:` step in `workflow.yml` still works under pwsh (the
  workflow is a thin trigger for `dev workflow`); fix any bash-only step.
- Coverage upload: the artifact path must match where `dev workflow` / tests write coverage.
- Anything `--fix` cannot fix: fix by hand, or waive under `[ganda.audit]` in `.editorconfig` with a reason and
  task id (only if the check truly does not apply).
- `releases.md` `# Unreleased`: no new bullet (audit/tooling task, no fork issue). If the Nuru bump changes
  user-visible behavior, say so in Results so a bullet and issue can be added.

## Checklist

- [ ] `ganda repo audit --fix`; commit its changes
- [ ] Nuru family at 4.0.0-beta.12; build adapted
- [ ] workflow.yml steps verified under pwsh; coverage uploaded
- [ ] `bin/dev build` and `bin/dev test` pass
- [ ] `ganda repo audit` exits 0

## Session

- Created: 93b68595-4648-49ee-a58b-6a1ec1f5dab9 (2026-11-11)

## Notes

- Prior audit task: 013.
