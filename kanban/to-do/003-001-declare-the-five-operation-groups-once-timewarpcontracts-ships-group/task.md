# Declare the five operation groups once TimeWarp.Contracts ships Group

## Description

Roslynk 003 shipped 29 `[Operation]` commands with no group, because a group on the request would have referenced TimeWarp.Nuru and the engine stays free of Nuru. timewarp-architecture 388 adds the group to the contract itself:

`[Operation("get_symbol", Description = "...", Group = "symbols")]`

`Group` is purpose metadata. It does not prefix the route or the command name. The CLI command stays the MCP tool name. Blank means ungrouped.

After the TimeWarp.Contracts package that contains `OperationAttribute.Group` is published, declare each operation's group once on its `[Operation]`. Do not add a Nuru attribute, a `[NuruRouteGroup]`, or an app-side map. Source version at filing is `2.0.0-beta.23`; NuGet latest is `2.0.0-beta.22`. This task starts when that release is restored.

Nuru reads the group as `groupPath` in timewarp-nuru 508-014 (without prefixing the route). 508-004 tool filtering uses that same path. 508-012 stays shelved.

Groups, from 003:

- `symbols`: get_symbol, get_members, get_symbol_body, find_definition, search_symbols, get_expression_info, get_type_hierarchy, find_implementations
- `references`: find_references, get_callers, get_callees, find_reads, find_writes, find_dead_code, find_dead_conditionals
- `solution`: open_solution, get_solution_status, reload_solution, get_diagnostics, get_code_actions
- `edit`: apply_code_action, apply_code_fix, extract_method, change_signature, rename_symbol, rename_parameter, remove_unused_usings, apply_patch
- `batch`: multi_query

The attributes live on the request types under `Source/App/Morris.Roslynk/Features/`. Operation names stay as they are.

## Goal (implementer restatement)

Give each of Roslynk's 29 `[Operation]` request types one purpose group — `symbols`, `references`, `solution`,
`edit` or `batch` — declared once through the new `OperationAttribute.Group` in TimeWarp.Contracts 2.0.0-beta.23,
so the Nuru CLI (4.0.0-beta.3) exposes it as each route's `groupPath` and `roslynk --capabilities --group-filter <g>`
can serve one group's slice of the catalog instead of the whole flat one.

Acceptance:

- Contracts pinned to 2.0.0-beta.23; Nuru (both props files) and Nuru.DevCli pinned to 4.0.0-beta.3.
- Every operation carries exactly one of the five groups (8/7/5/8/1); no Nuru attribute, no app-side map, no
  Nuru reference in the engine.
- Route patterns, MCP tool names and the published MCP schema are unchanged (McpTests unchanged).
- `CapabilitiesContractTests` asserts each route's one-segment `groupPath`.
- `--group-filter` returns exactly each group's routes; catalog sizes (chars, ~tokens) recorded in Results.
- README, AGENTS.md "CLI front end" and the existing Unreleased `releases.md` bullet document the groups.
- `bin/dev build` 0 warnings, Cli.Tests and McpTests green, `ganda repo audit` passes.

## Checklist

- [x] Pins: `TimeWarp.Contracts` 2.0.0-beta.23 (has `OperationAttribute.Group`) in `Source/Directory.Packages.props`;
      `TimeWarp.Nuru` 4.0.0-beta.3 (reads it as `groupPath`, nuru 508-014) in `Source/Directory.Packages.props`
      and the root `Directory.Packages.props`, plus `TimeWarp.Nuru.DevCli` 4.0.0-beta.3 in the root one
- [x] Each of the 29 operations has exactly one of the five groups above
- [x] No TimeWarp.Nuru reference in the engine, and no second group map
- [x] Operation names are unchanged
- [x] `CapabilitiesContractTests` asserts each route's `groupPath` (one segment, the group above) and that the
      29 route patterns and MCP tool names are unchanged; McpTests 30/30 unchanged (the MCP schema must not move)
- [x] `roslynk --capabilities --group-filter <g>` returns exactly that group's routes for each of the five;
      record in Results the catalog size (characters and an approximate token count) for the full catalog and
      for each group, since cutting the ~16k-token flat catalog was the reason for groups (Roslynk 003 / nuru 506)
- [x] Docs: README CLI section and AGENTS.md "CLI front end" drop "ungrouped", list the five groups and show
      `--group-filter`; say a new operation must declare one of them. `releases.md`: extend the existing
      Unreleased CLI bullet (it ends `(Fixes #4)`; this task has no issue of its own) rather than adding one
- [x] `bin/dev build` 0 warnings, Cli.Tests and McpTests green, `ganda repo audit` passes

## Session

- Created: 3436453 (2026-10-19)
- Filed: timewarp-architecture 388 (2026-10-19).
- 2026-10-20 cockpit (2532391): unblocked. TimeWarp.Contracts 2.0.0-beta.23 (architecture 388) and
  TimeWarp.Nuru 4.0.0-beta.3 (nuru 508-014, PR #333) are both indexed on nuget.org. 508-014 semantics: a non-blank
  `Group` becomes `groupPath`, segments split on `.` or space; it does not prefix the route, the command or the
  McpHost tool name, and it wins over `[NuruRouteGroup]` / `[NuruCliBinding(Group)]`. The bare, `daemon` and
  `stdio` host routes from Roslynk 004 stay ungrouped, so they drop out of any `--group-filter` result.
  Counts check: 8 + 7 + 5 + 8 + 1 = 29.

- 2026-10-20 implement (claude): pins bumped, `Group` added to all 29 `[Operation]`s, `CapabilitiesContractTests`
  extended, docs updated. Rebuilt `bin/dev` against Nuru.DevCli beta.3 (`self-install`). Audit: the only finding was the
  advisory `vscode-window-icon` (missing `.vscode/tasks.json`/`settings.json`); `ganda repo audit --fix` wrote them
  locally (`.vscode/` is gitignored, so nothing to commit) and the audit now passes all checks.
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-20T03:09:48Z
- Review oracle: grok session 01a15749-6275-7da2-8dbe-a49ecf133a60 (2026-10-20), effort 2, roster general, round 1, disposition clean
- Review oracle: review by reviewer-grok (grok, model grok-4.7), session not reported, max-turns 120 — 2026-10-20T03:15:25Z

## Results

- Pins: `TimeWarp.Contracts` 2.0.0-beta.23 (`Source/Directory.Packages.props`); `TimeWarp.Nuru` 4.0.0-beta.3 (both
  props files); `TimeWarp.Nuru.DevCli` 4.0.0-beta.3 (root).
- Each of the 29 request types under `Source/App/Morris.Roslynk/Features/` now ends its attribute with
  `Group = "<group>"` (8 symbols, 7 references, 5 solution, 8 edit, 1 batch). Descriptions and operation names are
  byte-identical; the engine still has no Nuru reference and there is no other group map.
- `roslynk --capabilities` reports `groupPath: ["<group>"]` for each operation route and `[]` for the bare, `daemon`
  and `stdio` host routes.
- `CapabilitiesContractTests` (Cli.Tests) adds: the operation names equal the 29 keys of `ExpectedGroups`; each
  route's `groupPath` is exactly its one group; the host routes are ungrouped; `--group-filter <g>` returns exactly
  that group's routes for each of the five. The existing pattern/name equality test is unchanged.
- Docs: README CLI section (group table plus a `--group-filter` example), AGENTS.md "CLI front end" (Groups bullet
  rewritten; "Adding an operation" now asks for a group; Verbatim output says beta.3), `releases.md` Unreleased CLI
  bullet extended (still `(Fixes #4)`). The `VerbatimTerminal` doc comment now says beta.3 (output tests pass unchanged).
- Gates: `bin/dev build` 0 warnings, 0 errors; Cli.Tests 190/190; McpTests 30/30 (no MCP test changed);
  `ganda repo audit` passes all checks.

Catalog size (`roslynk --capabilities [--group-filter g]` stdout; tokens ≈ chars / 4):

| Catalog | Routes | Characters | ≈ Tokens |
| --- | --- | --- | --- |
| full | 32 (29 + 3 host) | 63,190 | 15,800 |
| symbols | 8 | 17,076 | 4,270 |
| references | 7 | 14,922 | 3,730 |
| solution | 5 | 10,257 | 2,560 |
| edit | 8 | 18,348 | 4,590 |
| batch | 1 | 2,718 | 680 |

The largest group slice is ~29% of the full catalog.

### How to validate

Smoke:

```bash
bin/dev build
dotnet test Source/App/Morris.Roslynk.Cli.Tests/Morris.Roslynk.Cli.Tests.csproj -c Release
dotnet test Source/App/Morris.Roslynk.McpTests/Morris.Roslynk.McpTests.csproj -c Release
R="dotnet Source/App/Morris.Roslynk.Cli/bin/Release/net10.0/roslynk.dll"
for g in symbols references solution edit batch; do
  $R --capabilities --group-filter $g | python3 -c "import json,sys; print(sys.argv[1], sorted(e['pattern'] for e in json.load(sys.stdin)['endpoints']))" $g
done
ganda repo audit
```

Expect: build reports 0 warnings; Cli.Tests 190 passed, McpTests 30 passed; each group prints exactly its routes
from the Description (8 / 7 / 5 / 8 / 1, no host routes); audit says the repository passes.

### Review

Round 1, effort 2, roster `general` (grok session `01a15749-6275-7da2-8dbe-a49ecf133a60`). The implementer restatement matches the original description and checklist. No findings. Disposition `clean` (final open count 0).

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.

## Review

- Round 1: Done

## Notes

- Parent: 003. Related: timewarp-architecture 388, timewarp-nuru 508-014 (reads the group as `groupPath`), 508-004 (tool filtering), 508-012 (shelved), Roslynk 004 (host unification; it should not invent its own group map).
