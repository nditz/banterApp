import type { Metadata } from "next";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { CreateLeagueForm } from "@/components/leagues/CreateLeagueForm";
import { JoinLeagueForm } from "@/components/leagues/JoinLeagueForm";
import { LeaguesList } from "@/components/leagues/LeaguesList";
import { SessionKeyNotice } from "@/components/session/SessionKeyNotice";
import { PageContainer, SectionHeader } from "@/components/ui/section-header";

export const metadata: Metadata = {
  title: "Leagues",
  description:
    "Create private Ball Takes leagues for friends, family or the office — up to 50 players each. Share one invite link and compete across the Premier League season. No signup required.",
  alternates: { canonical: "/leagues" },
};

export default function LeaguesPage() {
  return (
    <PageContainer>
      <SectionHeader
        as="h1"
        eyebrow="Beat your mates"
        title="Leagues"
        description="Create private leagues for office mates, family or friends — up to 50 players per league. You can belong to up to 3 custom leagues (5 total including the Global and Country leagues you join automatically). Private leagues with at least 3 members unlock season calls (league winner, Golden Boot, and more). No signup required."
      />

      <SessionKeyNotice />

      <div className="grid gap-6 md:grid-cols-2">
        <Card className="overflow-hidden rounded-lg border-border shadow-sm">
          <div className="h-0.5 bg-pitch" />
          <CardHeader>
            <CardTitle>Create League</CardTitle>
            <CardDescription>
              Onboard as admin, then share one invite link with your people.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <CreateLeagueForm />
          </CardContent>
        </Card>

        <Card className="overflow-hidden rounded-lg border-border shadow-sm">
          <div className="h-0.5 bg-pitch" />
          <CardHeader>
            <CardTitle>Join League</CardTitle>
            <CardDescription>
              Got an invite code? Enter it with the name you want on the
              standings.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <JoinLeagueForm />
          </CardContent>
        </Card>
      </div>

      <LeaguesList />
    </PageContainer>
  );
}
