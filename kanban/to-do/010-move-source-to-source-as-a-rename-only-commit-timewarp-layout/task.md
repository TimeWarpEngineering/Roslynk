# Move Source/ to source/ as a rename-only commit (TimeWarp layout)

## Description

Fork issue #16. Steven (2026-10-23): "we have our structure this is Pete's structure... do we really want to
add configuration? TimeWarp is going for consistency." Then (2026-10-24): "move Source/ to source/ only first
we will decide on the others later".

TimeWarp repos keep their code under lowercase `source/`, with `source/Directory.Build.props`. DevCli
(`check-version`, `release`, the packable-project scan) hard-codes that path, which blocked task 008. Roslynk
still has upstream's `Source/` (453 tracked files). Upstream (mrpmorris/Roslynk) was merged through `dae11b1`
in task 009. A **pure directory rename** keeps later upstream merges working, because git's rename detection
places upstream changes under `Source/` into `source/`.

**Scope:** only the top-level directory. Project, file, folder and namespace names under it stay as they are
(`Morris.Roslynk.slnx`, `App/Morris.Roslynk.Cli`, `TestFixtures/...`). Kebab-case is a later decision.

## Requirements

- **Commit 1 is rename only:** `git mv Source source`. No content changes in that commit, so every path is a
  100% rename. Check this with `git show --stat -M HEAD` / `git diff --summary HEAD~1 HEAD` (only `rename`
  lines). On case-insensitive filesystems, git needs a two-step move (`Source` → temporary name → `source`) in
  the same commit. This machine is Linux, but say so in Results for Windows/macOS clones.
- **Commit 2 updates the references** to the new path, everywhere outside `kanban/` (kanban history stays as
  written):
  - `.editorconfig`, `.gitignore`, `Directory.Build.props`, `Directory.Packages.props`
  - `msbuild/repository.props`, `installer/run.sh`, `.github/workflows/workflow.yml`
  - `tools/dev-cli/Directory.Build.targets`, `tools/dev-cli/dev.cs`, `tools/dev-cli/endpoints/build-command.cs`,
    `tools/dev-cli/endpoints/test-command.cs`
  - `README.md`, `AGENTS.md` (including "solution directory (`Source`)" prose), `skills/roslynk/SKILL.md`
  - Everything else that `git grep -n 'Source[/\\]'` and `git grep -n '"Source"'` find, including test code
    that builds fixture paths and the Aspire AppHost.
  - Do not touch `Source`, which is a word in C# identifiers and prose (e.g. `RazorSourceDocument`,
    `GeneratedSource`). Change it only where it is the directory.
- **Audit waivers:** in `[ganda.audit]`, remove each waiver whose check now passes on its own. That probably
  includes `source-directory-build-props`, and maybe `cpm-consistency` and `slnx` (check each one by removing
  it and running `ganda repo audit`). Keep `kebab-path-names`, `directory-structure.exclude` and
  `dev-cli-capabilities` (008 owns that last one). Update the reasons on the waivers that stay so they no
  longer cite `Source/`. Apply and commit every `ganda repo audit --fix` change.
- **Do not** start 008's work: no DevCli `check-version`/`release` restore, no Version, no CI. Do mention
  anything you notice that unblocks or changes 008.
- **Verify:**
  - `dotnet build source/Morris.Roslynk.slnx -c Release` gives 0 warnings and 0 errors.
  - `dotnet test source/Morris.Roslynk.slnx -c Release` matches the post-009 baseline: engine 18 known Razor
    failures, CLI 0 failures. Any other failure is a path bug; fix it.
  - `bin/dev build` and `bin/dev test` work (run `dev self-install` first if `bin/dev` is stale).
  - `ganda repo audit` passes.
  - `git log --follow` on one file (for example `source/App/Morris.Roslynk/Infrastructure/Writing/AtomicFileWriter.cs`)
    shows its pre-move history.
- `releases.md`: under `# Unreleased`, add "Moved the source tree from `Source/` to `source/` (Fixes #16)".

## Goal (implementer restatement)

Rename the top-level `Source/` directory to lowercase `source/` so Roslynk matches the TimeWarp layout that
DevCli assumes, without disturbing upstream merges.

Acceptance:

- One commit that is nothing but `git mv Source source` (every entry a 100% rename, no content edits).
- A second commit that repoints every path reference to the directory (outside `kanban/`) to `source/`, leaving
  the word `Source` in identifiers and prose alone.
- `[ganda.audit]` waivers that now pass on their own are removed; the remaining ones no longer cite `Source/`;
  `ganda repo audit --fix` output committed; `ganda repo audit` clean.
- Release build has 0 warnings/0 errors; tests match the 009 baseline (engine: 18 known Razor failures, CLI: 0);
  `bin/dev build` / `bin/dev test` work.
- `git log --follow` on a moved file still shows pre-move history.
- `releases.md` Unreleased bullet with `(Fixes #16)`; AGENTS.md/README/SKILL.md updated.
- Results note what changes for 008 and for local tooling that hard-codes `Source/Morris.Roslynk.slnx`.
- No 008 work (DevCli check-version/release, Version, CI).

## Checklist

- [x] Commit 1: `git mv Source source`, renames only (verified)
- [x] Commit 2: every path reference outside kanban/ updated
- [x] Waivers that now pass removed, the rest re-reasoned; audit `--fix` committed; audit clean
- [x] Release build clean; tests match the 009 baseline; `bin/dev build`/`test` work
- [x] `git log --follow` keeps history
- [x] AGENTS.md / README / SKILL.md / releases.md (Fixes #16)
- [x] Results: what changed for 008, and a note for local tooling (MCP configs or scripts that hard-code
      `Source/Morris.Roslynk.slnx`)

## Session

- Created: 1287ec9b-6d3e-4805-9dd9-5ef6634981c8 (2026-10-24)
- 2026-10-24 implement (ganda task work, Claude): rename commit, reference commit, audit-fix commit; build,
  tests, `bin/dev`, audit and `--follow` verified (see Results).

## Results

Three product commits on the task branch:

1. `refactor: move Source/ to source/ (rename only)`: `git mv Source source`. `git show --stat -M` shows 453
   files, 0 insertions and 0 deletions. `git diff --summary HEAD~1 HEAD` has 453 lines, every one a `(100%)`
   rename. This machine is Linux. **Windows/macOS clones** (case-insensitive filesystems): checking out the
   branch is fine, because git handles the case-only rename on checkout. To redo such a move by hand there,
   use two steps in one commit (`git mv Source tmp-source && git mv tmp-source source`).
2. `chore: point path references at source/`: every directory reference outside `kanban/` (found with
   `git grep 'Source[/\\]'` and `'"Source"'`): `.editorconfig`, `.gitignore`, root `Directory.Build.props` /
   `Directory.Packages.props` comments, `msbuild/repository.props` (`SolutionFile`, `SourceDirectory`),
   `installer/run.sh`, `.github/workflows/workflow.yml` (test + pack paths), the dev CLI (`build`/`test`
   solution path and descriptions, `dev.cs`, `Directory.Build.targets` reason), `README.md`, `AGENTS.md`
   (including "solution directory (`source`)" and the fork-workflow paragraph), two comments in
   `GetDiagnosticsTests.cs`, and `releases.md` (Fixes #16). `skills/roslynk/SKILL.md` and the Aspire AppHost
   contain no directory path (the AppHost uses `Projects.Morris_Roslynk_Cli`). Test fixture lookup walks up to
   `TestFixtures`, so it needed no change. C# identifiers and prose that contain `Source` are unchanged.
3. `chore(audit): apply ganda repo audit fixes (package icon and URLs)`. Once `source/` existed, two new
   checks applied: `nuget-package-icon` and `nuget-package-urls`. `ganda repo audit --fix` added
   `PackageIcon`, `PackageProjectUrl`, `RepositoryUrl` and a `$(RepositoryRoot)assets/logo.png` pack item to
   `source/Directory.Build.props`, and generated `assets/logo.png`. It also wrote `.vscode/` files, which are
   gitignored. I re-formatted the fixer's one-line XML to the file's style: tabs, CRLF, no BOM.
   **Build fix:** `$(RepositoryRoot)` was undefined under `source/`, so `dotnet pack` of the Cli failed with
   "Could not find a part of the path .../Morris.Roslynk.Cli/assets". `source/Directory.Build.props` now imports
   `../msbuild/repository.props`, the TimeWarp convention. The Cli's own `PackageProjectUrl` and
   `RepositoryUrl` still override the shared values. The pack now contains `logo.png` and `README.md`.

Audit waivers: I removed `slnx`, `source-directory-build-props` and `cpm-consistency` one at a time and ran the
audit. All three still fail, so they stay:
- `slnx`: no root `.slnx`.
- `source-directory-build-props`: the only remaining failure is that `source/Directory.Build.props` has no
  `<Version>`, which is 008's job.
- `cpm-consistency`: 30 PackageVersions are missing from the root file.

I re-reasoned every waiver so none cites the capital `Source/` layout. `kebab-path-names`,
`directory-structure.exclude` and `dev-cli-capabilities` are kept as specified. `ganda repo audit` reports "passes
all audit checks".

Verification:
- `dotnet build source/Morris.Roslynk.slnx -c Release` (also `--no-incremental`): 0 warnings, 0 errors.
- `dotnet test source/Morris.Roslynk.slnx -c Release`: CLI 336/336 passed. Engine 981 passed, 18 failed, and all
  18 are the known Razor tests (`RazorRenameTests`, `RazorSourceToolTests`, `RazorSymbolMappingTests`,
  `ChangeSignatureTests` Razor cases, `GetExpressionInfo` Razor, `RazorGenerationTests`). This matches the 009
  baseline.
- `dotnet run --file tools/dev-cli/dev.cs -- self-install`, then `bin/dev build` succeeded with 0 warnings.
  `bin/dev test` runs the suite and gives the same 18 Razor failures, so it exits 1 just as it does on master.
- `git log --follow source/App/Morris.Roslynk/Infrastructure/Writing/AtomicFileWriter.cs` lists 7 commits,
  pre-move history included.

**For 008:** the layout blocker is gone. DevCli's `check-version`/`release` now find `source/` and the packable
Cli project. What 008 still needs:
- A `<Version>` in `source/Directory.Build.props`. That lets 008 drop the `source-directory-build-props` waiver,
  and it replaces the Cli's `0.0.1-local` fallback.
- The `Compile Remove` lines in `tools/dev-cli/Directory.Build.targets` taken out.
- The DevCli feed fix (nuru 525).

`source/Directory.Build.props` now imports `msbuild/repository.props` and carries the package icon/URL metadata.
`$(RepositoryRoot)` is therefore available to anything 008 adds there.

**Local tooling:** an MCP config, a script or a Roslynk `open_solution` call that hard-codes
`.../Roslynk/Source/Morris.Roslynk.slnx` or `Source/App/Morris.Roslynk.Cli` must change to `source/`. That
includes `claude mcp add ... dotnet run --project .../Source/App/Morris.Roslynk.Cli`. Linux is case-sensitive, so
the old path is simply not found there. A running roslynk server keyed on the old solution path should be
reopened with the new one.

### How to validate

Smoke:
```bash
r=$(git log --format=%h --grep='rename only' -1); git diff --summary $r~1 $r | grep -vc '(100%)'   # expect 0
git grep -n 'Source[/\\]' -- ':!kanban'                  # expect no output
dotnet build source/Morris.Roslynk.slnx -c Release
dotnet test source/Morris.Roslynk.slnx -c Release
ganda repo audit
git log --follow --oneline source/App/Morris.Roslynk/Infrastructure/Writing/AtomicFileWriter.cs
```

Expect:
- The rename commit has only 100% renames: the grep count is 0 and `--stat` shows 453 files, 0+/0-.
- No `Source/` path references remain outside `kanban/`.
- The build has 0 warnings and 0 errors.
- Tests: CLI 0 failures; engine exactly the 18 known Razor failures.
- The audit passes all checks.
- `--follow` shows commits from before the move (for example `0986755 Allow patching to non-CS files`).

## Notes

- Comes after 009 (merged in PR #15, 11c19b0). Comes before 008 (release flow), which then needs only the
  DevCli feed fix (nuru 525).
