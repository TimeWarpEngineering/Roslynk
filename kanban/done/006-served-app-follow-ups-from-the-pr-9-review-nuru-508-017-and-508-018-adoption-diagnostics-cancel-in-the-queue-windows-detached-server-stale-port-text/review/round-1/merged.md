# Round 1 — merged findings
**Date:** 2026-10-21
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: AGENTS.md:182
- Description: The MCP-contracts paragraph still says a new member type must round-trip through JSON "because a request crosses from the app's copy of the contract to the engine's as JSON (`OperationHandler.ToEngine`)". That method and the linked app copy are gone. Handlers call `FromCaller`, which resolves `solutionId` / `solutionPath` on the engine request Nuru already bound. The same file's handlers bullet (AGENTS.md:249) already says that. The JSON requirement itself is still right for `--json-args` and MCP; the reason and the method name are not, so the next change can be steered back toward a second contract copy.
- Suggestion: Keep the System.Text.Json round-trip rule, and tie it to Nuru binding `--json-args` and MCP arguments. Remove `OperationHandler.ToEngine` and the claim that the app still has its own copy of the contract.
- Source: general
- Disposition notes: fixed in the round-2 fix pass (2026-10-21). AGENTS.md now ties the System.Text.Json round-trip rule to Nuru binding `--json-args` and MCP arguments onto the engine's request type, and says the app has no contract copy of its own. `OperationHandler.ToEngine` is no longer named; no other stale reference remains in AGENTS.md, README.md or SKILL.md.

## Duplicates / conflicts

- One reviewer. Nothing to collapse.
