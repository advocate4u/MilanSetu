#!/usr/bin/env bash
set -euo pipefail
: "${DATABASE_URL:?Set DATABASE_URL}"
BACKUP_ROOT="${BACKUP_ROOT:-./backups}"
STAMP="$(date -u +%Y%m%dT%H%M%SZ)"
DEST="$BACKUP_ROOT/postgresql/$STAMP"
mkdir -p "$DEST"
command -v pg_dump >/dev/null || { echo "pg_dump is required." >&2; exit 1; }
pg_dump "$DATABASE_URL" --format=custom --file="$DEST/milansetu.dump"
pg_dump "$DATABASE_URL" --format=plain --no-owner --no-privileges --file="$DEST/milansetu.sql"
sha256sum "$DEST/milansetu.dump" "$DEST/milansetu.sql" > "$DEST/SHA256SUMS"
echo "Backup created: $DEST"
