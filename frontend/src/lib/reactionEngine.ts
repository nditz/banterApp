import { reactionContent, ReactionContentItem, ReactionKey, ReactionTone } from '@/reactions/reactionContent';
import { reactionRules } from '@/reactions/reactionRules';

export type PredictionOutcome = 'home' | 'draw' | 'away';

export interface FixturePredictionContext {
  fixtureId: string;
  homeTeamName: string;
  awayTeamName: string;
  userPick: PredictionOutcome;
  probabilities: Record<PredictionOutcome, number>;
  predictedScore?: string;
  predictionType?: 'result' | 'correct_score' | 'double_chance';
  userDisplayName?: string;
  tone?: ReactionTone;
}

export interface PredictionReaction extends ReactionContentItem {
  selectedCaption: string;
  pickedProbability: number;
  favoriteOutcome: PredictionOutcome;
  favoriteProbability: number;
}

const byKey = new Map(reactionContent.map((item) => [item.key, item]));

function pickRandom<T>(items: T[]): T {
  return items[Math.floor(Math.random() * items.length)];
}

function getFavoriteOutcome(probabilities: Record<PredictionOutcome, number>): PredictionOutcome {
  return (Object.entries(probabilities).sort((a, b) => b[1] - a[1])[0][0]) as PredictionOutcome;
}

export function getPredictionReaction(ctx: FixturePredictionContext): PredictionReaction {
  const tone = ctx.tone ?? reactionRules.familyFriendlyDefault;
  const favoriteOutcome = getFavoriteOutcome(ctx.probabilities);
  const favoriteProbability = ctx.probabilities[favoriteOutcome];
  const pickedProbability = ctx.probabilities[ctx.userPick];
  const homeAwayGap = Math.abs(ctx.probabilities.home - ctx.probabilities.away);

  let key: ReactionKey;

  if (ctx.predictionType === 'correct_score') {
    if (pickedProbability + reactionRules.chaosGap < favoriteProbability) {
      key = 'chaos_pick';
    } else if (ctx.userPick !== favoriteOutcome) {
      key = 'against_grain';
    } else {
      key = 'script_writer';
    }
  } else if (ctx.predictionType === 'double_chance') {
    key = ctx.userPick === favoriteOutcome && favoriteProbability >= reactionRules.smartFavoriteMinProbability
      ? 'smart_choice'
      : 'playing_safe';
  } else if (ctx.userPick === favoriteOutcome && favoriteProbability >= reactionRules.highConfidenceMinProbability) {
    key = 'locked_in';
  } else if (ctx.userPick === favoriteOutcome && favoriteProbability >= reactionRules.smartFavoriteMinProbability) {
    key = 'smart_choice';
  } else if (ctx.userPick === 'draw' || homeAwayGap < reactionRules.closeGameProbabilityGap) {
    key = 'playing_safe';
  } else if (pickedProbability + reactionRules.chaosGap < favoriteProbability) {
    key = 'chaos_pick';
  } else if (pickedProbability + reactionRules.underdogGap < favoriteProbability) {
    key = 'against_grain';
  } else {
    key = 'delulu_vision';
  }

  const content = byKey.get(key)!;

  return {
    ...content,
    selectedCaption: pickRandom(content.captions[tone]),
    pickedProbability,
    favoriteOutcome,
    favoriteProbability
  };
}

function toReactionItem(content: ReactionContentItem, tone: ReactionTone): PredictionReaction {
  return {
    ...content,
    selectedCaption: pickRandom(content.captions[tone]),
    pickedProbability: 0,
    favoriteOutcome: 'home',
    favoriteProbability: 0,
  };
}

/** Extra flavor reactions shown alongside the primary pick reaction. */
export function getSupplementalReactions(
  primaryKey: ReactionKey,
  ctx: FixturePredictionContext
): PredictionReaction[] {
  const tone = ctx.tone ?? reactionRules.familyFriendlyDefault;
  const bonusKeys = new Set<ReactionKey>();

  if (ctx.predictionType === 'correct_score') {
    bonusKeys.add('script_writer');
    bonusKeys.add('chaos_pick');
  } else if (ctx.predictionType === 'double_chance') {
    bonusKeys.add('playing_safe');
    bonusKeys.add('smart_choice');
  } else if (primaryKey === 'locked_in' || primaryKey === 'smart_choice') {
    bonusKeys.add('locked_in');
    bonusKeys.add('receipts_found');
  } else if (primaryKey === 'chaos_pick' || primaryKey === 'against_grain') {
    bonusKeys.add('against_grain');
    bonusKeys.add('delulu_vision');
  } else {
    bonusKeys.add('delulu_vision');
    bonusKeys.add('playing_safe');
  }

  bonusKeys.delete(primaryKey);

  return Array.from(bonusKeys)
    .slice(0, 2)
    .map((key) => {
      const content = byKey.get(key);
      return content ? toReactionItem(content, tone) : null;
    })
    .filter((item): item is PredictionReaction => item !== null);
}

export function getPostMatchReaction(params: {
  wasCorrect: boolean;
  exactScoreCorrect?: boolean;
  wasUnderdogPick?: boolean;
  tone?: ReactionTone;
}): PredictionReaction {
  const tone = params.tone ?? reactionRules.familyFriendlyDefault;
  let key: ReactionKey = 'prediction_fraud';

  if (params.exactScoreCorrect) key = 'script_writer';
  else if (params.wasCorrect) key = 'receipts_found';
  else if (params.wasUnderdogPick) key = 'brave_but_wrong';

  const content = byKey.get(key)!;

  return {
    ...content,
    selectedCaption: pickRandom(content.captions[tone]),
    pickedProbability: 0,
    favoriteOutcome: 'home',
    favoriteProbability: 0
  };
}
