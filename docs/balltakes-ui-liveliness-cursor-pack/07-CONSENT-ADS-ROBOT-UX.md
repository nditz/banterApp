# Consent, AdSense and Robot/Security UX

## Consent
Preserve the current architectural separation between essential terms/session handling and optional advertising consent.
- no dark patterns
- Allow and decline must be understandable
- consent choice must be revisitable
- no AdSense load before required consent
- accessibility: focus trap, keyboard navigation, labels and readable legal links

## Ads
Use a reusable `AdSlot` abstraction with named placements. The slot must collapse when:
- consent is absent
- AdSense is unavailable
- inventory does not fill after the integration's supported lifecycle
- the placement is disabled

Never leave large blank `ADVERTISEMENT` rectangles.

Ads must not interrupt a fixture's prediction controls or split a single receipt/story comparison.

## Robot/security checks
Do not write application code intended to bypass CAPTCHA, bot protection or WAF challenges. Treat challenges as infrastructure/security behavior. Ensure normal users can recover gracefully after verification and that challenge pages do not create redirect loops.
