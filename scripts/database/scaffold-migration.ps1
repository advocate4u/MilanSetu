param(
  [ValidateSet("PostgreSql", "MySQL")]
  [string]$Provider = "PostgreSql",
  [string]$Name = "SchemaUpdate"
)
$output = if ($Provider -eq "MySQL") { "Data/Migrations/MySql" } else { "Data/Migrations/PostgreSql" }
dotnet ef migrations add $Name --project backend/MilanSetu.Api --startup-project backend/MilanSetu.Api --context MilanSetuDbContext --provider $Provider --output-dir $output
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
