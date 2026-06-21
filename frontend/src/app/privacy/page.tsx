import Link from "next/link";

export const metadata = {
  title: "Privacy Policy — Banter",
};

export default function PrivacyPage() {
  return (
    <main className="mx-auto max-w-3xl px-4 py-10">
      <h1 className="text-2xl font-bold">Privacy Policy</h1>
      <p className="mt-4 text-sm text-muted-foreground">
        Banter collects account information through Supabase Auth, anonymous session
        identifiers for guest play, and usage data needed to run predictions, leagues, and
        the content feed. We do not sell personal data.
      </p>
      <ul className="mt-4 list-disc space-y-2 pl-5 text-sm text-muted-foreground">
        <li>Authentication data is processed by Supabase according to their privacy policy.</li>
        <li>Anonymous users receive a cookie and optional recovery token stored locally.</li>
        <li>Admin audit and auth audit logs retain IP address and user agent for security.</li>
        <li>AI and ingestion jobs process public sports content with source attribution.</li>
      </ul>
      <p className="mt-6 text-sm text-muted-foreground">
        For takedown requests or privacy questions, contact{" "}
        <a href="mailto:privacy@banter.app" className="text-primary hover:underline">
          privacy@banter.app
        </a>
        .
      </p>
      <p className="mt-6 text-sm">
        See also{" "}
        <Link href="/terms" className="text-primary hover:underline">
          Terms of Use
        </Link>
        .
      </p>
      <div className="mt-10 border-t pt-8 text-sm text-muted-foreground">
        Entertainment predictions only — not affiliated with FIFA, broadcasters, or pundits cited in parody desks.
      </div>
    </main>
  );
}
