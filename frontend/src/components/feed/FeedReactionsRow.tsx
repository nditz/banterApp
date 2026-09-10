"use client";

import { useState } from "react";
import { Minus, ThumbsDown, ThumbsUp } from "lucide-react";
import { useFeedReaction, type ReactionKind } from "@/hooks/useFeedReaction";
import type { FeedItem } from "@/lib/types";
import { cn } from "@/lib/utils";

export function FeedReactionsRow({ item }: { item: FeedItem }) {
  const [myReaction, setMyReaction] = useState<ReactionKind | null>(null);
  const [localReactions, setLocalReactions] = useState(
    item.reactions ?? { agree: 0, stale: 0, disagree: 0 }
  );
  const reactMutation = useFeedReaction(item.id);

  const handleReact = (kind: ReactionKind) => {
    if (myReaction === kind) return;
    setMyReaction(kind);
    setLocalReactions((prev) => ({
      ...prev,
      [kind]: prev[kind] + 1,
    }));
    reactMutation.mutate(kind, {
      onSuccess: (updated) => setLocalReactions(updated),
    });
  };

  return (
    <div className="mt-2.5 flex flex-wrap items-center gap-1.5">
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
  );
}
