import Link from "next/link";
import { TermsOfUseContent } from "@/components/legal/TermsOfUseContent";
import { buttonVariants } from "@/components/ui/button";
import { BRAND } from "@/lib/brand";
import { cn } from "@/lib/utils";

export const metadata = {
  title: "Terms of Use",
  description: `${BRAND.name} terms of use — entertainment only, not gambling.`,
  alternates: { canonical: "/terms" },
};

export default function TermsPage() {
  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <div>
        <h1 className="text-xl font-semibold sm:text-2xl">Terms of Use</h1>
        <p className="mt-2 text-sm text-muted-foreground">
          Please read these terms before making predictions or joining leagues.
        </p>
      </div>

      <section className="rounded-md border border-border bg-card p-4 shadow-sm sm:p-5">
        <TermsOfUseContent showTitle={false} />
      </section>

      <div className="flex flex-wrap gap-2">
        <Link href="/" className={cn(buttonVariants({ size: "sm" }), "btn-tournament h-8 text-xs")}>
          Back to home
        </Link>
        <Link
          href="/rules"
          className={cn(buttonVariants({ variant: "outline", size: "sm" }), "h-8 text-xs")}
        >
          Scoring rules
        </Link>
      </div>
    </div>
  );
}
