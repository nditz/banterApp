# Analytics, GDPR & Consent

## Objective

Add useful product analytics while minimizing personal data collection and avoiding unnecessary tracking infrastructure.

This is engineering guidance, not legal advice.

## Privacy strategy

Separate tracking into categories.

### Strictly necessary / operational

Examples:

- authentication/session cookies;
- recovery-key/session continuity mechanism;
- CSRF/security state where applicable;
- rate-limit/security telemetry;
- server health/error logs;
- cookie-consent preference itself.

These exist to deliver or secure the service and should not be mixed with marketing analytics.

### Privacy-friendly product analytics

Track aggregated product usage with minimal identifiers.

Examples:

- page/screen viewed;
- prediction flow started/completed;
- account created;
- guest account claimed;
- private league created/joined;
- pundit comparison opened;
- AI generation initiated/completed/exported;
- matchweek return activity.

For the Netherlands, low-impact analytical cookies may in some circumstances qualify for a consent exception where privacy impact is minimal. However, build BallTakes so analytics can be gated by consent/configuration rather than assuming every analytics configuration is exempt.

### Tracking/marketing

Not part of this implementation.

Do not install advertising pixels, cross-site tracking or behavioral advertising tools.

If introduced in future, they require a separate privacy review and appropriate consent before activation.

## Consent manager

Implement a consent model that can represent at minimum:

- `necessary` — always on;
- `analytics` — user-selectable/configurable according to deployment policy;
- `marketing` — off/not currently used.

Cookie banner requirements:

- clear Accept and Reject choices;
- reject must not be hidden or harder than accept;
- no pre-ticked optional categories;
- no consent inferred from scrolling/continued browsing;
- normal site use must remain available when optional tracking is refused;
- user can reopen privacy settings and withdraw consent;
- store consent version and timestamp;
- do not initialize consent-gated scripts before permission.

Suggested record for authenticated users where useful:

`UserConsentPreference`

- `UserId`
- `ConsentVersion`
- `AnalyticsAllowed`
- `MarketingAllowed`
- `UpdatedAtUtc`

For guests, use a minimal first-party preference cookie/local storage entry if appropriate.

## Event schema

Use a controlled event catalog.

Every analytics event should have:

- event name;
- timestamp;
- anonymous/pseudonymous session ID where allowed;
- authenticated user ID only when necessary and allowed;
- route/feature;
- small structured properties;
- application version/environment.

Do not send arbitrary objects.

## Recommended events

### Acquisition / activation

- `session_started`
- `landing_viewed`
- `guest_session_created`
- `recovery_key_created` — boolean/event only; NEVER the key value
- `registration_started`
- `registration_completed`
- `login_completed`
- `guest_claim_completed`

### Prediction engagement

- `fixture_viewed`
- `prediction_started`
- `prediction_created`
- `prediction_updated`
- `matchweek_predictions_completed`
- `prediction_result_viewed`
- `leaderboard_viewed`

### Social/league

- `prediction_league_created`
- `prediction_league_joined`
- `prediction_league_viewed`

### Pundits

- `pundit_list_viewed`
- `pundit_profile_viewed`
- `pundit_comparison_viewed`
- `pundit_source_opened`

### AI content

- `content_generation_started`
- `content_generation_completed`
- `content_generation_failed`
- `content_regenerated`
- `content_exported`

Never send full prompt/output content to analytics by default.

Use internal content IDs/types/tone/model class instead.

### Operational

Prefer logs/metrics rather than product analytics for:

- API failures;
- job failures;
- provider failures;
- cache failures;
- auth failures;
- AI provider failures.

## Useful metrics for admin

### Users

- total registered users;
- new users by day/week/month;
- active users (DAU/WAU/MAU where meaningful);
- anonymous sessions;
- guest-to-account conversion;
- login method distribution;
- returning users;
- dormant users.

### Prediction product

- predictions created;
- fixtures with predictions;
- predictions per active user;
- matchweek participation rate;
- next-matchweek return rate;
- exact-score rate;
- private leagues created/joined.

### Pundits

- pundit page views;
- comparisons performed;
- tracked pundits;
- prediction extraction success/review rate.

### AI content

- generations by type;
- success/failure rate;
- exports;
- generations per user;
- estimated provider cost;
- credits consumed later.

## Data minimization

Avoid collecting:

- full IP address as long-term analytics identity;
- precise geolocation;
- raw access/refresh tokens;
- passwords;
- recovery keys;
- private league invite secrets;
- full free-form AI prompts or content unless required for the actual product feature;
- third-party OAuth tokens;
- unnecessary device fingerprinting.

If IP is needed for security/rate limiting, treat it as security telemetry, restrict access and use a short documented retention period.

## Retention

Define configurable retention policies.

Suggested starting points for engineering design, subject to final policy review:

- raw product events: short/medium window sufficient for analysis;
- aggregated metrics: longer retention where no longer personal;
- security logs: based on incident-response need;
- admin audit logs: longer retention appropriate to privileged actions;
- deleted-user linkage: anonymize/delete where obligations and product integrity allow.

Do not hardcode retention periods throughout business logic.

## Data subject support

Admin/user account architecture should make it possible to:

- export a user's account/profile data;
- identify stored analytics linked directly to the user where applicable;
- delete/anonymize user data according to policy;
- record consent changes;
- provide account deletion flow.

## Vendor rule

Prefer privacy-preserving, EU-friendly analytics deployment/configuration.

Do not automatically add Google Analytics, Meta Pixel, TikTok Pixel or similar tools.

If an external analytics provider is selected later:

- document processor/controller implications;
- configure region/data residency where available;
- sign required data processing agreements;
- configure IP minimization;
- disable advertising/profiling features;
- integrate with consent manager before loading scripts.
