import { Clapperboard } from "lucide-react";
import { FeedCardShell } from "@/components/feed/FeedCardShell";
import type { FeedItem } from "@/lib/types";

export function StudioStoryCard({ item }: { item: FeedItem }) {
  return (
    <FeedCardShell
      item={item}
      icon={Clapperboard}
      label="Studio story"
      accentClassName="feed-accent-pundit"
    >
      <p className="mb-1 text-[10px] font-semibold uppercase tracking-wider text-foreground">
        Ready to export
      </p>
    </FeedCardShell>
  );
}
