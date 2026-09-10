import { describe, expect, it } from "vitest";
import { stripHtml } from "./strip-html";

describe("stripHtml", () => {
  it("returns empty string for nullish input", () => {
    expect(stripHtml(null)).toBe("");
    expect(stripHtml(undefined)).toBe("");
    expect(stripHtml("")).toBe("");
  });

  it("strips tags and collapses whitespace", () => {
    expect(stripHtml("<p><b>Salah</b> scores</p>")).toBe("Salah scores");
    expect(stripHtml("<ul><li>One</li><li>Two</li></ul>")).toBe("One Two");
  });

  it("decodes common entities", () => {
    expect(stripHtml("City &amp; Arsenal")).toBe("City & Arsenal");
    expect(stripHtml("It&#39;s a take")).toBe("It's a take");
    expect(stripHtml("A&nbsp;B")).toBe("A B");
  });

  it("does not leave visible markup from news summaries", () => {
    const leaked = "<p><b>Premier League</b></p></li></ul>";
    const plain = stripHtml(leaked);
    expect(plain).toBe("Premier League");
    expect(plain).not.toMatch(/<\/?[a-z]/i);
  });
});
