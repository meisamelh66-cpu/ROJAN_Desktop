<#
.SYNOPSIS
    Publishes Rojan.Desktop.Shell and packages it as "ROJAN Reception
    Setup.exe" - a real, distributable Windows installer.

.DESCRIPTION
    Desktop Productionization Sprint 1: wraps publish.ps1's self-contained,
    single-file win-x64 output with Inno Setup (build/installer/RojanReception.iss)
    instead of the ZIP-only packaging that script produces on its own -
    versioned Start Menu shortcut, uninstaller, clean install/upgrade flow,
    optional desktop shortcut. Requires Inno Setup 6 to be installed
    (winget install --id JRSoftware.InnoSetup); this script does not
    install it, since installing new machine-wide tooling is a decision
    made once by a human, not silently on every packaging run.

    Desktop Productionization Sprint 2 (Code Signing Preparation): optional
    -CertificatePath/-CertificatePassword/-TimestampUrl parameters. When
    supplied, the published .exe is Authenticode-signed via signtool.exe
    (Windows SDK) *before* Inno Setup packages it, and Inno Setup itself
    signs both the installer .exe and the uninstaller it embeds (via the
    [Setup] SignTool=/SignedUninstaller=yes directives in
    RojanReception.iss, gated behind the SignInstaller preprocessor
    symbol this script sets). When the parameters are omitted - the
    default, and the only path actually exercised in this environment
    (no certificate purchased, see docs/standards/code-signing.md) -
    nothing about signing runs at all and the build is identical to
    every prior unsigned run. This is the "unsigned fallback for
    development" requirement: not a flag to flip, just "don't pass the
    parameters."

.PARAMETER Version
    Version string (without a leading "v"), e.g. "1.0.0". Defaults
    to reading Directory.Build.props via get-version.ps1, the same single
    source of truth publish.ps1 already uses.

.PARAMETER IsccPath
    Path to ISCC.exe (Inno Setup's command-line compiler). Defaults to
    the two locations winget's JRSoftware.InnoSetup package installs to
    (per-machine and per-user); pass explicitly if installed elsewhere.

.PARAMETER CertificatePath
    Path to a .pfx/.p12 Authenticode code-signing certificate. Omit to
    produce an unsigned build (the default). See
    docs/standards/code-signing.md for what certificate type is required
    and how to obtain one - this script only consumes one, it doesn't
    help acquire it.

.PARAMETER CertificatePassword
    The certificate's private-key password. In CI, sourced from the
    CODE_SIGNING_CERT_PASSWORD secret (see docs/standards/code-signing.md);
    locally, pass it directly or via an environment variable your shell
    already has - never hardcode it into a script or commit it.

.PARAMETER TimestampUrl
    RFC 3161 timestamp authority URL, so the signature stays valid after
    the certificate itself expires. Defaults to DigiCert's public
    timestamp service (no account needed, works with any CA's cert).

.PARAMETER SignToolPath
    Path to signtool.exe (Windows SDK). Auto-detected under the standard
    Windows Kits install location if not supplied.
#>
[CmdletBinding()]
param(
    [string]$Version,
    [string]$RepoRoot = (Join-Path $PSScriptRoot '..'),
    [string]$IsccPath,
    [string]$CertificatePath,
    [string]$CertificatePassword,
    [string]$TimestampUrl = 'http://timestamp.digicert.com',
    [string]$SignToolPath
)

$ErrorActionPreference = 'Stop'

if (-not $Version) {
    $Version = & (Join-Path $PSScriptRoot 'get-version.ps1')
}

if (-not $IsccPath) {
    $candidatePaths = @(
        (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
        (Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe'),
        (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe')
    )
    $IsccPath = $candidatePaths | Where-Object { Test-Path $_ } | Select-Object -First 1
}

if (-not $IsccPath -or -not (Test-Path $IsccPath)) {
    throw "ISCC.exe (Inno Setup's compiler) was not found. Install Inno Setup 6 (winget install --id JRSoftware.InnoSetup) or pass -IsccPath explicitly."
}

$signingRequested = -not [string]::IsNullOrWhiteSpace($CertificatePath)

if ($signingRequested) {
    if (-not (Test-Path $CertificatePath)) {
        throw "CertificatePath '$CertificatePath' does not exist."
    }

    if (-not $SignToolPath) {
        # signtool.exe ships with the Windows SDK, under an
        # architecture/version-specific subfolder - search rather than
        # assume one exact path.
        $signToolCandidates = Get-ChildItem -Path 'C:\Program Files (x86)\Windows Kits\10\bin' -Filter 'signtool.exe' -Recurse -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName -match '\\x64\\' } |
            Sort-Object FullName -Descending
        $SignToolPath = $signToolCandidates | Select-Object -First 1 -ExpandProperty FullName
    }

    if (-not $SignToolPath -or -not (Test-Path $SignToolPath)) {
        throw "signtool.exe was not found. Install the Windows SDK (includes signtool.exe) or pass -SignToolPath explicitly."
    }
}

# Re-publishes fresh every time, same reasoning as publish.ps1 removing
# its own publish/ directory first - an installer must never silently
# package a stale build left over from an earlier run.
& (Join-Path $PSScriptRoot 'publish.ps1') -Version $Version -RepoRoot $RepoRoot
if ($LASTEXITCODE -ne 0) {
    throw "publish.ps1 failed with exit code $LASTEXITCODE"
}

if ($signingRequested) {
    $exePath = Join-Path $RepoRoot 'publish\Rojan.Desktop.Shell.exe'
    Write-Output "Signing $exePath"
    & $SignToolPath sign /f $CertificatePath /p $CertificatePassword /tr $TimestampUrl /td sha256 /fd sha256 $exePath
    if ($LASTEXITCODE -ne 0) {
        throw "signtool.exe failed to sign $exePath with exit code $LASTEXITCODE"
    }
}

$issPath = Join-Path $PSScriptRoot 'installer\RojanReception.iss'
$artifactsDir = Join-Path $RepoRoot 'artifacts'

$isccArgs = [System.Collections.Generic.List[string]]::new()
$isccArgs.Add("/DAppVersion=$Version")

if ($signingRequested) {
    # Defines the named "signtool" tool Inno Setup's own [Setup] SignTool=
    # directive (gated behind #ifdef SignInstaller in the .iss) invokes to
    # sign the installer .exe and the embedded uninstaller. $f is Inno
    # Setup's own placeholder for "the file being signed" - substituted at
    # sign time, not by PowerShell here.
    $isccArgs.Add('/DSignInstaller=1')
    $isccArgs.Add("/Ssigntool=`"$SignToolPath`" sign /f `"$CertificatePath`" /p `"$CertificatePassword`" /tr `"$TimestampUrl`" /td sha256 /fd sha256 `$f")
}

$isccArgs.Add($issPath)

& $IsccPath @isccArgs
if ($LASTEXITCODE -ne 0) {
    throw "ISCC.exe failed with exit code $LASTEXITCODE"
}

$installerPath = Join-Path $artifactsDir 'ROJAN Reception Setup.exe'
if (-not (Test-Path $installerPath)) {
    throw "Expected installer output not found at $installerPath"
}

Write-Output "Created $installerPath$(if ($signingRequested) { ' (signed)' } else { ' (unsigned)' })"
