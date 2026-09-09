import { describe, expect, it } from "vitest";
import { comparisonPhase } from "./comparison-phase";

describe("comparisonPhase", () => {
  it("is before kickoff when there is no result", () => {
    expect(comparisonPhase({ status: "NS" })).toBe("before");
    expect(comparisonPhase({ status: "TIMED" })).toBe("before");
  });

  it("is after full time when a scoreline or FT status is present", () => {
    expect(comparisonPhase({ actualResult: "2-1", status: "FT" })).toBe("after");
    expect(comparisonPhase({ status: "FT" })).toBe("after");
  });
});
