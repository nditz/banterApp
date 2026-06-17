"use client";

import { useState } from "react";
import { Check, Copy, Crown, Users } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import type { League } from "@/lib/types";

interface LeagueCardProps {
  league: League;
}

export function LeagueCard({ league }: LeagueCardProps) {
  const [copied, setCopied] = useState(false);

  const inviteLink =
    typeof window === "undefined"
      ? `/leagues/join/${league.inviteCode}`
      : `${window.location.origin}/leagues/join/${league.inviteCode}`;

  const handleCopy = async () => {
    await navigator.clipboard.writeText(inviteLink);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  return (
    <Card className="rounded-lg">
      <CardHeader>
        <div className="flex items-start justify-between gap-2">
          <CardTitle className="text-base">{league.name}</CardTitle>
          <div className="flex gap-1.5">
            {league.isAdmin && (
              <Badge className="gap-1 bg-gold/15 text-gold ring-1 ring-gold/40">
                <Crown className="size-3" aria-hidden />
                Admin
              </Badge>
            )}
            {league.rank && <Badge variant="secondary">Rank #{league.rank}</Badge>}
          </div>
        </div>
        <CardDescription className="flex items-center gap-1">
          <Users className="size-3.5" aria-hidden />
          {league.memberCount}
          {league.maxMembers ? ` / ${league.maxMembers}` : ""} players
          {league.bonusPointsEnabled && (
            <span className="ml-1 text-gold-foreground">· bonus picks on</span>
          )}
          {league.myDisplayName && ` · You play as ${league.myDisplayName}`}
        </CardDescription>
      </CardHeader>
      <CardContent>
        <div className="flex items-center justify-between gap-2 rounded-lg bg-muted/50 px-3 py-2">
          <span className="shrink-0 text-xs text-muted-foreground">Invite code</span>
          <code className="font-mono text-sm font-semibold">{league.inviteCode}</code>
        </div>
        {league.points !== undefined && (
          <p className="mt-2 text-sm text-muted-foreground">
            Your points:{" "}
            <span className="font-semibold text-foreground">{league.points}</span>
          </p>
        )}
      </CardContent>
      <CardFooter>
        <Button
          type="button"
          variant="outline"
          size="sm"
          className="w-full"
          onClick={handleCopy}
        >
          {copied ? (
            <Check className="size-3.5 text-pitch" aria-hidden />
          ) : (
            <Copy className="size-3.5" aria-hidden />
          )}
          {copied ? "Invite link copied" : "Copy invite link"}
        </Button>
      </CardFooter>
    </Card>
  );
}
