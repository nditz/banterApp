"use client";

import { useEffect, useRef } from "react";
import { Loader2 } from "lucide-react";
import { AdSlot } from "@/components/ads/AdSlot";
import { FeedItemCard } from "@/components/feed/FeedItem";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { useFeed } from "@/hooks/useFeed";

interface FeedListProps {
  embedded?: boolean;
}

export function FeedList({ embedded = false }: FeedListProps) {
  const { data, fetchNextPage, hasNextPage, isFetchingNextPage, isLoading, isError } =
    useFeed();
  const loadMoreRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const element = loadMoreRef.current;
    if (!element || !hasNextPage) return;

    const observer = new IntersectionObserver(
      ([entry]) => {
        if (entry.isIntersecting && hasNextPage && !isFetchingNextPage) {
          fetchNextPage();
        }
      },
      { rootMargin: "200px" }
    );

    observer.observe(element);
    return () => observer.disconnect();
  }, [fetchNextPage, hasNextPage, isFetchingNextPage]);

  if (isLoading) {
    return (
      <div className="space-y-3" aria-busy="true" aria-label="Loading feed">
        {Array.from({ length: 3 }).map((_, i) => (
          <Skeleton key={i} className="h-28 w-full rounded-lg" />
        ))}
      </div>
    );
  }

  const items =
    data?.pages.flatMap((page) => page.items).filter(Boolean) ?? [];

  return (
    <div className="space-y-3">
      {!embedded && (
        <div className="mb-3 flex items-center justify-between border-b border-border pb-2">
          <h2 className="text-sm font-semibold">Latest</h2>
          {isError && (
            <span className="text-xs text-muted-foreground">Demo feed</span>
          )}
        </div>
      )}

      {embedded && isError && (
        <p className="text-xs text-muted-foreground">Demo feed shown</p>
      )}

      {items.length === 0 && (
        <p className="py-6 text-center text-sm text-muted-foreground">
          No items yet.
        </p>
      )}

      {items.map((item, index) => (
        <div key={item.id ?? `feed-${index}`}>
          <FeedItemCard item={item} />
          {(index + 1) % 3 === 0 && (
            <AdSlot placement="feed" slotId={`feed-${index}`} className="mt-3" />
          )}
        </div>
      ))}

      <div ref={loadMoreRef} className="flex justify-center pt-2">
        {isFetchingNextPage && (
          <Loader2 className="size-5 animate-spin text-muted-foreground" aria-label="Loading more" />
        )}
        {hasNextPage && !isFetchingNextPage && (
          <Button variant="outline" size="sm" className="h-8 text-xs" onClick={() => fetchNextPage()}>
            Load more
          </Button>
        )}
        {!hasNextPage && items.length > 0 && (
          <p className="text-xs text-muted-foreground">End of feed</p>
        )}
      </div>
    </div>
  );
}
