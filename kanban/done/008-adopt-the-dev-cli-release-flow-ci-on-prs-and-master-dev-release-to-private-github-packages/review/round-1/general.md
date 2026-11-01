# Round 1 — general
**Date:** 2026-11-01
**Scope reviewed:** branch `task/008-adopt-the-dev-cli-release-flow-ci-on-prs-and-maste` vs `master`. Release workflow, `tools/dev-cli` pipeline, version and pack settings, both central package files, `[ganda.audit]` waivers, and the release docs. Compared with TimeWarp.Nuru.DevCli 4.0.0-beta.11 content, timewarp-taratibu's workflow command, and the task spec.

## Summary

The fork now types `1.0.0-beta.1` once in `source/Directory.Build.props`, packs only `TimeWarp.Roslynk`, and runs CI through `dev workflow --mode merge` on every push to master and every pull request into master or `feature/**`. A published GitHub Release promotes that `Packages-*` artifact to the org GitHub Packages feed. The numeric-tag trigger is gone, and the push path refuses `nuget.org`. DevCli 4.0.0-beta.11 ships `check-version` and `release` and does not ship a `workflow` command, so the local taratibu-shaped command is the shape the task allows. The Goal restatement matches the original task. No misread.

Re-checked on this tree: `ganda repo audit` passed (32 passed, 0 failed, 2 skipped) and `bin/dev --capabilities` lists build, check-version, clean, pack, release, self-install, test, verify-samples, and workflow. NuGet.org lists Nuru, DevCli, Daemon, and McpHost at 4.0.0-beta.11 as the newest prerelease. TimeWarp.Contracts 2.0.0-beta.31 depends on TimeWarp.Mediator 15.0.0-beta.4, while Nuru 4.0.0-beta.11 depends on TimeWarp.Mediator.Contracts 14.0.0, so the pin at beta.30 matches the Results note. The full Release build and test suite were not re-run in this review.

## Issues

<!-- None. -->
