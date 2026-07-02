import { FeedMedia } from "@/components/feed/FeedMedia";
import { sanitizeMediaUrl } from "@/lib/feed-media";
import type { FeedMedia as FeedMediaType } from "@/lib/types";
import { cn } from "@/lib/utils";

export function BanterLine({
  emoji = "⚽",
  text,
  imageUrl,
  media,
  className,
}: {
  emoji?: string;
  text: string;
  imageUrl?: string;
  media?: FeedMediaType;
  className?: string;
}) {
  const resolvedMedia = media
    ? { ...media, url: sanitizeMediaUrl(media.url) }
    : imageUrl
      ? {
          type: imageUrl.endsWith(".gif") ? ("gif" as const) : ("image" as const),
          url: sanitizeMediaUrl(imageUrl),
          alt: "Banter reaction",
        }
      : undefined;

  return (
    <div className={cn("banter-line overflow-hidden text-foreground", className)}>
      <div className="flex items-start gap-2.5">
        <span className="mt-0.5 shrink-0 text-base" aria-hidden>
          {emoji}
        </span>
        <p className="min-w-0 flex-1 text-sm leading-relaxed">{text}</p>
      </div>
      {resolvedMedia && (
        <div className="mt-2">
          <FeedMedia media={resolvedMedia} className="max-h-36" />
        </div>
      )}
    </div>
  );
}
