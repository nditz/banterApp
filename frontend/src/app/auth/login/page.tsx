"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { Button, buttonVariants } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { markJustSignedIn, withSignedInQuery } from "@/lib/auth-redirect";
import { syncAuthSession } from "@/lib/avatar-upload";
import { createClient } from "@/lib/supabase/client";
import { getOAuthRedirectUrl } from "@/lib/supabase/oauth";
import { cn } from "@/lib/utils";

export default function LoginPage() {
  const router = useRouter();
  const queryClient = useQueryClient();
  const [redirectTo] = useState(() => {
    if (typeof window === "undefined") return "/";
    const redirect = new URLSearchParams(window.location.search).get("redirect");
    return redirect?.startsWith("/") ? redirect : "/";
  });
  const [oauthError] = useState(
    () =>
      typeof window !== "undefined" &&
      new URLSearchParams(window.location.search).has("error")
  );
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const handleEmailLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setLoading(true);

    const supabase = createClient();
    if (!supabase) {
      setError("Supabase is not configured. Set NEXT_PUBLIC_SUPABASE_URL and NEXT_PUBLIC_SUPABASE_ANON_KEY.");
      setLoading(false);
      return;
    }

    const { error: authError } = await supabase.auth.signInWithPassword({
      email,
      password,
    });

    setLoading(false);
    if (authError) {
      setError(authError.message);
      return;
    }

    try {
      await syncAuthSession();
    } catch {
      // Non-blocking — session cookies are set.
    }

    // Refresh the cached session so the app immediately reflects the logged-in state.
    await queryClient.invalidateQueries({ queryKey: ["session"] });

    markJustSignedIn();
    router.push(withSignedInQuery(redirectTo.startsWith("/") ? redirectTo : "/"));
    router.refresh();
  };

  const handleGoogleLogin = async () => {
    setError(null);
    const supabase = createClient();
    if (!supabase) {
      setError("Supabase is not configured.");
      return;
    }

    const safeRedirect = redirectTo.startsWith("/") ? redirectTo : "/";
    const { error: authError } = await supabase.auth.signInWithOAuth({
      provider: "google",
      options: {
        redirectTo: getOAuthRedirectUrl(safeRedirect),
      },
    });

    if (authError) {
      setError(authError.message);
    }
  };

  return (
    <div className="mx-auto flex min-h-[60vh] max-w-md flex-col justify-center py-8">
      <Card className="border-border shadow-sm">
        <CardHeader>
          <CardTitle className="text-lg">Log in</CardTitle>
          <CardDescription>
            Sign in to save predictions, create leagues, and track your stats.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <Button
            type="button"
            variant="outline"
            className="w-full"
            onClick={handleGoogleLogin}
          >
            Continue with Google
          </Button>

          <div className="relative">
            <div className="absolute inset-0 flex items-center">
              <span className="w-full border-t border-border" />
            </div>
            <div className="relative flex justify-center text-xs uppercase">
              <span className="bg-card px-2 text-muted-foreground">Or email</span>
            </div>
          </div>

          <form onSubmit={handleEmailLogin} className="space-y-4">
            <div>
              <label htmlFor="email" className="mb-1.5 block text-sm font-medium">
                Email
              </label>
              <Input
                id="email"
                type="email"
                autoComplete="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
              />
            </div>
            <div>
              <label htmlFor="password" className="mb-1.5 block text-sm font-medium">
                Password
              </label>
              <Input
                id="password"
                type="password"
                autoComplete="current-password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
              />
            </div>
            {oauthError && !error && (
              <p className="text-sm text-destructive" role="alert">
                Google sign-in failed. Check Supabase Google provider and redirect URLs.
              </p>
            )}
            {error && (
              <p className="text-sm text-destructive" role="alert">
                {error}
              </p>
            )}
            <Button type="submit" className="w-full" disabled={loading}>
              {loading ? "Signing in..." : "Log in"}
            </Button>
          </form>

          <p className="text-center text-sm text-muted-foreground">
            No account?{" "}
            <Link href="/auth/register" className="text-primary underline underline-offset-2">
              Sign up
            </Link>
          </p>
          <Link
            href="/"
            className={cn(buttonVariants({ variant: "ghost", size: "sm" }), "w-full")}
          >
            Continue as guest
          </Link>
        </CardContent>
      </Card>
    </div>
  );
}
