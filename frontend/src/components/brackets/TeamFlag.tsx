"use client";

import Image from "next/image";
import { getFlagUrl } from "@/lib/team-flags";
import { cn } from "@/lib/utils";

interface TeamFlagProps {
  code: string;
  name?: string;
  size?: number;
  className?: string;
}

export function TeamFlag({ code, name, size = 20, className }: TeamFlagProps) {
  const url = getFlagUrl(code, size <= 20 ? 20 : 40, name);

  if (!url) {
    return (
      <span
        className={cn(
          "inline-flex shrink-0 items-center justify-center rounded-sm bg-muted font-mono text-[9px] text-muted-foreground",
          className
        )}
        style={{ width: size, height: Math.round(size * 0.75) }}
        title={name ?? code}
      >
        ?
      </span>
    );
  }

  return (
    <Image
      src={url}
      alt={name ? `${name} flag` : `${code} flag`}
      width={size}
      height={Math.round(size * 0.75)}
      className={cn("inline-block shrink-0 rounded-sm object-cover ring-1 ring-border/50", className)}
      unoptimized
    />
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
      <TeamFlag code={code} name={name} size={compact ? 16 : 20} />
      <span className="truncate">{compact ? code : name}</span>
    </span>
  );
}
