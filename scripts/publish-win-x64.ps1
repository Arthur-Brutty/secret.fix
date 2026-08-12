$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot "src/SecretFix.App/SecretFix.App.csproj"
$version = "v0.3"
$output = Join-Path $repoRoot "artifacts/secret-fix-$version-win-x64"

dotnet restore $project
dotnet publish $project -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $output
Rename-Item -Path (Join-Path $output "SecretFix.exe") -NewName "secret-fix-$version.exe" -Force
