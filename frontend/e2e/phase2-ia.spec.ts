import { expect, test } from "@playwright/test";

test.describe("Phase 2 product IA", () => {
  test("desktop primary nav is Predict / Banter / Studio / Leagues / Season Calls", async ({
    page,
  }, testInfo) => {
    test.skip(testInfo.project.name !== "phase2-desktop-1440", "desktop nav only");
    await page.goto("/", { waitUntil: "domcontentloaded" });
    const nav = page.getByRole("navigation", { name: "Main navigation" });
    await expect(nav).toBeVisible();
    await expect(nav.getByRole("link", { name: "Predict" })).toBeVisible();
    await expect(nav.getByRole("link", { name: "Banter" })).toHaveAttribute("href", "/banter");
    await expect(nav.getByRole("link", { name: "Studio" })).toBeVisible();
    await expect(nav.getByRole("link", { name: "Leagues" })).toBeVisible();
    await expect(nav.getByRole("link", { name: "Season Calls" })).toBeVisible();
    await expect(nav.getByRole("link", { name: "Table" })).toHaveCount(0);
    await expect(nav.getByRole("button", { name: "More" })).toBeVisible();
  });

  test("mobile bottom nav is Predict / Banter / Studio / Leagues / Me", async ({
    page,
  }, testInfo) => {
    test.skip(
      testInfo.project.name !== "phase2-mobile-375",
      "mobile bottom nav only"
    );
    await page.goto("/", { waitUntil: "domcontentloaded" });
    const bottom = page.getByRole("navigation", { name: "Mobile navigation" });
    await expect(bottom).toBeVisible();
    const labels = await bottom.locator("a").allTextContents();
    expect(labels.map((t) => t.trim())).toEqual([
      "Predict",
      "Banter",
      "Studio",
      "Leagues",
      "Me",
    ]);
    await expect(bottom.getByRole("link", { name: "Banter" })).toHaveAttribute("href", "/banter");
    await expect(bottom.getByRole("link", { name: "Table" })).toHaveCount(0);
    await expect(page.getByRole("button", { name: "Open menu" })).toBeVisible();
  });

  test("welcome, awards, studio, me, studio stays linked", async ({ page }, testInfo) => {
    test.skip(
      !["phase2-mobile-375", "phase2-desktop-1440"].includes(testInfo.project.name),
      "phase 2 viewports only"
    );

    await page.goto("/", { waitUntil: "domcontentloaded" });
    await expect(page.getByText("Start here")).toBeVisible();
    await expect(page.getByRole("link", { name: "Make a pick" })).toBeVisible();
    await expect(page.getByRole("link", { name: "Open Studio" }).first()).toBeVisible();
    await expect(page.getByRole("link", { name: "Watch the feed" })).toBeVisible();
    await expect(page.getByText("Quick tour")).toHaveCount(0);

    await page.goto("/awards", { waitUntil: "domcontentloaded" });
    await expect(page.locator("h1").filter({ hasText: "Season Calls" })).toBeAttached();

    await page.goto("/studio", { waitUntil: "domcontentloaded" });
    await expect(page.getByText("Content Studio").first()).toBeAttached();
    await expect(page.getByRole("tab", { name: "Stories" })).toBeAttached();
    await expect(page.getByRole("tab", { name: "vs Pundits" })).toBeAttached();
    await expect(page.getByText(/video content — coming soon/i)).toHaveCount(0);
    await expect(page.getByText(/fictional pundit/i)).toHaveCount(0);

    await page.goto("/me", { waitUntil: "domcontentloaded" });
    await expect(page.locator("h1").filter({ hasText: "Me" })).toBeAttached();
    await expect(page.getByText("Season Calls").first()).toBeAttached();
  });
});
