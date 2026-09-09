export const RECEIPT_STORY_LABELS: Record<string, string> = {
  exact_score: "Exact score",
  beat_pundit: "You beat the pundit",
  pundit_beat_user: "Pundit got there first",
  minority_right: "Minority call",
  majority_wrong: "Crowd miss",
  hit: "Hit",
  miss: "Miss",
};

export function formatReceiptStoryType(type?: string | null): string {
  if (!type) return "Receipt";
  return RECEIPT_STORY_LABELS[type] ?? type.replaceAll("_", " ");
}
