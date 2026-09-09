# Acceptance Criteria

## Product
- Homepage clearly communicates prediction → pundit comparison → receipts → Studio.
- Public homepage contains real/fallback banter timeline content.
- Studio is central and populated by structured stories, not a blank prompt.
- Users can follow pundits or the existing equivalent is clearly exposed.
- User vs pundit comparison is visible for relevant matches/takes.
- Receipts are persistent and reusable by Studio.

## Football Data
- Current matchweek resolves correctly.
- Fixtures display when expected.
- Results settle predictions.
- Standings display or fail gracefully.
- Data jobs are observable in admin.

## Ads
- Ad placeholders do not remain as empty dead rectangles.
- Consent is respected.
- AdSlot is reusable and centralized.

## UX
- No stale World Cup UI/copy remains in Premier League flows unless intentionally generalized.
- Mobile navigation is coherent.
- Loading, empty and error states are designed.
- No broken public route is knowingly shipped.

## Studio
- At least one complete receipt → content pack flow works end-to-end.
- Generated pack includes factual context and reusable output.
- Generation history is retained where architecture supports it.

## Engineering
- Existing architecture reused where sensible.
- Tests added for changed critical paths.
- No secrets committed.
- Implementation log updated after each phase.

