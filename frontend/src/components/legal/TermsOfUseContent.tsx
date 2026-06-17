import Link from "next/link";
import { TERMS_SECTIONS, TERMS_VERSION } from "@/lib/terms-of-use";
import { cn } from "@/lib/utils";

interface TermsOfUseContentProps {
  className?: string;
  /** Smaller type for inline panels and the footer */
  compact?: boolean;
  showTitle?: boolean;
}

export function TermsOfUseContent({
  className,
  compact = false,
  showTitle = true,
}: TermsOfUseContentProps) {
  const textSize = compact ? "text-[10px] leading-relaxed" : "text-xs leading-relaxed sm:text-sm";
  const headingSize = compact ? "text-[10px] font-semibold" : "text-sm font-semibold";

  return (
    <article className={cn("space-y-3", className)}>
      {showTitle && (
        <header className={compact ? "space-y-0.5" : "space-y-1"}>
          <h2 className={cn(compact ? "text-xs font-semibold" : "text-base font-semibold")}>
            Terms of Use
          </h2>
          <p className={cn("text-muted-foreground", compact ? "text-[10px]" : "text-xs")}>
            Last updated {TERMS_VERSION}
          </p>
        </header>
      )}

      {TERMS_SECTIONS.map((section) => (
        <section key={section.id} className={compact ? "space-y-1" : "space-y-1.5"}>
          <h3 className={cn(headingSize, "text-foreground")}>{section.title}</h3>
          {section.paragraphs.map((paragraph, index) => (
            <p key={index} className={cn(textSize, "text-muted-foreground")}>
              {section.id === "entertainment" && index === 2 ? (
                <>
                  If you or someone you know is affected by problem gambling, please seek support
                  at{" "}
                  <Link
                    href="https://www.begambleaware.org"
                    target="_blank"
                    rel="noopener noreferrer"
                    className="font-medium text-primary underline hover:text-foreground"
                  >
                    BeGambleAware.org
                  </Link>{" "}
                  or call the National Gambling Helpline:{" "}
                  <Link href="tel:08088020133" className="font-medium text-primary underline hover:text-foreground">
                    0808 802 0133
                  </Link>
                  .
                </>
              ) : (
                paragraph
              )}
            </p>
          ))}
        </section>
      ))}
    </article>
  );
}

/** Footer-style entertainment disclaimer (matches site footer copy). */
export function TermsEntertainmentNotice({ className }: { className?: string }) {
  return (
    <p
      className={cn(
        "rounded-md border border-border bg-muted/40 px-4 py-2.5 text-[10px] leading-relaxed text-muted-foreground",
        className
      )}
    >
      <strong className="text-foreground">Entertainment only — not gambling.</strong>{" "}
      Ball Knowledge is a free-to-play social predictions game intended for banter, entertainment,
      and friendly competition among friends and family. No real money is wagered, won, or lost.
      This platform does not encourage, promote, or facilitate gambling in any form. If you or
      someone you know is affected by problem gambling, please seek support at{" "}
      <Link
        href="https://www.begambleaware.org"
        target="_blank"
        rel="noopener noreferrer"
        className="underline hover:text-foreground"
      >
        BeGambleAware.org
      </Link>{" "}
      or call the National Gambling Helpline:{" "}
      <Link href="tel:08088020133" className="underline hover:text-foreground">
        0808 802 0133
      </Link>
      .
    </p>
  );
}
