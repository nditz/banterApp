"use client";

import Link from "next/link";
import { Receipt } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState, ErrorState } from "@/components/ui/states";
import { PageContainer, SectionHeader } from "@/components/ui/section-header";
import { PredictionReactionCard } from "@/components/PredictionReactionCard";
import { PredictionReceiptCard } from "@/components/PredictionReceiptCard";
import { useBanterMode } from "@/hooks/useBanterMode";
import { usePredictionHistory } from "@/hooks/usePredictions";
import { useReceipts } from "@/hooks/useReceipts";
import { formatReceiptStoryType } from "@/lib/receipt-story";
import {
  formatPickLabel,
  getFinishedMatchReaction,
  isUnderdogPick,
} from "@/lib/predictionReactions";
import { pickToOutcome } from "@/lib/postMatchResults";
import type { Prediction, PredictionReceipt } from "@/lib/types";
import { getApiErrorMessage } from "@/lib/api";
import { PRODUCT_METRICS, recordMetric } from "@/lib/metrics";
import { useEffect } from "react";

function formatDate(iso: string): string {
  return new Intl.DateTimeFormat("en-GB", {
    month: "short",
    day: "numeric",
    year: "numeric",
  }).format(new Date(iso));
}

function receiptToPrediction(receipt: PredictionReceipt): Prediction {
  return {
    id: receipt.predictionId,
    matchId: receipt.matchId,
    predictionType: receipt.predictionType,
    predictionValue: receipt.predictionValue,
    pointsAwarded: receipt.pointsAwarded,
    createdAt: receipt.createdAt,
    match: receipt.match
      ? {
          id: receipt.match.id ?? receipt.matchId,
          teamA: receipt.match.teamA,
          teamB: receipt.match.teamB,
          teamACode: receipt.match.teamACode,
          teamBCode: receipt.match.teamBCode,
          kickoffTime: receipt.match.kickoffTime,
          status: receipt.matchStatus,
          homeScore: receipt.homeScore ?? undefined,
          awayScore: receipt.awayScore ?? undefined,
        }
      : undefined,
  };
}

export default function PredictionHistoryPage() {
  const { data: receipts, isFetching, isError: receiptsError, error: receiptsErr } =
    useReceipts();
  const { data: predictions, isError: historyError, error: historyErr } = usePredictionHistory();
  const banterMode = useBanterMode();

  const settledIds = new Set((receipts ?? []).map((r) => r.predictionId));
  const pendingFinished = (predictions ?? []).filter((prediction) => {
    const finished = prediction.match?.status === "FT" ||
      (prediction.match?.homeScore != null && prediction.match?.awayScore != null);
    return finished && !settledIds.has(prediction.id);
  });

  const hasReceipts = (receipts?.length ?? 0) > 0;

  // A visit to a settled receipt is the "came back after the result" step of the funnel.
  useEffect(() => {
    recordMetric(PRODUCT_METRICS.receiptViewed);
    if (hasReceipts) {
      recordMetric(PRODUCT_METRICS.returnedAfterResult);
    }
  }, [hasReceipts]);

  return (
    <PageContainer width="narrow">
      <SectionHeader
        as="h1"
        eyebrow="Take ↔ outcome"
        title="Receipts"
        description={
          <>
            Settled picks stay here — private to you. Turn one into a script in{" "}
            <Link href="/studio" className="font-medium text-foreground hover:underline">
              Studio
            </Link>
            .
          </>
        }
      />

      {receiptsError ? (
        <ErrorState
          dense
          title="Receipts could not be loaded"
          description={getApiErrorMessage(receiptsErr)}
        />
      ) : null}
      {historyError ? (
        <ErrorState
          dense
          title="Open picks could not be loaded"
          description={getApiErrorMessage(historyErr)}
        />
      ) : null}

      {isFetching && !receipts ? (
        <div className="space-y-4">
          {Array.from({ length: 3 }).map((_, i) => (
            <Skeleton key={i} className="h-28 w-full rounded-md" />
          ))}
        </div>
      ) : (
        <div className="space-y-4">
          {(receipts ?? []).map((receipt) => (
            <SettledReceipt key={receipt.id} receipt={receipt} banterMode={banterMode} />
          ))}

          {pendingFinished.map((prediction) => (
            <PendingReceiptCard key={prediction.id} prediction={prediction} banterMode={banterMode} />
          ))}

          {(receipts?.length ?? 0) === 0 && pendingFinished.length === 0 && (
            <EmptyState
              icon={Receipt}
              title="No settled receipts yet"
              description="Lock a pick on Matchweek. Receipts land here the moment full time hits."
            />
          )}
        </div>
      )}
    </PageContainer>
  );
}

function SettledReceipt({
  receipt,
  banterMode,
}: {
  receipt: PredictionReceipt;
  banterMode: ReturnType<typeof useBanterMode>;
}) {
  const prediction = receiptToPrediction(receipt);
  const teamA = receipt.match?.teamA ?? "Team A";
  const teamB = receipt.match?.teamB ?? "Team B";
  const fixture = `${teamA} vs ${teamB}`;
  const pick = formatPickLabel(
    receipt.predictionType,
    receipt.predictionValue,
    teamA,
    teamB
  );
  const userPick = pickToOutcome(receipt.predictionType, receipt.predictionValue);
  const underdog =
    userPick != null ? isUnderdogPick(receipt.matchId, teamA, teamB, userPick) : false;
  const reaction = getFinishedMatchReaction(prediction, banterMode, underdog);
  const scoreline =
    receipt.homeScore != null && receipt.awayScore != null
      ? `${receipt.homeScore}–${receipt.awayScore}`
      : null;
  const beaten = receipt.punditTakes.find((t) => !t.wasCorrect);
  const beating = receipt.punditTakes.find((t) => t.wasCorrect);

  return (
    <Card className="border-border shadow-sm">
      <CardHeader className="pb-2">
        <div className="flex items-start justify-between gap-2">
          <div>
            <CardTitle className="text-base">{fixture}</CardTitle>
            <CardDescription>
              {formatDate(receipt.settledAt)} · {formatReceiptStoryType(receipt.storyType)}
            </CardDescription>
          </div>
          <Badge variant={receipt.pointsAwarded > 0 ? "default" : "secondary"}>
            +{receipt.pointsAwarded} pts
          </Badge>
        </div>
      </CardHeader>
      <CardContent className="space-y-3">
        {reaction ? (
          <PredictionReceiptCard
            fixture={fixture}
            pick={pick}
            reaction={reaction}
            createdAt={formatDate(receipt.settledAt)}
          />
        ) : null}

        {scoreline ? (
          <p className="text-xs font-medium text-pitch">Final {scoreline}</p>
        ) : null}

        {beaten ? (
          <p className="text-xs text-muted-foreground">
            You beat {beaten.name}&apos;s sourced pick
            {beaten.sourceUrl ? (
              <>
                {" · "}
                <a
                  href={beaten.sourceUrl}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="underline-offset-2 hover:underline"
                >
                  Source
                </a>
              </>
            ) : null}
          </p>
        ) : beating ? (
          <p className="text-xs text-muted-foreground">
            {beating.name}&apos;s sourced pick landed
            {beating.sourceUrl ? (
              <>
                {" · "}
                <a
                  href={beating.sourceUrl}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="underline-offset-2 hover:underline"
                >
                  Source
                </a>
              </>
            ) : null}
          </p>
        ) : null}

        {reaction ? <PredictionReactionCard reaction={reaction} animate={false} /> : null}

        <Link
          href={`/studio?receipt=${receipt.id}`}
          className="text-xs font-semibold text-foreground hover:underline"
        >
          Open in Studio
        </Link>
      </CardContent>
    </Card>
  );
}

function PendingReceiptCard({
  prediction,
  banterMode,
}: {
  prediction: Prediction;
  banterMode: ReturnType<typeof useBanterMode>;
}) {
  const teamA = prediction.match?.teamA ?? "Team A";
  const teamB = prediction.match?.teamB ?? "Team B";
  const userPick = pickToOutcome(prediction.predictionType, prediction.predictionValue);
  const underdog =
    userPick != null ? isUnderdogPick(prediction.matchId, teamA, teamB, userPick) : false;
  const reaction = getFinishedMatchReaction(prediction, banterMode, underdog);

  return (
    <Card className="border-border shadow-sm">
      <CardHeader className="pb-2">
        <div className="flex items-start justify-between gap-2">
          <div>
            <CardTitle className="text-base">
              {prediction.match ? `${teamA} vs ${teamB}` : `Match ${prediction.matchId}`}
            </CardTitle>
            <CardDescription>Waiting on score-sync to persist this receipt</CardDescription>
          </div>
          <Badge variant={(prediction.pointsAwarded ?? 0) > 0 ? "default" : "secondary"}>
            +{prediction.pointsAwarded ?? 0} pts
          </Badge>
        </div>
      </CardHeader>
      <CardContent>
        {reaction ? <PredictionReactionCard reaction={reaction} animate={false} /> : null}
      </CardContent>
    </Card>
  );
}
