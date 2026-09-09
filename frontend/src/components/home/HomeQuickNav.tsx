"use client";

import Link from "next/link";
import {
  Clapperboard,
  MessageCircle,
  Sparkles,
  Target,
  Users,
} from "lucide-react";
import { HOME_QUICK_NAV } from "@/lib/navigation";

const icons = {
  Predict: Target,
  Banter: MessageCircle,
  Studio: Clapperboard,
  Leagues: Users,
  "Season Calls": Sparkles,
} as const;

export function HomeQuickNav() {
  return (
    <nav
      className="mb-5 -mx-1 flex gap-2 overflow-x-auto pb-1 scrollbar-none sm:flex-wrap sm:overflow-visible"
      aria-label="Jump to a section"
    >
      {HOME_QUICK_NAV.map(({ href, label }) => {
        const Icon = icons[label as keyof typeof icons] ?? Target;
        return (
          <Link key={`${label}-${href}`} href={href} className="home-quick-nav-pill shrink-0">
            <Icon className="size-3.5 shrink-0" aria-hidden />
            {label}
          </Link>
        );
      })}
    </nav>
  );
}
