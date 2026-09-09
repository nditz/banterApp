Implement the next approved Ball Takes UI remediation phase from `plans/UI-IMPLEMENTATION-BACKLOG.md`.

Before editing:
1. Load the `balltakes-frontend-polish` skill.
2. Read applicable `.cursor/rules`.
3. Read the audit findings for this phase.
4. Inspect the existing components and dependencies.

Rules:
- Preserve existing backend/data contracts unless the backlog explicitly approves a required change.
- Prefer refactoring/reusing components over adding duplicate variants.
- Design loading, loaded, empty and error states.
- Keep the shell restrained and let football/banter create visual energy.
- Do not introduce generic AI-dashboard effects.
- Do not introduce a new dependency unless necessary; explain why before adding it.
- Keep Studio central to the product journey.

After implementation:
- run the existing relevant lint/typecheck/tests/build
- review at 375px, 768px and 1440px
- check keyboard/focus and reduced-motion behavior
- update `plans/UI-IMPLEMENTATION-BACKLOG.md`
- append a concise entry to `plans/UI-IMPLEMENTATION-LOG.md`
- list unresolved items clearly
