# Product Scope and Business Model

## Product statement

BallTakes is a football prediction and AI banter platform where supporters put their football knowledge on record, compete with friends, compare themselves with professional pundits/social analysts, and convert outcomes into shareable content.

## Premier League V1

Premier League is the only active football competition for the first rollout.

The UI should feel intentionally Premier League-focused rather than presenting an unfinished multi-league selector.

The database and APIs should nevertheless use competition/season identifiers rather than globally assuming Premier League.

## Future domestic-league expansion

After validating Premier League engagement, support additional domestic club leagues such as:

- La Liga
- Serie A
- Bundesliga
- Ligue 1
- Eredivisie
- other similar club leagues

At that later stage, introduce user competition preferences and multi-league feed/prediction experiences.

## Deferred scope

International competitions and knockout/cup competitions will be handled under a separate design plan.

Explicitly defer:

- World Cup
- Euros
- AFCON
- Copa America
- international group stages
- tournament brackets
- FA Cup progression
- Carabao Cup progression
- Champions League knockout progression

Preserve World Cup history where reasonable, but do not let tournament abstractions dictate the new league-focused core.

## Core user value

### Prediction
Users predict fixture outcomes and optionally exact scores.

### Competition with friends
Users join private prediction leagues and compare matchweek and season performance.

### Competition with pundits
BallTakes tracks selected pundits and social analysts, stores the source of each prediction, scores them using the same rules as users, and allows direct comparison.

### AI receipts and banter
Interesting outcomes become structured receipts, for example:

- exact score;
- terrible miss;
- winning streak;
- losing streak;
- beat a pundit;
- lost to a pundit;
- jumped several private-league positions;
- won a matchweek.

These receipts become high-value context for AI content generation.

## Monetization hypothesis

Basic prediction participation remains free.

Revenue primarily comes from premium AI-content creation and advanced creator features.

Potential tiers:

### Free
- predictions;
- private leagues;
- pundit comparisons;
- football feed/news;
- limited AI text/content generation.

### Pro
- larger AI credit allowance;
- premium meme/image generation;
- advanced stats;
- more content history and templates.

### Creator
- larger generation allowance;
- advanced images;
- later short-form video and creator export capabilities.

Keep exact prices configurable.

## Product metrics

Prepare analytics for:

- anonymous sessions started;
- prediction conversion;
- predictions per matchweek;
- next-matchweek return rate;
- private leagues created/joined;
- guest-to-account conversion;
- pundit comparison views;
- receipts generated;
- AI generations;
- content export/share rate;
- free-to-paid conversion;
- AI cost per active/paid user.

The key retention question is: "How many users return for the next matchweek?"
