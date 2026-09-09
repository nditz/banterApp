# AdSense & Consent

## Goal
Replace non-functional ad placeholders with a robust reusable ad system.

## Architecture
Create/reuse one `AdSlot` abstraction rather than page-specific ad markup.

Suggested placement keys:
- home-feed-1
- home-feed-2
- matchweek-between-fixtures
- banter-feed
- league-standings
- table-bottom

## Required Behavior
AdSlot should handle:
- consent status;
- script availability;
- ad request;
- responsive sizing;
- no-fill;
- errors;
- route changes;
- duplicate initialization prevention.

## Layout Rule
If an ad is unavailable or not allowed, collapse the slot so the user does not see a dead placeholder.

## Privacy
Audit current consent implementation against applicable GDPR/ePrivacy requirements.
Do not load advertising/tracking before the required consent state.

## Analytics
Track placement performance without introducing privacy-invasive custom tracking.

