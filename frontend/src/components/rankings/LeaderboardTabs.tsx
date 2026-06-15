"use client";

import { useState } from "react";
import { LeaderboardTable } from "@/components/rankings/LeaderboardTable";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { useLeaderboard, type LeaderboardTab } from "@/hooks/useLeaderboard";
import { cn } from "@/lib/utils";

const allTabs: { value: LeaderboardTab; label: string }[] = [
  { value: "league", label: "My League" },
  { value: "global", label: "Global" },
  { value: "pundits", label: "Pundits" },
  { value: "friends", label: "Friends" },
];

interface LeaderboardTabsProps {
  embedded?: boolean;
  /** When true, only Pundits and Friends tabs (league standings use the selector above). */
  punditsOnly?: boolean;
}

export function LeaderboardTabs({ embedded = false, punditsOnly = false }: LeaderboardTabsProps) {
  const tabs = punditsOnly
    ? allTabs.filter((t) => t.value === "pundits" || t.value === "friends")
    : allTabs;

  const [activeTab, setActiveTab] = useState<LeaderboardTab>(
    punditsOnly ? "pundits" : "league"
  );
  const { data, isLoading, isError } = useLeaderboard(activeTab);

  return (
    <div>
      {!embedded ? (
        <h2 className="mb-3 text-sm font-semibold">Leagues & rankings</h2>
      ) : punditsOnly ? (
        <p className="mb-2 text-[11px] font-medium text-muted-foreground">
          Also compare against pundits & friends
        </p>
      ) : null}

      <Tabs
        value={activeTab}
        onValueChange={(value) => setActiveTab(value as LeaderboardTab)}
      >
        <TabsList className="flex h-auto w-full gap-1 rounded-full border border-border bg-muted/70 p-1 backdrop-blur-sm">
          {tabs.map((tab) => (
            <TabsTrigger
              key={tab.value}
              value={tab.value}
              className={cn(
                "flex-1 rounded-full border-0 bg-transparent px-1 py-1.5 text-[11px] font-medium text-muted-foreground transition-all duration-200",
                "hover:text-foreground",
                "data-[state=active]:-translate-y-px data-[state=active]:bg-card data-[state=active]:font-semibold data-[state=active]:text-foreground data-[state=active]:shadow-md data-[state=active]:ring-1 data-[state=active]:ring-gold/40"
              )}
            >
              {tab.label}
            </TabsTrigger>
          ))}
        </TabsList>
        {tabs.map((tab) => (
          <TabsContent key={tab.value} value={tab.value} className="mt-3">
            {isError && tab.value === activeTab && (
              <p className="mb-2 text-xs text-muted-foreground">Demo data shown</p>
            )}
            <LeaderboardTable
              entries={tab.value === activeTab ? (data?.entries ?? []) : []}
              me={tab.value === activeTab ? data?.me : null}
              totalPlayers={tab.value === activeTab ? data?.totalPlayers : undefined}
              isLoading={tab.value === activeTab && isLoading}
            />
          </TabsContent>
        ))}
      </Tabs>
    </div>
  );
}
