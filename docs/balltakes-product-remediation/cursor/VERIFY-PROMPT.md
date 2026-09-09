# Cursor Verification Prompt

Verify the current Ball Takes implementation against:
- `16-ACCEPTANCE-CRITERIA.md`
- current phase goals in `17-IMPLEMENTATION-PHASES.md`
- `AUDIT-RESULTS.md`
- `IMPLEMENTATION-BACKLOG.md`

Perform a strict verification pass.

Do not assume implementation is correct because code exists.

Check:
- runtime behavior;
- database/data state assumptions;
- background jobs;
- API responses;
- frontend rendering;
- loading/error/empty states;
- mobile behavior;
- stale World Cup content;
- Studio context quality;
- receipt flow;
- pundit comparison;
- ad collapse behavior;
- tests/build/lint.

Create or update:
`plans/balltakes-remediation/VERIFICATION-REPORT.md`

For every failed item provide:
- severity;
- reproduction steps;
- root cause if known;
- files/services involved;
- recommended fix.

Do not mark a phase complete while P0 or P1 failures remain in that phase.
