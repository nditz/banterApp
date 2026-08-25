"use client";

import Link from "next/link";
import { useState } from "react";
import { Button } from "@/components/ui/button";
import { useConsent } from "@/hooks/useConsent";

/**
 * Cookie and analytics consent.
 *
 * Accept and Reject are rendered with identical prominence, nothing is pre-selected,
 * and there is no dismiss control that could be read as agreement. Consent is never
 * inferred from continued browsing.
 */
export function ConsentBanner() {
  const { consent, ready, acceptAll, rejectAll, save } = useConsent();
  const [customizing, setCustomizing] = useState(false);
  const [analytics, setAnalytics] = useState(false);
  const [marketing, setMarketing] = useState(false);
  const [saving, setSaving] = useState(false);

  if (!ready || consent) {
    return null;
  }

  const run = async (action: () => Promise<void>) => {
    setSaving(true);
    try {
      await action();
    } finally {
      setSaving(false);
    }
  };

  return (
    <div
      role="dialog"
      aria-modal="false"
      aria-labelledby="consent-banner-title"
      className="fixed inset-x-0 bottom-0 z-50 border-t border-border bg-background/95 p-4 backdrop-blur supports-[backdrop-filter]:bg-background/80"
    >
      <div className="mx-auto flex max-w-4xl flex-col gap-4">
        <div>
          <h2 id="consent-banner-title" className="text-sm font-semibold">
            Your privacy choices
          </h2>
          <p className="mt-1 text-sm text-muted-foreground">
            We use strictly necessary storage to keep you signed in and to remember your
            predictions — that part cannot be turned off. Everything else is optional and off
            until you say otherwise. Declining keeps every feature working.{" "}
            <Link href="/privacy" className="font-medium text-foreground hover:underline">
              Read the privacy policy
            </Link>
            .
          </p>
        </div>

        {customizing ? (
          <fieldset className="space-y-3 rounded-lg border border-border p-3">
            <legend className="px-1 text-xs font-medium uppercase tracking-wide text-muted-foreground">
              Optional categories
            </legend>

            <ConsentToggle
              id="consent-analytics"
              label="Product analytics"
              description="First-party, aggregated usage events so we can see which features are worth keeping. No third-party analytics service is used."
              checked={analytics}
              onChange={setAnalytics}
            />

            <ConsentToggle
              id="consent-marketing"
              label="Advertising"
              description="Lets Google AdSense load. Without this, ad slots stay empty and no advertising cookies are set."
              checked={marketing}
              onChange={setMarketing}
            />
          </fieldset>
        ) : null}

        <div className="flex flex-col gap-2 sm:flex-row sm:items-center">
          {customizing ? (
            <Button
              size="lg"
              className="flex-1"
              disabled={saving}
              onClick={() => run(() => save({ analytics, marketing }))}
            >
              Save choices
            </Button>
          ) : (
            <>
              <Button
                size="lg"
                className="flex-1"
                disabled={saving}
                onClick={() => run(acceptAll)}
              >
                Accept all
              </Button>
              <Button
                size="lg"
                className="flex-1"
                disabled={saving}
                onClick={() => run(rejectAll)}
              >
                Reject all
              </Button>
            </>
          )}
          <Button
            size="lg"
            variant="ghost"
            className="sm:flex-none"
            disabled={saving}
            onClick={() => setCustomizing((value) => !value)}
          >
            {customizing ? "Back" : "Customise"}
          </Button>
        </div>
      </div>
    </div>
  );
}

function ConsentToggle({
  id,
  label,
  description,
  checked,
  onChange,
}: {
  id: string;
  label: string;
  description: string;
  checked: boolean;
  onChange: (value: boolean) => void;
}) {
  return (
    <div className="flex items-start gap-3">
      <input
        id={id}
        type="checkbox"
        checked={checked}
        onChange={(e) => onChange(e.target.checked)}
        className="mt-1 size-4 shrink-0 accent-primary"
      />
      <label htmlFor={id} className="cursor-pointer text-sm">
        <span className="font-medium">{label}</span>
        <span className="mt-0.5 block text-xs text-muted-foreground">{description}</span>
      </label>
    </div>
  );
}
