"use client";

import Link from "next/link";
import { useCallback, useEffect, useMemo, useState } from "react";
import { ChevronLeft, ChevronRight, Clapperboard, Sparkles, Zap } from "lucide-react";
import { CumulativeScriptExport } from "@/components/content/CumulativeScriptExport";
import { WelcomeSlideBody } from "@/components/home/WelcomeSlideBody";
import { WelcomeSlidePanel } from "@/components/home/WelcomeSlidePanel";
import { Button } from "@/components/ui/button";
import { buttonVariants } from "@/components/ui/button";
import { BRAND } from "@/lib/brand";
import { HOME_WELCOME_SLIDES } from "@/lib/scoring-rules";
import { cn } from "@/lib/utils";

const AUTOPLAY_MS = 8000;

function WelcomeHeroEyebrow() {
  return (
    <p className="welcome-hero__brand">
      <span className="font-display text-lg text-foreground sm:text-xl">{BRAND.name}</span>
      <span className="text-muted-foreground">{BRAND.tagline}</span>
    </p>
  );
}

function SlideActions({ onCreateContent }: { onCreateContent: () => void }) {
  return (
    <div className="flex flex-wrap gap-1.5 pt-1.5">
      <Button
        type="button"
        size="sm"
        className="btn-tournament h-8 cursor-pointer px-3 text-[11px]"
        onClick={onCreateContent}
      >
        <Clapperboard className="size-3" aria-hidden />
        Get my script
      </Button>
      <Link
        href="#predictions"
        className={cn(
          buttonVariants({ variant: "outline", size: "sm" }),
          "h-8 cursor-pointer border-border px-3 text-[11px] font-bold uppercase tracking-wider"
        )}
      >
        <Sparkles className="size-3" aria-hidden />
        Lock a pick
      </Link>
    </div>
  );
}

export function HomeWelcomePanel() {
  const [index, setIndex] = useState(0);
  const [paused, setPaused] = useState(false);
  const [progress, setProgress] = useState(0);
  const slides = HOME_WELCOME_SLIDES;
  const total = slides.length;
  const slide = slides[index];

  const contentSlideIndex = useMemo(
    () => slides.findIndex((s) => s.id === "content"),
    [slides]
  );

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
    if (paused) return;

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
  }, [next, paused, index]);

  const goToContent = () => setIndex(contentSlideIndex >= 0 ? contentSlideIndex : 0);

  return (
    <section
      className="welcome-panel mb-4 rounded-2xl p-4 sm:p-5 lg:p-6"
      aria-roledescription="carousel"
      aria-label="How Ball Takes works"
      onMouseEnter={() => setPaused(true)}
      onMouseLeave={() => setPaused(false)}
      onFocusCapture={() => setPaused(true)}
      onBlurCapture={() => setPaused(false)}
    >
      <div className="mb-3 flex items-center justify-between gap-3">
        <div className="flex items-center gap-2">
          <span className="inline-flex size-7 items-center justify-center rounded-lg border border-border bg-muted/40">
            <Zap className="size-3.5 text-muted-foreground" aria-hidden />
          </span>
          <div>
            <p className="text-[10px] font-bold uppercase tracking-[0.14em] text-muted-foreground">
              Quick tour
            </p>
            <p className="text-xs font-semibold text-foreground">{slide.title}</p>
          </div>
        </div>
        <p className="shrink-0 text-[11px] font-bold tabular-nums text-muted-foreground">
          {index + 1}
          <span className="text-muted-foreground/50"> / </span>
          {total}
        </p>
      </div>

      <div className={cn("welcome-slide-viewport", index === 0 && "welcome-slide-viewport--hero")}>
        <div
          className="flex h-full transition-transform duration-500 ease-[cubic-bezier(0.32,0.72,0.24,1)] motion-reduce:transition-none"
          style={{ transform: `translateX(-${index * 100}%)` }}
        >
          {slides.map((s, i) => (
            <div
              key={s.id}
              className="h-full w-full shrink-0 pr-0.5"
              aria-hidden={i !== index}
              {...(i !== index ? { inert: true } : {})}
            >
              <WelcomeSlidePanel
                accent={s.accent}
                ariaLabel={s.title}
                className={s.id === "welcome" ? "welcome-slide-panel--hero-shell" : undefined}
              >
                <WelcomeSlideBody
                  slide={s}
                  variant={s.id === "welcome" ? "hero" : "compact"}
                  eyebrow={s.id === "welcome" ? <WelcomeHeroEyebrow /> : undefined}
                  footer={
                    <>
                      {s.id === "welcome" && <SlideActions onCreateContent={goToContent} />}
                      {s.id === "content" && (
                        <div className="mt-2 max-h-28 overflow-y-auto rounded-xl border border-border/60 bg-card/80 p-2.5 backdrop-blur-sm sm:max-h-32">
                          <CumulativeScriptExport minimal className="border-0 bg-transparent p-0" />
                        </div>
                      )}
                    </>
                  }
                />
              </WelcomeSlidePanel>
            </div>
          ))}
        </div>
      </div>

      <div className="mt-4 space-y-3 border-t border-border pt-3">
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
          <div className="flex flex-1 gap-1 overflow-x-auto pb-0.5 scrollbar-none" role="tablist" aria-label="Slides">
            {slides.map((s, i) => (
              <button
                key={s.id}
                type="button"
                role="tab"
                aria-selected={i === index}
                aria-label={s.title}
                onClick={() => goToSlide(i)}
                className={cn(
                  "cursor-pointer shrink-0 rounded-full border px-2.5 py-1 text-[10px] font-bold uppercase tracking-wide transition-all duration-200",
                  i === index
                    ? "border-border bg-muted text-foreground"
                    : "border-transparent bg-muted/40 text-muted-foreground hover:border-border hover:bg-muted/70 hover:text-foreground"
                )}
              >
                {s.subtitle.split("·").pop()?.trim() ?? s.subtitle}
              </button>
            ))}
          </div>
          <div className="flex shrink-0 items-center gap-1.5">
            <Link
              href="/rules"
              className="mr-1 hidden text-[11px] font-semibold text-primary hover:underline sm:inline"
            >
              Full rules
            </Link>
            <Button
              type="button"
              variant="outline"
              size="icon-sm"
              className="size-8 cursor-pointer"
              onClick={prev}
              aria-label="Previous slide"
            >
              <ChevronLeft className="size-4" />
            </Button>
            <Button
              type="button"
              variant="outline"
              size="icon-sm"
              className="size-8 cursor-pointer"
              onClick={next}
              aria-label="Next slide"
            >
              <ChevronRight className="size-4" />
            </Button>
          </div>
        </div>
      </div>
    </section>
  );
}
