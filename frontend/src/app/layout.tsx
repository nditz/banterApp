import type { Metadata, Viewport } from "next";
import { Barlow, Barlow_Condensed, Geist_Mono } from "next/font/google";
import Script from "next/script";
import { AppShell } from "@/components/layout/AppShell";
import { ClientErrorReporter } from "@/components/ClientErrorReporter";
import { QueryProvider } from "@/components/providers/QueryProvider";
import { ThemeProvider } from "@/components/providers/ThemeProvider";
import { OrganizationJsonLd, WebsiteJsonLd } from "@/components/JsonLd";
import { BRAND } from "@/lib/brand";
import { BASE_METADATA } from "@/lib/seo.config";
import { ADSENSE_CLIENT, ADSENSE_ENABLED } from "@/lib/ads";
import "./globals.css";

const THEME_INIT_SCRIPT = `(function(){try{var t=localStorage.getItem("ball-takes-theme");var d=t==="dark"||(t==="system"&&window.matchMedia("(prefers-color-scheme: dark)").matches);document.documentElement.classList.toggle("dark",!!d);}catch(e){}})();`;

const barlow = Barlow({
  variable: "--font-barlow",
  subsets: ["latin"],
  weight: ["300", "400", "500", "600", "700"],
});

const barlowCondensed = Barlow_Condensed({
  variable: "--font-barlow-condensed",
  subsets: ["latin"],
  weight: ["400", "500", "600", "700"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  ...BASE_METADATA,
  manifest: "/brand/site.webmanifest",
  icons: {
    icon: [
      { url: "/brand/icon-16x16.png", sizes: "16x16", type: "image/png" },
      { url: "/brand/icon-32x32.png", sizes: "32x32", type: "image/png" },
      { url: "/brand/icon-48x48.png", sizes: "48x48", type: "image/png" },
      { url: "/brand/icon-192x192.png", sizes: "192x192", type: "image/png" },
      { url: "/brand/icon-512x512.png", sizes: "512x512", type: "image/png" },
    ],
    apple: [
      { url: "/brand/icon-180x180.png", sizes: "180x180", type: "image/png" },
    ],
    shortcut: "/brand/favicon.ico",
  },
  appleWebApp: {
    capable: true,
    title: BRAND.name,
    statusBarStyle: "black-translucent",
  },
  ...(ADSENSE_ENABLED
    ? { other: { "google-adsense-account": ADSENSE_CLIENT } }
    : {}),
};

export const viewport: Viewport = {
  themeColor: [
    { media: "(prefers-color-scheme: light)", color: "#f4f2ec" },
    { media: "(prefers-color-scheme: dark)", color: "#0c0d10" },
  ],
  width: "device-width",
  initialScale: 1,
  viewportFit: "cover",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html
      lang="en"
      data-scroll-behavior="smooth"
      className={`${barlow.variable} ${barlowCondensed.variable} ${geistMono.variable} h-full antialiased`}
      suppressHydrationWarning
    >
      <body className="min-h-full flex flex-col">
        <Script id="theme-init" strategy="beforeInteractive">
          {THEME_INIT_SCRIPT}
        </Script>
        <OrganizationJsonLd />
        <WebsiteJsonLd />
        <ThemeProvider>
          <QueryProvider>
            <ClientErrorReporter />
            <AppShell>{children}</AppShell>
          </QueryProvider>
        </ThemeProvider>
      </body>
    </html>
  );
}
