import type { MetadataRoute } from "next";
import { SITE_URL } from "@/lib/seo.config";
import { SITEMAP_ROUTES } from "@/lib/sitemap-routes";

export default function sitemap(): MetadataRoute.Sitemap {
  const lastModified = new Date();

  return SITEMAP_ROUTES.map((route) => ({
    url: `${SITE_URL}${route.path}`,
    lastModified,
    changeFrequency: route.changeFrequency,
    priority: route.priority,
  }));
}
