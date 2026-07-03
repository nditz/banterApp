"use client";

import { useRef, useState } from "react";
import { Volume2, VolumeX } from "lucide-react";
import { Button } from "@/components/ui/button";
import type { FeedMedia as FeedMediaType } from "@/lib/types";
import { isSafeMediaUrl } from "@/lib/safe-url";
import { cn } from "@/lib/utils";

interface FeedMediaProps {
  media: FeedMediaType;
  className?: string;
}

/** Local, always-available image shown when a remote GIF/image URL fails (e.g. expired). */
const FALLBACK_MEDIA_SRC = "/images/banter-feed-hero.png";

export function FeedMedia({ media, className }: FeedMediaProps) {
  const videoRef = useRef<HTMLVideoElement>(null);
  const [muted, setMuted] = useState(true);
  const [failed, setFailed] = useState(false);

  if (!isSafeMediaUrl(media.url) || (media.posterUrl && !isSafeMediaUrl(media.posterUrl))) {
    return null;
  }

  if (media.type === "gif" || media.type === "image") {
    const isAnimatedGif =
      media.type === "gif" &&
      (media.url.includes("giphy.com") ||
        media.url.includes("tenor.com") ||
        /\.gif($|[?#])/i.test(media.url));

    return (
      <div className={cn("overflow-hidden rounded-md border border-border", className)}>
        {/* eslint-disable-next-line @next/next/no-img-element */}
        <img
          src={failed ? FALLBACK_MEDIA_SRC : media.url}
          alt={media.alt ?? "Feed media"}
          className={cn(
            "w-full object-contain",
            isAnimatedGif ? "max-h-56 sm:max-h-72" : "max-h-48 sm:max-h-64 sm:object-cover"
          )}
          loading="lazy"
          decoding="async"
          onError={() => {
            if (!failed) setFailed(true);
          }}
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
