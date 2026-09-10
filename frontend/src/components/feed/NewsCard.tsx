import { Newspaper } from "lucide-react";
import { FeedCardShell } from "@/components/feed/FeedCardShell";
import type { FeedItem } from "@/lib/types";

export function NewsCard({ item }: { item: FeedItem }) {
  return (
    <FeedCardShell
      item={item}
      icon={Newspaper}
      label="News"
      accentClassName="feed-accent-news"
    />
  );
}
