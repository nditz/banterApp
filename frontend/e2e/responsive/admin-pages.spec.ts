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

  test("users page exposes search and never renders a delete action", async ({ page }) => {
    await page.goto("/admin/users", { waitUntil: "domcontentloaded" });

    await expect(page.getByRole("heading", { name: "Users" })).toBeVisible();
    await expect(page.getByPlaceholder(/search by email or display name/i)).toBeVisible();

    // Account deletion is deliberately out of scope until the data-lifecycle workflow
    // exists, so the UI must not offer it.
    await expect(page.getByRole("button", { name: /delete account/i })).toHaveCount(0);
  });

  test("audit page is read-only and filterable", async ({ page }) => {
    await page.goto("/admin/audit", { waitUntil: "domcontentloaded" });

    await expect(page.getByRole("heading", { name: "Audit Log" })).toBeVisible();
    await expect(page.getByLabel("Filter by action")).toBeVisible();
    await expect(page.getByLabel("From date")).toBeVisible();
    await expect(page.getByRole("button", { name: /^(delete|edit|remove)/i })).toHaveCount(0);
  });
});

test.describe("admin pages redirect when unauthenticated", () => {
  test.use({ storageState: { cookies: [], origins: [] } });

  test("admin jobs is not accessible without auth", async ({ page }) => {
    await page.goto("/admin/jobs");
    await page.waitForLoadState("networkidle");
    expect(page.url()).not.toMatch(/\/admin\/jobs$/);
  });

  test("admin users is not accessible without auth", async ({ page }) => {
    await page.goto("/admin/users");
    await page.waitForLoadState("networkidle");
    expect(page.url()).not.toMatch(/\/admin\/users$/);
  });

  test("admin audit is not accessible without auth", async ({ page }) => {
    await page.goto("/admin/audit");
    await page.waitForLoadState("networkidle");
    expect(page.url()).not.toMatch(/\/admin\/audit$/);
  });
});
