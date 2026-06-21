"use client";

import { useRef, useState } from "react";
import { Volume2, VolumeX } from "lucide-react";
import { Button } from "@/components/ui/button";
import type { FeedMedia as FeedMediaType } from "@/lib/types";
import { isSafeExternalUrl } from "@/lib/safe-url";
import { cn } from "@/lib/utils";

interface FeedMediaProps {
  media: FeedMediaType;
  className?: string;
}

export function FeedMedia({ media, className }: FeedMediaProps) {
  const videoRef = useRef<HTMLVideoElement>(null);
  const [muted, setMuted] = useState(true);

  if (!isSafeExternalUrl(media.url) || (media.posterUrl && !isSafeExternalUrl(media.posterUrl))) {
    return null;
  }

  if (media.type === "gif" || media.type === "image") {
    return (
      <div className={cn("overflow-hidden rounded-md border border-border", className)}>
        {/* eslint-disable-next-line @next/next/no-img-element */}
        <img
          src={media.url}
          alt={media.alt ?? "Feed media"}
          className="max-h-48 w-full object-contain sm:max-h-64 sm:object-cover"
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
          className="max-h-48 w-full object-contain sm:max-h-64 sm:object-cover"
          playsInline
          loop
          muted={muted}
          controls
        />
        {media.type === "clip" && media.audioUrl && (
          <p className="border-t border-white/10 bg-black/80 px-2 py-1 text-xs text-white/70">
            Sound bite clip — unmute for commentary
          </p>
        )}
        <Button
          type="button"
          variant="ghost"
          size="icon-sm"
          className="touch-target absolute right-2 top-2 bg-black/50 text-white hover:bg-black/70"
          onClick={() => setMuted((m) => !m)}
          aria-label={muted ? "Unmute video" : "Mute video"}
        >
          {muted ? <VolumeX className="size-4" /> : <Volume2 className="size-4" />}
        </Button>
      </div>
    );
  }

  return null;
}
