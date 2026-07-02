import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "Prediction History",
  robots: { index: false, follow: true },
  alternates: { canonical: "/predictions/history" },
};

export default function PredictionHistoryLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return children;
}
