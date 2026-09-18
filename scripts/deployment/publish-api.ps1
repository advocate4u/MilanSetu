param(
  [string]$Project = "backend/MilanSetu.Api/MilanSetu.Api.csproj",
  [string]$Output = "artifacts/deployment/api"
)
$ErrorActionPreference = "Stop"
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw "dotnet SDK was not found." }
Remove-Item $Output -Recurse -Force -ErrorAction SilentlyContinue
dotnet restore $Project
dotnet publish $Project -c Release -o $Output --no-self-contained
Write-Host "API published to $Output"
