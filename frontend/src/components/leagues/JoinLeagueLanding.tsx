"use client";

import { useState } from "react";
import Link from "next/link";
import { useQueryClient } from "@tanstack/react-query";
import { PartyPopper, Trophy, Users } from "lucide-react";
import { SessionKeyNotice } from "@/components/session/SessionKeyNotice";
import { TermsAcceptPanel } from "@/components/session/TermsAcceptPanel";
import { Button, buttonVariants } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { useJoinLeague, useLeaguePreview } from "@/hooks/useLeaderboard";
import { useNeedsTerms } from "@/hooks/useNeedsTerms";
import { getApiErrorMessage } from "@/lib/api";
import { cn } from "@/lib/utils";

interface JoinLeagueLandingProps {
  inviteCode: string;
}

export function JoinLeagueLanding({ inviteCode }: JoinLeagueLandingProps) {
  const { data: preview, isLoading, isError } = useLeaguePreview(inviteCode);
  const { needsTerms, isLoading: sessionLoading } = useNeedsTerms();
  const joinLeague = useJoinLeague();
  const queryClient = useQueryClient();

  const [displayName, setDisplayName] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [joining, setJoining] = useState(false);
  const [joinedName, setJoinedName] = useState<string | null>(null);

  const handleJoin = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    if (!displayName.trim()) {
      setError("Pick a name for this league.");
      return;
    }

    setJoining(true);
    try {
      await joinLeague(inviteCode, displayName.trim());
      setJoinedName(displayName.trim());
      queryClient.invalidateQueries({ queryKey: ["leagues"] });
      queryClient.invalidateQueries({ queryKey: ["league-preview", inviteCode] });
    } catch (err) {
      setError(getApiErrorMessage(err));
    } finally {
      setJoining(false);
    }
  };

  if (isLoading) {
    return (
      <div className="space-y-3" aria-busy="true">
        <Skeleton className="h-24 w-full rounded-lg" />
        <Skeleton className="h-40 w-full rounded-lg" />
      </div>
    );
  }

  if (isError || !preview) {
    return (
      <div className="rounded-lg border border-border bg-card p-6 text-center">
        <h1 className="text-base font-semibold">Invite link not found</h1>
        <p className="mt-2 text-sm text-muted-foreground">
          This invite link is invalid or the league no longer exists. Ask the
          league admin for a fresh link.
        </p>
        <Link
          href="/leagues"
          className={cn(buttonVariants({ variant: "outline", size: "sm" }), "mt-4")}
        >
          Go to leagues
        </Link>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {/* League invite header */}
      <header className="rounded-lg border border-border bg-card/85 p-5 text-center backdrop-blur">
        <span className="mx-auto flex size-10 items-center justify-center rounded-full bg-gold/15 ring-1 ring-gold/40">
          <Trophy className="size-5 text-gold" aria-hidden />
        </span>
        <p className="mt-2 text-[11px] font-bold uppercase tracking-widest text-muted-foreground">
          You&apos;re invited to join
        </p>
        <h1 className="mt-1 text-lg font-bold">{preview.name}</h1>
        <p className="mt-1 inline-flex items-center gap-1 text-xs text-muted-foreground">
          <Users className="size-3.5" aria-hidden />
          {preview.memberCount} / {preview.maxMembers} players
        </p>
      </header>

      {joinedName ? (
        <div className="space-y-4">
          <div
            className="rounded-lg border border-pitch/40 bg-pitch/10 p-5 text-center"
            role="status"
          >
            <PartyPopper className="mx-auto size-6 text-pitch" aria-hidden />
            <p className="mt-2 text-sm font-semibold">
              You&apos;re in, {joinedName}!
            </p>
            <p className="mt-1 text-xs text-muted-foreground">
              Your picks now count in {preview.name}. Time to show them you know
              ball.
            </p>
            <div className="mt-3 flex justify-center gap-2">
              <Link href="/" className={cn(buttonVariants({ size: "sm" }), "btn-tournament h-8 text-xs")}>
                Make predictions
              </Link>
              <Link
                href="/leagues"
                className={cn(buttonVariants({ variant: "outline", size: "sm" }), "h-8 text-xs")}
              >
                My leagues
              </Link>
            </div>
          </div>
          <SessionKeyNotice compact />
        </div>
      ) : preview.isFull ? (
        <div className="rounded-lg border border-border bg-card p-5 text-center">
          <p className="text-sm font-semibold">This league is full</p>
          <p className="mt-1 text-xs text-muted-foreground">
            {preview.maxMembers} players max. Ask the admin to start a second
            league — it only takes a minute.
          </p>
        </div>
      ) : !sessionLoading && needsTerms ? (
        <div className="space-y-3">
          <p className="text-center text-sm text-muted-foreground">
            No signup needed — accept the terms, grab your session key, and
            you&apos;re in.
          </p>
          <TermsAcceptPanel variant="compact" />
        </div>
      ) : (
        <form
          onSubmit={handleJoin}
          className="space-y-4 rounded-lg border border-border bg-card/85 p-5 backdrop-blur"
        >
          <div>
            <label htmlFor="landing-display-name" className="mb-1.5 block text-sm font-medium">
              Your name in this league
            </label>
            <Input
              id="landing-display-name"
              value={displayName}
              onChange={(e) => setDisplayName(e.target.value)}
              placeholder="e.g. Wandi"
              maxLength={40}
              autoFocus
              required
            />
            <p className="mt-1.5 text-[11px] text-muted-foreground">
              This is how office mates and family will see you on the standings.
            </p>
          </div>
          {error && (
            <p className="text-sm text-destructive" role="alert">
              {error}
            </p>
          )}
          <Button type="submit" disabled={joining} className="btn-tournament w-full">
            {joining ? "Joining..." : `Join ${preview.name}`}
          </Button>
          <SessionKeyNotice compact />
        </form>
      )}
    </div>
  );
}
