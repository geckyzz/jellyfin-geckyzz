#!/usr/bin/env pwsh
# bump-version.ps1: Bump the plugin version in build.yaml and csproj.
# Usage: pwsh -NoProfile bump-version.ps1 [major|minor|patch]

param(
    [ValidateSet("major", "minor", "patch")]
    [string]$BumpType = "patch"
)

$ErrorActionPreference = "Stop"

# 1. Read current version from build.yaml (fallback: csproj).
$CurrentVersion = $null
$BuildYaml = Join-Path $PSScriptRoot "build.yaml"
$CsProj = Join-Path $PSScriptRoot "Jellyfin.Plugin.Geckyzz" "Jellyfin.Plugin.Geckyzz.csproj"

if (Test-Path $BuildYaml) {
    $Content = Get-Content $BuildYaml -Raw
    $M = [regex]::Match($Content, '(?m)^version:\s*"([^"]*)"')
    if ($M.Success) { $CurrentVersion = $M.Groups[1].Value }
}
if ($CurrentVersion -eq $null) {
    if (Test-Path $CsProj) {
        $Content = Get-Content $CsProj -Raw
        $M = [regex]::Match($Content, "<Version>([^<]*)</Version>")
        if ($M.Success) { $CurrentVersion = $M.Groups[1].Value }
    }
}

if (-not $CurrentVersion) {
    Write-Error "Could not read current version from build.yaml or csproj."
    exit 1
}

Write-Host "Current version: $CurrentVersion"

# 2. Parse and bump
$Parts = $CurrentVersion -split '\.'
if ($Parts.Count -lt 4) {
    while ($Parts.Count -lt 4) { $Parts += "0" }
    $CurrentVersion = ($Parts -join ".")
}
$Major, $Minor, $Patch, $Revision = [int]$Parts[0], [int]$Parts[1], [int]$Parts[2], [int]$Parts[3]

switch ($BumpType) {
    "major" { $Major += 1; $Minor = 0; $Patch = 0; $Revision = 0 }
    "minor" { $Minor += 1; $Patch = 0; $Revision = 0 }
    "patch" { $Patch += 1; $Revision = 0 }
}

$NewVersion = "$Major.$Minor.$Patch.$Revision"
Write-Host "Bumping to: $NewVersion"

# 3. Update files
if (Test-Path $BuildYaml) {
    $NewContent = (Get-Content $BuildYaml -Raw) -replace '(?m)^version:\s*"[^"]*"', "version: `"$NewVersion`""
    Set-Content -Path $BuildYaml -Value $NewContent -Encoding UTF8
    Write-Host "Updated build.yaml"
}
if (Test-Path $CsProj) {
    $NewContent = (Get-Content $CsProj -Raw) -replace "<Version>[^<]*</Version>", "<Version>$NewVersion</Version>"
    Set-Content -Path $CsProj -Value $NewContent -Encoding UTF8
    Write-Host "Updated $CsProj"
}

# 4. Commit
git add build.yaml Jellyfin.Plugin.Geckyzz/Jellyfin.Plugin.Geckyzz.csproj
git commit -m "Bump version to $NewVersion"

Write-Host "=============================================="
Write-Host "Successfully bumped version to $NewVersion!"
Write-Host "=============================================="
