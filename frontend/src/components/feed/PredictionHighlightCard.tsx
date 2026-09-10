import { TrendingUp } from "lucide-react";
import { FeedCardShell } from "@/components/feed/FeedCardShell";
import type { FeedItem } from "@/lib/types";

export function PredictionHighlightCard({ item }: { item: FeedItem }) {
  return (
    <FeedCardShell
      item={item}
      icon={TrendingUp}
      label="Highlight"
      accentClassName="feed-accent-highlight"
    >
      <p className="mb-1 text-[10px] font-semibold uppercase tracking-wider text-pitch">
        What landed
      </p>
    </FeedCardShell>
  );
}
