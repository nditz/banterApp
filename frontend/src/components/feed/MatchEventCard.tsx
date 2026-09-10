import { TrendingUp } from "lucide-react";
import { FeedCardShell } from "@/components/feed/FeedCardShell";
import type { FeedItem } from "@/lib/types";

export function MatchEventCard({ item }: { item: FeedItem }) {
  return (
    <FeedCardShell
      item={item}
      icon={TrendingUp}
      label="Match event"
      accentClassName="feed-accent-highlight"
    >
      <p className="mb-1 text-[10px] font-semibold uppercase tracking-wider text-pitch">
        What just happened
      </p>
    </FeedCardShell>
  );
}
