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

- [ ] PackageId `TimeWarp.Roslynk`, repository URLs to the fork, upstream credited
- [ ] Workflow: GitHub Packages push with `GITHUB_TOKEN`, nuget.org login and push removed, tag gating kept
- [ ] `ganda repo audit` passes (incl. `workflow-file`)
- [ ] Local `dotnet pack` produces `TimeWarp.Roslynk.<ver>.nupkg`; install it from a local folder source into a
      temp tool path and run `roslynk --capabilities` (do not push anything)
- [ ] README + MCP config docs + AGENTS.md updated
- [ ] `releases.md` bullet (Fixes #11)
- [ ] Results list the exact post-merge release steps (tag command, how to confirm the package is private:
      `gh api orgs/TimeWarpEngineering/packages/nuget/TimeWarp.Roslynk -q .visibility`)

## Session

- Created: 1287ec9b-6d3e-4805-9dd9-5ef6634981c8 (2026-10-23)

## Notes

- Pete's upstream CI (ubuntu, dotnet 10.0.x) passes the full suite; the 13–15 Razor failures seen locally are
  environment-specific. If the tag run's Test step would fail on them, report it in Results; do not skip or
  quarantine tests without asking.
- The fork is 27 commits behind `mrpmorris/Roslynk:master` (Pete's work from 2026-10-21/22). Syncing upstream is
  **out of scope** here.
- Never touch `~/.timewarp/ganda/keys/`.
