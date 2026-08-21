<#
.SYNOPSIS
    Generates src/Rojan.Desktop.Shell/Assets/RojanReception.ico from the
    real ROJAN brand mark, so the process is reproducible if the source
    logo is ever updated - not a one-off manually-authored binary.

.DESCRIPTION
    Desktop Productionization Sprint 2 (Production Branding). Source:
    ROJAN_DesignLab's own Play Store master icon
    (ROJAN_DesignLab_Main1/app/src/main/ic_launcher-playstore.png) - the
    same rose-gold "R"/silhouette mark used across the Manager/Customer
    Android apps. Reusing it here is consistent cross-product branding,
    not a new/invented asset; this script only reads that file, it makes
    no changes to ROJAN_DesignLab.

    Builds a multi-resolution .ico (16/32/48/256px) using System.Drawing
    (bundled with Windows PowerShell - no new dependency) rather than a
    third-party image tool, since none is installed in this environment.
    Each size is embedded as PNG-compressed data (the modern ICO format,
    Vista+) rather than legacy uncompressed BMP - simpler to assemble
    correctly and what every current Windows icon actually uses.

.PARAMETER SourcePng
    Path to the source PNG. Defaults to the real ROJAN_DesignLab Play
    Store icon via a relative sibling-repo path (both repos live under
    the same D:\AndroidProjects parent in this environment).

.PARAMETER OutputIco
    Where to write the generated .ico. Defaults to the Shell project's
    Assets folder, matching Rojan.Desktop.Shell.csproj's
    <ApplicationIcon> reference.
#>
[CmdletBinding()]
param(
    [string]$SourcePng = (Join-Path $PSScriptRoot '..\..\ROJAN_DesignLab_Main1\app\src\main\ic_launcher-playstore.png'),
    [string]$OutputIco = (Join-Path $PSScriptRoot '..\src\Rojan.Desktop.Shell\Assets\RojanReception.ico'),
    [int[]]$Sizes = @(16, 32, 48, 256)
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $SourcePng)) {
    throw "Source PNG not found at $SourcePng"
}

Add-Type -AssemblyName System.Drawing

$sourceBitmap = [System.Drawing.Bitmap]::FromFile((Resolve-Path $SourcePng))
try {
    $pngBlobs = [System.Collections.Generic.List[byte[]]]::new()

    foreach ($size in $Sizes) {
        $resized = New-Object System.Drawing.Bitmap $size, $size
        $graphics = [System.Drawing.Graphics]::FromImage($resized)
        try {
            $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
            $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
            $graphics.DrawImage($sourceBitmap, 0, 0, $size, $size)
        } finally {
            $graphics.Dispose()
        }

        $memoryStream = New-Object System.IO.MemoryStream
        $resized.Save($memoryStream, [System.Drawing.Imaging.ImageFormat]::Png)
        $pngBlobs.Add($memoryStream.ToArray())
        $memoryStream.Dispose()
        $resized.Dispose()
    }
} finally {
    $sourceBitmap.Dispose()
}

$outputDirectory = Split-Path -Parent $OutputIco
if (-not (Test-Path $outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory | Out-Null
}

# ICONDIR (6 bytes) + one ICONDIRENTRY (16 bytes) per image, then the raw
# PNG-encoded image data blocks back-to-back - the standard modern .ico
# layout (MS-ICO / Vista+ PNG-in-ICO).
$fileStream = [System.IO.File]::Create($OutputIco)
try {
    $writer = New-Object System.IO.BinaryWriter $fileStream

    $writer.Write([uint16]0)              # Reserved, must be 0
    $writer.Write([uint16]1)              # Type: 1 = icon
    $writer.Write([uint16]$pngBlobs.Count) # Image count

    $headerSize = 6 + (16 * $pngBlobs.Count)
    $offset = $headerSize

    for ($i = 0; $i -lt $pngBlobs.Count; $i++) {
        $size = $Sizes[$i]
        $blob = $pngBlobs[$i]

        # Width/height byte: 0 means 256 (a byte can't hold 256 itself).
        $writer.Write([byte]($(if ($size -ge 256) { 0 } else { $size })))
        $writer.Write([byte]($(if ($size -ge 256) { 0 } else { $size })))
        $writer.Write([byte]0)   # Color palette count (0 = no palette, true color)
        $writer.Write([byte]0)   # Reserved
        $writer.Write([uint16]1) # Color planes
        $writer.Write([uint16]32) # Bits per pixel
        $writer.Write([uint32]$blob.Length)
        $writer.Write([uint32]$offset)

        $offset += $blob.Length
    }

    foreach ($blob in $pngBlobs) {
        $writer.Write($blob)
    }

    $writer.Flush()
} finally {
    $fileStream.Dispose()
}

Write-Output "Created $OutputIco ($($Sizes -join 'x, ')x, PNG-encoded)"
