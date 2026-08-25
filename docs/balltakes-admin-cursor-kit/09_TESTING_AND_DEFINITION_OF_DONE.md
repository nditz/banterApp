# Testing & Definition of Done

## Authentication

- [ ] email/password registration works through Supabase;
- [ ] login works;
- [ ] invalid credentials fail safely;
- [ ] password reset works;
- [ ] session restoration works;
- [ ] logout works;
- [ ] Google OAuth works if enabled;
- [ ] guest/recovery-key flow still works;
- [ ] guest-to-account claim preserves existing BallTakes data;
- [ ] repeated claim is idempotent.

## Admin authorization

- [ ] anonymous request to admin API denied;
- [ ] authenticated normal user denied;
- [ ] admin allowed;
- [ ] client-side role tampering does not grant access;
- [ ] admin role source cannot be modified by normal user;
- [ ] frontend does not receive Supabase secret/service-role key;
- [ ] privileged operations create audit entries.

## User management

- [ ] admin user list paginated;
- [ ] search works safely;
- [ ] profile/identity mapping correct;
- [ ] role assignment/removal works;
- [ ] dangerous account action requires confirmation;
- [ ] deletion workflow handles linked app/storage data intentionally.

## Analytics/privacy

- [ ] event catalog contains only approved events;
- [ ] recovery key never appears in analytics;
- [ ] JWT/access token never appears in analytics;
- [ ] full IP is not used as persistent analytics identity;
- [ ] optional analytics obeys consent/configuration policy;
- [ ] Reject is as accessible as Accept;
- [ ] no optional categories preselected;
- [ ] continued browsing does not equal consent;
- [ ] user can change/withdraw preference;
- [ ] refusal does not block normal application usage;
- [ ] admin metrics work with users who refuse optional analytics;
- [ ] account-level operational data is separated from optional tracking.

## Analytics admin

- [ ] overview KPIs render;
- [ ] date filters work;
- [ ] empty state handled;
- [ ] user metrics correct;
- [ ] prediction metrics correct;
- [ ] pundit metrics correct;
- [ ] AI/content metrics correct where feature exists;
- [ ] large raw event datasets are not needlessly returned to browser.

## Background jobs

- [ ] jobs are sourced from server allowlist/registry;
- [ ] admin can view status;
- [ ] admin can view history;
- [ ] admin can trigger approved job;
- [ ] normal user cannot trigger job;
- [ ] arbitrary job names/methods rejected;
- [ ] duplicate/concurrent execution protected where needed;
- [ ] trigger creates audit event;
- [ ] failure is visible without leaking secrets;
- [ ] retry is idempotent where implemented.

## Caching

- [ ] cache candidate list documented;
- [ ] TTL documented per cache group;
- [ ] invalidation documented per group;
- [ ] cache hit tested;
- [ ] cache miss tested;
- [ ] expiry tested;
- [ ] source-of-truth fallback tested;
- [ ] cache outage does not break authorization;
- [ ] no cross-user cache leakage;
- [ ] sync jobs invalidate relevant caches;
- [ ] admin invalidation is allowlisted and audited.

## Security

- [ ] Supabase service role server-only;
- [ ] admin endpoints rate-limited appropriately;
- [ ] SQL/injection validation present through established patterns;
- [ ] destructive endpoints protected;
- [ ] logs do not contain secrets/tokens;
- [ ] analytics does not contain secrets/tokens;
- [ ] CORS/CSRF behavior reviewed for deployment architecture;
- [ ] dependency vulnerabilities reviewed;
- [ ] production error responses sanitized.

## Build quality

Run every available repository check:

- [ ] backend build;
- [ ] backend tests;
- [ ] frontend build;
- [ ] lint;
- [ ] type check;
- [ ] database migration validation;
- [ ] production build;
- [ ] deployment env validation.

## End-to-end definition of done

The feature is complete when this journey works:

1. A visitor can continue using BallTakes anonymously.
2. A visitor can register/login through Supabase.
3. Existing guest data can be claimed by the permanent account.
4. A normal user cannot access `/admin` or admin APIs.
5. An admin can open the admin dashboard.
6. Admin can see user/product metrics.
7. Admin can inspect registered users.
8. Admin can see critical background jobs and when they last ran.
9. Admin can manually trigger an approved job and observe its result.
10. Frequently reused slow-changing football data is cached.
11. Cache invalidates correctly when underlying data changes.
12. Admin can invalidate an approved cache group.
13. Optional analytics/tracking respects the configured consent policy.
14. Privacy-sensitive values are excluded from analytics/logging.
15. All privileged actions are audited.
16. Build/tests/lint/type checks pass.
