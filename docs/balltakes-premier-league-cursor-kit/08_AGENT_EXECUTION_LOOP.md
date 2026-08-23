# Cursor Agent Execution and Self-Review Loop

## Persistent files

Inside the BallTakes repository maintain:

- `docs/balltakes-evolution/MASTER_PLAN.md`
- `docs/balltakes-evolution/PROGRESS.md`
- `docs/balltakes-evolution/DECISIONS.md`
- `docs/balltakes-evolution/TECH_DEBT.md`

## PROGRESS.md structure

Track:

- current phase;
- current objective;
- completed tasks;
- in-progress tasks;
- blocked tasks;
- failed tests/builds;
- newly discovered technical debt;
- decisions made;
- next executable task.

## Loop to run before each phase

1. Read `MASTER_PLAN.md`.
2. Read `PROGRESS.md`.
3. Read relevant domain-specific docs.
4. Inspect repository state, not only documentation.
5. Identify the smallest coherent set of changes that advances the current phase.

## Loop to run after each implementation batch

1. Inspect the diff.
2. Run backend build.
3. Run frontend build/type-check.
4. Run lint.
5. Run relevant tests.
6. Run migration validation when schema changed.
7. Compare work to current phase acceptance criteria.
8. Fix reasonable defects immediately.
9. Add tests for newly introduced logic.
10. Update `DECISIONS.md` for important architectural choices.
11. Update `TECH_DEBT.md` for intentionally deferred work.
12. Update `PROGRESS.md`.
13. Re-read `MASTER_PLAN.md` before starting the next phase.

## Anti-loop rule

Do not repeatedly rewrite plans without implementation progress.

Documentation should guide code changes, not replace them.

## Scope-control rule

Before implementing a newly discovered idea ask:

"Is this required for Premier League V1 or required to safely preserve existing functionality?"

If no:

- record it as deferred;
- do not implement it now;
- continue with the current phase.

## Decision rule

When repository reality conflicts with the plan:

1. understand the existing reason;
2. preserve working behavior where reasonable;
3. prefer migration to replacement;
4. choose the least disruptive extensible option;
5. record the decision;
6. continue autonomously.

## When Cursor may stop and ask

Only when:

- a secret/API credential is required;
- destructive migration/data loss is unavoidable;
- external manual configuration is a hard blocker;
- a product decision cannot be reasonably inferred.

Do not stop for ordinary naming, refactoring or implementation decisions.

## Final repository sweep

Before declaring Premier League V1 complete, search again for:

- WorldCup
- World Cup
- WC2026
- Bracket
- GroupStage
- TournamentPrediction

Classify remaining references as:

- valid historical feature;
- legacy unused code;
- accidental active dependency.

Fix accidental dependencies.

Then run all available:

- backend build;
- frontend production build;
- tests;
- lint;
- type checking;
- migration validation.

For every requirement in `MASTER_PLAN.md`, record one status:

- COMPLETE
- PARTIAL
- NOT IMPLEMENTED
- DEFERRED

Resolve reasonable PARTIAL items before finishing.
