# Installs optional git hooks that run DmUnityGitSafeguard.ps1 before commit/push.
# Run once from repo root:  .\Tools\install-unity-git-hooks.ps1

$repoRoot = Split-Path -Parent $PSScriptRoot
$hooksDir = Join-Path $repoRoot ".git\hooks"
$guard = Join-Path $repoRoot "Tools\DmUnityGitSafeguard.ps1"

if (-not (Test-Path $hooksDir)) {
    Write-Error "Not a git repo (missing .git/hooks)."
}

$preCommit = @"
#!/bin/sh
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "$guard" -Strict
rc=`$?
if [ `$rc -ne 0 ]; then
  echo "DmUnityGitSafeguard blocked commit. Fix warnings or commit with SKIP_UNITY_GUARD=1"
  if [ "`$SKIP_UNITY_GUARD" = "1" ]; then exit 0; fi
  exit 1
fi
"@

$prePush = @"
#!/bin/sh
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "$guard" -BeforePush -Strict
rc=`$?
if [ `$rc -ne 0 ]; then
  echo "DmUnityGitSafeguard blocked push. Fix warnings or push with SKIP_UNITY_GUARD=1"
  if [ "`$SKIP_UNITY_GUARD" = "1" ]; then exit 0; fi
  exit 1
fi
"@

Set-Content -Path (Join-Path $hooksDir "pre-commit") -Value $preCommit -NoNewline
Set-Content -Path (Join-Path $hooksDir "pre-push") -Value $prePush -NoNewline
Write-Host "Installed pre-commit and pre-push hooks (use SKIP_UNITY_GUARD=1 to bypass once)."
