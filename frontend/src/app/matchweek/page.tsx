import type { Metadata } from "next";
import { MatchweekBoard } from "@/components/home/MatchweekBoard";
import { PageContainer, SectionHeader } from "@/components/ui/section-header";

export const metadata: Metadata = {
  title: "Matchweek picks",
  description:
    "Predict every Premier League fixture this matchweek. Result, exact score, or double chance — lock in before kickoff.",
  alternates: { canonical: "/matchweek" },
};

export default function MatchweekPage() {
  return (
    <PageContainer width="narrow">
      <SectionHeader
        as="h1"
        eyebrow="Premier League 2026/27"
        title="Matchweek picks"
        description="Lock every fixture this week. Result +3, exact score +7, double chance +2. Nail the lot and take the perfect matchweek bonus."
      />
      <MatchweekBoard />
    </PageContainer>
  );
}
