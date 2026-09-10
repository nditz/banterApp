import { Flame } from "lucide-react";
import { FeedCardShell } from "@/components/feed/FeedCardShell";
import type { FeedItem } from "@/lib/types";

export function TrendingTakeCard({ item }: { item: FeedItem }) {
  return (
    <FeedCardShell
      item={item}
      icon={Flame}
      label="Trending take"
      accentClassName="feed-accent-banter"
    >
      <p className="mb-1 text-[10px] font-semibold uppercase tracking-wider text-flare">
        What people are on
      </p>
    </FeedCardShell>
  );
}
