"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useSearchParams } from "next/navigation";
import {
  Clapperboard,
  FileText,
  Mic2,
  Sparkles,
  Users,
} from "lucide-react";
import { MatchComparisonCard } from "@/components/studio/MatchComparisonCard";
import { StudioStoryWorkspace } from "@/components/studio/StudioStoryWorkspace";
import { StudioSummaryBar } from "@/components/studio/StudioSummaryBar";
import { CumulativeScriptExport } from "@/components/content/CumulativeScriptExport";
import { PunditScriptGenerator } from "@/components/content/PunditScriptGenerator";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/ui/states";
import { useStudio } from "@/hooks/useStudio";
import { PRODUCT_METRICS, recordMetric } from "@/lib/metrics";
import { cn } from "@/lib/utils";
import type { StudioPickRole } from "@/lib/types";

type Tab = "stories" | "my_picks" | "vs_league" | "vs_pundits" | "script";

const tabs: { id: Tab; label: string; icon: React.ReactNode; description: string }[] = [
  {
    id: "stories",
    label: "Stories",
    icon: <Clapperboard className="size-3.5" />,
    description: "Pick a receipt, pundit clash, or sourced story — then export a content pack",
  },
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
    description: "Your picks side-by-side with sourced pundit predictions",
  },
  {
    id: "script",
    label: "Script",
    icon: <Sparkles className="size-3.5" />,
    description: "Generate pundit match analysis scripts and prediction recap exports",
  },
];

const roleFilter: Record<Tab, StudioPickRole[] | undefined> = {
  stories: undefined,
  my_picks: ["me"],
  vs_league: ["me", "league"],
  vs_pundits: ["me", "pundit"],
  script: undefined,
};

export function StudioPage() {
  const searchParams = useSearchParams();
  const receiptId = searchParams.get("receipt");
  const [tab, setTab] = useState<Tab>("stories");
  const { data, isLoading } = useStudio();

  const activeTab = tabs.find((t) => t.id === tab)!;
  const filter = roleFilter[tab];

  useEffect(() => {
    recordMetric(PRODUCT_METRICS.studioOpened);
  }, []);

  return (
    <div className="mx-auto max-w-[820px] space-y-5">
      <div className="rounded-xl border border-gold/30 bg-gradient-to-br from-brand/80 to-brand/60 px-5 py-4 text-brand-foreground shadow-md">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <h1 className="flex items-center gap-2 text-lg font-bold">
              <Sparkles className="size-5 text-gold" aria-hidden />
              Content Studio
            </h1>
            <p className="mt-0.5 text-sm text-brand-foreground/70">
              Pick a story · lock the facts · export a pack for your editor or AI tool
            </p>
          </div>
        </div>
      </div>

      {isLoading ? (
        <div className="flex gap-3">
          {[0, 1, 2].map((i) => (
            <Skeleton key={i} className="h-16 flex-1 rounded-xl" />
          ))}
        </div>
      ) : (
        data && <StudioSummaryBar data={data} />
      )}

      <div
        className="flex gap-1 rounded-xl border border-border bg-muted/40 p-1"
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
            <span className="sr-only sm:hidden">{t.label}</span>
          </button>
        ))}
      </div>

      <p className="text-sm text-muted-foreground">{activeTab.description}</p>
      {tab === "vs_pundits" && (
        <p className="text-xs text-muted-foreground">
          {data?.filteringToFollows
            ? `Showing ${data.followedPunditCount ?? 0} followed sourced desk${(data.followedPunditCount ?? 0) === 1 ? "" : "s"}.`
            : "Follow desks to filter this tab to voices you actually argue with."}{" "}
          <Link href="/pundits" className="font-semibold text-foreground hover:underline">
            Manage follows
          </Link>
        </p>
      )}

      {tab === "stories" ? (
        <StudioStoryWorkspace receiptId={receiptId} />
      ) : tab === "script" ? (
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
        <StudioTabEmpty tab={tab} />
      )}
    </div>
  );
}

function ScriptTab() {
  return (
    <div className="space-y-4">
      <div className="rounded-xl border border-gold/30 bg-gold/5 p-4">
        <div className="mb-3 flex items-center gap-2">
          <span className="flex size-8 items-center justify-center rounded-full bg-gold/20">
            <Mic2 className="size-4 text-gold" aria-hidden />
          </span>
          <div>
            <p className="text-sm font-semibold">Pundit Match Analysis</p>
            <p className="text-[11px] text-muted-foreground">
              Pick a match and persona — get an 8-scene video-ready script with visual cues for HeyGen, Synthesia, or DALL-E.
            </p>
          </div>
        </div>
        <PunditScriptGenerator />
      </div>

      <div className="rounded-xl border border-border bg-card p-4">
        <p className="text-xs font-semibold">How to use with AI video tools</p>
        <ol className="mt-2 space-y-1.5 text-[11px] text-muted-foreground">
          <li className="flex gap-2">
            <span className="flex size-4 shrink-0 items-center justify-center rounded-full bg-muted text-[10px] font-bold">1</span>
            Copy each <strong>DIALOGUE</strong> block into HeyGen or Synthesia as your avatar script.
          </li>
          <li className="flex gap-2">
            <span className="flex size-4 shrink-0 items-center justify-center rounded-full bg-muted text-[10px] font-bold">2</span>
            Use the <strong>[Visual]</strong> lines as B-roll prompts in DALL-E or your video editor.
          </li>
          <li className="flex gap-2">
            <span className="flex size-4 shrink-0 items-center justify-center rounded-full bg-muted text-[10px] font-bold">3</span>
            Match <strong>[Tone]</strong> and <strong>[Camera]</strong> hints to your avatar settings and transitions.
          </li>
        </ol>
      </div>

      <div className="rounded-xl border border-border bg-card p-4">
        <div className="mb-3 flex items-center gap-2">
          <span className="flex size-8 items-center justify-center rounded-full bg-muted">
            <FileText className="size-4 text-muted-foreground" aria-hidden />
          </span>
          <div>
            <p className="text-sm font-semibold">Prediction Recap Export</p>
            <p className="text-[11px] text-muted-foreground">
              Cumulative script from your prediction picks — pre-match picks or post-match praise/burn cuts.
            </p>
          </div>
        </div>
        <CumulativeScriptExport minimal={false} />
      </div>
    </div>
  );
}

function StudioTabEmpty({ tab }: { tab: Exclude<Tab, "stories" | "script"> }) {
  const messages: Record<Exclude<Tab, "stories" | "script">, { title: string; body: string }> = {
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
      body: "Make a few predictions, then follow sourced pundits so Studio can line up your calls against theirs.",
    },
  };

  const msg = messages[tab];
  return <EmptyState title={msg.title} description={msg.body} />;
}
