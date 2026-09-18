# MilanSetu Backup Manager

Windows desktop companion for the GoDaddy-to-local backup scripts.

## Modes
- PowerShell: the original automation engine and fallback.
- EXE: `MilanSetuBackupManager.exe` for Backup Now, daily schedule setup, folder access and the local dashboard.
- Webpage: the EXE serves a local-only dashboard on `http://127.0.0.1:51789/`.

The dashboard binds only to loopback and never exposes database credentials or backup files.

## Build
Use Visual Studio or .NET 8 SDK on Windows:
`dotnet publish tools/MilanSetuBackupManager/MilanSetuBackupManager.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true`

The published EXE can be distributed with the `godaddy` script folder beside it.
