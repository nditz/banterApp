import { test, expect } from "@playwright/test";
import { adminRoutes, assertNoHorizontalOverflow } from "./helpers";

test.describe("admin responsive (authenticated)", () => {
  for (const route of adminRoutes) {
    test(`${route} has no horizontal overflow`, async ({ page }) => {
      const response = await page.goto(route, { waitUntil: "domcontentloaded" });
      expect(response?.status(), `${route} should load`).toBeLessThan(500);
      await page.waitForTimeout(500);
      await assertNoHorizontalOverflow(page);
    });
  }

  test("mobile admin menu button is visible", async ({ page }) => {
    await page.goto("/admin");
    await expect(page.getByRole("button", { name: /open admin menu/i })).toBeVisible();
  });
});

test.describe("admin pages redirect when unauthenticated", () => {
  test.use({ storageState: { cookies: [], origins: [] } });

  test("admin jobs is not accessible without auth", async ({ page }) => {
    await page.goto("/admin/jobs");
    await page.waitForLoadState("networkidle");
    expect(page.url()).not.toMatch(/\/admin\/jobs$/);
  });
});
