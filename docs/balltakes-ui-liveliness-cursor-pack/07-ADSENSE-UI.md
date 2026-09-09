# AdSense UI Integration

Create one shared AdSlot component. Do not scatter raw AdSense markup.

Responsibilities:
- consent awareness
- script readiness
- responsive sizing
- fill/no-fill handling
- error handling
- route transition safety
- duplicate initialization prevention
- collapse when unfilled
- analytics event hooks

Recommended placements:
- after homepage feed block, never above core hero CTA
- between matchweek fixture groups, not between every fixture
- below standings/context sections
- banter feed after several organic items

Never:
- reserve large blank rectangles
- obscure prediction controls
- insert ads inside Studio generation flow
