# Wipe demo/mock data and trigger live ingest jobs (Development API only)
# Usage: .\scripts\reset-live-data.ps1

$ErrorActionPreference = "Stop"
$base = if ($env:API_BASE_URL) { $env:API_BASE_URL } else { "http://localhost:5000" }

Write-Host "Resetting demo data and triggering live ingest against $base" -ForegroundColor Cyan

try {
    $result = Invoke-RestMethod -Uri "$base/api/sync/refresh-all" -Method Post -TimeoutSec 30
    $result | ConvertTo-Json -Depth 5
    Write-Host ""
    Write-Host "Wait ~30 seconds for Hangfire jobs, then open:" -ForegroundColor Green
    Write-Host "  $base/api/health"
    Write-Host "  $base/api/matches/upcoming"
    Write-Host "  http://localhost:3000"
}
catch {
    Write-Host "FAILED: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Start the API first: .\scripts\run-api.ps1" -ForegroundColor Yellow
    exit 1
}
