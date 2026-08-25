import { expect, type Page } from "@playwright/test";

export async function assertNoHorizontalOverflow(page: Page) {
  const overflow = await page.evaluate(() => {
    const doc = document.documentElement;
    return doc.scrollWidth > window.innerWidth + 1;
  });
  expect(overflow, "Page should not have horizontal overflow").toBe(false);
}

export async function assertPrimaryNavVisible(page: Page, viewportWidth: number) {
  if (viewportWidth >= 1024) {
    await expect(page.getByRole("navigation", { name: "Main navigation" })).toBeVisible();
    return;
  }
  const bottomNav = page.getByRole("navigation", { name: "Mobile navigation" });
  const mobileMenuButton = page.getByRole("button", { name: /open menu/i });
  const hasBottomNav = await bottomNav.isVisible().catch(() => false);
  const hasMenuButton = await mobileMenuButton.isVisible().catch(() => false);
  expect(hasBottomNav || hasMenuButton, "Mobile navigation should be visible").toBe(true);
}

export async function collectConsoleErrors(page: Page) {
  const errors: string[] = [];
  page.on("console", (msg) => {
    if (msg.type() === "error") {
      errors.push(msg.text());
    }
  });
  return errors;
}

export const publicRoutes = [
  { path: "/", name: "home" },
  { path: "/#predictions", name: "predictions section" },
  { path: "/#banter-feed", name: "banter feed section" },
  { path: "/#rankings", name: "rankings section" },
  { path: "/predictions/history", name: "prediction history" },
  { path: "/studio", name: "studio" },
  { path: "/matchweek", name: "matchweek" },
  { path: "/awards", name: "awards" },
  { path: "/auth/login", name: "login" },
] as const;

export const adminRoutes = [
  "/admin",
  "/admin/jobs",
  "/admin/errors",
  "/admin/review",
  "/admin/stats",
  "/admin/health",
  "/admin/users",
  "/admin/audit",
] as const;
