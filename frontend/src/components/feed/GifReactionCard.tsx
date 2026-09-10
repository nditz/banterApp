import { Laugh } from "lucide-react";
import { FeedCardShell } from "@/components/feed/FeedCardShell";
import type { FeedItem } from "@/lib/types";

export function GifReactionCard({ item }: { item: FeedItem }) {
  return (
    <FeedCardShell
      item={item}
      icon={Laugh}
      label="GIF reaction"
      accentClassName="feed-accent-meme"
    />
  );
}
