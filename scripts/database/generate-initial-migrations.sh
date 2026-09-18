#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
PROJECT="$ROOT/backend/MilanSetu.Api"
OUT="$ROOT/artifacts/initial-migrations"
rm -rf "$OUT"
mkdir -p "$OUT/postgresql" "$OUT/mysql"
dotnet tool install --global dotnet-ef --version 8.0.8 --ignore-failed-sources >/dev/null 2>&1 || true
export PATH="$PATH:$HOME/.dotnet/tools"
generate() {
  local provider="$1"; local connection="$2"; local work="/tmp/milansetu-ef-$provider"
  rm -rf "$work"; mkdir -p "$work"; cp -a "$PROJECT/." "$work/"; mkdir -p "$OUT/$provider"
  pushd "$work" >/dev/null
  dotnet restore MilanSetu.Api.csproj
  dotnet build MilanSetu.Api.csproj --configuration Release --no-restore
  dotnet ef migrations add InitialCreate --context MilanSetuDbContext --output-dir Migrations -- --provider="$provider" --connection="$connection"
  dotnet ef migrations script 0 InitialCreate --context MilanSetuDbContext --idempotent --output "$OUT/$provider/001_InitialCreate.sql" -- --provider="$provider" --connection="$connection"
  cp -a Migrations/. "$OUT/$provider/"
  popd >/dev/null
}
generate postgresql "Host=localhost;Port=5432;Database=milansetu;Username=milansetu;Password=ci"
generate mysql "Server=localhost;Port=3306;Database=milansetu;User=milansetu;Password=ci"
echo "Generated PostgreSQL and MySQL initial migration sets under artifacts/initial-migrations."
