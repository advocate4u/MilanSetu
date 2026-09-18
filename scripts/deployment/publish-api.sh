#!/usr/bin/env bash
set -euo pipefail
PROJECT="${PROJECT:-backend/MilanSetu.Api/MilanSetu.Api.csproj}"
OUTPUT="${OUTPUT:-artifacts/deployment/api}"
command -v dotnet >/dev/null || { echo "dotnet SDK is required." >&2; exit 1; }
rm -rf "$OUTPUT"
dotnet restore "$PROJECT"
dotnet publish "$PROJECT" -c Release -o "$OUTPUT" --no-self-contained
echo "API published to $OUTPUT"
