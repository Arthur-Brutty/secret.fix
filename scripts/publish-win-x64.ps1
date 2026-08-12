$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot "src/SecretFix.App/SecretFix.App.csproj"

dotnet restore $project
dotnet publish $project -c Release -r win-x64 --self-contained true
