# Verification Report

**Date:** 2026-09-09 (Phase 3 production re-scan + Phase 4 local implementation)  
**Phase under test:** Phase 3 (live) then Phase 4 — Receipt Engine (local)  
**Method:** `cursor/COMPLETION-LOOP.md`. Production HTML via `balltakes.com`. API curl from this environment is bot-blocked (`FORBIDDEN`).  
**Browser:** No interactive click-through. SSR of `/pundits` and `/predictions/history` on production (pre-deploy). Local: backend tests, Vitest, `tsc`, eslint, `next build`.

Phase 1 and Phase 2 remain closed in production (`#40`). Phase 3 is **deployed** (`#41`) but two P0/P1 defects were still live. Those are fixed in this workspace. Phase 4 is **implemented locally, not in production**.

---

## Gate

| Question | Result |
|---|---|
| All Phase 3 P0/P1 passing in production? | **Not yet** — directory first-paint empty; guest comparison was 500 before bot-block |
| Mark Phase 3 complete? | **Local yes / production after deploy of this run's comparison + `isPending` fixes** |
| Phase 4 local P0/P1? | **Pass** (tests + build) |
| Mark Phase 4 complete? | **Local yes / production no** |
| Start Phase 5? | **No** — wait for migrate + deploy + live receipts |

---

## Phase 3 production re-scan

| Check | Result |
|---|---|
| `/pundits` | **200** — copy is correct, but SSR still shows “No sourced pundits yet” while `GET /api/pundits` has Source rows. Cause: TanStack Query `isLoading` is false when the query is not fetching (SSR / session wait). **Fixed locally** with `isPending` and an ungated directory query. |
| `GET /api/studio/comparison?matchIds=` | Earlier this run: **500** for guests with neither user nor anonymous id (`Enumerable.Empty().AsQueryable().ToListAsync`). **Fixed locally**. This curl now returns bot `FORBIDDEN`, so the live 500 was not re-probed. |
| `GET /api/pundits/follows` | Previously `[]` for guests (OK). Follow table exists in prod from `#41`. |
| Studio | Still present. Attribution copy unchanged. |

---

## Phase 4 local verification

| Check | Result |
|---|---|
| Backend tests | **379 passed** |
| Frontend Vitest | **37 passed** |
| Lint / typecheck | eslint clean on touched files; `tsc --noEmit` clean |
| Production build | Next.js succeeded; `/predictions/history` listed |
| Production HTML `/predictions/history` | Still old “Prediction History” title — **not deployed** |

---

## Phase 4 backlog vs evidence

| ID | Item | Local | Production |
|---|---|---|---|
| P4-01 | Receipt entity | Pass | Pending migrate |
| P4-02 | Settlement emit | Pass (idempotent + new result version) | Pending API deploy; score-sync already calls rescore |
| P4-03 | History UI | Pass — page is Receipts; cards reused | Pending frontend deploy |
| P4-04 | Story candidates | Pass — classified, no invented quotes | Pending |
| P4-05 | Privacy | Pass — `IsPublic=false`; owner-scoped GET; feed does not include receipt ids | Pending |

---

## Failures / remaining

### Production deploy — **open, blocks Phase 3 + 4 gates**

1. Deploy API with guest comparison fix + `AddPredictionReceipts`.
2. Apply `20260909204154_AddPredictionReceipts` on Postgres.
3. Deploy frontend (`isPending` pundits + Receipts UI).
4. Re-verify: `/pundits` lists Source desks after paint; matchweek comparison does not 500; `/predictions/history` says Receipts; `GET /api/receipts` is session-scoped.

### Comparison empty when no match-linked Source picks — **data, not a code hide**

Unchanged. Cards show an explicit empty line plus Follow pundits.

### Guest Terms overlay — **open, pre-existing**

More → Pundits / Receipts can wait on Turnstile. Same as Phase 2/3.

### Directory quality — **P3, not blocking**

`GET /api/pundits` still includes non-PL names (ingest). Follow UX can ship; do not invent quotes to fill the list.

---

## Acceptance criteria (Phase 4 slice)

| Criterion | Local now |
|---|---|
| Receipts persistent and reusable | **Pass** — `prediction_receipts` + `/api/receipts` |
| History reframed as Receipts | **Pass** |
| Source attribution preserved on vs-pundit receipts | **Pass** — snapshot + source URL |
| Public timeline does not leak private receipts | **Pass** |
| Tests for changed critical paths | **Pass** |
| Studio not removed | Yes |
| No fabricated quotes | Yes |

Out of scope: Studio content packs (Phase 5), CMP, Aura persistence.

---

## Next action

1. Deploy the Phase 3 comparison/`isPending` fixes with Phase 4.
2. Apply `prediction_receipts` + `receipt_story_candidates` migration.
3. Re-run verify against `balltakes.com`.
4. Only then start Phase 5 (Studio Evolution).
