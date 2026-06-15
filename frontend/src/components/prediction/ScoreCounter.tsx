"use client";

import { ChevronDown, ChevronUp } from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

const MIN = 0;
const MAX = 9;

interface ScoreCounterProps {
  label: string;
  value: number;
  onChange: (value: number) => void;
  className?: string;
}

export function ScoreCounter({ label, value, onChange, className }: ScoreCounterProps) {
  const decrement = () => onChange(Math.max(MIN, value - 1));
  const increment = () => onChange(Math.min(MAX, value + 1));

  return (
    <div className={cn("flex items-center gap-2", className)}>
      <span className="w-16 shrink-0 truncate text-right text-[11px] font-medium text-foreground">
        {label}
      </span>
      <div className="flex flex-1 items-center justify-center gap-1">
        <Button
          type="button"
          variant="outline"
          size="icon-sm"
          className="size-8 shrink-0 rounded-lg"
          onClick={decrement}
          disabled={value <= MIN}
          aria-label={`Decrease ${label} goals`}
        >
          <ChevronDown className="size-4" aria-hidden />
        </Button>
        <span
          className="flex size-10 items-center justify-center rounded-lg border border-border bg-muted/50 text-lg font-bold tabular-nums"
          aria-live="polite"
          aria-label={`${label} goals: ${value}`}
        >
          {value}
        </span>
        <Button
          type="button"
          variant="outline"
          size="icon-sm"
          className="size-8 shrink-0 rounded-lg"
          onClick={increment}
          disabled={value >= MAX}
          aria-label={`Increase ${label} goals`}
        >
          <ChevronUp className="size-4" aria-hidden />
        </Button>
      </div>
    </div>
  );
}
