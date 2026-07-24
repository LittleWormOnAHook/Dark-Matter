# Nuclear recompile helper for Safe Mode / "Unity won't recompile".
# 1) Hard-syncs World Engine branch files
# 2) Wipes Unity script compile caches so the next open MUST rebuild
#
# Close Unity completely before running (Task Manager: no Unity.exe / Unity Hub compiling).
# Run from the Unity project root (folder with Assets\ and .git\).

$ErrorActionPreference = "Stop"
$Branch = "cursor/world-engine-docs-honesty-782b"

Write-Host "Project root: $PWD"
if (-not (Test-Path ".git")) { throw "Not a git repo root." }
if (-not (Test-Path "Assets/_Project")) { throw "Assets/_Project missing — wrong folder?" }

$unity = Get-Process -Name "Unity","Unity Hub" -ErrorAction SilentlyContinue
if ($unity) {
    Write-Host "WARNING: Unity still running:" -ForegroundColor Yellow
    $unity | Format-Table Id, ProcessName -AutoSize
    throw "Close Unity (and wait until Task Manager shows no Unity.exe), then re-run."
}

Write-Host "== git sync =="
git fetch origin
git checkout $Branch
git reset --hard "origin/$Branch"
git clean -fd "Assets/_Project/Features"

$stamp = "Assets/_Project/Features/_FORCE_RECOMPILE.txt"
"force-recompile $(Get-Date -Format o) tip=$(git rev-parse --short HEAD)" | Set-Content -Path $stamp -Encoding UTF8

Write-Host "== wipe compile caches =="
$paths = @(
    "Library\ScriptAssemblies",
    "Library\Bee",
    "Library\BurstCache",
    "Temp",
    "Obj"
)
foreach ($p in $paths) {
    if (Test-Path $p) {
        Write-Host "Removing $p"
        Remove-Item -LiteralPath $p -Recurse -Force -ErrorAction SilentlyContinue
    }
}

Write-Host ""
Write-Host "=== VERIFY (must show SYNC_MARKER) ==="
git log -1 --oneline
Select-String -Path "Assets\_Project\Features\Directors\Adapters\WeatherCommandServiceAdapter.cs" -Pattern "SYNC_MARKER|SetCrisisActive"
Select-String -Path "Assets\_Project\Features\Directors\Runtime\WeatherDirectorService.cs" -Pattern "SYNC_MARKER|ResolveNextPhase"

Write-Host ""
Write-Host "Next:"
Write-Host "1. Open THIS folder in Unity Hub (same path as above)."
Write-Host "2. Wait for full reimport/compile (first open after cache wipe is slow)."
Write-Host "3. If Safe Mode appears with ZERO errors, click Exit Safe Mode."
Write-Host "4. If errors remain, copy the FIRST error CS**** line into chat."
