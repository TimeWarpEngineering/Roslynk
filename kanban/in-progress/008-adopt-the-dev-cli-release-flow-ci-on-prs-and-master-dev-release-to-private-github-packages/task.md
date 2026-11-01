# Adopt the dev-cli release flow: CI on PRs and master, dev release to private GitHub Packages

## Description

Fork issue #13. Respecced 2026-11-01. Steven: "add updating Nugets to 008 and fix all audit in 008 and then
launch 008". Earlier (2026-10-23): "reenable dev-cli in .editorconfig and thus stick to our own well
established workflow ... roll in any audit fixes."

The first walk (2026-10-23) stopped as **Blocked**, because DevCli hard-coded lowercase `source/` and
nuget.org. Both blockers are gone:
- Task 010 moved the tree to `source/` (PR #17).
- timewarp-nuru 536 shipped in TimeWarp.Nuru.DevCli 4.0.0-beta.10. check-version and release now treat a
  version as published if **nuget.org or the org GitHub Packages feed** lists it, with no configuration.

The Blocked note and old Results below this spec's history are obsolete. Replace them.

Today the fork releases the upstream way: a hand-typed numeric tag runs one workflow, which tests, packs and
pushes `TimeWarp.Roslynk` to GitHub Packages (task 007). No CI runs on fork PRs: `.github/workflows/workflow.yml`
triggers `pull_request` only for `feature/**`, and fork PRs come from `task/*`. Since ganda 1.0.0-beta.38,
`ganda pr merge` therefore refuses every Roslynk merge ("No CI checks ran"). This task fixes that too.

Move the fork onto the standard TimeWarp flow (`tw-release` skill):
- The version is typed once, in `source/Directory.Build.props`.
- CI builds, tests, packs and uploads the `Packages-*` artifact on every PR to master and every push to master.
- `dev release`, run from a clean synced master, tags `v{Version}` and creates the GitHub Release.
- The release pipeline **promotes the CI-built artifact** (byte-identical, never rebuilt) to the private feed
  `https://nuget.pkg.github.com/TimeWarpEngineering/index.json`.

**Reference implementations:**
- `timewarp-taratibu` releases this exact way to GitHub Packages. Look at its `.github/workflows/`,
  `tools/dev-cli`, `.timewarp/dev.jsonc` and `documentation/developer/guides/releasing.md`. Note that it forks
  DevCli's `workflow` command locally to hard-code the GitHub Packages push target. Prefer DevCli's own
  command if DevCli beta.10+ supports a GitHub Packages push; otherwise copy taratibu's shape and say so.
- `timewarp-nuru` is the canonical DevCli source.

This task does **not** release. `dev release` is Steven's gate after merge. The first version is
`1.0.0-beta.1`.

## Requirements

### Packages

- Run `ganda nuget outdated` and update **both** central files: the root `Directory.Packages.props` (tools,
  DevCli) and `source/Directory.Packages.props` (product). The command reads only the root file, so update
  `source/` by hand to the latest version on each package's line, as task 011 did.
- Keep the whole TimeWarp.Nuru family (Nuru, DevCli, Daemon, McpHost) on one version, at least 4.0.0-beta.11.
  DevCli must be beta.10 or later, which includes nuru 536.
- TimeWarp packages are also published to `https://nuget.pkg.github.com/TimeWarpEngineering/index.json`; check
  there as well as nuget.org.
- Keep existing prerelease lines. Do not move stable packages onto prereleases.
- A major-version bump of a stable package is not an exception. Take it and adapt the code.
- Keep the MSBuild compile-only packages (`Microsoft.Build.Framework`, `Microsoft.NET.StringTools`) on the
  line that matches the .NET 10 SDK's MSBuild (see 011's Results).
- Fix whatever the bumps break. Read the Nuru beta.10/beta.11 release notes
  (`gh release view v4.0.0-beta.N --repo TimeWarpEngineering/timewarp-nuru`).

### Audit: fix all of it

- Apply every `ganda repo audit --fix` change and commit it, including `required-gitignore-entries` and `nuru`,
  which fail today.
- Go through each waiver in `[ganda.audit]` and **remove it if this task can make the check pass**:
  - `dev-cli-capabilities`: must go. Restore DevCli's commands; see Release flow below.
  - `source-directory-build-props`: must go. Add `<Version>` in `source/Directory.Build.props`.
  - `cpm-consistency`: make the root and `source/` central files consistent the way the check expects, if
    that does not mean restructuring upstream projects.
  - `slnx`: satisfy it if a root `.slnx` (or the check's other accepted shape) can sit beside
    `source/Morris.Roslynk.slnx` without duplicating or moving Pete's solution. Otherwise keep it with a
    concrete reason.
  - **Keep** `kebab-path-names`. Steven decides the kebab-case renames later.
  - **Keep** `directory-structure.exclude` unless it is trivially satisfiable.
  - Every waiver that stays gets a reason that is true today (no `Source/` references) and the task id.
- `ganda repo audit` must pass.

### Release flow

- Add `<Version>1.0.0-beta.1</Version>` to `source/Directory.Build.props`. The Cli package uses it. Remove the
  Cli's `0.0.1-local` fallback, or make it fall back to the props.
- Restore DevCli `check-version` and `release`, plus anything else `dev-cli-capabilities` requires, in
  `tools/dev-cli`. Remove the `Compile Remove` strip in `tools/dev-cli/Directory.Build.targets` and the
  exclusion in `dev.cs`. `bin/dev --capabilities` lists them.
- Workflows (`.github/workflows/`), following the reference:
  - **CI** on `pull_request` to master (**every head branch**, including `task/*`) and on `push` to master:
    restore, build and test `source/Morris.Roslynk.slnx` in Release on ubuntu-latest, then pack and upload the
    `Packages-*` artifact. The check must register on fork PRs so that `ganda pr merge` sees it. Path filters
    must not skip a PR that changes source, tools or workflows.
  - **Release** on the GitHub Release published event: tag-gate, check-version, locate the CI run at the tag
    sha, download, verify, then push to GitHub Packages with `GITHUB_TOKEN` (`packages: write`). Add
    break-glass and probe modes as the reference has them.
  - Nothing goes to nuget.org from any trigger: no NuGet login and no `mrpmorris`. Keep a hard guard against
    nuget.org, as taratibu does.
  - Remove the old numeric-tag trigger.
- Run `dev check-version` and `dev release --dry-run` locally from the worktree, as a read-only check. Guards
  that refuse because of branch or sync state are expected; report what they say. Do **not** run `dev release`
  for real, and do not tag.

### First green CI

- The walk's host opens the PR, and CI should run on it.
- Check `gh pr checks` / `gh run list` and report the result in Results.
- Locally, 18 Razor tests fail (the known baseline). Pete's upstream CI on ubuntu passes the same suite, so
  find out whether they pass in CI.
- If they fail in CI, **report it in Results and stop**. Do not skip, quarantine or delete tests, and do not
  make CI non-blocking. Steven decides.

### Docs

- AGENTS.md: "Hosting and release touchpoints" (the release workflow paragraph is now wrong) and "TimeWarp
  fork workflow" (dev commands, release flow).
- README: install section only if wording changes.
- `documentation/developer/guides/releasing.md` if the audit's directory rules allow it; otherwise AGENTS.md.
- `releases.md`: under `# Unreleased`, add "Release flow: CI on every PR and master push; `dev release`
  promotes the CI artifact to GitHub Packages (Fixes #13)", plus a package-update bullet.

### Verify

- `dotnet build source/Morris.Roslynk.slnx -c Release`: 0 warnings, 0 errors.
- `dotnet test source/Morris.Roslynk.slnx -c Release`: CLI all passing; engine only the 18 known Razor
  failures.
- `dotnet run --file tools/dev-cli/dev.cs -- self-install`, then `bin/dev build` and `bin/dev test`.
- `ganda repo audit` is clean.

## Goal (implementer restatement)

Put the fork on the standard TimeWarp release flow, and bring its packages and audit up to date along the way.

- **Packages:** bump the root and `source/` central files to the latest versions on their current lines. The
  whole Nuru family (Nuru, DevCli, Daemon, McpHost) goes to 4.0.0-beta.11. MSBuild compile-only packages stay
  on the .NET 10 SDK line. Fix whatever the bumps break.
- **Version:** type it once, as `<Version>1.0.0-beta.1</Version>` in `source/Directory.Build.props`. The Cli
  pack uses it, with no `0.0.1-local` fallback.
- **dev CLI:** restore DevCli `check-version` and `release`. Add the commands the `dev-cli-capabilities` check
  requires: `workflow` (a local, taratibu-shaped CI/CD pipeline that pushes only to GitHub Packages) and
  `verify-samples`. Declare GitHub Packages as the publish feed in `.timewarp/dev.jsonc`.
- **Workflows:** CI runs on every PR to master (any head branch) and every push to master: build, test, pack,
  and upload `Packages-*`. A published GitHub Release promotes that artifact byte-for-byte to the org's GitHub
  Packages feed. Break-glass and probe modes exist. A hard guard stops any push to nuget.org. The numeric-tag
  trigger is gone.
- **Audit:** apply and commit `--fix`. Remove every waiver the task can make pass. The waivers that stay get a
  reason that is true today, with the task id. `ganda repo audit` must be clean.
- **Docs:** update AGENTS.md (hosting/release, fork workflow), a releasing guide, and two bullets in
  `releases.md` under Unreleased (Fixes #13).

**Acceptance:**
- The Release build has 0 warnings. The tests pass, apart from the 18 known Razor failures.
- `bin/dev --capabilities` lists build, check-version, clean, release, self-install, test, verify-samples and
  workflow.
- Audit is clean.
- `dev check-version` and `dev release --dry-run` output is reported.
- The first CI run on the PR is reported.
- Results list the exact release steps for after the merge.

## Checklist

- [ ] Both central package files updated; Nuru family on one version (≥ beta.11, DevCli ≥ beta.10)
- [ ] `ganda repo audit --fix` committed; waivers removed where the check now passes, the rest re-reasoned;
      audit clean
- [ ] `<Version>1.0.0-beta.1</Version>` in `source/Directory.Build.props`, used by the pack
- [ ] DevCli `check-version`/`release` restored; `dev-cli-capabilities` waiver gone
- [ ] CI workflow (every PR to master plus master push; build/test/pack/upload artifact)
- [ ] Release workflow promotes the CI artifact to GitHub Packages; nothing to nuget.org
- [ ] `dev check-version` and `dev release --dry-run` output reported
- [ ] First CI run on the PR: result reported (green, or the exact failing tests)
- [ ] AGENTS.md / releasing guide / releases.md (Fixes #13)
- [ ] Results list the exact release steps after merge

## Session

- Created: 1287ec9b-6d3e-4805-9dd9-5ef6634981c8 (2026-10-23)
- Blocked walk: 2026-10-23 (DevCli `source/` and nuget.org hard-codings), since resolved by Roslynk 010 and
  timewarp-nuru 536
- Respecced: 1287ec9b-6d3e-4805-9dd9-5ef6634981c8 (2026-11-01). Package updates and all audit fixes were added.

## Notes

- `ganda pr merge` cannot merge Roslynk PRs until CI registers: `--allow-no-ci` is lost when the merge is
  handed to the walk. This task's own PR still has no CI from master, so it may need the same manual merge as
  011.
- Pete's 7 new upstream commits (456684a … 7de4cfa) are a separate task. They are out of scope here.
- Never touch `~/.timewarp/ganda/keys/` except through `ganda repo attest`.
