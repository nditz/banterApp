"use client";

import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { RecoveryKeyPanel } from "@/components/session/RecoveryKeyPanel";
import { TermsAcceptPanel } from "@/components/session/TermsAcceptPanel";
import { useNeedsTerms } from "@/hooks/useNeedsTerms";
import { BRAND } from "@/lib/brand";
import { useSession } from "@/hooks/useSession";
import { getStoredRecoveryToken } from "@/lib/session";

export function TermsGate() {
  const { needsTerms } = useNeedsTerms();
  const { data: session } = useSession();

  const recoveryToken = session?.recoveryToken ?? getStoredRecoveryToken();
  const showRecoveryKey =
    Boolean(session?.termsAccepted) && !session?.authenticated && Boolean(recoveryToken);

  return (
    <>
      <Dialog open={needsTerms}>
        <DialogContent showCloseButton={false} className="z-[100] max-h-[85vh] overflow-y-auto sm:max-w-lg">
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
        <RecoveryKeyPanel recoveryToken={recoveryToken} username={session?.username} />
      )}
    </>
  );
}
