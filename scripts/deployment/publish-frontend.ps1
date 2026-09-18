param([string]$Output = "artifacts/deployment/web")
$ErrorActionPreference = "Stop"
if (-not (Get-Command npm -ErrorAction SilentlyContinue)) { throw "npm was not found." }
Push-Location frontend
try {
  npm ci
  npm run build
  Pop-Location
  Remove-Item $Output -Recurse -Force -ErrorAction SilentlyContinue
  New-Item -ItemType Directory -Force $Output | Out-Null
  Copy-Item frontend/dist/* $Output -Recurse -Force
  Write-Host "Frontend published to $Output"
} catch { Pop-Location; throw }
