"use client";

import { useState } from "react";
import { Sparkles, UserRound } from "lucide-react";
import { TurnstileWidget } from "@/components/security/TurnstileWidget";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { useSetUsername, useSuggestUsername } from "@/hooks/useSession";
import { getApiErrorMessage } from "@/lib/api";
import { isValidUsername, sanitizeUsernameInput, USERNAME_MAX } from "@/lib/username";
import { cn } from "@/lib/utils";

interface UsernameSetupProps {
  initialUsername?: string | null;
  className?: string;
  compact?: boolean;
  onSaved?: (username: string) => void;
}

/**
 * Guest username picker tied to the recovery session. Prefills an AI-suggested
 * fantasy-style nickname; user can edit and save to appear on league standings.
 */
export function UsernameSetup({
  initialUsername,
  className,
  compact = false,
  onSaved,
}: UsernameSetupProps) {
  const suggest = useSuggestUsername(!initialUsername);
  const setUsername = useSetUsername();
  const [draft, setDraft] = useState<string | null>(null);
  const [turnstileToken, setTurnstileToken] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [justSaved, setJustSaved] = useState(false);

  const username = draft ?? initialUsername ?? suggest.data?.username ?? "";
  const saved = justSaved || (Boolean(initialUsername) && draft === null);

  const handleInputChange = (value: string) => {
    setJustSaved(false);
    setErrorMessage(null);
    setDraft(sanitizeUsernameInput(value));
  };

  const handleSuggest = () => {
    if (suggest.data?.username) {
      setDraft(suggest.data.username);
      setJustSaved(false);
      setErrorMessage(null);
      return;
    }
    suggest.refetch();
  };

  const handleSave = async () => {
    if (!isValidUsername(username)) {
      setErrorMessage("Use 3–20 letters (A–Z) and numbers (0–9) only.");
      return;
    }

    setErrorMessage(null);
    try {
      const result = await setUsername.mutateAsync({ username: username.trim(), turnstileToken });
      setDraft(result.username);
      setJustSaved(true);
      onSaved?.(result.username);
    } catch (err) {
      setErrorMessage(getApiErrorMessage(err));
    }
  };

  return (
    <div className={cn("space-y-2", className)}>
      <div className="flex items-start gap-2">
        <UserRound className="mt-0.5 size-4 shrink-0 text-muted-foreground" aria-hidden />
        <div className="min-w-0 flex-1">
          <p className={cn("font-semibold text-foreground", compact ? "text-xs" : "text-sm")}>
            Your league username
          </p>
          <p className="mt-0.5 text-[11px] text-muted-foreground">
            This name appears on league standings and leaderboards. It stays linked to your recovery
            key when you restore your session.
          </p>
        </div>
      </div>

      <div className="flex flex-wrap items-center gap-2">
        <Input
          value={username}
          onChange={(e) => handleInputChange(e.target.value)}
          placeholder={suggest.isLoading ? "Generating nickname…" : "Shadowfox42"}
          maxLength={USERNAME_MAX}
          spellCheck={false}
          autoComplete="off"
          aria-label="League username"
          className="h-9 max-w-xs font-mono text-sm"
        />
        <Button
          type="button"
          variant="outline"
          size="sm"
          className="h-9 shrink-0"
          onClick={handleSuggest}
          disabled={suggest.isFetching}
        >
          <Sparkles className="size-3.5" aria-hidden />
          {suggest.isFetching ? "Thinking…" : "Suggest"}
        </Button>
      </div>

      <p className="text-[10px] text-muted-foreground">
        Letters A–Z and numbers 0–9 only. Must be unique across all players.
      </p>

      {!saved && (
        <div className="space-y-2">
          <TurnstileWidget onToken={setTurnstileToken} />
          {errorMessage && (
            <p className="text-xs text-destructive" role="alert">
              {errorMessage}
            </p>
          )}
          <Button
            type="button"
            size="sm"
            className="btn-tournament"
            disabled={!isValidUsername(username) || !turnstileToken || setUsername.isPending}
            onClick={handleSave}
          >
            {setUsername.isPending ? "Saving…" : "Save username"}
          </Button>
        </div>
      )}

      {saved && (
        <p className="text-xs text-pitch" role="status">
          Playing as <span className="font-semibold">{username}</span> in your leagues.
        </p>
      )}
    </div>
  );
}
