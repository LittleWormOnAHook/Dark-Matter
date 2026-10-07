# Grok bot — Commit & Push Handoff (Dark Matter: Genesis)

**Generated:** 2026-10-06  
**Project root:** `A:\Dark Matter Genesis`  
**Current branch:** `cursor/wip-clean-20260919` (tracks `origin/cursor/wip-clean-20260919`, up to date at handoff time)  
**HEAD before commit:** `5fa787bc4` — *X ammo cycling: only weapon-compatible Hot Cross ammo + toasts…*

---

## 1. Purpose

Copy-paste instructions for **grok bot** (or any agent) to **stage, commit, and push** this session’s WIP without `git add -A`, without vendor noise, and only after Unity console is clean. Anthony explicitly requested commit/push via handoff — do not push if console has errors.

**Working tree snapshot (agent run):**

| Check | Result |
|--------|--------|
| `git diff --stat` | **75 files** changed, **+3811 / −638** lines |
| Staged | **none** (`git diff --stat --cached` empty) |
| Untracked (all repo) | **482** paths (mostly vendor packs — **exclude**) |
| Session-relevant modified | **56** paths under `Assets/_Project/` + `Assets/UI Toolkit/` |
| Session-relevant untracked | **34** paths under `Assets/_Project/` (loot docs, MeleeUpper anims, enemy/audio cleanup, etc.) |

---

## 2. Authority rules (non-negotiable)

1. **Unity 6 HDRP** live folder: `A:\Dark Matter Genesis`. Play scene: `Assets/_Project/Scenes/Dark Matter Genesis v1.6.5.unity`.
2. **Auto Refresh is off** — after disk edits, run MCP **`refresh_unity`** (`mode: if_dirty`, `compile: request` if `.cs` staged, `wait_for_ready: true`). If MCP unavailable, tell Anthony **Ctrl+R** and wait for compile/domain reload.
3. **Zero Unity console errors** before commit and before push (`read_console`, type `error`). Warnings alone do not block unless Anthony says otherwise.
4. **Never commit with console errors** (`.cursor/rules/unity-agent-workflow.mdc`).
5. **Do not `git add -A`**. Stage **explicit paths** only (see §3).
6. **Always stage paired `.meta`** for new assets under `Assets/_Project/` and `Assets/UI Toolkit/`.
7. **Include content with scripts:** scene `.unity`, prefabs, profiles `.asset`, materials, new animations — same commit as the code that wires them.
8. **Before push:** `.\Tools\DmUnityGitSafeguard.ps1 -BeforePush` from repo root. Resolve WARN/blockers; do not force-push.
9. **No silent restore** — no checkout/reset/revert without Anthony’s explicit OK in the current message.
10. **Exclude unless Anthony explicitly asked:** untracked vendor packs (TaichiCharacterPack, Animations Free, Visual Scripting generated, etc.), `.cursor/rules`, `AGENTS.md`, incidental edits under `Assets/Invector-*`, QFX/third-party prefabs, `ProjectSettings/*` tweaks, huge Mixamo `.meta`-only churn, TutorialInfo/TMP fallback noise.

**Policy refs:** `AGENTS.md`, `Assets/_Project/Documentation/Engineering/Unity_Git_Disk_Truth.md`, `Tools/DmUnityGitSafeguard.ps1`.

---

## 3. Git command sequence (PowerShell — use `;` not `&&`)

### 3.1 Inspect (always first)

```powershell
Set-Location "A:\Dark Matter Genesis"

git status
git diff --stat
git diff --stat --cached
git log -5 --oneline
git branch -vv
```

Re-read `git status` after staging. Critical paths for this session must not remain unstaged if you intend them saved.

### 3.2 Unity readiness (MCP — if connected)

1. `refresh_unity` — `mode: if_dirty`, `scope: all` (or `scripts` if only docs staged), `compile: request` when C# included, `wait_for_ready: true`.
2. Poll until `compilation.is_compiling == false` and `ready_for_tools == true` if needed.
3. `read_console` — **errors must be 0**.

### 3.3 Stage — recommended single commit (WIP slice)

Stages **only** `_Project` + **UI Toolkit** (excludes vendor/Invector/ProjectSettings noise). Adds new loot docs, MeleeUpper clips, enemy death/audio helpers, etc.

```powershell
Set-Location "A:\Dark Matter Genesis"

git add "Assets/UI Toolkit/"
git add "Assets/_Project/"

# This handoff (optional but useful for audit trail)
git add "Assets/_Project/Documentation/Engineering/Grok_Commit_Push_Handoff.md"

git status
git diff --stat --cached
```

**Session highlights included in that staging:**

| Area | Paths |
|------|--------|
| UITK perf / HUD | `DMUiToolkitWorldChrome.cs`, `DMUiToolkitPilotCluster.cs`, `DMUiToolkitInputHost.cs`, `DMUiToolkitConfig.cs`, `DMUiToolkitMinimap.cs`, `DMUiToolkitHotCross.cs`, `DMUiToolkitConfig.asset`, `HotCross.uxml`, `HotCross.uss` |
| Melee double-tick | `PioneerShooterMeleeInput.cs`, `PioneerMeleeSwingHitDedupe.cs` |
| Loot design docs | `Assets/_Project/Documentation/Design/Loot/DM_Loot_Chest_System_Plan.md`, `DM_Loot_Chest_System_Handoff.md` (+ `.meta`, `Loot.meta`) |
| Combat / UI / AI WIP | Remaining modified `_Project` scripts (combat, AI/Invector bridges, inventory/ammo, UI shortcuts/registry/toasts), Editor combat/studio tools, profiles, scene `v1.6.5`, `Player_v7 Variant`, `IO_Ancient_Cache`, ammo VFX, enemy controller |
| New _Project assets | `Animations/Player/MeleeUpper/*`, `DM_MeleeUpper.mask`, melee layer policy doc, Wiki combat policy (if present), `DMSpawnedAudio*`, enemy death cleanup, `DMEnemyHitFlinch`, `DMEnemyMeleeComboDriver` |

**Explicitly NOT staged by the block above (leave dirty unless Anthony asks):**

- `Assets/TaichiCharacterPack/`, `Assets/Animations/Animations Free/`
- `Assets/Invector-3rdPersonController/...`
- `Assets/QFX/`, `Assets/PolygonNature/`, Malbers/Magic Spells particle prefabs
- `.cursor/rules/`, `AGENTS.md`
- `ProjectSettings/GraphicsSettings.asset`, `ProjectSettings/QualitySettings.asset`
- `Assets/Animations/Mixamo.../One Hand Sword Combo.fbx.meta` (large incidental diff)

### 3.4 Commit (PowerShell here-string)

**Recommendation: ONE commit** — UITK perf, melee dedupe, combat/enemy WIP, scene/prefab wiring, and loot docs belong to the same playable slice (~**90** staged paths: 56 modified + ~34 new under `_Project`/UITK). Split only if Anthony wants docs-only on disk first (§5).

```powershell
git commit -m @"
UITK HUD perf, melee hit dedupe, and combat WIP slice.

Reduce UITK world chrome / pilot cluster / input host churn; fix pioneer melee double-tick via PioneerMeleeSwingHitDedupe and PioneerShooterMeleeInput. Ship enemy combat/death/loot polish, Hot Cross and keyboard shortcut updates, Genesis v1.6.5 scene and profile wiring, MeleeUpper assets, and DM loot chest design docs.
"@
```

```powershell
git status
```

### 3.5 Safeguard + push

```powershell
Set-Location "A:\Dark Matter Genesis"
.\Tools\DmUnityGitSafeguard.ps1 -BeforePush
```

If safeguard reports blockers, **stop** and report to Anthony — do not `git push --force`.

Branch **tracks** remote → push is allowed when console clean and safeguard OK:

```powershell
git push
```

If upstream missing (should not on this branch):

```powershell
git push -u origin cursor/wip-clean-20260919
```

### 3.6 After push (live Unity machine)

MCP `refresh_unity` or Anthony **Ctrl+R**. Confirm canonical scene still `Dark Matter Genesis v1.6.5.unity`.

---

## 4. Suggested commit messages (alternatives)

### A — Single commit (preferred)

Same body as §3.4.

### B — Split 1: docs only

```powershell
git add "Assets/_Project/Documentation/Design/Loot/"
git add "Assets/_Project/Documentation/Design/Combat/DM_Melee_Locomotion_Layer_Policy.md"
git add "Assets/_Project/Documentation/Wiki/"
git add "Assets/_Project/Documentation/Design/Combat/DMG_Combat_Plan_v2.md"
git add "Assets/_Project/Documentation/Design/Combat/DM_Melee_Animation_Library_Plan.md"
git add "Assets/_Project/Documentation/Architecture/CursorPlans/combat_master_plan_handoff.md"

git commit -m @"
docs: DM loot chest system plan and melee layer policy handoffs.
"@
```

Then stage §3.3 remainder for commit **C**.

### C — Split 2: code + assets

Use §3.3 minus docs paths already committed; message:

```powershell
git commit -m @"
UITK perf and combat WIP: melee dedupe, enemy polish, scene v1.6.5.
"@
```

Only split if Anthony wants docs on git before code review; otherwise **one commit** is simpler.

---

## 5. Verification checklist before push

- [ ] Unity not compiling / domain reload finished (`ready_for_tools`)
- [ ] Console **0 errors** (`read_console`)
- [ ] `git diff --stat --cached` matches intent (no accidental `Assets/Taichi*` staged)
- [ ] Scene + prefabs + profiles staged with scripts
- [ ] New files have `.meta` staged
- [ ] `git commit` succeeded; `git status` shows branch ahead of origin
- [ ] `.\Tools\DmUnityGitSafeguard.ps1 -BeforePush` — no unresolved blocks
- [ ] `git push` to `origin/cursor/wip-clean-20260919` (no force)

---

## 6. Full copy-paste block for grok bot

```powershell
# Dark Matter: Genesis — commit + push handoff
# Branch: cursor/wip-clean-20260919
# Do NOT use git add -A. Unity console must be clean before commit/push.

Set-Location "A:\Dark Matter Genesis"

git status
git diff --stat
git diff --stat --cached
git log -5 --oneline
git branch -vv

# --- Unity (MCP): refresh_unity if_dirty, compile request if .cs staged, wait_for_ready true
# --- Unity (MCP): read_console errors -> must be 0

git add "Assets/UI Toolkit/"
git add "Assets/_Project/"
git add "Assets/_Project/Documentation/Engineering/Grok_Commit_Push_Handoff.md"

git status
git diff --stat --cached

git commit -m @"
UITK HUD perf, melee hit dedupe, and combat WIP slice.

Reduce UITK world chrome / pilot cluster / input host churn; fix pioneer melee double-tick via PioneerMeleeSwingHitDedupe and PioneerShooterMeleeInput. Ship enemy combat/death/loot polish, Hot Cross and keyboard shortcut updates, Genesis v1.6.5 scene and profile wiring, MeleeUpper assets, and DM loot chest design docs.
"@

git status

.\Tools\DmUnityGitSafeguard.ps1 -BeforePush

git push

# --- Unity (MCP): refresh_unity if_dirty after push; Anthony Ctrl+R if MCP down
```

---

*End of handoff.*
