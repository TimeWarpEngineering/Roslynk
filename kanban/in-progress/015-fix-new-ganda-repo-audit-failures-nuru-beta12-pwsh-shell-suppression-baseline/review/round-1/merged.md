# Round 1 — merged findings
**Date:** 2026-11-11
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 2 | 0 | 0 |
| nit | 1 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: README.md:33
- Description: The README still says "Requires the .NET 10 SDK", but the TimeWarp.Roslynk tool now targets net11.0, so a .NET 10-only user cannot run it.
- Suggestion: Say .NET 11 at README.md:33, and check the README for other version prerequisites.
- Source: general
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: AGENTS.md:35, AGENTS.md:217
- Description: AGENTS.md:35 says `source/Directory.Build.props` sets .NET 10. AGENTS.md:217 says CI has only 10.0 installed. Both are now stale: CI installs 8/10/11, product and tools are net11, and fixtures stay on the SDK 10 band.
- Suggestion: Update both sentences, and say a developer needs the SDK 10 band beside 11 for fixture loads.
- Source: general
- Disposition notes:

### M3 — Severity: nit — Status: open
- File: source/App/Morris.Roslynk.Tests/Features/Diagnostics/GetDiagnosticsTests/GetDiagnosticsTests.cs:191
- Description: The comment says Directory.Build.props forces the committed MultiTarget fixture to a single net10.0 target framework. That is false since `Multi.csproj` clears `TargetFramework`.
- Suggestion: Reword or drop the comment.
- Source: general
- Disposition notes:

### M4 — Severity: suggestion — Status: open
- File: task.md ## Results ("User-visible" paragraph)
- Description: The `SymbolResolver.DedupeMetadataCopies` fix is user-visible: in a multi-targeted solution, a BCL type now resolves once instead of once per framework. It is not flagged as user-visible, and Results say "No other user-facing behavior change was intended".
- Suggestion: List it next to the .NET 11 requirement as a candidate `releases.md` bullet.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None. A single reviewer ran this round.

## Not raised

- The `kanban-landed` audit failure is host-owned landing state for the in-progress kitchen, not a product defect.
