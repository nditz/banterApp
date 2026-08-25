# Admin Dashboard

## Information architecture

Create an `/admin` area that follows the existing frontend architecture and BallTakes visual identity while being optimized for operational clarity rather than consumer banter.

Suggested navigation:

- Overview
- Users
- Analytics
- Predictions
- Pundits
- Content / AI Usage
- Background Jobs
- Cache
- Audit Log
- System Health
- Settings (only if actual admin-configurable settings exist)

Do not add empty decorative pages.

## Overview page

Show concise operational KPIs:

- registered users;
- anonymous sessions;
- active users;
- guest-to-account conversion;
- predictions today/current matchweek;
- matchweek completion rate;
- AI generations today/current month;
- AI failure rate;
- background job failures;
- stale/failed data syncs;
- current Premier League data freshness;
- cache status if useful.

Also show:

- latest failed jobs;
- latest privileged admin actions;
- provider/sync warnings;
- current app version/environment.

## Users page

Use Supabase as the source of truth for identity.

Admin user list should support:

- pagination;
- search by email/display name where allowed;
- Supabase user ID;
- created date;
- last sign-in where available;
- provider/login method;
- application profile;
- app role;
- subscription/plan later;
- prediction activity summary;
- status.

Permitted management actions can include, where supported and intentionally implemented:

- invite user;
- resend appropriate auth flow via Supabase-supported mechanisms;
- disable/remove access if architecture supports it;
- delete account through a safe server-side workflow;
- assign/remove application admin role;
- inspect linked application profile.

Dangerous actions require confirmation and audit logging.

Do not expose secrets, password hashes or auth internals.

## Analytics pages

Provide filters:

- date range;
- environment where applicable;
- guest vs registered;
- feature;
- matchweek/season where relevant.

Pages:

### User analytics

- registrations;
- logins;
- active users;
- anonymous sessions;
- guest conversion;
- retention/return rate.

### Prediction analytics

- prediction volume;
- participation per fixture;
- matchweek participation;
- completion funnel;
- private league activity.

### Pundit analytics

- most viewed pundits;
- comparisons;
- source ingestion volume;
- extraction success/failure;
- review backlog.

### AI/content analytics

- generation volume;
- generation type;
- success rate;
- export rate;
- estimated cost;
- usage by free/paid plan later.

## Background Jobs page

Show:

- job key/name;
- description;
- category;
- schedule;
- last start;
- last finish;
- duration;
- status;
- last result summary;
- last error summary;
- next scheduled run if available;
- manual-run eligibility.

Support:

- view execution history;
- view sanitized error details;
- manually trigger approved jobs;
- retry failed execution if safe;
- prevent accidental duplicate concurrent execution where needed.

## Cache page

Show only operationally useful entries/groups.

Examples:

- Premier League standings;
- fixture lists;
- team metadata;
- player metadata;
- pundit profiles;
- news summaries;
- static app configuration.

Support:

- cache group/status;
- age;
- expiry;
- last refresh;
- invalidate specific key/group;
- refresh through the proper underlying service where supported.

Do not expose arbitrary Redis commands to the browser.

## Audit page

Show:

- timestamp;
- admin;
- action;
- target;
- result;
- correlation ID.

Allow filtering but not casual mutation/deletion from UI.

## UX requirements

- responsive at desktop/tablet sizes;
- clear loading/error/empty states;
- no secret values shown;
- destructive actions use explicit confirmation;
- manual job trigger provides immediate accepted/running result and later execution status;
- admin UI never assumes success before backend confirmation.
