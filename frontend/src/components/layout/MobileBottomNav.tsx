"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { Clapperboard, Grid3x3, History, Home, Sparkles, Trophy } from "lucide-react";
import { cn } from "@/lib/utils";

const navItems = [
  { href: "/", label: "Home", icon: Home },
  { href: "/#predictions", label: "Picks", icon: Grid3x3 },
  { href: "/bonuses", label: "Bonuses", icon: Sparkles },
  { href: "/brackets", label: "Knockout", icon: Trophy },
  { href: "/predictions/history", label: "Receipts", icon: History },
] as const;

export function MobileBottomNav() {
  const pathname = usePathname();

  const isActive = (href: string) => {
    if (href === "/") return pathname === "/";
    if (href.startsWith("/#")) return pathname === "/";
    return pathname === href || pathname.startsWith(`${href}/`);
  };

  return (
    <nav
      className="mobile-bottom-nav fixed inset-x-0 bottom-0 z-50 border-t border-border bg-card lg:hidden"
      aria-label="Mobile navigation"
    >
      <div className="mx-auto flex max-w-lg items-stretch justify-around px-1 pb-[env(safe-area-inset-bottom,0px)]">
        {navItems.map(({ href, label, icon: Icon }) => {
          const active = isActive(href);
          return (
            <Link
              key={href}
              href={href}
              className={cn(
                "mobile-nav-item flex min-h-[3.25rem] min-w-[3.5rem] flex-1 flex-col items-center justify-center gap-0.5 px-1 py-2 text-[10px] font-bold uppercase tracking-wide transition-colors duration-200",
                active
                  ? "text-electric"
                  : "text-muted-foreground hover:text-foreground"
              )}
              aria-current={active ? "page" : undefined}
            >
              <span
                className={cn(
                  "flex size-8 items-center justify-center rounded-md transition-all duration-200",
                  active && "bg-electric/12 ring-1 ring-electric/30"
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
