import { Suspense } from "react";
import { StudioPage } from "@/components/studio/StudioPage";

export const metadata = {
  title: "Content Studio",
  description:
    "Pick a receipt or sourced story, choose a format and tone, and export a Ball Takes content pack.",
  alternates: { canonical: "/studio" },
};

export default function Studio() {
  return (
    <Suspense fallback={<div className="mx-auto max-w-[1400px] py-10 text-sm text-muted-foreground">Loading Studio…</div>}>
      <StudioPage />
    </Suspense>
  );
}
