"use client";

import Link from "next/link";
import { useCallback, useEffect, useMemo, useState } from "react";
import { ChevronLeft, ChevronRight, Clapperboard, Sparkles, Trophy } from "lucide-react";
import { CumulativeScriptExport } from "@/components/content/CumulativeScriptExport";
import { Button } from "@/components/ui/button";
import { buttonVariants } from "@/components/ui/button";
import { HOME_WELCOME_SLIDES } from "@/lib/scoring-rules";
import { cn } from "@/lib/utils";

const accentText: Record<string, string> = {
  gold: "text-gold",
  pitch: "text-pitch",
  flare: "text-flare",
  brand: "text-brand",
};

type Slide = (typeof HOME_WELCOME_SLIDES)[number];

function WelcomeSlide({
  slide,
  onCreateContent,
}: {
  slide: Slide;
  onCreateContent: () => void;
}) {
  return (
    <div className="relative z-[1] flex h-full flex-col justify-center space-y-4">
      <div className="flex flex-wrap items-center gap-2">
        <span className="wc-badge border-gold/35 bg-gold/12 text-gold">
          <Trophy className="size-3" aria-hidden />
          {slide.subtitle}
        </span>
        <span className="live-chip">
          <span className="live-chip-dot" aria-hidden />
          Matchday live
        </span>
      </div>
      <div className="space-y-2">
        <h1 className="font-display text-2xl font-semibold leading-[1.15] sm:text-3xl lg:text-[2rem]">
          Predict. Banter.{" "}
          <span className="neon-gold-text bg-gradient-to-r from-gold via-pitch to-gold bg-clip-text text-transparent">
            Create content.
          </span>
        </h1>
        <p className="max-w-2xl text-sm leading-relaxed text-muted-foreground sm:text-base">
          {slide.body}
        </p>
      </div>
      <div className="flex flex-wrap gap-2 pt-0.5">
        <Button
          type="button"
          size="sm"
          className="btn-tournament h-9 px-4 text-xs shadow-md transition-shadow duration-200 hover:shadow-lg"
          onClick={onCreateContent}
        >
          <Clapperboard className="size-3.5" aria-hidden />
          Create my content
        </Button>
        <Link
          href="#predictions"
          className={cn(
            buttonVariants({ variant: "outline", size: "sm" }),
            "h-9 border-pitch/30 px-4 text-xs transition-colors duration-200 hover:border-pitch/50 hover:bg-pitch/5"
          )}
        >
          <Sparkles className="size-3.5" aria-hidden />
          Make a pick
        </Link>
        <Link
          href="/leagues"
          className={cn(
            buttonVariants({ variant: "ghost", size: "sm" }),
            "h-9 px-3 text-xs text-muted-foreground hover:text-foreground"
          )}
        >
          Join a league →
        </Link>
      </div>
    </div>
  );
}

function ConceptSlide({ slide }: { slide: Slide }) {
  return (
    <div className="relative z-[1] flex h-full flex-col justify-center space-y-3">
      <p
        className={cn(
          "text-[10px] font-bold uppercase tracking-widest",
          accentText[slide.accent]
        )}
      >
        {slide.subtitle}
      </p>
      <h2 className="font-display text-lg font-semibold leading-snug sm:text-xl">{slide.title}</h2>
      <p className="text-sm leading-relaxed text-muted-foreground">{slide.body}</p>
      {slide.id === "content" && (
        <div className="min-h-0 flex-1 overflow-y-auto">
          <CumulativeScriptExport minimal className="border-0 bg-muted/40 p-3" />
        </div>
      )}
    </div>
  );
}

export function HomeWelcomePanel() {
  const [index, setIndex] = useState(0);
  const [paused, setPaused] = useState(false);
  const slides = HOME_WELCOME_SLIDES;
  const total = slides.length;

  const contentSlideIndex = useMemo(
    () => slides.findIndex((s) => s.id === "content"),
    [slides]
  );

  const next = useCallback(() => setIndex((i) => (i + 1) % total), [total]);
  const prev = useCallback(() => setIndex((i) => (i - 1 + total) % total), [total]);

  useEffect(() => {
    if (paused) return;
    const timer = setInterval(next, 7000);
    return () => clearInterval(timer);
  }, [next, paused]);

  return (
    <section
      className="welcome-panel mb-4 rounded-2xl border border-border/80 p-5 shadow-md sm:p-6 lg:p-7"
      aria-roledescription="carousel"
      aria-label="Welcome — turn your predictions into content"
      onMouseEnter={() => setPaused(true)}
      onMouseLeave={() => setPaused(false)}
      onFocusCapture={() => setPaused(true)}
      onBlurCapture={() => setPaused(false)}
    >
      <div className="welcome-slide-viewport">
        <div
          className="flex h-full transition-transform duration-500 ease-[cubic-bezier(0.32,0.72,0.24,1)] motion-reduce:transition-none"
          style={{ transform: `translateX(-${index * 100}%)` }}
        >
          {slides.map((slide, i) => (
            <div
              key={slide.id}
              className="h-full w-full shrink-0 pr-1"
              aria-hidden={i !== index}
              {...(i !== index ? { inert: true } : {})}
            >
              {slide.id === "welcome" ? (
                <WelcomeSlide
                  slide={slide}
                  onCreateContent={() =>
                    setIndex(contentSlideIndex >= 0 ? contentSlideIndex : 0)
                  }
                />
              ) : (
                <ConceptSlide slide={slide} />
              )}
            </div>
          ))}
        </div>
      </div>

      <div className="mt-4 flex items-center justify-between gap-2 border-t border-border pt-3">
        <div className="flex gap-1.5" role="tablist" aria-label="Slides">
          {slides.map((s, i) => (
            <button
              key={s.id}
              type="button"
              role="tab"
              aria-selected={i === index}
              aria-label={s.title}
              onClick={() => setIndex(i)}
              className={cn(
                "cursor-pointer rounded-full transition-all duration-300",
                i === index
                  ? "h-1.5 w-6 bg-gradient-to-r from-gold to-pitch"
                  : "h-1.5 w-1.5 bg-muted-foreground/30 hover:bg-muted-foreground/60"
              )}
            />
          ))}
        </div>
        <div className="flex items-center gap-2">
          <Link
            href="/rules"
            className="text-[11px] font-medium text-primary hover:underline"
          >
            Rules
          </Link>
          <Button
            type="button"
            variant="outline"
            size="icon-sm"
            className="size-7"
            onClick={prev}
            aria-label="Previous"
          >
            <ChevronLeft className="size-4" />
          </Button>
          <Button
            type="button"
            variant="outline"
            size="icon-sm"
            className="size-7"
            onClick={next}
            aria-label="Next"
          >
            <ChevronRight className="size-4" />
          </Button>
        </div>
      </div>
    </section>
  );
}
