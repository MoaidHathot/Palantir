param(
    [Parameter(Mandatory)]
    [string]$NupkgPath
)

# Fix the .NET tool nupkg: rename the tools folder from
# net10.0-windows10.0.22621 to net10.0 so that 'dotnet tool install' works.

if (-not (Test-Path $NupkgPath)) {
    Write-Error "nupkg not found: $NupkgPath"
    exit 1
}

Add-Type -AssemblyName System.IO.Compression.FileSystem

$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) "palantir-pack-$([Guid]::NewGuid().ToString('N'))"

try {
    [System.IO.Compression.ZipFile]::ExtractToDirectory($NupkgPath, $tempDir)

    $toolsDir = Join-Path $tempDir 'tools'
    $wrongDir = Get-ChildItem $toolsDir -Directory | Where-Object { $_.Name -like 'net*-windows*' } | Select-Object -First 1

    if ($wrongDir) {
        $correctName = $wrongDir.Name -replace '-windows[^/\\]*', ''
        $correctDir = Join-Path $wrongDir.Parent.FullName $correctName
        Rename-Item $wrongDir.FullName $correctDir
        Remove-Item $NupkgPath
        [System.IO.Compression.ZipFile]::CreateFromDirectory($tempDir, $NupkgPath)
        Write-Host "Fixed nupkg tools folder: $($wrongDir.Name) -> $correctName"
    }
    else {
        Write-Host "No Windows-specific tools folder found, skipping fix."
    }
}
finally {
    if (Test-Path $tempDir) {
        Remove-Item $tempDir -Recurse -Force
    }
}
