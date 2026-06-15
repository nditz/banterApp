import Link from "next/link";
import { ConceptSlider } from "@/components/rules/ConceptSlider";
import { buttonVariants } from "@/components/ui/button";
import { SCORING_RULES } from "@/lib/scoring-rules";
import { cn } from "@/lib/utils";

export default function RulesPage() {
  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <div>
        <h1 className="text-xl font-semibold sm:text-2xl">Rules & how it works</h1>
        <p className="mt-2 text-sm text-muted-foreground">
          Scoring, content scripts, leagues, and taking on the pundits — everything
          you need to know.
        </p>
      </div>

      <ConceptSlider autoPlay />

      <section className="rounded-md border border-border bg-card p-4 shadow-sm sm:p-5">
        <h2 className="text-base font-semibold">Points scoring</h2>
        <p className="mt-1 text-xs text-muted-foreground">
          One prediction type per match. Points are awarded after full time.
        </p>

        <ul className="mt-4 space-y-3">
          {SCORING_RULES.map((rule) => (
            <li
              key={rule.title}
              className="rounded-md border border-border bg-muted/30 p-3"
            >
              <div className="flex items-start justify-between gap-3">
                <div>
                  <p className="text-sm font-semibold">{rule.title}</p>
                  <p className="mt-1 text-xs text-muted-foreground">
                    {rule.description}
                  </p>
                  <p className="mt-1.5 text-[11px] italic text-muted-foreground">
                    e.g. {rule.example}
                  </p>
                </div>
                <span
                  className={cn(
                    "shrink-0 rounded-md px-2 py-1 text-xs font-bold",
                    "bonus" in rule && rule.bonus
                      ? "bg-gold/15 text-gold-foreground"
                      : "bg-pitch/15 text-pitch"
                  )}
                >
                  {"bonus" in rule && rule.bonus ? "+" : ""}
                  {rule.points}
                  {"bonus" in rule && rule.bonus ? " bonus" : " pts"}
                </span>
              </div>
            </li>
          ))}
        </ul>
      </section>

      <section className="rounded-md border border-border bg-card p-4 shadow-sm sm:p-5">
        <h2 className="text-base font-semibold">Prediction types</h2>
        <dl className="mt-3 space-y-3 text-sm">
          <div>
            <dt className="font-semibold">Result</dt>
            <dd className="text-muted-foreground">
              Home win, Draw, or Away win — pick one outcome.
            </dd>
          </div>
          <div>
            <dt className="font-semibold">Correct score</dt>
            <dd className="text-muted-foreground">
              Enter the exact score (e.g. 2-1). Highest reward, hardest call.
            </dd>
          </div>
          <div>
            <dt className="font-semibold">Double chance</dt>
            <dd className="text-muted-foreground">
              Pick two outcomes: home or draw, away or draw, or home or away. Lower
              risk, +2 points if either outcome hits.
            </dd>
          </div>
        </dl>
      </section>

      <section className="rounded-md border border-gold/25 bg-gold/5 p-4 sm:p-5">
        <h2 className="text-base font-semibold">Content scripts (cumulative)</h2>
        <p className="mt-2 text-sm text-muted-foreground">
          Export one script for <strong>all</strong> your predictions — whether
          you picked one match or ten. Pre-match scripts bundle your hot takes;
          post-match scripts cover your full results story for social.
        </p>
        <Link
          href="/"
          className={cn(buttonVariants({ size: "sm" }), "btn-tournament mt-4 h-8 text-xs")}
        >
          Back to predictions
        </Link>
      </section>
    </div>
  );
}
