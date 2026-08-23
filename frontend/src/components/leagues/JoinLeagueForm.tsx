"use client";

import { useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { useJoinLeague } from "@/hooks/useLeaderboard";
import { getApiErrorMessage } from "@/lib/api";

export function JoinLeagueForm() {
  const [inviteCode, setInviteCode] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const joinLeague = useJoinLeague();
  const queryClient = useQueryClient();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setSuccess(null);

    if (!inviteCode.trim()) {
      setError("Enter the invite code.");
      return;
    }

    setLoading(true);
    try {
      const league = await joinLeague(inviteCode.trim().toUpperCase());
      setSuccess(
        league.myDisplayName
          ? `Joined ${league.name} as ${league.myDisplayName}!`
          : `Joined ${league.name}!`
      );
      setInviteCode("");
      queryClient.invalidateQueries({ queryKey: ["leagues"] });
    } catch (err) {
      setError(getApiErrorMessage(err));
    } finally {
      setLoading(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <div>
        <label htmlFor="invite-code" className="mb-1.5 block text-sm font-medium">
          Invite code
        </label>
        <Input
          id="invite-code"
          value={inviteCode}
          onChange={(e) => setInviteCode(e.target.value.toUpperCase())}
          placeholder="PL2026AB"
          className="font-mono uppercase"
          required
        />
      </div>
      {error && (
        <p className="text-sm text-destructive" role="alert">
          {error}
        </p>
      )}
      {success && (
        <p className="text-sm text-primary" role="status">
          {success}
        </p>
      )}
      <Button type="submit" disabled={loading} className="w-full sm:w-auto">
        {loading ? "Joining..." : "Join League"}
      </Button>
    </form>
  );
}
