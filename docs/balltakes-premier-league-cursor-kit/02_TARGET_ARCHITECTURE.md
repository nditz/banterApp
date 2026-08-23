# Target Architecture and Domain Model

## Core league model

Design the new active football domain around domestic club leagues:

Competition -> CompetitionSeason -> Matchweek -> Fixture

Supporting concepts:

- Team
- Player
- SeasonTeam
- SeasonPlayer
- Standing
- Prediction
- PredictionScore
- SeasonPredictionDefinition
- SeasonPrediction
- Pundit
- PunditSource
- PunditPrediction
- PunditScore
- Receipt
- GeneratedContent
- User
- GuestSession
- PredictionLeague / existing friend-league equivalent
- Subscription
- CreditTransaction

Reuse existing tables/entities where equivalent concepts exist.

## Competition

Suggested fields:

- Id
- Name
- Slug
- CountryCode
- LogoUrl
- Provider
- ProviderCompetitionId
- IsActive
- IsAvailableForPrediction
- DisplayOrder
- CreatedAt
- UpdatedAt

Initial active record: Premier League.

Other domestic leagues may be inserted/configured later but should not be exposed during V1.

## CompetitionSeason

Suggested fields:

- Id
- CompetitionId
- Name
- StartDate
- EndDate
- ProviderSeasonId
- Status
- IsCurrent
- CreatedAt
- UpdatedAt

Never hardcode season values in business logic.

## Matchweek

Suggested fields:

- Id
- CompetitionSeasonId
- Number
- Name
- StartDate
- EndDate
- Status

Matchweek is the primary recurring engagement unit.

## Fixture

Suggested fields:

- Id
- CompetitionSeasonId
- MatchweekId
- HomeTeamId
- AwayTeamId
- KickoffAtUtc
- Status
- HomeScore
- AwayScore
- ProviderFixtureId
- PredictionLockAtUtc

Use UTC internally.

## Team and season participation

A team should not be permanently owned by a competition because promotion/relegation changes membership.

Use a model equivalent to:

Team
- Id
- Name
- ShortName
- Slug
- LogoUrl
- provider mappings

SeasonTeam
- CompetitionSeasonId
- TeamId

Apply the same principle to players if needed.

## External provider IDs

External football API IDs must not be primary application IDs.

Use internal IDs plus provider mapping fields/tables so providers can change without corrupting domain identity.

## Football provider abstraction

If one does not exist, introduce an abstraction equivalent to:

`IFootballDataProvider`

Responsibilities may include:

- GetTeams
- GetPlayers
- GetFixtures
- GetResults
- GetStandings
- GetPlayerStatistics

Do not expose provider-specific contracts directly to UI components.

## Match prediction

Use or adapt the existing prediction entity so it references Fixture and user/guest identity.

Support initially:

- Home win
- Draw
- Away win
- Correct score

Scoring rules must live in application/domain logic, not controllers or frontend components.

Users and pundits must use the same scoring engine where prediction types are equivalent.

## Season predictions

Replace World Cup-specific tournament prediction columns with configurable definitions.

Suggested definitions for Premier League:

- League winner
- Top four
- Relegated teams
- Golden Boot
- Most assists
- Golden Glove
- Player of the Season
- Young Player of the Season
- Surprise team

Model these using a configurable definition system rather than hardcoded columns.

## Background jobs

Generalize jobs to operate by CompetitionId/SeasonId even while only Premier League is active.

Likely jobs:

- SyncTeams
- SyncPlayers
- SyncFixtures
- SyncResults
- SyncStandings
- SyncPlayerStats
- SyncNews
- SyncYouTubeContent
- SyncPunditSources
- ExtractPunditPredictions
- ScoreUserPredictions
- ScorePunditPredictions
- GenerateReceipts
- RecalculateLeaderboards

All synchronization/scoring jobs must be idempotent.
