import { describe, expect, it } from "vitest";
import { HOME_WELCOME_SLIDES } from "./scoring-rules";

describe("HOME_WELCOME_SLIDES", () => {
  it("leads with a compact product hero, not a three-slide tour", () => {
    expect(HOME_WELCOME_SLIDES).toHaveLength(1);
    expect(HOME_WELCOME_SLIDES.map((s) => s.id)).toEqual(["welcome"]);
    expect(HOME_WELCOME_SLIDES[0]?.title).toMatch(/receipts/i);
    expect(HOME_WELCOME_SLIDES[0]?.body.toLowerCase()).toMatch(/pundit/);
    expect(HOME_WELCOME_SLIDES[0]?.body.toLowerCase()).toMatch(/content/);
  });
});
