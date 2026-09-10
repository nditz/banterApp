"use client";

import { useState, type ReactNode } from "react";
import type { LucideIcon } from "lucide-react";
import { FeedMedia } from "@/components/feed/FeedMedia";
import { FeedMediaPlaceholder } from "@/components/feed/FeedMediaPlaceholder";
import { FeedReactionsRow } from "@/components/feed/FeedReactionsRow";
import { FeedNextActions } from "@/components/feed/feed-actions";
import { Badge } from "@/components/ui/badge";
import { formatFeedPublishedAt } from "@/lib/feed";
import { resolveFeedMedia } from "@/lib/feed-media";
import { safeExternalHref } from "@/lib/safe-url";
import type { FeedItem } from "@/lib/types";
import { cn } from "@/lib/utils";

/** Max characters shown before a post is collapsed behind "Show more". */
export const FEED_BODY_MAX_CHARS = 280;

export function feedContentLabel(item: FeedItem): string | null {
  if (item.contentLabel === "direct_quote") {
    return item.type === "pundit_quote" ? "Pundit quote" : "Direct quote";
  }
  if (item.contentLabel === "paraphrase") return "Pundit (paraphrased)";
  if (item.contentLabel === "ai_summary") return "AI banter";
  if (item.contentLabel === "inferred_prediction") return "Inferred prediction";
  if (item.type === "banter") return "AI banter";
  return null;
}

interface FeedCardShellProps {
  item: FeedItem;
  icon: LucideIcon;
  label: string;
  accentClassName: string;
  mediaPlacement?: "before" | "after" | "none";
  quoteBody?: boolean;
  children?: ReactNode;
}

export function FeedCardShell({
  item,
  icon: Icon,
  label,
  accentClassName,
  mediaPlacement = "before",
  quoteBody = false,
  children,
}: FeedCardShellProps) {
  const [expanded, setExpanded] = useState(false);
  const media = resolveFeedMedia(item);
  const sourceHref = safeExternalHref(item.sourceUrl);
  const published = formatFeedPublishedAt(item.publishedAt);
  const contentLabelText = feedContentLabel(item);

  const body = item.body ?? "";
  const isLong = body.length > FEED_BODY_MAX_CHARS;
  const visibleBody =
    isLong && !expanded ? `${body.slice(0, FEED_BODY_MAX_CHARS).trimEnd()}…` : body;

  const mediaBlock =
    mediaPlacement === "none" ? null : media ? (
      <div className={mediaPlacement === "before" ? "mb-2" : "mt-2"}>
        <FeedMedia media={media} />
      </div>
    ) : (
      <div className={mediaPlacement === "before" ? "mb-2" : "mt-2"}>
        <FeedMediaPlaceholder />
      </div>
    );

  return (
    <article className={cn("feed-card px-3.5 py-3", accentClassName)}>
      <div className="mb-1.5 flex items-center justify-between gap-2">
        <div className="flex items-center gap-1.5">
          <Badge variant="secondary" className="h-5 gap-1 px-1.5 text-[10px] font-normal">
            <Icon className="size-3" aria-hidden />
            {label}
            {media?.type === "gif" && " · GIF"}
            {media?.type === "clip" && " · Clip"}
          </Badge>
          {contentLabelText ? (
            <Badge variant="outline" className="h-5 px-1.5 text-[10px] font-normal">
              {contentLabelText}
            </Badge>
          ) : null}
        </div>
        {published ? (
          <time dateTime={published.dateTime} className="text-[10px] text-muted-foreground">
            {published.label}
          </time>
        ) : null}
      </div>

      {mediaPlacement === "before" ? mediaBlock : null}

      {children}

      <h3 className="break-words text-sm font-semibold leading-snug">{item.title}</h3>
      <p
        className={cn(
          "mt-1 whitespace-pre-line text-sm leading-relaxed text-muted-foreground",
          quoteBody && "border-l-2 border-border pl-2.5 italic"
        )}
      >
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

      {mediaPlacement === "after" ? mediaBlock : null}

      {(item.author || item.source) && (
        <p className="mt-1.5 text-xs text-muted-foreground">
          {item.author &&
          (item.type === "pundit_quote" ||
            item.type === "banter" ||
            item.type === "pundit_receipt") ? (
            <>
              <span
                className="truncate-safe inline-block max-w-full font-medium text-foreground"
                title={item.author}
              >
                {item.author}
              </span>
              {item.source ? (
                <>
                  {" · via "}
                  {item.sourceUrl ? (
                    sourceHref ? (
                      <a
                        href={sourceHref}
                        target="_blank"
                        rel="noopener noreferrer"
                        className="break-anywhere text-primary hover:underline"
                      >
                        {item.source}
                      </a>
                    ) : (
                      item.source
                    )
                  ) : (
                    item.source
                  )}
                </>
              ) : null}
            </>
          ) : (
            <>
              Source:{" "}
              {item.sourceUrl ? (
                sourceHref ? (
                  <a
                    href={sourceHref}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="text-primary hover:underline"
                  >
                    {item.source}
                  </a>
                ) : (
                  item.source
                )
              ) : (
                item.source
              )}
            </>
          )}
        </p>
      )}

      <FeedReactionsRow item={item} />
      <FeedNextActions item={item} />
    </article>
  );
}
