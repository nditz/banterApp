/** Premier League ranking during a season: points, then GD, then goals scored. */
export type LeagueTableRow = {
  rank: number;
  teamCode: string;
  teamName: string;
  logoUrl?: string;
  played: number;
  won: number;
  drawn: number;
  lost: number;
  goalsFor: number;
  goalsAgainst: number;
  goalDiff: number;
  points: number;
};

export function rankPremierLeagueTable(rows: LeagueTableRow[]): LeagueTableRow[] {
  return [...rows]
    .sort((a, b) => {
      if (b.points !== a.points) return b.points - a.points;
      if (b.goalDiff !== a.goalDiff) return b.goalDiff - a.goalDiff;
      const gfA = a.goalsFor ?? 0;
      const gfB = b.goalsFor ?? 0;
      if (gfB !== gfA) return gfB - gfA;
      return a.teamName.localeCompare(b.teamName, "en", { sensitivity: "base" });
    })
    .map((row, index) => ({ ...row, rank: index + 1 }));
}
