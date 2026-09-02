import { NextResponse } from "next/server";
import type { EmailOtpType } from "@supabase/supabase-js";
import { withSignedInQuery } from "@/lib/auth-redirect";
import { getSupabaseAvatarUrl } from "@/lib/avatars";
import { createClient } from "@/lib/supabase/server";

function resolveApiUrl(): string {
  return process.env.API_PROXY_URL ?? process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000";
}

/**
 * Handles Supabase email links that use the `token_hash` + `type` format
 * (e.g. the default "Confirm signup" template). Newer PKCE `code` links are
 * handled by /auth/callback instead.
 */
export async function GET(request: Request) {
  const { searchParams, origin } = new URL(request.url);
  const tokenHash = searchParams.get("token_hash");
  const type = searchParams.get("type") as EmailOtpType | null;
  const next = searchParams.get("next") ?? "/";
  const safeNext = next.startsWith("/") && !next.startsWith("//") ? next : "/";

  if (!tokenHash || !type) {
    return NextResponse.redirect(`${origin}/auth/login?error=confirm`);
  }

  const supabase = await createClient();
  if (!supabase) {
    return NextResponse.redirect(`${origin}/auth/login?error=supabase_config`);
  }

  const { data, error } = await supabase.auth.verifyOtp({
    type,
    token_hash: tokenHash,
  });

  if (error || !data.session) {
    return NextResponse.redirect(`${origin}/auth/login?error=confirm`);
  }

  try {
    const avatarUrl = data.user ? getSupabaseAvatarUrl(data.user) : undefined;
    await fetch(`${resolveApiUrl()}/api/auth/session/sync`, {
      method: "POST",
      headers: {
        Authorization: `Bearer ${data.session.access_token}`,
        "Content-Type": "application/json",
      },
      body: JSON.stringify(avatarUrl ? { avatarUrl } : {}),
    });
  } catch {
    // Session cookies are set — backend sync can retry on next API call.
  }

  return NextResponse.redirect(`${origin}${withSignedInQuery(safeNext)}`);
}
