---
name: balltakes-frontend-polish
description: Audit and improve the Ball Takes Next.js frontend when work involves UI polish, visual hierarchy, homepage energy, Banter feeds, Matchweek cards, Studio creator workflows, responsive behavior, empty/loading/error states, motion, design-system consistency, or frontend production quality. Use this skill for multi-step visual/product remediation rather than isolated one-line CSS changes.
---
# Ball Takes Frontend Polish

Work from product intent, not decorative styling.

## Workflow
1. Read `references/product-model.md` and `references/visual-system.md`.
2. Inspect the target route and its supporting components before editing.
3. State the user goal and primary action for the route.
4. Identify the five highest-impact UX/visual problems.
5. Check for reusable primitives before creating a new component.
6. Design loading, loaded, empty and error states.
7. Implement the smallest coherent change set.
8. Verify mobile, tablet and desktop.
9. Run existing lint/typecheck/test/build commands relevant to the edits.
10. Record remaining issues rather than hiding them.

## Non-negotiables
- Studio is central to Ball Takes.
- The homepage demonstrates the product before explaining it.
- Banter/media provides liveliness; the application shell stays restrained.
- Avoid generic AI-dashboard aesthetics.
- Do not destabilize backend contracts simply to make a page prettier.
- Do not declare work complete while core content is blank or only loading.

## For homepage work
Prioritize: compact hero -> live/mixed Banter timeline -> matchweek -> user-vs-pundit -> Studio teaser -> leagues/Aura -> short explanation.

## For Studio work
Build around existing context/receipts and content packs. Never default to a blank prompt-first interface.

## For Matchweek work
Use football-first fixture cards, visible progress, lock state and strong failure/empty handling.
