import type { LeaderboardEntry, LeaderboardView } from "./types";

type RawLeaderboardEntry = Record<string, unknown>;

export function normalizeLeaderboardEntry(
  raw: RawLeaderboardEntry,
  index: number
): LeaderboardEntry | null {
  const displayName =
    pickString(raw, "displayName", "name", "DisplayName", "Name") ??
    "Player";

  const userId =
    pickString(raw, "userId", "punditId", "id", "UserId", "PunditId") ??
    `player-${index}`;

  const points = pickNumber(
    raw,
    "points",
    "totalPoints",
    "Points",
    "TotalPoints"
  );

  const correctPredictions = pickNumber(
    raw,
    "correctPredictions",
    "CorrectPredictions"
  );

  const totalPredictions = pickNumber(
    raw,
    "totalPredictions",
    "predictionsCount",
    "TotalPredictions",
    "PredictionsCount"
  );

  const rank = pickNumber(raw, "rank", "Rank") ?? index + 1;

  const organization = pickString(
    raw,
    "organization",
    "Organization"
  );

  const isPundit =
    raw.isPundit === true ||
    raw.punditId !== undefined ||
    raw.PunditId !== undefined ||
    organization !== undefined;

  const isCurrentUser =
    raw.isCurrentUser === true || raw.IsCurrentUser === true;

  return {
    rank,
    userId,
    displayName,
    points: points ?? correctPredictions ?? 0,
    correctPredictions,
    totalPredictions,
    isPundit,
    organization,
    isCurrentUser,
  };
}

export function normalizeLeaderboardResponse(response: unknown): LeaderboardEntry[] {
  if (Array.isArray(response)) {
    return response
      .map((item, index) =>
        normalizeLeaderboardEntry(item as RawLeaderboardEntry, index)
      )
      .filter((entry): entry is LeaderboardEntry => entry !== null);
  }

  if (response && typeof response === "object") {
    const payload = response as RawLeaderboardEntry;

    for (const key of ["top", "standings", "items", "entries", "data"] as const) {
      const nested = payload[key];
      if (Array.isArray(nested)) {
        return normalizeLeaderboardResponse(nested);
      }
    }
  }

  return [];
}

/**
 * Normalizes the FPL-style leaderboard payload: top N entries, the current
 * user's pinned row, and the total player count. Falls back gracefully for
 * plain-array responses (e.g. pundits).
 */
export function normalizeLeaderboardView(response: unknown): LeaderboardView {
  const entries = normalizeLeaderboardResponse(response);

  let me: LeaderboardEntry | null =
    entries.find((e) => e.isCurrentUser) ?? null;
  let totalPlayers = entries.length;

  if (response && typeof response === "object" && !Array.isArray(response)) {
    const payload = response as RawLeaderboardEntry;

    const rawMe = payload.me ?? payload.Me;
    if (rawMe && typeof rawMe === "object") {
      me =
        normalizeLeaderboardEntry(rawMe as RawLeaderboardEntry, entries.length) ??
        me;
      if (me) me.isCurrentUser = true;
    }

    const rawTotal = pickNumber(payload, "totalPlayers", "TotalPlayers");
    if (rawTotal !== undefined) {
      totalPlayers = rawTotal;
    }
  }

  return { entries, me, totalPlayers };
}

function pickString(
  raw: RawLeaderboardEntry,
  ...keys: string[]
): string | undefined {
  for (const key of keys) {
    const value = raw[key];
    if (typeof value === "string" && value.trim()) {
      return value.trim();
    }
  }
  return undefined;
}

function pickNumber(
  raw: RawLeaderboardEntry,
  ...keys: string[]
): number | undefined {
  for (const key of keys) {
    const value = raw[key];
    if (typeof value === "number" && !Number.isNaN(value)) {
      return value;
    }
  }
  return undefined;
}

export function getInitials(displayName: string): string {
  const parts = displayName.trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) return "??";
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
  return `${parts[0][0] ?? ""}${parts[1][0] ?? ""}`.toUpperCase();
}
