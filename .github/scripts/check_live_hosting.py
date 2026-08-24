#!/usr/bin/env python3
"""Optional live checks against Vercel, Render, and Supabase when tokens are present."""

from __future__ import annotations

import json
import os
import sys
import urllib.error
import urllib.request

ERRORS: list[str] = []
SKIPPED: list[str] = []


def fail(message: str) -> None:
    ERRORS.append(message)


def get_json(url: str, headers: dict[str, str]) -> dict | list | None:
    request = urllib.request.Request(url, headers=headers)
    try:
        with urllib.request.urlopen(request, timeout=30) as response:
            payload = response.read().decode("utf-8")
            return json.loads(payload) if payload else {}
    except urllib.error.HTTPError as exc:
        body = exc.read().decode("utf-8", errors="replace")[:400]
        fail(f"HTTP {exc.code} from {url}: {body}")
        return None
    except urllib.error.URLError as exc:
        fail(f"Could not reach {url}: {exc.reason}")
        return None


def check_vercel() -> None:
    token = os.environ.get("VERCEL_TOKEN", "").strip()
    project_id = os.environ.get("VERCEL_PROJECT_ID", "").strip()
    org_id = os.environ.get("VERCEL_ORG_ID", "").strip()
    if not token or not project_id:
        SKIPPED.append("Vercel project inspect (set VERCEL_TOKEN and VERCEL_PROJECT_ID)")
        return

    query = f"?teamId={org_id}" if org_id else ""
    headers = {"Authorization": f"Bearer {token}"}
    project = get_json(f"https://api.vercel.com/v9/projects/{project_id}{query}", headers)
    if not isinstance(project, dict):
        return

    root = project.get("rootDirectory") or ""
    framework = project.get("framework") or ""
    if root not in ("frontend",):
        fail(f"Vercel rootDirectory is {root!r}; expected 'frontend'.")
    if framework and framework != "nextjs":
        fail(f"Vercel framework is {framework!r}; expected 'nextjs'.")

    env_payload = get_json(
        f"https://api.vercel.com/v9/projects/{project_id}/env{query}",
        headers,
    )
    rows = env_payload.get("envs", []) if isinstance(env_payload, dict) else []
    names = {str(row.get("key")) for row in rows if isinstance(row, dict)}
    required = {
        "API_PROXY_URL",
        "NEXT_PUBLIC_SITE_URL",
        "NEXT_PUBLIC_SUPABASE_URL",
        "NEXT_PUBLIC_SUPABASE_ANON_KEY",
        "NEXT_PUBLIC_TURNSTILE_SITE_KEY",
    }
    missing = sorted(required - names)
    if missing:
        fail("Vercel production/preview env is missing: " + ", ".join(missing))
    print("Vercel project settings look correct.")


def flatten_render_service(payload: dict) -> dict:
    if "service" in payload and isinstance(payload["service"], dict):
        return payload["service"]
    return payload


def check_render() -> None:
    token = os.environ.get("RENDER_API_KEY", "").strip()
    service_id = os.environ.get("RENDER_SERVICE_ID", "").strip()
    if not token or not service_id:
        SKIPPED.append("Render service inspect (set RENDER_API_KEY and RENDER_SERVICE_ID)")
        return

    headers = {"Authorization": f"Bearer {token}", "Accept": "application/json"}
    payload = get_json(f"https://api.render.com/v1/services/{service_id}", headers)
    if not isinstance(payload, dict):
        return

    service = flatten_render_service(payload)
    details = service.get("serviceDetails") if isinstance(service.get("serviceDetails"), dict) else {}
    health = str(details.get("healthCheckPath") or service.get("healthCheckPath") or "")
    root = str(service.get("rootDir") or details.get("rootDir") or "")
    runtime = str(details.get("env") or service.get("env") or "").lower()

    if health and health not in ("/health",):
        fail(f"Render healthCheckPath is {health!r}; expected '/health'.")
    if root and root not in ("backend",):
        fail(f"Render rootDir is {root!r}; expected 'backend'.")
    if runtime and runtime not in ("docker", "image"):
        fail(f"Render runtime is {runtime!r}; expected Docker.")

    env_payload = get_json(
        f"https://api.render.com/v1/services/{service_id}/env-vars?limit=100",
        headers,
    )
    rows: list = []
    if isinstance(env_payload, list):
        rows = env_payload
    elif isinstance(env_payload, dict):
        rows = list(env_payload.get("envVars") or env_payload.get("items") or [])

    names: set[str] = set()
    for row in rows:
        if not isinstance(row, dict):
            continue
        env_var = row.get("envVar") if isinstance(row.get("envVar"), dict) else row
        key = env_var.get("key") if isinstance(env_var, dict) else None
        if key:
            names.add(str(key))

    required = {
        "ASPNETCORE_ENVIRONMENT",
        "ALLOWED_ORIGINS",
        "DATABASE_URL",
        "Supabase__Url",
        "Supabase__JwtSecret",
        "Supabase__AnonKey",
        "Security__SessionSecret",
        "Security__TurnstileSecretKey",
    }
    missing = sorted(required - names)
    if missing:
        fail("Render env is missing: " + ", ".join(missing))
    print("Render service settings look correct.")


def check_supabase() -> None:
    url = os.environ.get("SUPABASE_URL", "").strip().rstrip("/")
    anon = os.environ.get("SUPABASE_ANON_KEY", "").strip()
    if not url:
        SKIPPED.append("Supabase Auth health (set SUPABASE_URL)")
        return

    headers = {"apikey": anon, "Authorization": f"Bearer {anon}"} if anon else {}
    request = urllib.request.Request(f"{url}/auth/v1/health", headers=headers)
    try:
        with urllib.request.urlopen(request, timeout=30) as response:
            if response.status >= 400:
                fail(f"Supabase Auth health returned HTTP {response.status}.")
                return
    except urllib.error.HTTPError as exc:
        fail(f"Supabase Auth health HTTP {exc.code} from {url}/auth/v1/health")
        return
    except urllib.error.URLError as exc:
        fail(f"Could not reach Supabase at {url}: {exc.reason}")
        return

    print("Supabase Auth health endpoint responded.")


def main() -> int:
    check_vercel()
    check_render()
    check_supabase()

    for item in SKIPPED:
        print(f"Skipped: {item}")

    if ERRORS:
        print("Live hosting checks failed:\n", file=sys.stderr)
        for item in ERRORS:
            print(f"  - {item}", file=sys.stderr)
        return 1

    if SKIPPED and not ERRORS:
        print("No blocking live hosting errors (some providers were skipped).")
    return 0


if __name__ == "__main__":
    sys.exit(main())
