
param([string]$ConfigPath = "$PSScriptRoot\backup-config.json")
$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Get-Cred([string]$Path) {
  if (-not (Test-Path -LiteralPath $Path)) { throw "Credential file not found: $Path" }
  return Import-Clixml -LiteralPath $Path
}
function FtpRequest([string]$Uri,[pscredential]$Cred,[string]$Method) {
  $r=[System.Net.FtpWebRequest]::Create($Uri)
  $r.Method=$Method; $r.Credentials=$Cred; $r.EnableSsl=$true
  $r.UsePassive=$true; $r.KeepAlive=$false; $r.Timeout=120000; $r.ReadWriteTimeout=120000
  return $r
}
function List-Ftp([string]$Uri,[pscredential]$Cred) {
  $r=FtpRequest $Uri $Cred ([System.Net.WebRequestMethods+Ftp]::ListDirectoryDetails)
  $resp=$r.GetResponse()
  try {
    $sr=[IO.StreamReader]::new($resp.GetResponseStream())
    try {$lines=@($sr.ReadToEnd() -split "\r?\n" | ? {$_.Trim()})} finally {$sr.Dispose()}
  } finally {$resp.Dispose()}
  foreach($line in $lines) {
    $x=$line.Trim(); $name=$null; $dir=$false
    if($x -match '^d[-rwx]{9}\s+\d+\s+\S+\s+\S+\s+\d+\s+\S+\s+\S+\s+(.+)$'){ $name=$Matches[1];$dir=$true }
    elseif($x -match '<DIR>\s+(.+)$'){ $name=$Matches[1];$dir=$true }
    elseif($x -match '^[-l][^\s]*\s+\S+\s+\S+\s+\d+\s+\S+\s+\S+\s+(.+)$'){ $name=$Matches[1] }
    else {$name=$x}
    if($name -and $name -notin @('.','..')){[pscustomobject]@{Name=$name;Directory=$dir}}
  }
}
function Download-Tree([string]$Uri,[string]$Local,[pscredential]$Cred) {
  New-Item -ItemType Directory -Force -Path $Local | Out-Null
  foreach($e in @(List-Ftp $Uri $Cred)) {
    $child=$Uri.TrimEnd('/')+'/'+[Uri]::EscapeDataString($e.Name)
    $target=Join-Path $Local $e.Name
    if($e.Directory){Download-Tree $child $target $Cred;continue}
    $r=FtpRequest $child $Cred ([System.Net.WebRequestMethods+Ftp]::DownloadFile)
    $resp=$r.GetResponse()
    try {
      $input=$resp.GetResponseStream()
      $output=[IO.File]::Open($target,[IO.FileMode]::Create)
      try{$input.CopyTo($output)}finally{$output.Dispose();$input.Dispose()}
    }finally{$resp.Dispose()}
  }
}
function Dump-MySql($Db,[pscredential]$Cred,[string]$Output) {
  if(-not (Get-Command mysqldump.exe -EA SilentlyContinue)){throw "mysqldump.exe not found on PATH."}
  $env:MYSQL_PWD=$Cred.GetNetworkCredential().Password
  try {
    & mysqldump.exe "--host=$($Db.Host)" "--port=$($Db.Port)" "--user=$($Cred.UserName)" --single-transaction --routines --triggers --events --hex-blob --set-gtid-purged=OFF $Db.Database | Out-File -LiteralPath $Output -Encoding utf8
    if($LASTEXITCODE -ne 0){throw "mysqldump failed with exit code $LASTEXITCODE"}
  }finally{Remove-Item Env:MYSQL_PWD -EA SilentlyContinue}
}

$config=Get-Content -LiteralPath $ConfigPath -Raw | ConvertFrom-Json
$root=[IO.Path]::GetFullPath($config.LocalBackupRoot)
$stamp=(Get-Date).ToUniversalTime().ToString("yyyyMMddTHHmmssZ")
$dest=Join-Path $root $stamp
New-Item -ItemType Directory -Force -Path $dest | Out-Null
$log=Join-Path $dest "backup.log"
Start-Transcript -Path $log | Out-Null
try {
  $ftpCred=Get-Cred $config.FtpCredentialFile
  $dbCred=Get-Cred $config.DatabaseCredentialFile
  $remote="ftp://$($config.Ftp.Host.TrimEnd('/'))/$($config.Ftp.RemoteRoot.Trim('/'))"
  Download-Tree $remote (Join-Path $dest "site-files") $ftpCred
  New-Item -ItemType Directory -Force -Path (Join-Path $dest "database") | Out-Null
  Dump-MySql $config.Database $dbCred (Join-Path $dest "database\milansetu.sql")
  Get-ChildItem $dest -File -Recurse | ? {$_.Name -ne "SHA256SUMS.txt"} | Get-FileHash -Algorithm SHA256 | % {"$($_.Hash)  $($_.Path.Substring($dest.Length+1))"} | Set-Content (Join-Path $dest "SHA256SUMS.txt")
  Set-Content (Join-Path $root "LATEST.txt") @($stamp,$dest)
  Write-Host "Backup completed: $dest"
}catch{Write-Error "BACKUP FAILED: $($_.Exception.Message)";exit 1}
finally{Stop-Transcript | Out-Null}
$keep=[Math]::Max(1,[int]$config.RetentionDays)
Get-ChildItem $root -Directory | ? {$_.Name -match '^\d{8}T\d{6}Z$'} | Sort-Object Name -Descending | Select-Object -Skip $keep | Remove-Item -Recurse -Force
