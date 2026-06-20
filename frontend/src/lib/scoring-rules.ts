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

/** Tournament-long bonus picks — only count in private leagues with 3+ members. */
export const TOURNAMENT_BONUS_RULES = [
  {
    id: "player_of_tournament",
    title: "Player of the Tournament",
    points: 50,
    difficulty: "Expert",
    description:
      "Call the official Player of the Tournament — the hardest pick on the board.",
    example: "You pick Vinícius Júnior — he wins POT → +50 points.",
    isTeamPick: false,
  },
  {
    id: "top_scorer",
    title: "Top Scorer",
    points: 40,
    difficulty: "Hard",
    description: "Who finishes as the tournament's leading goal scorer?",
    example: "You pick Kylian Mbappé — he wins the Golden Boot → +40 points.",
    isTeamPick: false,
  },
  {
    id: "top_assist",
    title: "Top Assist",
    points: 35,
    difficulty: "Hard",
    description: "Who leads the tournament in assists?",
    example: "You pick Kevin De Bruyne — he tops assists → +35 points.",
    isTeamPick: false,
  },
  {
    id: "golden_glove",
    title: "Golden Glove",
    points: 35,
    difficulty: "Hard",
    description: "Which goalkeeper wins the Golden Glove award?",
    example: "You pick Emiliano Martínez — Golden Glove winner → +35 points.",
    isTeamPick: false,
  },
  {
    id: "surprise_package",
    title: "Surprise Package",
    points: 30,
    difficulty: "Tricky",
    description:
      "Which team exceeds expectations and becomes the tournament's surprise package?",
    example: "You pick Japan — they reach the semi-finals → +30 points.",
    isTeamPick: true,
  },
] as const;

export const TOURNAMENT_BONUS_ELIGIBILITY = {
  minCustomLeagueMembers: 3,
  summary:
    "Anyone with an active session can save tournament bonus picks before kickoff. Bonus points only count on private league leaderboards with at least 3 members, after you've made at least one match or bracket pick. Global and Country leagues never include bonus points.",
} as const;

export const CONCEPT_SLIDES = [
  {
    id: "content",
    title: "Your picks become scripts, memes & banter",
    subtitle: "Content engine",
    body: "Export one cumulative script for all your picks — pre-match hot takes or post-match flex and roast — ready for TikTok, Reels, or Shorts. Studio-style, with the stats to back it up.",
    accent: "gold",
    backgroundImage: "/images/baller-scripts.png",
  },
  {
    id: "scoring",
    title: "Score points your way",
    subtitle: "Simple rules, big bragging rights",
    body: "Result picks earn +3, correct scores +7, double chance +2. Stack bonuses for perfect matchdays and group stages. In private leagues, nail tournament awards for up to +190 more.",
    accent: "pitch",
    backgroundImage: "/images/score-points-your-way.png",
  },
  {
    id: "bonuses",
    title: "Tournament bonus picks",
    subtitle: "Big swings for private leagues",
    body: "Player of the Tournament, Golden Boot, Golden Glove, top assists, and the surprise package team — up to +50 each. Only counts in custom leagues with 3+ mates who are actually playing.",
    accent: "flare",
    backgroundImage: "/images/bonus-picks.png",
  },
  {
    id: "leagues",
    title: "Leagues with your people",
    subtitle: "Friends, family, coworkers",
    body: "Create a private league, share an invite code, and see who really knows ball. Weekly, monthly, and tournament-long standings keep the banter going.",
    accent: "brand",
    backgroundImage: "/images/fans_main_image_2.png",
  },
  {
    id: "pundits",
    title: "You vs the pros",
    subtitle: "Podcasts, shows & media pundits",
    body: "Compare your picks against journalists, podcasters, and TV analysts. When you beat the pros on the leaderboard, that's content gold.",
    accent: "gold",
    backgroundImage: "/images/fans_main_image_1.png",
  },
] as const;

/** Homepage-only: welcome + actions + concepts + export in one carousel */
export const HOME_WELCOME_SLIDES = [
  {
    id: "welcome",
    title: "Ball Takes",
    subtitle: "World Cup 2026",
    body: "Every pick you make becomes ready-to-post content — pre-match hot takes, post-match receipts, full scripts for TikTok, Reels & Shorts. Play the tournament, walk away with a content library.",
    accent: "brand",
    backgroundImage: "/images/ball-takes-header.png",
  },
  ...CONCEPT_SLIDES,
] as const;
