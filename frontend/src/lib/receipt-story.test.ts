import { describe, expect, it } from "vitest";
import { formatReceiptStoryType } from "./receipt-story";

describe("receipt story labels", () => {
  it("labels classified events without inventing quotes", () => {
    expect(formatReceiptStoryType("beat_pundit")).toBe("You beat the pundit");
    expect(formatReceiptStoryType("exact_score")).toBe("Exact score");
    expect(formatReceiptStoryType("miss")).toBe("Miss");
  });
});
