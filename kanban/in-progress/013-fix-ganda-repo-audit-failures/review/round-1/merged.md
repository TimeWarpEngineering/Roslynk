# Round 1 — merged findings
**Date:** 2026-11-08
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 1 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: open
- File: .editorconfig:317 (comment 318–322); AGENTS.md:283; task.md Session/Results
- Description: The line-cap waiver's reason says the check "has no path exclude" and "cannot waive source/ alone". The installed ganda (beta.38) has the per-path `.timewarp/line-cap-baseline.json` ratchet. The real reason `off` is right is that the ratchet is shrink-only, so upstream merges that grow the 12 `source/` files would fail the audit.
- Suggestion: Reword the `.editorconfig` comment, the AGENTS.md fork-workflow sentence and the task.md wording to name the baseline and why it does not fit an upstream fork. The severity stays `off`.
- Source: general
- Disposition notes:

### M2 — Severity: nit — Status: open
- File: .editorconfig:319
- Description: The comment cites `documentation/developer/standards/repo-audit.md`, a path that exists in timewarp-ganda but not in this repo.
- Suggestion: Qualify it with the repo name (timewarp-ganda).
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None (single reviewer).

## Verification run by reviewer

- `ganda repo audit` exits 0 ("Repository passes all audit checks.")
- `bin/dev build`: 0 warnings, 0 errors; `dotnet build tools/dev-cli/dev.cs` succeeds; `bin/dev` mtime is after the split source
- `bin/dev workflow --mode nope` prints the expected message and exits 1
- Fork-owned C# outside `source/` is at most 390 lines
- `bin/dev test` was not re-run: no file under `source/` changed. The implementer reported 336 + 1006 passing.
