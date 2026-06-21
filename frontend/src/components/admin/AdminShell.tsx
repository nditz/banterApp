"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useEffect } from "react";
import { AdminMobileNav } from "@/components/admin/AdminMobileNav";
import { adminNavItems, isAdminNavActive } from "@/components/admin/admin-nav-items";
import { Skeleton } from "@/components/ui/skeleton";
import { useSession } from "@/hooks/useSession";
import { cn } from "@/lib/utils";

export function AdminShell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const { data: session, isLoading } = useSession();

  useEffect(() => {
    if (!isLoading && !session?.isPlatformAdmin) {
      router.replace("/");
    }
  }, [isLoading, session?.isPlatformAdmin, router]);

  if (isLoading) {
    return (
      <div className="min-h-screen bg-zinc-950 p-4 sm:p-8">
        <Skeleton className="h-8 w-48" />
      </div>
    );
  }

  if (!session?.isPlatformAdmin) {
    return null;
  }

  return (
    <div className="min-h-screen bg-zinc-950 text-zinc-100">
      <header className="safe-area-top border-b border-zinc-800 bg-zinc-900/80 px-4 py-3 sm:px-6 sm:py-4">
        <div className="mx-auto flex max-w-7xl items-center justify-between gap-3">
          <div className="flex min-w-0 items-center gap-2">
            <AdminMobileNav />
            <div className="min-w-0">
              <p className="text-xs uppercase tracking-widest text-zinc-500">Internal</p>
              <h1 className="truncate text-lg font-semibold">Admin Console</h1>
            </div>
          </div>
          <Link
            href="/"
            className="shrink-0 text-sm text-zinc-400 hover:text-white"
          >
            Back to app
          </Link>
        </div>
      </header>
      <div className="mx-auto flex max-w-7xl gap-4 px-4 py-4 sm:gap-8 sm:px-6 sm:py-6 md:py-8">
        <nav
          className="hidden w-48 shrink-0 flex-col gap-1 md:flex"
          aria-label="Admin navigation"
        >
          {adminNavItems.map((item) => {
            const active = isAdminNavActive(pathname, item.href, item.exact);
            return (
              <Link
                key={item.href}
                href={item.href}
                className={cn(
                  "rounded-md px-3 py-2 text-sm transition-colors",
                  active
                    ? "bg-zinc-800 text-white"
                    : "text-zinc-400 hover:bg-zinc-900 hover:text-zinc-200"
                )}
                aria-current={active ? "page" : undefined}
              >
                {item.label}
              </Link>
            );
          })}
        </nav>
        <main className="min-w-0 flex-1">{children}</main>
      </div>
    </div>
  );
}
