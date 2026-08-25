# Data Model & API Contracts

Cursor must adapt these concepts to the repository's actual naming conventions and avoid duplicate entities.

## Suggested supporting entities

### AnalyticsEvent (if using first-party event storage)

- `Id`
- `EventName`
- `OccurredAtUtc`
- `AnonymousSessionId` nullable
- `UserId` nullable
- `Feature`
- `PropertiesJson` sanitized/minimal
- `AppVersion` nullable
- `Environment`

Consider partitioning/retention implications before allowing this table to grow indefinitely.

If using an external analytics platform, an internal raw-event table may not be necessary. Still maintain a controlled event catalog in code.

### UserConsentPreference

- `UserId`
- `ConsentVersion`
- `AnalyticsAllowed`
- `MarketingAllowed`
- `UpdatedAtUtc`

### AdminAuditLog

- `Id`
- `AdminUserId`
- `Action`
- `TargetType`
- `TargetId`
- `MetadataJson`
- `OccurredAtUtc`
- `CorrelationId`

### BackgroundJobExecution

Only if the existing job framework does not already provide sufficient persistent history.

- `Id`
- `JobKey`
- `TriggerType`
- `TriggeredByUserId` nullable
- `EnqueuedAtUtc`
- `StartedAtUtc` nullable
- `CompletedAtUtc` nullable
- `Status`
- `Summary`
- `ErrorCode` nullable
- `ErrorSummary` nullable
- `CorrelationId`

### UserRole

Only if an equivalent role system does not already exist.

- `UserId`
- `Role`
- `CreatedAtUtc`
- `CreatedByUserId`

## Admin API surface

Use repository conventions; examples are conceptual.

### Overview

`GET /api/admin/overview?from=&to=`

Returns summarized KPI cards and health state.

### Users

`GET /api/admin/users?page=&pageSize=&search=`

`GET /api/admin/users/{userId}`

`POST /api/admin/users/{userId}/roles`

`DELETE /api/admin/users/{userId}/roles/{role}`

`DELETE /api/admin/users/{userId}` only if a complete safe deletion workflow is implemented.

### Analytics

`GET /api/admin/analytics/users`

`GET /api/admin/analytics/predictions`

`GET /api/admin/analytics/pundits`

`GET /api/admin/analytics/content`

Prefer aggregated endpoints rather than returning huge raw-event datasets to the browser.

### Jobs

`GET /api/admin/jobs`

`GET /api/admin/jobs/{jobKey}/executions`

`POST /api/admin/jobs/{jobKey}/run`

`POST /api/admin/jobs/executions/{executionId}/retry` if safe.

### Cache

`GET /api/admin/cache/groups`

`POST /api/admin/cache/groups/{groupKey}/invalidate`

`POST /api/admin/cache/groups/{groupKey}/refresh` if supported.

### Audit

`GET /api/admin/audit?page=&pageSize=&action=&adminUserId=`

## API rules

- admin endpoints require server-side authorization;
- enforce pagination bounds;
- validate date ranges;
- return aggregate data where possible;
- never return provider/service secrets;
- never accept arbitrary SQL, cache commands or background-job method names;
- use correlation IDs;
- use UTC timestamps;
- follow existing response/error conventions.
