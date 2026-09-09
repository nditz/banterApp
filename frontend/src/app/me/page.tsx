import type { Metadata } from "next";
import { MeHub } from "@/components/me/MeHub";

export const metadata: Metadata = {
  title: "Me",
  description: "Your Ball Takes account, receipts, season calls and rankings.",
  robots: { index: false, follow: false },
};

export default function MePage() {
  return <MeHub />;
}
