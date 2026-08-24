#!/usr/bin/env python3
"""Static checks that repo files match Vercel / Render / Supabase hosting docs."""

from __future__ import annotations

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
ERRORS: list[str] = []


def fail(message: str) -> None:
    ERRORS.append(message)


def read(relative: str) -> str:
    path = ROOT / relative
    if not path.is_file():
        fail(f"Missing required file: {relative}")
        return ""
    return path.read_text(encoding="utf-8")


def must_contain(relative: str, needle: str, *, hint: str | None = None) -> None:
    text = read(relative)
    if text and needle not in text:
        extra = f" ({hint})" if hint else ""
        fail(f"{relative} must contain {needle!r}{extra}")


def main() -> int:
    dockerfile = read("backend/Dockerfile")
    if dockerfile:
        if "mcr.microsoft.com/dotnet/sdk:9.0" not in dockerfile:
            fail("backend/Dockerfile must build with the .NET 9 SDK image (matches net9.0).")
        if "mcr.microsoft.com/dotnet/aspnet:9.0" not in dockerfile:
            fail("backend/Dockerfile must run on the .NET 9 aspnet image.")
        if "/health" not in dockerfile:
            fail("backend/Dockerfile HEALTHCHECK must probe /health (Render health check path).")
        if "ENV PORT=8080" not in dockerfile:
            fail("backend/Dockerfile must default PORT=8080 for local/Render binding.")
        if "ENTRYPOINT" not in dockerfile:
            fail("backend/Dockerfile must define ENTRYPOINT.")

    vercel = read("frontend/vercel.json")
    if vercel and '"framework": "nextjs"' not in vercel:
        fail('frontend/vercel.json must set framework to "nextjs" (Vercel Root Directory is frontend/).')

    next_config = read("frontend/next.config.ts")
    if next_config:
        if 'source: "/api-backend/:path*"' not in next_config:
            fail("frontend/next.config.ts must rewrite /api-backend to the Render API.")
        if "API_PROXY_URL" not in next_config:
            fail("frontend/next.config.ts must read API_PROXY_URL for the Vercel rewrite target.")

    health = read("backend/BanterApp.Api/Features/Health/HealthEndpoints.cs")
    if health:
        if 'MapGet("/health"' not in health:
            fail("API must expose GET /health for Render and Docker health checks.")
        if 'MapGet("/api/health"' not in health:
            fail("API must expose GET /api/health for database connectivity checks.")

    bot = read("backend/BanterApp.Api/Middleware/BotProtectionMiddleware.cs")
    if bot and 'path.Equals("/health"' not in bot and 'StartsWith("/health"' not in bot:
        fail("Bot protection must allow /health so Render, Docker, and CI probes are not blocked.")

    backend_env = read("backend/.env.template")
    if backend_env:
        for key in (
            "ASPNETCORE_ENVIRONMENT=Production",
            "ALLOWED_ORIGINS=",
            "DATABASE_URL=",
            "Supabase__Url=",
            "Supabase__JwtSecret=",
            "Supabase__AnonKey=",
            "Security__SessionSecret=",
            "Security__TurnstileSecretKey=",
        ):
            if key not in backend_env:
                fail(f"backend/.env.template is missing required Render key {key.rstrip('=')}.")
        if "pooler.supabase.com" not in backend_env:
            fail("backend/.env.template DATABASE_URL must use the Supabase session pooler host.")
        if ":5432/postgres" not in backend_env:
            fail("backend/.env.template DATABASE_URL must use session pooler port 5432 (EF Core).")
        if "db." in backend_env and ".supabase.co:5432" in backend_env:
            fail("backend/.env.template must not recommend the IPv6-only db.*.supabase.co direct host.")
        live_origins = [
            line
            for line in backend_env.splitlines()
            if line.startswith("ALLOWED_ORIGINS=") and "localhost" in line.lower()
        ]
        if live_origins:
            fail("backend/.env.template ALLOWED_ORIGINS must not include localhost for production.")

    frontend_env = read("frontend/.env.local.template")
    if frontend_env:
        for key in (
            "NEXT_PUBLIC_SUPABASE_URL=",
            "NEXT_PUBLIC_SUPABASE_ANON_KEY=",
            "NEXT_PUBLIC_SITE_URL=",
            "API_PROXY_URL=",
            "NEXT_PUBLIC_TURNSTILE_SITE_KEY=",
        ):
            if key not in frontend_env:
                fail(f"frontend/.env.local.template is missing required Vercel key {key.rstrip('=')}.")
        if "API_PROXY_URL=https://api.balltakes.com" not in frontend_env:
            fail("frontend/.env.local.template should default API_PROXY_URL to https://api.balltakes.com.")

    if not (ROOT / "backend/BanterApp.Api/Data/Migrations").is_dir():
        fail("backend/BanterApp.Api/Data/Migrations is missing.")

    if ERRORS:
        print("Hosting configuration checks failed:\n", file=sys.stderr)
        for item in ERRORS:
            print(f"  - {item}", file=sys.stderr)
        return 1

    print("Hosting configuration checks passed (Vercel frontend/, Render backend/ Docker, Supabase pooler).")
    return 0


if __name__ == "__main__":
    sys.exit(main())
