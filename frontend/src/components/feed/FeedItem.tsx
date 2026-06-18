"use client";

import { useState } from "react";
import {
  Flame,
  Laugh,
  Minus,
  Newspaper,
  ThumbsDown,
  ThumbsUp,
  Trophy,
  TrendingUp,
} from "lucide-react";
import { FeedMedia } from "@/components/feed/FeedMedia";
import { Badge } from "@/components/ui/badge";
import { useFeedReaction, type ReactionKind } from "@/hooks/useFeedReaction";
import { resolveFeedMedia } from "@/lib/feed-media";
import type { FeedItem } from "@/lib/types";
import { cn } from "@/lib/utils";

/** Max characters shown before a post is collapsed behind "Show more". */
export const FEED_BODY_MAX_CHARS = 280;

const typeConfig = {
  banter: {
    icon: Flame,
    label: "Banter",
    className: "feed-accent-banter",
  },
  meme: {
    icon: Laugh,
    label: "Meme",
    className: "feed-accent-meme",
  },
  news: {
    icon: Newspaper,
    label: "News",
    className: "feed-accent-news",
  },
  leaderboard: {
    icon: Trophy,
    label: "Leaderboard",
    className: "feed-accent-leaderboard",
  },
  prediction_highlight: {
    icon: TrendingUp,
    label: "Highlight",
    className: "feed-accent-highlight",
  },
};

interface FeedItemProps {
  item: FeedItem;
}

function formatTime(iso: string): string {
  const diff = Date.now() - new Date(iso).getTime();
  const hours = Math.floor(diff / 3600000);
  if (hours < 1) return "Just now";
  if (hours < 24) return `${hours}h ago`;
  return `${Math.floor(hours / 24)}d ago`;
}

export function FeedItemCard({ item }: FeedItemProps) {
  const [expanded, setExpanded] = useState(false);
  const [myReaction, setMyReaction] = useState<ReactionKind | null>(null);
  const [localReactions, setLocalReactions] = useState(item.reactions ?? { agree: 0, stale: 0, disagree: 0 });
  const reactMutation = useFeedReaction(item.id);
  const config = typeConfig[item.type];
  const Icon = config.icon;
  const media = resolveFeedMedia(item);

  const handleReact = (kind: ReactionKind) => {
    if (myReaction === kind) return; // no toggling off (keep it simple)
    setMyReaction(kind);
    setLocalReactions((prev) => ({
      ...prev,
      [kind]: prev[kind] + 1,
    }));
    reactMutation.mutate(kind, {
      onSuccess: (updated) => setLocalReactions(updated),
    });
  };

  const body = item.body ?? "";
  const isLong = body.length > FEED_BODY_MAX_CHARS;
  const visibleBody =
    isLong && !expanded ? `${body.slice(0, FEED_BODY_MAX_CHARS).trimEnd()}…` : body;

  return (
    <article
      className={cn(
        "feed-card px-3.5 py-3",
        config.className
      )}
    >
      <div className="mb-1.5 flex items-center justify-between gap-2">
        <Badge variant="secondary" className="h-5 gap-1 px-1.5 text-[10px] font-normal">
          <Icon className="size-3" aria-hidden />
          {config.label}
          {media?.type === "gif" && " · GIF"}
          {media?.type === "clip" && " · Clip"}
        </Badge>
        <time
          dateTime={item.publishedAt}
          className="text-[10px] text-muted-foreground"
        >
          {formatTime(item.publishedAt)}
        </time>
      </div>

      {media && (
        <div className="mb-2">
          <FeedMedia media={media} />
        </div>
      )}

      <h3 className="text-sm font-semibold leading-snug">{item.title}</h3>
      <p className="mt-1 whitespace-pre-line text-sm leading-relaxed text-muted-foreground">
        {visibleBody}
      </p>
      {isLong && (
        <button
          type="button"
          onClick={() => setExpanded((v) => !v)}
          className="mt-1 text-xs font-medium text-primary hover:underline"
        >
          {expanded ? "Show less" : "Show more"}
        </button>
      )}
      {item.source && (
        <p className="mt-1.5 text-[11px] text-muted-foreground">
          Source:{" "}
          {item.sourceUrl ? (
            <a
              href={item.sourceUrl}
              target="_blank"
              rel="noopener noreferrer"
              className="text-primary hover:underline"
            >
              {item.source}
            </a>
          ) : (
            item.source
          )}
        </p>
      )}
      {/* Reactions row */}
      <div className="mt-2.5 flex items-center gap-1.5">
        <button
          type="button"
          onClick={() => handleReact("agree")}
          disabled={myReaction !== null}
          className={cn("reaction-btn", myReaction === "agree" && "active-agree")}
          aria-label="Agree"
          aria-pressed={myReaction === "agree"}
        >
          <ThumbsUp className="size-3" aria-hidden />
          {localReactions.agree > 0 && <span>{localReactions.agree}</span>}
        </button>
        <button
          type="button"
          onClick={() => handleReact("stale")}
          disabled={myReaction !== null}
          className={cn("reaction-btn", myReaction === "stale" && "active-stale")}
          aria-label="Meh / stale"
          aria-pressed={myReaction === "stale"}
        >
          <Minus className="size-3" aria-hidden />
          {localReactions.stale > 0 && <span>{localReactions.stale}</span>}
        </button>
        <button
          type="button"
          onClick={() => handleReact("disagree")}
          disabled={myReaction !== null}
          className={cn("reaction-btn", myReaction === "disagree" && "active-disagree")}
          aria-label="Disagree"
          aria-pressed={myReaction === "disagree"}
        >
          <ThumbsDown className="size-3" aria-hidden />
          {localReactions.disagree > 0 && <span>{localReactions.disagree}</span>}
        </button>
      </div>
    </article>
  );
}
