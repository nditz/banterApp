import type { Metadata } from "next";
import { PunditsDirectory } from "@/components/pundits/PunditsDirectory";

export const metadata: Metadata = {
  title: "Pundits",
  description:
    "Follow sourced football pundits and compare your Premier League picks with theirs on Ball Takes.",
  alternates: { canonical: "/pundits" },
};

export default function PunditsPage() {
  return <PunditsDirectory />;
}
