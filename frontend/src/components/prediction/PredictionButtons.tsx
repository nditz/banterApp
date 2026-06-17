"use client";

import { useEffect, useMemo, useState } from "react";
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
  formatProbabilityContext,
} from "@/lib/predictionProbabilities";
import {
  formatPickLabel,
  getPreMatchReaction,
} from "@/lib/predictionReactions";
import type { PredictionReaction } from "@/lib/reactionEngine";
import { Button } from "@/components/ui/button";
import type { Prediction } from "@/lib/types";
import { cn } from "@/lib/utils";

interface PredictionButtonsProps {
  matchId: string;
  teamA: string;
  teamB: string;
  isLocked?: boolean;
  existingPredictions?: Prediction[];
  selectedValue?: string | null;
  onSelect?: (value: string) => void;
  userDisplayName?: string;
}

type Mode = "result" | "correct_score" | "double_chance";

interface SavedReactionState {
  reaction: PredictionReaction;
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

export function PredictionButtons({
  matchId,
  teamA,
  teamB,
  isLocked = false,
  existingPredictions = [],
  selectedValue,
  onSelect,
  userDisplayName = "You",
}: PredictionButtonsProps) {
  const [mode, setMode] = useState<Mode>("result");
  const [homeScore, setHomeScore] = useState(0);
  const [awayScore, setAwayScore] = useState(0);
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

  useEffect(() => {
    const savedScore = savedValueForMode("correct_score");
    if (!savedScore) return;
    const parsed = parseScoreline(savedScore);
    if (parsed) {
      setHomeScore(parsed[0]);
      setAwayScore(parsed[1]);
    }
  }, [existingByType]);
  const { award } = useAura();
  const banterMode = useBanterMode();
  const { data: myLeagues } = useMyLeagues();

  const dcOptions = doubleChanceOptions(teamA, teamB);
  const leagueName = myLeagues?.leagues[0]?.name;

  const showReaction = (value: string, type: Mode) => {
    try {
      const probabilities = estimateFixtureProbabilities(matchId, teamA, teamB);
      const reaction = getPreMatchReaction({
        fixtureId: matchId,
        homeTeamName: teamA,
        awayTeamName: teamB,
        predictionType: type,
        predictionValue: value,
        tone: banterMode,
        probabilities,
      });

      if (!reaction) return;

      const pickLabel = formatPickLabel(type, value, teamA, teamB);
      const probabilityContext = formatProbabilityContext(probabilities, teamA, teamB);
      award(reaction.auraDelta);
      addLocalBanterEntry({
        name: userDisplayName,
        pick: pickLabel,
        fixture: `${teamA} vs ${teamB}`,
        line: buildBanterLine(reaction.key, userDisplayName, pickLabel),
        emoji: reaction.emoji.split("")[0] ?? "⚽",
      });

      setSavedReaction({
        reaction,
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
            <div className="grid grid-cols-3 gap-1.5" role="group" aria-label="Match result">
              {(
                [
                  ["home", teamA, "+3"],
                  ["draw", "Draw", "+3"],
                  ["away", teamB, "+3"],
                ] as const
              ).map(([value, label, points]) => {
                const isSelected = activeValue === value;
                const isJustSelected = justSelected === value;
                return (
                  <button
                    key={value}
                    type="button"
                    disabled={isSaving}
                    onClick={() => handleSubmit(value, "result")}
                    className={cn(
                      "pick-btn flex h-auto min-h-10 flex-col gap-0 py-2 leading-tight",
                      isSelected && "pick-btn-selected",
                      isJustSelected && "pick-btn-just-selected"
                    )}
                  >
                    <span className="line-clamp-2 font-semibold">{label}</span>
                    <span className="text-[10px] font-normal opacity-70">{points}</span>
                  </button>
                );
              })}
            </div>
          )}

          {mode === "correct_score" && (
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
                  onClick={() =>
                    handleSubmit(`${homeScore}-${awayScore}`, "correct_score")
                  }
                >
                  Lock it in (+7)
                </Button>
              </div>
            </div>
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
              userName={userDisplayName}
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
