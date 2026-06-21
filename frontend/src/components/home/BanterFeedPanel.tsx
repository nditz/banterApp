"use client";

import { useEffect, useRef, useState } from "react";
import { Loader2 } from "lucide-react";
import { FeedList } from "@/components/feed/FeedList";
import { BanterLine } from "@/components/BanterLine";
import { Panel } from "@/components/ui/panel";
import { getLocalBanterEntries, type LocalBanterEntry } from "@/lib/banterFeed";
import { useFeed } from "@/hooks/useFeed";

const LOCAL_BATCH = 4;

const FEED_SUBTITLES = {
  personal: "Your picks vs reality — plus spicy pundit takes with GIFs",
  pundit: "Real pundit heat, Gen Z banter, memes & source tags",
  default: "RSS + YouTube + AI banter — GIFs, memes & football jokes",
} as const;

export function BanterFeedPanel() {
  const { data } = useFeed();
  const feedMode = data?.feedMode;
  const [localBanter, setLocalBanter] = useState<LocalBanterEntry[]>([]);
  const [visibleLocalCount, setVisibleLocalCount] = useState(LOCAL_BATCH);
  const localLoadRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const refresh = () => {
      const entries = getLocalBanterEntries();
      setLocalBanter(entries);
      setVisibleLocalCount((prev) =>
        entries.length === 0 ? LOCAL_BATCH : Math.min(prev, entries.length)
      );
    };
    refresh();

    window.addEventListener("banter-feed-updated", refresh);
    return () => window.removeEventListener("banter-feed-updated", refresh);
  }, []);

  const visibleLocal = localBanter.slice(0, visibleLocalCount);
  const hasMoreLocal = visibleLocalCount < localBanter.length;

  useEffect(() => {
    const element = localLoadRef.current;
    if (!element || !hasMoreLocal) return;

    const observer = new IntersectionObserver(
      ([entry]) => {
        if (entry.isIntersecting) {
          setVisibleLocalCount((count) =>
            Math.min(count + LOCAL_BATCH, localBanter.length)
          );
        }
      },
      { rootMargin: "120px" }
    );

    observer.observe(element);
    return () => observer.disconnect();
  }, [hasMoreLocal, localBanter.length]);

  return (
    <Panel
      id="banter-feed-heading"
      title="Banter feed"
      subtitle={FEED_SUBTITLES[feedMode ?? "default"]}
      accent="flare"
    >
      {visibleLocal.length > 0 && (
        <div className="mb-4 space-y-2">
          <p className="text-[11px] font-bold uppercase tracking-widest text-flare">
            Live league banter
          </p>
          {visibleLocal.map((entry) => (
            <BanterLine
              key={entry.id}
              emoji={entry.emoji}
              text={entry.line}
              imageUrl={entry.imageUrl}
              media={entry.media}
            />
          ))}
          {hasMoreLocal && (
            <div
              ref={localLoadRef}
              className="flex justify-center py-2"
              aria-hidden
            >
              <Loader2 className="size-4 animate-spin text-muted-foreground" />
            </div>
          )}
        </div>
      )}
      <FeedList embedded autoLoad />
    </Panel>
  );
}
