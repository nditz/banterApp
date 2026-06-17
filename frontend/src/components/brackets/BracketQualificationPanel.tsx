"use client";

import { TeamLabel } from "@/components/brackets/TeamFlag";
import type { BracketQualification } from "@/hooks/useBrackets";
import { cn } from "@/lib/utils";

interface BracketQualificationPanelProps {
  qualification: BracketQualification;
}

export function BracketQualificationPanel({ qualification }: BracketQualificationPanelProps) {
  const progress = `${qualification.groupsComplete}/${qualification.totalGroups}`;

  return (
    <section className="rounded-lg border border-border bg-muted/20 p-4">
      <h2 className="text-sm font-bold text-foreground">How Round of 32 teams are determined</h2>
      <p className="mt-2 text-xs leading-relaxed text-muted-foreground">
        {qualification.rulesSummary}
      </p>

      <div className="mt-3 grid gap-3 sm:grid-cols-2">
        <div className="rounded-md border border-border bg-card p-3">
          <h3 className="text-[11px] font-bold uppercase tracking-widest text-brand">
            Automatic qualifiers
          </h3>
          <p className="mt-1 text-xs text-muted-foreground">
            1st and 2nd in each of the 12 groups (24 teams) — filled from your group-stage picks
            or actual results.
          </p>
          <p className="mt-2 text-xs font-medium text-foreground">
            Groups complete: {progress}
          </p>
        </div>

        <div className="rounded-md border border-border bg-card p-3">
          <h3 className="text-[11px] font-bold uppercase tracking-widest text-brand">
            Best third-place ranking
          </h3>
          <ol className="mt-1 list-decimal space-y-0.5 pl-4 text-xs text-muted-foreground">
            {qualification.rankingCriteria.map((criterion) => (
              <li key={criterion}>{criterion}</li>
            ))}
          </ol>
        </div>
      </div>

      {qualification.thirdPlaceRanking.length > 0 && (
        <div className="mt-4 overflow-x-auto">
          <h3 className="mb-2 text-[11px] font-bold uppercase tracking-widest text-brand">
            Third-place league table
            {!qualification.isComplete && " (provisional)"}
          </h3>
          <table className="w-full min-w-[520px] text-[11px]">
            <thead>
              <tr className="text-left text-muted-foreground">
                <th className="pb-1 pr-2">#</th>
                <th className="pb-1 pr-2">Grp</th>
                <th className="pb-1">Team</th>
                <th className="pb-1 text-center">Pts</th>
                <th className="pb-1 text-center">GD</th>
                <th className="pb-1 text-center">GF</th>
                <th className="pb-1 text-center">R32</th>
              </tr>
            </thead>
            <tbody>
              {qualification.thirdPlaceRanking.map((row) => (
                  <tr
                    key={row.group}
                    className={cn(
                      row.qualified && "bg-pitch/10 font-semibold text-foreground",
                      !row.groupComplete && "opacity-60",
                      row.groupComplete &&
                        !row.qualified &&
                        row.rankAmongThirds > 0 &&
                        "text-muted-foreground"
                    )}
                  >
                    <td className="py-0.5 pr-2 tabular-nums">{row.rankAmongThirds || "—"}</td>
                    <td className="py-0.5 pr-2">{row.group}</td>
                    <td className="py-0.5">
                      <TeamLabel code={row.teamCode} name={row.teamName} compact />
                    </td>
                    <td className="py-0.5 text-center tabular-nums">{row.points}</td>
                    <td className="py-0.5 text-center tabular-nums">{row.goalDifference}</td>
                    <td className="py-0.5 text-center tabular-nums">{row.goalsFor}</td>
                    <td className="py-0.5 text-center">
                      {!row.groupComplete
                        ? "…"
                        : row.qualified
                          ? "✓"
                          : row.rankAmongThirds > 8
                            ? "—"
                            : "?"}
                    </td>
                  </tr>
                ))}
            </tbody>
          </table>
        </div>
      )}

      {qualification.isComplete && qualification.combinationKey && (
        <div className="mt-4 rounded-md border border-dashed border-border bg-card/60 p-3 text-xs">
          <p className="font-medium text-foreground">
            Annex C combination:{" "}
            <span className="font-mono text-brand">{qualification.combinationKey}</span>
          </p>
          {qualification.annexCSlotMapping && (
            <ul className="mt-2 grid gap-1 sm:grid-cols-2">
              {Object.entries(qualification.annexCSlotMapping).map(([slot, third]) => (
                <li key={slot} className="text-muted-foreground">
                  <span className="font-mono text-foreground">{slot}</span>
                  {" → "}
                  <span className="font-mono">{third ?? "TBD"}</span>
                </li>
              ))}
            </ul>
          )}
          {!qualification.annexCResolved && (
            <p className="mt-2 text-muted-foreground">
              Annex C lookup missing for this combination — check data file.
            </p>
          )}
        </div>
      )}

      {!qualification.isComplete && (
        <p className="mt-3 text-xs text-muted-foreground">
          Finish all group-stage picks to lock the eight third-place qualifiers and their Round of 32
          opponents (FIFA Annex C).
        </p>
      )}
    </section>
  );
}
