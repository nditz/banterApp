"use client";

import Image from "next/image";
import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { ChevronDown, KeyRound, LogIn, LogOut, Menu, Shield, User, X } from "lucide-react";
import { useEffect, useRef, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { MobileBottomNav } from "@/components/layout/MobileBottomNav";
import { ThemeToggle } from "@/components/layout/ThemeToggle";
import { TermsEntertainmentNotice } from "@/components/legal/TermsOfUseContent";
import { SessionKeyRestore } from "@/components/session/SessionKeyRestore";
import { TermsGate } from "@/components/session/TermsGate";
import { TurnstileProvider } from "@/components/security/TurnstileProvider";
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet";
import { useSession } from "@/hooks/useSession";
import { useSupabaseUser } from "@/hooks/useSupabaseUser";
import { BRAND } from "@/lib/brand";
import { Button, buttonVariants } from "@/components/ui/button";
import { createClient } from "@/lib/supabase/client";
import { cn } from "@/lib/utils";

const navLinks = [
  { href: "/", label: "Home" },
  { href: "/matchweek", label: "Matchweek" },
  { href: "/table", label: "Table" },
  { href: "/awards", label: "Awards" },
  { href: "/leagues", label: "Leagues" },
  { href: "/studio", label: "Studio" },
  { href: "/rules", label: "Rules" },
  { href: "/predictions/history", label: "History" },
];

interface AppShellProps {
  children: React.ReactNode;
}

export function AppShell({ children }: AppShellProps) {
  const pathname = usePathname();
  const router = useRouter();
  const queryClient = useQueryClient();
  const [mobileMenuPath, setMobileMenuPath] = useState<string | null>(null);
  const [restoreOpenPath, setRestoreOpenPath] = useState<string | null>(null);
  const [restoreSheetPath, setRestoreSheetPath] = useState<string | null>(null);
  const [accountOpenPath, setAccountOpenPath] = useState<string | null>(null);
  const [loggingOut, setLoggingOut] = useState(false);
  const mobileMenuOpen = mobileMenuPath === pathname;
  const restoreOpen = restoreOpenPath === pathname;
  const restoreSheetOpen = restoreSheetPath === pathname;
  const accountOpen = accountOpenPath === pathname;
  const restoreRef = useRef<HTMLDivElement>(null);
  const accountRef = useRef<HTMLDivElement>(null);
  const { data: session } = useSession();
  const { email } = useSupabaseUser();
  const isAuthenticated = session?.authenticated ?? false;
  const isAdminRoute = pathname.startsWith("/admin");
  const isAuthRoute = pathname.startsWith("/auth");
  const mobileMenuId = "app-mobile-menu";

  const handleLogout = async () => {
    setLoggingOut(true);
    try {
      const supabase = createClient();
      if (supabase) {
        await supabase.auth.signOut();
      }
      await queryClient.invalidateQueries({ queryKey: ["session"] });
      setMobileMenuPath(null);
      setAccountOpenPath(null);
      router.push("/");
      router.refresh();
    } finally {
      setLoggingOut(false);
    }
  };

  useEffect(() => {
    if (!restoreOpen) return;
    const handleClickOutside = (event: MouseEvent) => {
      if (restoreRef.current && !restoreRef.current.contains(event.target as Node)) {
        setRestoreOpenPath(null);
      }
    };
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, [restoreOpen]);

  useEffect(() => {
    if (!accountOpen) return;
    const handleClickOutside = (event: MouseEvent) => {
      if (accountRef.current && !accountRef.current.contains(event.target as Node)) {
        setAccountOpenPath(null);
      }
    };
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, [accountOpen]);

  if (isAdminRoute) {
    return <>{children}</>;
  }

  const openSessionRestore = () => {
    if (typeof window !== "undefined" && window.matchMedia("(max-width: 639px)").matches) {
      setRestoreSheetPath(pathname);
    } else {
      setRestoreOpenPath((current) => (current === pathname ? null : pathname));
    }
  };

  const handleLogoClick = (event: React.MouseEvent<HTMLAnchorElement>) => {
    if (pathname !== "/") {
      return;
    }

    event.preventDefault();
    if (typeof window !== "undefined" && window.location.hash) {
      router.push("/");
    }
    window.scrollTo({ top: 0, behavior: "smooth" });
  };

  return (
    <div className="stadium-bg flex min-h-screen flex-col">
      <header
        className="safe-area-top sticky top-0 z-50 text-white"
        style={{ backgroundColor: BRAND.headerBackground }}
      >
        <div className="mx-auto flex h-14 max-w-[1400px] items-center justify-between gap-4 px-4 sm:px-6">
          <div className="flex items-center gap-3">
            <Button
              variant="ghost"
              size="icon-sm"
              className="touch-target text-white hover:bg-white/10 lg:hidden"
              onClick={() =>
                setMobileMenuPath(mobileMenuOpen ? null : pathname)
              }
              aria-label={mobileMenuOpen ? "Close menu" : "Open menu"}
              aria-expanded={mobileMenuOpen}
              aria-controls={mobileMenuId}
            >
              {mobileMenuOpen ? <X /> : <Menu />}
            </Button>

            <Link href="/" onClick={handleLogoClick} className="group flex shrink-0 items-center">
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
                    ? "bg-white/10 text-white"
                    : "text-white/70 hover:bg-white/10 hover:text-white"
                )}
                aria-current={pathname === link.href ? "page" : undefined}
              >
                {link.label}
              </Link>
            ))}
            {session?.isPlatformAdmin && (
              <Link
                href="/admin"
                className={cn(
                  "flex items-center gap-1.5 rounded-md px-3 py-2 text-xs font-bold uppercase tracking-wider transition-colors",
                  isAdminRoute
                    ? "bg-white/10 text-white"
                    : "text-amber-300/90 hover:bg-white/10 hover:text-amber-200"
                )}
                aria-current={isAdminRoute ? "page" : undefined}
              >
                <Shield className="size-3.5" aria-hidden />
                Admin
              </Link>
            )}
          </nav>

          <div className="flex items-center gap-2">
            <ThemeToggle className="touch-target text-white hover:bg-white/10 hover:text-white" />
            {!isAuthenticated && (
              <>
                <Button
                  variant="ghost"
                  size="icon-sm"
                  className="touch-target text-white hover:bg-white/10 sm:hidden"
                  onClick={openSessionRestore}
                  aria-label="Restore session with key"
                  title="Restore session"
                >
                  <KeyRound className="size-4" />
                </Button>
                <div className="relative hidden sm:block" ref={restoreRef}>
                <Button
                  variant="ghost"
                  size="icon-sm"
                  className="touch-target text-white hover:bg-white/10"
                  onClick={openSessionRestore}
                  aria-label="Restore session with key"
                  title="Restore session"
                >
                  <KeyRound className="size-4" />
                </Button>
                {restoreOpen && (
                  <div className="absolute right-0 top-full z-50 mt-1.5 w-[min(18rem,calc(100vw-2rem))] sm:w-72">
                    <SessionKeyRestore onClose={() => setRestoreOpenPath(null)} />
                  </div>
                )}
              </div>
              </>
            )}

            {isAuthenticated ? (
              <div className="relative" ref={accountRef}>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() =>
                    setAccountOpenPath((current) =>
                      current === pathname ? null : pathname
                    )
                  }
                  className="h-8 gap-1.5 px-2 text-xs font-bold uppercase tracking-wider text-white/80 hover:bg-white/10 hover:text-white sm:px-3"
                  aria-label="Account menu"
                  aria-expanded={accountOpen}
                  aria-haspopup="menu"
                >
                  <User className="size-4" aria-hidden />
                  <span className="hidden max-w-[10rem] truncate normal-case tracking-normal sm:inline">
                    {email ?? "Account"}
                  </span>
                  <ChevronDown className="hidden size-3.5 sm:inline" aria-hidden />
                </Button>
                {accountOpen && (
                  <div
                    role="menu"
                    className="absolute right-0 top-full z-50 mt-1.5 w-[min(16rem,calc(100vw-2rem))] overflow-hidden rounded-lg border border-border bg-popover text-popover-foreground shadow-lg"
                  >
                    <div className="border-b border-border px-3 py-2.5">
                      <p className="text-[10px] font-semibold uppercase tracking-wider text-muted-foreground">
                        Signed in as
                      </p>
                      <p className="mt-0.5 truncate text-sm font-medium">
                        {email ?? "Your account"}
                      </p>
                    </div>
                    {session?.isPlatformAdmin && (
                      <Link
                        href="/admin"
                        role="menuitem"
                        onClick={() => setAccountOpenPath(null)}
                        className="flex w-full items-center gap-2 px-3 py-2.5 text-sm font-medium text-amber-600 hover:bg-muted"
                      >
                        <Shield className="size-4" aria-hidden />
                        Admin
                      </Link>
                    )}
                    <button
                      type="button"
                      role="menuitem"
                      onClick={handleLogout}
                      disabled={loggingOut}
                      className="flex w-full items-center gap-2 px-3 py-2.5 text-sm font-medium hover:bg-muted disabled:opacity-60"
                    >
                      <LogOut className="size-4" aria-hidden />
                      {loggingOut ? "Logging out..." : "Log out"}
                    </button>
                  </div>
                )}
              </div>
            ) : (
              <>
                <Link
                  href="/auth/login"
                  className={cn(
                    buttonVariants({ variant: "ghost", size: "sm" }),
                    "h-8 min-w-8 px-2 text-xs font-bold uppercase tracking-wider text-white/80 hover:bg-white/10 hover:text-white sm:px-3"
                  )}
                  aria-label="Log in"
                >
                  <LogIn className="size-4 sm:hidden" aria-hidden />
                  <span className="hidden sm:inline">Log in</span>
                </Link>
                <Link
                  href="/auth/register"
                  className={cn(
                    buttonVariants({ size: "sm" }),
                    "btn-tournament h-8 px-3 text-xs sm:px-4"
                  )}
                >
                  Join free
                </Link>
              </>
            )}
          </div>
        </div>

        <div className="header-accent-rule" aria-hidden />

        {mobileMenuOpen && (
          <nav
            id={mobileMenuId}
            className="border-t border-white/10 px-4 py-3 lg:hidden"
            style={{ backgroundColor: BRAND.headerBackground }}
            aria-label="Mobile navigation"
          >
            <div className="flex flex-col gap-0.5">
              {navLinks.map((link) => (
                <Link
                  key={link.href}
                  href={link.href}
                  onClick={() => setMobileMenuPath(null)}
                  className={cn(
                    "min-h-11 rounded-md px-3 py-2.5 text-xs font-bold uppercase tracking-wider",
                    pathname === link.href
                      ? "bg-white/10 text-white"
                      : "text-white/75 hover:bg-white/10"
                  )}
                  aria-current={pathname === link.href ? "page" : undefined}
                >
                  {link.label}
                </Link>
              ))}
              {session?.isPlatformAdmin && (
                <Link
                  href="/admin"
                  onClick={() => setMobileMenuPath(null)}
                  className={cn(
                    "flex min-h-11 items-center gap-2 rounded-md px-3 py-2.5 text-xs font-bold uppercase tracking-wider",
                    isAdminRoute
                      ? "bg-white/10 text-white"
                      : "text-amber-300/90 hover:bg-white/10"
                  )}
                  aria-current={isAdminRoute ? "page" : undefined}
                >
                  <Shield className="size-4" aria-hidden />
                  Admin
                </Link>
              )}
              {!isAuthenticated ? (
                <div className="mt-2 border-t border-white/10 pt-2">
                  {restoreOpen ? (
                    <SessionKeyRestore
                      onClose={() => {
                        setRestoreOpenPath(null);
                        setMobileMenuPath(null);
                      }}
                    />
                  ) : (
                    <button
                      type="button"
                      onClick={() => setRestoreOpenPath(pathname)}
                      className="flex min-h-11 w-full cursor-pointer items-center gap-2 rounded-md px-3 py-2.5 text-xs font-bold uppercase tracking-wider text-white/75 hover:bg-white/10"
                    >
                      <KeyRound className="size-4" aria-hidden />
                      Restore session with key
                    </button>
                  )}
                </div>
              ) : (
                <div className="mt-2 border-t border-white/10 pt-2">
                  {email && (
                    <p className="truncate px-3 pb-1.5 text-[11px] font-medium normal-case tracking-normal text-white/60">
                      {email}
                    </p>
                  )}
                  <button
                    type="button"
                    onClick={handleLogout}
                    disabled={loggingOut}
                    className="flex min-h-11 w-full cursor-pointer items-center gap-2 rounded-md px-3 py-2.5 text-xs font-bold uppercase tracking-wider text-white/75 hover:bg-white/10 disabled:opacity-60"
                  >
                    <LogOut className="size-4" aria-hidden />
                    {loggingOut ? "Logging out..." : "Log out"}
                  </button>
                </div>
              )}
            </div>
          </nav>
        )}
      </header>

      <main
        className={cn(
          "relative z-[1] flex-1 px-4 py-4 sm:px-6 sm:py-5",
          !isAuthRoute && "main-with-bottom-nav"
        )}
      >
        <TurnstileProvider />
        {!isAuthRoute && <TermsGate />}
        {children}
      </main>

      {!isAuthRoute && <MobileBottomNav />}

      {!isAuthRoute && (
        <footer className="site-footer relative z-[1] mt-auto border-t border-border py-6 text-muted-foreground">
          <div className="mx-auto max-w-[1400px] space-y-3 px-4 text-center text-xs sm:px-6">
            <Link href="/" onClick={handleLogoClick} className="mx-auto inline-flex justify-center">
              <span className="inline-flex rounded-md bg-black px-3 py-1">
                <Image
                  src={BRAND.logoFooter}
                  alt={BRAND.name}
                  width={168}
                  height={110}
                  className="h-14 w-auto max-w-[min(220px,60vw)] object-contain opacity-90"
                  unoptimized
                />
              </span>
            </Link>
            <p>
              {BRAND.tagline} · Premier League predictions · Not affiliated with the Premier League or any football governing body
            </p>

            <TermsEntertainmentNotice className="border-border bg-muted/40 text-muted-foreground [&_a]:text-foreground [&_a]:hover:underline [&_strong]:text-foreground" />
            <p>
              <Link href="/terms" className="font-semibold text-foreground hover:underline">
                Terms of Use
              </Link>
              {" · "}
              <Link href="/privacy" className="font-semibold text-foreground hover:underline">
                Privacy Policy
              </Link>
            </p>

            <p className="text-xs leading-relaxed">
              All images, media, and AI-generated content on this platform are produced using
              artificial intelligence tools for entertainment and social fun. Content is not sourced
              from real news publications unless explicitly credited and is not intended to
              represent factual reporting.
            </p>

            <p>
              © {new Date().getFullYear()} {BRAND.name} · {BRAND.domain}
            </p>
          </div>
        </footer>
      )}

      <Sheet
        open={restoreSheetOpen}
        onOpenChange={(next) => setRestoreSheetPath(next ? pathname : null)}
      >
        <SheetContent side="bottom" className="max-h-[85vh] overflow-y-auto">
          <SheetHeader>
            <SheetTitle>Restore session</SheetTitle>
          </SheetHeader>
          <SessionKeyRestore onClose={() => setRestoreSheetPath(null)} />
        </SheetContent>
      </Sheet>
    </div>
  );
}
