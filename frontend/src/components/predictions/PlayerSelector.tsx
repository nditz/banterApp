"use client";

import { useMemo, useState } from "react";
import Image from "next/image";
import { Button } from "@/components/ui/button";
import { useFootballCountries, useFootballPlayers } from "@/hooks/useFootballReference";
import {
  useCreateUserPrediction,
  useUpdateUserPrediction,
} from "@/hooks/useUserPredictions";
import type { FootballPlayer } from "@/lib/football-reference/types";
import { cn } from "@/lib/utils";
import { PredictionConfirmDialog } from "@/components/predictions/CountrySelector";

export function PlayerSelector({
  value,
  onChange,
  disabled,
  countryFilterId,
}: {
  value: string | null;
  onChange: (player: FootballPlayer) => void;
  disabled?: boolean;
  countryFilterId?: string | null;
}) {
  const [search, setSearch] = useState("");
  const [countryId, setCountryId] = useState<string | null>(countryFilterId ?? null);
  const { data: countries = [] } = useFootballCountries();
  const { data: players = [], isLoading } = useFootballPlayers({
    search: search || undefined,
    countryId: countryId ?? undefined,
    limit: 50,
  });

  const selected = useMemo(
    () => players.find((p) => p.id === value),
    [players, value]
  );

  return (
    <div className="space-y-3">
      <div className="grid gap-2 sm:grid-cols-2">
        <input
          type="search"
          placeholder="Search players…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          disabled={disabled}
          className="h-10 w-full rounded-md border border-input bg-background px-3 text-sm"
        />
        <select
          value={countryId ?? ""}
          onChange={(e) => setCountryId(e.target.value || null)}
          disabled={disabled}
          className="h-10 w-full rounded-md border border-input bg-background px-3 text-sm"
        >
          <option value="">All countries</option>
          {countries.map((c) => (
            <option key={c.id} value={c.id}>
              {c.name}
            </option>
          ))}
        </select>
      </div>

      {selected && (
        <div className="flex items-center gap-3 rounded-md border border-border bg-muted/20 px-3 py-2">
          {selected.photoUrl && (
            <Image
              src={selected.photoUrl}
              alt=""
              width={40}
              height={40}
              className="rounded-full object-cover"
              unoptimized
            />
          )}
          <div className="min-w-0 text-sm">
            <p className="font-medium">{selected.displayName}</p>
            <p className="text-muted-foreground">
              {[selected.countryName, selected.clubName].filter(Boolean).join(" · ")}
            </p>
            {selected.stats && (
              <p className="text-xs text-muted-foreground">
                {selected.stats.goals}G · {selected.stats.assists}A · {selected.stats.matchesPlayed}{" "}
                apps
              </p>
            )}
          </div>
        </div>
      )}

      <ul className="max-h-72 space-y-1 overflow-y-auto rounded-md border border-border p-1">
        {isLoading && <li className="px-3 py-2 text-sm text-muted-foreground">Loading…</li>}
        {!isLoading && players.length === 0 && (
          <li className="px-3 py-2 text-sm text-muted-foreground">No players found.</li>
        )}
        {players.map((player) => (
          <li key={player.id}>
            <button
              type="button"
              disabled={disabled}
              onClick={() => onChange(player)}
              className={cn(
                "flex w-full items-center gap-3 rounded-md px-3 py-2 text-left hover:bg-muted/50",
                value === player.id && "bg-muted"
              )}
            >
              {player.photoUrl ? (
                <Image
                  src={player.photoUrl}
                  alt=""
                  width={32}
                  height={32}
                  className="rounded-full object-cover"
                  unoptimized
                />
              ) : (
                <div className="flex h-8 w-8 items-center justify-center rounded-full bg-muted text-xs">
                  ?
                </div>
              )}
              <div className="min-w-0 flex-1">
                <p className="truncate text-sm font-medium">{player.displayName}</p>
                <p className="truncate text-xs text-muted-foreground">
                  {[player.countryName, player.position, player.clubName]
                    .filter(Boolean)
                    .join(" · ")}
                </p>
              </div>
              {player.stats && (
                <span className="shrink-0 text-xs text-muted-foreground">
                  {player.stats.goals}G
                </span>
              )}
            </button>
          </li>
        ))}
      </ul>
    </div>
  );
}

export function PlayerPredictionForm({
  predictionType,
  label,
  description,
  existingId,
  existingPlayerId,
  isLocked,
  canEdit,
  onSaved,
}: {
  predictionType: string;
  label: string;
  description: string;
  existingId?: string;
  existingPlayerId?: string | null;
  isLocked: boolean;
  canEdit: boolean;
  onSaved: () => void;
}) {
  const [playerId, setPlayerId] = useState<string | null>(existingPlayerId ?? null);
  const [selectedName, setSelectedName] = useState<string>("");
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const create = useCreateUserPrediction();
  const update = useUpdateUserPrediction();

  const handleSelect = (player: FootballPlayer) => {
    setPlayerId(player.id);
    setSelectedName(player.displayName);
  };

  const handleSubmit = async () => {
    setError(null);
    if (!playerId) {
      setError("Select a player first.");
      return;
    }

    try {
      if (existingId) {
        await update.mutateAsync({ id: existingId, playerId });
      } else {
        await create.mutateAsync({ predictionType, playerId });
      }
      setConfirmOpen(false);
      onSaved();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to save prediction.");
      setConfirmOpen(false);
    }
  };

  const disabled = isLocked || !canEdit;
  const pending = create.isPending || update.isPending;

  return (
    <div className="space-y-4">
      <div>
        <h2 className="text-lg font-semibold">{label}</h2>
        <p className="text-sm text-muted-foreground">{description}</p>
      </div>

      <PlayerSelector value={playerId} onChange={handleSelect} disabled={disabled} />

      {error && <p className="text-sm text-destructive">{error}</p>}

      <Button
        disabled={disabled || !playerId || pending}
        onClick={() => setConfirmOpen(true)}
      >
        {existingId ? "Update prediction" : "Submit prediction"}
      </Button>

      <PredictionConfirmDialog
        open={confirmOpen}
        title={existingId ? "Update prediction?" : "Submit prediction?"}
        description={`Confirm ${selectedName || "this player"} as your ${label.toLowerCase()} pick.`}
        onConfirm={handleSubmit}
        onCancel={() => setConfirmOpen(false)}
        loading={pending}
      />
    </div>
  );
}
