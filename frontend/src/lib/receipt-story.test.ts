import { describe, expect, it } from "vitest";
import { formatReceiptStoryCandidate, formatReceiptStoryType } from "./receipt-story";

describe("receipt story labels", () => {
  it("labels classified events without inventing quotes", () => {
    expect(formatReceiptStoryType("beat_pundit")).toBe("You beat the pundit");
    expect(formatReceiptStoryType("exact_score")).toBe("Exact score");
    expect(formatReceiptStoryType("miss")).toBe("Miss");
  });

  it("labels story candidates from classified types without inventing copy", () => {
    expect(
      formatReceiptStoryCandidate({
        storyType: "beat_pundit",
        summary: "You beat the sourced desk",
      })
    ).toBe("You beat the pundit · You beat the sourced desk");
    expect(formatReceiptStoryCandidate({ storyType: "hit" })).toBe("Hit");
  });
});
