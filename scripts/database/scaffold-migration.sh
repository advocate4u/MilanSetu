#!/usr/bin/env bash
set -euo pipefail
provider="${1:-PostgreSql}"
name="${2:-SchemaUpdate}"
case "${provider,,}" in
  postgresql|postgres|npgsql) output="Data/Migrations/PostgreSql" ;;
  mysql|mariadb) output="Data/Migrations/MySql" ;;
  *) echo "Unsupported provider: $provider"; exit 1 ;;
esac
dotnet ef migrations add "$name" --project backend/MilanSetu.Api --startup-project backend/MilanSetu.Api --context MilanSetuDbContext --provider "$provider" --output-dir "$output"
