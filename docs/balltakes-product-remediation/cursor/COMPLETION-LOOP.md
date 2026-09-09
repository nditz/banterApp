# Cursor Completion Loop

Use this loop after every implementation phase.

1. Read the current phase definition.
2. Read acceptance criteria.
3. Run tests/lint/build.
4. Inspect production-relevant flows.
5. Create/update verification report.
6. Identify failures.
7. Fix failures only within current phase scope.
8. Rerun verification.
9. Repeat until all current-phase P0/P1 items pass.
10. Update implementation log.
11. Mark phase complete.
12. Move to next phase only in a new deliberate task/run.

Never skip verification because the code compiles.
