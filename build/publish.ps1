<#
.SYNOPSIS
    Publishes Rojan.Desktop.Shell as a self-contained, single-file win-x64
    build and packages it as a ZIP artifact.

.DESCRIPTION
    Per the approved Phase 09 Release Engineering plan §7/§8: ZIP-of-a-
    published-build only, no installer (MSIX/MSI/WiX/Squirrel) - that
    remains a distinct future decision requiring its own approval, since
    every installer option costs a new dependency this phase deliberately
    avoids. Self-contained + single-file so the app runs on a machine
    without the .NET 8 runtime preinstalled.

.PARAMETER Version
    Version string (without a leading "v") used to name the output ZIP,
    e.g. "0.1.0-alpha". Defaults to reading Directory.Build.props via
    get-version.ps1 so local runs don't require passing it explicitly.
#>
[CmdletBinding()]
param(
    [string]$Version,
    [string]$RepoRoot = (Join-Path $PSScriptRoot '..'),
    [string]$OutputDirectory = (Join-Path $RepoRoot 'artifacts')
)

$ErrorActionPreference = 'Stop'

if (-not $Version) {
    $Version = & (Join-Path $PSScriptRoot 'get-version.ps1')
}

$projectPath = Join-Path $RepoRoot 'src\Rojan.Desktop.Shell\Rojan.Desktop.Shell.csproj'
$publishDirectory = Join-Path $RepoRoot 'publish'

if (Test-Path $publishDirectory) {
    Remove-Item $publishDirectory -Recurse -Force
}
if (-not (Test-Path $OutputDirectory)) {
    New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
}

dotnet publish $projectPath `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    --output $publishDirectory

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

$zipName = "RojanDesktop-v$Version-win-x64.zip"
$zipPath = Join-Path $OutputDirectory $zipName

if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

Compress-Archive -Path (Join-Path $publishDirectory '*') -DestinationPath $zipPath

Write-Output "Created $zipPath"
