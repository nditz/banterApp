"use client";

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
} from "react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { RecoveryKeyPanel } from "@/components/session/RecoveryKeyPanel";
import { TermsAcceptPanel } from "@/components/session/TermsAcceptPanel";
import { useNeedsTerms } from "@/hooks/useNeedsTerms";
import { useSession } from "@/hooks/useSession";
import { BRAND } from "@/lib/brand";
import { getStoredRecoveryToken } from "@/lib/session";

type TermsResolver = (accepted: boolean) => void;

interface TermsSaveGateValue {
  needsTerms: boolean;
  requireTerms: () => Promise<boolean>;
}

const TermsSaveGateContext = createContext<TermsSaveGateValue | null>(null);

export function useTermsSaveGate(): TermsSaveGateValue {
  const ctx = useContext(TermsSaveGateContext);
  if (!ctx) {
    return {
      needsTerms: false,
      requireTerms: async () => true,
    };
  }
  return ctx;
}

export function TermsSaveGateProvider({ children }: { children: React.ReactNode }) {
  const { needsTerms, isLoading } = useNeedsTerms();
  const { data: session } = useSession();
  const [requested, setRequested] = useState(false);
  const resolverRef = useRef<TermsResolver | null>(null);

  const settle = useCallback((accepted: boolean) => {
    setRequested(false);
    resolverRef.current?.(accepted);
    resolverRef.current = null;
  }, []);

  const requireTerms = useCallback(() => {
    return new Promise<boolean>((resolve) => {
      if (!isLoading && !needsTerms) {
        resolve(true);
        return;
      }
      resolverRef.current?.(false);
      resolverRef.current = resolve;
      setRequested(true);
    });
  }, [isLoading, needsTerms]);

  const open = requested && !isLoading && needsTerms;

  useEffect(() => {
    if (!requested || isLoading || needsTerms) return;
    // Pending save waited on the terms query; no dialog needed.
    // eslint-disable-next-line react-hooks/set-state-in-effect -- resolve the waiting promise after the query settles
    settle(true);
  }, [requested, isLoading, needsTerms, settle]);

  const recoveryToken = session?.recoveryToken ?? getStoredRecoveryToken();
  const showRecoveryKey =
    Boolean(session?.termsAccepted) && !session?.authenticated && Boolean(recoveryToken);

  const value = useMemo(
    () => ({ needsTerms, requireTerms }),
    [needsTerms, requireTerms]
  );

  return (
    <TermsSaveGateContext.Provider value={value}>
      {children}
      <Dialog
        open={open}
        onOpenChange={(next) => {
          if (!next) settle(false);
        }}
      >
        <DialogContent
          showCloseButton={false}
          className="z-[100] max-h-[85vh] overflow-y-auto sm:max-w-lg"
        >
          <DialogHeader>
            <DialogTitle>Terms of Use</DialogTitle>
            <DialogDescription>
              Accept the terms to save this to your browser. You can keep browsing
              without accepting — picks, follows and leagues won&apos;t save until you
              do. {BRAND.name} is for fun only — not gambling.
            </DialogDescription>
          </DialogHeader>
          <TermsAcceptPanel variant="compact" onComplete={() => settle(true)} />
          <Button
            type="button"
            variant="ghost"
            size="sm"
            className="mt-1 h-8 w-full text-xs text-muted-foreground"
            onClick={() => settle(false)}
          >
            Keep browsing
          </Button>
        </DialogContent>
      </Dialog>
      {showRecoveryKey && recoveryToken ? (
        <RecoveryKeyPanel recoveryToken={recoveryToken} username={session?.username} />
      ) : null}
    </TermsSaveGateContext.Provider>
  );
}
