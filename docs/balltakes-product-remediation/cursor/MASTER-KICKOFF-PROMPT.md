# Cursor Master Kickoff Prompt

You are working on the existing Ball Takes production application.

Your task is to evolve the existing application into the product described in the `balltakes-product-remediation` plan folder.

IMPORTANT: DO NOT START IMPLEMENTING FEATURES YET.

First read every markdown file in this plan folder, especially:
- 00-READ-ME-FIRST.md
- 01-PRODUCT-VISION.md
- 02-CRITICAL-AUDIT-FRAMEWORK.md
- 03-FEATURE-DECISIONS.md
- 17-IMPLEMENTATION-PHASES.md
- 16-ACCEPTANCE-CRITERIA.md

Then perform Phase 0: AUDIT ONLY.

## Product Context
Ball Takes is a football content platform centered on:
- user predictions/takes;
- followed pundits;
- user-vs-pundit comparison;
- real match results;
- receipts;
- banter/story generation;
- Studio content packs for external AI/media tools.

Studio is a CENTRAL product feature. Do not remove or demote Studio.

The primary product loop is:
Follow pundits → Predict → Football happens → Compare → Receipt → Banter → Studio → Export content pack → External AI/media platform → Social post.

The homepage should also provide an entertaining rolling timeline of memes, GIFs, pundit receipts, general football banter and examples of content so visitors immediately understand the product.

## Phase 0 Instructions
Do not change production code.

Inspect the full repository and produce:
`plans/balltakes-remediation/AUDIT-RESULTS.md`

The audit must include:
1. Architecture summary.
2. Frontend route inventory.
3. Backend/API inventory.
4. Database/entity/table inventory relevant to the product.
5. Background jobs and schedules.
6. External data providers.
7. Existing prediction flow.
8. Existing pundit flow.
9. Existing banter/Giphy/OpenAI flow.
10. Existing Studio implementation.
11. Existing Supabase auth/admin implementation.
12. Current AdSense implementation.
13. Current analytics/telemetry.
14. Legacy World Cup code/content residue.
15. Broken or incomplete flows.
16. Duplicate or redundant components.
17. Existing tests.
18. Security/privacy concerns.
19. SEO/stale metadata issues.
20. P0/P1/P2/P3 remediation backlog.

For every planned feature, explicitly mark:
- EXISTS AND REUSABLE
- EXISTS BUT NEEDS REFACTOR
- PARTIAL
- MISSING
- OBSOLETE

Do not assume this plan is more correct than working code. When existing architecture already solves a requirement well, preserve it.

## Critical Audit Targets
Pay particular attention to:
- why current matchweek fixtures may be missing;
- why standings may be empty;
- whether football sync jobs are running;
- whether World Cup-era business logic remains;
- why AdSense placeholders do not fill/collapse;
- whether pundit data is structured enough for comparison;
- whether Studio has access to all relevant context;
- whether the homepage Banter feed has usable data;
- whether result settlement generates reusable events/receipts.

## Audit Completion Gate
After `AUDIT-RESULTS.md` is complete:
1. Compare it against `16-ACCEPTANCE-CRITERIA.md`.
2. Create `plans/balltakes-remediation/IMPLEMENTATION-BACKLOG.md`.
3. Group backlog items under the phases in `17-IMPLEMENTATION-PHASES.md`.
4. List dependencies and migrations.
5. Identify anything in this plan that should be changed because the repository has a better existing implementation.

STOP after Phase 0.
Do not implement Phase 1 in the same run.

At the end, print a concise summary containing:
- top 10 gaps;
- top 5 risks;
- reusable components/services discovered;
- recommended first Phase 1 tasks.
