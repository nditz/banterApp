Implement ONLY Phase <PHASE_NUMBER> from the approved Ball Takes v2 UI backlog.

Before coding:
1. Read the v2 plan and `.cursor/skills/balltakes-frontend-polish/SKILL.md`.
2. Read `plans/UI-AUDIT-RESULTS.md` and `plans/UI-IMPLEMENTATION-BACKLOG.md`.
3. List the exact tasks you will implement in this phase and the files likely affected.
4. Confirm no task belongs to a later phase.

During implementation:
- reuse/refactor existing components before creating duplicates
- preserve contracts and existing working behavior
- implement loading/loaded/empty/stale/error states for touched async surfaces
- keep Studio/pundit/receipt product model intact
- do not introduce fake production data
- do not bypass consent/security controls
- prefer installed dependencies
- keep mobile/accessibility/performance in scope

After implementation:
- run formatter/lint/typecheck/tests/build available in the repo
- visually verify touched routes at mobile and desktop sizes using available tooling
- update `plans/UI-IMPLEMENTATION-LOG.md`
- list remaining failures or data-dependent blockers explicitly

STOP after this phase. Do not automatically start the next phase.
