# Production Security Configuration

## Required backend environment variables (Production)

| Variable | Purpose |
|----------|---------|
| `ASPNETCORE_ENVIRONMENT` | Must be `Production` |
| `ConnectionStrings__DefaultConnection` | PostgreSQL (required in prod) |
| `Supabase__Url` | Supabase project URL |
| `Supabase__JwtSecret` | JWT validation secret |
| `Security__SessionSecret` | HMAC for anonymous recovery tokens (32+ chars, not dev default) |
| `Security__TurnstileSecretKey` | Cloudflare Turnstile server secret |
| `Ai__ApiKey` | OpenAI key when `Ai__Provider=openai` |
| `YouTube__ApiKey` | YouTube Data API key |
| `Cors__AllowedOrigins__0` | Production frontend origin (no localhost) |
| `Admin__AllowedEmails__0` | At least one admin email |
| `Legal__DisclaimerText` | Footer/legal disclaimer |
| `Legal__TermsUrl` | Public terms URL |
| `Legal__PrivacyPolicyUrl` | Public privacy URL |
| `GIT_COMMIT` | Deploy SHA (optional, shown in admin health) |

## Frontend (Vercel)

| Variable | Purpose |
|----------|---------|
| `NEXT_PUBLIC_SITE_URL` | Canonical site URL |
| `NEXT_PUBLIC_TURNSTILE_SITE_KEY` | Turnstile widget (required in prod) |
| `API_PROXY_URL` | Backend API origin for rewrites |
| `NEXT_PUBLIC_SUPABASE_URL` | Supabase project URL |
| `NEXT_PUBLIC_SUPABASE_ANON_KEY` | Supabase anon key |

## Security controls implemented

- CSRF double-submit on state-changing API requests
- SSRF-safe outbound HTTP (`SafeHttpClient` + `OutboundUrlValidator`)
- Route-specific rate limits (`RateLimitPolicies`)
- Bot protection middleware (user-agent blocking)
- Turnstile on auth, consent, and sensitive writes
- Admin server-side authorization + audit logs
- Auth audit logging
- Security headers (API + Next.js CSP)
- HTML sanitization on feed/AI content
- Provider usage guard (OpenAI/YouTube daily limits + circuit breaker)

## Running tests

```bash
dotnet test backend/BanterApp.Api.Tests/BanterApp.Api.Tests.csproj
```

## Launch checklist

Open `/admin/launch-checklist` as a platform admin after deploy. All items should pass before v1 launch.

## Remaining follow-ups

- Hangfire PostgreSQL storage for durable jobs across restarts
- Redis-backed rate limits for multi-instance deployments
- Supabase MFA enforcement for admin users (`aal2` claim)
- Nonce-based CSP when Next.js build supports it without `unsafe-inline`
