# Next.js Frontend Implementation Guidance

Cursor must inspect the actual repository before assuming App Router, Pages Router, Tailwind, shadcn, Framer Motion/Motion or any version-specific API.

## Architecture rules
- Prefer Server Components for initial read-heavy content if the installed Next.js architecture supports them.
- Add Client Components only where interaction/browser APIs require them.
- Keep media/feed payloads bounded and paginated.
- Use `next/image` where appropriate and correctly configure remote image hosts.
- Use framework-native metadata/SEO capabilities for public content.
- Avoid client waterfalls for homepage feed + fixtures + user summary; fetch concurrently where possible.
- Do not import a new UI framework merely to redesign the site.
- Do not add an animation dependency if an installed solution or CSS is sufficient.

## Component targets
Likely shared primitives:
`PageContainer`, `SectionHeader`, `Button`, `IconButton`, `Card`, `Badge`, `Avatar`, `EmptyState`, `ErrorState`, `Skeleton`, `FreshnessBadge`, `TeamIdentity`, `FixtureCard`, `PredictionSelector`, `PunditIdentity`, `ReceiptComparison`, `FeedCardShell`, `StudioStoryCard`, `ContentPackSection`, `AdSlot`.

## Performance budget mindset
- reserve media aspect ratios
- lazy load below-fold media
- avoid huge GIF payloads when video/webp alternatives are available from provider
- no hydration of static decorative sections solely for animation
- measure Core Web Vitals before/after major homepage changes
