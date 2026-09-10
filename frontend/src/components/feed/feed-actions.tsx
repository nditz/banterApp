import Link from "next/link";
import { Clapperboard, Target } from "lucide-react";
import { buttonVariants } from "@/components/ui/button";
import { pickHrefForFeedItem, studioHrefForFeedItem } from "@/lib/feed";
import type { FeedItem } from "@/lib/types";
import { cn } from "@/lib/utils";

export function FeedNextActions({ item }: { item: FeedItem }) {
  const studioHref = studioHrefForFeedItem(item);
  const pickHref = pickHrefForFeedItem(item);

  if (!studioHref && !pickHref) return null;

  return (
    <div className="mt-2.5 flex flex-wrap items-center gap-1.5">
      {studioHref ? (
        <Link
          href={studioHref}
          className={cn(buttonVariants({ variant: "default", size: "sm" }), "h-7 text-xs")}
        >
          <Clapperboard className="size-3" aria-hidden />
          Open in Studio
        </Link>
      ) : null}
      {pickHref ? (
        <Link
          href={pickHref}
          className={cn(buttonVariants({ variant: "outline", size: "sm" }), "h-7 text-xs")}
        >
          <Target className="size-3" aria-hidden />
          Make your pick
        </Link>
      ) : null}
    </div>
  );
}
