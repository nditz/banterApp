You are starting the UPDATED frontend product-quality remediation for the existing Ball Takes Next.js application.

This v2 plan REPLACES the previous Ball Takes UI-liveliness plan. Do not implement recommendations from the older pack when they conflict with this one.

IMPORTANT:
- Do not rebuild the application.
- Do not begin implementation yet.
- This first pass is AUDIT ONLY.
- Preserve working backend/data contracts.
- Studio is a PRIMARY product destination and must not be removed/demoted.
- Pundit comparison is a core differentiator.
- Do not bypass or weaken robot/CAPTCHA/WAF protection.
- Preserve GDPR/advertising consent behavior while auditing its UX.

Read all files in this v2 plan, especially:
- 00-READ-ME-FIRST.md
- 01-LIVE-SITE-UI-AUDIT.md
- 02-PRODUCT-UI-DIRECTION.md
- 03-HOMEPAGE-REDESIGN.md
- 04-STUDIO-UI-PLAN.md
- 05-BANTER-TIMELINE-COMPONENTS.md
- 06-MATCHWEEK-AND-DATA-STATES.md
- 07-CONSENT-ADS-ROBOT-UX.md
- 08-NEXTJS-FRONTEND-IMPLEMENTATION.md
- 09-IMPLEMENTATION-PHASES.md
- checklists/UI-ACCEPTANCE.md

Load the project skill `.cursor/skills/balltakes-frontend-polish/SKILL.md` and applicable `.cursor/rules`.

PRODUCT MODEL:
Discover banter -> Follow pundits -> Predict -> Compare user vs pundit vs reality -> Receipt/story -> Studio -> Content pack -> External AI/media tools -> Social content -> Return.

AUDIT:
1. Inspect package.json/lockfile and identify actual Next.js, React, router, styling, UI, animation, media and testing stack. Do not assume versions.
2. Map public/authenticated routes, layouts and navigation.
3. Inventory shared primitives vs duplicated page-specific UI.
4. Audit homepage, Matchweek/Predict, Banter, Studio, Leagues, Season Calls, Table/context, auth/onboarding, pundit surfaces, consent and ads.
5. For every major route record user goal, CTA, top five issues, loading/loaded/empty/stale/error states, mobile, accessibility and performance risks.
6. Identify all places that render blank/unfinished when data is missing or delayed.
7. Identify whether first-time users are shown excessive zero-state metrics instead of active public content.
8. Trace current pundit-following/comparison UI and identify missing steps in the receipt -> Studio journey.
9. Trace Studio from entry to output and document every gap preventing it from functioning as a creator workspace.
10. Inventory existing dependencies before recommending any new frontend package.
11. Capture baseline screenshots at representative mobile and desktop widths using the repo's available browser/test tooling if present.
12. Produce:
   - `plans/UI-AUDIT-RESULTS.md`
   - `plans/UI-IMPLEMENTATION-BACKLOG.md`

Backlog:
- group by phases in 09-IMPLEMENTATION-PHASES.md
- P0/P1/P2
- likely files/components
- visual-only vs data-dependent
- risk/dependencies
- acceptance criteria
- tests/verification

STOP after the audit/backlog. Do not implement Phase 1 until I review it.
