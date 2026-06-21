"use client";

import { useMemo, useState } from "react";
import { Lock } from "lucide-react";
import { PredictionCelebration } from "@/components/prediction/PredictionCelebration";
import { ScoreCounter } from "@/components/prediction/ScoreCounter";
import { useAura } from "@/hooks/useAura";
import { useBanterMode } from "@/hooks/useBanterMode";
import { useMyLeagues } from "@/hooks/useLeaderboard";
import { usePredictions } from "@/hooks/usePredictions";
import { addLocalBanterEntry, buildBanterLine } from "@/lib/banterFeed";
import {
  estimateFixtureProbabilities,
  formatPickOddsHint,
  formatProbabilityContext,
} from "@/lib/predictionProbabilities";
import type { PredictionOutcome } from "@/lib/reactionEngine";
import {
  formatPickLabel,
  getPreMatchReactionBundle,
} from "@/lib/predictionReactions";
import type { PredictionReaction } from "@/lib/reactionEngine";
import { Button } from "@/components/ui/button";
import { TeamFlag } from "@/components/brackets/TeamFlag";
import type { Prediction } from "@/lib/types";
import { cn } from "@/lib/utils";

interface PredictionButtonsProps {
  matchId: string;
  teamA: string;
  teamB: string;
  teamACode?: string;
  teamBCode?: string;
  isLocked?: boolean;
  existingPredictions?: Prediction[];
  selectedValue?: string | null;
  onSelect?: (value: string) => void;
}

type Mode = "result" | "correct_score" | "double_chance";

interface SavedReactionState {
  reaction: PredictionReaction;
  supplementalReactions: PredictionReaction[];
  pickLabel: string;
  probabilityContext: string;
  leagueName?: string;
}

function doubleChanceOptions(teamA: string, teamB: string) {
  const shortA = teamA.split(" ").pop() ?? teamA;
  const shortB = teamB.split(" ").pop() ?? teamB;
  return [
    {
      value: "home_or_draw",
      label: `${shortA} or Draw`,
      hint: "Home win or draw both count",
    },
    {
      value: "away_or_draw",
      label: `${shortB} or Draw`,
      hint: "Away win or draw both count",
    },
    {
      value: "home_or_away",
      label: `${shortA} or ${shortB}`,
      hint: "No draw — either team wins",
    },
  ];
}

function parseScoreline(value: string): [number, number] | null {
  const match = /^(\d+)-(\d+)$/.exec(value.trim());
  if (!match) return null;
  return [Number(match[1]), Number(match[2])];
}

interface CorrectScoreSectionProps {
  teamA: string;
  teamB: string;
  savedScore: string | null;
  isSaving: boolean;
  onSubmit: (value: string) => void;
}

function CorrectScoreSection({
  teamA,
  teamB,
  savedScore,
  isSaving,
  onSubmit,
}: CorrectScoreSectionProps) {
  const parsed = savedScore ? parseScoreline(savedScore) : null;
  const [homeScore, setHomeScore] = useState(() => parsed?.[0] ?? 0);
  const [awayScore, setAwayScore] = useState(() => parsed?.[1] ?? 0);

  return (
    <div className="space-y-3">
      <ScoreCounter
        label={teamA.split(" ").pop() ?? teamA}
        value={homeScore}
        onChange={setHomeScore}
      />
      <ScoreCounter
        label={teamB.split(" ").pop() ?? teamB}
        value={awayScore}
        onChange={setAwayScore}
      />
      <div className="flex items-center justify-between border-t border-border pt-2">
        <span className="font-display text-lg font-bold tabular-nums text-foreground">
          {homeScore} – {awayScore}
        </span>
        <Button
          type="button"
          size="sm"
          className="btn-tournament h-8 cursor-pointer text-xs"
          disabled={isSaving}
          onClick={() => onSubmit(`${homeScore}-${awayScore}`)}
        >
          Lock it in (+7)
        </Button>
      </div>
    </div>
  );
}

export function PredictionButtons({
  matchId,
  teamA,
  teamB,
  teamACode,
  teamBCode,
  isLocked = false,
  existingPredictions = [],
  selectedValue,
  onSelect,
}: PredictionButtonsProps) {
  const [mode, setMode] = useState<Mode>("result");
  const [savedReaction, setSavedReaction] = useState<SavedReactionState | null>(null);
  const [justSelected, setJustSelected] = useState<string | null>(null);
  const { savePrediction, isSaving } = usePredictions();

  const existingByType = useMemo(() => {
    const map = new Map<Mode, Prediction>();
    for (const prediction of existingPredictions) {
      map.set(prediction.predictionType, prediction);
    }
    return map;
  }, [existingPredictions]);

  const savedValueForMode = (type: Mode) => existingByType.get(type)?.predictionValue ?? null;

  const { award } = useAura();
  const banterMode = useBanterMode();
  const { data: myLeagues } = useMyLeagues();

  const dcOptions = doubleChanceOptions(teamA, teamB);
  const leagueName = myLeagues?.leagues[0]?.name;
  const probabilities = useMemo(
    () => estimateFixtureProbabilities(matchId, teamA, teamB),
    [matchId, teamA, teamB]
  );

  const showReaction = (value: string, type: Mode) => {
    try {
      const probabilities = estimateFixtureProbabilities(matchId, teamA, teamB);
      const bundle = getPreMatchReactionBundle({
        fixtureId: matchId,
        homeTeamName: teamA,
        awayTeamName: teamB,
        predictionType: type,
        predictionValue: value,
        tone: banterMode,
        probabilities,
      });

      if (!bundle) return;

      const { primary: reaction, supplemental: supplementalReactions } = bundle;

      const pickLabel = formatPickLabel(type, value, teamA, teamB);
      const probabilityContext = formatProbabilityContext(probabilities, teamA, teamB);
      award(reaction.auraDelta);
      for (const bonus of supplementalReactions) {
        award(Math.round(bonus.auraDelta * 0.25));
      }
      addLocalBanterEntry({
        pick: pickLabel,
        fixture: `${teamA} vs ${teamB}`,
        line: buildBanterLine(reaction.key, pickLabel),
        emoji: reaction.emoji.split("")[0] ?? "⚽",
        reactionKey: reaction.key,
        reactionAsset: reaction.asset,
      });

      setSavedReaction({
        reaction,
        supplementalReactions,
        pickLabel,
        probabilityContext,
        leagueName,
      });
    } catch {
      // Reactions are decorative — never block the saved pick flow.
    }
  };

  const handleSubmit = async (value: string, type: Mode) => {
    const previousValue = savedValueForMode(type) ?? selectedValue;
    onSelect?.(value);
    setJustSelected(value);

    showReaction(value, type);

    try {
      await savePrediction({
        matchId,
        predictionType: type,
        predictionValue: value,
      });
    } catch {
      if (previousValue) onSelect?.(previousValue);
      setJustSelected(null);
    }
  };

  const activeValue = savedValueForMode(mode) ?? selectedValue;

  return (
    <div className="space-y-2.5">
      {isLocked ? (
        <p className="flex items-center gap-1.5 rounded-lg border border-dashed border-border bg-muted/40 px-3 py-2.5 text-xs text-muted-foreground">
          <Lock className="size-3.5 shrink-0" aria-hidden />
          Receipts closed — this match has kicked off.
        </p>
      ) : (
        <>
          <div
            className="inline-flex rounded-lg border border-border bg-muted/40 p-0.5"
            role="tablist"
            aria-label="Prediction type"
          >
            {(
              [
                ["result", "Result"],
                ["correct_score", "Scoreline"],
                ["double_chance", "Double"],
              ] as const
            ).map(([key, label]) => (
              <Button
                key={key}
                type="button"
                variant={mode === key ? "default" : "ghost"}
                size="sm"
                onClick={() => setMode(key)}
                role="tab"
                aria-selected={mode === key}
                className={cn(
                  "h-7 cursor-pointer px-2.5 text-xs",
                  mode === key && "bg-pitch text-pitch-foreground shadow-sm"
                )}
              >
                {label}
              </Button>
            ))}
          </div>

          {mode === "result" && (
            <div className="grid grid-cols-1 gap-1.5 min-[360px]:grid-cols-3" role="group" aria-label="Match result">
              {(
                [
                  ["home", teamA, "+3", "home" as PredictionOutcome],
                  ["draw", "Draw", "+3", "draw" as PredictionOutcome],
                  ["away", teamB, "+3", "away" as PredictionOutcome],
                ] as const
              ).map(([value, label, points, outcome]) => {
                const isSelected = activeValue === value;
                const isJustSelected = justSelected === value;
                const flagCode =
                  value === "home" ? teamACode : value === "away" ? teamBCode : undefined;
                const oddsHint = formatPickOddsHint(
                  outcome,
                  probabilities[outcome],
                  teamA,
                  teamB
                );
                return (
                  <button
                    key={value}
                    type="button"
                    disabled={isSaving}
                    onClick={() => handleSubmit(value, "result")}
                    className={cn(
                      "pick-btn flex h-auto min-h-11 flex-col gap-0 py-2 leading-tight sm:min-h-10",
                      isSelected && "pick-btn-selected",
                      isJustSelected && "pick-btn-just-selected"
                    )}
                  >
                    <span className="flex items-center justify-center gap-1.5 font-semibold">
                      {flagCode && (
                        <TeamFlag code={flagCode} name={label} />
                      )}
                      <span className="line-clamp-2">{label}</span>
                    </span>
                    <span className="text-[10px] font-normal opacity-70">{points}</span>
                    <span className="mt-0.5 line-clamp-2 px-1 text-xs font-normal leading-tight opacity-60 sm:text-[9px]">
                      {oddsHint}
                    </span>
                  </button>
                );
              })}
            </div>
          )}

          {mode === "correct_score" && (
            <CorrectScoreSection
              key={savedValueForMode("correct_score") ?? "new"}
              teamA={teamA}
              teamB={teamB}
              savedScore={savedValueForMode("correct_score")}
              isSaving={isSaving}
              onSubmit={(value) => handleSubmit(value, "correct_score")}
            />
          )}

          {mode === "double_chance" && (
            <div
              className="flex flex-col gap-2"
              role="radiogroup"
              aria-label="Double chance prediction"
            >
              {dcOptions.map((option) => {
                const isSelected = activeValue === option.value;
                const isJustSelected = justSelected === option.value;
                return (
                  <button
                    key={option.value}
                    type="button"
                    role="radio"
                    aria-checked={isSelected}
                    disabled={isSaving}
                    onClick={() => handleSubmit(option.value, "double_chance")}
                    className={cn(
                      "pick-btn flex w-full items-center justify-between gap-3 px-3 py-2.5 text-left",
                      isSelected && "pick-btn-selected",
                      isJustSelected && "pick-btn-just-selected"
                    )}
                  >
                    <span className="min-w-0 flex-1">
                      <span className="block text-xs font-semibold leading-snug text-foreground">
                        {option.label}
                      </span>
                      <span className="mt-0.5 block text-[10px] leading-snug text-muted-foreground">
                        {option.hint}
                      </span>
                    </span>
                    <span className="shrink-0 rounded-md bg-muted/80 px-2 py-0.5 text-[10px] font-bold text-muted-foreground">
                      +2
                    </span>
                  </button>
                );
              })}
            </div>
          )}

          {savedReaction && (
            <PredictionCelebration
              key={`${matchId}-${savedReaction.pickLabel}`}
              reaction={savedReaction.reaction}
              supplementalReactions={savedReaction.supplementalReactions}
              fixture={`${teamA} vs ${teamB}`}
              pick={savedReaction.pickLabel}
              probabilityContext={savedReaction.probabilityContext}
              leagueName={savedReaction.leagueName}
            />
          )}
        </>
      )}
    </div>
  );
}
