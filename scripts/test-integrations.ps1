$ErrorActionPreference = "Stop"
$base = if ($env:API_BASE_URL) { $env:API_BASE_URL } else { "http://localhost:5000" }

function Show-Error($err) {
    Write-Host "FAILED: $($err.Exception.Message)"
    exit 1
}

Write-Host "Integration smoke tests against $base"
Write-Host ""

try {
    # Health
    $health = Invoke-RestMethod -Uri "$base/api/health" -TimeoutSec 15
    Write-Host "1. HEALTH OK          status=$($health.status) matches=$($health.database.matchCount)"

    # Sync status
    $sync = Invoke-RestMethod -Uri "$base/api/sync/status" -TimeoutSec 15
    Write-Host "2. SYNC STATUS OK     runs=$($sync.latestRuns.Count) mediaItems=$($sync.mediaItemCount) standings=$($sync.standingRowCount)"
    Write-Host "   YouTube configured: $($sync.providers.youtubeConfigured)"
    foreach ($fb in $sync.providers.fallbacks) {
        Write-Host "   Fallback $($fb.providerName): configured=$($fb.isConfigured)"
    }

    # Trigger score sync
    $trigger = Invoke-RestMethod -Uri "$base/api/sync/trigger/score-sync" -Method Post -TimeoutSec 15
    Write-Host "3. TRIGGER SCORE SYNC $($trigger.triggered)"

    Start-Sleep -Seconds 3

    $runs = Invoke-RestMethod -Uri "$base/api/sync/runs?limit=5" -TimeoutSec 15
    $latest = @($runs)[0]
    if ($latest) {
        Write-Host "4. LATEST SYNC RUN    job=$($latest.jobName) status=$($latest.status) created=$($latest.recordsCreated) updated=$($latest.recordsUpdated)"
    }

    Write-Host ""
    Write-Host "ALL INTEGRATION SMOKE TESTS PASSED"
} catch {
    Show-Error $_
}
