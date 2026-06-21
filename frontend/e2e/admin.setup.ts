import { test as setup, expect } from "@playwright/test";
import fs from "fs";
import path from "path";

const authFile = path.join(__dirname, "../.auth/admin.json");

setup("authenticate admin", async ({ page }) => {
  const email = process.env.E2E_ADMIN_EMAIL;
  const password = process.env.E2E_ADMIN_PASSWORD;

  if (!email || !password) {
    setup.skip(true, "E2E_ADMIN_EMAIL and E2E_ADMIN_PASSWORD not set");
    return;
  }

  await page.goto("/auth/login?redirect=/admin");
  await page.getByLabel("Email").fill(email);
  await page.getByLabel("Password").fill(password);
  await page.getByRole("button", { name: /^log in$/i }).click();

  await page.waitForURL(/\/admin/, { timeout: 30_000 });
  await expect(page.getByRole("heading", { name: "Admin Console" })).toBeVisible();

  fs.mkdirSync(path.dirname(authFile), { recursive: true });
  await page.context().storageState({ path: authFile });
});
