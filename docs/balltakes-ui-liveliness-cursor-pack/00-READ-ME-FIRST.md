# Ball Takes UI Liveliness + Frontend Remediation Pack

This package is designed to be copied into the Ball Takes repository (or into your project plans folder first, then selected files copied to repo root).

## Primary goal
Make Ball Takes feel like a finished, energetic football-content product without rebuilding the application or destabilising working backend/data flows.

The product loop that the UI must communicate is:

**Discover banter -> Follow pundits -> Predict -> Compare -> Receipt -> Studio -> Export content pack -> Create media externally -> Share**

Studio is the destination, not a secondary utility.

## Important workflow
1. Run the audit prompt first.
2. Cursor must inspect the existing Next.js implementation before editing.
3. Create a visual/component inventory and implementation backlog.
4. Implement one phase at a time.
5. Verify desktop + mobile after each phase.
6. Do not redesign backend contracts unless required to fix a broken user-facing state.

## Copy these into the repository root
- `.cursor/rules/*`
- `.cursor/skills/balltakes-frontend-polish/*`

The `plans/`, `prompts/`, and `checklists/` folders can remain in your existing project plans directory.

Start with `prompts/MASTER-KICKOFF-PROMPT.md`.
