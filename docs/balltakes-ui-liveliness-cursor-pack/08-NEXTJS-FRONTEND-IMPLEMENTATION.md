# Next.js Frontend Implementation Guidance

Cursor must inspect the installed Next.js version, router type, styling system, UI library and package.json before introducing dependencies.

## Principles
- Preserve App Router/Pages Router patterns already used.
- Prefer Server Components for data-heavy/static shells where current architecture allows.
- Use Client Components only for interaction-heavy controls.
- Keep third-party scripts such as AdSense isolated and lifecycle-safe.
- Use `next/image` for appropriate images and optimize remote image domains deliberately.
- Avoid creating large monolithic page components.
- Reuse design primitives and semantic variants.
- Keep accessibility intact: keyboard, focus, labels, contrast, reduced motion.
- Prevent layout shift for known media dimensions.

## Performance targets
- fast LCP on homepage
- no persistent blank ad slots
- lazy-load below-fold GIF/media
- pause animated media when offscreen where practical
- avoid hydrating static feed chrome unnecessarily
- use skeletons instead of spinner-only loading

## Component layers
1. primitives
2. football entities (TeamBadge, Scoreline, MatchClock)
3. product components (FixtureCard, ReceiptCard)
4. composition sections
5. pages
