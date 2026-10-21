# Round 3 — merged findings
**Date:** 2026-10-21
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Resolved prior

### M1 — Severity: suggestion — Status: fixed
- File: AGENTS.md:182
- Description: The MCP-contracts paragraph still said a request crossed from the app's contract copy to the engine as JSON (`OperationHandler.ToEngine`).
- Suggestion: Keep the System.Text.Json round-trip rule, and tie it to Nuru binding `--json-args` and MCP arguments. Remove `OperationHandler.ToEngine` and the claim that the app still has its own copy of the contract.
- Source: general
- Disposition notes: Still fixed on this round. AGENTS.md:182 ties the rule to Nuru binding `--json-args` and MCP arguments onto the engine request type. `ToEngine` is gone from AGENTS.md, README.md, and `skills/roslynk/SKILL.md`. `FromCaller` mutates that same request in place.

## Issues

None.

## Duplicates / conflicts

- One reviewer. Nothing to collapse. No new findings on `61c7dc8`.
