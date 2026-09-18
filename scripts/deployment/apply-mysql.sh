#!/usr/bin/env bash
set -euo pipefail
: "${MYSQL_HOST:?Set MYSQL_HOST}"
: "${MYSQL_USER:?Set MYSQL_USER}"
: "${MYSQL_DATABASE:?Set MYSQL_DATABASE}"
: "${MIGRATION_SQL:?Set MIGRATION_SQL}"
command -v mysql >/dev/null || { echo "mysql client is required." >&2; exit 1; }
test -s "$MIGRATION_SQL" || { echo "Migration SQL not found or empty: $MIGRATION_SQL" >&2; exit 1; }
echo "Applying MySQL migration: $MIGRATION_SQL"
mysql --protocol=tcp --host="$MYSQL_HOST" --user="$MYSQL_USER" --database="$MYSQL_DATABASE" --show-warnings < "$MIGRATION_SQL"
echo "MySQL migration completed successfully."
