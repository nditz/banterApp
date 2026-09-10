import { cn } from "@/lib/utils";

interface FeedMediaPlaceholderProps {
  className?: string;
}

/** Designed empty media slot — decorative local sticker, never stock photography. */
export function FeedMediaPlaceholder({ className }: FeedMediaPlaceholderProps) {
  return (
    <div
      className={cn(
        "relative flex aspect-video max-h-48 items-center justify-center overflow-hidden rounded-md border border-dashed border-border bg-muted/30",
        className
      )}
      role="presentation"
    >
      <div className="absolute inset-x-0 top-0 h-0.5 bg-pitch" aria-hidden />
      {/* eslint-disable-next-line @next/next/no-img-element */}
      <img
        src="/reactions/locked-in.svg"
        alt=""
        aria-hidden
        className="size-12 opacity-40"
      />
    </div>
  );
}
