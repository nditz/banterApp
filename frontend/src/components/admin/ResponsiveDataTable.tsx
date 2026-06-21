"use client";

import type { ReactNode } from "react";
import { cn } from "@/lib/utils";

interface ResponsiveDataTableProps {
  children: ReactNode;
  mobileCards?: ReactNode;
  className?: string;
  tableClassName?: string;
  minWidth?: string;
}

export function ResponsiveDataTable({
  children,
  mobileCards,
  className,
  tableClassName,
  minWidth,
}: ResponsiveDataTableProps) {
  return (
    <>
      {mobileCards && (
        <div className="space-y-3 md:hidden" aria-label="Data list">
          {mobileCards}
        </div>
      )}
      <div
        className={cn(
          "table-scroll-container rounded-lg border border-zinc-800",
          mobileCards ? "hidden md:block" : "",
          className
        )}
      >
        <table
          className={cn("w-full text-left text-sm", tableClassName)}
          style={minWidth ? { minWidth } : undefined}
        >
          {children}
        </table>
      </div>
    </>
  );
}

export function AdminMobileCard({
  children,
  className,
}: {
  children: ReactNode;
  className?: string;
}) {
  return (
    <div
      className={cn(
        "rounded-lg border border-zinc-800 bg-zinc-900/50 p-4 text-sm",
        className
      )}
    >
      {children}
    </div>
  );
}

export function AdminMobileCardRow({
  label,
  children,
}: {
  label: string;
  children: ReactNode;
}) {
  return (
    <div className="flex flex-wrap items-start justify-between gap-2 py-1.5">
      <span className="text-xs uppercase tracking-wide text-zinc-500">{label}</span>
      <div className="min-w-0 text-right break-anywhere">{children}</div>
    </div>
  );
}
