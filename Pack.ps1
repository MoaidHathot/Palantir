param(
    [switch]$Push,
    [string]$ApiKey,
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$ProjectDir = Join-Path (Join-Path $PSScriptRoot "src") "Palantir"
$ProjectFile = Join-Path $ProjectDir "Palantir.csproj"

if (-not (Test-Path $ProjectFile)) {
    Write-Error "Project file not found: $ProjectFile"
    exit 1
}

# Clean previous artifacts
$binDir = Join-Path (Join-Path $ProjectDir "bin") $Configuration
if (Test-Path $binDir) {
    Write-Host "Cleaning $binDir..."
    Remove-Item $binDir -Recurse -Force
}

# Build
Write-Host "Building ($Configuration)..."
dotnet build $ProjectFile -c $Configuration --nologo
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Pack (the csproj has a post-pack target that fixes the nupkg tools folder)
Write-Host "Packing..."
dotnet pack $ProjectFile -c $Configuration --no-build --nologo
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Find the nupkg
$nupkg = Get-ChildItem (Join-Path (Join-Path $ProjectDir "bin") $Configuration) -Filter "*.nupkg" | Select-Object -First 1
if (-not $nupkg) {
    Write-Error "No .nupkg file found after pack."
    exit 1
}

Write-Host "Package created: $($nupkg.FullName)"

# Push
if ($Push) {
    if (-not $ApiKey) {
        $ApiKey = $env:NUGET_API_KEY
    }

    if (-not $ApiKey) {
        Write-Error "No API key provided. Use -ApiKey or set the NUGET_API_KEY environment variable."
        exit 1
    }

    Write-Host "Pushing to nuget.org..."
    dotnet nuget push $nupkg.FullName --api-key $ApiKey --source https://api.nuget.org/v3/index.json --skip-duplicate
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    Write-Host "Pushed successfully."
}
