#!/usr/bin/env bash
set -euo pipefail
: "${DATABASE_URL:?Set DATABASE_URL to the PostgreSQL connection string}"
: "${MIGRATION_SQL:?Set MIGRATION_SQL to the generated SQL file}"
command -v psql >/dev/null || { echo "psql is required." >&2; exit 1; }
test -s "$MIGRATION_SQL" || { echo "Migration SQL not found or empty: $MIGRATION_SQL" >&2; exit 1; }
echo "Applying PostgreSQL migration: $MIGRATION_SQL"
psql "$DATABASE_URL" -v ON_ERROR_STOP=1 -f "$MIGRATION_SQL"
echo "PostgreSQL migration completed successfully."
