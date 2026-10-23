# Port upstream mrpmorris/Roslynk changes through dae11b1 onto the Nuru fork

## Description

Fork issue #14. Steven (2026-10-23): "Yes do 1 first. then we restructure". Pete's commits are wanted, but they
will conflict.

The fork split from `mrpmorris/Roslynk` at merge base `788735a`. Since then upstream has added 28 commits
(27 PRs plus one direct commit), up to **`dae11b148c3b79cd1c9b2cfb3067247df3a1e386`**. That commit is the pin; do
not take anything newer. The fork has replaced the upstream host with one TimeWarp.Nuru app (tasks 003–007).
`Source/App/Morris.Roslynk.Mcp` and its stdio bridge, HTTP daemon and appsettings are gone. The CLI, the
served daemon and McpHost now live in `Source/App/Morris.Roslynk.Cli`. The engine is still Pete's engine, with
operation contracts added (`[Operation]` request/response/renderer beside each tool).

This task comes **before** the `source/` restructure. Our paths still match upstream now, so git can merge
them. Doing the port by a **real merge** moves the merge base forward, so later upstream changes merge from
`dae11b1` and not from `788735a`.

## Requirements

- **Merge, do not cherry-pick or squash.** Add the remote in the worktree:
  `git remote add upstream https://github.com/mrpmorris/Roslynk.git` (fetch only; never push to it). Then run
  `git merge dae11b1` to make one merge commit, resolve the conflicts, and put follow-up fixes in separate
  commits after it. The merge base afterwards must be `dae11b1` (check with `git merge-base HEAD dae11b1`).
  Never push anything to `mrpmorris/Roslynk`, and never open PRs or issues there.
- **Engine changes port as-is**, then reach the fork's surfaces:
  - When a tool gains, loses or renames a parameter, change its `[Operation]` request property too. The
    property must keep the same name, `[Description]`, `[DefaultValue]` and initializer. The method's
    `[Description]` must match the operation description word for word. That covers #121 standardised names,
    #152 `query`→`symbolNameFilter`, #114 the `kinds` filter, #148 `solutionId` on `get_solution_status`, #153
    `countOnly`, #137 `includeHidden`, and anything else in the range.
    `OperationContractTests`, `CapabilitiesContractTests`, `McpSurfaceTests` and `HandlerTests.SampleOf` must
    agree.
  - A new error code, such as #122 `UnknownArgument`, gets an exit code in `Output/ExitCodes`, and the
    AGENTS.md and README exit-code tables are updated in the same change.
  - Changes to server instructions (#120, d7c570d "call open_solution in parallel") go into
    `McpServerRegistration.Instructions`.
  - **#119 (wait server-side for the solution load instead of returning `Indexing`):** port it into the engine
    path the served app's handlers use. Then check that a CLI call against a cold solution waits, and that
    cancellation still works (task 006 queued-cancel tests).
- **Upstream host changes** (the deleted `Morris.Roslynk.Mcp`). Adapt each one to the Nuru app, or drop it when
  the fork already covers it. Do not restore the project. Expected outcomes, which you may change with reasons:
  - #103 install-dir content root: adapt if the served app reads `appsettings.json`, otherwise drop.
  - #113 daemon version match: drop, because per-build sockets (nuru 508-017) already cover it.
  - #142 typed settings object: adapt into `RoslynkEngine`'s settings read.
  - #143 `appsettings.json.example`: adapt if the package ships settings.
  - #144 `/restart` endpoint: drop, because there is no HTTP daemon. If restart matters, note a follow-up.
  - #105 watcher errors and #108 binding errors: engine parts port; MCP binding errors go through Nuru's
    `ProblemRenderer` path.
- **Behavior Nuru must provide.** An example is #114, parsing JSON-array strings for array arguments, which
  happens in McpHost binding. Port what the engine can do. Do **not** patch around Nuru inside Roslynk. List
  each gap in Results with the exact timewarp-nuru change it needs. Steven launches nuru work himself.
- **Keep the fork's decisions.** Keep the package id `TimeWarp.Roslynk` and the command `roslynk`. Keep
  publishing to GitHub Packages only, with no nuget.org and no `mrpmorris` push in any workflow. Keep Nuru-first
  hosting. Keep the `[ganda.audit]` waivers. Keep the fork's README install section. When upstream workflow or
  README changes conflict with these, the fork wins. Take upstream's README tool documentation where it is
  still true.
- **releases.md:** keep upstream's release-history entries for the merged range, with their upstream issue
  numbers written as `mrpmorris/Roslynk#NNN`. Add one fork bullet under `# Unreleased`: "Ported upstream
  mrpmorris/Roslynk through dae11b1 (Fixes #14)".
- **Tests:** run `dotnet test Source/Morris.Roslynk.slnx --configuration Release`. List failures against the
  pre-merge baseline from origin/master: about 13–15 Razor tests already fail locally. A new failure must be
  fixed or explained. Never skip, quarantine or delete a test. `ganda repo audit` must pass, and every `--fix`
  it makes is committed.
- **Docs:** update AGENTS.md wherever the engine contract it describes changed (for example #119 Indexing,
  #153 countOnly paging, #107 solutionId slashes). Update `skills/roslynk/SKILL.md` if routing changed.

## Checklist

- [ ] `upstream` remote added in the worktree; `git merge dae11b1` produces one merge commit; merge base is
      `dae11b1`
- [ ] Conflicts resolved: engine as upstream, hosting adapted to Nuru
- [ ] Contracts, CLI options, MCP schema and tests follow every parameter/error change
- [ ] `ExitCodes` plus the AGENTS.md/README tables for new error codes
- [ ] Server instructions updated
- [ ] Full Release test run compared with the pre-merge baseline
- [ ] `ganda repo audit` clean, with fixes committed
- [ ] AGENTS.md / SKILL.md / README / releases.md (Fixes #14)
- [ ] Results: a table of all 28 upstream commits (sha, PR, title), each marked **ported**, **adapted**
      (where it went) or **dropped** (why), plus any nuru follow-ups

## Session

- Created: 1287ec9b-6d3e-4805-9dd9-5ef6634981c8 (2026-10-23)

## Notes

- Upstream range, oldest first: dbe9aa2 #104, cb64432 #123, 0973ab3 #124, 4ec00df #125, 2030305 #126,
  f598cb1 #127, 6f11180 #128, d7c570d (direct), 55237c2 #129, 74b53f4 #130, 4cae895 #132, 5c4cf06 #131,
  4f3d945 #133, 09d0f78 #134, 7be07af #135, 0058664 #106, cca0002 #139, 4651b1d #140, d294fc2 #141,
  0ea95a9 #146, ed1623e #147, 1e2e338 #145, fabbb7d #155, dd2d7ab #156, 0e43e43 #157, 89e43b6 #158,
  0ab3b07 #159, dae11b1 #161.
- A read-only clone of upstream is at `/home/steve/worktrees/github.com/mrpmorris/Roslynk/master`. It is
  useful for `git show`. Do not work, commit or branch in it.
- Next steps after this task: the `source/` restructure task, then 008 (release flow), then `dev release`
  1.0.0-beta.1.
