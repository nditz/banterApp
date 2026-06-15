"use client";

import { useState } from "react";
import {
  FileText,
  Mic2,
  Sparkles,
  Users,
  Video,
} from "lucide-react";
import { MatchComparisonCard } from "@/components/studio/MatchComparisonCard";
import { StudioSummaryBar } from "@/components/studio/StudioSummaryBar";
import { CumulativeScriptExport } from "@/components/content/CumulativeScriptExport";
import { Skeleton } from "@/components/ui/skeleton";
import { useStudio } from "@/hooks/useStudio";
import { cn } from "@/lib/utils";
import type { StudioPickRole } from "@/lib/types";

type Tab = "my_picks" | "vs_league" | "vs_pundits" | "script";

const tabs: { id: Tab; label: string; icon: React.ReactNode; description: string }[] = [
  {
    id: "my_picks",
    label: "My Picks",
    icon: <FileText className="size-3.5" />,
    description: "A full breakdown of every prediction you've made",
  },
  {
    id: "vs_league",
    label: "vs League",
    icon: <Users className="size-3.5" />,
    description: "How your calls stack up against your league mates",
  },
  {
    id: "vs_pundits",
    label: "vs Pundits",
    icon: <Mic2 className="size-3.5" />,
    description: "Your picks side-by-side with professional journalists & analysts",
  },
  {
    id: "script",
    label: "Script",
    icon: <Sparkles className="size-3.5" />,
    description: "Generate a TV-journalist style broadcast script from your predictions",
  },
];

const roleFilter: Record<Tab, StudioPickRole[] | undefined> = {
  my_picks: ["me"],
  vs_league: ["me", "league"],
  vs_pundits: ["me", "pundit"],
  script: undefined,
};

export function StudioPage() {
  const [tab, setTab] = useState<Tab>("my_picks");
  const { data, isLoading } = useStudio();

  const activeTab = tabs.find((t) => t.id === tab)!;
  const filter = roleFilter[tab];

  return (
    <div className="mx-auto max-w-[820px] space-y-5">
      {/* Page header */}
      <div className="rounded-xl border border-gold/30 bg-gradient-to-br from-brand/80 to-brand/60 px-5 py-4 text-brand-foreground shadow-md">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <h1 className="flex items-center gap-2 text-lg font-bold">
              <Sparkles className="size-5 text-gold" aria-hidden />
              Content Studio
            </h1>
            <p className="mt-0.5 text-sm text-white/70">
              Review your predictions · compare with your league & the pros · generate your broadcast script
            </p>
          </div>
        </div>
      </div>

      {/* Summary stats */}
      {isLoading ? (
        <div className="flex gap-3">
          {[0, 1, 2].map((i) => (
            <Skeleton key={i} className="h-16 flex-1 rounded-xl" />
          ))}
        </div>
      ) : (
        data && <StudioSummaryBar data={data} />
      )}

      {/* Tabs */}
      <div
        className="flex gap-1 rounded-xl border border-border bg-muted/40 p-1 backdrop-blur-sm"
        role="tablist"
        aria-label="Studio sections"
      >
        {tabs.map((t) => (
          <button
            key={t.id}
            type="button"
            role="tab"
            aria-selected={tab === t.id}
            onClick={() => setTab(t.id)}
            className={cn(
              "flex flex-1 items-center justify-center gap-1.5 rounded-lg px-2 py-2 text-xs font-medium transition-all duration-200",
              tab === t.id
                ? "bg-card text-foreground shadow-sm ring-1 ring-border"
                : "text-muted-foreground hover:bg-card/60 hover:text-foreground"
            )}
          >
            {t.icon}
            <span className="hidden sm:inline">{t.label}</span>
          </button>
        ))}
      </div>

      {/* Tab description */}
      <p className="text-sm text-muted-foreground">{activeTab.description}</p>

      {/* Tab content */}
      {tab === "script" ? (
        <ScriptTab />
      ) : isLoading ? (
        <div className="space-y-4">
          {[0, 1, 2].map((i) => (
            <Skeleton key={i} className="h-40 w-full rounded-xl" />
          ))}
        </div>
      ) : data?.matches.length ? (
        <div className="space-y-4">
          {data.matches.map((match) => (
            <MatchComparisonCard
              key={match.matchId}
              match={match}
              filter={filter}
            />
          ))}
        </div>
      ) : (
        <EmptyState tab={tab} />
      )}

      {/* Video placeholder — coming soon */}
      {tab !== "script" && (
        <ComingSoonVideoSection />
      )}
    </div>
  );
}

// ─── Script tab ───────────────────────────────────────────────────────────────

function ScriptTab() {
  return (
    <div className="space-y-4">
      <div className="rounded-xl border border-gold/30 bg-gold/5 p-4">
        <div className="mb-3 flex items-center gap-2">
          <span className="flex size-8 items-center justify-center rounded-full bg-gold/20">
            <Mic2 className="size-4 text-gold" aria-hidden />
          </span>
          <div>
            <p className="text-sm font-semibold">TV Broadcast Script</p>
            <p className="text-[11px] text-muted-foreground">
              Converts your predictions into a camera-ready pundit script — ready to clip, post, and share.
            </p>
          </div>
        </div>
        <CumulativeScriptExport minimal={false} />
      </div>

      <div className="rounded-xl border border-border bg-card p-4">
        <p className="text-xs font-semibold">How to use your script</p>
        <ol className="mt-2 space-y-1.5 text-[11px] text-muted-foreground">
          <li className="flex gap-2">
            <span className="flex size-4 shrink-0 items-center justify-center rounded-full bg-muted text-[10px] font-bold">1</span>
            Make your predictions on the home page before kickoff.
          </li>
          <li className="flex gap-2">
            <span className="flex size-4 shrink-0 items-center justify-center rounded-full bg-muted text-[10px] font-bold">2</span>
            Come back here and generate your <strong>pre-match</strong> script — record yourself reading it.
          </li>
          <li className="flex gap-2">
            <span className="flex size-4 shrink-0 items-center justify-center rounded-full bg-muted text-[10px] font-bold">3</span>
            After the final whistle, generate your <strong>post-match</strong> script to reveal which calls landed.
          </li>
          <li className="flex gap-2">
            <span className="flex size-4 shrink-0 items-center justify-center rounded-full bg-muted text-[10px] font-bold">4</span>
            Copy, download, or paste into TikTok / YouTube Shorts / Instagram captions.
          </li>
        </ol>
      </div>
    </div>
  );
}

// ─── Coming Soon: Video section ───────────────────────────────────────────────

function ComingSoonVideoSection() {
  return (
    <div className="rounded-xl border border-dashed border-border bg-muted/20 px-5 py-6 text-center">
      <Video className="mx-auto mb-2 size-8 text-muted-foreground/50" aria-hidden />
      <p className="text-sm font-semibold text-muted-foreground">Video content — coming soon</p>
      <p className="mt-1 text-[11px] text-muted-foreground/70">
        Attach your prediction video, browse trending clips from online personalities, and use the
        same lingo as your favourite pundits.
      </p>
    </div>
  );
}

// ─── Empty state ──────────────────────────────────────────────────────────────

function EmptyState({ tab }: { tab: Tab }) {
  const messages: Record<Tab, { title: string; body: string }> = {
    my_picks: {
      title: "No predictions yet",
      body: "Head to the home page and pick some matches — your full record will appear here.",
    },
    vs_league: {
      title: "No league comparisons yet",
      body: "Create or join a league and make predictions — you'll see how you stack up against your mates here.",
    },
    vs_pundits: {
      title: "No picks to compare",
      body: "Make a few predictions and we'll show you how the pros called the same games.",
    },
    script: { title: "", body: "" },
  };

  const msg = messages[tab];
  return (
    <div className="rounded-xl border border-border bg-card px-6 py-10 text-center">
      <p className="text-sm font-semibold text-muted-foreground">{msg.title}</p>
      <p className="mt-1 text-[11px] text-muted-foreground/80">{msg.body}</p>
    </div>
  );
}
