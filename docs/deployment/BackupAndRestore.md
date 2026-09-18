# MilanSetu Backup and Restore

Back up both the database and persistent user-uploaded files.

## Required backups
- PostgreSQL or MySQL/MariaDB database
- App_Data/profile-photos
- App_Data/verification-documents
- Any future persistent upload storage

Never commit production backups to GitHub. They can contain private profile information and verification documents.

## Scripts
- PostgreSQL: scripts/backup/backup-postgresql.sh or .ps1
- MySQL: scripts/backup/backup-mysql.sh or .ps1
- Files: scripts/backup/backup-app-data.sh or .ps1

Backups are timestamped in UTC and include SHA-256 checksums.

## Recommended policy
- Daily automated database backup.
- Daily persistent-file backup.
- Keep at least 7 daily and 4 weekly copies.
- Keep an additional copy outside the application server.
- Encrypt backups at rest and in transit.
- Restrict backup access.
- Test restoration periodically.
- Take an additional backup/snapshot immediately before schema migrations.
- Never store production credentials in repository scripts.

## Restore
PostgreSQL custom dump:
pg_restore --clean --if-exists --dbname="$DATABASE_URL" backups/postgresql/<timestamp>/milansetu.dump

MySQL:
mysql --host="$MYSQL_HOST" --user="$MYSQL_USER" --database="$MYSQL_DATABASE" < backups/mysql/<timestamp>/milansetu.sql

Restore App_Data from App_Data.tar.gz or App_Data.zip into the persistent application storage location.

## Recovery order
1. Preserve logs where possible.
2. Identify the latest verified backup.
3. Restore to staging first when feasible.
4. Restore the matching App_Data backup.
5. Apply required versioned schema migrations.
6. Deploy the matching application release.
7. Verify /api/health and /api/health/database.
8. Test login, profile, discovery, photos, messaging, contact sharing and SuperAdmin controls.

The repository intentionally does not upload production backups to GitHub Actions artifacts. Use encrypted off-server backup storage or the hosting provider's database backup facility.
