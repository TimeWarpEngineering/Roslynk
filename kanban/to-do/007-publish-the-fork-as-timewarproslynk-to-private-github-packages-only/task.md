# Publish the fork as TimeWarp.Roslynk to private GitHub Packages only

## Description

Fork issue #11. The fork's release workflow (`.github/workflows/workflow.yml`) still publishes Pete's `Roslynk`
package id to nuget.org through Trusted Publishing as `mrpmorris`. The fork cannot and must not publish there.
Steven (2026-10-23): give the fork its own package id and release it **only** to private GitHub Packages.

Decisions (Steven, 2026-10-23):

- Package id **`TimeWarp.Roslynk`**. The tool command stays `roslynk`.
- First release **1.0.0-beta.1**. The fork has no tags yet; the tag is the version, no `v` prefix (keep the
  workflow's existing convention).
- Registry: `https://nuget.pkg.github.com/TimeWarpEngineering/index.json`, package visibility **private**.
  The org already does this for a public repo: `TimeWarp.Flexbox` is a private package linked to the public
  `timewarp-flexbox` repo.
- **Nothing goes to nuget.org**, from any trigger. Remove the NuGet/login (Trusted Publishing, `user: mrpmorris`)
  and the nuget.org push entirely; do not leave them behind a condition.

This task implements and opens the PR. **Tagging and releasing is a human gate** after merge; do not push a tag.

## Goal (implementer restatement)

Make the TimeWarp fork release its own tool package, `TimeWarp.Roslynk` (command still `roslynk`), to the org's
private GitHub Packages feed only, and never to nuget.org.

Acceptance:

- The CLI csproj packs as `TimeWarp.Roslynk`, links to `https://github.com/TimeWarpEngineering/Roslynk`, and its
  metadata (authors, description, readme) credits Peter Morris's upstream instead of posing as his package.
- `workflow.yml`: a numeric tag tests, packs at the tag version and pushes to
  `https://nuget.pkg.github.com/TimeWarpEngineering/index.json` with `GITHUB_TOKEN` (`packages: write`,
  `contents: read`, no `id-token`); no NuGet/login step and no nuget.org push in any form; a `feature/**` PR
  only tests. Header comment says so.
- README tells a fork user how to add the authenticated source and install `TimeWarp.Roslynk --prerelease`
  (with `dnx` only if verified to work), warns about the `roslynk` command clash with upstream `Roslynk`, and the
  MCP config examples use the new id.
- AGENTS.md (repository map, hosting/release touchpoints, CLI section) and `releases.md` (`(Fixes #11)`) updated.
- `ganda repo audit` passes; a local pack yields `TimeWarp.Roslynk.<ver>.nupkg` that installs from a local folder
  into a temp tool path and runs `roslynk --capabilities`. No tag, no push of packages.

## Requirements

- `Source/App/Morris.Roslynk.Cli/Morris.Roslynk.Cli.csproj`: `PackageId` `TimeWarp.Roslynk`; `RepositoryUrl`
  (and `PackageProjectUrl` if present) point at `https://github.com/TimeWarpEngineering/Roslynk`, so GitHub links
  the package to this repo. Keep `ToolCommandName` `roslynk`. Review authors/description/readme metadata so the
  package does not claim to be Pete's official package (credit upstream in the description or readme instead).
- Workflow: on a numeric tag, run the tests, pack with `-p:Version=<tag>`, and `dotnet nuget push` to the
  GitHub Packages source with `GITHUB_TOKEN` (`permissions: packages: write`, `contents: read`; drop `id-token`).
  Keep the audit's `pull_request` trigger and the tag gating from task 006, so a PR build only tests.
  `ganda repo audit` must pass, including `workflow-file`. Update the header comment.
- README install section for the fork's tool: add the GitHub Packages source with a PAT that has
  `read:packages` (`dotnet nuget add source ... --name timewarp-github --username <user> --password <PAT>
  --store-password-in-clear-text` or the user's NuGet.config), then
  `dotnet tool install -g TimeWarp.Roslynk --prerelease` (and the `dnx` form if it works with an authenticated
  source; verify, don't assume). Note that it conflicts with Pete's `Roslynk` global tool on the `roslynk`
  command; uninstall one first. Keep Pete's nuget.org instructions only where they describe upstream.
- MCP config docs (`roslynk stdio`, any `dnx Roslynk -- stdio` example) updated to the new id where they describe
  the fork's package.
- Tests or checks that pin the package id (packaging tests, AGENTS.md "Roslynk tool package" wording) updated.
- `releases.md` `# Unreleased`: one bullet ending `(Fixes #11)`.
- AGENTS.md "Hosting and release touchpoints" describes the new target.

## Checklist

- [x] PackageId `TimeWarp.Roslynk`, repository URLs to the fork, upstream credited
- [x] Workflow: GitHub Packages push with `GITHUB_TOKEN`, nuget.org login and push removed, tag gating kept
- [x] `ganda repo audit` passes (incl. `workflow-file`)
- [x] Local `dotnet pack` produces `TimeWarp.Roslynk.<ver>.nupkg`; install it from a local folder source into a
      temp tool path and run `roslynk --capabilities` (do not push anything)
- [x] README + MCP config docs + AGENTS.md updated
- [x] `releases.md` bullet (Fixes #11)
- [x] Results list the exact post-merge release steps (tag command, how to confirm the package is private:
      `gh api orgs/TimeWarpEngineering/packages/nuget/TimeWarp.Roslynk -q .visibility`)

## Session

- Created: 1287ec9b-6d3e-4805-9dd9-5ef6634981c8 (2026-10-23)
- Implemented 2026-10-23 (ganda task work, implement oracle, Claude Opus 5.5). `ganda repo audit` reported one
  advisory warning (`vscode-window-icon`: `.vscode/tasks.json` and `.vscode/settings.json` missing);
  `ganda repo audit --fix` created both. `.vscode/` is gitignored here, so the fix is local only and nothing
  was committed for it. Audit now passes all checks.
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-23T12:29:19Z
- Review oracle: Grok, session `01a168bc-b74d-7882-bab3-af456b9da72d` (2026-10-23). Effort 2, roster general. Round 1. Disposition accepted-exceptions (M1 nit wontfix).

## Notes

- Pete's upstream CI (ubuntu, dotnet 10.0.x) passes the full suite; the 13–15 Razor failures seen locally are
  environment-specific. If the tag run's Test step would fail on them, report it in Results; do not skip or
  quarantine tests without asking.
- The fork is 27 commits behind `mrpmorris/Roslynk:master` (Pete's work from 2026-10-21/22). Syncing upstream is
  **out of scope** here.
- Never touch `~/.timewarp/ganda/keys/`.

## Results

### What changed

- `Source/App/Morris.Roslynk.Cli/Morris.Roslynk.Cli.csproj`: `PackageId` is `TimeWarp.Roslynk` (`ToolCommandName`
  stays `roslynk`). `PackageProjectUrl` and `RepositoryUrl` point at `https://github.com/TimeWarpEngineering/Roslynk`,
  with `RepositoryType` `git`, so GitHub Packages links the package to this repo. `Authors`/`Company` are
  TimeWarp Engineering on the package project only. Copyright stays Peter Morris, as the MIT notice requires. The
  description says it is TimeWarp's fork of Peter Morris's Roslynk, links upstream and says it is not the official
  package. It also now says `roslynk stdio` instead of `dnx Roslynk -- stdio`.
- `.github/workflows/workflow.yml` ("Publish to GitHub Packages"): permissions are `contents: read` and
  `packages: write`; `id-token` is gone. The `NuGet/login` step (`user: mrpmorris`) and the nuget.org push are
  removed outright. On a numeric tag the workflow tests, packs with `-p:Version=$VERSION`, then runs
  `dotnet nuget push ./nupkgs/*.nupkg --api-key ${{ secrets.GITHUB_TOKEN }} --source
  https://nuget.pkg.github.com/TimeWarpEngineering/index.json --skip-duplicate`. The `pull_request` trigger on
  `feature/**` and the `startsWith(github.ref, 'refs/tags/')` gating (from task 006) are kept, so a PR build
  only tests. The header comment is rewritten.
- README: a fork banner credits upstream. The install section adds the authenticated feed
  (`dotnet nuget add source ... --name timewarp-github ... --store-password-in-clear-text`, PAT with
  `read:packages`, or user NuGet.config), then `dotnet tool install -g TimeWarp.Roslynk --prerelease`. It warns
  about the `roslynk` command clash with Pete's `Roslynk` (uninstall one first). The Claude, agy, mcp.json and
  dsh examples use `dnx TimeWarp.Roslynk --prerelease --yes -- stdio`. The release note says GitHub Packages, and
  the CLI and Aspire install lines use the new id.
- AGENTS.md: the repository map, the Forwarding bullet, the release-workflow paragraph (GitHub Packages,
  `GITHUB_TOKEN`, nothing to nuget.org, tagging is a human step) and the CLI section name `TimeWarp.Roslynk`.
- `releases.md` `## Unreleased`: one bullet ending `(Fixes #11)`.
- No test pins the package id (grep of `Source/` found none), so no test changed.

### Verification (local, nothing pushed to any feed)

- `ganda repo audit`: passes all checks, including `workflow-file`.
- `dotnet build Source/Morris.Roslynk.slnx -c Release`: 0 errors.
- `dotnet test Source/App/Morris.Roslynk.Cli.Tests -c Release`: 315 passed, 0 failed. The full engine suite was
  not re-run because this change touches only package metadata, docs and the workflow.
- `dotnet pack ... -p:Version=1.0.0-beta.1 --output /tmp/...` produced `TimeWarp.Roslynk.1.0.0-beta.1.nupkg`. Its
  nuspec has id `TimeWarp.Roslynk`, authors TimeWarp Engineering, the fork's projectUrl and repository url, and
  the upstream-crediting description.
- `dotnet tool install TimeWarp.Roslynk --tool-path <tmp> --add-source <tmp>/nupkgs --prerelease` installed it,
  and `<tmp>/roslynk --capabilities` printed JSON with name `roslynk` and version `1.0.0-beta.1+795d9a3…`.
- `dnx`, with an isolated `NUGET_PACKAGES` so nothing entered the global cache:
  - `dnx TimeWarp.Roslynk --add-source <tmp>/nupkgs --prerelease --yes -- --capabilities` worked.
  - Without `--prerelease` it failed ("not found"), so every README `dnx` example carries `--prerelease`.
  - Authenticated source: a temp `nuget.config` with only the GitHub feed plus `packageSourceCredentials` (gh token,
    `read:packages`) made `dnx TimeWarp.Ganda --prerelease --yes -- --version` resolve `1.0.0-beta.18`. That
    version exists only on GitHub Packages, so `dnx` does read an authenticated source from NuGet configuration.
    The temp config was deleted afterwards.

### Post-merge release steps (human gate; not done here)

1. From a clean, synced `master` in the origin-home checkout:
   `git tag 1.0.0-beta.1 && git push origin 1.0.0-beta.1` (no `v` prefix).
2. Watch the run: `gh run list --repo TimeWarpEngineering/Roslynk --workflow workflow.yml --limit 1`, then
   `gh run watch <id> --repo TimeWarpEngineering/Roslynk`.
3. Confirm the package exists and is private:
   `gh api orgs/TimeWarpEngineering/packages/nuget/TimeWarp.Roslynk -q .visibility` → `private`.
   A first push to GitHub Packages from a public repo can come out public. If it does, set it to private in
   the package settings (Org → Packages → TimeWarp.Roslynk → Package settings → Change visibility). Also check
   that the package is linked to the `Roslynk` repository (`-q .repository.full_name`).
4. Smoke from a machine with the feed configured:
   `dotnet tool install -g TimeWarp.Roslynk --prerelease && roslynk --capabilities | head`.

Risk to note: the tag run's Test step runs the full suite on ubuntu. Pete's upstream CI passes it there. The 13–15
Razor failures seen locally are environment-specific and were not re-checked here. If the tag run fails on them,
the push does not happen; report it rather than skip tests.

### How to validate

Smoke:

```bash
ganda repo audit
grep -n "nuget.org\|NuGet/login\|id-token\|mrpmorris" .github/workflows/workflow.yml
T=$(mktemp -d)
dotnet pack Source/App/Morris.Roslynk.Cli/Morris.Roslynk.Cli.csproj -c Release -p:Version=0.0.1-validate --output $T/nupkgs
dotnet tool install TimeWarp.Roslynk --tool-path $T/tools --add-source $T/nupkgs --prerelease
$T/tools/roslynk --capabilities | head -5
```

Expect:

- The audit passes all checks.
- The grep matches only the header comment's "nothing is published to nuget.org" sentence. No `NuGet/login`,
  `id-token`, `mrpmorris` or nuget.org push step remains.

### Review

Round 1, reviewer roster general, effort 2 (213 lines). The Goal restatement matches the original task; no misread.

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 1 |

Disposition: **accepted-exceptions**. Final open count 0. M1 (nit) is wontfix: `README.md` line 168 says `dnx TimeWarp.Roslynk -- stdio` without `--prerelease` while describing two builds side by side. The install section already requires `--prerelease` for `dnx`. Decided by the review oracle.

Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.
- `$T/nupkgs/TimeWarp.Roslynk.0.0.1-validate.nupkg` exists, the install reports
  `Tool 'timewarp.roslynk' (version '0.0.1-validate') was successfully installed`, and `--capabilities` prints
  JSON starting with `"name": "roslynk"`.
