"use client";

import Image from "next/image";
import { getFlagUrl } from "@/lib/team-flags";
import { cn } from "@/lib/utils";

/** Default crest / flag width. Club crests render square; country flags stay 3:2. */
export const TEAM_FLAG_WIDTH = 28;
const FLAG_ASPECT = 2 / 3;

export function teamFlagHeight(width: number = TEAM_FLAG_WIDTH, square = false): number {
  return square ? width : Math.round(width * FLAG_ASPECT);
}

interface TeamFlagProps {
  code: string;
  name?: string;
  logoUrl?: string;
  size?: number;
  className?: string;
}

export function TeamFlag({ code, name, logoUrl, size = TEAM_FLAG_WIDTH, className }: TeamFlagProps) {
  const isCrest = Boolean(logoUrl);
  const url = logoUrl || getFlagUrl(code, 80, name);
  const width = size;
  const height = teamFlagHeight(size, isCrest);

  if (!url) {
    return (
      <span
        className={cn(
          "team-flag inline-flex shrink-0 items-center justify-center bg-muted font-mono text-[9px] font-bold text-muted-foreground ring-1 ring-border/60",
          isCrest ? "rounded-full" : "rounded-md",
          className
        )}
        style={{ width, height, minWidth: width, minHeight: height }}
        title={name ?? code}
      >
        {(code || "?").slice(0, 3)}
      </span>
    );
  }

  return (
    <span
      className={cn(
        "team-flag relative inline-block shrink-0 overflow-hidden ring-1 ring-white/10",
        isCrest ? "rounded-full bg-white p-0.5" : "rounded-md",
        className
      )}
      style={{ width, height, minWidth: width, minHeight: height }}
      title={name ?? code}
    >
      <Image
        src={url}
        alt={name ? `${name} crest` : `${code} crest`}
        fill
        sizes={`${width}px`}
        className={isCrest ? "object-contain" : "object-cover object-center"}
        unoptimized
      />
    </span>
  );
}

interface TeamLabelProps {
  code: string;
  name: string;
  selected?: boolean;
  compact?: boolean;
  logoUrl?: string;
}

export function TeamLabel({ code, name, selected, compact, logoUrl }: TeamLabelProps) {
  return (
    <span className={cn("inline-flex min-w-0 items-center gap-2", selected && "font-semibold")}>
      <TeamFlag code={code} name={name} logoUrl={logoUrl} />
      <span className="truncate">{compact ? code : name}</span>
    </span>
  );
}
