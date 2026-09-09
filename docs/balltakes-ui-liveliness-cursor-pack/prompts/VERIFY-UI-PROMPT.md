Perform a critical verification pass of the latest Ball Takes frontend changes.

Do not make cosmetic changes until you have first documented failures.

Verify against `checklists/UI-ACCEPTANCE.md` and the relevant acceptance criteria in `plans/UI-IMPLEMENTATION-BACKLOG.md`.

Check:
- product hierarchy
- Studio prominence
- homepage liveliness
- data states
- navigation
- responsive behavior at 375/768/1440
- keyboard/focus accessibility
- contrast
- reduced motion
- media lazy loading/layout shift
- AdSlot collapse behavior where applicable
- build/typecheck/lint/tests

Create or update `plans/UI-VERIFICATION.md` with PASS/FAIL and exact files/components needing remediation.

Fix only failures that are clearly in scope for the completed phase, then repeat verification once.
