import { describe, expect, it } from "vitest";
import { getClubBadgeUrl } from "./club-badges";

describe("getClubBadgeUrl", () => {
  it("uses Newcastle's club crest, not a country flag", () => {
    expect(getClubBadgeUrl("NEW", "Newcastle")).toBe(
      "https://media.api-sports.io/football/teams/34.png"
    );
  });

  it("resolves 2026/27 promoted clubs", () => {
    expect(getClubBadgeUrl("COV", "Coventry City")).toContain("/teams/71.png");
    expect(getClubBadgeUrl("HUL", "Hull City")).toContain("/teams/64.png");
    expect(getClubBadgeUrl("IPS", "Ipswich Town")).toContain("/teams/57.png");
  });
});
