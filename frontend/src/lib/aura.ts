const AURA_TOTAL_KEY = "banter_aura_total";
const POST_MATCH_AURA_KEY = "banter_post_match_aura_awarded";

export function getAuraTotal(): number {
  if (typeof window === "undefined") return 0;
  const stored = localStorage.getItem(AURA_TOTAL_KEY);
  const parsed = Number.parseInt(stored ?? "0", 10);
  return Number.isFinite(parsed) ? Math.max(0, parsed) : 0;
}

export function applyAuraDelta(delta: number): number {
  if (typeof window === "undefined") return 0;
  const next = Math.max(0, getAuraTotal() + delta);
  localStorage.setItem(AURA_TOTAL_KEY, String(next));
  window.dispatchEvent(new CustomEvent("aura-updated", { detail: next }));
  return next;
}

function getPostMatchAwardedIds(): Set<string> {
  if (typeof window === "undefined") return new Set();
  try {
    const raw = localStorage.getItem(POST_MATCH_AURA_KEY);
    const parsed = raw ? (JSON.parse(raw) as string[]) : [];
    return new Set(Array.isArray(parsed) ? parsed : []);
  } catch {
    return new Set();
  }
}

export function awardPostMatchAura(predictionId: string, delta: number): boolean {
  if (typeof window === "undefined") return false;
  const awarded = getPostMatchAwardedIds();
  if (awarded.has(predictionId)) return false;
  awarded.add(predictionId);
  localStorage.setItem(POST_MATCH_AURA_KEY, JSON.stringify([...awarded]));
  applyAuraDelta(delta);
  return true;
}

export function hasPostMatchAuraAward(predictionId: string): boolean {
  return getPostMatchAwardedIds().has(predictionId);
}
