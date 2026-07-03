"use client";

import { useState } from "react";
import Link from "next/link";
import { PageWithSideAds } from "@/components/layout/PageWithSideAds";
import { PredictionLockBanner } from "@/components/predictions/PredictionLockBanner";
import {
  CountrySelector,
  PredictionConfirmDialog,
} from "@/components/predictions/CountrySelector";
import {
  useCreateUserPrediction,
  useUpdateUserPrediction,
  useUserPredictions,
} from "@/hooks/useUserPredictions";
import type { FootballCountry } from "@/lib/football-reference/types";
import { Button, buttonVariants } from "@/components/ui/button";
import { cn } from "@/lib/utils";

function CountryPredictionCard({
  predictionType,
  label,
  description,
  existingId,
  existingCountryId,
  isLocked,
  canEdit,
  onSaved,
}: {
  predictionType: string;
  label: string;
  description: string;
  existingId?: string;
  existingCountryId?: string | null;
  isLocked: boolean;
  canEdit: boolean;
  onSaved: () => void;
}) {
  const [countryId, setCountryId] = useState<string | null>(existingCountryId ?? null);
  const [countryName, setCountryName] = useState("");
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const create = useCreateUserPrediction();
  const update = useUpdateUserPrediction();

  const handleSelect = (country: FootballCountry) => {
    setCountryId(country.id);
    setCountryName(country.name);
  };

  const handleSubmit = async () => {
    if (!countryId) return;
    setError(null);
    try {
      if (existingId) {
        await update.mutateAsync({ id: existingId, countryId });
      } else {
        await create.mutateAsync({ predictionType, countryId });
      }
      setConfirmOpen(false);
      onSaved();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to save.");
      setConfirmOpen(false);
    }
  };

  const disabled = isLocked || !canEdit;
  const pending = create.isPending || update.isPending;

  return (
    <section className="rounded-md border border-border bg-card p-4 shadow-sm">
      <h2 className="font-semibold">{label}</h2>
      <p className="mt-1 text-xs text-muted-foreground">{description}</p>
      <div className="mt-4">
        <CountrySelector value={countryId} onChange={handleSelect} disabled={disabled} />
      </div>
      {error && <p className="mt-2 text-sm text-destructive">{error}</p>}
      <Button
        className="mt-4"
        size="sm"
        disabled={disabled || !countryId || pending}
        onClick={() => setConfirmOpen(true)}
      >
        {existingId ? "Update" : "Submit"}
      </Button>
      <PredictionConfirmDialog
        open={confirmOpen}
        title={`Confirm ${label}`}
        description={`Save ${countryName} as your pick?`}
        onConfirm={handleSubmit}
        onCancel={() => setConfirmOpen(false)}
        loading={pending}
      />
    </section>
  );
}

export default function MakePredictionsPage() {
  const { data: status, refetch } = useUserPredictions();

  const winner = status?.categories.find((c) => c.predictionType === "winner_country");
  const finalist = status?.categories.find((c) => c.predictionType === "finalist_country");

  return (
    <PageWithSideAds>
      <div className="mx-auto max-w-2xl space-y-5">
        <header>
          <Link href="/predictions" className={cn(buttonVariants({ variant: "ghost", size: "sm" }), "mb-2")}>
            ← All predictions
          </Link>
          <h1 className="text-xl font-bold">Country predictions</h1>
        </header>

        {status && (
          <PredictionLockBanner
            isLocked={status.isLocked}
            lockDeadline={status.lockDeadline}
          />
        )}

        <CountryPredictionCard
          predictionType="winner_country"
          label="Winning country"
          description="Who lifts the trophy?"
          existingId={winner?.pick?.id}
          existingCountryId={winner?.pick?.countryId}
          isLocked={status?.isLocked ?? false}
          canEdit={status?.canEdit ?? false}
          onSaved={() => refetch()}
        />

        <CountryPredictionCard
          predictionType="finalist_country"
          label="Finalist country"
          description="One team you think reaches the final."
          existingId={finalist?.pick?.id}
          existingCountryId={finalist?.pick?.countryId}
          isLocked={status?.isLocked ?? false}
          canEdit={status?.canEdit ?? false}
          onSaved={() => refetch()}
        />
      </div>
    </PageWithSideAds>
  );
}
