"use client";

import type { ReactNode } from "react";
import { WelcomeHeroSlide } from "@/components/home/WelcomeHeroSlide";
import { cn } from "@/lib/utils";

export type WelcomeAccent = "gold" | "pitch" | "flare" | "brand";

export interface WelcomeSlideData {
  id: string;
  title: string;
  subtitle: string;
  body: string;
  accent: WelcomeAccent;
  backgroundImage: string;
  highlights?: readonly string[];
  stickerImage?: string;
}

const accentChip: Record<WelcomeAccent, string> = {
  gold: "border-gold/35 bg-gold/10 text-gold",
  pitch: "border-pitch/35 bg-pitch/10 text-pitch",
  flare: "border-flare/35 bg-flare/10 text-flare",
  brand: "border-border bg-muted/40 text-muted-foreground",
};

interface WelcomeSlideBodyProps {
  slide: WelcomeSlideData;
  variant?: "hero" | "compact";
  footer?: ReactNode;
  eyebrow?: ReactNode;
}

export function WelcomeSlideBody({
  slide,
  variant = "compact",
  footer,
  eyebrow,
}: WelcomeSlideBodyProps) {
  const isHero = variant === "hero";

  if (isHero) {
    return <WelcomeHeroSlide slide={slide} eyebrow={eyebrow} footer={footer} />;
  }

  return (
    <div className="flex h-full min-w-0 flex-col justify-center space-y-2.5 sm:space-y-3">
        <span
          className={cn(
            "inline-flex w-fit items-center rounded-full border px-2.5 py-0.5 text-[10px] font-bold uppercase tracking-[0.12em]",
            accentChip[slide.accent]
          )}
        >
          {slide.subtitle}
        </span>

        <div className="space-y-1.5">
          <h2 className="font-display text-lg leading-[0.95] tracking-wide sm:text-xl">
            {slide.title}
          </h2>
          <p className="max-w-xl text-xs leading-relaxed text-muted-foreground sm:text-sm">
            {slide.body}
          </p>
        </div>

        {slide.highlights && slide.highlights.length > 0 && (
          <ul className="flex flex-wrap gap-1.5 pt-0.5" aria-label="Highlights">
            {slide.highlights.map((item) => (
              <li
                key={item}
                className="rounded-full border border-border/80 bg-background/70 px-2.5 py-1 text-[10px] font-semibold text-foreground sm:text-[11px]"
              >
                {item}
              </li>
            ))}
          </ul>
        )}

        {footer}
    </div>
  );
}
