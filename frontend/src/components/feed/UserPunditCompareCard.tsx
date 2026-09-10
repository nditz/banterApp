import { TrendingUp } from "lucide-react";
import { FeedCardShell } from "@/components/feed/FeedCardShell";
import type { FeedItem } from "@/lib/types";

export function UserPunditCompareCard({ item }: { item: FeedItem }) {
  return (
    <FeedCardShell
      item={item}
      icon={TrendingUp}
      label="You vs pundit"
      accentClassName="feed-accent-highlight"
    >
      <p className="mb-1 text-[10px] font-semibold uppercase tracking-wider text-pitch">
        Your pick against theirs
      </p>
    </FeedCardShell>
  );
}
