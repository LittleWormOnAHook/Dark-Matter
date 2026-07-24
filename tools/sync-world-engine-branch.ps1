# Force-sync World Engine Safe Mode fix files from origin.
# Run from the Unity project root (folder that contains Assets\ and .git\).
# Close Unity before running.

$ErrorActionPreference = "Stop"
$Branch = "cursor/world-engine-docs-honesty-782b"

$Files = @(
    "Assets/_Project/Features/Directors/Runtime/WeatherDirectorService.cs",
    "Assets/_Project/Features/Directors/Tests/WeatherDirectorServiceTests.cs",
    "Assets/_Project/Features/Directors/Adapters/WeatherCommandServiceAdapter.cs",
    "Assets/_Project/Scripts/UI/EnvironmentalCrisisHudMode.cs",
    "Assets/_Project/Features/WorldState/Adapters/EnvironmentWorldStateProvider.cs",
    "Assets/_Project/Features/Directors/Adapters/DarkMatterSmokeDriver.cs"
)

Write-Host "Project root: $PWD"
if (-not (Test-Path ".git")) {
    throw "Not a git repo root. cd to the folder that contains Assets and .git"
}
if (-not (Test-Path "Assets/_Project")) {
    throw "Assets/_Project missing — wrong folder?"
}

git fetch origin
git checkout $Branch
git reset --hard "origin/$Branch"
git clean -fd "Assets/_Project/Features"

git checkout "origin/$Branch" -- @Files

Write-Host ""
Write-Host "=== Verify tip + SYNC_MARKER ==="
git log -1 --oneline
foreach ($f in $Files) {
    if (-not (Test-Path $f)) { Write-Host "MISSING $f"; continue }
    $hit = Select-String -Path $f -Pattern "SYNC_MARKER|IsOperationsPaused|ResolveNextPhase" -SimpleMatch:$false | Select-Object -First 2
    Write-Host "--- $f ---"
    if ($hit) { $hit | ForEach-Object { Write-Host $_.Line.Trim() } }
    else { Write-Host "(no marker match — unexpected)" }
}

Write-Host ""
Write-Host "Done. Reopen Unity. Adapter tip must call SetCrisisActive(crisis, banner, true, true) — not a 1-arg call."
Write-Host "If console still says SetCrisisActive error at line 54, Unity is not reading this folder."
