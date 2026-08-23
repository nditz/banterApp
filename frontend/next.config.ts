import type { NextConfig } from "next";
import {
  ADSENSE_CSP_CONNECT,
  ADSENSE_CSP_FRAME,
  ADSENSE_CSP_SCRIPT,
  TURNSTILE_CSP,
} from "./src/lib/ads";

const backendUrl = process.env.API_PROXY_URL ?? "http://localhost:5000";

function apiConnectOrigins(): string[] {
  const origins = new Set<string>();

  for (const raw of [process.env.NEXT_PUBLIC_API_URL, backendUrl]) {
    if (!raw?.trim()) continue;
    try {
      origins.add(new URL(raw).origin);
    } catch {
      origins.add(raw.trim());
    }
  }

  return [...origins];
}

const nextConfig: NextConfig = {
  images: {
    remotePatterns: [
      {
        protocol: "https",
        hostname: "flagcdn.com",
        pathname: "/**",
      },
      {
        protocol: "https",
        hostname: "api.dicebear.com",
        pathname: "/**",
      },
      {
        protocol: "https",
        hostname: "media.api-sports.io",
        pathname: "/**",
      },
      {
        protocol: "https",
        hostname: "media-3.api-sports.io",
        pathname: "/**",
      },
    ],
  },
  async redirects() {
    return [
      { source: "/brackets", destination: "/matchweek", permanent: true },
      { source: "/bonuses", destination: "/awards", permanent: true },
      { source: "/predictions", destination: "/awards", permanent: true },
      { source: "/predictions/make", destination: "/awards", permanent: true },
      { source: "/predictions/best-player", destination: "/awards", permanent: true },
      { source: "/predictions/top-scorer", destination: "/awards", permanent: true },
      { source: "/predictions/top-assists", destination: "/awards", permanent: true },
    ];
  },
  async rewrites() {
    return [
      {
        source: "/api-backend/:path*",
        destination: `${backendUrl}/:path*`,
      },
    ];
  },
  async headers() {
    return [
      {
        source: "/(.*)",
        headers: [
          { key: "X-Frame-Options", value: "DENY" },
          { key: "X-Content-Type-Options", value: "nosniff" },
          { key: "Referrer-Policy", value: "strict-origin-when-cross-origin" },
          {
            key: "Permissions-Policy",
            value: "camera=(), microphone=(), geolocation=()",
          },
          ...(process.env.NODE_ENV === "production"
            ? [{ key: "Strict-Transport-Security", value: "max-age=31536000; includeSubDomains" }]
            : []),
          {
            key: "Content-Security-Policy",
            value: [
              "default-src 'self'",
              "script-src 'self' 'unsafe-inline' 'unsafe-eval' " +
                [...TURNSTILE_CSP, "https://pagead2.googlesyndication.com", "https://*.googlesyndication.com", "https://*.googleadservices.com", "https://*.g.doubleclick.net", "https://*.google.com", ...ADSENSE_CSP_SCRIPT].join(" "),
              "style-src 'self' 'unsafe-inline'",
              "img-src 'self' data: blob: https:",
              "font-src 'self' data: https://fonts.gstatic.com",
              `connect-src 'self' ${TURNSTILE_CSP.join(" ")} https://*.supabase.co wss://*.supabase.co https://pagead2.googlesyndication.com https://*.googlesyndication.com https://*.g.doubleclick.net https://*.google.com ${ADSENSE_CSP_CONNECT.join(" ")} ${apiConnectOrigins().join(" ")}`.trim(),
              "frame-src " +
                [...TURNSTILE_CSP, "https://*.googlesyndication.com", "https://*.g.doubleclick.net", "https://*.google.com", ...ADSENSE_CSP_FRAME].join(" "),
              "object-src 'none'",
              "base-uri 'self'",
              "form-action 'self'",
            ].join("; "),
          },
        ],
      },
    ];
  },
};

export default nextConfig;
