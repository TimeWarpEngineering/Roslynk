# Roslynk Kanban Board

This board tracks work on the TimeWarpEngineering fork of Roslynk. Tasks are managed with the
`ganda kanban` CLI (see the `tw-kanban` skill); never hand-number task files.

## Structure

- `backlog/`: ideas and research not yet ready to start.
- `to-do/`: ready to start; has met the Definition of Ready.
- `in-progress/`: claimed and being worked on in a ganda task worktree.
- `done/`: completed and merged.
- `archived/`: no longer relevant, kept for history.
- `task-template.md`: the shape of a new task.

## Task layout

Each task is a folder `NNN-short-description/` holding `task.md` (a standalone task may also be a
single `NNN-short-description.md`). Ids are allocated by `ganda kanban create`.
Sub-tasks use nested ids, for example `001-001`.

`task.md` sections: description, requirements, checklist, notes, `## Session` (claim record), and
`## Results` (added when completing).

## Workflow

1. `ganda kanban create "title"` allocates the id, claims it, and creates the task worktree.
2. Work only in that worktree; keep the checklist and `## Session` current.
3. Commit board changes on the task branch.
4. Add `## Results` before `ganda kanban done`.
5. After the PR merges, run `ganda worktree gc --task-id NNN`.

## Definition of Ready

- [ ] Requirements and acceptance criteria are clear.
- [ ] Impact on tool contracts (`[Description]` attributes, `SKILL.md`) is understood.
- [ ] Dependencies are identified and available.

## Definition of Done

- [ ] Core functionality implemented.
- [ ] Tests added or updated (`bin/dev test`, or focused `dotnet test` filters).
- [ ] `bin/dev build` succeeds with no new warnings.
- [ ] `releases.md` `# Unreleased` updated for feature changes, per `AGENTS.md`.
- [ ] `ganda repo audit` passes.
- [ ] `## Results` recorded in the task.
