import type { NextConfig } from "next";

const backendUrl = process.env.API_PROXY_URL ?? "http://localhost:5000";

/** AdSense + SODAR (ad traffic quality) origins required by CSP. */
const ADSENSE_CONNECT_SRC = [
  "https://*.adtrafficquality.google",
  "https://googleads.g.doubleclick.net",
  "https://tpc.googlesyndication.com",
  "https://www.googleadservices.com",
];

const ADSENSE_FRAME_SRC = [
  "https://googleads.g.doubleclick.net",
  "https://tpc.googlesyndication.com",
  "https://*.adtrafficquality.google",
  "https://www.google.com",
];

const ADSENSE_SCRIPT_SRC = [
  "https://*.adtrafficquality.google",
  "https://www.googletagservices.com",
];

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
        hostname: "images.unsplash.com",
        pathname: "/**",
      },
    ],
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
              "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://challenges.cloudflare.com https://pagead2.googlesyndication.com https://*.googlesyndication.com https://*.googleadservices.com https://*.g.doubleclick.net https://*.google.com " +
                ADSENSE_SCRIPT_SRC.join(" "),
              "style-src 'self' 'unsafe-inline'",
              "img-src 'self' data: blob: https:",
              "font-src 'self' data:",
              `connect-src 'self' https://*.supabase.co wss://*.supabase.co https://challenges.cloudflare.com https://pagead2.googlesyndication.com https://*.googlesyndication.com https://*.g.doubleclick.net https://*.google.com ${ADSENSE_CONNECT_SRC.join(" ")} ${apiConnectOrigins().join(" ")}`.trim(),
              "frame-src https://challenges.cloudflare.com https://*.googlesyndication.com https://*.g.doubleclick.net https://*.google.com " +
                ADSENSE_FRAME_SRC.join(" "),
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
