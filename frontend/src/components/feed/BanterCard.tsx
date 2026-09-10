import { Flame } from "lucide-react";
import { FeedCardShell } from "@/components/feed/FeedCardShell";
import type { FeedItem } from "@/lib/types";

export function BanterCard({ item }: { item: FeedItem }) {
  return (
    <FeedCardShell
      item={item}
      icon={Flame}
      label="Banter"
      accentClassName="feed-accent-banter"
    />
  );
}
