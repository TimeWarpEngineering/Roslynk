# Round 2 — general
**Date:** 2026-10-21
**Scope reviewed:** commit `4b55243` since round 1 (`dcfd8fe`). The product change is `AGENTS.md` line 182. Kitchen notes in the same commit are status only.

## Summary

Round 1 M1 asked AGENTS.md to keep the System.Text.Json round-trip rule, tie it to Nuru binding `--json-args` and MCP arguments, and drop `OperationHandler.ToEngine` plus the claim of an app-side contract copy. The new sentence does that. `OperationHandler.FromCaller` returns the same request after resolving caller-relative paths; it does not serialize a second type. `StdioCommand` forwards MCP invokes as JSON through Nuru, and `[assembly: NuruEndpointAssembly]` publishes the engine request types, which matches "onto the engine's request type". The cross-reference "Hosting and the CLI" is the heading that says the app compiles no contract source of its own. `ToEngine` does not appear in `README.md` or `skills/roslynk/SKILL.md`. No new defect on this delta. The Goal restatement still differs from the inbound checklist on `MediatorAssembly` and on `--capabilities` default text; those mismatches are already under `## Learnings` and are outside this delta.

## Resolved prior

### M1 — Severity: suggestion — Status: fixed
- File: AGENTS.md:182
- Re-verified: the paragraph no longer names `OperationHandler.ToEngine` or an app contract copy. The reason is Nuru binding `--json-args` and MCP arguments onto the engine request type with System.Text.Json.

## Issues

None.
