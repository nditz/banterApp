"use client";

import Image from "next/image";
import { getFlagUrl } from "@/lib/team-flags";
import { cn } from "@/lib/utils";

/** Uniform flag width (px) — 3:2 ratio matches standard flag proportions. */
export const TEAM_FLAG_WIDTH = 22;
const FLAG_ASPECT = 2 / 3;

export function teamFlagHeight(width: number = TEAM_FLAG_WIDTH): number {
  return Math.round(width * FLAG_ASPECT);
}

interface TeamFlagProps {
  code: string;
  name?: string;
  /** Flag width in px; height is derived from 3:2 ratio. */
  size?: number;
  className?: string;
}

export function TeamFlag({ code, name, size = TEAM_FLAG_WIDTH, className }: TeamFlagProps) {
  const url = getFlagUrl(code, 80, name);
  const width = size;
  const height = teamFlagHeight(size);

  if (!url) {
    return (
      <span
        className={cn(
          "team-flag inline-flex shrink-0 items-center justify-center rounded-md bg-muted font-mono text-[9px] text-muted-foreground ring-1 ring-border/50",
          className
        )}
        style={{ width, height, minWidth: width, minHeight: height }}
        title={name ?? code}
      >
        ?
      </span>
    );
  }

  return (
    <span
      className={cn(
        "team-flag relative inline-block shrink-0 overflow-hidden rounded-md ring-1 ring-border/50",
        className
      )}
      style={{ width, height, minWidth: width, minHeight: height }}
      title={name ?? code}
    >
      <Image
        src={url}
        alt={name ? `${name} flag` : `${code} flag`}
        fill
        sizes={`${width}px`}
        className="object-cover object-center"
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
}

export function TeamLabel({ code, name, selected, compact }: TeamLabelProps) {
  return (
    <span className={cn("inline-flex min-w-0 items-center gap-2", selected && "font-semibold")}>
      <TeamFlag code={code} name={name} />
      <span className="truncate">{compact ? code : name}</span>
    </span>
  );
}
