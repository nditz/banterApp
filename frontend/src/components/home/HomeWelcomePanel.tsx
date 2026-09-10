"use client";

import Link from "next/link";
import { Clapperboard } from "lucide-react";
import { WelcomeHeroSlide } from "@/components/home/WelcomeHeroSlide";
import { buttonVariants } from "@/components/ui/button";
import { useSupabaseUser } from "@/hooks/useSupabaseUser";
import { BRAND } from "@/lib/brand";
import { HOME_WELCOME_SLIDES } from "@/lib/scoring-rules";
import { cn } from "@/lib/utils";

function WelcomeHeroEyebrow({ greeting }: { greeting: string }) {
  return (
    <p className="welcome-hero__brand">
      <span className="font-display text-lg text-foreground sm:text-xl">{BRAND.name}</span>
      <span className="text-muted-foreground">
        {greeting ? `Welcome back, ${greeting}.` : BRAND.tagline}
      </span>
    </p>
  );
}

function HeroActions() {
  return (
    <div className="flex min-w-0 flex-wrap items-center gap-1.5 pt-1.5">
      <Link
        href="#predictions"
        className={cn(
          buttonVariants({ size: "sm" }),
          "btn-tournament h-8 cursor-pointer px-3 text-[11px] font-bold uppercase tracking-wider"
        )}
      >
        Make a pick
      </Link>
      <Link
        href="/studio"
        className={cn(
          buttonVariants({ variant: "outline", size: "sm" }),
          "h-8 cursor-pointer px-3 text-[11px] font-bold uppercase tracking-wider"
        )}
      >
        <Clapperboard className="size-3" aria-hidden />
        Open Studio
      </Link>
      <Link
        href="#banter-feed"
        className="px-1.5 text-[11px] font-semibold text-muted-foreground underline-offset-4 hover:text-foreground hover:underline"
      >
        Watch the feed
      </Link>
      <Link
        href="/rules"
        className="px-1.5 text-[11px] font-semibold text-muted-foreground underline-offset-4 hover:text-foreground hover:underline"
      >
        How it works
      </Link>
    </div>
  );
}

export function HomeWelcomePanel() {
  const { isSignedIn, displayName, email } = useSupabaseUser();
  const greeting = isSignedIn
    ? (displayName || email?.split("@")[0] || "").slice(0, 24)
    : "";
  const slide = HOME_WELCOME_SLIDES[0];

  if (!slide) return null;

  return (
    <section className="welcome-panel mb-4 min-w-0 rounded-2xl p-4 sm:p-5 lg:p-6" aria-label="Ball Takes">
      <p className="mb-2 text-[10px] font-bold uppercase tracking-[0.14em] text-muted-foreground">
        Start here
      </p>
      <WelcomeHeroSlide
        slide={slide}
        eyebrow={<WelcomeHeroEyebrow greeting={greeting} />}
        footer={<HeroActions />}
      />
    </section>
  );
}
