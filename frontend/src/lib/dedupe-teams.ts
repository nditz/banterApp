export interface TeamCodeName {
  code: string;
  name: string;
}

/** Keep one entry per ISO-style team code (case-insensitive). */
export function dedupeTeamsByCode<T extends TeamCodeName>(teams: T[]): T[] {
  const byCode = new Map<string, T>();
  for (const team of teams) {
    const key = team.code.trim().toUpperCase();
    if (!key || byCode.has(key)) continue;
    byCode.set(key, team);
  }
  return Array.from(byCode.values()).sort((a, b) =>
    a.name.localeCompare(b.name, undefined, { sensitivity: "base" })
  );
}
