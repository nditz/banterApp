export type FootballDatasetStatus = "ok" | "empty" | "error" | "stale";

export type CurrentMatchweekPayload = {
  number: number;
  matches: import("./types").Match[];
  status?: FootballDatasetStatus;
  source?: string;
  official?: boolean;
  error?: string | null;
};

export type StandingsPayload = {
  status: FootballDatasetStatus;
  source?: string;
  lastSyncedAt?: string | null;
  error?: string | null;
  rows: import("./league-table").LeagueTableRow[];
};

const FINISHED_STATUSES = new Set(["FT", "AET", "PEN", "WO", "CANC", "ABD"]);
const LIVE_STATUSES = new Set(["LIVE", "1H", "2H", "HT", "ET", "BT", "P", "INT", "SUSP"]);

export function looksLikeMockIds(matchIds: string[]): boolean {
  return matchIds.length > 0 && matchIds.every((id) => id.toLowerCase().startsWith("pl26-"));
}

export function hasOverdueUnfinished(
  matches: Array<{ status?: string; kickoffTime: string }>,
  now = Date.now()
): boolean {
  return matches.some((match) => {
    const status = match.status?.toUpperCase() ?? "";
    if (FINISHED_STATUSES.has(status) || LIVE_STATUSES.has(status)) {
      return false;
    }
    const kickoff = new Date(match.kickoffTime).getTime();
    return Number.isFinite(kickoff) && kickoff <= now;
  });
}

export function isUnofficialMatchweek(payload?: CurrentMatchweekPayload): boolean {
  if (!payload) return false;
  if (payload.official === false) return true;
  if (payload.official === true) return false;
  return looksLikeMockIds(payload.matches.map((match) => match.id));
}

export function parseStandingsPayload(payload: unknown): StandingsPayload {
  if (Array.isArray(payload)) {
    return {
      status: payload.length > 0 ? "ok" : "empty",
      rows: payload,
    };
  }

  if (payload && typeof payload === "object" && "rows" in payload) {
    const body = payload as StandingsPayload;
    return {
      status: body.status ?? (body.rows.length > 0 ? "ok" : "empty"),
      source: body.source,
      lastSyncedAt: body.lastSyncedAt,
      error: body.error,
      rows: body.rows ?? [],
    };
  }

  return { status: "error", error: "Standings response was not recognised.", rows: [] };
}

export function datasetStatusFromMatchweek(
  payload: CurrentMatchweekPayload | undefined,
  isError: boolean
): FootballDatasetStatus {
  if (isError) return "error";
  if (!payload) return "empty";
  if (payload.status) return payload.status;
  if (payload.matches.length === 0) return "empty";
  return hasOverdueUnfinished(payload.matches) ? "stale" : "ok";
}
