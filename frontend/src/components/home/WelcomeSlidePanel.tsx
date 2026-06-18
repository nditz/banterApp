"use client";

import type { ReactNode } from "react";
import { cn } from "@/lib/utils";

interface WelcomeSlidePanelProps {
  backgroundImage: string;
  accent: string;
  ariaLabel?: string;
  children: ReactNode;
  className?: string;
}

/** Full-bleed slide shell with theme-blended background image. */
export function WelcomeSlidePanel({
  backgroundImage,
  accent,
  ariaLabel,
  children,
  className,
}: WelcomeSlidePanelProps) {
  return (
    <div
      className={cn("welcome-slide-panel", `welcome-slide-panel--${accent}`, className)}
      style={{ backgroundImage: `url(${backgroundImage})` }}
      role="group"
      aria-label={ariaLabel}
    >
      <div className="welcome-slide-panel__content">{children}</div>
    </div>
  );
}
