# Live Site UI Audit - Ball Takes

Audit date: 2026-09-09
Site: balltakes.com

## Executive assessment
The site has a strong product idea and recognizable black/green identity, but it currently reads more like a product specification rendered as a website than a finished football-content platform.

The biggest UI issue is not lack of decorative effects. It is that the most exciting product concepts are visually subordinate to explanatory copy, while many important product areas have insufficient content density, empty states, or no clear next action.

## Highest-impact findings

### 1. Homepage is explanation-heavy
The homepage currently begins with an 8-step quick tour before the user experiences the real product. The user should see live football energy within the first viewport.

Recommendation:
- Collapse onboarding to 3 steps maximum: Predict, Compare, Create.
- Put a live Banter/Receipts timeline directly under the hero.
- Show at least one real fixture card, one pundit-vs-user example, one meme/GIF card, and one Studio CTA before extended explanation.

### 2. Studio is strategically central but visually underdeveloped
The current Studio page is effectively a title, description, prediction breakdown promise, and 'Video content - coming soon'. That presentation dramatically undersells the intended product.

Recommendation:
Turn Studio into a workspace with:
- Story sources
- User vs pundit receipts
- Content type picker
- Tone/energy picker
- Generated content-pack preview
- Copy/export actions
- Recent creations/history

Never start Studio with an empty prompt box.

### 3. The site lacks a living content rhythm
Many sections are visually similar and text-heavy. Football products need time, motion, conflict, reactions, scores, faces/crests, and changing states.

Recommendation:
Create a mixed-format timeline containing:
- Meme/GIF
- Pundit quote receipt
- User-vs-pundit result
- Exact-score hero
- 'This aged badly' card
- Community take
- Trending story
- Matchday pulse/stat

Vary layout while keeping shared visual tokens.

### 4. Matchweek currently has no visible fixtures
A product centered on predictions cannot visually recover from an empty fixture state. Treat fixture loading/failure as a designed state, not blank space.

Recommendation:
- Skeletons during load
- Explicit error/retry state
- Upcoming matchweek header
- Team crests + kickoff + pick controls
- Progress: `7/10 picks locked`
- Sticky mobile action for unfinished picks

### 5. Empty/loading states undermine perceived quality
Current examples include no receipts, zero picks, empty current matchweek, and loading season awards.

Recommendation:
Every asynchronous surface needs four designed states:
1. loading
2. loaded
3. empty
4. error

No section should silently collapse or render only a heading.

### 6. Navigation does not emphasize the creation loop
Recommended primary IA:
- Predict
- Banter
- Studio
- Leagues
- Profile/Aura

Studio should receive a stronger visual treatment, particularly on mobile.

### 7. Too little football imagery and identity
Use football visual language as structured data, not decoration:
- Club crests
- Match scorelines
- kickoff countdowns
- pundit avatars (only where licensed/appropriate)
- compact form pills
- league position
- player headshots only where rights/data source permits

Green remains the Ball Takes accent. Club colors should be contextual accents only.

### 8. Ad placeholders must disappear when not filled
AdSense should be rendered via a single shared `AdSlot` abstraction. If consent is missing, the ad fails, or no inventory fills, collapse the slot with no dead rectangle.

### 9. Footer weight is too high relative to product content
Keep required legal copy, but reduce visual prominence on product pages. Use compact summary + links with detailed legal text on policy pages where appropriate.

## Core design principle
**The shell is clean; the football and banter are chaotic.**

Avoid making every surface neon, glowing, gradient-heavy, or highly rounded. Let memes, GIFs, scores, pundit clashes and receipts create the energy.
