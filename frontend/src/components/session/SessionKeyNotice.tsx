"use client";

import { useState } from "react";
import { Check, Copy, KeyRound } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useSession } from "@/hooks/useSession";
import { getStoredRecoveryToken } from "@/lib/session";
import { cn } from "@/lib/utils";

interface SessionKeyNoticeProps {
  className?: string;
  /** Compact variant for embedding inside flows (e.g. after joining a league). */
  compact?: boolean;
}

/**
 * The no-signup promise: surfaces the guest session key with clear guidance to
 * copy it somewhere safe (a notepad) so the session can be restored anywhere.
 */
export function SessionKeyNotice({ className, compact = false }: SessionKeyNoticeProps) {
  const { data: session } = useSession();
  const [copied, setCopied] = useState(false);

  const sessionKey =
    session?.recoveryToken ?? getStoredRecoveryToken() ?? null;

  // Registered users don't need a session key; guests without one see nothing yet
  if (session?.authenticated || !sessionKey) {
    return null;
  }

  const handleCopy = async () => {
    await navigator.clipboard.writeText(sessionKey);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  return (
    <aside
      className={cn(
        "rounded-lg border border-gold/40 bg-gold/10 p-4",
        compact && "p-3",
        className
      )}
      aria-label="Your session key"
    >
      <div className="flex items-start gap-2.5">
        <span className="mt-0.5 flex size-7 shrink-0 items-center justify-center rounded-full bg-gold/20">
          <KeyRound className="size-3.5 text-gold" aria-hidden />
        </span>
        <div className="min-w-0 flex-1">
          <p className="text-sm font-semibold">
            No signup needed — just keep your session key safe
          </p>
          <p className="mt-1 text-xs text-muted-foreground">
            Your picks, points and leagues live on this key. Copy it into a
            notepad (or anywhere safe) — paste it on any device to restore your
            session. Lose the key, lose the data.
          </p>
          <div className="mt-2 flex items-center gap-2">
            <code className="min-w-0 flex-1 truncate rounded-md border border-border bg-card px-2 py-1.5 font-mono text-[11px]">
              {sessionKey}
            </code>
            <Button
              type="button"
              variant="outline"
              size="sm"
              className="h-8 shrink-0 text-xs"
              onClick={handleCopy}
            >
              {copied ? (
                <Check className="size-3.5 text-pitch" aria-hidden />
              ) : (
                <Copy className="size-3.5" aria-hidden />
              )}
              {copied ? "Copied" : "Copy key"}
            </Button>
          </div>
        </div>
      </div>
    </aside>
  );
}
