param([Parameter(Mandatory=$true)][string]$Server,[Parameter(Mandatory=$true)][string]$User,[Parameter(Mandatory=$true)][string]$Database,[string]$BackupRoot=".\backups")
$ErrorActionPreference="Stop"
if (-not (Get-Command mysqldump -ErrorAction SilentlyContinue)) { throw "mysqldump was not found." }
$password=Read-Host "Database password" -AsSecureString; $ptr=[Runtime.InteropServices.Marshal]::SecureStringToBSTR($password)
try{$plain=[Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr)}finally{[Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr)}
$stamp=(Get-Date).ToUniversalTime().ToString("yyyyMMddTHHmmssZ"); $dest=Join-Path $BackupRoot ("mysql\"+$stamp); New-Item -ItemType Directory -Force $dest | Out-Null
$env:MYSQL_PWD=$plain
try{& mysqldump --host=$Server --user=$User --single-transaction --routines --triggers --events --hex-blob $Database | Out-File (Join-Path $dest "milansetu.sql") -Encoding utf8;if($LASTEXITCODE -ne 0){throw "MySQL backup failed."}}finally{Remove-Item Env:MYSQL_PWD -ErrorAction SilentlyContinue}
Get-FileHash (Join-Path $dest "milansetu.sql") -Algorithm SHA256 | Out-File (Join-Path $dest "SHA256SUMS.txt")
Write-Host "Backup created: $dest"
