# Phased Implementation Plan

## Phase 0 — Repository Audit

Deliverables:

- `CURRENT_STATE.md`;
- authentication map;
- admin/security map;
- analytics inventory;
- job inventory;
- cache inventory;
- database impact assessment;
- implementation plan adjusted to actual repository.

No large feature rewrite yet.

Acceptance criterion:

> Cursor can explain exactly how identity, guest sessions, jobs, data sync and frontend routing currently work.

---

## Phase 1 — Supabase Auth Foundation

Implement/complete:

- registration;
- login;
- logout;
- session restoration;
- password reset;
- email verification if configured;
- Google OAuth if part of current setup;
- application profile mapping;
- secure server JWT validation.

Preserve anonymous/recovery-key behavior.

Acceptance criterion:

> Permanent users authenticate through Supabase and BallTakes stores no local passwords.

---

## Phase 2 — Admin Authorization

Implement:

- Admin application role;
- authoritative role assignment;
- backend policy/guard;
- frontend admin route guard;
- first-admin bootstrap documentation;
- admin audit logging foundation.

Acceptance criterion:

> Normal and anonymous users cannot retrieve any admin data even by calling APIs manually.

---

## Phase 3 — Admin Shell + User Management

Implement:

- `/admin` layout/navigation;
- overview placeholder backed by real minimal metrics;
- user list via trusted server-side Supabase Admin API;
- user detail;
- application profile link;
- safe role management;
- selected account-management operations;
- audit logging.

Acceptance criterion:

> Admin can securely inspect/manage BallTakes identities without exposing Supabase service credentials.

---

## Phase 4 — Privacy & Consent Foundation

Implement:

- privacy/consent categories;
- cookie/consent UI;
- Accept/Reject parity;
- analytics initialization guard;
- preference withdrawal/reopen;
- consent versioning;
- event catalog.

Acceptance criterion:

> Optional tracking does not activate contrary to configured consent policy, and refusing tracking does not break BallTakes.

---

## Phase 5 — Product Analytics

Instrument core existing flows:

- guest session;
- registration/login;
- guest claim;
- predictions;
- matchweek completion;
- private leagues;
- pundit interactions;
- AI content generation/export.

Build aggregate analytics backend queries/services.

Acceptance criterion:

> Admin can answer who uses the product, what features are used, and whether users return, without inspecting sensitive raw user content.

---

## Phase 6 — Analytics Admin Pages

Build:

- overview KPIs;
- user analytics;
- prediction analytics;
- pundit analytics;
- AI/content analytics.

Use charts only where they improve interpretation.

Acceptance criterion:

> Admin dashboard provides useful date-filtered product insight and handles empty/partial data correctly.

---

## Phase 7 — Background Job Operations

Implement:

- server-side admin job registry;
- execution-history integration;
- job list/status page;
- manual trigger for allowlisted jobs;
- safe retry where appropriate;
- concurrency/idempotency controls;
- audit logging.

Acceptance criterion:

> Admin can see when critical sync/scoring jobs last ran, inspect failures and safely trigger approved jobs.

---

## Phase 8 — Caching

Audit expensive/read-heavy calls first.

Implement caching for selected:

- league/team/player metadata;
- fixtures;
- standings;
- public leaderboards;
- pundit profiles/leaderboard;
- news/feed aggregates;
- homepage aggregate responses.

Add invalidation on sync/update events.

Acceptance criterion:

> Repeated public reads avoid unnecessary provider/database work while updates become visible within documented freshness bounds.

---

## Phase 9 — Cache Admin Operations

Implement:

- cache group status/freshness;
- safe invalidate;
- optional safe refresh;
- operational logging/audit.

Acceptance criterion:

> Admin can recover from stale cache without direct infrastructure access.

---

## Phase 10 — Privacy/User Rights Operations

Ensure architecture supports:

- account data export;
- account deletion/anonymization workflow;
- consent-history/update behavior;
- data retention cleanup jobs;
- admin ability to respond to user-account requests without querying secrets manually.

Acceptance criterion:

> User data lifecycle is documented and technically supportable.

---

## Phase 11 — Production Hardening

Perform:

- authorization tests;
- client-bundle secret inspection;
- rate limiting;
- admin audit review;
- analytics payload review;
- cache leakage tests;
- job trigger abuse tests;
- Supabase token validation tests;
- consent tests;
- build/lint/type checking;
- mobile/desktop admin usability;
- migration validation.

Acceptance criterion:

> `09_TESTING_AND_DEFINITION_OF_DONE.md` passes.
