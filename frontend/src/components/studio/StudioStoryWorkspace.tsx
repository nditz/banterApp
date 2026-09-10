"use client";

import { useMemo, useState } from "react";
import Link from "next/link";
import { Loader2 } from "lucide-react";
import { useTermsSaveGate } from "@/components/session/TermsSaveGate";
import { StudioPackPreview } from "@/components/studio/StudioPackPreview";
import { Button, buttonVariants } from "@/components/ui/button";
import { FreshnessBadge } from "@/components/ui/freshness-badge";
import { Panel } from "@/components/ui/panel";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState, ErrorState } from "@/components/ui/states";
import { useGenerateStudioPack } from "@/hooks/useStudioPack";
import { useStudioStories } from "@/hooks/useStudioStories";
import { getApiErrorMessage } from "@/lib/api";
import { formatReceiptStoryType } from "@/lib/receipt-story";
import { stripHtml } from "@/lib/strip-html";
import {
  STUDIO_CONTENT_TYPES,
  STUDIO_PERSPECTIVES,
  STUDIO_TONES,
  suggestedStudioPerspective,
} from "@/lib/studio-pack";
import type {
  StudioContentType,
  StudioPerspective,
  StudioStoryCard,
  StudioTone,
} from "@/lib/types";
import { cn } from "@/lib/utils";

const STEPS = [
  { id: 1, label: "Story" },
  { id: 2, label: "Shape" },
  { id: 3, label: "Pack" },
] as const;

type MobileStep = (typeof STEPS)[number]["id"];
type CopyKind = "script" | "pack" | "prompt" | "caption";

const KIND_LABEL: Record<StudioStoryCard["kind"], string> = {
  receipt: "Receipt",
  vs_pundit: "You vs pundit",
  trending: "Sourced story",
  project: "Saved pack",
};

export function StudioStoryWorkspace({ receiptId }: { receiptId?: string | null }) {
  const { data, isPending, isError, error, refetch, dataUpdatedAt, isFetching } =
    useStudioStories();
  const generate = useGenerateStudioPack();
  const { requireTerms } = useTermsSaveGate();
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [contentType, setContentType] = useState<StudioContentType>("short");
  const [tone, setTone] = useState<StudioTone>("explainer");
  const [perspectiveOverride, setPerspectiveOverride] = useState<StudioPerspective | null>(null);
  const [copied, setCopied] = useState<CopyKind | null>(null);
  const [mobileStep, setMobileStep] = useState<MobileStep | null>(null);

  const rails = useMemo(() => {
    if (!data) return [];
    return [
      { key: "receipts", title: "Your latest receipts", stories: data.latestReceipts },
      { key: "vs-pundits", title: "You vs pundits", stories: data.youVsPundits },
      { key: "trending", title: "Trending stories", stories: data.trending },
      { key: "projects", title: "Previous projects", stories: data.previousProjects },
    ].filter((rail) => rail.stories.length > 0);
  }, [data]);

  const allStories = useMemo(
    () => rails.flatMap((rail) => rail.stories),
    [rails]
  );

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
  const remainingGenerations = generate.data?.remainingGenerations;
  const perspective =
    perspectiveOverride ??
    (selected ? suggestedStudioPerspective(selected) : "my_take");
  const resolvedStep: MobileStep = mobileStep ?? (autoSelectedId ? 2 : 1);

  const selectStory = (id: string) => {
    setSelectedId(id);
    setPerspectiveOverride(null);
    generate.reset();
    setMobileStep(2);
  };

  const handleGenerate = async () => {
    if (!selected) return;
    const accepted = await requireTerms();
    if (!accepted) return;
    generate.mutate(
      {
        contentType,
        tone,
        perspective,
        receiptId: selected.receiptId,
        feedItemId: selected.feedItemId,
        projectId: selected.kind === "project" ? selected.projectId : null,
      },
      { onSuccess: () => setMobileStep(3) }
    );
  };

  const copy = async (kind: CopyKind, text: string) => {
    await navigator.clipboard.writeText(text);
    setCopied(kind);
    window.setTimeout(() => setCopied(null), 2000);
  };

  if (isPending) {
    return (
      <div className="@container">
        <div className="space-y-3 @min-[1100px]:grid @min-[1100px]:grid-cols-[18rem_minmax(0,1fr)_22rem] @min-[1100px]:gap-4 @min-[1100px]:space-y-0">
          <Skeleton className="h-64 w-full rounded-xl" />
          <Skeleton className="h-64 w-full rounded-xl" />
          <Skeleton className="hidden h-64 w-full rounded-xl @min-[1100px]:block" />
        </div>
      </div>
    );
  }

  const rail = (
    <Panel
      title="Story inbox"
      subtitle="Start from a receipt or sourced headline — never a blank prompt."
      accent="pitch"
      action={
        dataUpdatedAt ? (
          <FreshnessBadge
            status={isFetching ? "live" : "ok"}
            updatedAt={dataUpdatedAt}
          />
        ) : null
      }
    >
      {isError ? (
        <ErrorState
          dense
          title="Studio stories unavailable"
          description={`${getApiErrorMessage(error)} Stories could not be loaded. Studio will not invent a substitute inbox.`}
          onRetry={() => void refetch()}
        />
      ) : rails.length === 0 ? (
        <EmptyState
          dense
          title="No stories to package yet"
          description="Lock a pick, wait for full time, or follow a sourced desk. Studio will not invent a trending card."
          action={
            <div className="flex flex-wrap justify-center gap-2">
              <Link
                href="/matchweek"
                className={cn(buttonVariants({ variant: "outline", size: "sm" }))}
              >
                Go to Matchweek
              </Link>
              <Link
                href="/pundits"
                className={cn(buttonVariants({ variant: "outline", size: "sm" }))}
              >
                Follow pundits
              </Link>
            </div>
          }
        />
      ) : (
        <div className="space-y-4">
          {rails.map((section) => (
            <StorySection
              key={section.key}
              title={section.title}
              stories={section.stories}
              selectedId={effectiveSelectedId}
              onSelect={selectStory}
            />
          ))}
        </div>
      )}
    </Panel>
  );

  const workspace = (
    <Panel
      title="Shape the pack"
      subtitle={
        selected
          ? `Selected: ${stripHtml(selected.title)}. Facts stay locked to the receipt or sourced headline.`
          : "Pick a story first. Studio will not start from a blank prompt."
      }
      accent="gold"
    >
      <p className="text-[11px] font-semibold">Format</p>
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
          <Chip key={item.id} selected={tone === item.id} onClick={() => setTone(item.id)}>
            {item.label}
          </Chip>
        ))}
      </div>

      <p className="mt-3 text-[11px] font-semibold">Perspective</p>
      <p className="mt-0.5 text-[11px] text-muted-foreground">
        Frames the copy. It does not invent a pundit quote.
      </p>
      <div className="mt-1.5 flex flex-wrap gap-1.5">
        {STUDIO_PERSPECTIVES.map((item) => (
          <Chip
            key={item.id}
            selected={perspective === item.id}
            onClick={() => setPerspectiveOverride(item.id)}
            title={item.hint}
          >
            {item.label}
          </Chip>
        ))}
      </div>
      <p className="mt-1.5 text-[11px] text-muted-foreground">
        {STUDIO_PERSPECTIVES.find((item) => item.id === perspective)?.hint}
      </p>

      <Button
        type="button"
        className="mt-4"
        disabled={!selected || generate.isPending}
        onClick={() => void handleGenerate()}
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

      {remainingGenerations != null ? (
        <p className="mt-2 text-[11px] text-muted-foreground">
          {remainingGenerations} generation{remainingGenerations === 1 ? "" : "s"} remaining
        </p>
      ) : null}

      {generate.isError ? (
        <ErrorState
          dense
          className="mt-3"
          title="Pack could not be generated"
          description={getApiErrorMessage(generate.error)}
          onRetry={() => void handleGenerate()}
        />
      ) : null}
    </Panel>
  );

  const preview = (
    <Panel title="Preview" subtitle="Sourced facts stay separate from AI copy." accent="pitch">
      <StudioPackPreview
        pack={pack}
        perspective={perspective}
        remainingGenerations={remainingGenerations}
        copied={copied}
        onCopy={copy}
      />
    </Panel>
  );

  return (
    <div className="@container space-y-4">
      <nav
        className="flex items-center gap-1 rounded-xl border border-border bg-muted/40 p-1 @min-[1100px]:hidden"
        aria-label="Studio steps"
      >
        {STEPS.map((step) => (
          <button
            key={step.id}
            type="button"
            aria-current={resolvedStep === step.id ? "step" : undefined}
            onClick={() => setMobileStep(step.id)}
            className={cn(
              "flex flex-1 items-center justify-center gap-1.5 rounded-lg px-2 py-2 text-xs font-medium",
              resolvedStep === step.id
                ? "bg-card text-foreground shadow-sm ring-1 ring-border"
                : "text-muted-foreground hover:bg-card/60 hover:text-foreground"
            )}
          >
            <span className="flex size-5 items-center justify-center rounded-full bg-muted text-[10px] font-bold">
              {step.id}
            </span>
            {step.label}
          </button>
        ))}
      </nav>

      <div className="@min-[1100px]:grid @min-[1100px]:grid-cols-[minmax(16rem,18rem)_minmax(0,1fr)_minmax(18rem,22rem)] @min-[1100px]:items-start @min-[1100px]:gap-4">
        <div
          className={cn(
            "min-w-0 @min-[1100px]:sticky @min-[1100px]:top-20 @min-[1100px]:block @min-[1100px]:max-h-[calc(100vh-7rem)] @min-[1100px]:overflow-y-auto",
            resolvedStep !== 1 && "@max-[1099px]:hidden"
          )}
        >
          {rail}
        </div>
        <div
          className={cn(
            "min-w-0 @min-[1100px]:block",
            resolvedStep !== 2 && "@max-[1099px]:hidden"
          )}
        >
          {workspace}
          <div className="mt-3 flex gap-2 @min-[1100px]:hidden">
            <Button type="button" size="sm" variant="outline" onClick={() => setMobileStep(1)}>
              Back to stories
            </Button>
            <Button
              type="button"
              size="sm"
              variant="outline"
              onClick={() => setMobileStep(3)}
              disabled={!selected}
            >
              Preview pack
            </Button>
          </div>
        </div>
        <div
          className={cn(
            "min-w-0 @min-[1100px]:sticky @min-[1100px]:top-20 @min-[1100px]:block @min-[1100px]:max-h-[calc(100vh-7rem)] @min-[1100px]:overflow-y-auto",
            resolvedStep !== 3 && "@max-[1099px]:hidden"
          )}
        >
          {preview}
          <div className="mt-3 @min-[1100px]:hidden">
            <Button type="button" size="sm" variant="outline" onClick={() => setMobileStep(2)}>
              Back to shape
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}

function StorySection({
  title,
  stories,
  selectedId,
  onSelect,
}: {
  title: string;
  stories: StudioStoryCard[];
  selectedId: string | null;
  onSelect: (id: string) => void;
}) {
  return (
    <section>
      <h3 className="text-sm font-semibold">{title}</h3>
      <ul className="mt-2 grid gap-2">
        {stories.map((story) => (
          <li key={story.id}>
            <StoryCardButton
              story={story}
              selected={selectedId === story.id}
              onSelect={onSelect}
            />
          </li>
        ))}
      </ul>
    </section>
  );
}

function StoryCardButton({
  story,
  selected,
  onSelect,
}: {
  story: StudioStoryCard;
  selected: boolean;
  onSelect: (id: string) => void;
}) {
  const why = storyWhyItMatters(story);

  return (
    <button
      type="button"
      aria-pressed={selected}
      onClick={() => onSelect(story.id)}
      className={cn(
        "w-full rounded-lg border px-3 py-2.5 text-left transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring/50",
        selected ? "border-pitch/50 bg-pitch/10" : "border-border bg-card hover:bg-muted/40"
      )}
    >
      <p className="text-[10px] font-semibold uppercase tracking-wide text-muted-foreground">
        {KIND_LABEL[story.kind] ?? story.kind.replaceAll("_", " ")}
      </p>
      <p className="text-sm font-semibold">{stripHtml(story.title)}</p>
      <p className="mt-0.5 line-clamp-2 text-[11px] text-muted-foreground">
        {stripHtml(story.summary)}
      </p>
      {story.scoreline ? (
        <p className="mt-1 text-[11px] font-medium text-foreground">{story.scoreline}</p>
      ) : null}
      {why.length > 0 ? (
        <ul className="mt-1.5 flex flex-wrap gap-1">
          {why.map((item) => (
            <li
              key={item}
              className="rounded-full bg-muted px-1.5 py-0.5 text-[10px] font-medium text-muted-foreground"
            >
              {item}
            </li>
          ))}
        </ul>
      ) : null}
      {story.occurredAt ? (
        <FreshnessBadge status="ok" updatedAt={story.occurredAt} className="mt-1.5" />
      ) : null}
    </button>
  );
}

function storyWhyItMatters(story: StudioStoryCard): string[] {
  const items: string[] = [];
  if (story.storyType) items.push(formatReceiptStoryType(story.storyType));
  for (const tag of story.tags) {
    const trimmed = tag.trim();
    if (!trimmed) continue;
    if (items.some((item) => item.toLowerCase() === trimmed.toLowerCase())) continue;
    items.push(trimmed);
  }
  return items;
}

function Chip({
  selected,
  onClick,
  children,
  title,
}: {
  selected: boolean;
  onClick: () => void;
  children: React.ReactNode;
  title?: string;
}) {
  return (
    <button
      type="button"
      title={title}
      aria-pressed={selected}
      onClick={onClick}
      className={cn(
        "rounded-full px-2.5 py-1 text-[11px] font-medium focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring/50",
        selected
          ? "bg-primary text-primary-foreground"
          : "bg-muted text-muted-foreground hover:text-foreground"
      )}
    >
      {children}
    </button>
  );
}
