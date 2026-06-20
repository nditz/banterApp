"use client";

import { Copy } from "lucide-react";
import { useState } from "react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { TermsAcceptPanel } from "@/components/session/TermsAcceptPanel";
import { Button } from "@/components/ui/button";
import { useNeedsTerms } from "@/hooks/useNeedsTerms";
import { BRAND } from "@/lib/brand";
import { useSession } from "@/hooks/useSession";
import { getStoredRecoveryToken } from "@/lib/session";

export function TermsGate() {
  const { needsTerms } = useNeedsTerms();
  const { data: session } = useSession();
  const [copied, setCopied] = useState(false);

  const showRecoveryKey =
    session?.termsAccepted &&
    !session.authenticated &&
    Boolean(session.recoveryToken ?? getStoredRecoveryToken());

  const recoveryToken = session?.recoveryToken ?? getStoredRecoveryToken();

  const copyKey = async () => {
    if (!recoveryToken) return;
    await navigator.clipboard.writeText(recoveryToken);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  return (
    <>
      <Dialog open={needsTerms}>
        <DialogContent showCloseButton={false} className="z-[100] sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>Terms of Use</DialogTitle>
            <DialogDescription>
              Read and accept the terms below to save your picks to this browser. {BRAND.name} is for
              fun only — not gambling.
            </DialogDescription>
          </DialogHeader>
          <TermsAcceptPanel variant="compact" />
        </DialogContent>
      </Dialog>

      {showRecoveryKey && recoveryToken && (
        <div className="mx-auto mb-4 max-w-[1200px] rounded-md border border-gold/30 bg-gold/5 px-4 py-3">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <div>
              <p className="text-xs font-semibold text-foreground">Your recovery key</p>
              <p className="mt-0.5 text-[11px] text-muted-foreground">
                Copy this encrypted key now. Use it to restore your session if you clear cookies.
              </p>
            </div>
            <Button type="button" size="sm" variant="outline" onClick={copyKey}>
              <Copy className="size-3.5" aria-hidden />
              {copied ? "Copied" : "Copy key"}
            </Button>
          </div>
          <code className="mt-2 block overflow-x-auto rounded-md bg-card px-2 py-1.5 text-[10px] text-muted-foreground">
            {recoveryToken}
          </code>
        </div>
      )}
    </>
  );
}
