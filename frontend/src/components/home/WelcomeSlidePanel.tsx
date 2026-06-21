"use client";

import type { ReactNode } from "react";
import { cn } from "@/lib/utils";

interface WelcomeSlidePanelProps {
  accent: string;
  ariaLabel?: string;
  children: ReactNode;
  className?: string;
}

/** Slide shell with accent glow — content carries its own imagery via WelcomeSlideBody. */
export function WelcomeSlidePanel({
  accent,
  ariaLabel,
  children,
  className,
}: WelcomeSlidePanelProps) {
  return (
    <div
      className={cn("welcome-slide-panel", `welcome-slide-panel--${accent}`, className)}
      role="group"
      aria-label={ariaLabel}
    >
      <div className="welcome-slide-panel__content">{children}</div>
    </div>
  );
}
