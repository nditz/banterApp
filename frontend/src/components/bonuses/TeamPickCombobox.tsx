"use client";

import { useMemo, useRef, useState } from "react";
import { Check, ChevronsUpDown } from "lucide-react";
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
} from "@/components/ui/command";
import { ComboboxDropdown } from "@/components/bonuses/ComboboxDropdown";
import { TeamFlag } from "@/components/brackets/TeamFlag";
import type { TournamentBonusTeam } from "@/hooks/useTournamentBonuses";
import { cn } from "@/lib/utils";

export function TeamPickCombobox({
  value,
  onChange,
  teams,
  disabled,
  ariaLabel,
}: {
  value: string;
  onChange: (code: string) => void;
  teams: TournamentBonusTeam[];
  disabled?: boolean;
  ariaLabel?: string;
}) {
  const [open, setOpen] = useState(false);
  const anchorRef = useRef<HTMLButtonElement>(null);

  const selected = useMemo(
    () => teams.find((team) => team.code === value),
    [teams, value]
  );

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
          {selected ? (
            <TeamFlag code={selected.code} name={selected.name} size={18} />
          ) : null}
          <span className={cn("truncate", !selected && "text-muted-foreground")}>
            {selected ? selected.name : "Search a club…"}
          </span>
        </span>
        <ChevronsUpDown className="size-4 shrink-0 text-muted-foreground" aria-hidden />
      </button>

      <ComboboxDropdown anchorRef={anchorRef} open={open} onClose={() => setOpen(false)}>
          <Command loop>
            <CommandInput autoFocus placeholder="Type a team name…" />
            <CommandList>
              <CommandEmpty>No teams found.</CommandEmpty>
              <CommandGroup>
                {teams.map((team) => (
                  <CommandItem
                    key={`${team.code}-${team.name}`}
                    value={`${team.name} ${team.code}`}
                    onSelect={() => {
                      onChange(team.code);
                      setOpen(false);
                    }}
                  >
                    <span className="flex min-w-0 flex-1 items-center gap-2">
                      <TeamFlag code={team.code} name={team.name} size={18} />
                      <span className="truncate">{team.name}</span>
                    </span>
                    {team.code === value && (
                      <Check className="size-4 shrink-0 text-pitch" aria-hidden />
                    )}
                  </CommandItem>
                ))}
              </CommandGroup>
            </CommandList>
          </Command>
      </ComboboxDropdown>
    </div>
  );
}
