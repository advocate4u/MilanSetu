#!/bin/sh
set -eu

# Keep the scanner's signature database updated in the background. The API starts
# immediately, but document approvals remain fail-closed until a clean scan is possible.
if command -v freshclam >/dev/null 2>&1; then
  freshclam --daemon --foreground --stdout --config-file=/app/freshclam.conf &
fi

exec dotnet /app/MilanSetu.Api.dll
