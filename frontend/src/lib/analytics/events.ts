/**
 * Client mirror of `AnalyticsEventCatalog` on the backend.
 *
 * The server is authoritative: it rejects unknown event names and drops unknown
 * property keys. This mirror exists so a typo becomes a TypeScript error at build time
 * instead of a 400 at runtime. Keep the two in sync — if you add an event here, add it
 * to `backend/BanterApp.Api/Features/Analytics/AnalyticsEventCatalog.cs` as well.
 *
 * Never add a property that could carry a recovery key, an access token, a prompt, AI
 * output, or any free-form user text.
 */

export interface AnalyticsEventProperties {
  session_started: { isReturning?: boolean };
  landing_viewed: { variant?: string };
  guest_session_created: { countryCode?: string };
  recovery_key_created: Record<string, never>;

  registration_started: { method?: "password" | "google" };
  registration_completed: { method?: "password" | "google" };
  login_completed: { method?: "password" | "google" };
  guest_claim_completed: { predictionsClaimed?: number };

  fixture_viewed: { matchweek?: number };
  prediction_started: { matchweek?: number; predictionType?: string };
  prediction_created: { matchweek?: number; predictionType?: string };
  prediction_updated: { matchweek?: number; predictionType?: string };
  matchweek_predictions_completed: { matchweek?: number; predictionCount?: number };
  prediction_result_viewed: { matchweek?: number };
  leaderboard_viewed: { scope?: string };

  prediction_league_created: { kind?: string };
  prediction_league_joined: { kind?: string };
  prediction_league_viewed: { kind?: string };

  pundit_list_viewed: Record<string, never>;
  pundit_profile_viewed: { punditId?: string };
  pundit_comparison_viewed: { matchweek?: number };
  pundit_source_opened: { sourceType?: string };

  content_generation_started: { contentType?: string; tone?: string };
  content_generation_completed: {
    contentType?: string;
    tone?: string;
    durationBucket?: string;
  };
  content_generation_failed: { contentType?: string; errorCategory?: string };
  content_regenerated: { contentType?: string };
  content_exported: { contentType?: string; exportFormat?: string };
}

export type AnalyticsEventName = keyof AnalyticsEventProperties;

/** Buckets a duration so raw timings cannot be used to fingerprint a session. */
export function durationBucket(milliseconds: number): string {
  if (milliseconds < 1_000) return "under_1s";
  if (milliseconds < 3_000) return "1_3s";
  if (milliseconds < 10_000) return "3_10s";
  if (milliseconds < 30_000) return "10_30s";
  return "over_30s";
}
