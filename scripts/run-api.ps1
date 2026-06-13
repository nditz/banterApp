# Run BanterApp API from the correct project folder.
# Usage: .\scripts\run-api.ps1

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path $PSScriptRoot -Parent
$projectPath = Join-Path $projectRoot "backend\BanterApp.Api\BanterApp.Api.csproj"
$devSettings = Join-Path $projectRoot "backend\BanterApp.Api\appsettings.Development.json"
$port = 5000

if (-not (Test-Path $projectPath)) {
    Write-Error "Project not found: $projectPath"
}

if (-not (Test-Path $devSettings)) {
    Write-Host "Warning: appsettings.Development.json not found - API will use in-memory DB." -ForegroundColor Yellow
    Write-Host "  copy backend\BanterApp.Api\appsettings.Development.json.example backend\BanterApp.Api\appsettings.Development.json" -ForegroundColor Cyan
}

$listener = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue |
    Select-Object -First 1

if ($listener) {
    $proc = Get-Process -Id $listener.OwningProcess -ErrorAction SilentlyContinue
    Write-Host "Port $port is already in use by $($proc.ProcessName) (PID $($listener.OwningProcess))." -ForegroundColor Yellow
    Write-Host "Stop that process first, or only run one API instance:" -ForegroundColor Yellow
    Write-Host "  Stop-Process -Id $($listener.OwningProcess) -Force" -ForegroundColor Cyan
    exit 1
}

$env:ASPNETCORE_ENVIRONMENT = "Development"

Write-Host "Starting BanterApp API from $projectPath" -ForegroundColor Cyan
Write-Host "Config: appsettings.json + appsettings.Development.json (secrets, gitignored)" -ForegroundColor Gray
Write-Host "API will listen on http://localhost:$port" -ForegroundColor Gray
Write-Host ""

Set-Location (Split-Path $projectPath -Parent)
dotnet run --project $projectPath
