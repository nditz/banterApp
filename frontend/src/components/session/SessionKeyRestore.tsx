"use client";

import { useState } from "react";
import { Check, KeyRound, Loader2, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useRecoverSession } from "@/hooks/useSession";
import { getDeviceFingerprint } from "@/lib/fingerprint";
import { cn } from "@/lib/utils";

interface SessionKeyRestoreProps {
  onClose?: () => void;
  className?: string;
}

/**
 * Inline form that accepts a pasted recovery key and restores the guest session.
 * Sits inside the header account menu or anywhere else.
 */
export function SessionKeyRestore({ onClose, className }: SessionKeyRestoreProps) {
  const [key, setKey] = useState("");
  const [done, setDone] = useState(false);
  const recover = useRecoverSession();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const trimmed = key.trim();
    if (!trimmed) return;

    const fingerprint = await getDeviceFingerprint();
    recover.mutate(
      { recoveryToken: trimmed, turnstileToken: null, deviceFingerprint: fingerprint },
      {
        onSuccess: () => {
          setDone(true);
          setTimeout(() => onClose?.(), 1500);
        },
      }
    );
  };

  if (done) {
    return (
      <div className={cn("flex items-center gap-2 rounded-lg border border-pitch/40 bg-pitch/10 px-3 py-2 text-sm text-pitch", className)}>
        <Check className="size-4 shrink-0" aria-hidden />
        Session restored — welcome back!
      </div>
    );
  }

  return (
    <form
      onSubmit={handleSubmit}
      className={cn("rounded-lg border border-border bg-card p-3 shadow-md", className)}
    >
      <div className="mb-2 flex items-center justify-between gap-2">
        <p className="flex items-center gap-1.5 text-xs font-semibold text-foreground">
          <KeyRound className="size-3.5 text-gold" aria-hidden />
          Restore your session
        </p>
        {onClose && (
          <button
            type="button"
            onClick={onClose}
            className="rounded p-0.5 text-muted-foreground hover:text-foreground"
            aria-label="Close"
          >
            <X className="size-3.5" />
          </button>
        )}
      </div>
      <p className="mb-2 text-[11px] text-muted-foreground">
        Paste the key you saved earlier. If you use it on a new device, your old
        browser session will be signed out automatically.
      </p>
      <textarea
        value={key}
        onChange={(e) => setKey(e.target.value)}
        placeholder="banter.v1...."
        rows={2}
        aria-label="Recovery key"
        className="w-full resize-none rounded-md border border-border bg-background px-2.5 py-1.5 font-mono text-[11px] text-foreground placeholder:text-muted-foreground/60 focus:outline-none focus:ring-1 focus:ring-pitch"
      />
      {recover.isError && (
        <p className="mt-1 text-[11px] text-destructive" role="alert">
          {(recover.error as Error)?.message ?? "Invalid or expired key."}
        </p>
      )}
      <Button
        type="submit"
        size="sm"
        className="mt-2 h-8 w-full text-xs"
        disabled={!key.trim() || recover.isPending}
      >
        {recover.isPending && <Loader2 className="mr-1.5 size-3.5 animate-spin" aria-hidden />}
        Restore session
      </Button>
    </form>
  );
}
