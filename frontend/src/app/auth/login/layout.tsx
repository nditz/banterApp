import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "Log In",
  robots: { index: false, follow: true },
  alternates: { canonical: "/auth/login" },
};

export default function LoginLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return children;
}
