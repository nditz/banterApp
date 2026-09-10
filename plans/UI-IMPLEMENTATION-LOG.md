# UI Implementation Log

## Phase 1 — Foundations and state safety

**Date:** 2026-09-10

- P1-01 Browse without blocking terms modal. Saves/follows/leagues/season calls call `requireTerms()`.
- P1-02 HomeStatsBar / AuraSummary / StudioSummaryBar no longer present Aura 0 as loaded personal stats.
- P1-03 `stripHtml` on Studio story title/summary and feed title/body.
- P1-04 Comparison teaser shows loading/error/empty instead of `null`.
- P1-05 Season Calls uses Skeleton / EmptyState / ErrorState.
- P1-06 Shared `FreshnessBadge` on matchweek, table, feed.
- P1-07 SectionHeader / PageContainer on matchweek, awards, leagues, table, Me.
- P1-08 Ad consent banner sits above mobile bottom nav.
- P1-09 Welcome autoplay paused under reduced motion (carousel later removed in Phase 2).
- P1-10 Deleted unused `PredictionLockBanner`.

---

## Phase 2 — Homepage live product experience

**Date:** 2026-09-10

- P2-01 Compact hero: `Football keeps the score. We keep the receipts.` CTAs: Make a pick, Open Studio, How it works → `/rules`.
- P2-02 Order: hero → live takes → first fixture → pundits → Studio demo → leagues/table.
- P2-03 Personal stats hidden for guests/new users (no Aura 0 dashboard).
- P2-04 Returning recap only when signed-in APIs return real rows.
- P2-05 Homepage pundit strip (3–6 desks), Follow via `requireTerms()`.
- P2-06 Studio demo labelled when using product-shape placeholders.
- P2-07 Banter nav is active on `/banter`, not `/`. Homepage quick-nav still `#banter-feed`.

---

## Phase 3 — Matchweek lock-in

**Date:** 2026-09-10

- P3-01 MatchweekBoard and PredictionCenter show **X of Y locked**.
- P3-02 After save: lock copy, not a settled receipt.
- P3-03 Inline Follow on pundit comparison rows; `requireTerms()` first.
- P3-04 Save errors stay visible.
- P3-05 MatchCard still always renders the lock-in panel.
- P3-06 No ads inside pick controls.

---

## Phase 4 — Pundits, receipts, Studio from history

**Date:** 2026-09-10

- P4-01 Compact follow CTA on MeHub. Homepage strip in Phase 2.
- P4-02 FT MatchCard with a pick: **Open in Studio**.
- P4-03 Pre-FT copy never claims a settled receipt.
- P4-04 Settled receipts list `storyCandidates`.
- P4-05 World Cup chrome labels sanitized; pundit stays visible.
- P4-06 Directory loading uses skeleton cards; errors use `ErrorState`.

---

## Phase 5 — Studio creator workspace

**Date:** 2026-09-10

- P5-01 Desktop story rail + workspace + preview; mobile stepper with visible labels.
- P5-02 Perspective step; optional POST `perspective`.
- P5-03 Empty story rails omitted.
- P5-04 Script tab labelled Legacy export / persona.
- P5-05 occurredAt / why-it-matters / FreshnessBadge; stripHtml kept.
- P5-06 Back to Banter (`/banter`) + Share later after pack.
- P5-07 remainingGenerations shown only when the API returns a number.

---

## Phase 6 — Banter feed and retention

**Date:** 2026-09-10

- P6-01 Discriminated `FeedCard` registry. Unknown types map to `news`.
- P6-02 Per-type cards; title/body from the API only.
- P6-03 Unsplash defaults removed. Never “Just now”.
- P6-04 Manual Refresh on `FeedList`. Infinite scroll kept.
- P6-05 Studio CTA when receipt/story/feed ids exist. Pick CTA when `matchId` exists.
- P6-06 `/banter` page. Primary Banter href is `/banter`. Banter is not active on `/`.

### Data-dependent blockers
API still emits the existing six types only. Public timeline does not emit receipt ids, so Studio CTAs stay hidden until the API sends them.

---

## Phase 7 — Polish

**Date:** 2026-09-10

- P7-01 Overflow clip; HomeQuickNav wraps; header tighter on 375.
- P7-02 Escape closes menus; pick-button focus rings; menu outside-click attaches on next tick.
- P7-03 Reduced-motion safety net; feed spinners use `motion-reduce:animate-none`.
- P7-04 `next/image` for feed raster on configured hosts. Giphy/Tenor/GIF/SVG stay `<img>`.
- P7-05 Removed unused direct `@radix-ui/react-dialog|slot|tabs`.
- P7-06 Theme: paper-light default. Theme toggle hydrates as light (not inverted).
- P7-07 Turnstile retry after failed challenge.
- P7-08 SEO kept; `/banter` in sitemap.

---

## Verify

- lint, `tsc --noEmit`, vitest 71/71, `next build` (includes `/banter`).
- Playwright public pages at 375 and 1440: no horizontal overflow, including `/banter`.
- Phase 2 IA: compact hero, Studio tabs present, Banter href `/banter`.
- Backend was not running locally (`ECONNREFUSED` on `:5000`). Lock, follow, Refresh, and Studio-from-receipt were not click-verified against live data.
- Terms still required to save. Ads still opt-in.
