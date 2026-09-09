# Ball Takes Product Remediation & Studio-Centric Evolution

## Purpose
This package is the implementation guide for evolving the existing Ball Takes application into a finished, coherent, production-ready football content platform.

The goal is **not** to rebuild the application. Cursor must first inspect the existing repository, identify what already exists, map it against this plan, and only then implement the missing or broken parts in controlled phases.

## Core Product Definition
Ball Takes is not simply a prediction game.

Its core loop is:

1. User follows teams and pundits.
2. User makes football predictions and takes.
3. Pundits make predictions or statements.
4. Real football results and events happen.
5. Ball Takes compares user vs pundit vs reality.
6. Ball Takes creates a permanent "receipt".
7. Banter and story engines identify interesting angles.
8. Studio turns those stories into reusable content packs.
9. Users take those content packs into external AI/media tools to create videos, podcasts, memes, captions, reels, shorts and social content.

## Product Principle
**Football keeps the score. Ball Takes keeps the receipts.**

## Non-Negotiable Engineering Rule
Do not rewrite working architecture merely to match this document.

For every phase:
- inspect existing implementation;
- document current state;
- identify reusable code;
- list gaps and risks;
- implement only the required delta;
- test;
- verify production behavior;
- update the implementation log.

## Order of Work
1. Repository audit
2. Production integrity
3. Football data reliability
4. Product/navigation cleanup
5. Prediction + pundit comparison
6. Receipt/story engine
7. Studio evolution
8. Banter timeline
9. Leagues/Aura retention
10. Ads/consent
11. Observability/admin
12. Visual polish
13. Final verification

## Required Cursor Behavior
Cursor must read every file in this package before changing code.

Start with:
- `01-PRODUCT-VISION.md`
- `02-CRITICAL-AUDIT-FRAMEWORK.md`
- `03-FEATURE-DECISIONS.md`
- `17-IMPLEMENTATION-PHASES.md`
- `cursor/MASTER-KICKOFF-PROMPT.md`

