import { describe, expect, it } from "vitest";
import {
  formatPunditSubtitle,
  formatSourcePlatformLabel,
  PREMIER_LEAGUE_SOURCE_LABEL,
  sanitizePunditChromeLabel,
} from "./pundits";

describe("World Cup leftover label stripping", () => {
  it("replaces a World Cup-only label with a Premier League-safe source label", () => {
    expect(sanitizePunditChromeLabel("World Cup")).toBe(PREMIER_LEAGUE_SOURCE_LABEL);
    expect(sanitizePunditChromeLabel("world cup desk")).toBe(PREMIER_LEAGUE_SOURCE_LABEL);
  });

  it("replaces any World Cup mention with a Premier League-safe source label", () => {
    expect(sanitizePunditChromeLabel("Sky Sports World Cup desk")).toBe(
      PREMIER_LEAGUE_SOURCE_LABEL
    );
  });

  it("leaves Premier League chrome unchanged and does not hide empty input", () => {
    expect(sanitizePunditChromeLabel("Premier League")).toBe("Premier League");
    expect(sanitizePunditChromeLabel("  ")).toBeUndefined();
    expect(sanitizePunditChromeLabel(null)).toBeUndefined();
  });

  it("sanitizes subtitle organization and parody chrome", () => {
    expect(
      formatPunditSubtitle({
        parodyCue: "World Cup hot takes",
      })
    ).toBe(PREMIER_LEAGUE_SOURCE_LABEL);
    expect(
      formatPunditSubtitle({
        archetype: "Contrarian",
        organization: "World Cup Daily",
      })
    ).toBe(`Contrarian · ${PREMIER_LEAGUE_SOURCE_LABEL}`);
    expect(
      formatPunditSubtitle({
        organization: "World Cup",
      })
    ).toBe(PREMIER_LEAGUE_SOURCE_LABEL);
  });

  it("maps World Cup source platform strings to the PL-safe label", () => {
    expect(formatSourcePlatformLabel("World Cup TV")).toBe(PREMIER_LEAGUE_SOURCE_LABEL);
    expect(formatSourcePlatformLabel("youtube")).toBe("YouTube");
  });
});
