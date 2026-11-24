#!/usr/bin/env -S pwsh -NoProfile -NonInteractive

<#
.SYNOPSIS
    Update manifest.json with a new plugin version entry.

.DESCRIPTION
    Reads VERSION, SOURCE_URL (or DOWNLOAD_URL), ARTIFACT (or CHECKSUM), and TIMESTAMP
    from parameters or the environment, removes any existing entry with the same version
    from manifest.json, appends a new entry with version, targetAbi, sourceUrl, checksum,
    and timestamp, sorts the versions descending by version, and writes the manifest
    back with a trailing newline.

.NOTES
    Maintains Jellyfin PackageInfo[] array structure and canonical PackageVersionInfo metadata.
#>

[CmdletBinding()]
param(
    [Parameter()]
    [string]$Version = $env:VERSION,

    [Parameter()]
    [string]$SourceUrl = $(if ($env:SOURCE_URL) { $env:SOURCE_URL } else { $env:DOWNLOAD_URL }),

    [Parameter()]
    [string]$ArtifactPath = $env:ARTIFACT,

    [Parameter()]
    [string]$Checksum = $env:CHECKSUM,

    [Parameter()]
    [string]$Timestamp = $env:TIMESTAMP,

    [Parameter()]
    [string]$TargetAbi = $env:TARGET_ABI,

    [Parameter()]
    [string]$Changelog = $env:CHANGELOG,

    [Parameter()]
    [string]$ManifestPath = $(if ($env:MANIFEST_PATH) { $env:MANIFEST_PATH } else { "manifest.json" }),

    [Parameter()]
    [Alias("h", "-help")]
    [switch]$Help
)

if ($Help) {
    Get-Help $PSCommandPath -Detailed
    exit 0
}

$ErrorActionPreference = "Stop"

if (-not $Version) {
    throw "Version is not specified. Set VERSION environment variable or pass -Version."
}

if (-not $SourceUrl) {
    throw "Source/Download URL is not specified. Set SOURCE_URL or DOWNLOAD_URL environment variable or pass -SourceUrl."
}

# Resolve artifact checksum (MD5) if not explicitly provided
if (-not $Checksum -and $ArtifactPath) {
    if (Test-Path -LiteralPath $ArtifactPath) {
        $Checksum = (Get-FileHash -LiteralPath $ArtifactPath -Algorithm MD5).Hash.ToLowerInvariant()
        Write-Host "Calculated MD5 checksum from artifact: $Checksum"
    } else {
        Write-Warning "Artifact path '$ArtifactPath' does not exist; skipping checksum calculation."
    }
}

# Default build timestamp to current UTC ISO 8601 (yyyy-MM-ddTHH:mm:ssZ)
if (-not $Timestamp) {
    $Timestamp = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
}

# Resolve Target ABI if not provided
if (-not $TargetAbi) {
    $RepoRoot = (Get-Item -LiteralPath $PSScriptRoot).Parent.Parent.FullName
    $CsProjPath = Join-Path $RepoRoot "Jellyfin.Plugin.Geckyzz" "Jellyfin.Plugin.Geckyzz.csproj"
    if (Test-Path -LiteralPath $CsProjPath) {
        $Content = Get-Content -LiteralPath $CsProjPath -Raw
        $Match = [regex]::Match($Content, '<PackageReference Include="Jellyfin\.Controller" Version="([^"]+)"')
        if ($Match.Success) {
            $Parts = [System.Collections.Generic.List[string]](($Match.Groups[1].Value -split '\.') | ForEach-Object { [int]$_ })
            while ($Parts.Count -lt 4) { $Parts.Add("0") }
            $TargetAbi = ($Parts[0..3] -join ".")
        }
    }
}

# ConvertFrom-Json unwraps a single-element JSON array to the element itself;
# keep the manifest as an array (PackageInfo[]) when writing it back.
$Manifest = @(Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json)
$Plugin = $Manifest[0]

$Plugin.versions = @($Plugin.versions | Where-Object { $_.version -ne $Version })

$NewEntry = [ordered]@{
    version = $Version
}
if ($Changelog) {
    $NewEntry["changelog"] = $Changelog
}
if ($TargetAbi) {
    $NewEntry["targetAbi"] = $TargetAbi
}
$NewEntry["sourceUrl"] = $SourceUrl
if ($Checksum) {
    $NewEntry["checksum"] = $Checksum
}
if ($Timestamp) {
    $NewEntry["timestamp"] = $Timestamp
}

$Plugin.versions += $NewEntry

# Sort by version, newest (highest) first.
$Plugin.versions = @($Plugin.versions | Sort-Object -Descending -Property {
    $Parts = [int[]](($_.version -split '\.') | ForEach-Object { [int]$_ })
    while ($Parts.Length -lt 4) { $Parts += 0 }
    return ($Parts[0..3] | ForEach-Object { "{0:D10}" -f [int]$_ }) -join "."
})

# Use -InputObject instead of piping to ConvertTo-Json to prevent PowerShell
# from unrolling the single-element $Manifest array into an object.
$Json = ConvertTo-Json -InputObject $Manifest -Depth 10
$Json = $Json.TrimEnd() + "`n"
Set-Content -LiteralPath $ManifestPath -Value $Json -Encoding UTF8 -NoNewline

Write-Host "Updated $ManifestPath with version $Version"
