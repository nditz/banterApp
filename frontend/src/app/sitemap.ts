import type { MetadataRoute } from "next";
import { SITE_URL } from "@/lib/seo.config";

export default function sitemap(): MetadataRoute.Sitemap {
  const lastModified = new Date();

  const routes: Array<{
    path: string;
    changeFrequency: MetadataRoute.Sitemap[number]["changeFrequency"];
    priority: number;
  }> = [
    { path: "/", changeFrequency: "daily", priority: 1 },
    { path: "/matchweek", changeFrequency: "daily", priority: 0.9 },
    { path: "/table", changeFrequency: "daily", priority: 0.8 },
    { path: "/awards", changeFrequency: "weekly", priority: 0.8 },
    { path: "/leagues", changeFrequency: "weekly", priority: 0.8 },
    { path: "/studio", changeFrequency: "weekly", priority: 0.5 },
    { path: "/rules", changeFrequency: "monthly", priority: 0.6 },
    { path: "/terms", changeFrequency: "yearly", priority: 0.3 },
    { path: "/privacy", changeFrequency: "yearly", priority: 0.3 },
  ];

  return routes.map((route) => ({
    url: `${SITE_URL}${route.path}`,
    lastModified,
    changeFrequency: route.changeFrequency,
    priority: route.priority,
  }));
}
