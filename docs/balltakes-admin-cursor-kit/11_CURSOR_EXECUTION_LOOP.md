# Cursor Execution Loop

Use this as an operational instruction while Cursor works.

## At every phase start

Cursor must:

1. read `docs/balltakes-admin/MASTER_PLAN.md`;
2. read `docs/balltakes-admin/PROGRESS.md`;
3. inspect previous phase changes;
4. identify the smallest coherent next unit of work;
5. check whether repository reality conflicts with the master assumptions.

If conflict exists:

- prefer repository-compatible migration;
- document rationale in `DECISIONS.md`;
- update `MASTER_PLAN.md` if needed;
- continue.

## During implementation

For each coherent unit:

1. implement backend/domain change;
2. implement frontend/UI change where required;
3. add tests;
4. validate authorization/privacy implications;
5. run relevant build/test/lint commands;
6. fix failures immediately when reasonably attributable to the change;
7. update progress.

Do not accumulate dozens of untested changes.

## Before a phase is complete

Cursor must answer:

- Is the code actually used?
- Is admin access enforced server-side?
- Did this introduce a secret into client code/logging?
- Did this introduce a new analytics payload containing PII?
- Is a consent-gated script being loaded too early?
- Is the background job idempotent?
- Can the job be triggered arbitrarily?
- Does the cache have a TTL?
- Does the cache have an invalidation path?
- Can cached data leak between users?
- Does the implementation preserve guest/recovery-key flows?
- Does Supabase remain the identity provider?

Then run all relevant checks and record result in `PROGRESS.md`.

## Progress file format

Keep sections:

### Current Phase

### Completed

### In Progress

### Blocked

### Failed Checks

### Decisions Made

### Technical Debt

### Next Actions

## Stop conditions

Cursor may stop and request human action only for things such as:

- Supabase OAuth provider needs dashboard credentials;
- production secret needs to be supplied;
- Redis/service needs provisioning;
- a destructive migration cannot safely infer data mapping;
- legal/privacy policy text requires owner approval.

When blocked by external configuration, Cursor should still complete all code and documentation that can be completed without that configuration.

## Final repository review

Before declaring completion, search for:

- exposed `service_role` values;
- `NEXT_PUBLIC` secret misuse;
- unprotected `/admin` APIs;
- role checks implemented only in frontend;
- analytics calls containing tokens/emails/raw recovery keys;
- unbounded raw event queries;
- arbitrary background-job dispatch;
- cache keys containing PII;
- cache entries without TTL/invalidation;
- duplicated authentication/password code.

Fix accidental violations.

Then run the full shipping checklist in `09_TESTING_AND_DEFINITION_OF_DONE.md`.
