# Ball Takes UI Remediation Pack — Live Re-Audit v2

This pack replaces the previous UI-liveliness plan. It reflects the current Ball Takes product direction after the latest live-site scan.

## Product truth
Ball Takes is a football-content creation product, not only a prediction game. The core loop is:

**Discover banter → Follow pundits → Predict → Compare user vs pundit vs reality → Create receipt/story → Studio → Export content pack → External AI/media tools → Social post → Return.**

Studio is a primary destination. Pundit comparison is a core differentiator. The homepage must demonstrate the entertainment/content output before asking a new visitor to understand the mechanics.

## How to use
1. Copy this folder under the repository `plans/` folder, replacing the previous UI pack.
2. Copy the included `.cursor/` directory to the repository root. Merge it with an existing `.cursor/` directory rather than deleting unrelated rules/skills.
3. Start Cursor Agent with `prompts/MASTER-KICKOFF-PROMPT.md`.
4. The first run is audit-only. Review `plans/UI-AUDIT-RESULTS.md` and `plans/UI-IMPLEMENTATION-BACKLOG.md` before implementation.
5. Execute one phase at a time using `prompts/IMPLEMENT-PHASE-PROMPT.md` and verify each phase with `prompts/VERIFY-UI-PROMPT.md`.

## Non-negotiables
- Do not rebuild the app.
- Do not break existing backend/data contracts to achieve a visual redesign.
- Do not remove Studio or demote it.
- Do not hide missing data behind fake production content.
- Do not use animation, glow, gradients or green everywhere as a substitute for product energy.
- Design every async surface for loading, loaded, empty, stale and error states.
- Preserve GDPR/advertising consent architecture; improve its UX without weakening consent.
- Treat robot/security checks as production controls, not UI elements to bypass.
