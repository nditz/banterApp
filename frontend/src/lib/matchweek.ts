import type { Match } from "@/lib/types";

const londonDateKey = new Intl.DateTimeFormat("en-CA", {
  timeZone: "Europe/London",
  year: "numeric",
  month: "2-digit",
  day: "2-digit",
});

const londonDateLabel = new Intl.DateTimeFormat("en-GB", {
  timeZone: "Europe/London",
  weekday: "long",
  day: "numeric",
  month: "long",
});

export type MatchweekDayGroup = {
  key: string;
  label: string;
  matches: Match[];
};

/** Group a matchweek the Premier League / BBC Sport way: by UK calendar day, then kickoff. */
export function groupMatchesByUkDate(matches: Match[]): MatchweekDayGroup[] {
  const grouped = new Map<string, Match[]>();

  for (const match of [...matches].sort(
    (a, b) => new Date(a.kickoffTime).getTime() - new Date(b.kickoffTime).getTime()
  )) {
    const when = new Date(match.kickoffTime);
    const key = londonDateKey.format(when);
    const list = grouped.get(key);
    if (list) {
      list.push(match);
    } else {
      grouped.set(key, [match]);
    }
  }

  return [...grouped.entries()].map(([key, dayMatches]) => ({
    key,
    label: londonDateLabel.format(new Date(dayMatches[0].kickoffTime)),
    matches: dayMatches,
  }));
}
