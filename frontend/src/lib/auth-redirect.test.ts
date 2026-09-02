import { describe, expect, it } from "vitest";
import { withSignedInQuery } from "./auth-redirect";

describe("withSignedInQuery", () => {
  it("adds signedIn to a bare path", () => {
    expect(withSignedInQuery("/")).toBe("/?signedIn=1");
  });

  it("preserves existing query params and hash", () => {
    expect(withSignedInQuery("/leagues?tab=mine#list")).toBe(
      "/leagues?tab=mine&signedIn=1#list"
    );
  });
});
