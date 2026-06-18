/** OAuth return URL — must match Supabase redirect allow list and hit /auth/callback. */
export function getOAuthRedirectUrl(next = "/"): string {
  const origin =
    typeof window !== "undefined"
      ? window.location.origin
      : (process.env.NEXT_PUBLIC_SITE_URL ?? "http://localhost:3000");

  return `${origin}/auth/callback?next=${encodeURIComponent(next)}`;
}
