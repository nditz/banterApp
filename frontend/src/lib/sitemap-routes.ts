import type { MetadataRoute } from "next";

export const SITEMAP_ROUTES: Array<{
  path: string;
  changeFrequency: MetadataRoute.Sitemap[number]["changeFrequency"];
  priority: number;
}> = [
  { path: "/", changeFrequency: "daily", priority: 1 },
  { path: "/matchweek", changeFrequency: "daily", priority: 0.9 },
  { path: "/studio", changeFrequency: "weekly", priority: 0.9 },
  { path: "/awards", changeFrequency: "weekly", priority: 0.8 },
  { path: "/leagues", changeFrequency: "weekly", priority: 0.8 },
  { path: "/table", changeFrequency: "daily", priority: 0.6 },
  { path: "/rules", changeFrequency: "monthly", priority: 0.6 },
  { path: "/terms", changeFrequency: "yearly", priority: 0.3 },
  { path: "/privacy", changeFrequency: "yearly", priority: 0.3 },
];
