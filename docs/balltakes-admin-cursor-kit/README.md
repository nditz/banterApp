# BallTakes Admin, Analytics, Jobs, Caching & Supabase Cursor Kit

This kit is intended to be placed in the BallTakes repository and used with Cursor Agent/Plan mode.

## Scope

This package adds a secure admin capability to the existing BallTakes Premier League-first application without changing the product direction established in the earlier BallTakes evolution plan.

It covers:

- admin-only application area;
- privacy-aware product analytics and consent management;
- admin analytics dashboards;
- background-job observability and manual execution;
- caching for slow-changing/read-heavy data;
- Supabase Auth for registration, login and user administration;
- admin audit logging;
- security, testing and production-readiness requirements.

## Recommended order

1. Read `00_CURSOR_MASTER_PROMPT.md`.
2. Cursor audits the repository before modifying code.
3. Cursor creates its own working state under `docs/balltakes-admin/`.
4. Execute phases in `08_PHASED_IMPLEMENTATION.md`.
5. Use `09_TESTING_AND_DEFINITION_OF_DONE.md` as the shipping gate.

## Important architectural constraints

- Do not create a second password/user authentication system. Supabase Auth is the identity provider.
- Do not expose Supabase secret/service-role keys to the browser.
- Admin authorization must be enforced server-side, not only in frontend navigation.
- Do not turn product analytics into advertising/tracking infrastructure.
- Do not record raw recovery keys, access tokens, passwords, Stripe secrets, OAuth tokens or AI provider secrets in analytics/logs.
- Respect the existing BallTakes anonymous-user/recovery-key flow.
- Preserve the Premier League-first scope and existing prediction/pundit/content functionality.

## Legal note

This kit provides engineering guidance for privacy-aware implementation and is not legal advice. Cookie/privacy notices and retention periods should be reviewed before production launch, particularly if analytics vendors or marketing tools are changed.
