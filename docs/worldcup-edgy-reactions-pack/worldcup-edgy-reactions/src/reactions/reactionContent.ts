export type ReactionTone = 'family' | 'standard' | 'spicy';

export type ReactionKey =
  | 'smart_choice'
  | 'playing_safe'
  | 'against_grain'
  | 'chaos_pick'
  | 'locked_in'
  | 'delulu_vision'
  | 'receipts_found'
  | 'prediction_fraud'
  | 'brave_but_wrong'
  | 'script_writer';

export interface ReactionContentItem {
  key: ReactionKey;
  title: string;
  archetype: string;
  emoji: string;
  asset: string;
  auraDelta: number;
  captions: Record<ReactionTone, string[]>;
  microcopy: string[];
}

export const reactionContent: ReactionContentItem[] = [
  {
    key: 'smart_choice',
    title: 'Smart Choice',
    archetype: 'Football Professor',
    emoji: '🤓☝️📈',
    asset: '/reactions/smart-choice.svg',
    auraDelta: 50,
    captions: {
      family: ['Sensible pick. Good ball knowledge.', 'The safe brainy option.', 'You read the room correctly.'],
      standard: ['Cooked.', 'Ball knowledge detected.', 'This pick has spreadsheet energy, in a good way.', 'Lowkey elite decision-making.'],
      spicy: ['Bro downloaded the script.', 'Everyone and their nan saw this one coming.', 'No cap, this is the HR-approved pick.']
    },
    microcopy: ['Favourite backed', 'Logic merchant', 'Probability pal']
  },
  {
    key: 'playing_safe',
    title: 'Playing It Safe',
    archetype: 'Fence Sitter',
    emoji: '🤷😬🪑',
    asset: '/reactions/playing-safe.svg',
    auraDelta: 25,
    captions: {
      family: ['Keeping it cautious.', 'Could go either way.', 'A careful prediction.'],
      standard: ['Could go either way ngl.', 'Switzerland called. They want their neutrality back.', 'It’s giving cautious analyst.'],
      spicy: ['Fence sitting with confidence is still fence sitting.', 'Bro picked emotional insurance.', 'Zero risk, zero screenshots. Respectfully.']
    },
    microcopy: ['Cautious energy', 'Neutrality mode', 'Draw merchant']
  },
  {
    key: 'against_grain',
    title: 'Against the Grain',
    archetype: 'Chaos Merchant',
    emoji: '😤🚩⚡',
    asset: '/reactions/against-grain.svg',
    auraDelta: 120,
    captions: {
      family: ['Bold move.', 'Backing the underdog.', 'Brave pick. Let’s see.'],
      standard: ['Standing on business.', 'I see the vision.', 'Risky but respectable.', 'Main character pick unlocked.'],
      spicy: ['Screenshots have been taken.', 'This either ends in glory or group chat exile.', 'Generational confidence or generational receipts.']
    },
    microcopy: ['Underdog backed', 'Brave mode', 'Aura farming']
  },
  {
    key: 'chaos_pick',
    title: 'Chaos Pick',
    archetype: 'Generational Headloss Alert',
    emoji: '🚨💀🎲',
    asset: '/reactions/chaos-pick.svg',
    auraDelta: 250,
    captions: {
      family: ['That is very adventurous.', 'Huge risk, huge reward.', 'You went for the wild outcome.'],
      standard: ['Delulu or genius. No middle ground.', 'This pick needs a documentary if it lands.', 'The group chat is watching.'],
      spicy: ['Prediction Fraud Department has opened a file.', 'Bro is either cooking or burning down the kitchen.', 'This is not a prediction, it’s a manifesto.']
    },
    microcopy: ['Extreme risk', 'Delulu vision', 'Upset hunter']
  },
  {
    key: 'locked_in',
    title: 'Locked In',
    archetype: 'Matchday Oracle',
    emoji: '🔒🧠🔥',
    asset: '/reactions/locked-in.svg',
    auraDelta: 90,
    captions: {
      family: ['Confident and focused.', 'You trust your read.', 'Strong prediction energy.'],
      standard: ['Locked in.', 'Aura looking healthy.', 'This is giving matchday oracle.'],
      spicy: ['The spreadsheet is sweating.', 'Bro saw the matrix.', 'No notes. Dangerous levels of confidence.']
    },
    microcopy: ['High confidence', 'Oracle mode', 'No hesitation']
  },
  {
    key: 'delulu_vision',
    title: 'Delulu Vision',
    archetype: 'Vibes Scout',
    emoji: '🌀👀✨',
    asset: '/reactions/delulu-vision.svg',
    auraDelta: 80,
    captions: {
      family: ['A vibes-based prediction.', 'Interesting choice.', 'You are trusting instinct.'],
      standard: ['Lowkey I see the vision.', 'The maths disagrees but the vibes are loud.', 'It’s giving “trust me bro”.'],
      spicy: ['Delulu might be the solulu.', 'Stats said no. You said bet.', 'This pick was made with vibes and WiFi.']
    },
    microcopy: ['Vibes pick', 'Instinct mode', 'Trust me bro']
  },
  {
    key: 'receipts_found',
    title: 'Receipts Found',
    archetype: 'Screenshot Historian',
    emoji: '📜✅🏆',
    asset: '/reactions/receipts-found.svg',
    auraDelta: 300,
    captions: {
      family: ['You called it.', 'Correct prediction.', 'Great read.'],
      standard: ['Receipts found.', 'Aged beautifully.', 'Ball knowledge confirmed.'],
      spicy: ['Talk your talk.', 'They doubted. You documented.', 'Apology forms are now available.']
    },
    microcopy: ['Correct pick', 'Proof secured', 'Talk your talk']
  },
  {
    key: 'prediction_fraud',
    title: 'Prediction Fraud Department',
    archetype: 'Case Under Review',
    emoji: '🚔📉🫣',
    asset: '/reactions/prediction-fraud.svg',
    auraDelta: -50,
    captions: {
      family: ['That one did not land.', 'Prediction missed.', 'Back to the tactics board.'],
      standard: ['This aged like milk.', 'Prediction under investigation.', 'The timeline remembers.'],
      spicy: ['Fraud watch activated.', 'Bro predicted with vibes only.', 'Delete button looking tempting right now.']
    },
    microcopy: ['Missed pick', 'Case opened', 'Back to the lab']
  },
  {
    key: 'brave_but_wrong',
    title: 'Brave But Wrong',
    archetype: 'Hero Ball Casualty',
    emoji: '🫡💔🚩',
    asset: '/reactions/brave-but-wrong.svg',
    auraDelta: -10,
    captions: {
      family: ['Brave call, wrong result.', 'The idea was bold.', 'Respect for the risk.'],
      standard: ['Wrong, but aura survived.', 'The vision was there. The result was not.', 'Hero ball did not land.'],
      spicy: ['You stood on business. Business collapsed.', 'A historic miss, but with chest.', 'The streets respect the attempt.']
    },
    microcopy: ['Wrong but bold', 'Aura survived', 'Risk tax paid']
  },
  {
    key: 'script_writer',
    title: 'Script Writer',
    archetype: 'Football Oracle',
    emoji: '🎬🔮⚽',
    asset: '/reactions/script-writer.svg',
    auraDelta: 700,
    captions: {
      family: ['Exact score. Incredible.', 'You predicted the script.', 'Perfect call.'],
      standard: ['You wrote the script.', 'Exact score merchant.', 'This is not luck. This is cinema.'],
      spicy: ['Check this person’s hard drive for the fixture script.', 'Generational ball knowledge.', 'The pundits are unemployed now.']
    },
    microcopy: ['Exact score', 'Cinema', 'Oracle confirmed']
  }
];

export const auraLevels = [
  { min: 0, label: 'Casual Fan', emoji: '🙂' },
  { min: 100, label: 'Kitchen Analyst', emoji: '🍳' },
  { min: 500, label: 'Twitter Tactician', emoji: '📱' },
  { min: 1000, label: 'Matchday Oracle', emoji: '🔮' },
  { min: 2500, label: 'Ball Knowledge Merchant', emoji: '🧠' },
  { min: 5000, label: 'Generational Talent', emoji: '🏆' }
];

export const banterTemplates = {
  smart_choice: [
    '{name} picked {pick}. Safe, sensible, very spreadsheet-coded.',
    '{name} backed the favourite. Ball knowledge or basic? We’ll find out.',
    '{name} chose logic over chaos. Respectable behaviour.'
  ],
  playing_safe: [
    '{name} picked {pick}. Fence officially occupied.',
    '{name} has entered neutrality mode.',
    '{name} said “I’m not here for screenshots today.”'
  ],
  against_grain: [
    '{name} backed {pick}. The group chat has been notified.',
    '{name} is standing on business with this one.',
    '{name} picked the underdog. Aura farming has begun.'
  ],
  chaos_pick: [
    '{name} has submitted a chaos pick. Medical team on standby.',
    '{name} said stats are optional.',
    '{name} is either a genius or needs WiFi supervision.'
  ]
};
