import { getBanterMediaForReaction } from "@/lib/feed-media";
import type { FeedMedia } from "@/lib/types";
import {
  banterTemplates,
  type ReactionKey,
} from "@/reactions/reactionContent";

const BANTER_FEED_KEY = "banter_local_feed";
const MAX_ENTRIES = 50;

export interface LocalBanterEntry {
  id: string;
  name: string;
  pick: string;
  fixture: string;
  line: string;
  emoji: string;
  imageUrl?: string;
  media?: FeedMedia;
  createdAt: string;
}

function pickRandom<T>(items: T[]): T {
  return items[Math.floor(Math.random() * items.length)];
}

export function buildBanterLine(
  reactionKey: ReactionKey,
  pick: string
): string {
  const templates = banterTemplates[reactionKey as keyof typeof banterTemplates];
  if (!templates?.length) {
    return `Locked in: ${pick}.`;
  }

  const template = pickRandom(templates);
  return template.replaceAll("{pick}", pick);
}

export function getLocalBanterEntries(): LocalBanterEntry[] {
  if (typeof window === "undefined") return [];
  try {
    const raw = localStorage.getItem(BANTER_FEED_KEY);
    if (!raw) return [];
    const parsed = JSON.parse(raw) as LocalBanterEntry[];
    return Array.isArray(parsed) ? parsed : [];
  } catch {
    return [];
  }
}

export function addLocalBanterEntry(entry: {
  pick: string;
  fixture: string;
  line: string;
  emoji: string;
  reactionKey?: ReactionKey;
  reactionAsset?: string;
}): LocalBanterEntry {
  const media =
    entry.reactionKey && entry.reactionAsset
      ? getBanterMediaForReaction(entry.reactionKey, entry.reactionAsset)
      : undefined;

  const created: LocalBanterEntry = {
    id: crypto.randomUUID(),
    createdAt: new Date().toISOString(),
    name: "",
    ...entry,
    imageUrl: media?.url ?? entry.reactionAsset,
    media,
  };

  if (typeof window === "undefined") return created;

  const next = [created, ...getLocalBanterEntries()].slice(0, MAX_ENTRIES);
  localStorage.setItem(BANTER_FEED_KEY, JSON.stringify(next));
  window.dispatchEvent(new CustomEvent("banter-feed-updated"));
  return created;
}
