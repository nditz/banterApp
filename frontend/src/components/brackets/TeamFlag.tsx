"use client";

import Image from "next/image";
import { useMemo, useState } from "react";
import { getClubBadgeUrl } from "@/lib/club-badges";
import { cn } from "@/lib/utils";

/** Default club badge width. Crests render square. */
export const TEAM_FLAG_WIDTH = 28;

export function teamFlagHeight(width: number = TEAM_FLAG_WIDTH): number {
  return width;
}

interface TeamFlagProps {
  code: string;
  name?: string;
  logoUrl?: string;
  size?: number;
  className?: string;
}

export function TeamFlag({ code, name, logoUrl, size = TEAM_FLAG_WIDTH, className }: TeamFlagProps) {
  const sources = useMemo(() => {
    const mapped = getClubBadgeUrl(code, name);
    return [...new Set([logoUrl?.trim() || null, mapped].filter(Boolean))] as string[];
  }, [code, name, logoUrl]);
  const sourceKey = `${code}\0${name ?? ""}\0${logoUrl ?? ""}`;
  const [sourceIndex, setSourceIndex] = useState(0);
  const [seenSourceKey, setSeenSourceKey] = useState(sourceKey);
  if (sourceKey !== seenSourceKey) {
    setSeenSourceKey(sourceKey);
    setSourceIndex(0);
  }
  const url = sources[sourceIndex] ?? null;
  const width = size;
  const height = size;

  if (!url) {
    return (
      <span
        className={cn(
          "team-flag inline-flex shrink-0 items-center justify-center rounded-md bg-muted font-mono text-[9px] font-bold text-muted-foreground ring-1 ring-border/60",
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
        "team-flag relative inline-block shrink-0 overflow-hidden rounded-md bg-white p-0.5 ring-1 ring-border/50 dark:bg-card",
        className
      )}
      style={{ width, height, minWidth: width, minHeight: height }}
      title={name ?? code}
    >
      <Image
        src={url}
        alt={name ? `${name} badge` : `${code} badge`}
        fill
        sizes={`${width}px`}
        className="object-contain"
        unoptimized
        onError={() => setSourceIndex((index) => index + 1)}
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
