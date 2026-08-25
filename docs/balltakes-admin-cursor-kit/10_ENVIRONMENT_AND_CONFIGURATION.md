# Environment & Configuration Checklist

Cursor must inspect existing environment conventions and extend them rather than creating duplicate configuration systems.

## Supabase

Likely required values, exact names should follow existing repository:

- Supabase project URL;
- public/anon or publishable key where appropriate for client SDK;
- server secret/service-role key for trusted admin operations only;
- JWT issuer/JWKS configuration if backend validates tokens directly;
- OAuth redirect URLs;
- production site URL.

Rules:

- secret/service-role key server only;
- no secret key in `NEXT_PUBLIC_*`;
- use Vercel/server secret storage for production;
- validate required configuration at startup where practical.

## Analytics

If first-party analytics only:

- analytics enabled flag;
- consent policy mode;
- retention configuration;
- event ingestion endpoint/config.

If an external privacy-friendly analytics provider is later selected:

- provider key/site ID;
- region/host;
- consent-required flag;
- server/client initialization separated;
- do not load provider before consent when consent is required.

## Caching

Possible configuration:

- cache provider mode: memory/distributed;
- Redis/distributed cache connection string if needed;
- default TTLs by domain;
- feature flag to disable cache for debugging;
- cache namespace/environment prefix.

Never expose cache credentials to browser.

## Background jobs

- job framework storage/connection;
- scheduler enable flag;
- dashboard/admin exposure rules;
- provider API keys used by sync jobs;
- concurrency settings where required.

## Admin bootstrap

Document a one-time safe procedure using user ID/role assignment.

Never include a permanent hardcoded admin password in source control.

## Privacy

Configuration should allow:

- privacy policy version;
- consent version;
- analytics on/off;
- marketing category on/off (default off/not used);
- configurable retention windows.

## Deployment checklist

Before production:

- production Supabase redirect URLs correct;
- Vercel/client env contains only publishable values;
- backend/server env contains secrets;
- admin role assigned intentionally;
- database migrations applied;
- RLS verified where relevant;
- analytics consent behavior verified on clean browser;
- background scheduler verified;
- cache provider reachable;
- health checks/logging verified.
