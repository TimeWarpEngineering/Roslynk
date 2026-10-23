# Round 1 — general
**Date:** 2026-10-23
**Scope reviewed:** branch `task/007-publish-the-fork-as-timewarproslynk-to-private-git` vs `master` (`c28b35d`). Product files: workflow, CLI csproj, README, AGENTS.md, releases.md. Checked against the original task and `## Goal (implementer restatement)`.

## Summary

The fork's tool package is now `TimeWarp.Roslynk` (command still `roslynk`), with repository URLs on this repo and a description that credits Peter Morris and says it is not the official package. The release workflow drops Trusted Publishing and the nuget.org push, and on a numeric tag it tests, packs at the tag version, and pushes to the TimeWarpEngineering GitHub Packages feed with `GITHUB_TOKEN`. A `feature/**` pull request still only tests. README install and MCP examples use the new id with `--prerelease`. Risk is low: metadata, docs, and one workflow. One prose example omits `--prerelease`; the install section already states that flag is required.

## Goal vs original task

The restatement matches the original task. Both require package id `TimeWarp.Roslynk`, command `roslynk`, GitHub Packages only, no nuget.org path, tag gating, docs, `releases.md` `(Fixes #11)`, and no tag push from this task. No misread.

## Issues

### Issue 1 — Severity: nit
- File: README.md:168
- Description: The per-build socket paragraph still says `dnx TimeWarp.Roslynk -- stdio` with no `--prerelease`. The install section (README.md:47-48) and the implementer's local check both say a prerelease-only package fails `dnx` without that flag. The MCP config examples (Claude, agy, mcp.json, dsh) already include `--prerelease`. AGENTS.md:238 has the same shorthand; that sentence describes two builds side by side, not an install recipe.
- Suggestion: Add `--prerelease` to the README sentence so a reader who copies it does not hit "package not found".
- Status: wontfix
- Disposition notes: The install section already says `--prerelease` is required for `dnx`. This sentence explains two builds sharing a machine; it is not an install recipe. Accepted by the review oracle (2026-10-23).
