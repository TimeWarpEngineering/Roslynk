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

- [x] kanban columns, overview, template
- [x] tools/dev-cli and bin/dev; --capabilities works
- [x] .envrc, .githooks, .gitignore entries
- [x] root .editorconfig with [ganda.audit] waivers and reasons
- [x] msbuild/repository.props and root Directory.Build.props (or recorded as not needed)
- [x] ganda repo audit exit 0; Source build unchanged
- [x] AGENTS.md fork workflow section
- [x] Results: list of waived checks and why

## Notes

- Audit baseline before this task (fork master, 2026-10-18): 4 passed, 20 failed, 8 skipped. Failures included missing kanban/, bin/dev, .envrc, root .editorconfig, Directory.Packages.props at root, root .slnx, source/ layout, 231 non-kebab paths, gitignore entries, dev CLI, Nuru reference.
- Related: timewarp-nuru 506 (spike, done), 508 (contracts-first parent), timewarp-architecture 384 (TimeWarp.Contracts 2.0.0-beta.20, done).

## Session

- Created: 2841042 (2026-10-18)

## Results

Final audit: `Passed: 19 | Failed: 2 | Skipped: 6`, exit 0 ("Repository passes", 2 advisory warnings). Baseline was 4 passed, 20 failed, 8 skipped.

### Implemented (additive)

`kanban/overview.md`, `.envrc`, `.githooks/` (the five reference hook runfiles plus symlinks, unchanged), `BannedSymbols.txt`, `msbuild/repository.props` (SolutionFile = `Source/Morris.Roslynk.slnx`, SourceDirectory = `Source/`), root `Directory.Build.props` and `Directory.Packages.props` (tools/ only), root `.editorconfig`, `tools/dev-cli/**`, TimeWarp section appended to `.gitignore`, "TimeWarp fork workflow" section appended to `AGENTS.md`. Nothing under `Source/` changed. Root `Directory.Build.props` and `Directory.Packages.props` are shadowed by the nearer `Source/` files, so Source projects never see them. Root `.editorconfig` has `root = true`; its style sections are scoped to `tools/`, `kanban/`, `msbuild/`. The one `[*.cs]` section holds only the TW0007 filename key, which the audit insists on and which only the (Source-unused) TimeWarp.SourceGenerators analyzer reads.

### Waived checks (`.editorconfig` `[ganda.audit]`)

| Check | Reason |
| --- | --- |
| kebab-path-names | 231 upstream paths are PascalCase; renaming breaks upstream merges |
| slnx | solution is `Source/Morris.Roslynk.slnx`; a root .slnx would duplicate it |
| source-directory-build-props | upstream uses capital `Source/` with its own Directory.Build.props |
| cpm-consistency | `Source/Directory.Packages.props` is the CPM file for Source; root file pins tools/ only |
| directory-structure (exclude documentation, tests) | no root documentation/ or tests/; tests live under `Source/App/*Tests` |
| dev-cli-capabilities | requires check-version, verify-samples, workflow; check-version assumes lowercase `source/`, the others are timewarp-amuru specific |

### Remaining advisory warnings (not waived)

- memsearch-scaffold: flags `git config core.hooksPath=.githooks` (shared repo config, intentionally not touched; same hooksPath as the reference repo).
- vscode-window-icon: needs `assets/Roslynk-avatar.svg` and `.vscode/*`; upstream `.gitignore` ignores `.vscode/`, so skipped.

### Dev CLI endpoints (`bin/dev --capabilities`)

`build` (Release build of the .slnx), `test` (Release test of the .slnx), `clean` and `self-install` (shared TimeWarp.Nuru.DevCli, RepoCleanService from TimeWarp.Amuru.Tools). Excluded in `tools/dev-cli/Directory.Build.targets`: shared `check-version` and `release`, which assume lowercase `source/` and `source/Directory.Build.props` `<Version>`. TimeWarp.Nuru and TimeWarp.Nuru.DevCli are 3.0.0 (the audit's `nuru` check requires latest; the reference's beta.80 failed it).

### Verification

- Baseline `dotnet build Source/Morris.Roslynk.slnx -c Release`: 0 warnings, 0 errors.
- `dotnet run --file tools/dev-cli/dev.cs -- self-install`: published `bin/dev`.
- `bin/dev --capabilities`: JSON catalog with build, test, clean, self-install.
- `bin/dev build`: "Build completed successfully!", 0 warnings.
- `dotnet build Source/Morris.Roslynk.slnx -c Release` after changes (also after deleting Source bin/obj): 0 warnings, 0 errors.
- `ganda repo audit`: exit 0.
- `git status --short Source`: empty.
- `bin/dev test` was not run (the full suite is long); `build` and the Source build cover the acceptance criteria.
- Review pass (2026-10-18, session 1479989): added `.gitattributes` LF rules for `.githooks/*`, `.envrc` and `tools/**/*.cs` (upstream `* text=auto eol=crlf` would check them out with CRLF shebangs); AGENTS.md now names reserve/claim instead of the deprecated `kanban create`. Re-ran `ganda repo audit` (exit 0, 19 passed, 2 advisory) and `bin/dev --capabilities` (build, test, clean, self-install).
