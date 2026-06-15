"use client";

import { useEffect, useState } from "react";
import { FeedList } from "@/components/feed/FeedList";
import { BanterLine } from "@/components/BanterLine";
import { Panel } from "@/components/ui/panel";
import { getLocalBanterEntries, type LocalBanterEntry } from "@/lib/banterFeed";

export function BanterFeedPanel() {
  const [localBanter, setLocalBanter] = useState<LocalBanterEntry[]>([]);

  useEffect(() => {
    const refresh = () => setLocalBanter(getLocalBanterEntries());
    refresh();

    window.addEventListener("banter-feed-updated", refresh);
    return () => window.removeEventListener("banter-feed-updated", refresh);
  }, []);

  return (
    <Panel
      id="banter-feed-heading"
      title="Banter feed"
      subtitle="Hot takes, memes & matchday chaos"
      accent="flare"
    >
      {localBanter.length > 0 && (
        <div className="mb-3 space-y-2">
          <p className="text-[11px] font-bold uppercase tracking-widest text-flare">
            Live league banter
          </p>
          {localBanter.slice(0, 4).map((entry) => (
            <BanterLine key={entry.id} emoji={entry.emoji} text={entry.line} />
          ))}
        </div>
      )}
      <FeedList embedded />
    </Panel>
  );
}
