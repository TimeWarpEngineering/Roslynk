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

## Checklist

- [ ] Commit 1: `git mv Source source`, renames only (verified)
- [ ] Commit 2: every path reference outside kanban/ updated
- [ ] Waivers that now pass removed, the rest re-reasoned; audit `--fix` committed; audit clean
- [ ] Release build clean; tests match the 009 baseline; `bin/dev build`/`test` work
- [ ] `git log --follow` keeps history
- [ ] AGENTS.md / README / SKILL.md / releases.md (Fixes #16)
- [ ] Results: what changed for 008, and a note for local tooling (MCP configs or scripts that hard-code
      `Source/Morris.Roslynk.slnx`)

## Session

- Created: 1287ec9b-6d3e-4805-9dd9-5ef6634981c8 (2026-10-24)

## Notes

- Comes after 009 (merged in PR #15, 11c19b0). Comes before 008 (release flow), which then needs only the
  DevCli feed fix (nuru 525).
