# Cursor Phase Implementation Prompt

Read:
- all plan documents;
- `AUDIT-RESULTS.md`;
- `IMPLEMENTATION-BACKLOG.md`;
- the implementation log.

Implement ONLY the next unfinished phase from `17-IMPLEMENTATION-PHASES.md`.

Rules:
1. Preserve existing architecture where sensible.
2. Do not refactor unrelated code.
3. Prefer extending existing services/components over duplicating them.
4. Add tests for all changed critical paths.
5. Add/modify migrations only when required.
6. Never commit secrets.
7. Do not remove Studio.
8. Do not replace real pundit quotes with fabricated quotes.
9. Do not silently hide data failures.
10. Update the implementation log.

Before coding:
- list the exact backlog items being addressed;
- identify impacted files/components/services;
- identify risks.

After coding:
- run relevant unit tests;
- run integration tests where available;
- run lint/type checks;
- run production build;
- verify changed routes;
- update acceptance criteria status;
- record remaining failures.

Do not begin the following phase until the current phase is verified.
