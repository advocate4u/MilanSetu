# GoDaddy to Local Windows Daily Backup

This is the Windows-PC pull backup for a GoDaddy Windows Hosting (Plesk) deployment.

## Daily flow

GoDaddy hosting -> FTPS site-file download -> local dated backup
GoDaddy MySQL -> mysqldump -> local SQL backup
Then SHA-256 checksums, log, and retention cleanup are performed locally.

Default retention is 30 daily backup sets.

## One-time setup

1. Install MySQL client tools on the Windows PC and verify:
   powershell
   mysqldump --version

2. From the repository run:
   powershell
   cd scripts\backup\godaddy
   .\setup-daily-backup.ps1

3. On first run, the example configuration is copied to backup-config.json. Edit:
   - LocalBackupRoot
   - Ftp.Host
   - Ftp.RemoteRoot (normally httpdocs for the primary GoDaddy Windows site)
   - Database.Host
   - Database.Port
   - Database.Database

4. Run setup-daily-backup.ps1 again. It stores FTP and database credentials using Windows Export-Clixml and registers the daily Task Scheduler job at 02:00.

5. Test immediately:
   Start-ScheduledTask -TaskName "MilanSetu-GoDaddy-Daily-Backup"
   Get-ScheduledTaskInfo -TaskName "MilanSetu-GoDaddy-Daily-Backup"

## Local result

Each run creates:
D:\MilanSetu-Backups\YYYYMMDDTHHMMSSZ\
  site-files\
  database\milansetu.sql
  SHA256SUMS.txt
  backup.log

The latest successful location is also written to LATEST.txt.

## Important GoDaddy database note

GoDaddy documents MySQL remote access for Windows Hosting/Plesk, but also states that direct database connections do not support SSL. The database dump in this client therefore uses the remote MySQL connection only when the hosting setup permits it. For sensitive production data, a Plesk-created database dump or Plesk backup copied to the PC is preferable where the hosting plan exposes that workflow.

Plesk supports scheduled backups that can include website content and databases. GoDaddy also documents exporting a database dump from Plesk.

## Security

- Do not commit backup-config.json.
- Do not commit credential XML files.
- Keep the backup drive encrypted/protected.
- Never upload private production backups to GitHub.
- Periodically perform a real restore test.
- Keep an additional backup copy on a different physical device/location when practical.

## Limitations

The Windows PC must be powered on and connected for the daily pull. This implementation backs up the configured FTP remote root and MySQL database; it does not depend on GoDaddy storing the backup on the PC.
