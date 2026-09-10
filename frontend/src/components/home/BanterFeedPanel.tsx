"use client";

import { FeedList } from "@/components/feed/FeedList";
import { Panel } from "@/components/ui/panel";
import { useFeed } from "@/hooks/useFeed";

const FEED_SUBTITLES = {
  personal: "Your picks vs reality — plus spicy pundit takes with GIFs",
  pundit: "Real pundit heat, Gen Z banter, memes & source tags",
  default: "RSS + YouTube + AI banter — GIFs, memes & football jokes",
} as const;

export function BanterFeedPanel() {
  const { data } = useFeed();
  const feedMode = data?.feedMode;

  return (
    <Panel
      id="banter-feed-heading"
      title="Banter feed"
      subtitle={FEED_SUBTITLES[feedMode ?? "default"]}
      accent="flare"
    >
      <FeedList embedded autoLoad />
    </Panel>
  );
}
