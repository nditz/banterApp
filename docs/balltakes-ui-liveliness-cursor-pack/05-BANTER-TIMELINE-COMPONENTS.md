# Banter Timeline Component System

## Feed architecture
Use a discriminated content model and a renderer registry rather than a single mega-card with dozens of conditionals.

Suggested feed item types:
`meme`, `gif_reaction`, `pundit_receipt`, `user_pundit_compare`, `community_receipt`, `trending_take`, `match_event`, `studio_story`.

## Shared anatomy
Each card can draw from:
- eyebrow/type
- timestamp/freshness
- football context
- primary statement
- media
- before/after comparison
- attribution
- reaction/engagement controls if real
- Studio CTA

## Motion
Use motion to communicate state:
- receipt stamp on reveal
- score/result transition
- Aura/rank delta
- subtle card entrance on newly fetched content
- media crossfade
Respect `prefers-reduced-motion`. No perpetual bouncing CTAs.

## Feed behavior
- SSR/server-render initial useful content where practical.
- Client enhancement for refresh/reactions.
- Stable card heights where possible to avoid layout shift.
- Media lazy-loading below fold.
- Provide pause controls for any automatically moving carousel/ticker.
- Do not autoplay audio.
