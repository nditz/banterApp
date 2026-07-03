"use client";

import { useState } from "react";
import Image from "next/image";
import {
  getAvatarInitials,
  getLeagueAvatarUrl,
  getUserAvatarUrl,
} from "@/lib/avatars";
import { cn } from "@/lib/utils";

interface UserAvatarProps {
  userId: string;
  displayName: string;
  avatarUrl?: string;
  size?: number;
  className?: string;
  highlight?: boolean;
}

export function UserAvatar({
  userId,
  displayName,
  avatarUrl,
  size = 24,
  className,
  highlight = false,
}: UserAvatarProps) {
  const [failed, setFailed] = useState(false);
  const name = displayName?.trim() || "Player";
  const src = getUserAvatarUrl(userId, name, avatarUrl);
  const showImage = Boolean(src) && !failed;

  if (!showImage) {
    return (
      <span
        className={cn(
          "inline-flex shrink-0 items-center justify-center rounded-full font-semibold",
          highlight
            ? "bg-pitch text-pitch-foreground"
            : "bg-muted text-muted-foreground",
          className
        )}
        style={{ width: size, height: size, fontSize: Math.max(9, Math.round(size * 0.38)) }}
        aria-hidden
      >
        {getAvatarInitials(name)}
      </span>
    );
  }

  return (
    <Image
      src={src!}
      alt=""
      width={size}
      height={size}
      unoptimized
      onError={() => setFailed(true)}
      style={{ width: "auto", height: "auto", maxWidth: size, maxHeight: size }}
      className={cn(
        "inline-block shrink-0 rounded-full object-cover ring-1 ring-border/50",
        highlight && "ring-pitch/40",
        className
      )}
    />
  );
}

interface LeagueAvatarProps {
  league: {
    id: string;
    name: string;
    kind?: "custom" | "global" | "country";
    countryCode?: string;
  };
  size?: number;
  className?: string;
  selected?: boolean;
}

export function LeagueAvatar({
  league,
  size = 32,
  className,
  selected = false,
}: LeagueAvatarProps) {
  const [failed, setFailed] = useState(false);
  const src = getLeagueAvatarUrl(league);
  const isFlag = league.kind === "country" && Boolean(league.countryCode);
  const initial = league.name.trim().charAt(0).toUpperCase() || "L";

  if (failed) {
    return (
      <span
        className={cn(
          "inline-flex shrink-0 items-center justify-center rounded-full font-bold",
          selected ? "bg-gold/20 text-gold" : "bg-muted text-muted-foreground",
          className
        )}
        style={{ width: size, height: size, fontSize: Math.max(10, Math.round(size * 0.4)) }}
        aria-hidden
      >
        {initial}
      </span>
    );
  }

  return (
    <Image
      src={src}
      alt=""
      width={size}
      height={size}
      unoptimized
      onError={() => setFailed(true)}
      style={{ width: "auto", height: "auto", maxWidth: size, maxHeight: size }}
      className={cn(
        "inline-block shrink-0 object-cover ring-1 ring-border/50",
        isFlag ? "rounded-sm" : "rounded-full",
        selected && "ring-gold/40",
        className
      )}
    />
  );
}
