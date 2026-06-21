import { test, expect } from "@playwright/test";
import {
  assertNoHorizontalOverflow,
  assertPrimaryNavVisible,
  publicRoutes,
} from "./helpers";

for (const route of publicRoutes) {
  test(`${route.name} has no horizontal overflow`, async ({ page, viewport }) => {
    const errors: string[] = [];
    page.on("console", (msg) => {
      if (msg.type() === "error") errors.push(msg.text());
    });

    const response = await page.goto(route.path, { waitUntil: "domcontentloaded" });
    expect(response?.status(), `${route.path} should load`).toBeLessThan(500);

    await page.waitForTimeout(500);
    await assertNoHorizontalOverflow(page);

    if (!route.path.startsWith("/auth")) {
      await assertPrimaryNavVisible(page, viewport?.width ?? 375);
    }

    const criticalErrors = errors.filter(
      (e) => !e.includes("favicon") && !e.includes("Turnstile")
    );
    expect(criticalErrors, "No critical console errors").toEqual([]);
  });
}

test("login form is usable on mobile", async ({ page }) => {
  await page.setViewportSize({ width: 320, height: 700 });
  await page.goto("/auth/login");
  await expect(page.getByLabel("Email")).toBeVisible();
  await expect(page.getByLabel("Password")).toBeVisible();
  await expect(page.getByRole("button", { name: /^log in$/i })).toBeVisible();
  await assertNoHorizontalOverflow(page);
});
