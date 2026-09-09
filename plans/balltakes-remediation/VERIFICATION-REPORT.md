# Verification Report

**Date:** 2026-09-09  
**Phase under test:** Phase 2 — Product Cleanup  
**Method:** Implement-phase checks from `cursor/IMPLEMENT-PHASE-PROMPT.md`. Local Vitest, eslint, `tsc --noEmit`, Next.js production build, and SSR probes against `next start` on port 3010.  
**Browser:** No interactive click-through. Public HTML from the local production server was inspected.

Phase 1 football/integrity remains closed. This run implemented Phase 2 only.

---

## Gate

| Question | Result |
|---|---|
| All current-phase P0/P1 passing? | **Yes** — Phase 2 items are copy/IA/docs; no P0 data regressions introduced |
| Mark Phase 2 complete? | **Yes** (nav, Season Calls, welcome, WC archive, seed, orphan UI, sitemap, Studio copy) |
| Start Phase 3? | **Not in this run** — start only in a new deliberate task |

---

## Local verification (this run)

| Check | Result |
|---|---|
| Frontend Vitest | **31 passed** (0 failed) |
| ESLint | **clean** (`--max-warnings 0`) |
| `tsc --noEmit` | **clean** |
| Next.js production build | **succeeded**; routes include `/me` and `/studio` |
| SSR `/` | Start here; Lock a pick; Watch the feed; Season Calls; no Quick tour |
| SSR `/awards` | h1 **Season Calls** (not Season awards) |
| SSR `/studio` | no “fictional pundit” / “pundit desk” |
| `/sitemap.xml` | `/studio` priority **0.9**; `/table` **0.6** |
| `/brackets` | **308** redirect preserved |

---

## Phase 2 backlog vs evidence

| ID | Item | Status |
|---|---|---|
| P2-01 | Navigation IA | **Done** — tests lock Predict/Banter/Studio/Leagues/Me; Table not in mobile primary |
| P2-02 | Awards → Season Calls | **Done** — copy/nav; `/awards` URL kept |
| P2-03 | Welcome tour | **Done** — 3 slides; unit test |
| P2-04 | WC docs/pack | **Done** — `docs/_archive/world-cup/` |
| P2-05 | seed.sql | **Done** — do-not-use notice |
| P2-06 | Orphan prediction UI | **Done** — selectors/hook removed |
| P2-07 | Studio sitemap | **Done** — 0.9 |
| P2-08 | Studio pundit copy | **Done** — sourced pundit predictions |

---

## Failures found this loop

### R3-style click-through — **open (P2)**

Walk `/`, `/matchweek`, `/studio`, `/awards`, `/me` at 1440 and 375: bottom nav order, More menu, Season Calls, welcome CTAs. Not a Phase 2 gate.

No P0/P1 failures in this phase.

---

## Acceptance criteria (Phase 2 slice)

From `16-ACCEPTANCE-CRITERIA.md`:

| Criterion | Now |
|---|---|
| Mobile navigation is coherent | **Pass** (config + SSR) — Predict / Banter / Studio / Leagues / Me |
| Studio is central | **Pass** — center of five on mobile; desktop primary; sitemap 0.9 |
| No stale World Cup UI/copy in PL flows | **Pass** for nav/welcome/seed/docs archive. `/brackets` redirect kept |
| Tests for changed critical paths | **Pass** |
| Implementation log updated | Yes |
| Existing architecture reused | Yes — AppShell, TournamentBonusBoard, StudioPage extended |

Follow pundits, receipts, Studio packs, banter timeline product mix, CMP: later phases.

---

## Next action

1. Phase 2 product cleanup is **closed**.
2. Optional: click-verify the new nav at 1440 / 375.
3. Start Phase 3 (Pundits & Comparison) only in a **new deliberate task**.
4. Do not rename `TournamentBonus*` APIs. Do not remove Studio. Do not re-seed World Cup fixtures.
