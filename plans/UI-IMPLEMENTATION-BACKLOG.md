# Ball Takes UI Implementation Backlog (v2)

**Source:** `plans/UI-AUDIT-RESULTS.md` mapped onto `docs/balltakes-ui-liveliness-cursor-pack/09-IMPLEMENTATION-PHASES.md`.  
**Rule:** One phase per Cursor run (`prompts/IMPLEMENT-PHASE-PROMPT.md`). Verify with `prompts/VERIFY-UI-PROMPT.md`. Do not rebuild the app. Do not break backend contracts. Do not demote Studio. Do not fabricate feed/pundit content. Do not bypass Turnstile/WAF or collapse ads+terms into one consent.

**Priority:** P0 = looks broken, empty, or blocks the product. P1 = core loop/liveliness. P2 = polish.

**Type:** V = visual/UX only. D = needs API/data. Mix = UI plus careful contract-compatible backend.

---

## Phase 0 — Repository audit and baseline

**Status:** Done 2026-09-10. No further work in this phase.

| ID | Item | Pri | Type | Notes |
|---|---|---|---|---|
| P0-01 | Audit + backlog + live screenshots | — | V | `plans/UI-AUDIT-RESULTS.md`, this file, `plans/ui-audit-baselines/` |

---

## Phase 1 — Foundations and state safety

Do **not** redesign every page. Make the shell honest, states distinct, consent/ads safe.

| ID | Item | Pri | Type | Likely files | Risk / deps | Acceptance | Verify |
|---|---|---|---|---|---|---|---|
| P1-01 | Allow browsing without a blocking terms modal; keep terms required for **save/mutate** | P0 | V | `TermsGate.tsx`, `useNeedsTerms.ts`, `AppShell.tsx`, `TermsAcceptPanel.tsx` | Legal: do not weaken acceptance for writes. Copy already says “to save your picks.” | Anonymous users see home/matchweek/studio/feed. Saving a pick still gates on terms + Turnstile. No close-to-skip for the save gate. | Manual first-visit 375/1440; Playwright public pages still load; attempt save without terms still blocked |
| P1-02 | Stop rendering Aura/league/studio zeros as if they were loaded personal stats | P0 | Mix | `HomeStatsBar.tsx`, `AuraSummary.tsx`, `StudioSummaryBar.tsx`, `useAura.ts` | Aura API already exists; surface `isError`/`isPending` | Loading skeleton; error uses `ErrorState`; anonymous/new users do not see a “Your aura 0” dashboard above live content | Fail Aura query in UI test or React Query; visual QA |
| P1-03 | Never render raw HTML in Studio/feed summaries | P0 | Mix | `StudioStoryWorkspace.tsx` (story.summary), feed body, news ingest if needed | Prefer strip/sanitize in UI; backend strip is bonus | Trending cards show plain text. No `<p>`, `<b>`, `</li>` on screen | Live studio screenshot; unit test on sanitizer |
| P1-04 | Comparison/teaser must not vanish on API error | P0 | V | `useStudio.ts`, `MatchPunditComparison.tsx`, `MatchCard.tsx` | Do not invent pundit picks | Loading skeleton under fixture; error/empty designed; `null` return removed | Mock `ApiError` on `/api/studio/comparison` |
| P1-05 | Season Calls uses shared Skeleton/EmptyState/ErrorState | P1 | V | `TournamentBonusBoard.tsx`, `awards/page.tsx` | None | Five-state chrome matches table/matchweek | `/awards` loading + error |
| P1-06 | Shared `FreshnessBadge` + use on matchweek/table/feed where status exists | P1 | V | new small primitive, `MatchweekBoard`, `LeagueTable`, `FeedList`, `football-dataset.ts` | Stale already on football dataset | Stale cached data stays visible with freshness, not a blank error | Matchweek stale banner QA |
| P1-07 | Deduplicate page headers onto `SectionHeader` / `Panel` | P2 | V | matchweek, awards, leagues, table, me, `section-header.tsx` | Visual-only | Consistent kicker/title/description | Spot-check routes |
| P1-08 | Ad slot / consent stacking | P1 | V | `AdConsentBanner.tsx`, `MobileBottomNav.tsx`, `PageWithSideAds.tsx`, `globals.css` | Do not auto-grant ads. Do not place `matchweek-between-fixtures` inside pick controls | Banner and bottom nav do not cover each other; slots still collapse without consent | 375px with/without consent |
| P1-09 | Pause welcome autoplay when `prefers-reduced-motion` | P1 | V | `HomeWelcomePanel.tsx` | None | Timer does not advance under reduced motion | Playwright `emulateMedia({ reducedMotion: 'reduce' })` |
| P1-10 | Wire or delete `PredictionLockBanner` | P2 | V | `PredictionLockBanner.tsx`, matchweek | Dead code | No unused lock component | grep |

**Phase 1 out of scope:** homepage redesign, typed feed cards, Studio 3-pane layout, follow-on-home.

**Phase 1 gate:** lint/typecheck/frontend tests/build; 375 + 1440 on home, matchweek, studio, awards; terms still required to save; ads still opt-in.

---

## Phase 2 — Homepage live product experience

Use `prompts/HOMEPAGE-PHASE-PROMPT.md`. Homepage must **show** Ball Takes.

| ID | Item | Pri | Type | Likely files | Risk / deps | Acceptance | Verify |
|---|---|---|---|---|---|---|---|
| P2-01 | Compact product hero (not a 3-slide how-to) | P0 | V | `HomeWelcomePanel.tsx`, `WelcomeHeroSlide.tsx`, `scoring-rules.ts` `HOME_WELCOME_SLIDES`, `page.tsx` | Keep Studio CTA. Suggested copy in `03-HOMEPAGE-REDESIGN.md` | Hero is not a full viewport. Headline/CTAs: Make a pick + Open Studio / see Studio. Tour details move to `/rules` or a dismissible control | 375/1440 screenshots vs baselines |
| P2-02 | Reorder anonymous home: hero → live takes → first fixture → pundits → Studio demo → leagues | P0 | V | `page.tsx`, `BanterFeedPanel.tsx`, `PredictionCenter.tsx` | Do not fabricate feed items | First viewport after terms/hero shows real cards or a designed empty, not four stat tiles | Visual QA |
| P2-03 | Hide personal metric strip for anonymous/new users | P0 | V | `HomeStatsBar.tsx` | League count 2 is auto-join, not “my activity” | No Aura 0 / empty personal dashboard above the fold for first-time | Logged-out 375 |
| P2-04 | Returning-user home variant | P1 | Mix | `page.tsx`, new small composition, `useReceipts`, `useStudioStories`, unfinished picks | Data already exists; no fake recap | Returning: next pick, new receipts, followed-pundit activity, Studio-ready, league movement — only if APIs return them | Signed-in + empty vs populated |
| P2-05 | Homepage pundit discovery strip | P1 | Mix | new strip using `PunditsDirectory` pieces, `usePundits.ts` | Follow API exists | 3–6 followable desks with source; no invented quotes | `/` + `/pundits` stay in sync |
| P2-06 | Studio demonstration block (labelled if demo) | P1 | V | home, `StudioStoryWorkspace` extract, `CumulativeScriptExport` | Do not fake a pack from unsourced quotes | Shows take → receipt → pack shape, or designed empty + “Open Studio” | Visual QA |
| P2-07 | Stop marking Banter nav active for the whole homepage | P1 | V | `navigation.ts` `isNavHrefActive` | Hash routing limits | Banter active only when `#banter-feed` is the target or user is in the feed region if practical; otherwise don’t highlight on hero | Desktop nav |

**Phase 2 out of scope:** full typed card registry (Phase 6 can land the renderer; homepage may consume a first slice).

**Phase 2 gate:** anonymous home is entertaining without zeros; Studio still in nav + hero CTA; no fake engagement counts.

---

## Phase 3 — Matchweek / prediction experience

| ID | Item | Pri | Type | Likely files | Risk / deps | Acceptance | Verify |
|---|---|---|---|---|---|---|---|
| P3-01 | Matchweek progress (locked / remaining) | P1 | V | `MatchweekBoard.tsx`, `PredictionCenter.tsx` | Derived from existing matches | User sees X of Y locked without a new API | MW4 live |
| P3-02 | Lock feedback: “Locked. We'll keep the receipt.” | P1 | V | `PredictionButtons.tsx`, `PredictionCelebration.tsx`, `FixtureStatusBadge.tsx` | Do not claim a settled receipt before FT | After lock, payoff copy + optional followed-pundit teaser | Make a pick on open fixture |
| P3-03 | Inline follow on fixture pundit teaser | P1 | Mix | `MatchPunditComparison.tsx`, `useFollowPundit` | Same follow API as directory | Follow/unfollow without leaving matchweek when a pundit row exists; empty state still links to `/pundits` | Follow then see filtering |
| P3-04 | Visible save-error on prediction failure | P1 | V | `usePredictions` / MatchCard | None | Rollback already exists; user sees error, not silence | Force API fail |
| P3-05 | PredictionCenter empty/open/upcoming already OK — keep fallbacks | P2 | V | `PredictionCenter.tsx` | — | No blank lock-in panel | Empty week stub |
| P3-06 | Do not insert ads between pick controls | P0 | V | `lib/ads.ts`, matchweek page | Unused `matchweek-between-fixtures` key | If ads are added, they sit **between matches**, never inside a card’s selector | Code review + consented QA |

**Phase 3 gate:** pick path still works for guest after terms; comparison teaser never blank; 375 no overflow on fixture cards.

---

## Phase 4 — Pundit discovery and receipts

| ID | Item | Pri | Type | Likely files | Risk / deps | Acceptance | Verify |
|---|---|---|---|---|---|---|---|
| P4-01 | Follow in onboarding / first-run, not only `/pundits` | P0 | V | `HomeWelcomePanel` or post-terms sheet, `PunditsDirectory.tsx`, `MeHub.tsx` | Do not block predict behind follow | User can follow ≥1 desk without hunting More menu | First-run flow |
| P4-02 | Receipt → Studio from history **and** settled matchweek | P0 | V | `predictions/history/page.tsx`, `PredictionReceiptCard.tsx`, `MatchCard.tsx`, `MatchweekBoard.tsx` | Receipts API exists | FT match with a receipt offers Open in Studio; history link still works | Settled pick |
| P4-03 | Distinguish celebration “share” from settled receipt | P1 | V | `PredictionCelebration.tsx` | None | Copy doesn’t call a reaction card a settled receipt | Lock a pick |
| P4-04 | Surface `storyCandidates` on receipt UI | P1 | Mix | `receipt-story.ts`, history card, Studio picker | Classifier already on backend | Types shown; none invented | Receipt with vs-pundit |
| P4-05 | World Cup leftover labels in pundit directory | P1 | D | directory UI + pundit source strings / ingest | May already be in flight on hotfix branch | No “World Cup” outlet chrome on PL product surfaces | `/pundits` scan |
| P4-06 | Loading state for pundits directory (not plain “Loading…”) | P2 | V | `PunditsDirectory.tsx` | None | Skeleton cards | Throttle network |

**Phase 4 gate:** follow + compare + receipt + Studio deep link is a continuous path. No fabricated quotes.

---

## Phase 5 — Studio creator workspace

Use `prompts/STUDIO-PHASE-PROMPT.md`.

| ID | Item | Pri | Type | Likely files | Risk / deps | Acceptance | Verify |
|---|---|---|---|---|---|---|---|
| P5-01 | Desktop: story rail + workspace + preview. Mobile: stepper/sheets | P0 | V | `StudioPage.tsx`, `StudioStoryWorkspace.tsx`, `studio/page.tsx` | Do not shrink 3 columns on 375 | 1440 shows rail; 375 is stepped, not icon-only mystery tabs | 375 + 1440 |
| P5-02 | Add **perspective** step | P1 | Mix | `studio-pack.ts`, pack POST body if already optional | Prefer UI-only if backend ignores unknown fields; otherwise additive API | User chooses me vs pundit / my take / pundit receipt / match / league | Generate pack |
| P5-03 | Richer story inbox (Aura, league, season-call) only when data exists | P1 | Mix | `StudioStoryService` (backend), `useStudioStories.ts` | No invented stories | New rails appear only with real candidates; otherwise omitted, not zero-filled | Empty vs populated user |
| P5-04 | Quarantine Script tab (legacy generators) so Stories is the product | P1 | V | `StudioPage.tsx`, `PunditScriptGenerator.tsx` | Keep generators reachable without looking like the home of Studio | Default tab Stories; Script clearly “legacy export / persona” | Tab QA |
| P5-05 | Show occurredAt / why-it-matters / freshness on story cards | P1 | V | `StudioStoryWorkspace.tsx` | Fields already on types | Cards answer source, context, why, freshness | Studio |
| P5-06 | Granular export already exists — add “return to Banter / share later” CTA | P2 | V | workspace footer | No fake share counts | After pack: copy actions + next-step links | Generate pack |
| P5-07 | Show remaining generations if API returns them | P2 | V | `useStudioPack.ts` | Field already typed | Quota visible; no fake number | Pack response |

**Phase 5 gate:** Studio never starts blank; sourced facts vs AI copy remain labelled; mobile usable; tests/build green.

---

## Phase 6 — Banter feed and retention

| ID | Item | Pri | Type | Likely files | Risk / deps | Acceptance | Verify |
|---|---|---|---|---|---|---|---|
| P6-01 | Discriminated feed renderer registry | P0 | Mix | `FeedItem.tsx`, `FeedList.tsx`, `lib/types.ts` `FeedItemType`, backend `FeedEndpoints` if new types | Additive types only; map unknown → safe fallback card | Mix of card anatomies; no ten identical shells | Feed with mixed types; empty/error still designed |
| P6-02 | Target cards: pundit receipt, gif reaction, user vs pundit, community receipt, trending, match event, exact-score, studio-ready | P1 | Mix | new card components under `components/feed/` | Render only when API provides; **no client fabrication** | Each type answers what was said / what happened / next action (Studio or pick) | Live feed + stub stories |
| P6-03 | Stop default Unsplash / false “Just now” | P0 | V | `feed-media.ts`, `feed.ts` | Local reaction SVGs OK if labelled decorative | Missing media → designed placeholder, not a stock stadium. Missing time → hidden or “time unknown” | Feed item without media/date |
| P6-04 | Feed freshness + pagination already infinite — add stale/refresh affordance | P1 | V | `FeedList.tsx`, `useFeed.ts` | `staleTime: 30_000` | Manual refresh; no unpausable auto-marquee | Feed |
| P6-05 | Studio CTA on receipt-like feed cards | P1 | V | feed cards | Only if item has receipt/story id | CTA present when id exists | Click-through |
| P6-06 | Banter as a real destination if hash UX stays weak | P2 | V | `app` route or keep hash after P2-07 | Avoid extra empty page | Either `/`# works or `/banter` reuses `BanterFeedPanel` | Nav |

**Phase 6 gate:** no fake likes/views; mixed cards; empty/error; reduced-motion safe.

---

## Phase 7 — Polish

| ID | Item | Pri | Type | Likely files | Risk / deps | Acceptance | Verify |
|---|---|---|---|---|---|---|---|
| P7-01 | 375/390 overflow, header icon cluster, quick-nav clip | P0 | V | `AppShell.tsx`, `HomeQuickNav.tsx`, `globals.css` | None | No horizontal scroll; Season Calls reachable | Playwright `public-pages.spec.ts` |
| P7-02 | Keyboard: More/Account menus, pick buttons, terms dialog | P1 | V | `AppShell.tsx`, `pick-btn`, Dialog | None | Visible focus; Escape closes menus | Keyboard pass |
| P7-03 | Reduced-motion sweep beyond welcome | P1 | V | celebration, `motionConfig.ts`, `globals.css` | Keep lock feedback without bounce | No perpetual animation | `prefers-reduced-motion` |
| P7-04 | `next/image` for feed/studio media where hosts allow | P2 | V | `FeedMedia.tsx`, next.config remotePatterns | Giphy/etc may stay `<img>` | Less CLS; no broken remote config | Lighthouse spot |
| P7-05 | Remove unused `@radix-ui/*` if Base UI fully replaced them | P2 | V | `package.json` | Confirm no imports | Smaller install | grep + install |
| P7-06 | Theme: decide paper-light vs v2 night shell | P2 | V | `globals.css`, `ThemeProvider` | **Do not invert by default** | Documented decision; green remains accent | Design review |
| P7-07 | Turnstile recovery copy after failed challenge | P1 | V | `TurnstileWidget.tsx`, `TurnstileProvider.tsx` | No bypass | User can retry; no redirect loop | Fail challenge |
| P7-08 | SEO metadata already present — keep; avoid hydrating decorative sections | P2 | V | `layout.tsx`, home | None | No new client wrappers for static chrome | bundle/spot |

**Phase 7 gate:** acceptance checklist in `checklists/UI-ACCEPTANCE.md` is all pass or explicitly waived. Visual regression vs `plans/ui-audit-baselines/`.

---

## Suggested implement order (after review)

1. Phase 1 P0s (terms browse, zeros, HTML leak, comparison null)  
2. Phase 2 anonymous homepage  
3. Phase 3 pick/lock/receipt payoff  
4. Phase 4 follow + receipt handoff  
5. Phase 5 Studio workspace  
6. Phase 6 typed feed  
7. Phase 7 QA polish  

---

## Explicitly out of backlog

- Rebuilding Next.js / moving off App Router  
- New UI framework or extra animation library  
- Weakening GDPR ads opt-in or Turnstile  
- Fake pundit quotes, fake Aura, fake feed engagement  
- Demoting Studio or making `/table` primary nav again  
- Sports provider / scoring pipeline changes (owned by `plans/balltakes-remediation/`)

---

## Tests available

- `frontend`: `npm run lint`, `npx tsc --noEmit`, `npm test` (vitest), `npm run test:e2e` (Playwright viewports), `npm run build`  
- Recapture baselines: `node plans/ui-audit-baselines/capture.mjs` from a context that can resolve `frontend` Playwright  

STOP. Wait for review before Phase 1.
