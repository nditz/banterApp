"use client";

import { useMemo, useState } from "react";
import { Check, Copy, Download, Loader2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  type CumulativeScriptRequest,
  type PredictionPickSummary,
  type ScriptPhase,
  type ScriptStyle,
  useContentScript,
} from "@/hooks/useContentScript";
import { usePredictionHistory } from "@/hooks/usePredictions";
import type { Match, Prediction } from "@/lib/types";
import { cn } from "@/lib/utils";

const resultLabels: Record<string, string> = {
  home: "Home Win",
  draw: "Draw",
  away: "Away Win",
  home_or_draw: "Home or Draw",
  away_or_draw: "Away or Draw",
  home_or_away: "Home or Away",
};

const FINISHED_STATUSES = new Set(["FT", "FINISHED", "AET", "PEN", "FULL_TIME"]);

function isFinishedMatch(match?: Match): boolean {
  if (!match) return false;
  if (match.status && FINISHED_STATUSES.has(match.status.toUpperCase())) return true;
  return match.homeScore != null && match.awayScore != null;
}

function formatActualResult(match: Match): string {
  const home = match.homeScore ?? 0;
  const away = match.awayScore ?? 0;
  const outcome =
    home > away ? "Home Win" : home < away ? "Away Win" : "Draw";
  return `${match.teamA} ${home}-${away} ${match.teamB} (${outcome})`;
}

function formatPickValue(type: string, value: string): string {
  if (type === "result" || type === "double_chance") {
    return resultLabels[value] ?? value;
  }
  return value;
}

function toPickSummaries(predictions: Prediction[], phase: ScriptPhase): PredictionPickSummary[] {
  return predictions.map((p) => {
    const match = p.match;
    const teamA = match?.teamA ?? "Team A";
    const teamB = match?.teamB ?? "Team B";
    const base: PredictionPickSummary = {
      matchId: p.matchId,
      teamA,
      teamB,
      prediction: formatPickValue(p.predictionType, p.predictionValue),
      predictionType: p.predictionType,
    };

    if (phase === "post_match" && match && isFinishedMatch(match)) {
      return {
        ...base,
        actualResult: formatActualResult(match),
        pointsAwarded: p.pointsAwarded,
      };
    }
    return base;
  });
}

interface CumulativeScriptExportProps {
  className?: string;
  minimal?: boolean;
}

export function CumulativeScriptExport({ className, minimal = false }: CumulativeScriptExportProps) {
  const [phase, setPhase] = useState<ScriptPhase>("pre_match");
  const [style, setStyle] = useState<ScriptStyle>("full");
  const [copied, setCopied] = useState(false);
  const { data: predictions, isLoading } = usePredictionHistory();
  const { mutate, data, isPending, isError } = useContentScript();

  const picks = useMemo(() => {
    if (!predictions?.length) return [];
    if (phase === "post_match") {
      return toPickSummaries(
        predictions.filter((p) => isFinishedMatch(p.match)),
        "post_match"
      );
    }
    return toPickSummaries(predictions, "pre_match");
  }, [predictions, phase]);

  const styledPickCount = useMemo(() => {
    if (phase !== "post_match") return picks.length;
    if (style === "praise") return picks.filter((p) => (p.pointsAwarded ?? 0) > 0).length;
    if (style === "burn") return picks.filter((p) => (p.pointsAwarded ?? 0) <= 0).length;
    return picks.length;
  }, [picks, phase, style]);

  const handleExport = () => {
    const request: CumulativeScriptRequest = {
      phase,
      picks,
      style: phase === "post_match" ? style : undefined,
    };
    mutate(request);
  };

  const handleCopy = async () => {
    if (!data?.content) return;
    await navigator.clipboard.writeText(data.content);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  const handleDownload = () => {
    if (!data?.content) return;
    const blob = new Blob([data.content], { type: "text/plain" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = `banterapp-${phase}${phase === "post_match" ? `-${style}` : ""}.txt`;
    a.click();
    URL.revokeObjectURL(url);
  };

  return (
    <div className={cn(!minimal && "rounded-md border border-gold/30 bg-gold/5 p-3", className)}>
      <div className={cn("flex flex-col gap-2", !minimal && "sm:flex-row sm:items-start sm:justify-between")}>
        {!minimal && (
          <div>
            <p className="text-xs font-semibold">Export content script</p>
            <p className="mt-0.5 text-[11px] text-muted-foreground">
              Pundit-style pre-match or praise/burn post-match — {styledPickCount} pick
              {styledPickCount === 1 ? "" : "s"} in this export
            </p>
          </div>
        )}
        <div className={cn("inline-flex rounded-md border border-border bg-muted/50 p-0.5", minimal && "w-full sm:w-auto")}>
          {(
            [
              ["pre_match", "Pre-match"],
              ["post_match", "Post-match"],
            ] as const
          ).map(([key, label]) => (
            <Button
              key={key}
              type="button"
              variant={phase === key ? "default" : "ghost"}
              size="sm"
              className="h-7 px-2.5 text-[11px]"
              onClick={() => setPhase(key)}
            >
              {label}
            </Button>
          ))}
        </div>
      </div>

      {phase === "post_match" && (
        <div className="mt-2 inline-flex rounded-md border border-border bg-muted/50 p-0.5">
          {(
            [
              ["full", "Full recap"],
              ["praise", "Praise cut"],
              ["burn", "Burn cut"],
            ] as const
          ).map(([key, label]) => (
            <Button
              key={key}
              type="button"
              variant={style === key ? "default" : "ghost"}
              size="sm"
              className="h-7 px-2.5 text-[11px]"
              onClick={() => setStyle(key)}
            >
              {label}
            </Button>
          ))}
        </div>
      )}

      {!isLoading && picks.length > 0 && (
        <ul className="mt-2 max-h-24 space-y-1 overflow-y-auto rounded-md border border-border bg-card p-2 text-[11px] text-muted-foreground">
          {picks
            .filter((pick) => {
              if (phase !== "post_match") return true;
              if (style === "praise") return (pick.pointsAwarded ?? 0) > 0;
              if (style === "burn") return (pick.pointsAwarded ?? 0) <= 0;
              return true;
            })
            .map((pick, i) => (
              <li key={`${pick.teamA}-${pick.teamB}-${i}`}>
                {pick.teamA} v {pick.teamB}: <strong className="text-foreground">{pick.prediction}</strong>
                {phase === "post_match" && pick.pointsAwarded !== undefined && (
                  <span> · +{pick.pointsAwarded} pts</span>
                )}
              </li>
            ))}
        </ul>
      )}

      {!isLoading && styledPickCount === 0 && (
        <p className="mt-2 text-[11px] text-muted-foreground">
          {phase === "pre_match"
            ? "Make a prediction first — your pundit-style script drops every pick."
            : style === "praise"
              ? "No W's to flex yet. Full recap or burn cut still available when results land."
              : style === "burn"
                ? "No L's to roast — suspiciously clean or no finished games yet."
                : "No finished results yet. Check back after full time."}
        </p>
      )}

      <Button
        size="sm"
        className="btn-tournament mt-3 h-8 w-full text-xs sm:w-auto"
        onClick={handleExport}
        disabled={isPending || isLoading || styledPickCount === 0}
      >
        {isPending ? (
          <Loader2 className="size-3.5 animate-spin" aria-hidden />
        ) : phase === "pre_match" ? (
          "Export pre-match script"
        ) : style === "praise" ? (
          "Export praise script"
        ) : style === "burn" ? (
          "Export burn script"
        ) : (
          "Export full recap script"
        )}
      </Button>

      {data?.content && (
        <div className={cn("mt-3 space-y-2", minimal && "mt-2")}>
          <pre
            className={cn(
              "overflow-auto rounded-md border border-border bg-card p-2 text-[11px] leading-relaxed whitespace-pre-wrap",
              minimal ? "max-h-24" : "max-h-48"
            )}
          >
            {data.content}
          </pre>
          <div className="flex gap-2">
            <Button variant="outline" size="sm" className="h-7 flex-1 text-[11px]" onClick={handleCopy}>
              {copied ? <Check className="size-3.5" /> : <Copy className="size-3.5" />}
              {copied ? "Copied" : "Copy"}
            </Button>
            <Button variant="outline" size="sm" className="h-7 flex-1 text-[11px]" onClick={handleDownload}>
              <Download className="size-3.5" />
              Download
            </Button>
          </div>
        </div>
      )}

      {isError && (
        <p className="mt-2 text-[11px] text-destructive">Could not reach API — try again.</p>
      )}
    </div>
  );
}
