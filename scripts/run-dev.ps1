$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot "src/SecretFix.App/SecretFix.App.csproj"

dotnet restore $project
dotnet run --project $project
