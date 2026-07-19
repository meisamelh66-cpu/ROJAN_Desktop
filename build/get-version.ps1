<#
.SYNOPSIS
    Prints the single-source-of-truth version from Directory.Build.props
    (VersionPrefix[-VersionSuffix]), per docs/standards/versioning.md §2.

.DESCRIPTION
    Pure PowerShell + .NET XML parsing - no new dependency. Used by
    release.yml to verify a pushed tag matches the version actually
    committed, and available for local use (e.g. before tagging a release).
#>
[CmdletBinding()]
param(
    [string]$PropsPath = (Join-Path $PSScriptRoot '..\Directory.Build.props')
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $PropsPath)) {
    throw "Directory.Build.props not found at $PropsPath"
}

[xml]$xml = Get-Content -Path $PropsPath -Raw
$prefixNode = $xml.SelectSingleNode('//VersionPrefix')
$suffixNode = $xml.SelectSingleNode('//VersionSuffix')

if ($null -eq $prefixNode -or [string]::IsNullOrWhiteSpace($prefixNode.InnerText)) {
    throw "VersionPrefix not found in $PropsPath"
}

$version = $prefixNode.InnerText.Trim()
if ($null -ne $suffixNode -and -not [string]::IsNullOrWhiteSpace($suffixNode.InnerText)) {
    $version = "$version-$($suffixNode.InnerText.Trim())"
}

Write-Output $version
