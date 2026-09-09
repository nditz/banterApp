import { describe, expect, it } from "vitest";
import { SITEMAP_ROUTES } from "./sitemap-routes";

describe("sitemap routes", () => {
  it("gives Studio the same priority as matchweek", () => {
    const studio = SITEMAP_ROUTES.find((r) => r.path === "/studio");
    const matchweek = SITEMAP_ROUTES.find((r) => r.path === "/matchweek");
    expect(studio?.priority).toBe(0.9);
    expect(matchweek?.priority).toBe(0.9);
    expect(studio?.priority).toBeGreaterThan(
      SITEMAP_ROUTES.find((r) => r.path === "/table")?.priority ?? 0
    );
  });

  it("lists the pundit browse route", () => {
    const pundits = SITEMAP_ROUTES.find((r) => r.path === "/pundits");
    expect(pundits?.priority).toBe(0.8);
  });
});
