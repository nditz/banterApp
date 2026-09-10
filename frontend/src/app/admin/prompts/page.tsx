"use client";

import { useMemo, useState } from "react";
import { useAdminToast } from "@/components/admin/AdminToast";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { useAdminPrompts, useAdminSavePrompt } from "@/hooks/admin/useAdmin";
import { getApiErrorMessage } from "@/lib/api";
import type { AdminPrompt } from "@/lib/admin/types";

export default function AdminPromptsPage() {
  const { data: prompts, isLoading, isError, refetch } = useAdminPrompts();
  const savePrompt = useAdminSavePrompt();
  const { showToast } = useAdminToast();
  const [selectedKey, setSelectedKey] = useState<string>("");
  const [drafts, setDrafts] = useState<Record<string, string>>({});

  const selected = useMemo(
    () => prompts?.find((p) => p.key === selectedKey) ?? prompts?.[0] ?? null,
    [prompts, selectedKey]
  );
  const draft = selected ? (drafts[selected.key] ?? selected.body) : "";
  const dirty = selected != null && draft !== selected.body;

  const save = async (body: string | null) => {
    if (!selected) {
      return;
    }
    const key = selected.key;
    try {
      await savePrompt.mutateAsync({ key, body });
      setDrafts((current) => {
        const next = { ...current };
        delete next[key];
        return next;
      });
      showToast(body == null || body.trim() === "" ? "Reverted to shipped default" : "Prompt saved");
      await refetch();
    } catch (e) {
      showToast(getApiErrorMessage(e), "error");
    }
  };

  if (isLoading) return <Skeleton className="h-64 w-full" />;

  if (isError) {
    return (
      <div className="space-y-3">
        <h2 className="text-xl font-semibold sm:text-2xl">Prompts</h2>
        <p className="text-sm text-zinc-500">Could not load prompt overrides.</p>
        <Button size="sm" variant="outline" onClick={() => refetch()}>
          Retry
        </Button>
      </div>
    );
  }

  if (!prompts?.length || !selected) {
    return (
      <div className="space-y-3">
        <h2 className="text-xl font-semibold sm:text-2xl">Prompts</h2>
        <p className="text-sm text-zinc-500">No prompt keys are registered.</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-semibold sm:text-2xl">Prompts</h2>
        <p className="text-sm text-zinc-500">
          Override AI system prompts in the database. Empty rows keep the shipped default.
          SQL edits apply within about a minute without a redeploy.
        </p>
      </div>

      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <label className="flex min-w-0 flex-1 flex-col gap-1 text-sm">
          <span className="text-zinc-500">Prompt</span>
          <select
            className="h-9 rounded-lg border border-zinc-700 bg-zinc-950 px-2 text-sm"
            value={selected.key}
            onChange={(e) => setSelectedKey(e.target.value)}
          >
            {prompts.map((prompt) => (
              <option key={prompt.key} value={prompt.key}>
                {prompt.label}
              </option>
            ))}
          </select>
        </label>
        <p className="text-sm text-zinc-500">
          {selected.isOverride ? "Database override" : "Shipped default"}
        </p>
      </div>

      <p className="text-sm text-zinc-400">{selected.description}</p>
      <p className="font-mono text-xs text-zinc-500">{selected.key}</p>
      {selected.applyCompetitionFocus ? (
        <p className="text-xs text-zinc-500">
          Competition focus is still prepended at runtime so World Cup framing cannot sneak back in.
        </p>
      ) : null}

      <textarea
        className="min-h-72 w-full rounded-lg border border-zinc-700 bg-zinc-950 p-3 font-mono text-sm"
        value={draft}
        onChange={(e) => {
          const value = e.target.value;
          setDrafts((current) => ({ ...current, [selected.key]: value }));
        }}
        spellCheck={false}
      />

      <div className="flex flex-wrap gap-2">
        <Button
          size="sm"
          disabled={!dirty || savePrompt.isPending}
          onClick={() => save(draft)}
        >
          Save override
        </Button>
        <Button
          size="sm"
          variant="outline"
          disabled={savePrompt.isPending || (!selected.isOverride && !dirty)}
          onClick={() => void save(null)}
        >
          Revert to default
        </Button>
      </div>

      <SqlHint prompt={selected} />
    </div>
  );
}

function SqlHint({ prompt }: { prompt: AdminPrompt }) {
  return (
    <div className="space-y-2 rounded-lg border border-zinc-800 p-4">
      <p className="text-sm font-medium">Change with SQL</p>
      <pre className="overflow-x-auto whitespace-pre-wrap break-anywhere text-xs text-zinc-500">
{`insert into prompt_overrides ("Key", "Body", "UpdatedAt")
values ('${prompt.key}', 'your prompt here', now())
on conflict ("Key") do update
set "Body" = excluded."Body", "UpdatedAt" = now();`}
      </pre>
    </div>
  );
}
