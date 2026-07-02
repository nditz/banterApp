"use client";

import { useMemo, useState } from "react";
import Image from "next/image";
import { Check, Copy, Download, Loader2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  type PunditScriptDuration,
  type PunditScriptPhase,
  type PunditScriptRequest,
  usePunditScript,
} from "@/hooks/usePunditScript";
import { useMatches, useMatchResults } from "@/hooks/useMatches";
import { PUNDIT_PARODY_DISCLAIMER, getPunditAvatarUrl } from "@/lib/pundits";
import { PUNDIT_PERSONAS } from "@/lib/pundit-personas";
import type { Match } from "@/lib/types";
import { cn } from "@/lib/utils";

const FINISHED_STATUSES = new Set(["FT", "FINISHED", "AET", "PEN", "FULL_TIME"]);

function isFinishedMatch(match: Match): boolean {
  if (match.status && FINISHED_STATUSES.has(match.status.toUpperCase())) return true;
  return match.homeScore != null && match.awayScore != null;
}

function formatMatchLabel(match: Match): string {
  const score =
    match.homeScore != null && match.awayScore != null
      ? ` (${match.homeScore}-${match.awayScore})`
      : "";
  return `${match.teamA} v ${match.teamB}${score}`;
}

interface PunditScriptGeneratorProps {
  className?: string;
}

export function PunditScriptGenerator({ className }: PunditScriptGeneratorProps) {
  const [phase, setPhase] = useState<PunditScriptPhase>("pre_match");
  const [styleSlug, setStyleSlug] = useState(PUNDIT_PERSONAS[0].styleSlug);
  const [duration, setDuration] = useState<PunditScriptDuration>(60);
  const [selectedMatchId, setSelectedMatchId] = useState<string>("");
  const [copied, setCopied] = useState(false);

  const { data: upcoming, isLoading: upcomingLoading } = useMatches();
  const { data: results, isLoading: resultsLoading } = useMatchResults();
  const { mutate, data, isPending, isError } = usePunditScript();

  const matches = useMemo(() => {
    const list = phase === "post_match" ? (results ?? []) : (upcoming ?? []);
    return list.filter((m) => (phase === "post_match" ? isFinishedMatch(m) : !isFinishedMatch(m)));
  }, [phase, upcoming, results]);

  const selectedMatch = useMemo(
    () => matches.find((m) => m.id === selectedMatchId) ?? matches[0],
    [matches, selectedMatchId]
  );

  const isLoading = phase === "post_match" ? resultsLoading : upcomingLoading;

  const handleGenerate = () => {
    if (!selectedMatch) return;
    const request: PunditScriptRequest = {
      matchId: selectedMatch.id,
      phase,
      styleSlug,
      duration,
      match: selectedMatch,
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
    if (!data?.content || !selectedMatch) return;
    const persona = PUNDIT_PERSONAS.find((p) => p.styleSlug === styleSlug);
    const slug = persona?.avatarSeed ?? "pundit";
    const teams = `${selectedMatch.teamA}-v-${selectedMatch.teamB}`.replace(/\s+/g, "-").toLowerCase();
    const blob = new Blob([data.content], { type: "text/plain" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = `balltakes-pundit-${slug}-${teams}.txt`;
    a.click();
    URL.revokeObjectURL(url);
  };

  return (
    <div className={cn("space-y-3", className)}>
      <div className="inline-flex rounded-md border border-border bg-muted/50 p-0.5">
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
            onClick={() => {
              setPhase(key);
              setSelectedMatchId("");
            }}
          >
            {label}
          </Button>
        ))}
      </div>

      <div>
        <p className="text-[11px] font-semibold">Choose a match</p>
        {isLoading ? (
          <p className="mt-1 text-[11px] text-muted-foreground">Loading fixtures…</p>
        ) : matches.length === 0 ? (
          <p className="mt-1 text-[11px] text-muted-foreground">
            {phase === "post_match"
              ? "No finished results yet — check back after full time."
              : "No upcoming fixtures right now."}
          </p>
        ) : (
          <select
            className="mt-1 w-full rounded-md border border-border bg-card px-2 py-1.5 text-[11px]"
            value={selectedMatch?.id ?? ""}
            onChange={(e) => setSelectedMatchId(e.target.value)}
          >
            {matches.map((m) => (
              <option key={m.id} value={m.id}>
                {formatMatchLabel(m)}
                {m.venue ? ` · ${m.venue}` : ""}
              </option>
            ))}
          </select>
        )}
      </div>

      <div>
        <p className="text-[11px] font-semibold">Pundit persona</p>
        <p className="text-[10px] text-muted-foreground">{PUNDIT_PARODY_DISCLAIMER}</p>
        <div className="mt-2 grid grid-cols-2 gap-2 sm:grid-cols-4">
          {PUNDIT_PERSONAS.map((persona) => (
            <button
              key={persona.styleSlug}
              type="button"
              onClick={() => setStyleSlug(persona.styleSlug)}
              className={cn(
                "flex flex-col items-center gap-1.5 rounded-lg border p-2 text-center transition-colors",
                styleSlug === persona.styleSlug
                  ? "border-gold bg-gold/10 ring-1 ring-gold/40"
                  : "border-border bg-card hover:border-gold/30"
              )}
            >
              <Image
                src={getPunditAvatarUrl(persona.avatarSeed, persona.name)}
                alt=""
                width={40}
                height={40}
                className="size-10 rounded-full bg-muted"
                unoptimized
              />
              <span className="text-[10px] font-semibold leading-tight">{persona.name}</span>
              <span className="text-[9px] text-muted-foreground leading-tight">{persona.archetype}</span>
            </button>
          ))}
        </div>
      </div>

      <div>
        <p className="text-[11px] font-semibold">Script length</p>
        <div className="mt-1 inline-flex rounded-md border border-border bg-muted/50 p-0.5">
          {([30, 60, 90] as const).map((d) => (
            <Button
              key={d}
              type="button"
              variant={duration === d ? "default" : "ghost"}
              size="sm"
              className="h-7 px-2.5 text-[11px]"
              onClick={() => setDuration(d)}
            >
              {d}s
            </Button>
          ))}
        </div>
      </div>

      <Button
        size="sm"
        className="btn-tournament h-8 w-full text-xs sm:w-auto"
        onClick={handleGenerate}
        disabled={isPending || isLoading || !selectedMatch}
      >
        {isPending ? (
          <Loader2 className="size-3.5 animate-spin" aria-hidden />
        ) : (
          "Generate pundit script"
        )}
      </Button>

      {data?.remainingGenerations != null && (
        <p className="text-[10px] text-muted-foreground">
          {data.remainingGenerations} AI generation{data.remainingGenerations === 1 ? "" : "s"} remaining
        </p>
      )}

      {data?.content && (
        <div className="space-y-2">
          <pre className="max-h-64 overflow-auto rounded-md border border-border bg-card p-2 text-[11px] leading-relaxed whitespace-pre-wrap">
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
        <p className="text-[11px] text-destructive">Could not reach API — try again.</p>
      )}
    </div>
  );
}
