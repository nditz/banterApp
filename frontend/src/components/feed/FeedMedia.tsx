"use client";

import { useRef, useState } from "react";
import Image from "next/image";
import { Volume2, VolumeX } from "lucide-react";
import { FeedMediaPlaceholder } from "@/components/feed/FeedMediaPlaceholder";
import { Button } from "@/components/ui/button";
import { canUseNextImage } from "@/lib/feed-media";
import type { FeedMedia as FeedMediaType } from "@/lib/types";
import { isSafeMediaUrl } from "@/lib/safe-url";
import { cn } from "@/lib/utils";

interface FeedMediaProps {
  media: FeedMediaType;
  className?: string;
}

export function FeedMedia({ media, className }: FeedMediaProps) {
  const videoRef = useRef<HTMLVideoElement>(null);
  const [muted, setMuted] = useState(true);
  const [failed, setFailed] = useState(false);

  if (!isSafeMediaUrl(media.url) || (media.posterUrl && !isSafeMediaUrl(media.posterUrl))) {
    return <FeedMediaPlaceholder className={className} />;
  }

  if (failed) {
    return <FeedMediaPlaceholder className={className} />;
  }

  if (media.type === "gif" || media.type === "image") {
    const isAnimatedGif =
      media.type === "gif" &&
      (media.url.includes("giphy.com") ||
        media.url.includes("tenor.com") ||
        /\.gif($|[?#])/i.test(media.url));
    const decorative = !media.alt;
    const maxHeightClass = isAnimatedGif ? "max-h-56 sm:max-h-72" : "max-h-48 sm:max-h-64";
    const useOptimized = !isAnimatedGif && canUseNextImage(media.url);

    return (
      <div className={cn("overflow-hidden rounded-md border border-border", className)}>
        {useOptimized ? (
          <div className={cn("relative aspect-video w-full", maxHeightClass)}>
            <Image
              src={media.url}
              alt={media.alt ?? ""}
              aria-hidden={decorative || undefined}
              fill
              sizes="(max-width: 640px) 100vw, 640px"
              className="object-contain sm:object-cover"
              onError={() => {
                if (!failed) setFailed(true);
              }}
            />
          </div>
        ) : (
          // Giphy/Tenor/GIF/unknown hosts — not in next.config remotePatterns.
          // eslint-disable-next-line @next/next/no-img-element
          <img
            src={media.url}
            alt={media.alt ?? ""}
            aria-hidden={decorative || undefined}
            className={cn("w-full object-contain", maxHeightClass, !isAnimatedGif && "sm:object-cover")}
            loading="lazy"
            decoding="async"
            onError={() => {
              if (!failed) setFailed(true);
            }}
          />
        )}
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
