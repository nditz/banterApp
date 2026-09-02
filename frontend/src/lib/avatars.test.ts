import { describe, expect, it } from "vitest";
import { getSupabaseAvatarUrl, getSupabaseDisplayName } from "./avatars";

describe("getSupabaseAvatarUrl", () => {
  it("prefers a https avatar_url from user metadata", () => {
    expect(
      getSupabaseAvatarUrl({
        user_metadata: {
          avatar_url: "https://lh3.googleusercontent.com/a/photo",
          picture: "https://example.com/other.png",
        },
      })
    ).toBe("https://lh3.googleusercontent.com/a/photo");
  });

  it("rejects javascript URLs", () => {
    expect(
      getSupabaseAvatarUrl({
        user_metadata: { avatar_url: "javascript:alert(1)" },
      })
    ).toBeUndefined();
  });
});

describe("getSupabaseDisplayName", () => {
  it("uses full_name then email local-part", () => {
    expect(
      getSupabaseDisplayName({
        email: "sam@example.com",
        user_metadata: { full_name: "Sam Player" },
      })
    ).toBe("Sam Player");
    expect(getSupabaseDisplayName({ email: "sam@example.com", user_metadata: {} })).toBe(
      "sam"
    );
  });
});
