"use client";



import Link from "next/link";

import { useState } from "react";

import { KeyRound, ShieldCheck } from "lucide-react";

import { TermsOfUseContent } from "@/components/legal/TermsOfUseContent";

import { TurnstileWidget } from "@/components/security/TurnstileWidget";

import { useAcceptTerms, useRecoverSession } from "@/hooks/useSession";

import { getApiErrorMessage } from "@/lib/api";
import { BRAND } from "@/lib/brand";

import { COUNTRIES, setCountryCode } from "@/lib/country";

import { getStoredRecoveryToken } from "@/lib/session";

import { Button } from "@/components/ui/button";

import { Input } from "@/components/ui/input";

import { cn } from "@/lib/utils";



interface TermsAcceptPanelProps {

  variant?: "inline" | "compact";

  className?: string;

}



export function TermsAcceptPanel({ variant = "inline", className }: TermsAcceptPanelProps) {

  const acceptTerms = useAcceptTerms();

  const recoverSession = useRecoverSession();

  const [turnstileToken, setTurnstileToken] = useState<string | null>(null);

  const [agreed, setAgreed] = useState(false);

  const [country, setCountry] = useState("");

  const [showRecover, setShowRecover] = useState(false);

  const [recoveryInput, setRecoveryInput] = useState(getStoredRecoveryToken() ?? "");

  const [errorMessage, setErrorMessage] = useState<string | null>(null);



  const handleAccept = async () => {

    if (!agreed) return;

    setErrorMessage(null);

    try {

      const chosen = country.trim() || null;

      setCountryCode(chosen);

      await acceptTerms.mutateAsync({ turnstileToken, countryCode: chosen });

    } catch (err) {

      setErrorMessage(getApiErrorMessage(err));

    }

  };



  const handleRecover = async () => {

    if (!recoveryInput.trim()) return;

    setErrorMessage(null);

    try {

      await recoverSession.mutateAsync({

        recoveryToken: recoveryInput.trim(),

        turnstileToken,

      });

      setShowRecover(false);

    } catch (err) {

      setErrorMessage(getApiErrorMessage(err));

    }

  };



  if (showRecover) {

    return (

      <div

        className={cn(

          "rounded-md border border-border bg-card p-4 shadow-sm",

          variant === "compact" && "p-3",

          className

        )}

      >

        <h2 className="text-sm font-semibold">Restore your session</h2>

        <p className="mt-1 text-xs text-muted-foreground">

          Paste the recovery key you saved earlier to link your picks to this browser again.

        </p>

        <Input

          className="mt-3"

          value={recoveryInput}

          onChange={(e) => setRecoveryInput(e.target.value)}

          placeholder="banter.v1...."

          spellCheck={false}

        />

        <TurnstileWidget onToken={setTurnstileToken} />

        {errorMessage && (

          <p className="mt-2 text-xs text-destructive" role="alert">

            {errorMessage}

          </p>

        )}

        <div className="mt-4 flex flex-wrap gap-2">

          <Button type="button" variant="outline" size="sm" onClick={() => setShowRecover(false)}>

            Back

          </Button>

          <Button

            type="button"

            size="sm"

            className="btn-tournament"

            disabled={!recoveryInput.trim() || !turnstileToken || recoverSession.isPending}

            onClick={handleRecover}

          >

            Restore session

          </Button>

        </div>

      </div>

    );

  }



  return (

    <div

      className={cn(

        "rounded-md border border-gold/30 bg-gold/5 p-4 shadow-sm",

        variant === "compact" && "p-3",

        className

      )}

    >

      <div className="flex items-start gap-2">

        <ShieldCheck className="mt-0.5 size-4 shrink-0 text-muted-foreground" aria-hidden />

        <div>

          <h2 className="text-sm font-semibold text-foreground">Terms of Use</h2>

          <p className="mt-1 text-xs text-muted-foreground">

            {BRAND.name} is open to everyone. Read the terms below, then accept to start predicting.

            No account needed — we create a browser session and give you a recovery key for your picks.

          </p>

        </div>

      </div>



      <div

        className="mt-3 max-h-52 overflow-y-auto rounded-md border border-border bg-card/80 p-3 sm:max-h-60"

        tabIndex={0}

        aria-label="Terms of Use full text"

      >

        <TermsOfUseContent compact showTitle={false} />

      </div>



      <label className="mt-3 flex cursor-pointer items-start gap-2.5">

        <input

          type="checkbox"

          checked={agreed}

          onChange={(e) => setAgreed(e.target.checked)}

          className="mt-0.5 size-4 shrink-0 cursor-pointer rounded border-border accent-pitch"

        />

        <span className="text-xs text-foreground">

          I have read and agree to the{" "}

          <Link href="/terms" className="font-medium text-primary hover:underline" target="_blank">

            Terms of Use

          </Link>

          , including that {BRAND.name} is for fun and entertainment only and is not a gambling site.

        </span>

      </label>



      <div className="mt-3">

        <label htmlFor="country-select" className="text-xs font-medium text-foreground">

          Country (optional)

        </label>

        <select

          id="country-select"

          value={country}

          onChange={(e) => setCountry(e.target.value)}

          className="mt-1 h-9 w-full rounded-md border border-border bg-card px-2 text-sm text-foreground"

        >

          <option value="">No country — Global league only</option>

          {COUNTRIES.map((c) => (

            <option key={c.code} value={c.code}>

              {c.name}

            </option>

          ))}

        </select>

        <p className="mt-1 text-[11px] text-muted-foreground">

          Pick your country to join its league too. Leave blank to just play the Global league.

        </p>

      </div>



      <div className="mt-3">

        <TurnstileWidget onToken={setTurnstileToken} />

      </div>



      {errorMessage && (

        <p className="mt-2 text-xs text-destructive" role="alert">

          {errorMessage}

        </p>

      )}



      <div className="mt-4 flex flex-wrap gap-2">

        <Button

          type="button"

          variant="outline"

          size="sm"

          onClick={() => setShowRecover(true)}

        >

          <KeyRound className="size-3.5" aria-hidden />

          Restore session

        </Button>

        <Button

          type="button"

          size="sm"

          className="btn-tournament"

          disabled={!agreed || !turnstileToken || acceptTerms.isPending}

          onClick={handleAccept}

        >

          Accept &amp; continue

        </Button>

      </div>

    </div>

  );

}


