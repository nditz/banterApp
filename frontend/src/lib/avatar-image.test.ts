import { describe, expect, it } from "vitest";
import { AVATAR_MAX_INPUT_BYTES, getAvatarFileError } from "./avatar-image";

describe("getAvatarFileError", () => {
  it("rejects non-images", () => {
    const file = new File(["x"], "notes.txt", { type: "text/plain" });
    expect(getAvatarFileError(file)).toMatch(/photo/i);
  });

  it("rejects files over 8 MB", () => {
    const file = new File([new Uint8Array(AVATAR_MAX_INPUT_BYTES + 1)], "huge.png", {
      type: "image/png",
    });
    expect(getAvatarFileError(file)).toMatch(/8 MB/i);
  });

  it("accepts a small jpeg", () => {
    const file = new File([new Uint8Array(32)], "me.jpg", { type: "image/jpeg" });
    expect(getAvatarFileError(file)).toBeNull();
  });
});
