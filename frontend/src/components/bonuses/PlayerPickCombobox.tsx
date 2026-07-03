"use client";

import { useEffect, useId, useMemo, useRef, useState } from "react";
import { ChevronsUpDown, Loader2, UserRound } from "lucide-react";
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
} from "@/components/ui/command";
import { ComboboxDropdown } from "@/components/bonuses/ComboboxDropdown";
import {
  usePlayerSearch,
  type TournamentBonusPlayerOption,
  type TournamentBonusTeam,
} from "@/hooks/useTournamentBonuses";
import { cn } from "@/lib/utils";

function useDebouncedValue<T>(value: T, delay: number): T {
  const [debounced, setDebounced] = useState(value);
  useEffect(() => {
    const id = window.setTimeout(() => setDebounced(value), delay);
    return () => window.clearTimeout(id);
  }, [value, delay]);
  return debounced;
}

export function PlayerPickCombobox({
  value,
  onChange,
  teams,
  disabled,
  ariaLabel,
}: {
  value: string;
  onChange: (name: string) => void;
  teams: TournamentBonusTeam[];
  disabled?: boolean;
  ariaLabel?: string;
}) {
  const [open, setOpen] = useState(false);
  const [search, setSearch] = useState("");
  const [country, setCountry] = useState<string | null>(null);
  const anchorRef = useRef<HTMLButtonElement>(null);
  const countryFilterId = useId();

  const debouncedSearch = useDebouncedValue(search, 200);
  const { data, isFetching } = usePlayerSearch(debouncedSearch, country, open);

  const players = useMemo(() => data?.players ?? [], [data]);

  const groups = useMemo(() => {
    const map = new Map<string, TournamentBonusPlayerOption[]>();
    for (const player of players) {
      const list = map.get(player.teamName);
      if (list) {
        list.push(player);
      } else {
        map.set(player.teamName, [player]);
      }
    }
    return Array.from(map.entries());
  }, [players]);

  const trimmedSearch = search.trim();
  const hasExactMatch = players.some(
    (p) => p.name.toLowerCase() === trimmedSearch.toLowerCase()
  );

  const commit = (name: string) => {
    onChange(name);
    setOpen(false);
    setSearch("");
  };

  return (
    <div className="relative">
      <button
        ref={anchorRef}
        type="button"
        disabled={disabled}
        aria-label={ariaLabel}
        onClick={() => setOpen((prev) => !prev)}
        className={cn(
          "flex w-full items-center justify-between gap-2 rounded-md border border-border bg-background px-3 py-2 text-left text-sm transition-colors",
          "focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none",
          "disabled:pointer-events-none disabled:opacity-50"
        )}
      >
        <span className="flex min-w-0 items-center gap-2">
          <UserRound className="size-4 shrink-0 text-muted-foreground" aria-hidden />
          <span className={cn("truncate", !value && "text-muted-foreground")}>
            {value || "Search a player…"}
          </span>
        </span>
        <ChevronsUpDown className="size-4 shrink-0 text-muted-foreground" aria-hidden />
      </button>

      <ComboboxDropdown anchorRef={anchorRef} open={open} onClose={() => setOpen(false)}>
          <div className="flex items-center gap-2 border-b border-border p-2">
            <label className="sr-only" htmlFor={countryFilterId}>
              Filter by country
            </label>
            <select
              id={countryFilterId}
              value={country ?? ""}
              onChange={(e) => setCountry(e.target.value || null)}
              className="h-8 w-full rounded-md border border-border bg-background px-2 text-xs"
            >
              <option value="">All countries</option>
              {teams.map((team) => (
                <option key={`${team.code}-${team.name}`} value={team.code}>
                  {team.name}
                </option>
              ))}
            </select>
          </div>

          <Command shouldFilter={false} loop>
            <CommandInput
              autoFocus
              placeholder="Type a player name…"
              value={search}
              onValueChange={setSearch}
            />
            <CommandList>
              {isFetching && players.length === 0 && (
                <div className="flex items-center justify-center gap-2 py-5 text-sm text-muted-foreground">
                  <Loader2 className="size-4 animate-spin" aria-hidden />
                  Searching…
                </div>
              )}

              {!isFetching && (
                <CommandEmpty>No players found.</CommandEmpty>
              )}

              {trimmedSearch && !hasExactMatch && (
                <CommandGroup heading="Custom">
                  <CommandItem
                    value={`__custom__${trimmedSearch}`}
                    onSelect={() => commit(trimmedSearch)}
                  >
                    Use “{trimmedSearch}”
                  </CommandItem>
                </CommandGroup>
              )}

              {groups.map(([teamName, teamPlayers]) => (
                <CommandGroup key={teamName} heading={teamName}>
                  {teamPlayers.map((player) => (
                    <CommandItem
                      key={`${player.teamCode}-${player.name}`}
                      value={`${player.teamCode}-${player.name}`}
                      onSelect={() => commit(player.name)}
                    >
                      <span className="truncate">{player.name}</span>
                    </CommandItem>
                  ))}
                </CommandGroup>
              ))}
            </CommandList>
          </Command>
      </ComboboxDropdown>
    </div>
  );
}
