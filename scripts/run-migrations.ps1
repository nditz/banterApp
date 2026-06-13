# Apply EF Core migrations to the database configured in appsettings.Development.json
# Usage: .\scripts\run-migrations.ps1

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path $PSScriptRoot -Parent
$projectPath = Join-Path $projectRoot "backend\BanterApp.Api\BanterApp.Api.csproj"
$devSettings = Join-Path $projectRoot "backend\BanterApp.Api\appsettings.Development.json"

if (-not (Test-Path $devSettings)) {
    Write-Error @"
appsettings.Development.json not found.

Copy the template and add your Supabase session pooler URL:
  copy backend\BanterApp.Api\appsettings.Development.json.example backend\BanterApp.Api\appsettings.Development.json

See docs/BACKEND-CONFIGURATION.md
"@
}

$env:ASPNETCORE_ENVIRONMENT = "Development"

Write-Host "Applying EF migrations (ConnectionStrings:DefaultConnection from appsettings.Development.json)..." -ForegroundColor Cyan
dotnet ef database update --project $projectPath
Write-Host "Done." -ForegroundColor Green
