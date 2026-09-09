# Verification Report

**Date:** 2026-09-09 (Phase 3 implementation run; local only)  
**Phase under test:** Phase 3 — Pundits & Comparison  
**Method:** `cursor/IMPLEMENT-PHASE-PROMPT.md` (implement + local verify). Production HTML/API not re-probed for follow APIs — they are not deployed yet.  
**Browser:** No interactive click-through. SSR of `/pundits`, `/studio`, `/matchweek`, `/me` via local `next start` on port 3011.

Phase 1 football/integrity and Phase 2 product cleanup remain closed in production (`#40`). Phase 3 is **implemented locally, not in production**.

---

## Gate

| Question | Result |
|---|---|
| All current-phase P0/P1 passing in production? | **Not yet** — code not deployed; `pundit_follows` migration not applied |
| Mark Phase 3 complete? | **Local yes / production no** |
| Start Phase 4? | **No** — wait for production migrate + deploy + live follow/compare |

---

## Local verification

| Check | Result |
|---|---|
| Backend tests | **367 passed** |
| Frontend Vitest | **35 passed** (nav Pundits overflow, sitemap `/pundits` 0.8, comparison phase) |
| Lint / typecheck | eslint clean on touched files; `tsc --noEmit` clean |
| Production build | Next.js succeeded; `/pundits` listed |
| SSR `/pundits` | **200** — Compare your takes, Follow sourced |
| SSR `/me` | **200** — Follow desks + `/pundits` |
| SSR `/studio` | **200** — Content Studio (Studio not removed) |
| SSR `/matchweek` | **200** — comparison strip is client-fetched after API |

---

## Phase 3 backlog vs evidence

| ID | Item | Local | Production |
|---|---|---|---|
| P3-01 | Follow model | Pass (unit tests: follow Source, reject Persona, idempotent, unfollow) | Pending migrate |
| P3-02 | Follow UX | Pass (page + overflow + Me; feed/Studio filter when follows exist) | Pending deploy |
| P3-03 | Matchweek comparison | Pass (unit: matchIds without user pick, wasCorrect after FT, follow filter) | Pending deploy |
| P3-04 | Attribution | Pass (Source URL + resolver note on directory/comparison; no invented quotes) | Pending deploy |
| P3-05 | Ingest quality | Pass (unreviewed hidden; approve writes `PunditPrediction`; health counts) | Pending deploy |

---

## Failures / remaining

### Production migrate + deploy — **open, blocks Phase 3 gate**

- Apply `20260909195409_AddPunditFollows` on Postgres, deploy API + frontend.
- Then verify: `POST /api/pundits/{id}/follow`, `/pundits` Follow button, matchweek you-vs-pundits strip, Studio vs-pundits follow filter.

### Comparison empty when no match-linked Source picks — **data, not a code hide**

- If current MW has no reviewed `PunditPrediction` rows, cards show an explicit empty line plus Follow pundits. Do not treat that as a silent failure of fixtures.

### Comparison API errors still collapse to empty — **open, not a Phase 3 P0**

- `useStudio` catches `ApiError` and returns an empty DTO (pre-existing Studio behavior).

### Guest Terms overlay — **open, pre-existing**

- More → Pundits can be inert until terms + Turnstile. Same as Phase 2.

---

## Acceptance criteria (Phase 3 slice)

| Criterion | Local now |
|---|---|
| Users can follow pundits or equivalent is exposed | **Pass** — `/pundits`, overflow, Me |
| User vs pundit comparison visible | **Pass** — matchweek, homepage picks, Studio |
| Source attribution preserved | **Pass** |
| Tests for changed critical paths | **Pass** |
| Implementation log updated | Yes |
| Existing architecture reused | Yes — no second pundit table; Studio kept |
| Studio not removed | Yes |
| No fabricated quotes | Yes |

Out of scope: receipts, Studio packs, CMP, Aura persistence.

---

## Next action

1. Apply `pundit_follows` migration on production Postgres.
2. Deploy API then frontend.
3. Re-run `cursor/VERIFY-PROMPT.md` against `balltakes.com` (follow, matchweek strip, Studio filter, health prediction counts).
4. Only then start Phase 4 (Receipt Engine).
