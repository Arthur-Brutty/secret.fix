$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot "src/SecretFix.App/SecretFix.App.csproj"
$version = "v0.6"
$output = Join-Path $repoRoot "artifacts/secret-test-$version-win-x64"

& dotnet restore $project -r win-x64
if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed with exit code $LASTEXITCODE" }

& dotnet publish $project -c Release -r win-x64 --self-contained true --no-restore `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $output
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }

$publishedExe = Join-Path $output "secret.test-v0.6.exe"
if (-not (Test-Path -LiteralPath $publishedExe)) {
    throw "Published executable was not produced: $publishedExe"
}
