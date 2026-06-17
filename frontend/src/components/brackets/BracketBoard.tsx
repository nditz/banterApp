"use client";

import { Lock } from "lucide-react";
import {
  useBracket,
  useSaveBracketPick,
  type BracketRound,
  type BracketSlot,
  type GroupStanding,
} from "@/hooks/useBrackets";
import { TermsAcceptPanel } from "@/components/session/TermsAcceptPanel";
import { BracketQualificationPanel } from "@/components/brackets/BracketQualificationPanel";
import { TeamLabel } from "@/components/brackets/TeamFlag";
import { useNeedsTerms } from "@/hooks/useNeedsTerms";
import { ApiError } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

function GroupStandingsTable({
  group,
  rows,
}: {
  group: string;
  rows: GroupStanding[];
}) {
  return (
    <div className="rounded-md border border-border bg-card p-2">
      <h3 className="mb-2 text-[11px] font-bold uppercase tracking-widest text-brand">
        Group {group}
      </h3>
      <table className="w-full text-[10px]">
        <thead>
          <tr className="text-left text-muted-foreground">
            <th className="pb-1 pr-1">#</th>
            <th className="pb-1">Team</th>
            <th className="pb-1 text-center">P</th>
            <th className="pb-1 text-center">GD</th>
            <th className="pb-1 text-center">Pts</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => (
            <tr key={row.teamCode} className={cn(
              row.rank <= 2 && "font-semibold text-foreground",
              row.rank === 3 && "text-muted-foreground"
            )}>
              <td className="py-0.5 pr-1">{row.rank}</td>
              <td className="py-0.5">
                <TeamLabel code={row.teamCode} name={row.teamName} compact />
              </td>
              <td className="py-0.5 text-center">{row.played}</td>
              <td className="py-0.5 text-center">{row.goalDifference}</td>
              <td className="py-0.5 text-center">{row.points}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function BracketMatch({
  slot,
  onPick,
  saving,
}: {
  slot: BracketSlot;
  onPick: (winnerCode: string) => void;
  saving: boolean;
}) {
  const teams = [slot.teamA, slot.teamB].filter(Boolean) as Array<{ code: string; name: string }>;

  return (
    <article
      className={cn(
        "min-w-[180px] rounded-md border bg-card p-2 shadow-sm",
        slot.isLocked ? "border-muted opacity-80" : "border-border",
        slot.kind === "GroupMatch" && "min-w-[200px]"
      )}
    >
      <div className="mb-2 flex items-center justify-between gap-2">
        <span className="text-[10px] font-semibold uppercase tracking-wide text-muted-foreground">
          {slot.kind === "GroupMatch" ? slot.round : slot.qualifierLabel ?? slot.round}
        </span>
        {slot.isLocked && (
          <span className="inline-flex items-center gap-1 text-[10px] text-muted-foreground">
            <Lock className="size-3" aria-hidden />
            Locked
          </span>
        )}
      </div>

      <div className="space-y-1">
        {!slot.ready ? (
          <p className="rounded-md border border-dashed border-border px-2 py-3 text-center text-[11px] text-muted-foreground">
            {slot.kind === "GroupMatch"
              ? "Pick all group matches to unlock knockouts"
              : "Qualified teams appear after group stage picks"}
          </p>
        ) : (
          teams.map((team) => {
            const selected = slot.pickedWinnerCode === team.code;
            return (
              <Button
                key={team.code}
                type="button"
                variant={selected ? "default" : "outline"}
                size="sm"
                disabled={slot.isLocked || saving || !slot.ready}
                className={cn(
                  "h-9 w-full justify-between px-2 text-xs",
                  selected && "btn-tournament"
                )}
                onClick={() => onPick(team.code)}
              >
                <TeamLabel code={team.code} name={team.name} selected={selected} />
              </Button>
            );
          })
        )}
      </div>

      {slot.kickoffTime && (
        <p className="mt-2 text-[10px] text-muted-foreground">
          {new Date(slot.kickoffTime).toLocaleString(undefined, {
            month: "short",
            day: "numeric",
            hour: "2-digit",
            minute: "2-digit",
          })}
        </p>
      )}
    </article>
  );
}

function KnockoutColumn({ round, onPick, saving }: {
  round: BracketRound;
  onPick: (slotId: string, code: string) => void;
  saving: boolean;
}) {
  return (
    <section className="flex w-[200px] flex-col gap-3">
      <h2 className="sticky top-0 z-10 bg-background/90 py-1 text-xs font-bold uppercase tracking-widest text-brand">
        {round.label}
      </h2>
      <div
        className="flex flex-col justify-around gap-3"
        style={{ minHeight: `${Math.max(420, round.slots.length * 88)}px` }}
      >
        {round.slots.map((slot) => (
          <BracketMatch
            key={slot.slotId}
            slot={slot}
            saving={saving}
            onPick={(code) => onPick(slot.slotId, code)}
          />
        ))}
      </div>
    </section>
  );
}

export function BracketBoard() {
  const { needsTerms, isLoading: sessionLoading } = useNeedsTerms();
  const { data, isLoading, error } = useBracket();
  const savePick = useSaveBracketPick();

  const termsRequired =
    needsTerms ||
    (error instanceof ApiError && (error.status === 401 || error.status === 403));

  if (sessionLoading || isLoading) {
    return <p className="text-sm text-muted-foreground">Loading bracket...</p>;
  }

  if (termsRequired) {
    return <TermsAcceptPanel className="max-w-lg" />;
  }

  if (error || !data) {
    return (
      <p className="text-sm text-muted-foreground">
        Could not load your bracket. Check that the API is running and refresh the page.
      </p>
    );
  }

  const groupRound = data.rounds.find((round) => round.phase === "group");
  const knockoutRounds = data.rounds.filter((round) => round.phase === "knockout");
  const groupSlotsByGroup = (groupRound?.slots ?? []).reduce<Record<string, BracketSlot[]>>(
    (acc, slot) => {
      const key = slot.round.replace("Group ", "").trim();
      acc[key] = acc[key] ?? [];
      acc[key].push(slot);
      return acc;
    },
    {}
  );

  const handlePick = (slotId: string, winnerCode: string) => {
    savePick.mutate({ slotId, winnerTeamCode: winnerCode });
  };

  return (
    <div className="space-y-6">
      {data.qualification && <BracketQualificationPanel qualification={data.qualification} />}

      <section>
        <h2 className="mb-3 text-sm font-bold text-foreground">Group stage predictions</h2>
        <p className="mb-4 text-xs text-muted-foreground">
          Pick every group match across 12 groups (A–L). Top two per group qualify automatically;
          the eight best third-place teams are ranked and placed via FIFA Annex C (see above).
        </p>

        <div className="mb-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
          {Object.entries(data.standings)
            .sort(([a], [b]) => a.localeCompare(b))
            .map(([group, rows]) => (
              <GroupStandingsTable key={group} group={group} rows={rows} />
            ))}
        </div>

        <div className="overflow-x-auto pb-2">
          <div className="flex min-w-max gap-4">
            {Object.entries(groupSlotsByGroup)
              .sort(([a], [b]) => a.localeCompare(b))
              .map(([group, slots]) => (
                <section key={group} className="flex w-[210px] flex-col gap-2">
                  <h3 className="text-[11px] font-bold uppercase tracking-widest text-brand">
                    Group {group}
                  </h3>
                  {slots.map((slot) => (
                    <BracketMatch
                      key={slot.slotId}
                      slot={slot}
                      saving={savePick.isPending}
                      onPick={(code) => handlePick(slot.slotId, code)}
                    />
                  ))}
                </section>
              ))}
          </div>
        </div>
      </section>

      <section>
        <h2 className="mb-3 text-sm font-bold text-foreground">Knockout bracket</h2>
        <p className="mb-4 text-xs text-muted-foreground">
          Round of 32 → Round of 16 → quarter-finals → semi-finals → Final (plus third-place play-off).
          Winners propagate round by round; changing an earlier pick clears downstream selections.
        </p>
        <div className="overflow-x-auto pb-2">
          <div className="flex min-w-max gap-4">
            {knockoutRounds.map((round) => (
              <KnockoutColumn
                key={round.order}
                round={round}
                saving={savePick.isPending}
                onPick={handlePick}
              />
            ))}
          </div>
        </div>
      </section>
    </div>
  );
}
