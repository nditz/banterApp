import Link from "next/link";
import { comparisonPhase } from "@/lib/comparison-phase";
import { formatSourcePlatformLabel } from "@/lib/pundits";
import type { StudioMatchComparison, StudioPickEntry } from "@/lib/types";

interface MatchPunditComparisonProps {
  comparison?: StudioMatchComparison;
  filteringToFollows?: boolean;
}

export function MatchPunditComparison({
  comparison,
  filteringToFollows,
}: MatchPunditComparisonProps) {
  if (!comparison) {
    return null;
  }

  const you = comparison.picks.filter((p) => p.role === "me");
  const pundits = comparison.picks.filter((p) => p.role === "pundit");
  const phase = comparisonPhase(comparison);

  return (
    <div className="border-t border-border bg-muted/20 px-3.5 py-2.5">
      <div className="mb-1.5 flex flex-wrap items-center justify-between gap-1">
        <p className="text-[10px] font-bold uppercase tracking-[0.14em] text-muted-foreground">
          {phase === "after" ? "After full time" : "Before kickoff"} · you vs pundits
        </p>
        <Link href="/pundits" className="text-[10px] font-semibold text-foreground hover:underline">
          {filteringToFollows ? "Followed desks" : "Follow pundits"}
        </Link>
      </div>

      {you.length === 0 && pundits.length === 0 ? (
        <p className="text-[11px] text-muted-foreground">
          No sourced takes for this match yet.{" "}
          <Link href="/pundits" className="font-semibold text-foreground hover:underline">
            Follow pundits
          </Link>{" "}
          so their picks land here when ingest links them.
        </p>
      ) : (
        <div className="space-y-1.5">
          {you.length > 0 ? (
            <PickRow label="You" picks={you} phase={phase} />
          ) : (
            <p className="text-[11px] text-muted-foreground">Lock your pick above to compare.</p>
          )}
          {pundits.length > 0 ? (
            <PickRow label="Pundits" picks={pundits} phase={phase} />
          ) : (
            <p className="text-[11px] text-muted-foreground">
              {filteringToFollows
                ? "None of the desks you follow have a sourced pick for this fixture."
                : "No reviewed pundit pick is linked to this fixture yet."}
            </p>
          )}
          {comparison.actualResult ? (
            <p className="text-[11px] font-semibold text-pitch">Final {comparison.actualResult}</p>
          ) : null}
        </div>
      )}
    </div>
  );
}

function PickRow({
  label,
  picks,
  phase,
}: {
  label: string;
  picks: StudioPickEntry[];
  phase: "before" | "after";
}) {
  return (
    <div className="flex flex-wrap items-start gap-x-2 gap-y-1 text-[11px]">
      <span className="w-14 shrink-0 font-semibold text-muted-foreground">{label}</span>
      <div className="flex min-w-0 flex-1 flex-wrap gap-1.5">
        {picks.map((p, i) => (
          <span
            key={`${p.name}-${i}`}
            className="inline-flex max-w-full items-center gap-1 rounded-md border border-border bg-card px-1.5 py-0.5"
          >
            {label !== "You" ? (
              <span className="truncate font-medium text-foreground/90">{p.name}</span>
            ) : null}
            <span className="truncate text-foreground">{p.prediction}</span>
            {phase === "after" && p.wasCorrect != null ? (
              <span className={p.wasCorrect ? "text-pitch" : "text-muted-foreground"}>
                {p.wasCorrect ? "hit" : "miss"}
              </span>
            ) : null}
            {p.sourceUrl ? (
              <a
                href={p.sourceUrl}
                target="_blank"
                rel="noopener noreferrer"
                className="text-pitch underline-offset-2 hover:underline"
              >
                {formatSourcePlatformLabel(p.sourcePlatform) ?? "Source"}
              </a>
            ) : null}
          </span>
        ))}
      </div>
    </div>
  );
}
