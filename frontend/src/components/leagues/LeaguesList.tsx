"use client";

import { Users } from "lucide-react";
import { LeagueCard } from "@/components/leagues/LeagueCard";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState, ErrorState } from "@/components/ui/states";
import { useLeagues } from "@/hooks/useLeaderboard";

export function LeaguesList() {
  const { data: leagues, isLoading, isError, refetch } = useLeagues();

  return (
    <section aria-labelledby="my-leagues-heading">
      <h2 id="my-leagues-heading" className="mb-4 font-heading text-xl font-semibold">
        My Leagues
      </h2>
      {isError ? (
        <ErrorState
          title="Couldn't load your leagues"
          description="Your leagues are served live, so nothing is shown until we reach the server."
          onRetry={() => refetch()}
        />
      ) : isLoading ? (
        <div className="grid gap-4 sm:grid-cols-2">
          {Array.from({ length: 2 }).map((_, i) => (
            <Skeleton key={i} className="h-48 w-full rounded-xl" />
          ))}
        </div>
      ) : leagues && leagues.length > 0 ? (
        <div className="grid gap-4 sm:grid-cols-2">
          {leagues.map((league) => (
            <LeagueCard key={league.id} league={league} />
          ))}
        </div>
      ) : (
        <EmptyState
          icon={Users}
          title="No leagues yet"
          description="Create one or join with an invite code above, then settle it on the board."
        />
      )}
    </section>
  );
}
