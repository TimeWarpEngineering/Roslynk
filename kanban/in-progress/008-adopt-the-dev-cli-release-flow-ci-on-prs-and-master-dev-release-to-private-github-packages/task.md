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

- [x] Both central package files updated; Nuru family on one version (≥ beta.11, DevCli ≥ beta.10)
- [x] `ganda repo audit --fix` committed; waivers removed where the check now passes, the rest re-reasoned;
      audit clean
- [x] `<Version>1.0.0-beta.1</Version>` in `source/Directory.Build.props`, used by the pack
- [x] DevCli `check-version`/`release` restored; `dev-cli-capabilities` waiver gone
- [x] CI workflow (every PR to master plus master push; build/test/pack/upload artifact)
- [x] Release workflow promotes the CI artifact to GitHub Packages; nothing to nuget.org
- [x] `dev check-version` and `dev release --dry-run` output reported
- [ ] First CI run on the PR: result reported (green, or the exact failing tests)
- [x] AGENTS.md / releasing guide / releases.md (Fixes #13)
- [x] Results list the exact release steps after merge

## Session

- Created: 1287ec9b-6d3e-4805-9dd9-5ef6634981c8 (2026-10-23)
- Blocked walk: 2026-10-23 (DevCli `source/` and nuget.org hard-codings), since resolved by Roslynk 010 and
  timewarp-nuru 536
- Respecced: 1287ec9b-6d3e-4805-9dd9-5ef6634981c8 (2026-11-01). Package updates and all audit fixes were added.
- Implement walk: 2026-11-01 (ganda task work, implementer). Merged origin/master (land kanban 008) first.
  Audit boy-scout fixes: `required-gitignore-entries` (bin/dev* lines), `nuru` (beta.11), `vscode-window-icon`
  (`.vscode/settings.json` and `tasks.json`, force-added past upstream's `.vscode/` ignore), and
  `directory-structure` (documentation/ now holds the releasing guide).
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-11-01T08:45:13Z

## Results

### What changed

- **Packages:** the whole Nuru family moved together, 4.0.0-beta.9 → 4.0.0-beta.11, in both central files:
  Nuru and DevCli in the root file, Nuru, Daemon and McpHost in `source/`. TimeWarp.Contracts moved 2.0.0-beta.29
  → **2.0.0-beta.30**, not beta.31: beta.31 depends on TimeWarp.Mediator 15.0.0-beta.4, while Nuru beta.11
  still depends on TimeWarp.Mediator.Contracts 14.0.0. With beta.31 the Cli failed with 66 errors (CS0433
  `IMediator` in both assemblies, plus CS0311 on every handler). Take beta.31 once Nuru moves to Mediator 15.
  Every other package was already the newest on its line, according to `ganda nuget outdated` and nuget.org
  flat-container queries:
  - OpenTelemetry stays on 1.20.1. Its next version is only 1.21.0-rc, and a stable package does not move onto
    a prerelease.
  - DI.Abstractions stays on 10.0.14, the newest 10.0.x.
  - Microsoft.Build.Framework and Microsoft.NET.StringTools stay on 18.9.6, the .NET 10 SDK line (011).

  None of these packages is on the org GitHub Packages feed. The feed holds only Taratibu, Ganda, Flexbox and
  Zana.
- **Version:** `source/Directory.Build.props` now has `<Version>1.0.0-beta.1</Version>` and defaults
  `<IsPackable>false</IsPackable>`. Only `Morris.Roslynk.Cli` opts in, so the derived package set is exactly
  `TimeWarp.Roslynk`. The Cli's `0.0.1-local` fallback is gone.
- **dev CLI:**
  - The `Compile Remove` strip (`tools/dev-cli/Directory.Build.targets`) and the `dev.cs` exclusion note are
    removed. DevCli `check-version` and `release` are back.
  - New local endpoints: `pack`, `verify-samples` and `workflow`. `verify-samples` says Roslynk has none, and
    fails if a `samples/` directory ever appears.
  - `build` and `test` now report through `Environment.ExitCode` and stream output, so they compose as
    pipeline steps. `services/process-helpers.cs` was replaced by `services/solution-locator.cs`.
  - CA1812 is suppressed in the dev-cli props, because DevCli beta.10 made its content internal and the
    mediator constructs the handlers.
  - `bin/dev --capabilities` lists build, check-version, clean, pack, release, self-install, test,
    verify-samples and workflow.
- **DevCli has no workflow command,** so `workflow` is a local copy of timewarp-taratibu's. Its shape: PR/merge
  runs build → test → pack. Release runs tag gate → check-version → locate run → download → verify → push to
  `https://nuget.pkg.github.com/TimeWarpEngineering/index.json`. A hard guard throws on any argument that
  contains `nuget.org`. Two steps were dropped from taratibu's version: `clean` (CI checks out fresh) and
  `aot-host` (taratibu-specific).
- **Feeds:** `.timewarp/dev.jsonc` sets `checkVersionConfig.feeds = "github-packages"`, the DevCli beta.11 key
  (nuru 564). The gates never read nuget.org, where `Roslynk` is Pete's package.
- **`.github/workflows/workflow.yml`** (named "CI/CD"):
  - The `ci` job runs on every push to master, and on every PR into master or `feature/**` from any head
    branch. It has no path filters, so every PR registers a check. It runs `dev workflow --mode merge` on
    ubuntu-latest with .NET 10.0.x, then uploads `Packages-<run_number>`.
  - The `release` job runs on `release: published` and promotes the artifact. Its permissions are `packages:
    write` and `actions: read`.
  - `workflow_dispatch` offers `merge`, `release` (break-glass, with `confirm=release`) and `probe` (runs
    `check-version` against the private feed with the job token; no build and no push).
  - No NuGet login, nothing to nuget.org, and the numeric-tag trigger is removed.
  - A `pull_request` run uses the workflow file from the PR's merge ref, so this task's own PR runs the new CI.
- **Audit:**
  - Applied and committed `--fix`.
  - Removed waivers: `dev-cli-capabilities`, `source-directory-build-props`, and `documentation` from the
    `directory-structure` exclude.
  - Kept, with reasons that are true today and the task id:
    - `kebab-path-names`: Steven decides later.
    - `slnx`: a root solution would either duplicate Pete's projects or build nothing.
    - `cpm-consistency`: the check reads only the root file. It ignores `source/Directory.Packages.props`
      and the fixtures' deliberate CPM opt-out, so passing it would mean a drifting second copy of upstream's
      pins.
    - `directory-structure.exclude = tests`.
  - **`ganda repo audit`: "Repository passes all audit checks."**
- **Docs:** AGENTS.md ("Hosting and release touchpoints" release paragraph, "TimeWarp fork workflow"),
  `documentation/developer/guides/releasing.md` (new), the README release sentence, and two `releases.md` bullets
  under Unreleased (Fixes #13).

### Verification (local, WSL, default SDK 11.0.100-rc.3)

- `bin/dev build` (= `dotnet build source/Morris.Roslynk.slnx -c Release`): **0 Warning(s), 0 Error(s)**.
- `bin/dev test` (= `dotnet test source/Morris.Roslynk.slnx -c Release`):
  - CLI: **336/336 passed**.
  - Engine: **981 passed, 18 failed**. All 18 are the known Razor baseline: RazorRenameTests ×8,
    RazorSourceToolTests ×5, RazorSymbolMappingTests ×2, ChangeSignature Razor ×2, GetExpressionInfo Razor ×1,
    RazorGenerationTests ×1.
  - `dev test` therefore exits 1 locally.
- Re-ran the Razor, ChangeSignature and GetExpressionInfo tests pinned to **SDK 10.0.403** (the CI SDK line),
  with a temporary global.json: **14 failed, 91 passed**. So the local Razor failures are not only an SDK 11
  effect. Whether they pass on ubuntu-latest is still unknown.
- `bin/dev pack`: `Packable set (1): TimeWarp.Roslynk` → `artifacts/packages/TimeWarp.Roslynk.1.0.0-beta.1.nupkg`,
  "Packed set verified."
- `dotnet run --file tools/dev-cli/dev.cs -- self-install` succeeded.
- `bin/dev check-version` (exit 0):
  ```
  Version in source: 1.0.0-beta.1
  Latest NuGet version: (none)
  Packages checked: TimeWarp.Roslynk
  ✓ Version in source is new — safe to release.
  ```
- `bin/dev release --dry-run` (exit 1, the expected guard):
  ```
  ✓ gh authenticated
  ✓ working tree clean
  Error: release must be cut from master — current branch is task/008-adopt-the-dev-cli-release-flow-ci-on-prs-and-maste.
  ```
  Before the commits it refused with "working tree is dirty". Nothing was tagged and no release was created.

### First CI run

- **Not observable in the implement node.** No PR exists yet: the host's open-pr node creates it. The new
  workflow has no branch-push trigger, and `workflow_dispatch` cannot run before the file is on master.
- When the PR opens, `pull_request` runs the `ci` job from this branch's workflow file. Check it with
  `gh pr checks <pr> --repo TimeWarpEngineering/Roslynk` or
  `gh run list --repo TimeWarpEngineering/Roslynk --workflow workflow.yml`.
- **If the 18 Razor tests fail on ubuntu-latest, the check is red,** and pack and upload do not run. Per spec,
  that gets reported, not worked around: no skip, no quarantine, no non-blocking CI. Steven decides. A red
  check also means `ganda pr merge` will not merge this PR.

### Release steps after merge (Steven's gate)

1. Merge this PR. The push to master runs the `ci` job, which uploads `Packages-<n>` with
   `TimeWarp.Roslynk.1.0.0-beta.1.nupkg`. It must be green; a red `ci` leaves nothing to promote.
2. In the origin-home master worktree: `git pull`, then confirm it is clean and in sync with `origin/master`.
3. `dev check-version`. Expect "Version in source is new". It needs a token with `read:packages`:
   `gh auth refresh -h github.com -s read:packages` if needed.
4. `dev release --dry-run`, then `dev release`. This creates and pushes tag `v1.0.0-beta.1` and the GitHub
   Release.
5. The `release: published` event runs the `release` job, which promotes the master CI artifact to GitHub
   Packages. Watch it with `gh run list --repo TimeWarpEngineering/Roslynk --workflow workflow.yml --event release`.
6. Check the package: `gh api /orgs/TimeWarpEngineering/packages/nuget/TimeWarp.Roslynk/versions --jq '.[].name'`,
   then `dotnet tool install -g TimeWarp.Roslynk --prerelease` (feed set up as in the README).
7. If the publish fails after the tag exists, do not re-tag. Re-run the release run, or dispatch
   `gh workflow run workflow.yml --ref v1.0.0-beta.1 -f mode=release -f confirm=release`.

### How to validate

**Smoke:**
```bash
dotnet run --file tools/dev-cli/dev.cs -- self-install
bin/dev --capabilities | python3 -c 'import json,sys; print(sorted(e["pattern"] for e in json.load(sys.stdin)["endpoints"]))'
bin/dev build && bin/dev pack && bin/dev check-version
bin/dev release --dry-run
ganda repo audit
```

**Expect:**
- capabilities lists build, check-version, clean, pack, release, self-install, test, verify-samples and workflow.
- build reports 0 warnings and 0 errors.
- pack writes exactly `artifacts/packages/TimeWarp.Roslynk.1.0.0-beta.1.nupkg` and prints "Packed set verified".
- check-version prints "Version in source is new — safe to release" and exits 0. It needs a token with
  `read:packages`.
- release --dry-run refuses with "release must be cut from master" on the task branch. On a clean synced
  master it prints the tag and release it would create.
- audit prints "Repository passes all audit checks."
- On the PR, the "CI/CD / ci" check runs; then report its result.

## Notes

- `ganda pr merge` cannot merge Roslynk PRs until CI registers: `--allow-no-ci` is lost when the merge is
  handed to the walk. This task's own PR still has no CI from master, so it may need the same manual merge as
  011.
- Pete's 7 new upstream commits (456684a … 7de4cfa) are a separate task. They are out of scope here.
- Never touch `~/.timewarp/ganda/keys/` except through `ganda repo attest`.
