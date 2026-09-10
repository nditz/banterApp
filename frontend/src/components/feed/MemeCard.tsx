import { Laugh } from "lucide-react";
import { FeedCardShell } from "@/components/feed/FeedCardShell";
import type { FeedItem } from "@/lib/types";

export function MemeCard({ item }: { item: FeedItem }) {
  return (
    <FeedCardShell
      item={item}
      icon={Laugh}
      label="Meme"
      accentClassName="feed-accent-meme"
    />
  );
}
