"use client";

import { LeagueCard } from "@/components/leagues/LeagueCard";
import { Skeleton } from "@/components/ui/skeleton";
import { useLeagues } from "@/hooks/useLeaderboard";

export function LeaguesList() {
  const { data: leagues, isLoading, isError } = useLeagues();

  return (
    <section aria-labelledby="my-leagues-heading">
      <h2 id="my-leagues-heading" className="mb-4 font-heading text-xl font-semibold">
        My Leagues
      </h2>
      {isError && (
        <p className="mb-3 text-sm text-muted-foreground">Showing demo leagues</p>
      )}
      {isLoading ? (
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
        <p className="rounded-lg border border-dashed border-border py-12 text-center text-sm text-muted-foreground">
          No leagues yet. Create one or join with an invite code above.
        </p>
      )}
    </section>
  );
}
