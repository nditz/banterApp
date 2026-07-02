"use client";

import { useMemo, useState } from "react";
import Link from "next/link";
import { Lock, Sparkles } from "lucide-react";
import { Panel } from "@/components/ui/panel";
import { Button, buttonVariants } from "@/components/ui/button";
import {
  useSaveTournamentBonusPick,
  useTournamentBonuses,
  type TournamentBonusCategoryId,
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
  Tricky: "bg-gold/15 text-gold-foreground",
};

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
  const [value, setValue] = useState(category.pick?.pickValue ?? "");
  const [error, setError] = useState<string | null>(null);

  const ruleMeta = useMemo(() => {
    const labels: Record<TournamentBonusCategoryId, string> = {
      player_of_tournament: "Expert",
      top_scorer: "Hard",
      top_assist: "Hard",
      golden_glove: "Hard",
      surprise_package: "Tricky",
    };
    return labels[category.category];
  }, [category.category]);

  const handleSave = async () => {
    setError(null);
    if (!value.trim()) {
      setError("Enter a pick before saving.");
      return;
    }

    try {
      await savePick.mutateAsync({
        category: category.category,
        pickValue: value.trim(),
      });
      onSaved();
    } catch (err) {
      setError(getApiErrorMessage(err));
    }
  };

  const savedValue = category.pick?.pickValue;
  const teamNameByCode = useMemo(
    () => new Map(teams.map((t) => [t.code, t.name])),
    [teams]
  );
  const savedDisplay =
    savedValue && category.isTeamPick
      ? teamNameByCode.get(savedValue) ?? savedValue
      : savedValue;
  const hasOfficial = Boolean(category.officialResult);
  const isCorrect =
    hasOfficial &&
    category.pick &&
    category.pick.pointsAwarded > 0;

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
        <span className="shrink-0 rounded-md bg-gold/15 px-2 py-1 text-xs font-bold text-gold-foreground">
          +{category.points}
        </span>
      </div>

      <div className="mt-3 space-y-2">
        {category.isTeamPick ? (
          <TeamPickCombobox
            value={value}
            onChange={setValue}
            teams={teams}
            disabled={!canEdit || isLocked || savePick.isPending}
            ariaLabel={`${category.label} team pick`}
          />
        ) : (
          <PlayerPickCombobox
            value={value}
            onChange={setValue}
            teams={teams}
            disabled={!canEdit || isLocked || savePick.isPending}
            ariaLabel={`${category.label} player pick`}
          />
        )}

        {savedValue && (
          <p className="text-[11px] text-muted-foreground">
            Saved: <span className="font-medium text-foreground">{savedDisplay}</span>
            {category.pick && category.pick.pointsAwarded > 0 && (
              <span className="ml-1 text-pitch">(+{category.pick.pointsAwarded} pts)</span>
            )}
          </p>
        )}

        {hasOfficial && (
          <p
            className={cn(
              "text-[11px] font-medium",
              isCorrect ? "text-pitch" : "text-muted-foreground"
            )}
          >
            Official: {category.officialResult?.answerDisplay ?? category.officialResult?.answerValue}
            {isCorrect ? " — correct!" : savedValue ? " — missed" : ""}
          </p>
        )}

        {error && <p className="text-xs text-destructive">{error}</p>}

        {canEdit && !isLocked && (
          <Button
            size="sm"
            className="h-8 text-xs"
            onClick={handleSave}
            disabled={savePick.isPending || value.trim() === (savedValue ?? "")}
          >
            {savePick.isPending ? "Saving…" : savedValue ? "Update pick" : "Save pick"}
          </Button>
        )}
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

  const totalPossible = data?.categories.reduce((sum, c) => sum + c.points, 0) ?? 0;
  const totalEarned =
    data?.categories.reduce((sum, c) => sum + (c.pick?.pointsAwarded ?? 0), 0) ?? 0;

  const content = (
    <>
      {(sessionLoading || isLoading) && (
        <p className="text-sm text-muted-foreground">Loading tournament bonus picks…</p>
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
              <p className="font-semibold text-gold-foreground">Bonus points on leaderboards</p>
              <p className="mt-1 text-xs text-muted-foreground">
                You can save tournament bonus picks below anytime before kickoff. Points only
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
              Bonus picks locked at tournament kickoff.
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
            <p className="text-xs font-medium text-pitch">Bonus pick saved.</p>
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
      title="Tournament bonus picks"
      subtitle="Big-point awards for private leagues"
      accent="gold"
    >
      {content}
    </Panel>
  );
}
