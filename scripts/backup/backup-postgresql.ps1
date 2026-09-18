param([Parameter(Mandatory=$true)][string]$ConnectionString,[string]$BackupRoot=".\backups")
$ErrorActionPreference="Stop"
if (-not (Get-Command pg_dump -ErrorAction SilentlyContinue)) { throw "pg_dump was not found." }
$stamp=(Get-Date).ToUniversalTime().ToString("yyyyMMddTHHmmssZ"); $dest=Join-Path $BackupRoot ("postgresql\"+$stamp)
New-Item -ItemType Directory -Force $dest | Out-Null
& pg_dump $ConnectionString --format=custom --file=(Join-Path $dest "milansetu.dump"); if($LASTEXITCODE -ne 0){throw "PostgreSQL backup failed."}
& pg_dump $ConnectionString --format=plain --no-owner --no-privileges --file=(Join-Path $dest "milansetu.sql"); if($LASTEXITCODE -ne 0){throw "PostgreSQL SQL backup failed."}
Get-FileHash (Join-Path $dest "milansetu.dump"),(Join-Path $dest "milansetu.sql") -Algorithm SHA256 | Out-File (Join-Path $dest "SHA256SUMS.txt")
Write-Host "Backup created: $dest"
