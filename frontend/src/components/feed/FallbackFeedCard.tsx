import { Newspaper } from "lucide-react";
import { FeedCardShell } from "@/components/feed/FeedCardShell";
import type { FeedItem } from "@/lib/types";

/** Safe shell for news and unrecognized types. */
export function FallbackFeedCard({ item }: { item: FeedItem }) {
  return (
    <FeedCardShell
      item={item}
      icon={Newspaper}
      label="Take"
      accentClassName="feed-accent-news"
    />
  );
}
