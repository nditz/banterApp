import { JoinLeagueLanding } from "@/components/leagues/JoinLeagueLanding";

interface JoinLeaguePageProps {
  params: Promise<{ code: string }>;
}

export default async function JoinLeaguePage({ params }: JoinLeaguePageProps) {
  const { code } = await params;
  return (
    <div className="mx-auto max-w-lg py-4">
      <JoinLeagueLanding inviteCode={code.toUpperCase()} />
    </div>
  );
}
