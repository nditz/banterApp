import { defineConfig } from "@playwright/test";

const baseURL = process.env.PLAYWRIGHT_BASE_URL ?? "http://localhost:3000";

const viewports = [
  { name: "mobile-320", width: 320, height: 700 },
  { name: "mobile-375", width: 375, height: 812 },
  { name: "mobile-390", width: 390, height: 844 },
  { name: "tablet-768", width: 768, height: 1024 },
  { name: "tablet-1024", width: 1024, height: 768 },
  { name: "desktop-1440", width: 1440, height: 900 },
] as const;

export default defineConfig({
  testDir: "./e2e",
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: process.env.CI ? "github" : "list",
  use: {
    baseURL,
    trace: "on-first-retry",
  },
  webServer: process.env.PLAYWRIGHT_SKIP_WEBSERVER
    ? undefined
    : {
        command: process.env.CI ? "npm run start" : "npm run dev",
        url: baseURL,
        reuseExistingServer: !process.env.CI,
        timeout: 180_000,
      },
  projects: [
    { name: "admin-setup", testMatch: /admin\.setup\.ts/ },
    ...viewports.map((viewport) => ({
      name: viewport.name,
      testMatch: /responsive\/public-pages\.spec\.ts/,
      use: { viewport: { width: viewport.width, height: viewport.height } },
    })),
    {
      name: "admin-mobile",
      testMatch: /responsive\/admin-pages\.spec\.ts/,
      use: {
        viewport: { width: 375, height: 812 },
        storageState: "e2e/.auth/admin.json",
      },
      dependencies: ["admin-setup"],
    },
  ],
});
