"use client";

import { useRef, useState } from "react";
import { Play, Volume2, VolumeX } from "lucide-react";
import { Button } from "@/components/ui/button";
import type { FeedMedia as FeedMediaType } from "@/lib/types";
import { cn } from "@/lib/utils";

interface FeedMediaProps {
  media: FeedMediaType;
  className?: string;
}

export function FeedMedia({ media, className }: FeedMediaProps) {
  const videoRef = useRef<HTMLVideoElement>(null);
  const [muted, setMuted] = useState(true);

  if (media.type === "gif" || media.type === "image") {
    return (
      <div className={cn("overflow-hidden rounded-md border border-border", className)}>
        {/* eslint-disable-next-line @next/next/no-img-element */}
        <img
          src={media.url}
          alt={media.alt ?? "Feed media"}
          className="max-h-64 w-full object-cover"
          loading="lazy"
        />
      </div>
    );
  }

  if (media.type === "video" || media.type === "clip") {
    return (
      <div className={cn("relative overflow-hidden rounded-md border border-border bg-black", className)}>
        <video
          ref={videoRef}
          src={media.url}
          poster={media.posterUrl}
          className="max-h-64 w-full object-cover"
          playsInline
          loop
          muted={muted}
          controls
        />
        {media.type === "clip" && media.audioUrl && (
          <p className="border-t border-white/10 bg-black/80 px-2 py-1 text-[10px] text-white/70">
            Sound bite clip — unmute for commentary
          </p>
        )}
        <Button
          type="button"
          variant="ghost"
          size="icon-sm"
          className="absolute right-2 top-2 bg-black/50 text-white hover:bg-black/70"
          onClick={() => setMuted((m) => !m)}
          aria-label={muted ? "Unmute" : "Mute"}
        >
          {muted ? <VolumeX className="size-3.5" /> : <Volume2 className="size-3.5" />}
        </Button>
        <span className="absolute left-2 top-2 flex items-center gap-1 rounded-md bg-black/50 px-1.5 py-0.5 text-[10px] text-white">
          <Play className="size-3" aria-hidden />
          {media.type === "clip" ? "Clip" : "Video"}
        </span>
      </div>
    );
  }

  return null;
}
