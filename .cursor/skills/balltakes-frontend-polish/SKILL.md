---
name: balltakes-frontend-polish
description: Improve and review the Ball Takes Next.js frontend while preserving its product architecture. Use for Ball Takes homepage, Banter feed, prediction/matchweek UI, pundit comparisons, receipts, Studio creator workflows, leagues, Aura, Season Calls, consent/ad surfaces, responsive design, visual polish, accessibility and frontend performance. Treat Studio and user-vs-pundit receipts as core product capabilities and prefer football/content-driven liveliness over decorative AI-dashboard styling.
---

# Ball Takes Frontend Polish

Read the current v2 UI plan before substantial UI work.

## Product model
Preserve this loop:
`Discover -> Follow pundits -> Predict -> Compare -> Receipt -> Banter -> Studio -> Export -> Share -> Return`.

## Workflow
1. Inspect existing code and dependencies before proposing implementation.
2. Identify user state: anonymous, new, returning.
3. Define page goal and primary CTA.
4. Reuse shared primitives before adding variants.
5. For async content implement loading, loaded, empty, stale and error states.
6. Verify mobile and desktop.
7. Run available lint/typecheck/tests/build.
8. Record changes and blockers.

## Visual rules
- Keep shell restrained; let football, media and receipts provide energy.
- Use Ball Takes green for action/selection/status, not every surface.
- Use club identity contextually.
- Avoid unnecessary gradients, glow, glassmorphism and giant rounded cards.
- Use motion for state changes, not decoration. Respect reduced motion.
- Never show blank sections because data failed.
- Never fabricate production content, pundit quotes or engagement counts.

## Studio
Treat Studio as a story-driven creator workspace, never a blank prompt page. Start from context Ball Takes already knows. Keep source/fact attribution visible in generated content packs.

## Security/privacy
Do not bypass CAPTCHA, bot/WAF protection or weaken consent. Optional advertising must remain optional where the existing consent architecture requires it.
