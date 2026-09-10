"use client";

import { FeedCard } from "@/components/feed/feed-registry";
import type { FeedItem } from "@/lib/types";

interface FeedItemProps {
  item: FeedItem;
}

export function FeedItemCard({ item }: FeedItemProps) {
  return <FeedCard item={item} />;
}
