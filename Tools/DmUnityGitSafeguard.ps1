# Dark Matter: Genesis — git / Unity safety check before commit, push, checkout, or Cloud Agent branch work.
# Usage:
#   .\Tools\DmUnityGitSafeguard.ps1              # report only (default)
#   .\Tools\DmUnityGitSafeguard.ps1 -Strict      # exit 1 on warnings (for hooks)
#   .\Tools\DmUnityGitSafeguard.ps1 -BeforePush  # extra remote divergence checks

param(
    [switch]$Strict,
    [switch]$BeforePush,
    [string]$AnchorBranch = "backup/wip-pre-restore-20260907"
)

$ErrorActionPreference = "Continue"
Set-Location (Split-Path -Parent $PSScriptRoot)

$Warnings = New-Object System.Collections.Generic.List[string]
$Blocks = New-Object System.Collections.Generic.List[string]

function Write-Section {
    param([string]$Title)
    Write-Host ""
    Write-Host "=== $Title ===" -ForegroundColor Cyan
}

function Add-Warning {
    param([string]$Msg)
    [void]$Warnings.Add($Msg)
    Write-Host "WARN: $Msg" -ForegroundColor Yellow
}

Write-Section "Branch"
$branch = git branch --show-current
Write-Host "Current: $branch"
$head = git rev-parse --short HEAD
Write-Host "HEAD:    $head"

Write-Section "Uncommitted Unity-critical files"
$dirty = git status --porcelain
$unityLines = @()
foreach ($line in $dirty) {
    if ($line -match "\.(unity|prefab|asset|mat|terrainlayer)") {
        $unityLines += $line
    }
}
if ($unityLines.Count -gt 0) {
    Add-Warning "Uncommitted scene/prefab/material/terrain changes - commit or stash before checkout/push."
    foreach ($line in $unityLines) { Write-Host "  $line" }
} else {
    Write-Host "  (none)"
}

Write-Section "Anchor branch (local WIP safety net)"
$anchorRef = "refs/heads/$AnchorBranch"
git show-ref --verify --quiet $anchorRef 2>$null | Out-Null
if ($LASTEXITCODE -eq 0) {
    $anchorTip = git rev-parse --short $anchorRef
    Write-Host "Anchor: $AnchorBranch at $anchorTip"
    $rangeAnchorOnly = "${head}..${AnchorBranch}"
    $rangeHeadOnly = "${AnchorBranch}..${head}"
    $onlyOnAnchor = @(git log --oneline $rangeAnchorOnly 2>$null)
    $onlyOnHead = @(git log --oneline $rangeHeadOnly 2>$null)
    if ($onlyOnAnchor.Count -gt 0) {
        Add-Warning "Anchor has commits not on current branch - checkout without merge would drop Unity work."
        $onlyOnAnchor | Select-Object -First 8 | ForEach-Object { Write-Host "  $_" }
    }
    if ($onlyOnHead.Count -gt 0) {
        Write-Host "Commits on current branch not on anchor:"
        $onlyOnHead | Select-Object -First 5 | ForEach-Object { Write-Host "  $_" }
    }
} else {
    Write-Host "Anchor branch not found: $AnchorBranch"
    Add-Warning "Create an anchor after big Unity days: git branch $AnchorBranch"
}

Write-Section "Remote (if tracking)"
$upstream = git rev-parse --abbrev-ref '@{u}' 2>$null
if ($LASTEXITCODE -eq 0 -and $upstream) {
    Write-Host "Tracking: $upstream"
    git fetch origin --quiet 2>$null
    $behind = git rev-list --count "HEAD..$upstream" 2>$null
    $ahead = git rev-list --count "$upstream..HEAD" 2>$null
    Write-Host "Ahead: $ahead  Behind: $behind"
    if ($BeforePush -and [int]$behind -gt 0) {
        Add-Warning "Local branch is behind remote - pull/rebase before push."
    }
} else {
    Write-Host "No upstream configured."
}

Write-Section "Playable scene reminder"
Write-Host "Open: Assets/_Project/Scenes/Dark Matter Genesis v1.6.4.unity (or current Genesis scene)."
Write-Host "After git changes: Ctrl+R in Unity (Auto Refresh is off)."

Write-Section "Unity Editor"
Write-Host "Before commit/push: Unity Console must have zero Errors (MCP read_console when available)."

Write-Section "Summary"
if ($Warnings.Count -eq 0) {
    Write-Host "No safeguard warnings." -ForegroundColor Green
    exit 0
}
if ($Strict) {
    exit 1
}
exit 0
