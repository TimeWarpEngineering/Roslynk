# Releasing TimeWarp.Roslynk

The fork publishes one package, the `TimeWarp.Roslynk` .NET tool (command `roslynk`). It goes only to the
private org GitHub Packages feed:

`https://nuget.pkg.github.com/TimeWarpEngineering/index.json`

Nothing goes to nuget.org from any trigger. `Roslynk` on nuget.org is Peter Morris's upstream package, and the fork
must not publish it (task 007, fork issue #11).

## Overview

- **The version** is typed once, as `<Version>` in `source/Directory.Build.props`. `source/` projects default to
  `IsPackable=false`, and only `Morris.Roslynk.Cli` opts in. The derived package set is therefore exactly
  `TimeWarp.Roslynk`.
- **Publish feed:** `.timewarp/dev.jsonc` declares `checkVersionConfig.feeds = "github-packages"`. That tells
  `dev check-version` and `dev release` to read only the GitHub Packages feed when they ask whether a version is
  already published. Reading the feed needs a token with `read:packages`: `GITHUB_TOKEN` or `GH_TOKEN`, or
  `gh auth refresh -h github.com -s read:packages`.
- **CI** runs on every push to master and every PR into master, whatever its head branch, and on `feature/**`
  PRs. It runs `dev workflow --mode merge` (build → test → pack) on ubuntu-latest and uploads the tested
  `.nupkg` as `Packages-{run_number}`.
- **Release** runs on `release: published`. `dev workflow --mode release` promotes that artifact, with no
  rebuild, in these steps:
  1. Tag gate: the tag must equal `v{Version}`, HEAD must be at the tag, and HEAD must be on master.
  2. check-version.
  3. Locate the successful CI run of the tagged commit.
  4. Download its `Packages-*` artifact.
  5. Verify the package set.
  6. `dotnet nuget push --skip-duplicate` to GitHub Packages with the job's `GITHUB_TOKEN`
     (`packages: write`).

  A hard guard in `workflow-command.cs` refuses any nuget.org push target.

## Normal release flow

1. Set `<Version>` in `source/Directory.Build.props` in a task PR, and merge it once CI is green. Merge CI on
   master packs and uploads `Packages-*` for the merge commit.
2. From a clean `master` in sync with `origin/master`, run `dev check-version`, then `dev release --dry-run`,
   then `dev release`. `dev release` runs its guards, creates and pushes the annotated tag `v{Version}`, and
   creates the GitHub Release.
3. The `release: published` event runs the release job, which publishes the artifact to GitHub Packages.
4. Check it: `gh run list --workflow workflow.yml --event release`, then look at the package under the org's
   Packages tab.

Never hand-type a tag. `dev release` is the only way to cut one.

## Break-glass, resume and probe

Run these with `workflow_dispatch` from the Actions UI or `gh workflow run workflow.yml`:

- **Resume a failed publish:** use `mode=release` and `confirm=release`, run against the tag
  (`--ref v{Version}`), so the tag-pin gate passes. Use it when the GitHub Release and tag exist but the package
  never landed. Do not cut a new tag for the same version. Re-running the failed release run also works.
- **Probe:** `mode=probe` runs only `dev check-version` with the job token. It proves the workflow can read the
  private feed. It builds and publishes nothing. Once the current version is published, check-version refuses
  ("already published"), which still shows that the feed was read.

## Consuming the package

Add the org feed with credentials that have `read:packages`, then install the tool:

```bash
dotnet nuget add source https://nuget.pkg.github.com/TimeWarpEngineering/index.json \
  --name github-timewarp --username <github-user> --password "$(gh auth token)" --store-password-in-clear-text
dotnet tool install --global TimeWarp.Roslynk --prerelease
```

Map `TimeWarp.Roslynk` to that source only (package source mapping). Never list it under nuget.org.
