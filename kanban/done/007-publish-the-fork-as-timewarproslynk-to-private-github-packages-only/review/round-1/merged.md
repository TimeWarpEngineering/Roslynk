# Round 1 — merged findings
**Date:** 2026-10-23
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 1 |

## Issues

### M1 — Severity: nit — Status: wontfix
- File: README.md:168
- Description: The per-build socket paragraph still says `dnx TimeWarp.Roslynk -- stdio` with no `--prerelease`. The install section (README.md:47-48) already says a prerelease-only package needs that flag, and the MCP config examples include it. AGENTS.md:238 uses the same shorthand while describing two builds side by side.
- Suggestion: Add `--prerelease` to the README sentence so a copied command does not miss the package.
- Source: general
- Disposition notes: Not an install recipe. The install section already requires `--prerelease` for `dnx`. Accepted by the review oracle (2026-10-23).

## Duplicates / conflicts

- None. One reviewer, one finding.
