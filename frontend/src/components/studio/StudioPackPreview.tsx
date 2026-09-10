"use client";

import Link from "next/link";
import { Check, Copy, Download } from "lucide-react";
import { Button, buttonVariants } from "@/components/ui/button";
import { EmptyState } from "@/components/ui/states";
import { downloadTextFile, formatStudioPackText, studioPerspectiveLabel } from "@/lib/studio-pack";
import { cn } from "@/lib/utils";
import type { StudioContentPack, StudioPerspective } from "@/lib/types";

type CopyKind = "script" | "pack" | "prompt" | "caption";

function factKind(provenance: string): "sourced" | "user" {
  return provenance === "prediction" || provenance === "receipt" ? "user" : "sourced";
}

interface StudioPackPreviewProps {
  pack: StudioContentPack | null;
  perspective?: StudioPerspective | null;
  remainingGenerations?: number | null;
  copied: CopyKind | null;
  onCopy: (kind: CopyKind, text: string) => void;
}

export function StudioPackPreview({
  pack,
  perspective,
  remainingGenerations,
  copied,
  onCopy,
}: StudioPackPreviewProps) {
  if (!pack) {
    return (
      <EmptyState
        dense
        title="Pack preview"
        description="Generate a pack to see the script, sourced facts, and export actions. Studio will not start from a blank prompt."
      />
    );
  }

  const packText = formatStudioPackText(pack, { perspective });
  const slug = pack.title.replace(/\s+/g, "-").toLowerCase().slice(0, 40);
  const sourcedFacts = pack.facts.filter((fact) => factKind(fact.provenance) === "sourced");
  const userTakes = pack.facts.filter((fact) => factKind(fact.provenance) === "user");

  return (
    <div className="space-y-3">
      <div>
        <p className="text-sm font-semibold">{pack.title}</p>
        <p className="text-[11px] text-muted-foreground">
          {pack.contentType} · {pack.tone}
          {studioPerspectiveLabel(perspective) ? ` · ${studioPerspectiveLabel(perspective)}` : ""} · facts stay sourced
        </p>
      </div>

      {remainingGenerations != null ? (
        <p className="text-[11px] text-muted-foreground">
          {remainingGenerations} generation{remainingGenerations === 1 ? "" : "s"} remaining
        </p>
      ) : null}

      <PreviewBlock kicker="AI-generated" title="Hook">
        <p className="text-sm">{pack.hook}</p>
      </PreviewBlock>

      <PreviewBlock kicker="AI-generated" title="Script">
        <pre className="max-h-56 overflow-auto whitespace-pre-wrap rounded-lg bg-muted/40 p-3 text-[11px] leading-5">
          {pack.script}
        </pre>
      </PreviewBlock>

      {userTakes.length > 0 ? (
        <PreviewBlock kicker="Your take" title="From your pick">
          <ul className="space-y-1 text-[11px] text-muted-foreground">
            {userTakes.map((fact) => (
              <li key={`${fact.label}-${fact.value}`}>
                <span className="font-medium text-foreground">{fact.label}:</span> {fact.value}
              </li>
            ))}
          </ul>
        </PreviewBlock>
      ) : null}

      <PreviewBlock kicker="Sourced" title="Locked facts">
        {sourcedFacts.length ? (
          <ul className="space-y-1 text-[11px] text-muted-foreground">
            {sourcedFacts.map((fact) => (
              <li key={`${fact.label}-${fact.value}`}>
                <span className="font-medium text-foreground">{fact.label}:</span> {fact.value}{" "}
                <span className="text-muted-foreground/80">({fact.provenance})</span>
              </li>
            ))}
          </ul>
        ) : (
          <p className="text-[11px] text-muted-foreground">No sourced facts on this pack.</p>
        )}
      </PreviewBlock>

      <PreviewBlock kicker="AI-generated" title="Visual plan">
        <ul className="list-disc space-y-0.5 pl-4 text-[11px] text-muted-foreground">
          {pack.visualPlan.map((item) => (
            <li key={item}>{item}</li>
          ))}
        </ul>
      </PreviewBlock>

      {pack.memeDirection ? (
        <PreviewBlock kicker="AI-generated" title="Meme / GIF direction">
          <p className="text-[11px] text-muted-foreground">{pack.memeDirection}</p>
        </PreviewBlock>
      ) : null}

      <PreviewBlock kicker="AI-generated" title="Caption">
        <p className="text-sm">{pack.caption}</p>
        <p className="mt-1 text-[11px] text-muted-foreground">{pack.hashtags.join(" ")}</p>
      </PreviewBlock>

      <PreviewBlock kicker="Sourced" title="Source notes">
        <ul className="list-disc space-y-0.5 pl-4 text-[11px] text-muted-foreground">
          {pack.sourceNotes.map((note) => (
            <li key={note}>{note}</li>
          ))}
        </ul>
      </PreviewBlock>

      <div className="flex flex-wrap gap-2">
        <Button type="button" size="sm" variant="outline" onClick={() => onCopy("script", pack.script)}>
          {copied === "script" ? <Check className="size-3.5" /> : <Copy className="size-3.5" />}
          Copy script
        </Button>
        <Button type="button" size="sm" variant="outline" onClick={() => onCopy("prompt", pack.imagePrompt)}>
          {copied === "prompt" ? <Check className="size-3.5" /> : <Copy className="size-3.5" />}
          Copy prompt
        </Button>
        <Button type="button" size="sm" variant="outline" onClick={() => onCopy("caption", pack.caption)}>
          {copied === "caption" ? <Check className="size-3.5" /> : <Copy className="size-3.5" />}
          Copy caption
        </Button>
        <Button type="button" size="sm" variant="outline" onClick={() => onCopy("pack", packText)}>
          {copied === "pack" ? <Check className="size-3.5" /> : <Copy className="size-3.5" />}
          Copy content pack
        </Button>
        <Button
          type="button"
          size="sm"
          variant="outline"
          onClick={() => downloadTextFile(`balltakes-pack-${slug}.txt`, packText)}
        >
          <Download className="size-3.5" />
          Download text
        </Button>
        <Button
          type="button"
          size="sm"
          variant="outline"
          onClick={() =>
            downloadTextFile(
              `balltakes-pack-${slug}.json`,
              JSON.stringify(
                {
                  perspective: perspective ?? null,
                  attribution: {
                    script: "ai-generated",
                    facts: "sourced",
                    userTakes: "from your Ball Takes picks",
                  },
                  pack,
                },
                null,
                2
              ),
              "application/json"
            )
          }
        >
          <Download className="size-3.5" />
          Download JSON
        </Button>
      </div>

      <div className="flex flex-wrap gap-2 border-t border-border pt-3">
        <Link
          href="/banter"
          className={cn(buttonVariants({ variant: "outline", size: "sm" }))}
        >
          Back to Banter
        </Link>
        <Link
          href="/predictions/history"
          className={cn(buttonVariants({ variant: "outline", size: "sm" }))}
        >
          Share later
        </Link>
      </div>
      <p className="text-[11px] text-muted-foreground">
        Copy the caption when you are ready to post. No share counts — open receipts or head back to Banter.
      </p>
    </div>
  );
}

function PreviewBlock({
  kicker,
  title,
  children,
}: {
  kicker: string;
  title: string;
  children: React.ReactNode;
}) {
  return (
    <div>
      <p className="text-[10px] font-semibold uppercase tracking-wide text-muted-foreground">{kicker}</p>
      <p className="text-[11px] font-semibold">{title}</p>
      <div className="mt-1">{children}</div>
    </div>
  );
}
