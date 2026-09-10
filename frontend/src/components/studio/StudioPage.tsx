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
import { Panel } from "@/components/ui/panel";
import { PageContainer, SectionHeader } from "@/components/ui/section-header";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState, ErrorState } from "@/components/ui/states";
import { useStudio } from "@/hooks/useStudio";
import { PRODUCT_METRICS, recordMetric } from "@/lib/metrics";
import { cn } from "@/lib/utils";
import type { StudioPickRole } from "@/lib/types";

type Tab = "stories" | "my_picks" | "vs_league" | "vs_pundits" | "script";

const tabs: {
  id: Tab;
  label: string;
  shortLabel: string;
  icon: React.ReactNode;
  description: string;
}[] = [
  {
    id: "stories",
    label: "Stories",
    shortLabel: "Stories",
    icon: <Clapperboard className="size-3.5" aria-hidden />,
    description: "Pick a receipt, pundit clash, or sourced story — then export a content pack",
  },
  {
    id: "my_picks",
    label: "My Picks",
    shortLabel: "Picks",
    icon: <FileText className="size-3.5" aria-hidden />,
    description: "A full breakdown of every prediction you've made",
  },
  {
    id: "vs_league",
    label: "vs League",
    shortLabel: "League",
    icon: <Users className="size-3.5" aria-hidden />,
    description: "How your calls stack up against your league mates",
  },
  {
    id: "vs_pundits",
    label: "vs Pundits",
    shortLabel: "Pundits",
    icon: <Mic2 className="size-3.5" aria-hidden />,
    description: "Your picks side-by-side with sourced pundit predictions",
  },
  {
    id: "script",
    label: "Legacy export / persona",
    shortLabel: "Legacy",
    icon: <Sparkles className="size-3.5" aria-hidden />,
    description:
      "Legacy persona scripts and recap exports. Stories is the Studio home — start from a receipt or sourced headline.",
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
  const { data, isLoading, isError, refetch } = useStudio();

  const activeTab = tabs.find((t) => t.id === tab)!;
  const filter = roleFilter[tab];

  useEffect(() => {
    recordMetric(PRODUCT_METRICS.studioOpened);
  }, []);

  return (
    <PageContainer width="wide" className="space-y-5">
      <SectionHeader
        as="h1"
        eyebrow="Creator workspace"
        title="Content Studio"
        description="Pick a story · lock the facts · export a pack for your editor or AI tool"
      />

      {isLoading ? (
        <div className="flex gap-3">
          {[0, 1, 2].map((i) => (
            <Skeleton key={i} className="h-16 flex-1 rounded-xl" />
          ))}
        </div>
      ) : isError ? (
        <ErrorState
          dense
          title="Studio comparison unavailable"
          description="Stories still load below. League and pundit comparison totals will return when this request succeeds."
          onRetry={() => void refetch()}
        />
      ) : (
        data && <StudioSummaryBar data={data} />
      )}

      <div className="-mx-1 overflow-x-auto">
        <div
          className="flex min-w-max gap-1 rounded-xl border border-border bg-muted/40 p-1 sm:min-w-0"
          role="tablist"
          aria-label="Studio sections"
        >
          {tabs.map((t) => (
            <button
              key={t.id}
              type="button"
              role="tab"
              aria-label={t.label}
              aria-selected={tab === t.id}
              onPointerDown={() => setTab(t.id)}
              onClick={() => setTab(t.id)}
              className={cn(
                "inline-flex shrink-0 cursor-pointer items-center justify-center gap-1.5 rounded-lg px-2.5 py-2 text-xs font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring/50 sm:flex-1 [&_svg]:pointer-events-none",
                tab === t.id
                  ? "bg-card text-foreground shadow-sm ring-1 ring-border"
                  : "text-muted-foreground hover:bg-card/60 hover:text-foreground"
              )}
            >
              {t.icon}
              <span aria-hidden="true" className="sm:hidden">{t.shortLabel}</span>
              <span aria-hidden="true" className="hidden sm:inline">{t.label}</span>
            </button>
          ))}
        </div>
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
    </PageContainer>
  );
}

function ScriptTab() {
  return (
    <div className="space-y-4">
      <Panel
        title="Legacy export / persona"
        subtitle="Parody persona scripts are not sourced pundit quotes. For a receipt-backed pack, stay on Stories."
        accent="gold"
      >
        <PunditScriptGenerator />
      </Panel>

      <div className="rounded-xl border border-dashed border-border bg-muted/20 p-4">
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

      <Panel
        title="Prediction recap export"
        subtitle="Cumulative script from your prediction picks — pre-match picks or post-match praise/burn cuts."
      >
        <CumulativeScriptExport minimal={false} />
      </Panel>
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
