"use client";

import Link from "next/link";
import { buttonVariants } from "@/components/ui/button";
import { PredictionLockBanner } from "@/components/predictions/PredictionLockBanner";
import { useUserPredictions } from "@/hooks/useUserPredictions";
import { PageWithSideAds } from "@/components/layout/PageWithSideAds";
import { cn } from "@/lib/utils";
import { PREDICTION_ROUTES } from "@/lib/football-reference/types";
import { useSession } from "@/hooks/useSession";

export default function PredictionsHubPage() {
  const { data: session } = useSession();
  const { data: status, isLoading } = useUserPredictions();

  return (
    <PageWithSideAds>
      <div className="mx-auto max-w-3xl space-y-5">
        <header>
          <h1 className="text-xl font-bold sm:text-2xl">Tournament predictions</h1>
          <p className="mt-2 text-sm text-muted-foreground">
            Pick winners, finalists, and award favourites backed by synced squad data.
          </p>
        </header>

        {!session?.authenticated && (
          <div className="rounded-md border border-border bg-card p-4 text-sm">
            <Link href="/auth/login" className="font-medium text-primary underline">
              Sign in
            </Link>{" "}
            to save your predictions.
          </div>
        )}

        {status && (
          <PredictionLockBanner
            isLocked={status.isLocked}
            lockDeadline={status.lockDeadline}
          />
        )}

        {isLoading && <p className="text-sm text-muted-foreground">Loading…</p>}

        <ul className="space-y-3">
          {status?.categories.map((cat) => (
            <li
              key={cat.predictionType}
              className="rounded-md border border-border bg-card p-4 shadow-sm"
            >
              <div className="flex flex-wrap items-start justify-between gap-3">
                <div>
                  <h2 className="font-semibold">{cat.label}</h2>
                  <p className="mt-1 text-xs text-muted-foreground">{cat.description}</p>
                  {cat.pick && (
                    <p className="mt-2 text-sm">
                      Your pick:{" "}
                      <span className="font-medium">
                        {cat.pick.playerName ?? cat.pick.countryName ?? "—"}
                      </span>
                    </p>
                  )}
                </div>
                <Link
                  href={PREDICTION_ROUTES[cat.predictionType as keyof typeof PREDICTION_ROUTES] ?? "/predictions/make"}
                  className={cn(
                    buttonVariants({ variant: "outline", size: "sm" }),
                    (!status.canEdit || !session?.authenticated) && "pointer-events-none opacity-50"
                  )}
                >
                  {cat.pick ? "Edit" : "Pick"}
                </Link>
              </div>
            </li>
          ))}
        </ul>

        <Link href="/predictions/make" className={buttonVariants({ variant: "default", size: "sm" })}>
          Country predictions
        </Link>
      </div>
    </PageWithSideAds>
  );
}
