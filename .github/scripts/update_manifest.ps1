#!/usr/bin/env -S pwsh -NoProfile -NonInteractive

<#
.SYNOPSIS
    Update manifest.json with a new plugin version entry.

.DESCRIPTION
    Reads TAG, VERSION and DOWNLOAD_URL from the environment, removes any existing
    entry with the same version from manifest.json, appends a new entry
    { "version": ..., "downloadUrl": ... }, sorts the versions by version
    descending, and writes the manifest back with a trailing newline.

.NOTES
    PowerShell port of .github/scripts/update_manifest.py.

.EXAMPLE
    .\update_manifest.ps1
#>

[CmdletBinding()]
param(
    [Parameter()]
    [Alias("h", "-help")]
    [switch]$Help
)

if ($Help) {
    Get-Help $PSCommandPath -Detailed
    exit 0
}

$ErrorActionPreference = "Stop"

foreach ($Var in @("TAG", "VERSION", "DOWNLOAD_URL")) {
    if (-not [Environment]::GetEnvironmentVariable($Var)) {
        throw "Environment variable $Var is not set."
    }
}

$Version = $env:VERSION
$DownloadUrl = $env:DOWNLOAD_URL
$ManifestPath = if ($env:MANIFEST_PATH) { $env:MANIFEST_PATH } else { "manifest.json" }

$Manifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
$Plugin = $Manifest[0]

$Plugin.versions = @($Plugin.versions | Where-Object { $_.version -ne $Version })
$Plugin.versions += [ordered]@{ version = $Version; downloadUrl = $DownloadUrl }

# Sort by version, newest (highest) first.
$Plugin.versions = @($Plugin.versions | Sort-Object -Descending -Property {
    $Parts = [int[]](($_.Version -split '\.') | ForEach-Object { [int]$_ })
    while ($Parts.Length -lt 4) { $Parts += 0 }
    return ($Parts[0..3] | ForEach-Object { "{0:D10}" -f [int]$_ }) -join "."
})

$Json = $Manifest | ConvertTo-Json -Depth 10
$Json = $Json.TrimEnd() + "`n"
Set-Content -LiteralPath $ManifestPath -Value $Json -Encoding UTF8 -NoNewline

Write-Host "Updated $ManifestPath with version $Version"
