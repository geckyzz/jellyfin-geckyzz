#!/usr/bin/env -S pwsh -NoProfile -NonInteractive

<#
.SYNOPSIS
    Append targetAbi to the latest version entry in manifest.json.

.DESCRIPTION
    Reads the Jellyfin.Controller package version from the csproj, ensures it is
    four parts (Major.Minor.Patch.Revision), appends it as "targetAbi" to the
    first version entry in manifest.json, and writes the manifest back with a
    trailing newline. Does nothing if the latest version entry already has a
    targetAbi.

.NOTES
    PowerShell port of .github/scripts/append_target_abi.py.
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

function Get-TargetAbi {
    param([string]$CsProjPath)

    $Content = Get-Content -LiteralPath $CsProjPath -Raw
    $Match = [regex]::Match($Content, '<PackageReference Include="Jellyfin\.Controller" Version="([^"]+)"')
    if (-not $Match.Success) {
        throw "Error: Could not find Jellyfin.Controller version in csproj"
    }

    # Ensure four parts (Major.Minor.Patch.Revision); pad with zeros if needed.
    $Parts = [System.Collections.Generic.List[string]](($Match.Groups[1].Value -split '\.') | ForEach-Object { [int]$_ })
    while ($Parts.Count -lt 4) { $Parts.Add("0") }
    return ($Parts[0..3] -join ".")
}

$RepoRoot = (Get-Item -LiteralPath $PSScriptRoot).Parent.Parent.FullName
$ManifestPath = Join-Path $RepoRoot "manifest.json"
$CsProjPath = Join-Path $RepoRoot "Jellyfin.Plugin.Geckyzz" "Jellyfin.Plugin.Geckyzz.csproj"

$TargetAbi = Get-TargetAbi -CsProjPath $CsProjPath

$Manifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
$Plugin = $Manifest[0]

$Versions = @($Plugin.versions | Where-Object { $_ })
if (-not $Versions) {
    $Versions = @(@{ version = if ($Plugin.version) { $Plugin.version } else { "0.0.0.0" } })
    $Plugin.versions = $Versions
}

$Latest = $Versions[0]
if ($Latest.targetAbi) {
    Write-Host "targetAbi already set for version $($Latest.version): $($Latest.targetAbi)"
    return
}

$Latest | Add-Member -MemberType NoteProperty -Name targetAbi -Value $TargetAbi -Force

$Json = $Manifest | ConvertTo-Json -Depth 10
$Json = $Json.TrimEnd() + "`n"
Set-Content -LiteralPath $ManifestPath -Value $Json -Encoding UTF8 -NoNewline

Write-Host "Appended targetAbi to manifest: $TargetAbi"
