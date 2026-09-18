param([string]$Source="backend\MilanSetu.Api\App_Data",[string]$BackupRoot=".\backups")
$ErrorActionPreference="Stop"
if (-not (Test-Path $Source -PathType Container)) { throw "App_Data directory not found: $Source" }
$stamp=(Get-Date).ToUniversalTime().ToString("yyyyMMddTHHmmssZ"); $dest=Join-Path $BackupRoot ("app-data\"+$stamp); New-Item -ItemType Directory -Force $dest | Out-Null
Compress-Archive -Path (Join-Path $Source "*") -DestinationPath (Join-Path $dest "App_Data.zip") -CompressionLevel Optimal
Get-FileHash (Join-Path $dest "App_Data.zip") -Algorithm SHA256 | Out-File (Join-Path $dest "SHA256SUMS.txt")
Write-Host "Backup created: $dest"
