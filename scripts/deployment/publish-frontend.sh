#!/usr/bin/env bash
set -euo pipefail
OUTPUT="${OUTPUT:-artifacts/deployment/web}"
cd frontend
npm ci
npm run build
cd ..
rm -rf "$OUTPUT"
mkdir -p "$OUTPUT"
cp -a frontend/dist/. "$OUTPUT/"
echo "Frontend published to $OUTPUT"
