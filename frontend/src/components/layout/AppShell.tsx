"use client";

import Image from "next/image";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { KeyRound, Menu, X } from "lucide-react";
import { useRef, useState } from "react";
import { MobileBottomNav } from "@/components/layout/MobileBottomNav";
import { TermsEntertainmentNotice } from "@/components/legal/TermsOfUseContent";
import { SessionKeyRestore } from "@/components/session/SessionKeyRestore";
import { TermsGate } from "@/components/session/TermsGate";
import { TurnstileProvider } from "@/components/security/TurnstileProvider";
import { useSession } from "@/hooks/useSession";
import { BRAND } from "@/lib/brand";
import { Button, buttonVariants } from "@/components/ui/button";
import { cn } from "@/lib/utils";

const navLinks = [
  { href: "/", label: "Home" },
  { href: "/brackets", label: "Knockout bracket" },
  { href: "/bonuses", label: "Bonuses" },
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
      <header
        className="sticky top-0 z-50 text-white"
        style={{ backgroundColor: BRAND.headerBackground }}
      >
        <div className="mx-auto flex h-14 max-w-[1400px] items-center justify-between gap-4 px-4 sm:px-6">
          <div className="flex items-center gap-3">
            <Button
              variant="ghost"
              size="icon-sm"
              className="text-white hover:bg-white/10 lg:hidden"
              onClick={() => setMobileMenuOpen((open) => !open)}
              aria-label={mobileMenuOpen ? "Close menu" : "Open menu"}
            >
              {mobileMenuOpen ? <X /> : <Menu />}
            </Button>

            <Link href="/" className="group flex shrink-0 items-center">
              <Image
                src={BRAND.logoHeader}
                alt={`${BRAND.name} — ${BRAND.tagline}`}
                width={72}
                height={48}
                className="block h-10 w-auto max-w-[min(200px,52vw)] object-contain sm:h-10"
                priority
                unoptimized
              />
            </Link>
          </div>

          <nav className="hidden items-center gap-1 lg:flex" aria-label="Main navigation">
            {navLinks.map((link) => (
              <Link
                key={link.href}
                href={link.href}
                className={cn(
                  "rounded-md px-3 py-2 text-xs font-bold uppercase tracking-wider transition-colors",
                  pathname === link.href
                    ? "bg-electric/15 text-electric"
                    : "text-white/70 hover:bg-white/10 hover:text-white"
                )}
              >
                {link.label}
              </Link>
            ))}
          </nav>

          <div className="flex items-center gap-2">
            {!session?.authenticated && (
              <div className="relative" ref={restoreRef}>
                <Button
                  variant="ghost"
                  size="icon-sm"
                  className="text-white hover:bg-white/10"
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
                "hidden h-8 text-xs font-bold uppercase tracking-wider text-white/80 hover:bg-white/10 hover:text-white sm:inline-flex"
              )}
            >
              Log in
            </Link>
            <Link
              href="/auth/register"
              className={cn(
                buttonVariants({ size: "sm" }),
                "btn-tournament h-8 px-4 text-xs"
              )}
            >
              Join free
            </Link>
          </div>
        </div>

        <div className="header-accent-rule" aria-hidden />

        {mobileMenuOpen && (
          <nav
            className="border-t border-white/10 px-4 py-3 lg:hidden"
            style={{ backgroundColor: BRAND.headerBackground }}
            aria-label="Mobile navigation"
          >
            <div className="flex flex-col gap-0.5">
              {navLinks.map((link) => (
                <Link
                  key={link.href}
                  href={link.href}
                  onClick={() => setMobileMenuOpen(false)}
                  className={cn(
                    "rounded-md px-3 py-2.5 text-xs font-bold uppercase tracking-wider",
                    pathname === link.href
                      ? "bg-electric/15 text-electric"
                      : "text-white/75 hover:bg-white/10"
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
                      className="flex w-full cursor-pointer items-center gap-2 rounded-md px-3 py-2.5 text-xs font-bold uppercase tracking-wider text-white/75 hover:bg-white/10"
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

      <footer
        className="mt-auto border-t border-logo-green/40 py-6 text-white/65"
        style={{ backgroundColor: BRAND.headerBackground }}
      >
        <div className="mx-auto max-w-[1400px] space-y-3 px-4 text-center text-[11px] sm:px-6">
          <Link href="/" className="mx-auto inline-flex justify-center">
            <Image
              src={BRAND.logoFooter}
              alt={BRAND.name}
              width={108}
              height={72}
              className="h-16 w-auto object-contain"
              unoptimized
            />
          </Link>
          <p>
            {BRAND.tagline} · Fan prediction game for the World Cup · Not affiliated with FIFA or
            any football governing body
          </p>

          <TermsEntertainmentNotice className="border-logo-green/35 bg-white/5 text-white/60 [&_a]:text-logo-green [&_a]:hover:text-logo-green/80 [&_strong]:text-white/90" />
          <p>
            <Link href="/terms" className="font-semibold text-logo-green hover:underline">
              Terms of Use
            </Link>
          </p>

          <p className="text-[10px] leading-relaxed">
            All images, media, and AI-generated content on this platform are produced using
            artificial intelligence tools for entertainment and social fun.{" "}
            Content is not sourced from real news publications unless explicitly credited and is
            not intended to represent factual reporting.
          </p>

          <p>
            © {new Date().getFullYear()} {BRAND.name} · {BRAND.domain}
          </p>
        </div>
      </footer>
    </div>
  );
}
