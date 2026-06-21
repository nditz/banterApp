"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { Flame, Grid3x3, Home, Trophy, Users } from "lucide-react";
import { useCallback, useEffect, useState } from "react";
import { cn } from "@/lib/utils";

const navItems = [
  { href: "/", label: "Home", icon: Home, hash: null },
  { href: "/#predictions", label: "Picks", icon: Grid3x3, hash: "#predictions" },
  { href: "/#banter-feed", label: "Feed", icon: Flame, hash: "#banter-feed" },
  { href: "/#rankings", label: "Board", icon: Users, hash: "#rankings" },
  { href: "/brackets", label: "Knockout", icon: Trophy, hash: null },
] as const;

function getCurrentHash() {
  if (typeof window === "undefined") return "";
  return window.location.hash;
}

export function MobileBottomNav() {
  const pathname = usePathname();
  const [hash, setHash] = useState(getCurrentHash);

  useEffect(() => {
    const updateHash = () => setHash(window.location.hash);
    updateHash();
    window.addEventListener("hashchange", updateHash);
    return () => window.removeEventListener("hashchange", updateHash);
  }, [pathname]);

  const isActive = useCallback(
    (href: string, itemHash: string | null) => {
      if (href === "/") {
        return pathname === "/" && !hash;
      }
      if (itemHash && pathname === "/") {
        return hash === itemHash;
      }
      if (href.startsWith("/#")) {
        return pathname === "/";
      }
      return pathname === href || pathname.startsWith(`${href}/`);
    },
    [pathname, hash]
  );

  return (
    <nav
      className="mobile-bottom-nav fixed inset-x-0 bottom-0 z-50 border-t border-border bg-card/90 backdrop-blur-md lg:hidden"
      aria-label="Mobile navigation"
    >
      <div className="mx-auto flex max-w-lg items-stretch justify-around px-1 pb-[env(safe-area-inset-bottom,0px)]">
        {navItems.map(({ href, label, icon: Icon, hash: itemHash }) => {
          const active = isActive(href, itemHash);
          return (
            <Link
              key={href}
              href={href}
              className={cn(
                "mobile-nav-item flex min-h-[3.25rem] min-w-[3rem] flex-1 flex-col items-center justify-center gap-0.5 px-0.5 py-2 text-[10px] font-bold uppercase tracking-wide transition-colors duration-200 sm:min-w-[3.5rem]",
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
