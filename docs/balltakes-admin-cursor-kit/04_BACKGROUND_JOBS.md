# Background Job Management

## Objective

Make background processing observable and safely controllable by admins.

Reuse the existing job framework rather than replacing it without reason.

## Job registry

Create a server-side registry/abstraction for jobs that may be shown or triggered from admin.

Conceptually:

`IAdminJobRegistry`

Each job definition should contain:

- stable `JobKey`;
- display name;
- description;
- category;
- whether manually triggerable;
- concurrency policy;
- expected timeout or runtime class;
- required permission;
- parameter schema if parameters are genuinely required.

Never accept an arbitrary class/method name from the browser.

## Candidate BallTakes jobs

Depending on the existing system:

- Sync Premier League teams
- Sync Premier League players
- Sync fixtures
- Sync results
- Sync standings
- Sync player statistics
- Sync RSS/news
- Sync YouTube content
- Sync pundit sources
- Extract pundit predictions
- Score user predictions
- Score pundit predictions
- Generate receipts
- Recalculate leaderboards
- Rebuild aggregate analytics
- Cleanup expired sessions/data according to retention policy

Use generic competition/season identifiers internally where the earlier Premier League architecture already supports them.

## Execution history

Persist or expose execution history with:

- execution ID;
- job key;
- trigger source: Scheduled / Admin / Retry / System;
- triggering admin user where applicable;
- enqueued time;
- started time;
- completed time;
- status;
- duration;
- summary;
- error category/message;
- correlation ID.

Avoid storing massive exception dumps indefinitely.

## Manual trigger API

Example:

`POST /api/admin/jobs/{jobKey}/run`

Backend flow:

1. authenticate admin;
2. authorize `Admin.Jobs.Execute` or role equivalent;
3. validate job key against allowlist;
4. validate optional parameters;
5. enforce concurrency/idempotency rule;
6. enqueue via existing background-job system;
7. write admin audit record;
8. return execution/job ID;
9. UI polls/subscribes to status using existing architecture.

Do not execute long-running jobs synchronously in the HTTP request.

## Concurrency

For jobs that must not overlap, use the facilities available in the current job framework or a distributed lock.

Examples:

- result sync;
- standings recalculation;
- pundit extraction for same source batch;
- leaderboard rebuild.

## Idempotency

Every sync/scoring job must tolerate retries.

Prefer provider external-ID upsert/mapping and deterministic scoring records.

Repeated execution must not create duplicate fixtures, predictions, scores, receipts or analytics aggregates.

## Failure handling

Classify failures where useful:

- transient provider error;
- authentication/configuration error;
- validation/data mapping error;
- rate limit;
- database error;
- unknown error.

Expose a sanitized admin summary and keep detailed diagnostics in application logging/observability.

## Admin safeguards

- confirmation for expensive jobs;
- permission check for manual execution;
- rate-limit repeated manual triggers;
- prevent job-trigger endpoint abuse;
- record who triggered what and when;
- do not allow users to provide arbitrary URLs/provider credentials as trigger parameters.
