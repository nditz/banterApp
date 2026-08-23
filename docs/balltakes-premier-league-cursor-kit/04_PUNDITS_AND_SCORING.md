# Pundits, Provenance and Scoring

## Why this matters

Pundit tracking is one of BallTakes' strongest differentiators. The system should make professional or social predictions measurable using the same scoring logic as ordinary users.

## Pundit model

Suggested fields:

Pundit
- Id
- DisplayName
- Slug
- AvatarUrl
- Bio
- PunditType
- IsActive
- IsVerified

Pundit types may include:

- TV
- Journalist
- YouTube
- Podcast
- SocialMedia
- Publication

## Pundit source

PunditSource
- Id
- PunditId
- Platform
- ChannelName
- ExternalAccountId
- Url
- IsActive

Potential platforms:

- YouTube
- RSS/web publication
- podcast/transcript source
- social platform where permitted
- manual/admin entry

## Prediction provenance is mandatory

Every PunditPrediction should preserve enough information to prove where it came from.

Suggested fields:

- Id
- PunditId
- FixtureId
- PredictedOutcome
- PredictedHomeScore nullable
- PredictedAwayScore nullable
- SourceUrl
- SourcePlatform
- SourcePublishedAt
- RawQuote / excerpt reference
- SourceTimestamp where available
- Confidence
- VerificationStatus
- ExtractedAt

Never generate a pundit's opinion with AI and present it as an authentic prediction.

## Ingestion pipeline

Source -> ingestion -> transcript/text -> AI extraction -> fixture matching -> candidate -> confidence validation -> publish/review

AI extraction should produce structured output such as:

```json
{
  "pundit": "Example Pundit",
  "fixture": "Arsenal vs Liverpool",
  "prediction": "Arsenal",
  "homeScore": 2,
  "awayScore": 1,
  "confidence": 0.94,
  "sourceTimestamp": "...",
  "sourceUrl": "..."
}
```

Low-confidence records should enter an admin review queue.

## Scoring

Use the same core scoring service for users and pundits when they submit equivalent prediction types.

Track at minimum:

- total predictions;
- correct outcomes;
- incorrect outcomes;
- exact scores;
- accuracy percentage;
- total points;
- points per prediction;
- current streak;
- last five;
- matchweek score;
- season score.

## Pundit leaderboard

Create a Premier League pundit table showing:

- rank;
- pundit;
- points;
- accuracy;
- exact scores;
- current streak;
- previous/latest matchweek performance.

## Pundit profile

A pundit profile should show:

- profile information;
- current rank;
- season statistics;
- recent predictions;
- correct/incorrect takes;
- receipts;
- source links;
- comparison with current user.

## User-vs-pundit experience

Prominent dashboard examples:

- "You're beating 8 of 12 tracked pundits"
- "You: 281 points / Pundit average: 247"
- matchweek comparison;
- season comparison;
- head-to-head against individual pundits.

## Start small

Do not attempt to track every pundit or social creator for V1. Begin with a curated set whose predictions can be sourced reliably.
