# Critical Audit Framework

Before implementation, Cursor must perform a full repository and production-behavior audit.

## Audit Categories

### A. Production Integrity
Check whether the live application currently has:
- missing fixtures;
- missing current matchweek;
- empty standings;
- stale team/player data;
- failing background jobs;
- empty banter feed;
- missing prediction persistence;
- result settlement failures;
- missing Aura updates;
- silent API failures;
- broken AdSense placeholders;
- empty states with no explanation.

### B. Legacy / World Cup Residue
Search the entire repository for:
- World Cup
- WC2026
- FIFA
- Tournament
- Knockout
- Bracket
- Group Stage
- Player of the Tournament
- Golden Ball
- national team assumptions
- tournament-only scoring
- matchday terminology
- obsolete navigation

For each occurrence classify:
- KEEP
- GENERALISE
- REPLACE FOR PREMIER LEAGUE
- DELETE

Do not blindly replace text. Identify business logic coupled to the old model.

### C. Product Coherence
For each route/page answer:
- What user problem does it solve?
- Does it feed Studio?
- Does it support retention?
- Is it commodity football information available elsewhere?
- Should it be primary, secondary, merged, contextual or removed?

### D. Studio Readiness
Verify whether Studio can access structured context from:
- predictions;
- pundit takes;
- match results;
- player stats;
- team stats;
- standings;
- league rank;
- Aura movement;
- RSS/news;
- YouTube/transcripts;
- historical receipts;
- memes/GIFs.

### E. UX and Visual Quality
Audit:
- page hierarchy;
- navigation;
- spacing;
- typography;
- responsiveness;
- empty states;
- loading states;
- error states;
- mobile widths;
- duplicate components;
- one-off Tailwind values;
- inconsistent radii;
- excessive borders/green accents.

### F. SEO / Public Experience
Check:
- metadata consistency;
- stale World Cup descriptions;
- canonical URLs;
- sitemap;
- robots;
- page titles;
- public pages that appear unfinished;
- structured data if relevant.

## Required Audit Output
Create `plans/balltakes-remediation/AUDIT-RESULTS.md` containing:
- current architecture summary;
- route inventory;
- background job inventory;
- data source inventory;
- feature inventory;
- P0/P1/P2/P3 gaps;
- dependencies;
- risks;
- recommended implementation delta.

No production code changes are allowed before this report exists.

