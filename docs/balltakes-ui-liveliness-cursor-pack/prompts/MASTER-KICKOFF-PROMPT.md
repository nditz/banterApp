You are starting a frontend product-quality remediation of the existing Ball Takes application.

IMPORTANT:
- Do not rebuild the application.
- Do not change backend/data contracts unless necessary to correct a documented broken frontend state.
- Do not begin implementation yet.
- This first pass is AUDIT ONLY.

Read these files first:
- plans/balltakes-ui-liveliness-cursor-pack/00-READ-ME-FIRST.md (or equivalent copied location)
- 01-LIVE-SITE-UI-AUDIT.md
- 02-PRODUCT-UI-DIRECTION.md
- 03-HOMEPAGE-REDESIGN.md
- 04-STUDIO-UI-PLAN.md
- 05-BANTER-TIMELINE-COMPONENTS.md
- 06-MATCHWEEK-UI.md
- 07-ADSENSE-UI.md
- 08-NEXTJS-FRONTEND-IMPLEMENTATION.md
- 09-IMPLEMENTATION-PHASES.md
- checklists/UI-ACCEPTANCE.md

Also load the project skill `balltakes-frontend-polish` and applicable `.cursor/rules`.

PRODUCT INTENT:
Ball Takes is not merely a Premier League prediction site. Users make predictions, follow pundits, compare their football takes against pundit takes and actual results, generate receipts/banter, and use Studio to produce structured content packs for short videos, podcasts, memes, captions, threads and external AI/media tools.

The homepage should hook users with a lively mixed timeline of football banter, memes, GIFs, pundit receipts, community takes and match events. Studio is a PRIMARY destination.

AUDIT TASK:
1. Inspect package.json and identify Next.js version, router, React version, styling solution, component libraries, animation/media packages, test stack and package manager.
2. Map the main frontend routes and layouts.
3. Inventory design primitives and duplicated/one-off components.
4. Audit homepage, Matchweek, Banter/feed surfaces, Studio, Leagues, Awards/season calls, Table/context and navigation.
5. For every route identify:
   - user goal
   - primary CTA
   - top 5 UX/visual issues
   - loading/loaded/empty/error states
   - mobile weaknesses
   - accessibility issues
   - performance risks
6. Identify why fixtures, ads, content or other sections appear blank/unfinished from the UI perspective, without changing code yet.
7. Identify any UI that still communicates an outdated product model.
8. Identify components that should become shared primitives.
9. Identify existing dependencies that can support tasteful motion; do NOT propose new dependencies unless current capabilities are insufficient.
10. Produce:
   - `plans/UI-AUDIT-RESULTS.md`
   - `plans/UI-IMPLEMENTATION-BACKLOG.md`

Backlog requirements:
- group into phases from `09-IMPLEMENTATION-PHASES.md`
- P0/P1/P2 priority
- files/components likely affected
- risks
- dependencies
- acceptance criteria
- mark tasks that are visual-only vs data-dependent

STOP after creating the audit and backlog.
Do not implement Phase 1 until I review the audit.
