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

## Checklist

- [ ] Restore the TimeWarp.Contracts release that contains `OperationAttribute.Group`
- [ ] Each of the 29 operations has exactly one of the five groups above
- [ ] No TimeWarp.Nuru reference in the engine, and no second group map
- [ ] Operation names are unchanged
- [ ] Build and the operation contract tests pass

## Session

- Created: 3436453 (2026-10-19)
- Filed: timewarp-architecture 388 (2026-10-19).

## Notes

- Parent: 003. Related: timewarp-architecture 388, timewarp-nuru 508-014 (reads the group as `groupPath`), 508-004 (tool filtering), 508-012 (shelved), Roslynk 004 (host unification; it should not invent its own group map).
