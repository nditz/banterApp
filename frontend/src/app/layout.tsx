import type { Metadata, Viewport } from "next";
import { Barlow, Barlow_Condensed, Geist_Mono } from "next/font/google";
import { AppShell } from "@/components/layout/AppShell";
import { ClientErrorReporter } from "@/components/ClientErrorReporter";
import { QueryProvider } from "@/components/providers/QueryProvider";
import { BRAND } from "@/lib/brand";
import "./globals.css";

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
  metadataBase: new URL(`https://${BRAND.domain}`),
  title: {
    default: BRAND.name,
    template: `%s | ${BRAND.name}`,
  },
  description: BRAND.description,
  applicationName: BRAND.name,
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
  openGraph: {
    title: BRAND.name,
    description: BRAND.description,
    siteName: BRAND.name,
    images: [{ url: BRAND.logoDefault }],
  },
};

export const viewport: Viewport = {
  themeColor: "#0891b2",
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
      className={`${barlow.variable} ${barlowCondensed.variable} ${geistMono.variable} h-full antialiased`}
      suppressHydrationWarning
    >
      <body className="min-h-full flex flex-col">
        <QueryProvider>
          <ClientErrorReporter />
          <AppShell>{children}</AppShell>
        </QueryProvider>
      </body>
    </html>
  );
}
