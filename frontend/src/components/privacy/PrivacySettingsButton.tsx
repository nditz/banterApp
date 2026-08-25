"use client";

import { useConsent } from "@/hooks/useConsent";
import { cn } from "@/lib/utils";

/**
 * Reopens the consent banner so a decision can be changed at any time. Withdrawal must
 * be as easy as giving consent, so this is a plain always-visible control rather than
 * something buried behind a menu.
 */
export function PrivacySettingsButton({ className }: { className?: string }) {
  const { reopen } = useConsent();

  return (
    <button
      type="button"
      onClick={reopen}
      className={cn("font-semibold text-foreground hover:underline", className)}
    >
      Privacy settings
    </button>
  );
}
