# Round 2 — merged findings
**Date:** 2026-11-11
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: README.md:33
- Description: The README required the .NET 10 SDK, but the tool targets net11.0.
- Source: general (round 1)
- Disposition notes: Fixed in `f4cdbf9f`. The README requires the .NET 11 SDK, and no other prerequisite there says .NET 10.

### M2 — Severity: suggestion — Status: fixed
- File: AGENTS.md:35, AGENTS.md:167, AGENTS.md:217
- Description: The .NET 10 and CI-SDK sentences were stale.
- Source: general (round 1)
- Disposition notes: Fixed in `f4cdbf9f`. AGENTS.md now says the product and the tools are net11.0, CI installs 8/10/11 (checked against workflow.yml), and a developer needs the SDK 10 band beside 11.

### M3 — Severity: nit — Status: fixed
- File: source/App/Morris.Roslynk.Tests/Features/Diagnostics/GetDiagnosticsTests/GetDiagnosticsTests.cs:191
- Description: The MultiTarget comment was wrong.
- Source: general (round 1)
- Disposition notes: Fixed in `f4cdbf9f`. The comment matches `Multi.csproj`, and the test still passes.

### M4 — Severity: suggestion — Status: fixed
- File: task.md ## Results
- Description: Results did not flag `DedupeMetadataCopies` as user-visible.
- Source: general (round 1)
- Disposition notes: Fixed in `f4cdbf9f`. Results now list it as a candidate `releases.md` bullet.

## Duplicates / conflicts

- None. This round raised no new findings.
