"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { Clapperboard, Grid3x3, MessageCircle, User, Users } from "lucide-react";
import { useCallback } from "react";
import { MOBILE_BOTTOM_NAV, isNavHrefActive } from "@/lib/navigation";
import { cn } from "@/lib/utils";

const icons = {
  Predict: Grid3x3,
  Banter: MessageCircle,
  Studio: Clapperboard,
  Leagues: Users,
  Me: User,
} as const;

export function MobileBottomNav() {
  const pathname = usePathname();

  const isActive = useCallback(
    (href: string) => isNavHrefActive(href, pathname),
    [pathname]
  );

  return (
    <nav
      className="mobile-bottom-nav fixed inset-x-0 bottom-0 z-50 border-t border-border bg-background/92 backdrop-blur-md lg:hidden"
      aria-label="Mobile navigation"
    >
      <div className="mx-auto flex max-w-lg items-stretch justify-around px-1 pb-[env(safe-area-inset-bottom,0px)]">
        {MOBILE_BOTTOM_NAV.map(({ href, label }) => {
          const Icon = icons[label as keyof typeof icons] ?? Grid3x3;
          const active = isActive(href);
          const isStudio = label === "Studio";
          return (
            <Link
              key={`${label}-${href}`}
              href={href}
              className={cn(
                "mobile-nav-item flex min-h-[3.25rem] min-w-[3rem] flex-1 flex-col items-center justify-center gap-0.5 px-0.5 py-2 text-[10px] font-bold uppercase tracking-wide transition-colors duration-200 sm:min-w-[3.5rem]",
                active
                  ? "text-foreground"
                  : "text-muted-foreground hover:text-foreground"
              )}
              aria-current={active ? "page" : undefined}
            >
              <span
                className={cn(
                  "flex size-8 items-center justify-center rounded-md transition-all duration-200",
                  active && "bg-foreground/8",
                  isStudio && !active && "text-foreground/80"
                )}
              >
                <Icon className="size-[18px]" aria-hidden />
              </span>
              <span className="leading-none">{label}</span>
            </Link>
          );
        })}
      </div>
    </nav>
  );
}
