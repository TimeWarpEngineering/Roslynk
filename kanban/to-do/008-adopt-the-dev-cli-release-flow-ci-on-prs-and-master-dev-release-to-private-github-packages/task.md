# Adopt the dev-cli release flow: CI on PRs and master, dev release to private GitHub Packages

## Goal (implementer restatement)

Release the fork through the standard TimeWarp DevCli flow instead of hand-typed numeric tags. Type the
version once in props. CI builds, tests and packs on every PR and on every master push. `dev release` tags
`v{Version}`, and the release event promotes the CI-built `.nupkg` to private GitHub Packages without
rebuilding it. Turn the `dev-cli-capabilities` audit check back on and leave Pete's `Source/` where it is.

Acceptance: `bin/dev --capabilities` lists `check-version` and `release`. DevCli reads
`<Version>1.0.0-beta.1</Version>` and the CLI pack uses it. check-version and release look up
GitHub Packages, not nuget.org. The CI and release workflows match taratibu's shape, and nothing goes to
nuget.org. `ganda repo audit` is clean with the waiver removed. The first CI run on the PR is reported.
AGENTS.md and releases.md are updated (Fixes #13). If DevCli hard-codes lowercase `source/` and has no
setting for it, stop and report Blocked with the exact nuru change needed.

## Description

Fork issue #13. Steven (2026-10-23): "reenable dev-cli in .editorconfig and thus stick to our own well
established workflow ... roll in any audit fixes."

Today the fork releases the upstream way: pushing a hand-typed numeric tag runs one workflow that tests, packs
and pushes `TimeWarp.Roslynk` to GitHub Packages (task 007). No CI has ever run on the fork: every merge said
"zero CI checks". The `dev-cli-capabilities` audit check is waived in the root `.editorconfig`, and
`tools/dev-cli/Directory.Build.targets` / `dev.cs` strip DevCli's `check-version` and `release` commands,
because they read `<Version>` from lowercase `source/Directory.Build.props` and upstream uses `Source/`.

Move the fork onto the standard TimeWarp flow (the `tw-release` skill): the version is typed once in props, CI
builds and uploads the package on every PR and master push, and `dev release` from a clean synced master tags
`v{Version}`, creates the GitHub Release, and the release pipeline **promotes the CI-built artifact**
(byte-identical, never rebuilt) to the private GitHub Packages feed.

**Reference implementation:** `timewarp-taratibu` releases this exact way to
`https://nuget.pkg.github.com/TimeWarpEngineering/index.json` (its `documentation/developer/guides/releasing.md`,
`.github/workflows/`, `.timewarp/dev.jsonc`, `tools/dev-cli`). Copy its shape; do not invent a new one.
`timewarp-nuru` is the canonical DevCli source (`documentation/developer/guides/releasing.md`).

This task does **not** release. Tagging/`dev release` is Steven's gate after merge. First version: 1.0.0-beta.1.

## Requirements

- Remove `dev-cli-capabilities.severity = off` (and its reason) from `[ganda.audit]`; `ganda repo audit` passes
  with it on. Apply **every** `ganda repo audit --fix` change and commit it (global policy: audit fixes are the
  intended baseline). Keep the other waivers unless their check now passes on its own.
- Restore DevCli's `check-version` and `release` (and whatever else `dev-cli-capabilities` requires) in
  `tools/dev-cli`, matching taratibu/nuru. Remove the strip in `Directory.Build.targets` / the exclusion in `dev.cs`.
- Version: one `<Version>1.0.0-beta.1</Version>` where DevCli reads it, and the CLI package uses it
  (`Morris.Roslynk.Cli.csproj` currently has `0.0.1-local`). **Do not rename or move Pete's `Source/`** (kebab
  and source-directory waivers exist for upstream merges). Look first for a DevCli setting
  (`.timewarp/dev.jsonc`, e.g. `checkVersionConfig`, a props path) that points it at `Source/`. If DevCli
  hard-codes `source/Directory.Build.props` and has no setting, stop and report **Blocked** with the exact
  timewarp-nuru DevCli change needed (Steven launches nuru work himself); do not add a lowercase `source/`
  directory, a symlink, or a duplicate props file as a workaround without asking.
- check-version / release must check **GitHub Packages** for "already published", not nuget.org (the
  `TimeWarp.Roslynk` id does not exist on nuget.org; Pete's `Roslynk` is unrelated). Same as taratibu.
- Workflows (`.github/workflows/`), per the reference:
  - CI on `pull_request` (to master, plus whatever `workflow-file` requires) and `push` to master: restore,
    build, test `Source/Morris.Roslynk.slnx` in Release on ubuntu-latest, pack, upload the `Packages-*`
    artifact.
  - Release on the GitHub Release published event: tag-gate, check-version, locate the CI run at the tag sha,
    download, verify, push to GitHub Packages with `GITHUB_TOKEN` (`packages: write`). Break-glass/probe modes
    as the reference has them.
  - Nothing to nuget.org from any trigger. No NuGet/login, no `mrpmorris`.
  - Remove the old hand-tag numeric-tag trigger.
- **First green CI.** Open the PR and let CI run on it (the walk's host opens the PR; if CI only runs after
  push, check `gh pr checks` / `gh run list` and report). The 13–15 Razor tests that fail locally may fail on
  ubuntu too; Pete's upstream CI on ubuntu passes the same suite, so first check whether they pass there. If they
  fail in CI: **report in Results and stop**. Do not skip, quarantine or delete tests, and do not mark CI
  non-blocking; Steven decides.
- Docs: AGENTS.md "Hosting and release touchpoints" and "TimeWarp fork workflow" (dev commands, release flow),
  README install section unchanged unless the feed/version wording changes, a fork
  `documentation/developer/guides/releasing.md` only if the audit's directory rules allow it (otherwise in
  AGENTS.md), `releases.md` `# Unreleased` bullet ending `(Fixes #13)`.

## Checklist

- [ ] `dev-cli-capabilities` waiver removed; DevCli `check-version`/`release` restored; `bin/dev --capabilities`
      lists them
- [ ] `<Version>1.0.0-beta.1</Version>` read by DevCli and used by the pack, without moving `Source/`
      (or Blocked with the exact nuru DevCli ask)
- [ ] CI workflow (PR + master push, build/test/pack/upload artifact)
- [ ] Release workflow promotes the CI artifact to GitHub Packages; nothing to nuget.org
- [ ] `ganda repo audit --fix` applied and committed; `ganda repo audit` clean
- [ ] `dev check-version` and `dev release --dry-run` run locally from the worktree (dry-run only; read-only
      guards may refuse on branch/sync, report what they say)
- [ ] First CI run on the PR: result reported (green, or the exact failing tests)
- [ ] AGENTS.md / releases.md (Fixes #13)
- [ ] Results list the exact release steps after merge

## Session

- Created: 1287ec9b-6d3e-4805-9dd9-5ef6634981c8 (2026-10-23)
- 2026-10-23 implement oracle (claude-opus-5-5): **Blocked, no product changes made.** DevCli
  (TimeWarp.Nuru.DevCli 4.0.0-beta.5, and timewarp-nuru master 8c9a0562) has no setting for the source
  directory, and on Linux `Path.Combine(repoRoot, "source")` is case-sensitive, so it never finds `Source/`:
  - `content/any/services/props-version-reader.cs:35`: `PropsVersionReader.Read` reads
    `{repoRoot}/source/Directory.Build.props`. check-version, release and `TagAssertion` (the release
    tag gate) all use it.
  - `content/any/services/packable-project-service.cs:56`: `PackableProjectService` lists `*.csproj` only
    under `{repoRoot}/source`.
  - `content/any/services/repo-config.cs`: `.timewarp/dev.jsonc` (`RepoConfig`) has only
    `checkVersionConfig.packages` and `attestation`. There is no source-dir or props-path key.
    `checkVersionConfig.packages = "TimeWarp.Roslynk"` would skip the project scan, but the version read
    would still fail.
  - `content/any/services/nuget-version-service.cs:72`: `NuGetVersionService.GetIndexUrl` hard-codes
    `https://api.nuget.org/v3-flatcontainer/`. check-version and release's publish-state guard cannot
    look up GitHub Packages. taratibu has the same gap: its "already published" check asks nuget.org,
    gets 404 and reports the version as new.

  **Nuru DevCli change that unblocks this (timewarp-nuru, `source/timewarp-nuru-devcli`):**
  1. Add `sourceDirectory` to `RepoConfig` (`.timewarp/dev.jsonc`, default `"source"`, relative to the
     repo root). Pass it through `PropsVersionReader.Read`, `TagAssertion` and
     `PackableProjectService.GetPackableProjectsAsync`, and through the check-version and release error
     text. Roslynk would then set `"sourceDirectory": "Source"`.
  2. Add a feed setting, e.g. `checkVersionConfig.feed` set to a NuGet v3 service index URL (default
     nuget.org). `NuGetVersionService` would resolve `PackageBaseAddress/3.0.0` from that index and
     authenticate GitHub Packages with a token (`GITHUB_TOKEN`/`GH_TOKEN`, or `gh auth token`). It should
     fail closed on 401/403, the same way it treats other non-404 errors.
  3. Release a DevCli beta with both settings. Roslynk 008 then bumps `TimeWarp.Nuru.DevCli` in the root
     `Directory.Packages.props` and resumes this walk (`ganda task work 008`).

  Not done, as the spec says: no lowercase `source/`, no symlink, no duplicate props file. The
  `dev-cli-capabilities` waiver stays until DevCli can read `Source/`.


## Notes

- Next after this: a separate task merges Pete's 27 upstream commits (mrpmorris/Roslynk master since 788735a),
  ported onto the Nuru app, with this CI guarding it. Out of scope here.
- Never touch `~/.timewarp/ganda/keys/` except through `ganda repo attest`.
