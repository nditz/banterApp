import { describe, expect, it } from "vitest";
import { HOME_WELCOME_SLIDES } from "./scoring-rules";

describe("HOME_WELCOME_SLIDES", () => {
  it("stays short and leads with picks, feed, then Studio", () => {
    expect(HOME_WELCOME_SLIDES).toHaveLength(3);
    expect(HOME_WELCOME_SLIDES.map((s) => s.id)).toEqual([
      "welcome",
      "banter",
      "content",
    ]);
    expect(HOME_WELCOME_SLIDES[0]?.body.toLowerCase()).toMatch(/pick/);
    expect(HOME_WELCOME_SLIDES[1]?.id).toBe("banter");
    expect(HOME_WELCOME_SLIDES[2]?.body.toLowerCase()).toMatch(/studio/);
  });
});
