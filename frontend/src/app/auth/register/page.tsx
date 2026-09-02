"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { AvatarPicker } from "@/components/session/AvatarPicker";
import { Button, buttonVariants } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { getOrCreateAnonymousUser } from "@/lib/anonymous";
import { markJustSignedIn, withSignedInQuery } from "@/lib/auth-redirect";
import { prepareAvatarPreview, stashPendingAvatarDataUrl, syncAuthSession } from "@/lib/avatar-upload";
import { getStoredRecoveryToken } from "@/lib/session";
import { createClient } from "@/lib/supabase/client";
import { getOAuthRedirectUrl } from "@/lib/supabase/oauth";
import { cn } from "@/lib/utils";

export default function RegisterPage() {
  const router = useRouter();
  const queryClient = useQueryClient();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [confirmationSent, setConfirmationSent] = useState(false);
  const [avatarPreview, setAvatarPreview] = useState<string | null>(null);
  const [avatarError, setAvatarError] = useState<string | null>(null);
  const [avatarBusy, setAvatarBusy] = useState(false);
  const [recoveryCode] = useState<string | null>(() => {
    if (typeof window === "undefined") return null;
    getOrCreateAnonymousUser();
    return getStoredRecoveryToken();
  });

  const handleAvatarChosen = async (file: File) => {
    setAvatarError(null);
    setAvatarBusy(true);
    try {
      const preview = await prepareAvatarPreview(file);
      setAvatarPreview(preview);
      stashPendingAvatarDataUrl(preview);
    } catch (err) {
      setAvatarError(err instanceof Error ? err.message : "Couldn't read that photo.");
    } finally {
      setAvatarBusy(false);
    }
  };

  const handleRegister = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setLoading(true);

    const supabase = createClient();
    if (!supabase) {
      setError("Supabase is not configured. Set NEXT_PUBLIC_SUPABASE_URL and NEXT_PUBLIC_SUPABASE_ANON_KEY.");
      setLoading(false);
      return;
    }

    const { data, error: authError } = await supabase.auth.signUp({
      email,
      password,
      options: {
        data: { display_name: email },
        emailRedirectTo: getOAuthRedirectUrl("/"),
      },
    });

    setLoading(false);
    if (authError) {
      setError(authError.message);
      return;
    }

    // When email confirmation is required, Supabase returns no session. The user
    // must click the link in their email before a session exists — so we show a
    // "check your inbox" state instead of pretending they're logged in.
    if (!data.session) {
      setConfirmationSent(true);
      return;
    }

    try {
      await syncAuthSession();
    } catch {
      // Non-blocking — session cookies are set.
    }

    await queryClient.invalidateQueries({ queryKey: ["session"] });

    markJustSignedIn();
    router.push(withSignedInQuery("/"));
    router.refresh();
  };

  const handleGoogleRegister = async () => {
    setError(null);
    const supabase = createClient();
    if (!supabase) {
      setError("Supabase is not configured.");
      return;
    }

    const { error: authError } = await supabase.auth.signInWithOAuth({
      provider: "google",
      options: {
        redirectTo: getOAuthRedirectUrl("/"),
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
          <CardTitle className="text-lg">Create account</CardTitle>
          <CardDescription>
            Register to unlock leagues, unlimited AI content, and full stats.
            Google sign-up uses your Google photo; email sign-up can add one below.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {confirmationSent ? (
            <div className="space-y-4">
              <div className="rounded-lg border border-border bg-muted/50 p-4 text-sm">
                <p className="font-medium">Confirm your email</p>
                <p className="mt-1 text-muted-foreground">
                  We sent a confirmation link to{" "}
                  <span className="font-medium text-foreground">{email}</span>. Click the
                  link to activate your account, then log in.
                  {avatarPreview
                    ? " We'll add your photo as soon as you're signed in."
                    : ""}
                </p>
              </div>
              <Link
                href="/auth/login"
                className={cn(buttonVariants({ size: "sm" }), "w-full")}
              >
                Go to log in
              </Link>
              <Link
                href="/"
                className={cn(buttonVariants({ variant: "ghost", size: "sm" }), "w-full")}
              >
                Continue as guest
              </Link>
            </div>
          ) : (
          <>
          {recoveryCode && (
            <div className="rounded-lg border border-border bg-muted/50 p-3 text-sm">
              <p className="font-medium">Guest recovery key</p>
              <p className="mt-1 break-all font-mono text-[11px] text-primary">{recoveryCode}</p>
              <p className="mt-1 text-xs text-muted-foreground">
                Save this key to restore anonymous predictions if you clear cookies. Registered accounts keep picks on your profile.
              </p>
            </div>
          )}

          <Button
            type="button"
            variant="outline"
            className="w-full"
            onClick={handleGoogleRegister}
          >
            Sign up with Google
          </Button>

          <div className="relative">
            <div className="absolute inset-0 flex items-center">
              <span className="w-full border-t border-border" />
            </div>
            <div className="relative flex justify-center text-xs uppercase">
              <span className="bg-card px-2 text-muted-foreground">Or email</span>
            </div>
          </div>

          <form onSubmit={handleRegister} className="space-y-4">
            <AvatarPicker
              displayName={email.trim() || "You"}
              previewUrl={avatarPreview ?? undefined}
              busy={avatarBusy}
              error={avatarError}
              onFileChosen={handleAvatarChosen}
            />
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
                autoComplete="new-password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                minLength={8}
                required
              />
            </div>
            {error && (
              <p className="text-sm text-destructive" role="alert">
                {error}
              </p>
            )}
            <Button type="submit" className="w-full" disabled={loading}>
              {loading ? "Creating account..." : "Create account"}
            </Button>
          </form>

          <p className="text-center text-sm text-muted-foreground">
            Already have an account?{" "}
            <Link href="/auth/login" className="text-primary underline underline-offset-2">
              Log in
            </Link>
          </p>
          <Link
            href="/"
            className={cn(buttonVariants({ variant: "ghost", size: "sm" }), "w-full")}
          >
            Continue as guest
          </Link>
          </>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
