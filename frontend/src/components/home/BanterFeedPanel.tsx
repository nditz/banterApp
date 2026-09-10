"use client";

import { FeedList } from "@/components/feed/FeedList";
import { Panel } from "@/components/ui/panel";
import { useFeed } from "@/hooks/useFeed";

const FEED_SUBTITLES = {
  personal: "Your picks vs the result — plus sourced pundit desks",
  pundit: "Sourced pundit heat, GIFs, and matchday chaos",
  default: "Sourced RSS, YouTube, and matchday banter",
} as const;

export function BanterFeedPanel() {
  const { data } = useFeed();
  const feedMode = data?.feedMode;

  return (
    <Panel
      id="banter-feed-heading"
      title="Live takes"
      subtitle={FEED_SUBTITLES[feedMode ?? "default"]}
      accent="flare"
    >
      <FeedList embedded autoLoad />
    </Panel>
  );
}
