import { describe, expect, it } from "vitest";
import {
  DESKTOP_OVERFLOW_NAV,
  DESKTOP_PRIMARY_NAV,
  HOME_QUICK_NAV,
  MOBILE_BOTTOM_NAV,
  MOBILE_OVERFLOW_NAV,
  isNavHrefActive,
} from "./navigation";

function labels(links: readonly { label: string }[]) {
  return links.map((l) => l.label);
}

describe("product navigation IA", () => {
  it("keeps Studio on desktop primary and mobile bottom", () => {
    expect(labels(DESKTOP_PRIMARY_NAV)).toContain("Studio");
    expect(labels(MOBILE_BOTTOM_NAV)).toContain("Studio");
    expect(MOBILE_BOTTOM_NAV[2]?.label).toBe("Studio");
  });

  it("uses Predict / Banter / Studio / Leagues as the shared spine", () => {
    expect(labels(DESKTOP_PRIMARY_NAV)).toEqual([
      "Predict",
      "Banter",
      "Studio",
      "Leagues",
      "Season Calls",
    ]);
    expect(labels(MOBILE_BOTTOM_NAV)).toEqual([
      "Predict",
      "Banter",
      "Studio",
      "Leagues",
      "Me",
    ]);
  });

  it("exposes Season Calls on desktop and in mobile overflow", () => {
    expect(labels(DESKTOP_PRIMARY_NAV)).toContain("Season Calls");
    expect(labels(MOBILE_OVERFLOW_NAV)).toContain("Season Calls");
    expect(DESKTOP_PRIMARY_NAV.find((l) => l.label === "Season Calls")?.href).toBe(
      "/awards"
    );
  });

  it("exposes Pundits in overflow, not the primary spine", () => {
    expect(labels(DESKTOP_OVERFLOW_NAV)).toContain("Pundits");
    expect(labels(MOBILE_OVERFLOW_NAV)).toContain("Pundits");
    expect(labels(DESKTOP_PRIMARY_NAV)).not.toContain("Pundits");
    expect(labels(MOBILE_BOTTOM_NAV)).not.toContain("Pundits");
    expect(DESKTOP_OVERFLOW_NAV.find((l) => l.label === "Pundits")?.href).toBe("/pundits");
  });

  it("exposes Receipts in overflow at the history route", () => {
    expect(labels(DESKTOP_OVERFLOW_NAV)).toContain("Receipts");
    expect(labels(MOBILE_OVERFLOW_NAV)).toContain("Receipts");
    expect(DESKTOP_OVERFLOW_NAV.find((l) => l.label === "Receipts")?.href).toBe(
      "/predictions/history"
    );
  });


  it("demotes Table from mobile primary", () => {
    expect(labels(MOBILE_BOTTOM_NAV)).not.toContain("Table");
    expect(labels(DESKTOP_OVERFLOW_NAV)).toContain("Table");
    expect(labels(MOBILE_OVERFLOW_NAV)).toContain("Table");
  });

  it("keeps home quick nav aligned and without Table", () => {
    expect(labels(HOME_QUICK_NAV)).toEqual([
      "Predict",
      "Banter",
      "Studio",
      "Leagues",
      "Season Calls",
    ]);
  });
});

describe("isNavHrefActive", () => {
  it("treats the homepage as the Banter destination", () => {
    expect(isNavHrefActive("/#banter-feed", "/")).toBe(true);
    expect(isNavHrefActive("/#banter-feed", "/matchweek")).toBe(false);
    expect(isNavHrefActive("/#banter-feed", "/studio")).toBe(false);
  });

  it("matches Predict, Studio, Season Calls, and Me by path", () => {
    expect(isNavHrefActive("/matchweek", "/matchweek")).toBe(true);
    expect(isNavHrefActive("/studio", "/studio")).toBe(true);
    expect(isNavHrefActive("/awards", "/awards")).toBe(true);
    expect(isNavHrefActive("/me", "/me")).toBe(true);
    expect(isNavHrefActive("/matchweek", "/")).toBe(false);
    expect(isNavHrefActive("/studio", "/me")).toBe(false);
  });
});
