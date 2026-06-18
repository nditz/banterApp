import { NextResponse } from "next/server";
import { createClient } from "@/lib/supabase/server";

function resolveApiUrl(): string {
  return process.env.API_PROXY_URL ?? process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000";
}

export async function GET(request: Request) {
  const { searchParams, origin } = new URL(request.url);
  const code = searchParams.get("code");
  const next = searchParams.get("next") ?? "/";
  const safeNext = next.startsWith("/") && !next.startsWith("//") ? next : "/";

  if (!code) {
    return NextResponse.redirect(`${origin}/auth/login?error=oauth`);
  }

  const supabase = await createClient();
  if (!supabase) {
    return NextResponse.redirect(`${origin}/auth/login?error=supabase_config`);
  }

  const { data, error } = await supabase.auth.exchangeCodeForSession(code);
  if (error || !data.session) {
    return NextResponse.redirect(`${origin}/auth/login?error=oauth`);
  }

  try {
    await fetch(`${resolveApiUrl()}/api/auth/session/sync`, {
      method: "POST",
      headers: {
        Authorization: `Bearer ${data.session.access_token}`,
        "Content-Type": "application/json",
      },
    });
  } catch {
    // Session cookies are set — backend sync can retry on next API call.
  }

  return NextResponse.redirect(`${origin}${safeNext}`);
}
