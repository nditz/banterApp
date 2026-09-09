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
    await expect(nav.getByRole("link", { name: "Banter" })).toBeVisible();
    await expect(nav.getByRole("link", { name: "Studio" })).toBeVisible();
    await expect(nav.getByRole("link", { name: "Leagues" })).toBeVisible();
    await expect(nav.getByRole("link", { name: "Season Calls" })).toBeVisible();
    await expect(nav.getByRole("link", { name: "Table" })).toHaveCount(0);
    await expect(nav.getByRole("button", { name: "More" })).toBeVisible();
    await nav.getByRole("button", { name: "More" }).click({ force: true });
    const menu = page.locator('[role="menu"]');
    await expect(menu).toBeAttached();
    await expect(menu.getByText("Pundits", { exact: true })).toBeAttached();
    await expect(menu.getByText("Table", { exact: true })).toBeAttached();
    await expect(menu.getByText("History", { exact: true })).toBeAttached();
    await expect(menu.getByText("Rules", { exact: true })).toBeAttached();
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
    await expect(bottom.getByRole("link", { name: "Table" })).toHaveCount(0);

    await page.locator('button[aria-label="Open menu"], button[aria-label="Close menu"]').first().click({ force: true });
    const more = page.locator("#app-mobile-menu");
    await expect(more).toBeAttached();
    await expect(more.getByText("Season Calls", { exact: true })).toBeAttached();
    await expect(more.getByText("Table", { exact: true })).toBeAttached();
  });

  test("welcome, awards, studio, me, studio stays linked", async ({ page }, testInfo) => {
    test.skip(
      !["phase2-mobile-375", "phase2-desktop-1440"].includes(testInfo.project.name),
      "phase 2 viewports only"
    );

    await page.goto("/", { waitUntil: "domcontentloaded" });
    await expect(page.getByText("Start here")).toBeVisible();
    await expect(page.getByRole("link", { name: "Lock a pick" })).toBeVisible();
    await expect(page.getByRole("link", { name: "Watch the feed" })).toBeVisible();
    await expect(page.getByText("Quick tour")).toHaveCount(0);

    await page.goto("/awards", { waitUntil: "domcontentloaded" });
    await expect(page.locator("h1").filter({ hasText: "Season Calls" })).toBeAttached();

    await page.goto("/studio", { waitUntil: "domcontentloaded" });
    await expect(page.getByText("Content Studio").first()).toBeAttached();
    const punditTab = page.locator('[aria-label="Studio sections"] [role="tab"]').nth(2);
    await punditTab.click({ force: true });
    await expect(page.getByText(/sourced pundit predictions/i)).toBeAttached();
    await expect(page.getByText(/fictional pundit/i)).toHaveCount(0);

    await page.goto("/me", { waitUntil: "domcontentloaded" });
    await expect(page.locator("h1").filter({ hasText: "Me" })).toBeAttached();
    await expect(page.getByText("Season Calls").first()).toBeAttached();
  });
});
