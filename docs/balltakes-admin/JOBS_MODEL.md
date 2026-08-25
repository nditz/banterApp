# Background Jobs Model

Largely pre-existing. This document records the model as it stands and the one addition
made by the admin core workstream.

## Framework

Hangfire, hosted in-process by the .NET API with two workers
(`backend/BanterApp.Api/Program.cs`). Recurring jobs are registered at startup by
`HangfireJobRegistration.RegisterRecurringJobs`, with intervals read from the
`BackgroundJobs` configuration section. `BackgroundJobs:Enabled` is the master switch.

## Registry

`backend/.../Integrations/Jobs/JobRegistry.cs` is the server-side allowlist. Each entry
is a `JobDefinition`:

| Field | Purpose |
| --- | --- |
| `Key` | stable public identifier, e.g. `score-sync` |
| `HangfireJobId` | internal recurring-job ID |
| `DisplayName`, `Description` | admin UI text |
| `DefaultSchedule` | cron expression, or null for manual-only |
| `CanRunManually` | admin trigger eligibility |
| `CanPause` | pause/resume eligibility |
| `IsStub` | not yet fully implemented |

The admin API accepts only a `Key` from this list. A class name, method name or
Hangfire ID supplied by the browser is never executed. An unknown key returns 404.

## Jobs

Sync: `score-sync`, `match-details-sync`, `standings-sync`, `news-ingest`,
`football.countries.sync`, `football.players.sync`, `football.player_stats.sync`,
`football.top_scorers.sync`, `football.top_assists.sync`,
`football.reference_data.full_sync`.

Content: `rss.sync`, `youtube.search.sync`, `youtube.metadata.sync`,
`youtube.transcript.sync`, `openai.opinion.extract`, `openai.banter.generate`,
`ai-reactions`.

Aggregation: `predictions.aggregate.refresh`.

Maintenance: `failed-items.retry` (stub), `stale-content.cleanup` (stub),
`analytics.retention.cleanup` (added by this workstream).

## Added: analytics retention cleanup

`analytics.retention.cleanup` runs daily at 03:30 UTC and deletes `analytics_events`
rows older than `Analytics:RawEventRetentionDays`. It is manually triggerable, pausable,
and reports the deleted row count through the normal `SyncRunTracker` history so the
existing `/admin/jobs` page shows it without modification.

Deletion is chunked so a large backlog cannot hold a long transaction open.

## Execution history

`SyncRunTracker` opens a `sync_runs` row on start and closes it on completion with
status, duration and item counts. Per-entity failures go to `sync_errors`. Unhandled
exceptions are captured by `HangfireErrorLoggingFilter` and deduplicated into `errors`
by fingerprint.

Admin reads this through `GET /api/admin/jobs/{jobKey}/runs` and
`GET /api/admin/jobs/{jobKey}/runs/{runId}`. Stack traces are only included when
`Admin:ExposeErrorDetail` is enabled.

## Manual trigger flow

1. Policy `"Admin"` authorizes the request.
2. `jobKey` is resolved against `JobRegistry`; unknown keys 404.
3. Rate limit `admin.jobs.run` applies, 5 per minute.
4. The job is enqueued through `IRecurringJobManager.Trigger`. Nothing runs
   synchronously inside the HTTP request.
5. An audit row is written.
6. The UI polls run history for the outcome.

## Known limitation

Hangfire uses `UseInMemoryStorage()`. Schedules, pause state and in-flight jobs do not
survive a process restart. Pause/enable state is separately persisted in
`job_registry_state`, so admin intent is durable even though Hangfire's own view is not.
Moving to `Hangfire.PostgreSql` is recorded in `TECH_DEBT.md`.
