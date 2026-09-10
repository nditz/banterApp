"use client";

import { useMemo, useState } from "react";
import Link from "next/link";
import { Check, Copy, Download, Loader2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { useGenerateStudioPack } from "@/hooks/useStudioPack";
import { useStudioStories } from "@/hooks/useStudioStories";
import { getApiErrorMessage } from "@/lib/api";
import { formatReceiptStoryType } from "@/lib/receipt-story";
import {
  downloadTextFile,
  formatStudioPackText,
  STUDIO_CONTENT_TYPES,
  STUDIO_TONES,
} from "@/lib/studio-pack";
import type {
  StudioContentPack,
  StudioContentType,
  StudioStoryCard,
  StudioTone,
} from "@/lib/types";
import { cn } from "@/lib/utils";

export function StudioStoryWorkspace({ receiptId }: { receiptId?: string | null }) {
  const { data, isPending, isError, error } = useStudioStories();
  const generate = useGenerateStudioPack();
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [contentType, setContentType] = useState<StudioContentType>("short");
  const [tone, setTone] = useState<StudioTone>("explainer");
  const [copied, setCopied] = useState<"script" | "pack" | "prompt" | null>(null);

  const allStories = useMemo(() => {
    if (!data) return [];
    return [
      ...data.latestReceipts,
      ...data.youVsPundits,
      ...data.trending,
      ...data.previousProjects,
    ];
  }, [data]);

  const autoSelectedId = useMemo(() => {
    if (!receiptId || !data) return null;
    return (
      data.latestReceipts.find((s) => s.receiptId === receiptId)?.id ??
      data.youVsPundits.find((s) => s.receiptId === receiptId)?.id ??
      null
    );
  }, [receiptId, data]);

  const effectiveSelectedId = selectedId ?? autoSelectedId;
  const selected = allStories.find((s) => s.id === effectiveSelectedId) ?? null;
  const pack = generate.data?.pack ?? null;

  const selectStory = (id: string) => {
    setSelectedId(id);
    generate.reset();
  };

  const handleGenerate = () => {
    if (!selected) return;
    generate.mutate({
      contentType,
      tone,
      receiptId: selected.receiptId,
      feedItemId: selected.feedItemId,
      projectId: selected.kind === "project" ? selected.projectId : null,
    });
  };

  const copy = async (kind: "script" | "pack" | "prompt", text: string) => {
    await navigator.clipboard.writeText(text);
    setCopied(kind);
    window.setTimeout(() => setCopied(null), 2000);
  };

  if (isPending) {
    return (
      <div className="space-y-3">
        <Skeleton className="h-24 w-full rounded-xl" />
        <Skeleton className="h-24 w-full rounded-xl" />
        <Skeleton className="h-40 w-full rounded-xl" />
      </div>
    );
  }

  if (isError) {
    return (
      <p role="alert" className="rounded-xl border border-border bg-card px-4 py-6 text-sm text-muted-foreground">
        {getApiErrorMessage(error)} Studio stories could not be loaded.
      </p>
    );
  }

  return (
    <div className="space-y-5">
      <StorySection
        title="Your latest receipts"
        empty="No settled receipts yet. Lock a pick, wait for full time, then come back."
        emptyHref="/matchweek"
        emptyLabel="Go to Matchweek"
        stories={data?.latestReceipts ?? []}
        selectedId={effectiveSelectedId}
        onSelect={selectStory}
      />
      <StorySection
        title="You vs pundits"
        empty="No sourced pundit clash on a receipt yet. Follow desks and wait for a settled pick."
        emptyHref="/pundits"
        emptyLabel="Follow pundits"
        stories={data?.youVsPundits ?? []}
        selectedId={effectiveSelectedId}
        onSelect={selectStory}
      />
      <StorySection
        title="Trending stories"
        empty="No sourced feed stories to package yet. Studio will not invent a trending card."
        stories={data?.trending ?? []}
        selectedId={effectiveSelectedId}
        onSelect={selectStory}
      />
      <StorySection
        title="Previous projects"
        empty="Generate a pack and it lands here."
        stories={data?.previousProjects ?? []}
        selectedId={effectiveSelectedId}
        onSelect={selectStory}
      />

      <div className="rounded-xl border border-border bg-card p-4">
        <p className="text-sm font-semibold">Content pack</p>
        <p className="mt-0.5 text-[11px] text-muted-foreground">
          {selected
            ? `Selected: ${selected.title}. Facts stay locked to the receipt or sourced headline.`
            : "Pick a story first. Studio will not start from a blank prompt."}
        </p>

        <p className="mt-3 text-[11px] font-semibold">Format</p>
        <div className="mt-1.5 flex flex-wrap gap-1.5">
          {STUDIO_CONTENT_TYPES.map((item) => (
            <Chip
              key={item.id}
              selected={contentType === item.id}
              onClick={() => setContentType(item.id)}
            >
              {item.label}
            </Chip>
          ))}
        </div>

        <p className="mt-3 text-[11px] font-semibold">Tone</p>
        <div className="mt-1.5 flex flex-wrap gap-1.5">
          {STUDIO_TONES.map((item) => (
            <Chip
              key={item.id}
              selected={tone === item.id}
              onClick={() => setTone(item.id)}
            >
              {item.label}
            </Chip>
          ))}
        </div>

        <Button
          type="button"
          className="mt-4"
          disabled={!selected || generate.isPending}
          onClick={handleGenerate}
        >
          {generate.isPending ? (
            <>
              <Loader2 className="size-3.5 animate-spin" aria-hidden />
              Building pack
            </>
          ) : (
            "Generate content pack"
          )}
        </Button>

        {generate.isError ? (
          <p role="alert" className="mt-3 text-sm text-muted-foreground">
            {getApiErrorMessage(generate.error)} Pack could not be generated.
          </p>
        ) : null}

        {pack ? <PackOutput pack={pack} copied={copied} onCopy={copy} /> : null}
      </div>
    </div>
  );
}

function StorySection({
  title,
  empty,
  emptyHref,
  emptyLabel,
  stories,
  selectedId,
  onSelect,
}: {
  title: string;
  empty: string;
  emptyHref?: string;
  emptyLabel?: string;
  stories: StudioStoryCard[];
  selectedId: string | null;
  onSelect: (id: string) => void;
}) {
  return (
    <section>
      <h2 className="text-sm font-semibold">{title}</h2>
      {stories.length === 0 ? (
        <p className="mt-1.5 text-[11px] text-muted-foreground">
          {empty}
          {emptyHref && emptyLabel ? (
            <>
              {" "}
              <Link href={emptyHref} className="font-semibold text-foreground hover:underline">
                {emptyLabel}
              </Link>
            </>
          ) : null}
        </p>
      ) : (
        <ul className="mt-2 grid gap-2 sm:grid-cols-2">
          {stories.map((story) => (
            <li key={story.id}>
              <button
                type="button"
                aria-pressed={selectedId === story.id}
                onClick={() => onSelect(story.id)}
                className={cn(
                  "w-full rounded-xl border px-3 py-2.5 text-left transition-colors",
                  selectedId === story.id
                    ? "border-pitch/50 bg-pitch/10"
                    : "border-border bg-card hover:bg-muted/40"
                )}
              >
                <p className="text-sm font-semibold">{story.title}</p>
                <p className="mt-0.5 line-clamp-2 text-[11px] text-muted-foreground">{story.summary}</p>
                <p className="mt-1 text-[10px] font-medium text-muted-foreground">
                  {story.scoreline ? `${story.scoreline} · ` : ""}
                  {story.storyType ? formatReceiptStoryType(story.storyType) : story.kind.replace("_", " ")}
                </p>
              </button>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}

function Chip({
  selected,
  onClick,
  children,
}: {
  selected: boolean;
  onClick: () => void;
  children: React.ReactNode;
}) {
  return (
    <button
      type="button"
      aria-pressed={selected}
      onClick={onClick}
      className={cn(
        "rounded-full px-2.5 py-1 text-[11px] font-medium",
        selected
          ? "bg-primary text-primary-foreground"
          : "bg-muted text-muted-foreground hover:text-foreground"
      )}
    >
      {children}
    </button>
  );
}

function PackOutput({
  pack,
  copied,
  onCopy,
}: {
  pack: StudioContentPack;
  copied: "script" | "pack" | "prompt" | null;
  onCopy: (kind: "script" | "pack" | "prompt", text: string) => void;
}) {
  const packText = formatStudioPackText(pack);
  const slug = pack.title.replace(/\s+/g, "-").toLowerCase().slice(0, 40);

  return (
    <div className="mt-4 space-y-3 border-t border-border pt-4">
      <div>
        <p className="text-sm font-semibold">{pack.title}</p>
        <p className="text-[11px] text-muted-foreground">
          {pack.contentType} · {pack.tone} · facts stay sourced
        </p>
      </div>

      <div>
        <p className="text-[11px] font-semibold">Hook</p>
        <p className="mt-1 text-sm">{pack.hook}</p>
      </div>

      <div>
        <p className="text-[11px] font-semibold">Script</p>
        <pre className="mt-1 max-h-56 overflow-auto whitespace-pre-wrap rounded-lg bg-muted/40 p-3 text-[11px] leading-5">
          {pack.script}
        </pre>
      </div>

      <div>
        <p className="text-[11px] font-semibold">Sourced facts</p>
        <ul className="mt-1 space-y-1 text-[11px] text-muted-foreground">
          {pack.facts.map((fact) => (
            <li key={`${fact.label}-${fact.value}`}>
              <span className="font-medium text-foreground">{fact.label}:</span> {fact.value}{" "}
              <span className="text-muted-foreground/80">({fact.provenance})</span>
            </li>
          ))}
        </ul>
      </div>

      <div>
        <p className="text-[11px] font-semibold">Visual plan</p>
        <ul className="mt-1 list-disc space-y-0.5 pl-4 text-[11px] text-muted-foreground">
          {pack.visualPlan.map((item) => (
            <li key={item}>{item}</li>
          ))}
        </ul>
      </div>

      {pack.memeDirection ? (
        <div>
          <p className="text-[11px] font-semibold">Meme / GIF direction</p>
          <p className="mt-1 text-[11px] text-muted-foreground">{pack.memeDirection}</p>
        </div>
      ) : null}

      <div>
        <p className="text-[11px] font-semibold">Caption</p>
        <p className="mt-1 text-sm">{pack.caption}</p>
        <p className="mt-1 text-[11px] text-muted-foreground">{pack.hashtags.join(" ")}</p>
      </div>

      <div>
        <p className="text-[11px] font-semibold">Source notes</p>
        <ul className="mt-1 list-disc space-y-0.5 pl-4 text-[11px] text-muted-foreground">
          {pack.sourceNotes.map((note) => (
            <li key={note}>{note}</li>
          ))}
        </ul>
      </div>

      <div className="flex flex-wrap gap-2">
        <Button type="button" size="sm" variant="outline" onClick={() => onCopy("script", pack.script)}>
          {copied === "script" ? <Check className="size-3.5" /> : <Copy className="size-3.5" />}
          Copy script
        </Button>
        <Button type="button" size="sm" variant="outline" onClick={() => onCopy("prompt", pack.imagePrompt)}>
          {copied === "prompt" ? <Check className="size-3.5" /> : <Copy className="size-3.5" />}
          Copy prompt
        </Button>
        <Button type="button" size="sm" variant="outline" onClick={() => onCopy("pack", packText)}>
          {copied === "pack" ? <Check className="size-3.5" /> : <Copy className="size-3.5" />}
          Copy content pack
        </Button>
        <Button
          type="button"
          size="sm"
          variant="outline"
          onClick={() => downloadTextFile(`balltakes-pack-${slug}.txt`, packText)}
        >
          <Download className="size-3.5" />
          Download text
        </Button>
        <Button
          type="button"
          size="sm"
          variant="outline"
          onClick={() =>
            downloadTextFile(
              `balltakes-pack-${slug}.json`,
              JSON.stringify(pack, null, 2),
              "application/json"
            )
          }
        >
          <Download className="size-3.5" />
          Download JSON
        </Button>
      </div>
    </div>
  );
}
