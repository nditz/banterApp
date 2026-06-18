import { Calendar } from "lucide-react";
import { PickChip } from "@/components/studio/PickChip";
import type { StudioMatchComparison, StudioPickRole } from "@/lib/types";
import {
  formatPunditSubtitle,
  formatSourcePlatformLabel,
  PUNDIT_PARODY_DISCLAIMER,
} from "@/lib/pundits";

interface MatchComparisonCardProps {
  match: StudioMatchComparison;
  /** Which roles to display. Defaults to all. */
  filter?: StudioPickRole[];
}

function formatKickoff(iso: string) {
  return new Intl.DateTimeFormat("en-GB", {
    weekday: "short",
    day: "numeric",
    month: "short",
    hour: "2-digit",
    minute: "2-digit",
  }).format(new Date(iso));
}

const roleLabel: Record<StudioPickRole, string> = {
  me: "You",
  league: "Your League",
  pundit: "The Pundits",
};

export function MatchComparisonCard({ match, filter }: MatchComparisonCardProps) {
  const picks = filter
    ? match.picks.filter((p) => filter.includes(p.role as StudioPickRole))
    : match.picks;

  // Group by role for clean display
  const groups = (["me", "league", "pundit"] as StudioPickRole[])
    .map((role) => ({ role, entries: picks.filter((p) => p.role === role) }))
    .filter((g) => g.entries.length > 0);

  return (
    <div className="rounded-xl border border-border bg-card shadow-sm">
      {/* Match header */}
      <div className="border-b border-border bg-muted/30 px-4 py-3">
        <div className="flex flex-wrap items-center justify-between gap-2">
          <h3 className="text-sm font-bold">
            {match.teamA}{" "}
            <span className="font-normal text-muted-foreground">vs</span>{" "}
            {match.teamB}
          </h3>
          <span className="inline-flex items-center gap-1 text-[11px] text-muted-foreground">
            <Calendar className="size-3" aria-hidden />
            {formatKickoff(match.kickoffTime)}
          </span>
        </div>
        {match.actualResult && (
          <p className="mt-1 text-xs font-semibold text-pitch">
            Final: {match.actualResult}
          </p>
        )}
      </div>

      {/* Picks grouped by role */}
      <div className="divide-y divide-border">
        {groups.map(({ role, entries }) => (
          <div key={role} className="flex flex-wrap items-start gap-x-4 gap-y-2 px-4 py-3">
            <span className="w-24 shrink-0 text-[11px] font-semibold text-muted-foreground pt-0.5">
              {roleLabel[role]}
            </span>
            <div className="flex min-w-0 flex-1 flex-wrap gap-2">
              {entries.map((p, i) => (
                <div key={i} className="flex flex-col items-start gap-0.5">
                  {role !== "me" && (
                    <span className="text-[10px] text-muted-foreground">
                      <span className="font-medium text-foreground/90">{p.name}</span>
                      {role === "pundit" && p.parodyCue && (
                        <span className="mt-0.5 block text-[10px] leading-snug text-flare/90">
                          {p.parodyCue}
                        </span>
                      )}
                      {role === "pundit" && !p.parodyCue && p.archetype && (
                        <span className="text-muted-foreground/70"> · {p.archetype}</span>
                      )}
                      {p.organization && role !== "pundit" && (
                        <span className="text-muted-foreground/60"> · {p.organization}</span>
                      )}
                      {p.sourcePlatform && (
                        <span className="text-muted-foreground/60">
                          {" "}
                          · via {formatSourcePlatformLabel(p.sourcePlatform)}
                        </span>
                      )}
                      {p.sourceUrl && (
                        <a
                          href={p.sourceUrl}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="ml-1 text-pitch underline-offset-2 hover:underline"
                        >
                          Source
                        </a>
                      )}
                    </span>
                  )}
                  <PickChip
                    prediction={p.prediction}
                    role={role}
                    pointsAwarded={p.pointsAwarded}
                    secondary={role !== "me"}
                  />
                </div>
              ))}
            </div>
          </div>
        ))}
      </div>
      {groups.some((g) => g.role === "pundit") && (
        <p className="border-t border-border px-4 py-2 text-[10px] text-muted-foreground">
          {PUNDIT_PARODY_DISCLAIMER} You&apos;ll see who each desk is parodying — pit your picks against
          their takes, not the real person.
        </p>
      )}
    </div>
  );
}
