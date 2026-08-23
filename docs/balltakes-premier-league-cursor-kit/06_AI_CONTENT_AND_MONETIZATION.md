# AI Content, Receipts and Monetization

## Core principle

BallTakes should not monetize generic prompt access. The differentiated asset is structured football context generated from user predictions, real results, rankings, friends and pundit outcomes.

## Receipt domain

A receipt represents an interesting, structured football event.

Examples:

- exact score;
- correct upset;
- terrible miss;
- winning streak;
- losing streak;
- private-league position jump/drop;
- beat a pundit;
- lost to a pundit;
- matchweek winner.

Suggested model:

Receipt
- Id
- UserId/GuestSessionId
- FixtureId nullable
- MatchweekId nullable
- ReceiptType
- ContextJson
- CreatedAt

Example context:

- user predicted Arsenal 3-1 Tottenham;
- final score Arsenal 3-1 Tottenham;
- exact score;
- 7 points earned;
- only 4% predicted exact score;
- tracked pundit predicted Tottenham win;
- user climbed from 8th to 2nd in a private league.

## Content Studio flow

Receipt -> Create Content -> choose format -> choose tone -> generate -> preview -> edit/regenerate -> export/share.

Initial formats:

- banter text;
- social caption;
- post-match roast;
- victory flex;
- meme copy;
- meme/image.

Later:

- GIF recommendations/generation;
- short video;
- animated receipt;
- voiceover.

## Tones

Examples:

- Savage
- Funny
- Dry
- Confident
- Self-roast
- Group-chat

## Provider-neutral AI

Do not make OpenAI-specific concepts the domain model.

Use abstractions equivalent to:

- ITextGenerationProvider
- IImageGenerationProvider
- IVideoGenerationProvider

Store provider/model metadata per generation for observability and cost accounting.

## GeneratedContent

Suggested fields:

- Id
- UserId nullable
- GuestSessionId nullable
- FixtureId nullable
- PredictionId nullable
- ReceiptId nullable
- ContentType
- PromptTemplateId nullable
- Provider
- ProviderModel
- Status
- InputContext
- OutputText nullable
- AssetUrl nullable
- EstimatedCost
- CreditsConsumed
- CreatedAt

## Sharing

V1 should prioritize reliable export over direct social publishing integrations:

- copy caption;
- download image;
- download meme;
- Web Share API where supported;
- downloadable media assets.

Direct TikTok/X/Instagram publishing can be a later phase.

## Credits

Use a transaction ledger, not only a mutable user balance.

CreditTransaction examples:

- SubscriptionGrant
- Purchase
- GenerationUsage
- Refund
- Promotion
- AdminAdjustment

A cached balance is acceptable if the ledger remains authoritative.

## Stripe

Integrate Stripe for:

- subscriptions;
- checkout;
- billing portal;
- webhooks;
- optional one-off credit purchases.

Webhook processing must be idempotent. Store processed event IDs.

The server, not the browser, determines successful entitlement changes.

## AI cost control

Track:

- provider;
- model;
- input/output token use where available;
- image generation count;
- video duration when applicable;
- estimated cost;
- user credits consumed.

AI endpoints must enforce:

- valid user/guest context;
- entitlement/credit validation;
- rate limits;
- request-size limits;
- usage accounting.
