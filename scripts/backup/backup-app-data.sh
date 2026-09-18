#!/usr/bin/env bash
set -euo pipefail
SOURCE="${APP_DATA:-backend/MilanSetu.Api/App_Data}"
BACKUP_ROOT="${BACKUP_ROOT:-./backups}"
STAMP="$(date -u +%Y%m%dT%H%M%SZ)"
DEST="$BACKUP_ROOT/app-data/$STAMP"
test -d "$SOURCE" || { echo "App_Data directory not found: $SOURCE" >&2; exit 1; }
mkdir -p "$DEST"
tar -czf "$DEST/App_Data.tar.gz" -C "$SOURCE" .
sha256sum "$DEST/App_Data.tar.gz" > "$DEST/SHA256SUMS"
echo "Backup created: $DEST"
