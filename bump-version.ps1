#!/usr/bin/env -S pwsh -NoProfile -NonInteractive

<#
.SYNOPSIS
    Bumps the Jellyfin plugin version in build.yaml and .csproj, then commits the change.

.DESCRIPTION
    Reads the current 4-part version from build.yaml (falling back to Jellyfin.Plugin.Geckyzz.csproj),
    increments the specified SemVer/Assembly component (or sets an explicit version), updates both
    files in-place without altering trailing newlines, and creates a Git commit.

.PARAMETER BumpType
    The version component to increment ("major", "minor", "patch", "revision") or an explicit
    version string (e.g., "2.0.0.0"). Defaults to "patch".

.PARAMETER NoCommit
    Updates build.yaml and .csproj on disk without staging or committing to Git.

.PARAMETER Tag
    Creates an annotated Git tag (e.g., "v1.2.0.0") after committing. Ignored if -NoCommit is used.

.PARAMETER Force
    Allows bumping even if build.yaml or .csproj have uncommitted changes in the working tree.

.EXAMPLE
    ./bump-version.ps1
    Bumps the patch version (e.g., 1.1.2.0 -> 1.1.3.0) and commits.

.EXAMPLE
    ./bump-version.ps1 minor -Tag
    Bumps the minor version (e.g., 1.1.2.0 -> 1.2.0.0), commits, and creates tag v1.2.0.0.

.EXAMPLE
    ./bump-version.ps1 2.0.0.0 -NoCommit
    Sets the version explicitly to 2.0.0.0 without creating a Git commit.

.EXAMPLE
    ./bump-version.ps1 major -WhatIf
    Previews the version bump without modifying any files or running Git commands.
#>

[CmdletBinding(SupportsShouldProcess, DefaultParameterSetName = "Bump")]
param(
    [Parameter(ParameterSetName = "Bump", Position = 0)]
    [ValidatePattern('^(major|minor|patch|revision|\d+(\.\d+){1,3})$')]
    [string]$BumpType = "patch",

    [Parameter(ParameterSetName = "Bump")]
    [switch]$NoCommit,

    [Parameter(ParameterSetName = "Bump")]
    [switch]$Tag,

    [Parameter(ParameterSetName = "Bump")]
    [switch]$Force,

    [Parameter(ParameterSetName = "Help")]
    [Alias("h", "-help")]
    [switch]$Help
)

if ($Help) {
    Get-Help $PSCommandPath -Detailed
    exit 0
}

$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $true

$BuildYaml = Join-Path $PSScriptRoot "build.yaml"
$CsProj = Join-Path $PSScriptRoot "Jellyfin.Plugin.Geckyzz" "Jellyfin.Plugin.Geckyzz.csproj"

# 1. Guard against uncommitted changes in target files
if (-not $Force -and -not $WhatIfPreference) {
    $DirtyFiles = git -C $PSScriptRoot status --porcelain -- $BuildYaml $CsProj
    if ($DirtyFiles) {
        throw "Target files have uncommitted changes. Commit/stash them first or pass -Force:`n$($DirtyFiles -join "`n")"
    }
}

# 2. Read current version from build.yaml (fallback: csproj)
$CurrentVersion = $null

if (Test-Path $BuildYaml) {
    $Content = Get-Content $BuildYaml -Raw
    $M = [regex]::Match($Content, '(?m)^version:\s*["'']?([0-9]+(?:\.[0-9]+){1,3})["'']?\s*$')
    if ($M.Success) {
        $CurrentVersion = $M.Groups[1].Value
        Write-Verbose "Read version $CurrentVersion from $BuildYaml"
    }
}

if (-not $CurrentVersion -and (Test-Path $CsProj)) {
    $Content = Get-Content $CsProj -Raw
    $M = [regex]::Match($Content, '<Version>([0-9]+(?:\.[0-9]+){1,3})</Version>')
    if ($M.Success) {
        $CurrentVersion = $M.Groups[1].Value
        Write-Verbose "Read version $CurrentVersion from $CsProj"
    }
}

if (-not $CurrentVersion) {
    throw "Could not read a valid numeric version from build.yaml or csproj."
}

Write-Host "Current version: $CurrentVersion"

# 3. Calculate target version
$Parts = [System.Collections.Generic.List[string]]($CurrentVersion -split '\.')
while ($Parts.Count -lt 4) { $Parts.Add("0") }
$Major, $Minor, $Patch, $Revision = [int]$Parts[0], [int]$Parts[1], [int]$Parts[2], [int]$Parts[3]

switch ($BumpType.ToLowerInvariant()) {
    "major"    { $Major += 1; $Minor = 0; $Patch = 0; $Revision = 0 }
    "minor"    { $Minor += 1; $Patch = 0; $Revision = 0 }
    "patch"    { $Patch += 1; $Revision = 0 }
    "revision" { $Revision += 1 }
    default {
        $ExplicitParts = [System.Collections.Generic.List[string]]($BumpType -split '\.')
        while ($ExplicitParts.Count -lt 4) { $ExplicitParts.Add("0") }
        $Major, $Minor, $Patch, $Revision = [int]$ExplicitParts[0], [int]$ExplicitParts[1], [int]$ExplicitParts[2], [int]$ExplicitParts[3]
    }
}

$NewVersion = "$Major.$Minor.$Patch.$Revision"
Write-Host "Bumping to:      $NewVersion"

# 4. Update files
$FilesToStage = @()

if (Test-Path $BuildYaml) {
    $RawYaml = Get-Content $BuildYaml -Raw
    $NewYaml = $RawYaml -replace '(?m)^version:\s*["'']?[^"''\r\n]+["'']?', "version: `"$NewVersion`""
    if ($NewYaml -eq $RawYaml -and $CurrentVersion -ne $NewVersion) {
        throw "Failed to update version field in $BuildYaml"
    }
    if ($PSCmdlet.ShouldProcess($BuildYaml, "Set version to $NewVersion")) {
        Set-Content -Path $BuildYaml -Value $NewYaml -Encoding UTF8 -NoNewline
        Write-Host "Updated build.yaml"
    }
    $FilesToStage += $BuildYaml
}

if (Test-Path $CsProj) {
    $RawProj = Get-Content $CsProj -Raw
    $NewProj = $RawProj -replace '<Version>[^<]*</Version>', "<Version>$NewVersion</Version>"
    if ($NewProj -eq $RawProj -and $CurrentVersion -ne $NewVersion) {
        throw "Failed to update <Version> tag in $CsProj"
    }
    if ($PSCmdlet.ShouldProcess($CsProj, "Set <Version> to $NewVersion")) {
        Set-Content -Path $CsProj -Value $NewProj -Encoding UTF8 -NoNewline
        Write-Host "Updated $CsProj"
    }
    $FilesToStage += $CsProj
}

# 5. Git commit & optional tag
if (-not $NoCommit) {
    if ($PSCmdlet.ShouldProcess("Git repository", "Commit version bump $NewVersion")) {
        git -C $PSScriptRoot add -- $FilesToStage
        if ($LASTEXITCODE -ne 0) { throw "git add failed." }

        git -C $PSScriptRoot commit -m "Bump version to $NewVersion" -- $FilesToStage
        if ($LASTEXITCODE -ne 0) { throw "git commit failed." }

        if ($Tag) {
            $TagName = "v$NewVersion"
            git -C $PSScriptRoot tag -a $TagName -m "Release $NewVersion"
            if ($LASTEXITCODE -ne 0) { throw "git tag failed." }
            Write-Host "Created tag:     $TagName"
        }
    }
} elseif ($Tag) {
    Write-Warning "-Tag was ignored because -NoCommit was specified."
}

Write-Host "=============================================="
Write-Host "Successfully bumped version to $NewVersion!"
Write-Host "=============================================="
