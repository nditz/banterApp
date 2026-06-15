"use client";

import Link from "next/link";
import {
  Clapperboard,
  Grid3x3,
  MessageSquareQuote,
  Target,
  Trophy,
} from "lucide-react";

const sections = [
  { href: "#predictions", label: "Picks", icon: Target },
  { href: "#banter-feed", label: "Banter", icon: MessageSquareQuote },
  { href: "#rankings", label: "Rankings", icon: Trophy },
  { href: "/brackets", label: "Bracket", icon: Grid3x3 },
  { href: "/studio", label: "Studio", icon: Clapperboard },
] as const;

export function HomeQuickNav() {
  return (
    <nav
      className="mb-5 -mx-1 flex gap-2 overflow-x-auto pb-1 scrollbar-none sm:flex-wrap sm:overflow-visible"
      aria-label="Jump to a section"
    >
      {sections.map(({ href, label, icon: Icon }) => (
        <Link key={href} href={href} className="home-quick-nav-pill shrink-0">
          <Icon className="size-3.5 shrink-0" aria-hidden />
          {label}
        </Link>
      ))}
    </nav>
  );
}
