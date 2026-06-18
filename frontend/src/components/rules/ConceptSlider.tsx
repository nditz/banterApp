"use client";

import { useCallback, useEffect, useState } from "react";
import { ChevronLeft, ChevronRight } from "lucide-react";
import { WelcomeSlidePanel } from "@/components/home/WelcomeSlidePanel";
import { Button } from "@/components/ui/button";
import { BRAND } from "@/lib/brand";
import { CONCEPT_SLIDES } from "@/lib/scoring-rules";
import { cn } from "@/lib/utils";

const accentText: Record<string, string> = {
  gold: "text-gold",
  pitch: "text-pitch",
  flare: "text-flare",
  brand: "text-brand",
};

interface ConceptSliderProps {
  autoPlay?: boolean;
  className?: string;
  /** When true, renders inside a parent panel (no outer card chrome). */
  embedded?: boolean;
}

export function ConceptSlider({
  autoPlay = true,
  className,
  embedded = false,
}: ConceptSliderProps) {
  const [index, setIndex] = useState(0);
  const slide = CONCEPT_SLIDES[index];
  const total = CONCEPT_SLIDES.length;

  const next = useCallback(() => {
    setIndex((i) => (i + 1) % total);
  }, [total]);

  const prev = useCallback(() => {
    setIndex((i) => (i - 1 + total) % total);
  }, [total]);

  useEffect(() => {
    if (!autoPlay) return;
    const timer = setInterval(next, 6000);
    return () => clearInterval(timer);
  }, [autoPlay, next]);

  const slideContent = (
    <>
      <WelcomeSlidePanel
        backgroundImage={slide.backgroundImage}
        accent={slide.accent}
        ariaLabel={slide.title}
        className={cn(
          embedded ? "min-h-[11rem]" : "min-h-[12rem] rounded-t-md",
          "border-b border-border/40"
        )}
      >
        <div className="flex h-full flex-col justify-center py-1">
          <p
            className={cn(
              "text-[10px] font-bold uppercase tracking-widest",
              accentText[slide.accent]
            )}
          >
            {slide.subtitle}
          </p>
          <h2 className="mt-1 text-base font-bold leading-snug sm:text-lg">
            {slide.title}
          </h2>
          <p className="mt-1.5 max-w-2xl text-xs leading-relaxed text-muted-foreground sm:text-sm">
            {slide.body}
          </p>
        </div>
      </WelcomeSlidePanel>

      <div className="flex items-center justify-between gap-2 px-4 py-2">
        <div className="flex gap-1.5" role="tablist" aria-label="Concept slides">
          {CONCEPT_SLIDES.map((s, i) => (
            <button
              key={s.id}
              type="button"
              role="tab"
              aria-selected={i === index}
              aria-label={`Slide ${i + 1}: ${s.title}`}
              onClick={() => setIndex(i)}
              className={cn(
                "h-1.5 rounded-full transition-all",
                i === index ? "w-6 bg-brand" : "w-1.5 bg-muted-foreground/30"
              )}
            />
          ))}
        </div>

        <div className="flex gap-1">
          <Button
            type="button"
            variant="outline"
            size="icon-sm"
            className="size-7"
            onClick={prev}
            aria-label="Previous slide"
          >
            <ChevronLeft className="size-4" />
          </Button>
          <Button
            type="button"
            variant="outline"
            size="icon-sm"
            className="size-7"
            onClick={next}
            aria-label="Next slide"
          >
            <ChevronRight className="size-4" />
          </Button>
        </div>
      </div>
    </>
  );

  if (embedded) {
    return (
      <div
        className={cn("bg-card", className)}
        aria-roledescription="carousel"
        aria-label={`${BRAND.name} core concepts`}
      >
        {slideContent}
      </div>
    );
  }

  return (
    <section
      className={cn(
        "relative overflow-hidden rounded-md border border-logo-green/50 bg-card shadow-sm",
        className
      )}
      aria-roledescription="carousel"
      aria-label={`${BRAND.name} core concepts`}
    >
      {slideContent}
    </section>
  );
}
