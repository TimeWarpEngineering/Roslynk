# Adopt ganda workflow on the fork: kanban board, dev CLI, audit configuration

## Description

Make the TimeWarpEngineering fork of Roslynk operable with the standard TimeWarp tooling (ganda kanban, `bin/dev`, repo audit, task worktrees) without restructuring Peter Morris's layout under `Source/`. Steven decided 2026-10-18: treat the fork as ours; Pete incorporates what he wants upstream. The Roslynk contracts-first work (serial plan on timewarp-nuru 508 and timewarp-architecture 384) then lands here as ordinary tasks.

Rule for each failing audit check: implement it when the fix is additive (new files at the root, new directories, ignore entries); switch it off in `.editorconfig` under `[ganda.audit]` with a comment when passing it would move or rename Pete's files (kebab-case paths, root `.slnx`, `source/` layout, central package files at the root).

## Requirements

- `kanban/` with the standard columns, `overview.md` and `task-template.md` (reference: timewarp-amuru).
- `tools/dev-cli` on TimeWarp.Nuru with `build`, `test`, `clean` against `Source/Morris.Roslynk.slnx`, plus `self-install` producing `bin/dev`; `bin/dev --capabilities` works. Shared `TimeWarp.Nuru.DevCli` endpoints only where they work with this layout.
- `.envrc` (`PATH_add bin`), `.githooks` as in the reference, root `.gitignore` entries the audit requires.
- Root `.editorconfig` carrying the `[ganda.audit]` section; `Source/.editorconfig` (tabs, Pete's style) stays authoritative for `Source/`.
- `msbuild/repository.props` and a root `Directory.Build.props` only if they do not change how `Source/` builds (the nearer `Source/Directory.Build.props` must remain the one Source projects pick up).
- `ganda repo audit` exits 0 in the task worktree; `dotnet build Source/Morris.Roslynk.slnx` is unchanged.
- `AGENTS.md` gains a short section on the fork workflow (board, `bin/dev`, ganda) without altering Pete's guidance.

## Checklist

- [ ] kanban columns, overview, template
- [ ] tools/dev-cli and bin/dev; --capabilities works
- [ ] .envrc, .githooks, .gitignore entries
- [ ] root .editorconfig with [ganda.audit] waivers and reasons
- [ ] msbuild/repository.props and root Directory.Build.props (or recorded as not needed)
- [ ] ganda repo audit exit 0; Source build unchanged
- [ ] AGENTS.md fork workflow section
- [ ] Results: list of waived checks and why

## Notes

- Audit baseline before this task (fork master, 2026-10-18): 4 passed, 20 failed, 8 skipped. Failures included missing kanban/, bin/dev, .envrc, root .editorconfig, Directory.Packages.props at root, root .slnx, source/ layout, 231 non-kebab paths, gitignore entries, dev CLI, Nuru reference.
- Related: timewarp-nuru 506 (spike, done), 508 (contracts-first parent), timewarp-architecture 384 (TimeWarp.Contracts 2.0.0-beta.20, done).

## Session

- Created: 2841042 (2026-10-18)
