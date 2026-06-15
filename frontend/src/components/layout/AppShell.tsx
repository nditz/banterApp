"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { KeyRound, Menu, Trophy, X } from "lucide-react";
import { useRef, useState } from "react";
import { MobileBottomNav } from "@/components/layout/MobileBottomNav";
import { TermsEntertainmentNotice } from "@/components/legal/TermsOfUseContent";
import { SessionKeyRestore } from "@/components/session/SessionKeyRestore";
import { TermsGate } from "@/components/session/TermsGate";
import { TurnstileProvider } from "@/components/security/TurnstileProvider";
import { useSession } from "@/hooks/useSession";
import { Button, buttonVariants } from "@/components/ui/button";
import { cn } from "@/lib/utils";

const navLinks = [
  { href: "/", label: "Home" },
  { href: "/brackets", label: "Brackets" },
  { href: "/studio", label: "Studio" },
  { href: "/rules", label: "Rules" },
  { href: "/leagues", label: "Leagues" },
  { href: "/predictions/history", label: "History" },
];

interface AppShellProps {
  children: React.ReactNode;
}

export function AppShell({ children }: AppShellProps) {
  const pathname = usePathname();
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);
  const [restoreOpen, setRestoreOpen] = useState(false);
  const restoreRef = useRef<HTMLDivElement>(null);
  const { data: session } = useSession();

  return (
    <div className="flex min-h-screen flex-col">
      <header className="sticky top-0 z-50 border-b border-brand-foreground/10 bg-brand text-brand-foreground shadow-md backdrop-blur-xl">
        <div className="mx-auto flex h-12 max-w-[1400px] items-center justify-between gap-4 px-4 sm:px-6">
          <div className="flex items-center gap-3">
            <Button
              variant="ghost"
              size="icon-sm"
              className="text-brand-foreground hover:bg-white/10 lg:hidden"
              onClick={() => setMobileMenuOpen((open) => !open)}

              aria-label={mobileMenuOpen ? "Close menu" : "Open menu"}
            >
              {mobileMenuOpen ? <X /> : <Menu />}
            </Button>

            <Link href="/" className="flex items-center gap-2.5">
              <span className="flex size-8 items-center justify-center rounded-full bg-gold/20 ring-1 ring-gold/50">
                <Trophy className="size-4 text-gold" aria-hidden />
              </span>
              <div className="leading-tight">
                <span className="block text-sm font-bold tracking-tight">
                  BanterApp
                </span>
                <span className="block text-[10px] font-medium text-gold">
                  I know ball..watch me!
                </span>
              </div>
            </Link>
          </div>

          <nav className="hidden items-center gap-0.5 lg:flex" aria-label="Main navigation">
            {navLinks.map((link) => (
              <Link
                key={link.href}
                href={link.href}
                className={cn(
                  "rounded-md px-3 py-1.5 text-sm font-medium transition-colors",
                  pathname === link.href
                    ? "bg-white/10 text-gold"
                    : "text-white/75 hover:bg-white/10 hover:text-white"
                )}
              >
                {link.label}
              </Link>
            ))}
          </nav>

          <div className="flex items-center gap-1.5">
            {/* Restore session key — only shown to guests without an active session */}
            {!session?.authenticated && (
              <div className="relative" ref={restoreRef}>
                <Button
                  variant="ghost"
                  size="icon-sm"
                  className="text-brand-foreground hover:bg-white/10"
                  onClick={() => setRestoreOpen((o) => !o)}
                  aria-label="Restore session with key"
                  title="Restore session"
                >
                  <KeyRound className="size-4" />
                </Button>
                {restoreOpen && (
                  <div className="absolute right-0 top-full z-50 mt-1.5 w-72">
                    <SessionKeyRestore onClose={() => setRestoreOpen(false)} />
                  </div>
                )}
              </div>
            )}

            <Link
              href="/auth/login"
              className={cn(
                buttonVariants({ variant: "ghost", size: "sm" }),
                "hidden h-8 text-white/90 hover:bg-white/10 hover:text-white sm:inline-flex"
              )}
            >
              Log in
            </Link>
            <Link
              href="/auth/register"
              className={cn(
                buttonVariants({ size: "sm" }),
                "btn-tournament h-8 px-3 text-xs"
              )}
            >
              Join free
            </Link>
          </div>
        </div>

        <div className="header-gold-rule" aria-hidden />

        {mobileMenuOpen && (
          <nav
            className="border-t border-white/10 bg-brand-muted px-4 py-2 lg:hidden"
            aria-label="Mobile navigation"
          >
            <div className="flex flex-col gap-0.5">
              {navLinks.map((link) => (
                <Link
                  key={link.href}
                  href={link.href}
                  onClick={() => setMobileMenuOpen(false)}
                  className={cn(
                    "rounded-md px-3 py-2 text-sm font-medium",
                    pathname === link.href
                      ? "text-gold"
                      : "text-white/80 hover:bg-white/10"
                  )}
                >
                  {link.label}
                </Link>
              ))}
              {!session?.authenticated && (
                <div className="mt-2 border-t border-white/10 pt-2">
                  {restoreOpen ? (
                    <SessionKeyRestore onClose={() => { setRestoreOpen(false); setMobileMenuOpen(false); }} />
                  ) : (
                    <button
                      type="button"
                      onClick={() => setRestoreOpen(true)}
                      className="flex w-full items-center gap-2 rounded-md px-3 py-2 text-sm font-medium text-white/80 hover:bg-white/10"
                    >
                      <KeyRound className="size-4" aria-hidden />
                      Restore session with key
                    </button>
                  )}
                </div>
              )}
            </div>
          </nav>
        )}
      </header>

      <main className="stadium-bg main-with-bottom-nav relative flex-1 px-4 py-4 sm:px-6 sm:py-5">
        <TurnstileProvider />
        <TermsGate />
        {children}
      </main>

      <MobileBottomNav />

      <footer className="mt-auto border-t border-border bg-card py-6">
        <div className="mx-auto max-w-[1400px] space-y-3 px-4 text-center text-[11px] text-muted-foreground sm:px-6">
          <p className="font-semibold text-foreground">BanterApp</p>
          <p>Fan prediction game for the World Cup · Not affiliated with FIFA or any football governing body</p>

          <TermsEntertainmentNotice />
          <p>
            <Link href="/terms" className="font-medium text-primary hover:underline">
              Terms of Use
            </Link>
          </p>

          {/* AI content notice */}
          <p className="text-[10px] leading-relaxed">
            All images, media, and AI-generated content on this platform are produced using
            artificial intelligence tools for entertainment and social fun.{" "}
            Content is not sourced from real news publications unless explicitly credited and is
            not intended to represent factual reporting.
          </p>

          <p>© {new Date().getFullYear()} BanterApp · Built for the beautiful game</p>
        </div>
      </footer>
    </div>
  );
}
