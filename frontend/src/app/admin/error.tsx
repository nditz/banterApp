"use client";

import { Button } from "@/components/ui/button";

export default function AdminErrorPage({
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  return (
    <div className="mx-auto flex min-h-[40vh] max-w-lg flex-col items-center justify-center gap-4 px-4 text-center">
      <h2 className="text-xl font-semibold text-zinc-100">Admin panel error</h2>
      <p className="text-sm text-zinc-400">
        Something went wrong loading this admin page. Try again or return to the overview.
      </p>
      <div className="flex gap-2">
        <Button onClick={() => reset()}>Try again</Button>
        <Button variant="outline" onClick={() => (window.location.href = "/admin")}>
          Back to overview
        </Button>
      </div>
    </div>
  );
}
