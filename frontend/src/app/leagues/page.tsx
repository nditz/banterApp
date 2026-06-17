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

export default function LeaguesPage() {
  return (
    <div className="mx-auto max-w-4xl space-y-8">
      <div>
        <h1 className="text-xl font-semibold text-foreground sm:text-2xl">
          Leagues
        </h1>
        <p className="mt-2 text-muted-foreground">
          Create private leagues for office mates, family or friends — up to 50
          players per league. You can belong to up to 3 custom leagues (5 total
          including the Global and Country leagues you join automatically).
          Private leagues with at least 3 members unlock tournament bonus picks
          (Player of the Tournament, Golden Boot, and more). No signup required.
        </p>
      </div>

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
    </div>
  );
}
