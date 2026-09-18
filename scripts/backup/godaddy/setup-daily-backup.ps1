
param(
  [string]$ConfigPath = "$PSScriptRoot\backup-config.json",
  [string]$TaskName = "MilanSetu-GoDaddy-Daily-Backup",
  [string]$Time = "02:00"
)
$ErrorActionPreference="Stop"
Set-StrictMode -Version Latest
if(-not (Test-Path $ConfigPath)){
  Copy-Item "$PSScriptRoot\backup-config.example.json" $ConfigPath
  Write-Host "Created $ConfigPath. Edit it, then run this script again."
  exit 0
}
$config=Get-Content $ConfigPath -Raw | ConvertFrom-Json
New-Item -ItemType Directory -Force -Path $config.LocalBackupRoot | Out-Null
if(-not (Test-Path $config.FtpCredentialFile)){
  Get-Credential -Message "GoDaddy FTPS credentials" | Export-Clixml $config.FtpCredentialFile
}
if(-not (Test-Path $config.DatabaseCredentialFile)){
  Get-Credential -Message "GoDaddy MySQL credentials" | Export-Clixml $config.DatabaseCredentialFile
}
$script=(Resolve-Path "$PSScriptRoot\backup-milansetu.ps1").Path
$argument='-NoProfile -ExecutionPolicy Bypass -File "'+$script+'" -ConfigPath "'+$ConfigPath+'"'
$action=New-ScheduledTaskAction -Execute "PowerShell.exe" -Argument $argument
$trigger=New-ScheduledTaskTrigger -Daily -At ([DateTime]::ParseExact($Time,"HH:mm",$null))
$principal=New-ScheduledTaskPrincipal -UserId ([Security.Principal.WindowsIdentity]::GetCurrent().Name) -LogonType InteractiveToken -RunLevel Limited
Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger $trigger -Principal $principal -Description "Daily local MilanSetu backup from GoDaddy." -Force | Out-Null
Write-Host "Installed $TaskName at $Time daily."
