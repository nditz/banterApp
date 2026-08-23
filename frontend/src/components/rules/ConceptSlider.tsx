"use client";

import { useCallback, useEffect, useState } from "react";
import { ChevronLeft, ChevronRight } from "lucide-react";
import { WelcomeSlideBody } from "@/components/home/WelcomeSlideBody";
import { WelcomeSlidePanel } from "@/components/home/WelcomeSlidePanel";
import { Button } from "@/components/ui/button";
import { BRAND } from "@/lib/brand";
import { CONCEPT_SLIDES } from "@/lib/scoring-rules";
import { cn } from "@/lib/utils";

const AUTOPLAY_MS = 7000;

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
  const [progress, setProgress] = useState(0);
  const slide = CONCEPT_SLIDES[index];
  const total = CONCEPT_SLIDES.length;

  const next = useCallback(() => {
    setIndex((i) => (i + 1) % total);
    setProgress(0);
  }, [total]);

  const prev = useCallback(() => {
    setIndex((i) => (i - 1 + total) % total);
    setProgress(0);
  }, [total]);

  const goToSlide = useCallback((i: number) => {
    setIndex(i);
    setProgress(0);
  }, []);

  useEffect(() => {
    if (!autoPlay) return;

    const tickMs = 50;
    const step = (tickMs / AUTOPLAY_MS) * 100;
    const timer = setInterval(() => {
      setProgress((p) => {
        if (p + step >= 100) {
          next();
          return 0;
        }
        return p + step;
      });
    }, tickMs);

    return () => clearInterval(timer);
  }, [autoPlay, next, index]);

  const slideContent = (
    <>
      <WelcomeSlidePanel accent={slide.accent} ariaLabel={slide.title} className="min-h-[13rem]">
        <WelcomeSlideBody slide={slide} variant="compact" />
      </WelcomeSlidePanel>

      <div className="space-y-2.5 px-4 py-3">
        <div
          className="h-1 overflow-hidden rounded-full bg-muted/60"
          role="progressbar"
          aria-valuenow={Math.round(progress)}
          aria-valuemin={0}
          aria-valuemax={100}
          aria-label="Slide autoplay progress"
        >
          <div
            className="h-full rounded-full bg-foreground/35 transition-[width] duration-100 ease-linear motion-reduce:transition-none"
            style={{ width: `${progress}%` }}
          />
        </div>

        <div className="flex items-center justify-between gap-2">
          <div className="flex flex-1 gap-1 overflow-x-auto scrollbar-none" role="tablist" aria-label="Concept slides">
            {CONCEPT_SLIDES.map((s, i) => (
              <button
                key={s.id}
                type="button"
                role="tab"
                aria-selected={i === index}
                aria-label={`Slide ${i + 1}: ${s.title}`}
                onClick={() => goToSlide(i)}
                className={cn(
                  "cursor-pointer shrink-0 rounded-full border px-2 py-0.5 text-[10px] font-bold uppercase tracking-wide transition-all duration-200",
                  i === index
                    ? "border-border bg-muted text-foreground"
                    : "border-transparent bg-muted/40 text-muted-foreground hover:text-foreground"
                )}
              >
                {s.subtitle.split("·").pop()?.trim() ?? s.subtitle}
              </button>
            ))}
          </div>

          <div className="flex shrink-0 gap-1">
            <Button
              type="button"
              variant="outline"
              size="icon-sm"
              className="size-7 cursor-pointer"
              onClick={prev}
              aria-label="Previous slide"
            >
              <ChevronLeft className="size-4" />
            </Button>
            <Button
              type="button"
              variant="outline"
              size="icon-sm"
              className="size-7 cursor-pointer"
              onClick={next}
              aria-label="Next slide"
            >
              <ChevronRight className="size-4" />
            </Button>
          </div>
        </div>
      </div>
    </>
  );

  if (embedded) {
    return (
      <div
        className={cn("overflow-hidden rounded-2xl bg-card", className)}
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
        "welcome-panel relative overflow-hidden rounded-2xl shadow-sm",
        className
      )}
      aria-roledescription="carousel"
      aria-label={`${BRAND.name} core concepts`}
    >
      {slideContent}
    </section>
  );
}
