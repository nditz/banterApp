import { describe, expect, it } from "vitest";
import { canUseNextImage, DEFAULT_FEED_MEDIA, resolveFeedMedia } from "./feed-media";

describe("DEFAULT_FEED_MEDIA", () => {
  it("does not use Unsplash or other stock photography", () => {
    for (const media of Object.values(DEFAULT_FEED_MEDIA)) {
      expect(media?.url).not.toMatch(/unsplash\.com/i);
      expect(media?.url).toMatch(/^\/reactions\//);
    }
  });

  it("has no default image for news, leaderboard, or highlights", () => {
    expect(DEFAULT_FEED_MEDIA.news).toBeUndefined();
    expect(DEFAULT_FEED_MEDIA.leaderboard).toBeUndefined();
    expect(DEFAULT_FEED_MEDIA.prediction_highlight).toBeUndefined();
  });
});

describe("resolveFeedMedia", () => {
  it("returns undefined for news without media so the UI can show a placeholder", () => {
    expect(
      resolveFeedMedia({ type: "news", title: "A headline" })
    ).toBeUndefined();
  });

  it("passes through API media", () => {
    const media = resolveFeedMedia({
      type: "news",
      title: "A headline",
      media: { type: "image", url: "https://cdn.example/photo.jpg", alt: "A headline" },
    });
    expect(media?.url).toBe("https://cdn.example/photo.jpg");
  });
});

describe("canUseNextImage", () => {
  it("allows configured remote hosts and same-origin raster", () => {
    expect(canUseNextImage("https://media.api-sports.io/football/leagues/39.png")).toBe(true);
    expect(canUseNextImage("https://flagcdn.com/gb.svg")).toBe(false);
    expect(canUseNextImage("/images/banter-feed-hero.png")).toBe(true);
  });

  it("keeps Giphy, Tenor, GIF and unknown CDNs on img", () => {
    expect(canUseNextImage("https://media.giphy.com/media/abc/giphy.gif")).toBe(false);
    expect(canUseNextImage("https://cdn.example/photo.jpg")).toBe(false);
    expect(canUseNextImage("/reactions/locked-in.svg")).toBe(false);
  });
});
