param(
  [ValidateSet('seed','rollback')]
  [string]$Action = 'seed'
)

$project = Join-Path $PSScriptRoot '..\Backend\Hrevolve.Web\Hrevolve.Web.csproj'

if ($Action -eq 'seed') {
  dotnet run --project $project -- --seed-demo
  exit $LASTEXITCODE
}

dotnet run --project $project -- --rollback-demo
exit $LASTEXITCODE
