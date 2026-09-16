# Cloud Agent + Unity — Safeguards

Cloud Agents and branch switches can change **files on disk** while Unity still holds an older in-memory view—or reload scenes from an **older commit**. That feels like "reversed progress" even when git history is consistent.

## Quick checklist (Anthony)

| When | Do |
|------|-----|
| Before Cloud Agent asks to switch branch | Run `.\Tools\DmUnityGitSafeguard.ps1` |
| After any git pull/checkout/merge on this PC | Unity **Ctrl+R**; reopen **Dark Matter Genesis v1.6.4** (or current Genesis scene) |
| Before commit/push | Console **0 errors**; safeguard script clean |
| After big Unity day | `git branch backup/wip-pre-restore-YYYYMMDD` on current HEAD |

## Install git hooks (optional)

```powershell
cd "A:\Dark Matter Genesis"
.\Tools\install-unity-git-hooks.ps1
```

Hooks run the safeguard in `-Strict` mode. One-time bypass: `$env:SKIP_UNITY_GUARD=1; git commit ...`

## Anchor branch

Default anchor name in the script: **`backup/wip-pre-restore-20260907`**. Update the `-AnchorBranch` parameter if you rename it.

If `git log HEAD..backup/wip-pre-restore-20260907` shows commits, **do not checkout** a branch that lacks them without merging first.

## Agent rules

See `.cursor/rules/cloud-agent-unity-safeguards.mdc` and `confirm-before-depot-restore.mdc`.

## Vendor assets (large packs)

Do not commit full Asset Store trees. See **`Vendor_Assets_And_Git_Policy.md`** and **`Vendor_Install_Manifest.md`** in this folder.
