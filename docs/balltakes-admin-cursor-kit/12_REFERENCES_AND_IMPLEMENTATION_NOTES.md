# References & Implementation Notes

These references were current when this kit was prepared in August 2026. Cursor should re-check official documentation before implementing provider-specific behavior if package/API versions in the repository differ.

## Supabase

Supabase Auth overview:
https://supabase.com/docs/guides/auth

Supabase users:
https://supabase.com/docs/guides/auth/users

Supabase user management:
https://supabase.com/docs/guides/auth/managing-user-data

Supabase custom claims and RBAC:
https://supabase.com/docs/guides/api/custom-claims-and-role-based-access-control-rbac

Supabase JWT guidance:
https://supabase.com/docs/guides/auth/jwts

Supabase Admin API methods require trusted server execution and secret/service-role credentials. Never expose these credentials to the browser.

## EU / Dutch privacy guidance

European Data Protection Board cookie information:
https://www.edpb.europa.eu/edpb-cookie-policy_en

European Commission cookie policy:
https://commission.europa.eu/cookies-policy_en

European Commission consent information:
https://commission.europa.eu/law/law-topic/data-protection/information-individuals_en

Dutch government cookie guidance:
https://www.rijksoverheid.nl/vraag-en-antwoord/telecommunicatie/mag-een-website-ongevraagd-cookies-plaatsen

Dutch Data Protection Authority cookie guidance/search:
https://autoriteitpersoonsgegevens.nl/en/search?keys=cookies

## Engineering interpretation used in this kit

- Functional/strictly necessary cookies are separated from optional tracking.
- Privacy-friendly analytics should minimize identification and avoid cross-site profiling.
- BallTakes should be designed so optional analytics can be consent-gated.
- Tracking/advertising cookies are explicitly out of scope.
- Rejecting optional tracking must not block normal use.
- Consent UI must not use dark patterns.
- Admin user management uses server-side Supabase Admin APIs.
- Admin role/authorization is server enforced.

This document is not legal advice; production privacy/cookie wording and vendor contracts should be reviewed for the actual final deployment.
