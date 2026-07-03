"use client";

import { useMemo, useState } from "react";
import Image from "next/image";
import { Button } from "@/components/ui/button";
import { useFootballCountries } from "@/hooks/useFootballReference";
import type { FootballCountry } from "@/lib/football-reference/types";
import { cn } from "@/lib/utils";

export function CountrySelector({
  value,
  onChange,
  disabled,
}: {
  value: string | null;
  onChange: (country: FootballCountry) => void;
  disabled?: boolean;
}) {
  const [search, setSearch] = useState("");
  const { data: countries = [], isLoading } = useFootballCountries(search || undefined);

  const selected = useMemo(
    () => countries.find((c) => c.id === value),
    [countries, value]
  );

  return (
    <div className="space-y-3">
      <input
        type="search"
        placeholder="Search countries…"
        value={search}
        onChange={(e) => setSearch(e.target.value)}
        disabled={disabled}
        className="h-10 w-full rounded-md border border-input bg-background px-3 text-sm"
      />

      {selected && (
        <div className="flex items-center gap-2 rounded-md border border-border bg-muted/20 px-3 py-2 text-sm">
          {selected.flagUrl && (
            <Image
              src={selected.flagUrl}
              alt=""
              width={24}
              height={16}
              style={{ width: "auto", height: "auto" }}
              className="rounded-sm object-cover"
              unoptimized
            />
          )}
          <span className="font-medium">{selected.name}</span>
        </div>
      )}

      <ul className="max-h-64 space-y-1 overflow-y-auto rounded-md border border-border p-1">
        {isLoading && <li className="px-3 py-2 text-sm text-muted-foreground">Loading…</li>}
        {!isLoading && countries.length === 0 && (
          <li className="px-3 py-2 text-sm text-muted-foreground">No countries found.</li>
        )}
        {countries.map((country) => (
          <li key={country.id}>
            <button
              type="button"
              disabled={disabled}
              onClick={() => onChange(country)}
              className={cn(
                "flex w-full items-center gap-2 rounded-md px-3 py-2 text-left text-sm hover:bg-muted/50",
                value === country.id && "bg-muted"
              )}
            >
              {country.flagUrl && (
                <Image
                  src={country.flagUrl}
                  alt=""
                  width={24}
                  height={16}
                  style={{ width: "auto", height: "auto" }}
                  className="rounded-sm object-cover"
                  unoptimized
                />
              )}
              <span>{country.name}</span>
              {country.code && (
                <span className="ml-auto text-xs text-muted-foreground">{country.code}</span>
              )}
            </button>
          </li>
        ))}
      </ul>
    </div>
  );
}

export function PredictionConfirmDialog({
  open,
  title,
  description,
  onConfirm,
  onCancel,
  loading,
}: {
  open: boolean;
  title: string;
  description: string;
  onConfirm: () => void;
  onCancel: () => void;
  loading?: boolean;
}) {
  if (!open) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-end justify-center bg-black/50 p-4 sm:items-center">
      <div className="w-full max-w-md rounded-lg border border-border bg-card p-5 shadow-lg">
        <h3 className="text-base font-semibold">{title}</h3>
        <p className="mt-2 text-sm text-muted-foreground">{description}</p>
        <div className="mt-4 flex justify-end gap-2">
          <Button variant="outline" size="sm" onClick={onCancel} disabled={loading}>
            Cancel
          </Button>
          <Button size="sm" onClick={onConfirm} disabled={loading}>
            {loading ? "Saving…" : "Confirm"}
          </Button>
        </div>
      </div>
    </div>
  );
}
