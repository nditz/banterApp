"use client";

import { useQuery } from "@tanstack/react-query";
import { apiFetch, ApiError } from "@/lib/api";
import { normalizeLeaderboardView } from "@/lib/leaderboard";
import {
  mockFriendsLeaderboard,
  mockGlobalLeaderboard,
  mockLeagueLeaderboard,
  mockSystemLeagues,
  mockPunditLeaderboard,
} from "@/lib/mock-data";
import { detectCountryCode, getStoredCountryCode } from "@/lib/country";
import { useSession } from "@/hooks/useSession";
import type {
  LeaderboardEntry,
  LeaderboardView,
  League,
  LeagueKind,
  LeagueLimits,
  LeaguePreview,
  MyLeaguesPayload,
} from "@/lib/types";

export type LeaderboardTab = "league" | "global" | "pundits" | "friends";

const endpoints: Record<LeaderboardTab, string> = {
  league: "/api/leaderboards/leagues",
  global: "/api/leaderboards/global",
  pundits: "/api/leaderboards/pundits",
  friends: "/api/leaderboards/friends",
};

const mockData: Record<LeaderboardTab, LeaderboardEntry[]> = {
  league: mockLeagueLeaderboard,
  global: mockGlobalLeaderboard,
  pundits: mockPunditLeaderboard,
  friends: mockFriendsLeaderboard,
};

function mockView(tab: LeaderboardTab): LeaderboardView {
  const entries = mockData[tab];
  return {
    entries,
    me: entries.find((e) => e.displayName === "You") ?? null,
    totalPlayers: entries.length,
  };
}

async function fetchLeaderboard(tab: LeaderboardTab): Promise<LeaderboardView> {
  try {
    const response = await apiFetch<unknown>(endpoints[tab]);
    const view = normalizeLeaderboardView(response);
    if (view.entries.length > 0 || tab === "pundits") {
      return view;
    }
    return mockView(tab);
  } catch (error) {
    if (error instanceof ApiError && tab !== "pundits") {
      return mockView(tab);
    }
    if (error instanceof ApiError) {
      return { entries: [], me: null, totalPlayers: 0 };
    }
    throw error;
  }
}

export function useLeaderboard(tab: LeaderboardTab) {
  return useQuery({
    queryKey: ["leaderboard", tab],
    queryFn: () => fetchLeaderboard(tab),
    staleTime: 60_000,
    enabled: tab !== "league",
  });
}

async function fetchLeagueLeaderboard(leagueId: string): Promise<LeaderboardView> {
  try {
    const response = await apiFetch<unknown>(`/api/leaderboards/leagues/${leagueId}`);
    return normalizeLeaderboardView(response);
  } catch (error) {
    if (error instanceof ApiError) {
      return mockView("league");
    }
    throw error;
  }
}

export function useLeagueLeaderboard(leagueId: string | null | undefined) {
  return useQuery({
    queryKey: ["leaderboard", "league", leagueId],
    queryFn: () => fetchLeagueLeaderboard(leagueId!),
    enabled: Boolean(leagueId),
    staleTime: 60_000,
  });
}

const DEFAULT_LIMITS: LeagueLimits = {
  customLeaguesUsed: 0,
  customLeaguesMax: 3,
  totalLeaguesUsed: 0,
  totalLeaguesMax: 5,
};

function normalizeLimits(raw: Record<string, unknown> | undefined): LeagueLimits {
  if (!raw) return DEFAULT_LIMITS;
  return {
    customLeaguesUsed: Number(raw.customLeaguesUsed ?? raw.CustomLeaguesUsed ?? 0),
    customLeaguesMax: Number(raw.customLeaguesMax ?? raw.CustomLeaguesMax ?? 3),
    totalLeaguesUsed: Number(raw.totalLeaguesUsed ?? raw.TotalLeaguesUsed ?? 0),
    totalLeaguesMax: Number(raw.totalLeaguesMax ?? raw.TotalLeaguesMax ?? 5),
  };
}

function normalizeMyLeaguesPayload(response: unknown): MyLeaguesPayload {
  if (Array.isArray(response)) {
    return {
      leagues: response.map((item) => normalizeLeague(item as Record<string, unknown>)),
      limits: DEFAULT_LIMITS,
    };
  }

  if (response && typeof response === "object") {
    const payload = response as Record<string, unknown>;
    const rawLeagues = payload.leagues ?? payload.Leagues;
    const leagues = Array.isArray(rawLeagues)
      ? rawLeagues.map((item) => normalizeLeague(item as Record<string, unknown>))
      : [];
    const limits = normalizeLimits(
      (payload.limits ?? payload.Limits) as Record<string, unknown> | undefined
    );
    return { leagues, limits };
  }

  return { leagues: [], limits: DEFAULT_LIMITS };
}

export function useMyLeagues() {
  const { data: session } = useSession();
  const storedCountry = typeof window !== "undefined" ? getStoredCountryCode() : null;
  const previewCountry =
    typeof window !== "undefined" ? detectCountryCode() : "GB";
  const queryCountry = session?.termsAccepted ? storedCountry ?? previewCountry : previewCountry;

  return useQuery({
    queryKey: ["leagues", session?.termsAccepted ? "member" : "guest", queryCountry],
    queryFn: async () => {
      try {
        const path = session?.termsAccepted
          ? "/api/leagues"
          : `/api/leagues?countryCode=${encodeURIComponent(queryCountry)}`;
        const response = await apiFetch<unknown>(path);
        const payload = normalizeMyLeaguesPayload(response);
        if (payload.leagues.length > 0) {
          return payload;
        }
        return { leagues: mockSystemLeagues, limits: DEFAULT_LIMITS };
      } catch (error) {
        if (error instanceof ApiError) {
          return { leagues: mockSystemLeagues, limits: DEFAULT_LIMITS };
        }
        throw error;
      }
    },
    staleTime: 60_000,
    enabled: session !== undefined,
  });
}

/** @deprecated Use useMyLeagues instead */
export function useLeagues() {
  const query = useMyLeagues();
  return {
    ...query,
    data: query.data?.leagues,
  };
}

function normalizeLeague(raw: Record<string, unknown>): League {
  const kindRaw = String(raw.kind ?? raw.Kind ?? "custom").toLowerCase();
  const kind = (["custom", "global", "country"].includes(kindRaw)
    ? kindRaw
    : "custom") as LeagueKind;

  return {
    id: String(raw.id ?? ""),
    name: String(raw.name ?? "League"),
    inviteCode: String(raw.inviteCode ?? ""),
    memberCount: Number(raw.memberCount ?? 1),
    maxMembers: raw.maxMembers !== undefined ? Number(raw.maxMembers) : undefined,
    isAdmin: raw.isAdmin === true,
    myDisplayName:
      typeof raw.myDisplayName === "string" ? raw.myDisplayName : undefined,
    points: raw.myPoints !== undefined ? Number(raw.myPoints) : undefined,
    kind,
    bonusPointsEnabled: raw.bonusPointsEnabled === true,
    countryCode:
      typeof raw.countryCode === "string"
        ? raw.countryCode
        : typeof raw.CountryCode === "string"
          ? raw.CountryCode
          : undefined,
  };
}

export function pickDefaultLeague(leagues: League[]): League | null {
  if (leagues.length === 0) return null;
  const global = leagues.find((l) => l.kind === "global");
  if (global) return global;
  const country = leagues.find((l) => l.kind === "country");
  if (country) return country;
  return leagues.find((l) => l.kind === "custom") ?? leagues[0];
}

export function useLeaguePreview(inviteCode: string) {
  return useQuery({
    queryKey: ["league-preview", inviteCode],
    queryFn: () =>
      apiFetch<LeaguePreview>(
        `/api/leagues/preview?inviteCode=${encodeURIComponent(inviteCode)}`,
        { skipAuth: true }
      ),
    enabled: Boolean(inviteCode),
    retry: 1,
  });
}

export function useCreateLeague() {
  return async (name: string) => {
    try {
      return await apiFetch<League>("/api/leagues/create", {
        method: "POST",
        body: JSON.stringify({ name }),
      });
    } catch (error) {
      if (error instanceof ApiError && error.status >= 500) {
        return {
          id: `mock-${Date.now()}`,
          name,
          inviteCode: `WC${Math.random().toString(36).slice(2, 8).toUpperCase()}`,
          memberCount: 1,
          maxMembers: 50,
          isAdmin: true,
          myDisplayName: "player@example.com",
          points: 0,
        } satisfies League;
      }
      throw error;
    }
  };
}

export function useJoinLeague() {
  return async (inviteCode: string) => {
    return await apiFetch<League>("/api/leagues/join", {
      method: "POST",
      body: JSON.stringify({ inviteCode }),
    });
  };
}
