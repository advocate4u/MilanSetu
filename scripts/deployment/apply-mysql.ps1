param(
  [Parameter(Mandatory=$true)][string]$Server,
  [Parameter(Mandatory=$true)][string]$User,
  [Parameter(Mandatory=$true)][string]$Database,
  [Parameter(Mandatory=$true)][string]$MigrationSql
)
$ErrorActionPreference = "Stop"
if (-not (Get-Command mysql -ErrorAction SilentlyContinue)) { throw "mysql client was not found. Install MySQL client tools and retry." }
if (-not (Test-Path $MigrationSql)) { throw "Migration SQL not found: $MigrationSql" }
$Password = Read-Host "Database password" -AsSecureString
$ptr=[Runtime.InteropServices.Marshal]::SecureStringToBSTR($Password)
try { $plain=[Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr) } finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr) }
$env:MYSQL_PWD=$plain
try {
  Write-Host "Applying MySQL migration: $MigrationSql"
  & mysql --protocol=tcp --host=$Server --user=$User --database=$Database --show-warnings < $MigrationSql
  if ($LASTEXITCODE -ne 0) { throw "MySQL migration failed." }
} finally { Remove-Item Env:MYSQL_PWD -ErrorAction SilentlyContinue }
Write-Host "MySQL migration completed successfully."
