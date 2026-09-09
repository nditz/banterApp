# Implementation Phases

## Phase 0 - Audit Only
No code changes.

Deliverables:
- architecture map;
- route inventory;
- feature inventory;
- job inventory;
- data-source map;
- legacy World Cup residue report;
- gap report with P0/P1/P2/P3;
- delta implementation proposal.

Gate: `AUDIT-RESULTS.md` reviewed by Cursor against this package.

## Phase 1 - Production Integrity
Fix P0 issues:
- fixture loading;
- current matchweek;
- standings/data failures;
- background job failures;
- silent errors;
- broken empty states;
- stale competition references.

## Phase 2 - Product Cleanup
- align navigation;
- reframe Awards as Season Calls where appropriate;
- demote commodity table UX;
- remove/merge stale World Cup-only pages;
- preserve Studio and make its central role explicit.

## Phase 3 - Pundits & Comparison
- expose followed pundits;
- normalize pundit predictions/takes;
- add before/after comparison views;
- preserve source attribution.

## Phase 4 - Receipt Engine
- create persistent structured receipts;
- generate story candidates after outcomes/events;
- add history/receipt UI.

## Phase 5 - Studio Evolution
- replace blank/general Studio entry with story-driven workspace;
- build content-type and tone selection;
- build structured content packs;
- add copy/export;
- retain project history where possible.

## Phase 6 - Homepage Banter Timeline
- rolling public feed;
- GIF/meme diversity;
- pundit receipts;
- community/general football stories;
- personalization for signed-in users.

## Phase 7 - Leagues & Aura
- improve retention loops;
- rank movement stories;
- league rivalry receipts;
- Studio entry points from league events.

## Phase 8 - Ads & Consent
- centralized AdSlot;
- collapse no-fill;
- verify GDPR/ePrivacy consent behavior;
- add placement analytics.

## Phase 9 - Observability & Admin
- data-health dashboard;
- background job monitoring;
- trigger controls;
- error visibility;
- product funnel metrics.

## Phase 10 - Visual Finishing
Only after core functionality is reliable:
- design tokens;
- shared components;
- page hierarchy;
- responsive polish;
- transitions/micro-interactions;
- final accessibility pass.

## Phase 11 - Final Verification Loop
Repeat until all acceptance criteria pass:
1. run tests;
2. run lint/type checks;
3. build production bundle;
4. inspect key routes;
5. verify data/jobs;
6. compare against acceptance criteria;
7. record failures;
8. fix failures;
9. rerun.

