import type { StudioMatchComparison } from "@/lib/types";

export function comparisonPhase(
  match: Pick<StudioMatchComparison, "actualResult" | "status">
): "before" | "after" {
  if (match.actualResult) {
    return "after";
  }
  const status = match.status?.toUpperCase() ?? "";
  if (status === "FT" || status === "FINISHED" || status === "AET" || status === "PEN") {
    return "after";
  }
  return "before";
}
