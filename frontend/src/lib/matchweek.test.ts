import { describe, expect, it } from "vitest";
import { groupMatchesByUkDate } from "./matchweek";
import type { Match } from "./types";

function match(id: string, kickoffTime: string): Match {
  return {
    id,
    teamA: id,
    teamB: "Away",
    kickoffTime,
  };
}

describe("groupMatchesByUkDate", () => {
  it("groups by London calendar day, not UTC date", () => {
    const groups = groupMatchesByUkDate([
      match("late-utc-saturday", "2026-08-22T23:00:00Z"),
      match("saturday-lunch", "2026-08-22T11:30:00Z"),
    ]);

    expect(groups.map((g) => g.key)).toEqual(["2026-08-22", "2026-08-23"]);
    expect(groups[0].matches.map((m) => m.id)).toEqual(["saturday-lunch"]);
    expect(groups[1].matches.map((m) => m.id)).toEqual(["late-utc-saturday"]);
    expect(groups[1].label).toMatch(/Sunday/i);
  });
});
