# Ball Takes UI Audit Results (v2)

**Date:** 2026-09-10  
**Scope:** Phase 0 — audit only. No production UI code was changed.  
**Plan:** `docs/balltakes-ui-liveliness-cursor-pack/` (v2 liveliness pack). This **replaces** the previous UI-liveliness plan when they conflict. It does **not** replace the football-integrity work in `plans/balltakes-remediation/`.  
**Skills / rules loaded:** `.cursor/skills/balltakes-frontend-polish/SKILL.md`, `.cursor/rules/balltakes-ui.mdc`, `.cursor/rules/nextjs-ui-quality.mdc`.  
**Live baselines:** `plans/ui-audit-baselines/` (anonymous visit to https://balltakes.com at 375×812 and 1440×900). First-visit captures include the terms gate; product-chrome captures hide the overlay for inspection only.

---

## Executive summary

The product **loop exists in code** and Studio is already a primary nav destination. Live football data on 10 Sep 2026 is real (Matchweek 4 fixtures, Premier League table, pundit directory with source links). The UI still **explains before it shows**, puts **personal zeros and a blocking terms modal** in front of entertainment, and treats Banter/Studio as **one-card / one-column tools** rather than a mixed timeline and a creator workspace.

| Loop step | UI status |
|---|---|
| Discover banter | Partial — home feed exists, one mega-card, below a tour + stats |
| Follow pundits | Exists on `/pundits` only; not in onboarding or homepage |
| Predict | Strong — `MatchCard` + prediction modes on home and `/matchweek` |
| Compare user vs pundit | Present on fixture cards + Studio tabs; silent if comparison API fails |
| Receipt | History page + Studio story picker; weak handoff from lock/FT |
| Banter | Home `#banter-feed` only; nav Banter is a hash, not a route |
| Studio | Stories → format/tone → pack → export exists; looks like a tabbed info page |
| Export | Pack copy/download on Stories tab; Script tab still feels like a prompt tool |
| Share / return | Missing as a designed loop |

**Do not rebuild.** Extend existing App Router surfaces, `EmptyState`/`ErrorState`/`AdSlot`, `MatchCard`, `StudioStoryWorkspace`, and the feed renderer.

---

## Related plan files used

Kickoff file: `docs/balltakes-ui-liveliness-cursor-pack/prompts/MASTER-KICKOFF-PROMPT.md`

Same pack (authoritative v2 docs):

| File | Role |
|---|---|
| `00-READ-ME-FIRST.md` | Non-negotiables |
| `01-LIVE-SITE-UI-AUDIT.md` | Known live gaps |
| `02-PRODUCT-UI-DIRECTION.md` | Audience-state layouts, nav target, brand |
| `03-HOMEPAGE-REDESIGN.md` | Anonymous/returning home order |
| `04-STUDIO-UI-PLAN.md` | Creator workspace |
| `05-BANTER-TIMELINE-COMPONENTS.md` | Typed feed cards |
| `06-MATCHWEEK-AND-DATA-STATES.md` | Fixture + five-state rule |
| `07-CONSENT-ADS-ROBOT-UX.md` | Terms vs ads vs Turnstile |
| `08-NEXTJS-FRONTEND-IMPLEMENTATION.md` | Stack-first implementation |
| `09-IMPLEMENTATION-PHASES.md` | Phase order for the backlog |
| `checklists/UI-ACCEPTANCE.md` | Gate for later phases |
| `checklists/PAGE-REVIEW.md` | Per-route template (used below) |
| `prompts/IMPLEMENT-PHASE-PROMPT.md` | Next run, after review |
| `prompts/VERIFY-UI-PROMPT.md` | Per-phase verify |
| `prompts/HOMEPAGE-PHASE-PROMPT.md` | Phase 2 specialist |
| `prompts/STUDIO-PHASE-PROMPT.md` | Phase 5 specialist |
| `plans/UI-IMPLEMENTATION-LOG-TEMPLATE.md` | Log after each implement run |

Also present (do not treat as a second source of truth if they diverge):

- `docs/balltakes-ui-liveliness-cursor-pack-v2/` — same pack plus bundled `.cursor` skill/rules
- `docs/balltakes-ui-liveliness-cursor-pack-v2.zip`
- Repo skill already merged: `.cursor/skills/balltakes-frontend-polish/`
- Repo rules already merged: `.cursor/rules/balltakes-ui.mdc`, `.cursor/rules/nextjs-ui-quality.mdc`

---

## 1. Stack (from `frontend/package.json`, not assumed)

| Layer | Actual |
|---|---|
| Framework | Next.js **16.3.4** App Router (`frontend/src/app`) |
| UI runtime | React **19.2.8** |
| Request interception | `frontend/src/proxy.ts` (Next 16; not `middleware.ts`) |
| Styling | Tailwind **v4**, `tw-animate-css`, `frontend/src/app/globals.css` tokens |
| Components | shadcn-style wrappers + **@base-ui/react** (Button, Dialog, Tabs, Input, Badge) |
| Unused in `src` | `@radix-ui/react-dialog`, `@radix-ui/react-slot`, `@radix-ui/react-tabs` |
| Data | TanStack Query **5.102.8**; `frontend/src/lib/api.ts` → `NEXT_PUBLIC_API_URL` or `/api-backend` |
| Auth | `@supabase/ssr` + `@supabase/supabase-js`; guest session + terms |
| Motion | `framer-motion` **12.43.0** in 6 files (welcome, celebration, recovery) |
| Icons | `lucide-react` |
| Theme | `next-themes` (light paper default, dark charcoal) |
| Ads | First-party CMP + AdSense `AdSlot` |
| Bot | Cloudflare Turnstile (`TurnstileProvider` / `TurnstileWidget`) |
| Tests | Vitest, Playwright (`frontend/e2e`, viewports 320/375/390/768/1024/1440) |
| Images | `next/image` remote hosts: flagcdn, dicebear, api-sports, Google avatars, Supabase storage |
| Dead dep | `@prisma/client` / `frontend/prisma` empty shell (known; out of UI scope) |

**Do not add** a new UI kit, animation library, or feed framework. Installed stack is sufficient.

**Brand tension:** v2 direction asks for a black/off-black shell. Live site is **light paper** (`#f4f2ec`) with a black header. Treat a full night-mode inversion as an explicit later decision, not a Phase 1 rewrite.

---

## 2. Routes, layouts, navigation

### Layouts

- Root `frontend/src/app/layout.tsx` — fonts (Barlow / Barlow Condensed / Geist Mono), theme, QueryProvider, `AppShell`, JSON-LD
- `AppShell` wraps all non-admin routes; admin skips consumer chrome
- Auth routes keep header but skip TermsGate, ads, consent banner, bottom nav, footer
- No App Router BFF under `frontend/src/app/api/**`

### Public / session routes

| Path | Page | Goal | Auth |
|---|---|---|---|
| `/` | Home | Discover + first pick + feed | Guest OK |
| `/matchweek` | Predict board | Lock fixtures | Guest OK |
| `/studio` | Studio | Story → pack | Guest OK |
| `/leagues`, `/leagues/join/[code]` | Leagues | Create/join | Guest OK; mutate needs terms |
| `/awards` | Season Calls | Title/top-four/awards | Guest OK; mutate needs terms |
| `/pundits` | Directory | Follow sourced desks | Guest OK |
| `/table` | Standings | Context for calls | Guest OK |
| `/predictions/history` | Receipts | Settled stories | Session/terms |
| `/me` | Me hub | Overflow home (mobile) | Guest OK |
| `/rules`, `/terms`, `/privacy` | Legal | Rules + ad-consent revisit | Public |
| `/auth/login`, `/register`, `/callback`, `/confirm` | Auth | Account | Public |

Redirects: `/brackets` → `/matchweek`; bonus/prediction URLs → `/awards`.

### Navigation (source: `frontend/src/lib/navigation.ts`)

**Desktop primary:** Predict `/matchweek` · Banter `/#banter-feed` · Studio `/studio` · Leagues `/leagues` · Season Calls `/awards`  
**Desktop More:** Pundits · Table · Receipts · Rules  
**Mobile bottom:** Predict · Banter · Studio (center) · Leagues · Me  
**Mobile overflow:** Season Calls · Pundits · Table · Receipts · Rules  

**IA vs v2:** Nav labels match the target. Gaps:

- Banter is a **homepage hash**, not a destination. `isNavHrefActive` treats any `/` as Banter-active (desktop “Banter” is highlighted on the hero).
- Pundits / Receipts are overflow-only, so the differentiator is easy to miss.
- Mobile Season Calls is overflow-only (acceptable if desktop keeps it primary).
- Studio is present and central on mobile, but not visually a creation action beyond being the third tab.

---

## 3. Shared primitives vs duplicates

### Shared and worth extending

| Primitive | Path |
|---|---|
| Button, Card, Panel, Badge, Input, Dialog, Sheet, Tabs, Command | `frontend/src/components/ui/*` |
| Skeleton | `ui/skeleton.tsx` |
| EmptyState, ErrorState | `ui/states.tsx` |
| SectionHeader | `ui/section-header.tsx` (underused) |
| AdSlot | `components/ads/AdSlot.tsx` |
| MatchCard / PredictionButtons / FixtureStatusBadge | `components/prediction/*` |
| TeamFlag | `components/brackets/TeamFlag.tsx` |
| FeedList / FeedItemCard / FeedMedia | `components/feed/*` |
| StudioStoryWorkspace | `components/studio/StudioStoryWorkspace.tsx` |

### Missing vs pack component targets

No dedicated `FreshnessBadge`, `FeedCardShell`, typed feed registry, `ReceiptComparison` shell, `StudioStoryCard` primitive, or `PageContainer` used consistently.

### Duplicates / drift

- Page headers: ad-hoc `page-kicker` + `h1` on matchweek/awards/leagues/table vs `SectionHeader` on receipts
- Season Calls loading/error is plain text, not `EmptyState`/`ErrorState`/`Skeleton`
- Welcome carousel ≈ `ConceptSlider` on `/rules`
- `HomeStatsBar` tiles vs `AuraSummary` / `StudioSummaryBar` zeros
- `PredictionLockBanner` exists and is unused
- Pick chrome: `pick-btn` in `globals.css` vs `Button` variants

---

## 4. Route reviews

Template: `checklists/PAGE-REVIEW.md`. User type unless noted: **anonymous first visit** (live 2026-09-10).

### `/` Home

- **User goal:** Be entertained, then lock a pick, then understand Studio.
- **Primary CTA:** “Lock a pick” → `#predictions`. Secondary: feed, Studio, Join free.
- **What is uniquely Ball Takes:** Fixture card + you-vs-pundits teaser + feed + Studio in one shell.
- **Top issues:**
  1. Terms modal blocks the whole product before any take is visible.
  2. Tour carousel (“Think you know ball? Prove it.”) sits above live content; not the v2 compact product hero.
  3. `HomeStatsBar` shows **Your aura 0** (and auto-joined league counts) before the feed.
  4. Feed is one `FeedItemCard` (live: generic **News** + stadium photo).
  5. No homepage pundit strip or Studio-ready story demo.
- **States:** Predict/feed/table have skeleton/empty/error. Stats bar has **no loading/error** (zeros while pending; Aura errors look like 0). Feed has no freshness badge.
- **Mobile:** Hero + four stat cards + quick nav consume the first viewport; first fixture is below the fold. Bottom nav + header icon cluster is dense. Season Calls pill clips.
- **A11y:** Welcome autoplay (8s) does not stop for `prefers-reduced-motion` (only CSS transition muted).
- **Performance:** Entire home modules are client components; feed/fixtures not SSR’d. Default Unsplash URLs in `feed-media.ts` for news/leaderboard/highlights.
- **Screenshot:** `plans/ui-audit-baselines/home-375.png`, `home-1440.png`, `home-terms-gate-*.png`.

### `/matchweek` Predict

- **User goal:** Lock every open fixture quickly.
- **Primary CTA:** Result / Scoreline / Double on `MatchCard`.
- **Uniquely Ball Takes:** Crests, lock state, spicy pick labels, you-vs-pundits row.
- **Top issues:**
  1. No matchweek progress (X of Y locked).
  2. `MatchPunditComparison` returns `null` when `useStudio` swallows `ApiError`.
  3. Empty pundit row is designed (good) but follow is link-out only.
  4. Unused `PredictionLockBanner`; save failure has no toast.
  5. Header utility icons compete with “JOIN FREE” on 375px.
- **States:** Board implements loading / loaded / empty / stale / error (`datasetStatusFromMatchweek`). Strongest football surface in the app.
- **Screenshot:** `matchweek-375.png`, `matchweek-1440.png` (MW4 Palace–Ipswich, Liverpool–Fulham).

### `/studio`

- **User goal:** Turn a known story into a content pack.
- **Primary CTA:** Pick story → Generate (after format/tone).
- **Uniquely Ball Takes:** Receipt/pundit story inbox + sourced facts in packs.
- **Top issues:**
  1. Opens as a **narrow tabbed page** with dashboard zeros (0 points, — rank, 0 matches), not a workspace.
  2. **Raw HTML in trending story summaries** (`<p><b>…`, `</p></li></ul>`) — looks broken.
  3. Mobile tabs are **icon-only**; easy to misread.
  4. No perspective step; no Aura/league/season-call story rails; no 3-pane desktop / stepper mobile.
  5. Script tab still sits beside Stories as a persona/prompt generator.
- **States:** Story sections have skeletons and designed empties with CTAs (good). Comparison API errors become empty lists. No stale/freshness.
- **Screenshot:** `studio-375.png`, `studio-1440.png`.

### `/pundits`

- **User goal:** Follow desks so comparisons and receipts work.
- **Primary CTA:** Follow on `PunditFollowCard`.
- **Uniquely Ball Takes:** Sourced citations, “we never invent quotes.”
- **Top issues:** Route is overflow-only; all visible desks showed **0 match picks**; cartoon DiceBear avatars; at least one **World Cup** leftover in an outlet label (“BBC Sport World Cup”); no onboarding/home entry.
- **States:** Loading is plain text; empty/error exist. No stale.
- **Screenshot:** `pundits-375.png`.

### `/predictions/history` Receipts

- **User goal:** See settled before/after and open Studio.
- **Primary CTA:** Open in Studio (`?receipt=`).
- **Gaps:** Not in primary nav; lock celebration “Share receipt” is a reaction card, not a settled receipt; no Studio CTA from matchweek after FT; `storyCandidates[]` unused in UI.

### `/leagues`

- **User goal:** Create/join a private league.
- **Top issues:** First screen is create form + **inline Terms of Use** (third copy of terms: modal + Season Calls + here). Empty list states are otherwise solid.
- **Screenshot:** `leagues-375.png`.

### `/awards` Season Calls

- **User goal:** Lock season bonuses.
- **Top issues:** Loading/error are muted text, not shared primitives; unauthenticated view is another terms form. Eligibility zeros are handled better than home Aura zeros.
- **Screenshot:** `season-calls-375.png`.

### `/table`

- **User goal:** Context for title/top-four/drop.
- **Status:** Best five-state implementation. Live table populated (City/Arsenal/Hull/Chelsea…). Commodity surface — correctly demoted in nav.
- **Screenshot:** `table-375.png`.

### `/me`, auth, `/privacy`

- Me hub is a link board + rankings (inherits Aura error→zero).
- Auth is Google + email; **no pundit-follow onboarding**.
- Privacy hosts ad-consent revisit (`AdConsentSettings`) — keep this.

---

## 5. Blank / misleading async surfaces

Forbidden by the pack: blank sections when data is missing.

| Surface | Failure mode |
|---|---|
| `MatchPunditComparison` | `return null` if comparison undefined |
| `useStudio` | `ApiError` → `EMPTY_COMPARISON` (looks like “no takes”) |
| `HomeStatsBar` / `AuraSummary` | Pending/error → **0** |
| `StudioSummaryBar` | Anonymous zeros above the workspace |
| Studio trending | HTML entities/tags rendered as text |
| Season Calls | Weak loading/error chrome |
| Feed missing `publishedAt` | Falls back to “Just now” (`feed.ts`) |
| `AdSlot` | `null` without consent / unfilled — **correct collapse** |
| `PredictionLockBanner` | Dead code |

Strong counterexamples: `MatchweekBoard`, `LeagueTable`, `LeaguesList`, `FeedList`, receipts history, Studio story empty copy.

---

## 6. First-time vs returning users

**Anonymous / first visit (live):**

1. Blocking Terms of Use + Turnstile “Verifying…”
2. How-it-works carousel
3. Personal metric strip (Aura 0; “Your leagues” from auto-join)
4. Then fixtures + feed

v2 wants: compact hero → mixed Live Takes → first pick → pundits → Studio demo. Personal metrics only after activity.

**Returning users:** Same section order. Signed-in welcome only rewrites the first slide title. No unfinished-picks / new-receipts / followed-pundit / Studio-ready home variant.

---

## 7. Pundit → receipt → Studio journey

Implemented:

- Follow: `/pundits` only (`useFollowPundit`)
- Compare: `MatchPunditComparison` on home + matchweek; Studio vs Pundits tab
- Receipts: `/predictions/history` → `Open in Studio`
- Studio stories: latest receipts, you vs pundits, trending, previous projects
- Pack: format + tone + generate + copy/download (`useGenerateStudioPack`)

Missing steps:

- Follow during onboarding / homepage / inline on a fixture
- Feed cards for receipt / user-vs-pundit / Studio-ready
- Matchweek FT → receipt → Studio
- Celebration share ≠ settled receipt
- Deep link does not focus generate/export
- Perspective step; Aura/league/season stories
- After export: share / return to Banter

Attribution is generally honest (source URLs, parody disclaimer, pack provenance). Risks: Script-tab fictional personas next to sourced packs; reaction copy that can be mistaken for a pundit quote; default stock photos.

---

## 8. Studio as creator workspace

`StudioStoryWorkspace` already refuses a blank prompt. Live trending proves the inbox can fill — but with **news HTML and kickoff listings**, not ready-to-create receipts.

Gaps vs `04-STUDIO-UI-PLAN.md`: no perspective; missing story sources; single column `max-w-[820px]`; mobile icon tabs; zeros in the summary bar; Script tab competes with Stories.

---

## 9. Consent, ads, robot UX

**Keep the architecture.** Terms (`TermsGate` / `TermsAcceptPanel` + Turnstile) are separate from advertising (`advertising-consent.ts`, `AdConsentBanner`, `AdSenseLoader`). Ads do not load until grant. `AdSlot` collapses on no consent / unfilled.

UX problems (do not bypass Turnstile/WAF):

- Terms **modal has no close** and covers every consumer route, while copy says it is only required to **save picks**. Align the gate with that promise so anonymous users can see the product.
- Terms repeated inline on Leagues and Season Calls.
- Consent banner and mobile nav both `z-50`.
- Turnstile on first paint stuck on “Verifying…” in baselines; recovery copy is thin.
- Dev-only `"dev-bypass"` when site key missing — do not extend to production UI.
- Unused placement keys: `matchweek-between-fixtures`, `banter-feed`, `table-bottom`. **Do not** insert ads between prediction controls or inside a receipt comparison.

---

## 10. Motion, a11y, performance

- Welcome autoplay ignores reduced-motion for the timer.
- Framer usage is limited and often checks `useReducedMotion` (celebration, slides).
- Desktop More / Account menus: click-outside, weak focus trap vs Sheet.
- Custom `pick-btn` focus vs shared Button rings.
- Home is client-heavy; independent fixtures/feed/aura should fetch concurrently (already separate queries) but still hydrate together.
- Reserve media: feed max-heights exist; default Unsplash is extra weight.
- No autoplay audio (good).

---

## 11. Baseline screenshots

Captured 2026-09-10 against production. Method: Playwright Chromium; terms overlay hidden after a first-visit gate shot so product chrome is visible. Script: `plans/ui-audit-baselines/capture.mjs`.

| Viewport | Files |
|---|---|
| 375×812 | `home`, `home-terms-gate`, `matchweek`, `studio`, `pundits`, `leagues`, `season-calls`, `table` |
| 1440×900 | same set |

Not captured this pass: logged-in returning home, `/predictions/history` with receipts, reduced-motion, ad-consented rails, error states (those need fixture/API stubs in later verify runs).

---

## 12. Acceptance checklist (current)

From `checklists/UI-ACCEPTANCE.md`:

| Criterion | Now |
|---|---|
| Clear primary action above the fold | Fail — terms + tour first |
| Studio is a primary destination | Pass in nav; fail as workspace feel |
| Pundit comparison discoverable | Partial — on fixture cards, not homepage discovery |
| Homepage useful with no history | Partial — fixtures/table yes; zeros + tour no |
| No blank async sections | Fail — null comparison, Aura zeros |
| Distinct loading/empty/stale/error | Partial — table/matchweek/feed yes; stats/awards/studio comparison no |
| No fake production data/metrics | Partial — no fake counts on cards; default Unsplash + “Just now” |
| Mobile 375 no overflow | Playwright e2e exists; live header is cramped |
| Keyboard focus / reduced motion | Fail — welcome autoplay, some menus |
| Ad slots collapse | Pass (code) |
| Consent remains optional | Pass for ads; terms UX over-blocks |
| Existing flows remain functional | Pass — do not regress predict/studio/follow APIs |

---

## Stop

Phase 0 complete. Implementation backlog: `plans/UI-IMPLEMENTATION-BACKLOG.md`.

Do **not** start Phase 1 until this audit and backlog are reviewed.
