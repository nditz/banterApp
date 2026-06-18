import type { PredictionOutcome } from "@/lib/reactionEngine";

function hashString(value: string): number {
  let hash = 0;
  for (let i = 0; i < value.length; i += 1) {
    hash = (hash << 5) - hash + value.charCodeAt(i);
    hash |= 0;
  }
  return Math.abs(hash);
}

function normalizeProbabilities(
  home: number,
  draw: number,
  away: number
): Record<PredictionOutcome, number> {
  const total = home + draw + away;
  if (total === 0) {
    return { home: 34, draw: 33, away: 33 };
  }

  const scaled = {
    home: Math.round((home / total) * 100),
    draw: Math.round((draw / total) * 100),
    away: Math.round((away / total) * 100),
  };

  const delta = 100 - (scaled.home + scaled.draw + scaled.away);
  if (delta !== 0) {
    const favorite = (Object.entries(scaled).sort((a, b) => b[1] - a[1])[0][0]) as PredictionOutcome;
    scaled[favorite] += delta;
  }

  return scaled;
}

/**
 * Deterministic fallback when provider probabilities are unavailable.
 * Uses fixture identity so the same match always gets the same model.
 */
export function estimateFixtureProbabilities(
  fixtureId: string,
  homeTeamName: string,
  awayTeamName: string
): Record<PredictionOutcome, number> {
  const seed = hashString(`${fixtureId}:${homeTeamName}:${awayTeamName}`);
  const home = 30 + (seed % 35);
  const away = 20 + ((seed >> 8) % 30);
  const draw = 18 + ((seed >> 16) % 15);
  return normalizeProbabilities(home, draw, away);
}

export function scorelineToOutcome(scoreline: string): PredictionOutcome | null {
  const [homeRaw, awayRaw] = scoreline.split("-").map((part) => Number(part.trim()));
  if (!Number.isFinite(homeRaw) || !Number.isFinite(awayRaw)) return null;
  if (homeRaw > awayRaw) return "home";
  if (homeRaw < awayRaw) return "away";
  return "draw";
}

export function formatProbabilityContext(
  probabilities: Record<PredictionOutcome, number>,
  homeTeamName: string,
  awayTeamName: string
): string {
  const entries = Object.entries(probabilities) as [PredictionOutcome, number][];
  const favorite = entries.sort((a, b) => b[1] - a[1])[0];
  const favoriteLabel =
    favorite[0] === "home" ? homeTeamName : favorite[0] === "away" ? awayTeamName : "Draw";

  return `Odds read: ${favoriteLabel} is the chalk at ${favorite[1]}% · Draw ${probabilities.draw}% · ${homeTeamName} ${probabilities.home}% · ${awayTeamName} ${probabilities.away}%`;
}

export function formatPickOddsHint(
  outcome: PredictionOutcome,
  probability: number,
  homeTeamName: string,
  awayTeamName: string
): string {
  const label =
    outcome === "home" ? homeTeamName : outcome === "away" ? awayTeamName : "Draw";

  if (probability >= 45) {
    return `Chalk · ${probability}% — safe but valid`;
  }
  if (probability >= 30) {
    return `${probability}% — mid, not mid`;
  }
  if (probability >= 18) {
    return `${probability}% — spicy, I see you`;
  }
  return `${probability}% on ${label} — delulu era unlocked`;
}
