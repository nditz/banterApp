import { Megaphone } from "lucide-react";
import { FeedCardShell } from "@/components/feed/FeedCardShell";
import type { FeedItem } from "@/lib/types";

export function PunditReceiptCard({ item }: { item: FeedItem }) {
  return (
    <FeedCardShell
      item={item}
      icon={Megaphone}
      label="Pundit receipt"
      accentClassName="feed-accent-pundit"
      quoteBody
    >
      <p className="mb-1 text-[10px] font-semibold uppercase tracking-wider text-sky-600 dark:text-sky-400">
        What they said · what happened
      </p>
    </FeedCardShell>
  );
}
