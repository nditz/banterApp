import { createRequire } from "node:module";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const require = createRequire("c:/banterapp/frontend/package.json");
const { chromium } = require("playwright");

const outDir = path.dirname(fileURLToPath(import.meta.url));
fs.mkdirSync(outDir, { recursive: true });

const routes = [
  { path: "/", name: "home" },
  { path: "/matchweek", name: "matchweek" },
  { path: "/studio", name: "studio" },
  { path: "/pundits", name: "pundits" },
  { path: "/leagues", name: "leagues" },
  { path: "/awards", name: "season-calls" },
  { path: "/table", name: "table" },
];

const viewports = [
  { name: "375", width: 375, height: 812 },
  { name: "1440", width: 1440, height: 900 },
];

const browser = await chromium.launch();
const notes = [];

for (const vp of viewports) {
  const context = await browser.newContext({
    viewport: { width: vp.width, height: vp.height },
    userAgent:
      vp.width < 768
        ? "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1"
        : undefined,
  });
  const page = await context.newPage();
  for (const route of routes) {
    const url = `https://balltakes.com${route.path}`;
    try {
      const response = await page.goto(url, {
        waitUntil: "domcontentloaded",
        timeout: 45_000,
      });
      await page.waitForTimeout(1800);
      const termsVisible = await page
        .getByRole("dialog", { name: /terms of use/i })
        .isVisible()
        .catch(() => false);
      if (route.name === "home" && termsVisible) {
        await page.screenshot({
          path: path.join(outDir, `${route.name}-terms-gate-${vp.name}.png`),
          fullPage: false,
        });
      }
      await page.addStyleTag({
        content:
          '[role="dialog"], [data-slot="dialog-overlay"], [data-slot="dialog-content"] { display: none !important; }',
      });
      await page.waitForTimeout(1200);
      const file = `${route.name}-${vp.name}.png`;
      await page.screenshot({ path: path.join(outDir, file), fullPage: false });
      notes.push({
        file,
        status: response?.status() ?? null,
        title: await page.title(),
        termsGate: termsVisible,
      });
    } catch (error) {
      notes.push({
        file: `${route.name}-${vp.name}.png`,
        error: String(error),
      });
    }
  }
  await context.close();
}

await browser.close();
fs.writeFileSync(path.join(outDir, "capture-log.json"), JSON.stringify(notes, null, 2));
console.log(JSON.stringify(notes, null, 2));
