#!/usr/bin/env bash
set -euo pipefail
: "${MYSQL_HOST:?Set MYSQL_HOST}"
: "${MYSQL_PORT:?Set MYSQL_PORT}"
: "${MYSQL_USER:?Set MYSQL_USER}"
: "${MYSQL_DATABASE:?Set MYSQL_DATABASE}"
: "${MYSQL_PASSWORD:?Set MYSQL_PASSWORD}"
BACKUP_ROOT="${BACKUP_ROOT:-./backups}"
STAMP="$(date -u +%Y%m%dT%H%M%SZ)"
DEST="$BACKUP_ROOT/mysql/$STAMP"
mkdir -p "$DEST"
command -v mysqldump >/dev/null || { echo "mysqldump is required." >&2; exit 1; }
MYSQL_PWD="$MYSQL_PASSWORD" mysqldump --host="$MYSQL_HOST" --port="$MYSQL_PORT" --user="$MYSQL_USER" --single-transaction --routines --triggers --events --hex-blob "$MYSQL_DATABASE" > "$DEST/milansetu.sql"
sha256sum "$DEST/milansetu.sql" > "$DEST/SHA256SUMS"
echo "Backup created: $DEST"
