import type { Metadata } from "next";
import { BanterFeedPanel } from "@/components/home/BanterFeedPanel";
import { PageContainer, SectionHeader } from "@/components/ui/section-header";

export const metadata: Metadata = {
  title: "Banter",
  description:
    "The Ball Takes timeline — pundit takes, crowd cards, and matchweek heat. Sourced when we have it, never invented.",
  alternates: { canonical: "/banter" },
};

export default function BanterPage() {
  return (
    <PageContainer width="narrow">
      <SectionHeader
        as="h1"
        eyebrow="Timeline"
        title="Banter"
        description="Pundit takes, receipts, and matchweek heat. Refresh for the latest — we only show what the feed actually sent."
      />
      <BanterFeedPanel />
    </PageContainer>
  );
}
