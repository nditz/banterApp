export const SCORING_RULES = [
  {
    title: "Match result (Home / Draw / Away)",
    points: 3,
    description: "Pick the correct outcome of the full match.",
    example: "You pick Brazil to win — Brazil wins 2-1 → +3 points.",
  },
  {
    title: "Correct score",
    points: 7,
    description: "Call the exact final scoreline.",
    example: "You pick 2-1 — final is 2-1 → +7 points.",
  },
  {
    title: "Double chance",
    points: 2,
    description: "Cover two of the three possible outcomes in one pick.",
    example: "Home or Draw — if Brazil win or draw → +2 points.",
  },
  {
    title: "Perfect matchday",
    points: 5,
    bonus: true,
    description: "Bonus when every result pick on a matchday is correct.",
    example: "All your result picks hit on the same day → +5 bonus.",
  },
  {
    title: "Perfect group stage",
    points: 20,
    bonus: true,
    description: "Bonus for a flawless group-stage run (all picks in the group).",
    example: "Every group-stage pick correct → +20 bonus.",
  },
] as const;

export const CONCEPT_SLIDES = [
  {
    id: "content",
    title: "Your picks become scripts, memes & banter",
    subtitle: "Content engine",
    body: "Export one cumulative script for all your picks — pre-match hot takes or post-match flex and roast — ready for TikTok, Reels, or Shorts. Studio-style, with the stats to back it up.",
    accent: "gold",
  },
  {
    id: "scoring",
    title: "Score points your way",
    subtitle: "Simple rules, big bragging rights",
    body: "Result picks earn +3, correct scores +7, double chance +2. Stack bonuses for perfect matchdays and group stages. Climb global and league tables.",
    accent: "pitch",
  },
  {
    id: "leagues",
    title: "Leagues with your people",
    subtitle: "Friends, family, coworkers",
    body: "Create a private league, share an invite code, and see who really knows ball. Weekly, monthly, and tournament-long standings keep the banter going.",
    accent: "flare",
  },
  {
    id: "pundits",
    title: "You vs the pros",
    subtitle: "Podcasts, shows & media pundits",
    body: "Compare your picks against journalists, podcasters, and TV analysts. When you beat the pros on the leaderboard, that's content gold.",
    accent: "brand",
  },
] as const;

/** Homepage-only: welcome + actions + concepts + export in one carousel */
export const HOME_WELCOME_SLIDES = [
  {
    id: "welcome",
    title: "Predict. Banter. Create content.",
    subtitle: "World Cup 2026",
    body: "Every pick you make becomes ready-to-post content — pre-match hot takes, post-match receipts, full scripts for TikTok, Reels & Shorts. Play the tournament, walk away with a content library.",
    accent: "brand",
  },
  ...CONCEPT_SLIDES,
] as const;
