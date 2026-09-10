import type { Metadata } from "next";
import { LeagueTable } from "@/components/home/LeagueTable";
import { PageContainer, SectionHeader } from "@/components/ui/section-header";

export const metadata: Metadata = {
  title: "Premier League table",
  description: "Live Premier League standings ranked by points, goal difference, then goals scored.",
  alternates: { canonical: "/table" },
};

export default function TablePage() {
  return (
    <PageContainer width="narrow">
      <SectionHeader
        as="h1"
        eyebrow="2026/27 standings"
        title="Premier League table"
        description="Ranked the Premier League way: points, then goal difference, then goals scored. Use it when you call the title, top four, and relegation."
      />
      <LeagueTable />
    </PageContainer>
  );
}
