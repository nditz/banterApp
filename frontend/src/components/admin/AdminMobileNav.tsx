"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { Menu } from "lucide-react";
import { useState } from "react";
import { adminNavItems, isAdminNavActive } from "@/components/admin/admin-nav-items";
import { Button } from "@/components/ui/button";
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet";
import { cn } from "@/lib/utils";

export function AdminMobileNav() {
  const pathname = usePathname();
  const [openPath, setOpenPath] = useState<string | null>(null);
  const open = openPath === pathname;

  return (
    <>
      <Button
        type="button"
        variant="ghost"
        size="icon-sm"
        className="touch-target text-zinc-100 hover:bg-zinc-800 md:hidden"
        onClick={() => setOpenPath(pathname)}
        aria-label="Open admin menu"
        aria-expanded={open}
        aria-controls="admin-mobile-nav"
      >
        <Menu className="size-5" />
      </Button>

      <Sheet open={open} onOpenChange={(next) => setOpenPath(next ? pathname : null)}>
        <SheetContent
          id="admin-mobile-nav"
          side="left"
          className="w-[min(100vw-2rem,18rem)] border-zinc-800 bg-zinc-950 p-0 text-zinc-100"
        >
          <SheetHeader className="border-b border-zinc-800 px-4 py-4">
            <SheetTitle className="text-left text-base font-semibold text-white">
              Admin Console
            </SheetTitle>
          </SheetHeader>
          <nav className="flex flex-col gap-1 p-3" aria-label="Admin navigation">
            {adminNavItems.map((item) => {
              const active = isAdminNavActive(pathname, item.href, item.exact);
              return (
                <Link
                  key={item.href}
                  href={item.href}
                  className={cn(
                    "rounded-md px-3 py-3 text-sm transition-colors",
                    active
                      ? "bg-zinc-800 text-white"
                      : "text-zinc-400 hover:bg-zinc-900 hover:text-zinc-200"
                  )}
                  aria-current={active ? "page" : undefined}
                  onClick={() => setOpenPath(null)}
                >
                  {item.label}
                </Link>
              );
            })}
          </nav>
        </SheetContent>
      </Sheet>
    </>
  );
}
