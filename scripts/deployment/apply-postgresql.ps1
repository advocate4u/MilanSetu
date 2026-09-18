param(
  [Parameter(Mandatory=$true)][string]$ConnectionString,
  [Parameter(Mandatory=$true)][string]$MigrationSql
)
$ErrorActionPreference = "Stop"
if (-not (Get-Command psql -ErrorAction SilentlyContinue)) { throw "psql was not found. Install PostgreSQL client tools and retry." }
if (-not (Test-Path $MigrationSql)) { throw "Migration SQL not found: $MigrationSql" }
Write-Host "Applying PostgreSQL migration: $MigrationSql"
& psql "$ConnectionString" -v ON_ERROR_STOP=1 -f $MigrationSql
if ($LASTEXITCODE -ne 0) { throw "PostgreSQL migration failed." }
Write-Host "PostgreSQL migration completed successfully."
