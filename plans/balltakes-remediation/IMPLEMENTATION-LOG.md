# Implementation Log

## Phase
Phase 5 — Studio Evolution

## Date
2026-09-09

## Existing Implementation Found
- Studio was a comparison + script workspace (`StudioPage` tabs: My Picks / vs League / vs Pundits / Script). Not a blank prompt, but not a story picker.
- `GeneratedContent` logged Analyze/Banter/Meme/VideoScript as plain strings. No structured pack.
- Receipts API + `/predictions/history` already existed from Phase 4. History linked to `/studio` without a receipt id.
- Video section was a dead “coming soon” CTA on non-script tabs.
- Broadcast/pundit script export already existed and is kept.

## Changes Made
- **P5-01** Studio default tab is Stories: latest receipts, you vs pundits, trending (sourced `news_feed_items` only), previous projects.
- **P5-02** Format chips (short, podcast, meme, caption, thread, carousel, commentary) and tone chips.
- **P5-03** `POST /api/studio/packs` returns a structured pack and stores JSON on `GeneratedContent` with new type `ContentPack`. No new table.
- **P5-04** Copy script / copy prompt / copy pack / download text / download JSON.
- **P5-05** Context assembler copies receipt/match/prediction or sourced headline facts. Missing scores stay missing. Pundit lines only from sourced takes. Creative framing is labeled separately.
- **P5-06** Removed the video coming-soon block. Comparison and Script tabs remain.
- Receipts history “Open in Studio” now deep-links `/studio?receipt={id}`.

## Files Changed
- Backend: `GeneratedContentType.ContentPack`, `StudioStoryService`, `StudioContextAssembler`, `StudioContentPackComposer`, `StudioPackService`, `StudioEndpoints`, validators, Program DI.
- Frontend: `StudioPage`, `StudioStoryWorkspace`, `useStudioStories`, `useStudioPack`, `studio-pack.ts`, `/studio` metadata, receipts history link, phase-2 e2e tab selector.
- Tests: assembler, composer, pack endpoints, pack export labels.

## Database / Migration Changes
None. Pack JSON lives in existing `generated_content.output`. Enum value `ContentPack` is stored as an integer; no schema migration.

## API Changes
- `GET /api/studio/stories` — public. Personal sections empty without a session. Trending is sourced feed rows only.
- `POST /api/studio/packs` — session + terms. Body: contentType, tone, receiptId | feedItemId | projectId.
- `GET /api/studio/packs` — owner list.
- `GET /api/studio/packs/{id}` — owner only; otherwise 404.
- Existing `GET /api/studio/comparison` unchanged.

DTOs omit user/anonymous ids. Facts include provenance (`match` / `prediction` / `receipt` / `pundit_source` / `news`).

## Tests Added/Updated
- Backend: **407 passed** (was 379 at Phase 4 close; includes 21 Studio tests).
- Frontend Vitest: **39 passed** (was 37).
- Lint: eslint clean on touched files. Typecheck: `tsc --noEmit` clean (via Next build).
- Production build: Next.js succeeded; `/studio` in the route list. Backend Release build succeeded.

## Risks / Follow-up
- Packs are deterministic from sourced facts, not an OpenAI rewrite. That is intentional for P5-05.
- Trending stays empty when `news_feed_items` is empty — designed empty, not a fake card.
- Anonymous generation still counts toward the existing 3-generation cap.
- Production needs this API + frontend deploy before the receipt → pack click-through is live.
- Do not invent pundit quotes. Do not remove Studio. Do not start Phase 6 in this run.

## Verification Results
See `plans/balltakes-remediation/VERIFICATION-REPORT.md` (2026-09-09 Phase 5 implement pass).

## Acceptance Criteria Status
- [x] Passed — story workspace, types/tone, structured pack, export, fact lock, video CTA removed (local tests/build)
- [ ] Partial — no authenticated production click-through of receipt → pack
- [ ] Blocked

**Phase complete:** **local yes / production not yet.** Next phase is Phase 6 after a production verify of Phase 5.

---

## Phase
Phases 6-11 - Homepage Timeline, Leagues & Aura, Ads & Consent, Observability, Visual Finishing, Final Verification

## Date
2026-09-10

Run mode: the owner asked for the remaining phases in one pass. The product is not public yet (single user, the owner), so breaking changes to client-side storage and ad behaviour were accepted rather than migrated.

## Existing Implementation Found
- Phase 6: `PersonalizedFeedService` + `FeedEndpoints` existed but the homepage also rendered a `localStorage` "banter_local_feed" list, so the timeline was part product, part browser cache. No community/crowd content.
- Phase 7: Aura lived in `frontend/src/lib/aura.ts` (localStorage) while the server already awarded `Prediction.PointsAwarded` - two competing truths. `/api/leaderboards/friends` returned an empty stub, and `lib/mock-data.ts` backed demo leagues/boards.
- Phase 8: AdSense loaded unless consent was explicitly "denied" (opt-out), every placement shared one slot id, and there was no first-party consent UI.
- Phase 9: `/admin/health` and `/admin/jobs` existed with data freshness and job history (including `NextRunAt` from Hangfire). The stats page showed "Not wired" cards; `AppMetric` was unused.
- Phase 10: `Panel`, `Card` and admin primitives existed; empty/error states were ad-hoc sentences per component.

## Changes Made
**Phase 6 - Homepage Banter Timeline**
- New `CommunityFeedService` builds anonymised crowd cards (pre-match crowd split, post-match "the crowd got cooked") from aggregate predictions, with a minimum-picks threshold so a single pick never becomes a card.
- `FeedEndpoints` merges personal + community items and rotates card types so the timeline does not run three of the same card back to back.
- `PersonalizedFeedService` now serves anonymous identities, adds an unfinished-picks card, and colours post-match copy from the owner's receipts without exposing receipt ids.
- Deleted `lib/banterFeed.ts` and `BanterLine.tsx`; `BanterFeedPanel` renders the server feed only.

**Phase 7 - Leagues & Aura**
- Decision on P7-01: Aura is a UX label over server points. No second currency, no client storage. New `GET /api/aura/me` returns total, weekly change, streak, settled/correct picks, rank and percentile, all derived from settled predictions plus matchweek bonuses.
- League standings and leaderboards return `rank`, `previousRank`, `rankDelta` and `weeklyPoints`; previous rank is derived from points earned before a 7-day cutoff, so no snapshot table was needed.
- `/api/leaderboards/friends` now aggregates the caller's custom leagues instead of returning an empty stub.
- Deleted `lib/aura.ts` and `lib/mock-data.ts`. Reaction cards show a vibe label ("Chaos tier", "Safe take") instead of a fake Aura number.

**Phase 8 - Ads & Consent**
- Advertising consent is now opt-in and reactive: `advertising-consent.ts` is a small store, `useAdvertisingConsent` subscribes to it, and nothing from Google is requested until the visitor answers `AdConsentBanner`. The choice can be changed later from `/privacy`.
- Distinct placement keys (`AD_PLACEMENT_KEYS`) with per-placement env overrides, replacing the single shared display slot.
- `AdSlot` collapses on no-fill (`data-ad-status="unfilled"`) and resets per route, so a dead unit never holds layout space.
- Fill analytics: `ad_slot_filled`, `ad_slot_unfilled` and `ad_init_failed` counters, surfaced in admin stats.

**Phase 9 - Observability & Admin**
- `ProductMetricService` + `POST /api/metrics/event` record allowlisted, anonymous funnel counters into the existing `AppMetric` table. Unknown keys are rejected so the endpoint cannot become a tracking sink.
- Frontend records: prediction made, receipt viewed, returned after result, Studio opened, content generated, content exported, league joined, pundit followed, ad fill/no-fill/init failure.
- `/admin/stats` shows real counts and distinguishes "genuine zero" from "never wired".
- `AdminHealthService` gained receipts, Studio generation and ad-system health plus a `BuildAlerts` pass (missing current matchweek, overdue fixtures, receipt backlog, repeated job failures, critical errors, ad init failures). `/admin/health` renders those alerts first.

**Phase 10 - Visual Finishing**
- Shared `EmptyState` / `ErrorState` primitives plus `SectionHeader` / `PageContainer`, adopted by the feed, matchweek board, league table, standings, leaderboards, leagues list, prediction centre, pundits directory, Studio tabs and receipts history.
- Global reduced-motion safety net in `globals.css` on top of the existing per-component rules.
- Token audit: outside the deliberately zinc-themed admin console, product surfaces already use design tokens - the only matches were false positives (`translate-y-1/2`, `ring-gold`). No token rewrite was needed.

**Phase 11 - Final Verification**
- Fixed four `react-hooks/set-state-in-effect` violations introduced by the consent work: added a `useHydrated` hook and moved `AdSlot` state behind a route-keyed value updated from observer callbacks.
- Added the missing backend coverage for the new Phase 7/9 services, and cleared the one nullable-dereference warning in `AdminOverviewService`.

## Files Changed
- Backend: `Features/Feed/CommunityFeedService.cs`, `PersonalizedFeedService`, `FeedEndpoints`, `Features/Aura/AuraEndpoints.cs`, `Features/Leagues/*`, `Features/Leaderboards/*`, `Features/Metrics/{ProductMetrics,ProductMetricService,MetricEndpoints}.cs`, `Features/Admin/{AdminHealthService,AdminOverviewService}.cs`, `Program.cs`.
- Frontend: `hooks/{useAura,useAdvertisingConsent,useHydrated,usePundits,useLeaderboard,useStudioPack}.ts`, `lib/{ads,advertising-consent,metrics,studio-pack,types}.ts`, `components/ads/*`, `components/feed/FeedList.tsx`, `components/home/*`, `components/rankings/*`, `components/leagues/LeaguesList.tsx`, `components/studio/StudioPage.tsx`, `components/ui/{states,section-header}.tsx`, `app/{admin/health,admin/stats,predictions/history,privacy}/page.tsx`, `app/globals.css`.
- Deleted: `lib/banterFeed.ts`, `lib/aura.ts`, `lib/mock-data.ts`, `components/BanterLine.tsx`.

## Database / Migration Changes
None. `AppMetric` already existed; Aura and rank movement are derived at query time.

## API Changes
- `GET /api/aura/me` - new. Session or anonymous identity; returns zeros for an unknown caller.
- `GET /api/leaderboards/friends` - now returns real aggregated standings.
- League standings / leaderboard entries gained `rank`, `previousRank`, `rankDelta`, `weeklyPoints`.
- `POST /api/metrics/event` - new. Anonymous, rate-limited, allowlisted keys only, 400 on anything else.
- Feed responses now include community cards. No user identifiers are emitted on them.

## Tests Added/Updated
- Backend: **424 passed** (was 407 at Phase 5 close). New: `Feed/CommunityFeedServiceTests` (threshold, pre/post-match copy, no identifier leakage), `Aura/AuraSummaryTests` (points + bonuses, weekly window, streak break, unsettled picks, rank/percentile), `Metrics/ProductMetricServiceTests` (allowlist, window, zero vs never-wired).
- Frontend Vitest: **41 passed** (was 39) - advertising consent is opt-in, placement keys resolve.
- ESLint: clean across the whole project. `tsc --noEmit`: clean. Next.js production build: succeeded. Backend Release build: succeeded with 0 warnings.

## Risks / Follow-up
- Ads are now opt-in. Expect ad revenue to drop relative to the previous opt-out behaviour; that is the intended trade.
- Auto Ads vs manual units (P8-04) cannot be settled in code - Auto Ads must stay off in the AdSense dashboard for the pages that render manual `AdSlot` units.
- Rank movement uses a rolling 7-day points window rather than a stored weekly snapshot. It is directionally right but not a true historical rank.
- Community cards need real prediction volume; below the threshold they simply do not appear.
- No browser click-through in this run (no browser tooling available in the session), so 375/768/1440 verification and keyboard/contrast checks are still owner-verified items.
- Nothing here is production-verified yet: all of it needs a deploy plus a pass through `cursor/VERIFY-PROMPT.md`.

## Verification Results
See `plans/balltakes-remediation/VERIFICATION-REPORT.md` (2026-09-10, Phases 6-11).

## Acceptance Criteria Status
- [x] Passed - product, football data, ads, UX, Studio and engineering criteria pass locally against tests, lint, typecheck and production builds
- [ ] Partial - responsive/a11y click-through and production verification outstanding
- [ ] Blocked

**Phase complete:** **local yes / production not yet.** Next action is deploy, then `cursor/VERIFY-PROMPT.md` against production.
