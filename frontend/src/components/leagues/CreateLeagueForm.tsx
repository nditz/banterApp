"use client";

import { useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { Check, Copy, Link2 } from "lucide-react";
import { TermsAcceptPanel } from "@/components/session/TermsAcceptPanel";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { useCreateLeague } from "@/hooks/useLeaderboard";
import { useNeedsTerms } from "@/hooks/useNeedsTerms";
import { getApiErrorMessage } from "@/lib/api";
import type { League } from "@/lib/types";

const LEAGUE_NAME_MAX = 25;

function buildInviteLink(inviteCode: string): string {
  if (typeof window === "undefined") return `/leagues/join/${inviteCode}`;
  return `${window.location.origin}/leagues/join/${inviteCode}`;
}

export function CreateLeagueForm() {
  const [name, setName] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [created, setCreated] = useState<League | null>(null);
  const [loading, setLoading] = useState(false);
  const [copied, setCopied] = useState(false);
  const createLeague = useCreateLeague();
  const queryClient = useQueryClient();
  const { needsTerms } = useNeedsTerms();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!name.trim()) {
      setError("Enter a league name.");
      return;
    }

    setLoading(true);
    try {
      const league = await createLeague(name.trim());
      setCreated(league);
      setName("");
      queryClient.invalidateQueries({ queryKey: ["leagues"] });
    } catch (err) {
      setError(getApiErrorMessage(err));
    } finally {
      setLoading(false);
    }
  };

  const handleCopyLink = async () => {
    if (!created) return;
    await navigator.clipboard.writeText(buildInviteLink(created.inviteCode));
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  if (needsTerms) {
    return (
      <div className="space-y-3">
        <p className="text-sm text-muted-foreground">
          To create a league you first onboard as the league admin — no signup
          needed, just accept the terms and keep your session key.
        </p>
        <TermsAcceptPanel variant="compact" />
      </div>
    );
  }

  if (created) {
    return (
      <div className="space-y-3" role="status">
        <p className="text-sm font-semibold text-pitch">
          {created.name} is live! Share this invite link:
        </p>
        <div className="flex items-center gap-2">
          <code className="min-w-0 flex-1 truncate rounded-md border border-border bg-muted/50 px-2 py-1.5 font-mono text-[11px]">
            {buildInviteLink(created.inviteCode)}
          </code>
          <Button
            type="button"
            variant="outline"
            size="sm"
            className="h-8 shrink-0 text-xs"
            onClick={handleCopyLink}
          >
            {copied ? (
              <Check className="size-3.5 text-pitch" aria-hidden />
            ) : (
              <Copy className="size-3.5" aria-hidden />
            )}
            {copied ? "Copied" : "Copy link"}
          </Button>
        </div>
        <p className="text-xs text-muted-foreground">
          <Link2 className="mr-1 inline size-3" aria-hidden />
          Up to {created.maxMembers ?? 50} players can join with this link. Names
          are assigned automatically from account email or a guest ID.
        </p>
        <Button
          type="button"
          variant="ghost"
          size="sm"
          className="h-8 text-xs"
          onClick={() => setCreated(null)}
        >
          Create another league
        </Button>
      </div>
    );
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <div>
        <label htmlFor="league-name" className="mb-1.5 block text-sm font-medium">
          League name
        </label>
        <Input
          id="league-name"
          value={name}
          onChange={(e) => setName(e.target.value)}
          placeholder="Office World Cup Pool"
          maxLength={LEAGUE_NAME_MAX}
          required
        />
        <p className="mt-1 text-[11px] text-muted-foreground">
          Max {LEAGUE_NAME_MAX} characters. Keep it family-friendly.
        </p>
      </div>
      {error && (
        <p className="text-sm text-destructive" role="alert">
          {error}
        </p>
      )}
      <Button type="submit" disabled={loading} className="w-full sm:w-auto">
        {loading ? "Creating..." : "Create league & get invite link"}
      </Button>
    </form>
  );
}
