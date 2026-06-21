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
    id: "picks",
    title: "Lock picks before kickoff",
    subtitle: "Step 1 · Predict",
    body: "Tap a match, choose result, exact score, or double chance — save it to your session in seconds. No account needed to start; your browser keeps your slate.",
    accent: "pitch",
    backgroundImage: "/images/baller-score-points.png",
    highlights: ["Result · score · double", "Saved to your session", "Kickoff deadlines"],
    stickerImage: "/reactions/locked-in.svg",
  },
  {
    id: "banter",
    title: "Scroll banter that hits different",
    subtitle: "Step 2 · Watch",
    body: "Your feed mixes your picks vs reality, pundit hot takes, RSS + YouTube clips, and AI reactions with GIFs and memes — built for matchday group-chat energy.",
    accent: "brand",
    backgroundImage: "/images/banter-feed-hero.png",
    highlights: ["GIF reactions", "Pundit source tags", "Picks vs reality"],
    stickerImage: "/reactions/receipts-found.svg",
  },
  {
    id: "content",
    title: "Turn picks into post-ready scripts",
    subtitle: "Step 3 · Create",
    body: "Studio exports one cumulative script for every pick — pre-match bold calls or post-match flex and roast — formatted for TikTok, Reels, and Shorts with stats baked in.",
    accent: "gold",
    backgroundImage: "/images/baller-scripts.png",
    highlights: ["Pre + post scripts", "Copy & film", "All picks in one export"],
    stickerImage: "/reactions/script-writer.svg",
  },
  {
    id: "scoring",
    title: "Points that actually mean something",
    subtitle: "How you eat",
    body: "Result +3 · exact score +7 · double chance +2. Stack perfect matchday and group-stage bonuses. Private leagues unlock tournament awards for up to +190 more.",
    accent: "pitch",
    backgroundImage: "/images/score-points-your-way.png",
    highlights: ["+7 exact score", "Perfect day bonus", "Big league swings"],
    stickerImage: "/reactions/smart-choice.svg",
  },
  {
    id: "bonuses",
    title: "Bonus picks for the bold",
    subtitle: "High risk · high flex",
    body: "Golden Boot, POTY, Golden Glove, top assists, surprise package team — up to +50 each. Only scores in custom leagues with 3+ people actually playing.",
    accent: "flare",
    backgroundImage: "/images/bonus-picks.png",
    highlights: ["+50 POTY", "Private leagues only", "Flex when it lands"],
    stickerImage: "/reactions/chaos-pick.svg",
  },
  {
    id: "leagues",
    title: "Leagues = weekly drama",
    subtitle: "Your squad",
    body: "Spin up a private league, drop the invite in the group chat, and let weekly + tournament standings turn every matchday into an argument worth having.",
    accent: "brand",
    backgroundImage: "/images/fans_main_image_2.png",
    highlights: ["Invite code", "Weekly standings", "Global + country boards"],
  },
  {
    id: "pundits",
    title: "Beat the pundits on the board",
    subtitle: "Media vs you",
    body: "Your picks stack against podcasters, journalists, and TV analysts on the same leaderboard. Outrank them and you've got clip material for days.",
    accent: "gold",
    backgroundImage: "/images/ball-knowledge-header.png",
    highlights: ["Real pundit picks", "Aura rankings", "Receipts when you're right"],
    stickerImage: "/reactions/against-grain.svg",
  },
] as const;

/** Homepage-only: welcome + actions + concepts + export in one carousel */
export const HOME_WELCOME_SLIDES = [
  {
    id: "welcome",
    title: "Predict. Post. Win the group chat.",
    subtitle: "WC 2026 · free to play",
    body: "Lock picks, scroll live banter, export Reels-ready scripts — then flex on mates and pundits.",
    accent: "brand",
    backgroundImage: "/images/welcome-hero-panel.png",
    highlights: ["No account needed", "Scripts for Reels"],
    stickerImage: "/reactions/locked-in.svg",
  },
  ...CONCEPT_SLIDES,
] as const;
