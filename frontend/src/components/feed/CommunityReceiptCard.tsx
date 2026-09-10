import { Trophy } from "lucide-react";
import { FeedCardShell } from "@/components/feed/FeedCardShell";
import type { FeedItem } from "@/lib/types";

export function CommunityReceiptCard({ item }: { item: FeedItem }) {
  return (
    <FeedCardShell
      item={item}
      icon={Trophy}
      label="Crowd receipt"
      accentClassName="feed-accent-leaderboard"
    >
      <p className="mb-1 text-[10px] font-semibold uppercase tracking-wider text-gold">
        What the crowd called
      </p>
    </FeedCardShell>
  );
}
