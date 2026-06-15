# Cursor Prompt: Add Edgy Prediction Reactions, Aura, Banter and Share Receipts

You are working in an existing Next.js frontend for a World Cup prediction game. Most of the core app already exists: fixtures, user picks, leagues, prediction scoring and backend APIs. Your task is to compare this package with the current codebase, keep what already exists, and enhance the UX layer with a fun, social, Gen Z-inspired reaction system.

## Goal

When a user makes a prediction, do not only show “prediction saved”. Show a reaction that makes the pick feel social, funny and shareable.

Examples:

- Sensible favourite pick → “Smart Choice”, football professor, green aura, clever/confident animation.
- Draw or cautious pick → “Fence Sitter”, cautious energy, yellow/orange animation.
- Underdog pick → “Against the Grain”, brave/chaos merchant, purple/red animation.
- Very risky pick → “Generational Headloss Alert”, receipts-or-regret energy.
- Correct prediction after result → “Receipts Found”.
- Wrong prediction after result → “Prediction Fraud Department”.

## Important implementation principle

Do not hardcode slang and captions inside React components. All emoji, captions, tone, labels and thresholds must live in config files so they can be updated later.

This package contains:

```text
src/reactions/reactionContent.ts
src/reactions/reactionRules.ts
src/components/PredictionReactionCard.tsx
src/components/AuraBadge.tsx
src/components/PredictionReceiptCard.tsx
src/components/BanterLine.tsx
src/lib/reactionEngine.ts
public/reactions/*.svg
```

Compare these files with the existing project structure and adapt imports/styles to match the app.

## Data contract expected from current app

The reaction engine expects this shape or something equivalent:

```ts
export type PredictionOutcome = 'home' | 'draw' | 'away';

export interface FixturePredictionContext {
  fixtureId: string;
  homeTeamName: string;
  awayTeamName: string;
  userPick: PredictionOutcome;
  probabilities: {
    home: number;
    draw: number;
    away: number;
  };
  predictedScore?: string;
  userDisplayName?: string;
}
```

Probabilities should come from your existing provider integration:

1. API-Football predictions if available.
2. Sportmonks predictions if available.
3. Odds-implied probabilities if available.
4. Fallback model from rankings/form/stats.

## Integration steps

### 1. Copy package files into the Next.js app

Copy:

```text
src/reactions/* → your app src/reactions/*
src/components/* → your app src/components/reactions/* or equivalent
src/lib/reactionEngine.ts → your app src/lib/reactionEngine.ts
public/reactions/* → your app public/reactions/*
```

### 2. Install optional animation dependency

If the project does not already have Framer Motion:

```bash
npm install framer-motion
```

If you prefer not to add Framer Motion, keep the SVG animations and use CSS transitions only.

### 3. Wire into pick submission UI

Find the component where a user selects a match prediction. After the pick is saved successfully, call:

```ts
import { getPredictionReaction } from '@/lib/reactionEngine';

const reaction = getPredictionReaction({
  fixtureId,
  homeTeamName,
  awayTeamName,
  userPick,
  probabilities,
  predictedScore,
  userDisplayName,
});
```

Then render:

```tsx
<PredictionReactionCard reaction={reaction} />
```

### 4. Add post-match reactions

When a result is final, compare the user pick with the actual result and show:

- correct → receipts-found
- wrong favourite pick → prediction-fraud
- wrong underdog pick → brave-but-wrong
- exact score correct → script-writer

### 5. Add Aura Points

Use the reaction result to award immediate “vibe/aura” points. These are separate from serious league scoring.

Suggested:

```text
Smart favourite pick: +50 aura
Cautious draw/fence pick: +25 aura
Underdog pick: +120 aura
Massive upset pick: +250 aura
Correct prediction: +300 aura
Wrong prediction: -50 aura
Exact score: +700 aura
```

Aura should be playful and not replace real scoring.

### 6. Add share receipts

Add a “Generate Receipt” button after a prediction is saved. The receipt should show:

```text
User name
Fixture
Pick
Probability context
Reaction label
Aura change
Timestamp
League name if available
```

Use `PredictionReceiptCard.tsx` first. Later, convert the card to an image using `html-to-image` or a server-side image endpoint.

### 7. Add banter feed lines

For league activity feeds, use `BanterLine` and random lines from `reactionContent.ts`.

Examples:

```text
Stanley picked Brazil. Safe, sensible, very HR-approved.
Maya backed the underdog. Screenshots have been taken.
David picked a draw. Switzerland called, they want their neutrality back.
```

### 8. Accessibility and tone guardrails

- Keep jokes about predictions, never protected characteristics or real personal traits.
- Avoid abusive language.
- Use “savage” as playful banter, not harassment.
- Let users disable edgy copy in settings later.
- Provide a family-friendly mode.

## Suggested user setting

Add:

```ts
type BanterMode = 'family' | 'standard' | 'spicy';
```

Default to `standard`.

## Acceptance criteria

- After every pick, the user sees an animated reaction card.
- Reaction type is based on probabilities, not random choice.
- Captions and emoji are config-driven.
- Works in Next.js without backend changes except probability data.
- Assets are loaded from `/public/reactions`.
- The system does not block prediction saving if animations fail.
- There is a clear path to add more captions and animations.

## Enhancement ideas after first implementation

- Add AI-generated personalised roast/praise.
- Add league-wide “Aura Table”.
- Add “Prediction Fraud Department” post-match recap.
- Add “Receipts Found” share cards.
- Add weekly pundit comparison leaderboard.
- Add sound effects with mute toggle.
