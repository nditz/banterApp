# Cursor Master Prompt — BallTakes Admin & Platform Operations Upgrade

You are acting as the principal engineer responsible for extending the existing BallTakes repository.

The application is already being evolved from a World Cup prediction application into a Premier League-first football prediction, pundit comparison and AI-content platform.

This task adds the operational and account-management layer required for production use.

## Desired outcome

Implement a secure admin panel and supporting backend capabilities for:

1. user/product analytics;
2. GDPR/privacy-aware data collection;
3. viewing usage metrics in admin dashboards;
4. background job management and manual triggering;
5. caching of read-heavy and slow-changing datasets;
6. Supabase Auth-based registration/login/user management;
7. admin audit trails and operational visibility.

Do not rebuild existing working features unnecessarily.

## First action: audit the repository

Before changing code, determine:

- frontend framework and routing;
- backend architecture and API projects;
- Supabase integration status;
- current authentication flow;
- anonymous/recovery-key implementation;
- current user/profile tables;
- admin functionality already present;
- background job framework (e.g. Hangfire, scheduled jobs, cron, queues);
- existing caching libraries/configuration;
- analytics/tracking code already installed;
- logging and observability;
- database/provider;
- deployment architecture;
- current environment variables;
- existing API authorization conventions.

Search for terms including:

`admin`, `role`, `supabase`, `auth`, `service_role`, `anon`, `recovery`, `hangfire`, `background`, `job`, `cache`, `redis`, `memorycache`, `analytics`, `posthog`, `plausible`, `ga4`, `cookie`, `consent`, `telemetry`, `audit`.

Document findings before implementation.

## Create project working documents

Create:

`docs/balltakes-admin/MASTER_PLAN.md`

`docs/balltakes-admin/CURRENT_STATE.md`

`docs/balltakes-admin/SECURITY_MODEL.md`

`docs/balltakes-admin/ANALYTICS_MODEL.md`

`docs/balltakes-admin/JOBS_MODEL.md`

`docs/balltakes-admin/CACHING_MODEL.md`

`docs/balltakes-admin/SUPABASE_AUTH.md`

`docs/balltakes-admin/DECISIONS.md`

`docs/balltakes-admin/PROGRESS.md`

`docs/balltakes-admin/TECH_DEBT.md`

Maintain these throughout implementation.

## Autonomous agent rule

Only stop for human input when:

- a credential or external account configuration is required;
- a destructive migration cannot be made safe;
- an external service must be configured manually;
- multiple incompatible choices genuinely cannot be inferred.

Otherwise:

1. inspect current implementation;
2. choose the least disruptive production-safe design;
3. document the decision;
4. implement;
5. test;
6. review against acceptance criteria;
7. continue.

## Self-referencing loop

At the start and end of every implementation phase:

1. read `MASTER_PLAN.md`;
2. read `PROGRESS.md`;
3. inspect current repository state;
4. implement the next incomplete tasks;
5. run tests/build/lint/type checking;
6. review security/privacy implications;
7. fix reasonable issues automatically;
8. update `DECISIONS.md` where architecture changed;
9. update `PROGRESS.md`;
10. continue to the next unblocked phase.

Never mark work complete simply because code was written.

## Key design principles

### Authentication

Supabase Auth is the identity provider.

Do not implement local password storage.

Use Supabase-supported login flows already appropriate for the application, initially including email/password and Google OAuth if already planned.

Application/domain profile data may exist in a public application table referencing the Supabase Auth user ID.

### Admin authorization

Admin authorization must be enforced at several layers:

- server/API authorization;
- Supabase/RLS authorization where directly accessing Supabase data;
- frontend route guard for UX only;
- admin API/service checks;
- audit logging for privileged changes.

Never rely solely on hiding admin links.

Prefer a role/permission claim such as `user_role = admin`, backed by a server-managed role table or equivalent existing RBAC implementation.

### Privacy

Collect only data that has a clear product/operational purpose.

Use aggregate or pseudonymous data wherever possible.

Do not introduce advertising trackers in this task.

Do not store raw IP addresses indefinitely.

Never place optional tracking scripts before required consent.

Analytics refusal must not prevent normal use of BallTakes.

### Analytics

Focus on product analytics such as:

- acquisition;
- activation;
- prediction activity;
- retention;
- account conversion;
- league participation;
- pundit engagement;
- AI content usage;
- subscriptions/entitlements later;
- errors/jobs/operational health.

Do not collect sensitive/free-form payloads simply because they are available.

### Background jobs

Admin can inspect and trigger approved jobs through backend endpoints/services.

Never allow the frontend to execute arbitrary code, arbitrary job names or arbitrary method invocation.

Use a server-side allowlist/registry of admin-triggerable jobs.

### Caching

Use caching only where stale data is acceptable for a defined period.

Every cache entry must have an explicit invalidation/expiration strategy.

Do not cache user authorization decisions for unsafe periods.

## Start

Begin with repository audit and documentation only.

Then execute the phases in `08_PHASED_IMPLEMENTATION.md`.
