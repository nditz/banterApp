# Master Plan — Admin Core

Derived from `docs/balltakes-admin-cursor-kit/` after the Phase 0 audit recorded in
`CURRENT_STATE.md`. The kit assumes a greenfield admin area; the repository already has
a working admin console, so this plan targets only the genuine gaps.

## Scope of the current workstream

Selected scope: **admin core**. Deliver user management, admin audit visibility, the
consent foundation and the first-party analytics pipeline, then re-plan.

| Work item | Kit phase | State |
| --- | --- | --- |
| Working documents | 0 | Done |
| Authorization hardening | 2 | Done |
| Supabase user management | 3 | Done |
| Admin audit log UI | 2 / 3 | Done |
| Privacy and consent foundation | 4 | Done |
| First-party analytics pipeline | 5 | Done |
| Verification | 11 (partial) | Done |

## Deferred

These are intentionally out of scope for this run and require a follow-up plan.

| Work item | Kit phase | Reason |
| --- | --- | --- |
| Analytics admin dashboards | 6 | Needs event data to exist first |
| Caching layer + invalidation | 8 | Independent workstream, no dependency on admin core |
| Cache admin operations | 9 | Depends on the caching layer |
| Privacy / user rights operations | 10 | Account deletion needs a full data-lifecycle design |
| Production hardening sweep | 11 | Final gate before launch |
| Password reset UI | 1 | Missing but unrelated to admin core |
| Guest to account claim | 1 | Missing; needs a dedicated idempotent merge design |
| Durable Hangfire storage | 7 | Recorded in `TECH_DEBT.md` |

## Design constraints carried from the kit

- Supabase Auth is the only identity provider. No local password storage.
- The Supabase service-role key is server-only and never reaches the browser.
- Admin authorization is enforced in the API. Frontend guards are UX only.
- Analytics is first-party, minimal, and gated on consent.
- No advertising or cross-site tracking is introduced.
- Recovery keys, JWTs, passwords, prompts and AI output never enter analytics or logs.
- Every privileged action writes an audit entry.

## Execution order

1. Working documents.
2. Authorization hardening, so later endpoints can declare permissions.
3. User management backend, then frontend.
4. Audit log filters and page.
5. Consent backend, then the banner and gating.
6. Analytics backend, then client instrumentation.
7. Tests, build, lint, bundle-secret check.

Consent precedes analytics deliberately: the ingest endpoint must be able to reject
events from users who have not opted in on the day it ships.
