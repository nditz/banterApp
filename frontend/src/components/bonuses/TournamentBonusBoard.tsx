"use client";

import { useMemo, useState } from "react";
import Link from "next/link";
import { Lock, Sparkles } from "lucide-react";
import { Panel } from "@/components/ui/panel";
import { Button, buttonVariants } from "@/components/ui/button";
import {
  useSaveTournamentBonusPick,
  useTournamentBonuses,
  type TournamentBonusCategoryInfo,
} from "@/hooks/useTournamentBonuses";
import { ApiError, getApiErrorMessage } from "@/lib/api";
import { TermsAcceptPanel } from "@/components/session/TermsAcceptPanel";
import { useNeedsTerms } from "@/hooks/useNeedsTerms";
import { TOURNAMENT_BONUS_ELIGIBILITY } from "@/lib/scoring-rules";
import { cn } from "@/lib/utils";
import { PlayerPickCombobox } from "@/components/bonuses/PlayerPickCombobox";
import { TeamPickCombobox } from "@/components/bonuses/TeamPickCombobox";

const difficultyTone: Record<string, string> = {
  Expert: "bg-flare/15 text-flare",
  Hard: "bg-pitch/15 text-pitch",
  Tricky: "bg-gold/15 text-gold",
};

function slotOrdinal(index: number, total: number): string {
  if (total <= 1) return "";
  if (total === 4) return ["1st", "2nd", "3rd", "4th"][index] ?? `Slot ${index + 1}`;
  if (total === 3) return ["1st", "2nd", "3rd"][index] ?? `Slot ${index + 1}`;
  return `Pick ${index + 1}`;
}

function BonusCategoryCard({
  category,
  teams,
  isLocked,
  canEdit,
  onSaved,
}: {
  category: TournamentBonusCategoryInfo;
  teams: Array<{ code: string; name: string }>;
  isLocked: boolean;
  canEdit: boolean;
  onSaved: () => void;
}) {
  const savePick = useSaveTournamentBonusPick();
  const slotCount = Math.max(1, category.slotCount ?? 1);
  const savedPicks = category.picks ?? (category.pick ? [category.pick] : []);
  const [values, setValues] = useState<string[]>(() =>
    Array.from({ length: slotCount }, (_, i) => savedPicks.find((p) => (p.slotIndex ?? 0) === i)?.pickValue ?? "")
  );
  const [error, setError] = useState<string | null>(null);
  const [savingSlot, setSavingSlot] = useState<number | null>(null);

  const ruleMeta = useMemo(() => {
    const labels: Record<string, string> = {
      player_of_the_season: "Expert",
      player_of_tournament: "Expert",
      league_winner: "Expert",
      golden_boot: "Hard",
      top_scorer: "Hard",
      most_assists: "Hard",
      top_assist: "Hard",
      golden_glove: "Hard",
      young_player_of_the_season: "Hard",
      top_four: "Hard",
      relegated: "Hard",
      surprise_team: "Tricky",
      surprise_package: "Tricky",
    };
    return labels[category.category];
  }, [category.category]);

  const handleSave = async (slotIndex: number) => {
    setError(null);
    const value = values[slotIndex]?.trim() ?? "";
    if (!value) {
      setError("Enter a pick before saving.");
      return;
    }

    try {
      setSavingSlot(slotIndex);
      await savePick.mutateAsync({
        category: category.category,
        pickValue: value,
        slotIndex,
      });
      onSaved();
    } catch (err) {
      setError(getApiErrorMessage(err));
    } finally {
      setSavingSlot(null);
    }
  };

  const teamNameByCode = useMemo(
    () => new Map(teams.map((t) => [t.code, t.name])),
    [teams]
  );
  const hasOfficial = Boolean(category.officialResult);
  const pointsLabel = slotCount > 1 ? `+${category.points} each` : `+${category.points}`;

  return (
    <article className="rounded-md border border-border bg-muted/20 p-3 sm:p-4">
      <div className="flex items-start justify-between gap-3">
        <div>
          <div className="flex flex-wrap items-center gap-2">
            <h3 className="text-sm font-semibold">{category.label}</h3>
            <span
              className={cn(
                "rounded-md px-1.5 py-0.5 text-[10px] font-bold uppercase tracking-wide",
                difficultyTone[ruleMeta] ?? "bg-muted text-muted-foreground"
              )}
            >
              {ruleMeta}
            </span>
          </div>
          <p className="mt-1 text-xs text-muted-foreground">{category.description}</p>
        </div>
        <span className="shrink-0 rounded-md bg-gold/15 px-2 py-1 text-xs font-bold text-gold">
          {pointsLabel}
        </span>
      </div>

      <div className="mt-3 space-y-3">
        {Array.from({ length: slotCount }, (_, slotIndex) => {
          const saved = savedPicks.find((p) => (p.slotIndex ?? 0) === slotIndex);
          const savedValue = saved?.pickValue;
          const savedDisplay =
            savedValue && category.isTeamPick
              ? teamNameByCode.get(savedValue) ?? savedValue
              : savedValue;
          const isCorrect = hasOfficial && saved && saved.pointsAwarded > 0;
          const ordinal = slotOrdinal(slotIndex, slotCount);
          const value = values[slotIndex] ?? "";
          const pending = savingSlot === slotIndex;

          return (
            <div key={slotIndex} className="space-y-2">
              {slotCount > 1 && (
                <p className="text-[10px] font-bold uppercase tracking-wide text-muted-foreground">
                  {ordinal}
                </p>
              )}
              {category.isTeamPick ? (
                <TeamPickCombobox
                  value={value}
                  onChange={(next) =>
                    setValues((current) => current.map((item, i) => (i === slotIndex ? next : item)))
                  }
                  teams={teams}
                  disabled={!canEdit || isLocked || savePick.isPending}
                  ariaLabel={`${category.label}${ordinal ? ` ${ordinal}` : ""} team pick`}
                />
              ) : (
                <PlayerPickCombobox
                  value={value}
                  onChange={(next) =>
                    setValues((current) => current.map((item, i) => (i === slotIndex ? next : item)))
                  }
                  teams={teams}
                  disabled={!canEdit || isLocked || savePick.isPending}
                  ariaLabel={`${category.label}${ordinal ? ` ${ordinal}` : ""} player pick`}
                />
              )}

              {savedValue && (
                <p className="text-[11px] text-muted-foreground">
                  Saved: <span className="font-medium text-foreground">{savedDisplay}</span>
                  {saved && saved.pointsAwarded > 0 && (
                    <span className="ml-1 text-pitch">(+{saved.pointsAwarded} pts)</span>
                  )}
                </p>
              )}

              {canEdit && !isLocked && (
                <Button
                  size="sm"
                  className="h-8 text-xs"
                  onClick={() => void handleSave(slotIndex)}
                  disabled={pending || value.trim() === (savedValue ?? "")}
                >
                  {pending ? "Saving…" : savedValue ? "Update pick" : "Save pick"}
                </Button>
              )}

              {hasOfficial && slotIndex === slotCount - 1 && (
                <p
                  className={cn(
                    "text-[11px] font-medium",
                    isCorrect ? "text-pitch" : "text-muted-foreground"
                  )}
                >
                  Official: {category.officialResult?.answerDisplay ?? category.officialResult?.answerValue}
                  {isCorrect ? " — on the board" : savedValue ? " — missed this slot" : ""}
                </p>
              )}
            </div>
          );
        })}

        {error && <p className="text-xs text-destructive">{error}</p>}
      </div>
    </article>
  );
}

export function TournamentBonusBoard({ embedded = false }: { embedded?: boolean }) {
  const { needsTerms, isLoading: sessionLoading } = useNeedsTerms();
  const { data, isLoading, isError, error, refetch } = useTournamentBonuses();
  const [savedFlash, setSavedFlash] = useState(false);

  const termsRequired =
    needsTerms ||
    (error instanceof ApiError && (error.status === 401 || error.status === 403));

  const totalPossible =
    data?.categories.reduce((sum, c) => sum + c.points * Math.max(1, c.slotCount ?? 1), 0) ?? 0;
  const totalEarned =
    data?.categories.reduce((sum, c) => {
      const picks = c.picks ?? (c.pick ? [c.pick] : []);
      return sum + picks.reduce((slotSum, pick) => slotSum + (pick.pointsAwarded ?? 0), 0);
    }, 0) ?? 0;

  const content = (
    <>
      {(sessionLoading || isLoading) && (
        <p className="text-sm text-muted-foreground">Loading season awards…</p>
      )}

      {termsRequired && !sessionLoading && (
        <TermsAcceptPanel className="max-w-lg" />
      )}

      {isError && !termsRequired && (
        <p className="text-sm text-muted-foreground">
          Could not load bonus picks. Make sure the backend is running and refresh the page.
        </p>
      )}

      {data && !termsRequired && (
        <div className="space-y-4">
          {!data.isEligible && (
            <div className="rounded-md border border-gold/30 bg-gold/5 p-3 text-sm">
              <p className="font-semibold text-gold">Award points on leaderboards</p>
              <p className="mt-1 text-xs text-muted-foreground">
                You can save season awards below anytime before the first kickoff. Points only
                count toward private leagues that meet these rules:
              </p>
              <ul className="mt-2 list-inside list-disc space-y-1 text-xs text-muted-foreground">
                {data.ineligibilityReasons.map((reason) => (
                  <li key={reason}>{reason}</li>
                ))}
              </ul>
              <Link
                href="/leagues"
                className={cn(buttonVariants({ variant: "outline", size: "sm" }), "mt-3 h-8 text-xs")}
              >
                Manage leagues
              </Link>
            </div>
          )}

          {data.isLocked && (
            <p className="flex items-center gap-2 text-xs text-muted-foreground">
              <Lock className="size-3.5 shrink-0" aria-hidden />
              Season awards locked at the first Premier League kickoff.
            </p>
          )}

          {data.canPick && (
            <p className="flex items-center gap-2 text-xs text-muted-foreground">
              <Sparkles className="size-3.5 shrink-0 text-gold" aria-hidden />
              Up to +{totalPossible} bonus points in qualifying private leagues
              {totalEarned > 0 ? ` · you've earned +${totalEarned} so far` : ""}.
            </p>
          )}

          <div className="grid gap-3 sm:grid-cols-2">
            {data.categories.map((category) => (
              <BonusCategoryCard
                key={category.category}
                category={category}
                teams={data.teams}
                isLocked={data.isLocked}
                canEdit={data.canPick}
                onSaved={() => {
                  setSavedFlash(true);
                  void refetch();
                  window.setTimeout(() => setSavedFlash(false), 2000);
                }}
              />
            ))}
          </div>

          {savedFlash && (
            <p className="text-xs font-medium text-pitch">Award pick saved.</p>
          )}

          <p className="text-[11px] leading-relaxed text-muted-foreground">
            {TOURNAMENT_BONUS_ELIGIBILITY.summary}
          </p>
        </div>
      )}
    </>
  );

  if (embedded) {
    return content;
  }

  return (
    <Panel
      title="Season awards"
      subtitle="Big-point awards for private leagues"
      accent="gold"
    >
      {content}
    </Panel>
  );
}
