# ChatGPT — Session Handoff (Dark Matter: Genesis)

**Generated:** 2026-10-16  
**Doc sync:** 2026-10-16 — combat docs, INDEX §1b, `DMG_Combat_Plan_v2.md`, spacing plan §10, handoffs, **`AGENTS.md`**, **`.cursor/rules`** (`core`, `unity-agent-workflow`, depot-restore), and Grok handoff aligned to playable **`v1.6.6`**. Phase 2–3 acceptance history remains **v1.6.5** (Oct 5–8, 2026). **Next combat gate unchanged:** Phase 4 Combat Director (§31 #4) per plan v2.4.  
**Project root:** `A:\Dark Matter Genesis`  
**Branch:** `cursor/wip-clean-20260919` (tracks `origin/cursor/wip-clean-20260919`; **HEAD `8b09be6c6`** at handoff time)  
**Prior handoff style:** `Assets/_Project/Documentation/Engineering/Grok_Commit_Push_Handoff.md`

---

## 1. Project locks (read first)

| Lock | Value |
|------|--------|
| Engine | Unity **6 HDRP** `6000.4.11f1` |
| Playable scene | **`Assets/_Project/Scenes/Dark Matter Genesis v1.6.6.unity`** — canonical tune/play/acceptance (Oct 2026). Phase 2–3 sign-off used **v1.6.5** Oct 5–8; do not rewrite that history in commit notes. |
| Git branch | **`cursor/wip-clean-20260919`** |
| Source control | **git** is agent truth; Plastic check-in is Anthony in the Plastic window (**no `cm` CLI**) |
| Player physics | **Do not retune `Player_v7`** capsule, layers, or physics matrix. Tune on **`Player_v7 Variant`**, profiles (`DM_ScopeProfile`, `DM_CombatCoreProfile`, climb/dash assets), not the base prefab |
| Pathing | **No NavMesh** (bake, agent, terrain NavMesh refs) |
| UI | **All new UI is UITK only** — no new uGUI. Palette: `DarkMatterGenesisUiPalette` / Shift theme |
| Economy / GDD | **UEA Credits only**; see `Assets/_Project/GAME_DESIGN_DOCUMENT_5.0.txt` |
| Live folder | Work Anthony’s open Unity project path — **do not clone to a remote VM for Play** |
| Auto Refresh | **Off** — after disk edits under `Assets/` / `ProjectSettings/`, agent runs MCP **`refresh_unity`** |

### MCP refresh + console (mandatory before “done” or commit)

1. **`refresh_unity`:** `mode: if_dirty`, `scope: all` (or `scripts` if only `.cs`), `compile: request` when C# / asmdef changed, `wait_for_ready: true`.
2. **Wait up to 240s** after script edits (poll `mcpforunity://editor/state` until `is_compiling == false`, `is_domain_reload_pending == false`, `advice.ready_for_tools == true`). Unity 6 + `compile: request` often returns before reload finishes.
3. **`read_console`** — **zero errors and zero warnings** before claiming done or committing (`.cursor/rules/unity-agent-workflow.mdc`). Fix project warnings in `Assets/_Project/`; document only truly unfixable third-party noise.

**Authority:** `.cursor/rules/` → GDD 5.0 → `AGENTS.md`. Index: `Assets/_Project/Documentation/INDEX.md`.

---

## 2. What shipped in commit `8b09be6c6`

**Message:** *Scope optics and scoped fire cadence with studio tuning and clean compile.*

### Scope / optics (UITK)

- **`DMUiToolkitOpticsOverlay`** — rifle ADS drives UITK scope; retires duplicate uGUI optics when `DM_ScopeProfile.retireUguiOptics`.
- **Live range readout** on rifle scope (`optics-range` label): world distance along reticle aim; shows **`??m`** when beyond scoped range cap (see §4).
- **Outer viewport mask** — dims **outside** the inner ring’s outer edge (not a tiny center hole only):
  - `DMScopeCenterMaskElement` + profile fields `scopeCenterMask*` / `scopeInnerRingViewportEdgeSoftness`.
  - Mask radius from `ViewportMaskRadiusFromOuterDiameterPx(ringOuterDiameterPx, Screen.height)`.
- **`DMScopeReticleElement`** updates for new scope art path (`useNewScopeArt`, rifle/pistol colors, mil-dot sprite, emission).

### Genesis Studio + Combat Studio

- **Genesis Studio → Combat → Scope Overlay** — filtered **`DM_ScopeProfile`** fields (cosmetics: reticle, ring, outer crosshair, center/viewport mask, diameters). `playModeSave: true`.
- **Genesis Studio → Combat → Scope Profile** — full **`DM_ScopeProfile`** (range, FOV, fire interval, post-shot wait, damage, ammo gate, audio, overlay toggles).
- **Combat Studio** — ranged/combat tooling touch-ups; **Play** tab opens **`Dark Matter Genesis v1.6.6.unity`**; links to profiles and Genesis Combat subtabs.

### Scoped fire cadence (gameplay)

**Order of operations** (rifle vs pistol columns):

1. **Base interval (seconds)** — `DM_ScopeProfile.rifleScopedFireInterval` / `pistolScopedFireInterval` when **> 0**.
2. **Else fallback** — per-ammo `ItemData.rifleScopedFireInterval` / `pistolScopedFireInterval` on loaded ammo FX profile.
3. **Ammo ±10%** — `rifleScopedFireIntervalPercentModifier` / `pistolScopedFireIntervalPercentModifier` on loaded ammo:  
   `interval = base × (1 − modifier/100)` (modifier clamped **−10…+10**; positive = faster).
4. **Rifles scope skills** — `SkillModifierType.ScopeFireIntervalPercent` via `PlayerSkillAllocator`:  
   `interval = max(minScopedFireInterval, beforeSkills × (1 − skillPct))`.
5. **Post-shot wait** — add `rifleScopedPostShotWaitSeconds` / `pistolScopedPostShotWaitSeconds`.

**Burst while scoped:** **`DMRangedAmmoStats.ResolveBurstCount`** returns **1** when `DMScopeMode.IsScoped`; scoped shots are single-shot cadence, not burst bypass.

**Runtime patch:** `DMScopeMode` raises Invector `shootFrequency` to scoped interval while ADS.

### Other notable changes in the same commit

- **Scene `Dark Matter Genesis v1.6.6.unity`** — combat/audio/enemy wiring updates.
- **`DM_ScopeProfile.asset`**, **`DM_CombatCoreProfile`**, ammo **`DMAmmoFxProfile_*`**, **`GameAudioProfile`**, enemy prefabs/data (large enemy prefab cleanup incl. removal of **`The_Evil_One.prefab`**).
- **Compile / API fixes** — e.g. `CombatHitAudio` → **`MeleeHitAudio`**, enemy audit editor splits, **`DMUiToolkitDamage`** overlay hooks.
- **UITK perf** — small churn reductions across HUD modules (`LateUpdate` guards, config checks).
- **Death / respawn** — further **`PioneerInvectorDeathRagdoll`**, **`PlayerDeathHandler`**, UITK death path (verify RETRY slide in Play — see §6).
- **Agent workflow** — `.cursor/rules/unity-agent-workflow.mdc` now requires **zero warnings** (not errors-only) before done/commit.

---

## 3. Tuning paths

| What | Where |
|------|--------|
| Scope cosmetics (mask, ring px, colors) | **Tools → Dark Matter Genesis → Genesis Studio → Combat → Scope Overlay** → `Assets/_Project/Resources/Combat/DM_ScopeProfile.asset` |
| Scope gameplay (range cap, FOV, interval, post-shot wait, damage, ammo keywords) | **Genesis Studio → Combat → Scope Profile** (same asset, unfiltered) |
| Scoped cadence fallback + ±% per ammo type | **Genesis Studio → Combat → Ammo FX** (folder `Assets/_Project/Data/Items/ammo/`) — fields *Rifle/Pistol Scoped Fire Interval* and *Percent Modifier*; inspector help in **`DMAmmoFxProfileEditor`** |
| Combat core / respawn settle | **`Assets/_Project/Resources/Combat/DM_CombatCoreProfile.asset`** — Combat Studio **Core** tab |
| Ranged combat hub | **Tools → Dark Matter Genesis → Combat → Combat Studio** |
| Live runtime accessor | **`DM_ScopeProfile.Live`** → `Resources.Load("Combat/DM_ScopeProfile")` |
| Mil-dot sprite fallback | `Resources/UI/Combat/sniperCrossHair` or profile `scopeCrosshairSprite` |

**Code anchors:** `DMScopeMode.cs`, `DMRangedAmmoStats.cs`, `DMUiToolkitOpticsOverlay.cs`, `DM_ScopeProfile.cs`.

---

## 4. Resolution formulas (cadence + range readout)

### Scoped fire interval (seconds per shot)

```
base = DM_ScopeProfile.(rifle|pistol)ScopedFireInterval  if > 0.01
     else loaded ammo.(rifle|pistol)ScopedFireInterval     if > 0.01
     else profile value

afterAmmo = base × (1 − clamp(ammoPercentModifier, −10, 10) / 100)

afterSkills = max(minScopedFireInterval,
                  afterAmmo × (1 − ScopeFireIntervalPercent skill bonus))

final = afterSkills + (rifle|pistol)ScopedPostShotWaitSeconds
```

Implemented: `DMRangedAmmoStats.ResolveScopedFireIntervalBeforeSkills` → `DMScopeMode.ScopedFireInterval`.

**Unscoped** interval: `ResolveShootInterval` uses fire rate / burst as before (burst count > 1 only when **not** scoped).

### Scoped range readout (UITK)

**Cap (metres):**

```
cap = (rifleScopedRange | pistolScopedRange) × (1 + ScopeRangePercent skill bonus)
```

**Live label:**

- Ray from gameplay camera via `RangedFireSolver.ResolveReticleAimPoint` (max ray ≈ `max(cap × 4, 250)`).
- Distance = camera position → aim point.
- If distance **> cap** → display **`??m`** (beyond skill/profile range).
- Else → **`{round(distance)} m`**.

Rifle only while scoped with sufficient zoom blend (`TryMeasureScopedAimDistanceMeters`).

**Player shot range** when scoped: `ResolvePlayerRange` uses `max(normalRange, cap)`.

---

## 5. Open / not committed (high level)

**HEAD is pushed** (`8b09be6c6` on `origin/cursor/wip-clean-20260919`). Working tree still has **local dirty + untracked** noise — **do not `git add -A`**.

| Category | Examples (non-exhaustive) |
|----------|---------------------------|
| Vendor / third-party churn | `Assets/Invector-*`, `Assets/PolygonNature`, `Assets/Magic Spells`, Malbers mats, QFX prefabs, TMP fallback, TutorialInfo deletes |
| Untracked animation packs | `Assets/Animations/Animations Free/`, `Mixamo Animations/Changed Folder/`, `Universal Animation Library 2/`, extra Mixamo climb/injured/zombie FBXs |
| Untracked character packs | `TaichiCharacterPack/`, `SpaceSuitGirl/`, `KriptoFX/` |
| Audio moves | Deleted `Assets/Audio/Melee Sword Sounds/*`; new folders under `Assets/Audio/Close Combat Sounds Effects/` (untracked) |
| Incidental | `AddressableAssetSettings`, ANGRY MESH mat, `Assets/DMI Default.wlt`, `Tools/__pycache__/`, stray `DMEnemyRuntimeAudit.PlayerArm.cs` at Assets root |
| _Project WIP (untracked) | Enemy audit JSON/MD snapshots Oct 15, `Fred.prefab`, `Test.asset`, movement debug script, experiment meta folders, dash spark prefabs |

**This handoff file** (+ Oct 16 doc sync edits to combat plan, INDEX, spacing plan, agent handoffs) are on disk — not necessarily in `8b09be6c6`; Anthony can commit explicitly when ready.

---

## 6. Suggested next tasks

**Combat roadmap:** `DMG_Combat_Plan_v2.md` **v2.4** — Phase 3 **Done**; **Phase 4 Combat Director (§31 #4)** is the active next gate (unchanged after v1.6.6 scene sync).

1. **Phase 4 combat director** — extend **`DM_CombatDirectorProfile`** + Combat Studio **Director** tab; wire `DirectorOrchestrator` / `WorldStateSnapshot` per `DMG_Combat_Plan_v2.md` (INDEX §1b). Play-test acceptance in **v1.6.6**.
2. **Loot chest system** — only if Anthony says go: plans under `Assets/_Project/Documentation/Design/Loot/` (`DM_Loot_Chest_System_Plan.md`).
3. **Death RETRY idle slide** — Play-test die → **RETRY** (in-place `Respawn`, no gel); tune `DM_CombatCoreProfile` respawn settle fields; code in `PioneerInvectorDeathRagdoll` / `PlayerDeathHandler` (see Grok handoff §7).
4. **v1.6.6 acceptance** — scope ADS (range line, `??m` past cap, mask outside red ring), scoped cadence vs hip burst, pistol vs rifle columns, Genesis Studio Play-mode saves on `DM_ScopeProfile`.
5. **Console clean** — after any new edits: MCP refresh → **0 errors, 0 warnings** before commit.

---

## 7. Do-not list

- **`git add -A`** or staging vendor packs / mocap / `TaichiCharacterPack` / `UIElementsSchema` / PlanetPack02 / OlegWER / GDKEditionAutoGen unless Anthony explicitly asks.
- **Silent restore** — no checkout/reset/revert to older commits or Plastic changesets without Anthony’s explicit OK in the current message (`confirm-before-depot-restore.mdc`).
- **Commit or push with Unity console errors or warnings** (including fixable `_Project` warnings).
- **Retune `Player_v7`** capsule, layers, rigidbody, physics matrix.
- **NavMesh**, new uGUI HUD/menu surfaces, or Invector-branded **new** `_Project` identifiers (use **DM / DMI**).
- **Delete/move** `Assets/_Project/Documentation/Design/ArtReference/` (Life Sheets) without explicit approval.
- **Claim done** while Unity is compiling or domain-reloading.

Before push: `.\Tools\DmUnityGitSafeguard.ps1 -BeforePush` from repo root.

---

## 8. Copy-paste starter prompt (ChatGPT)

```text
You are continuing Dark Matter: Genesis on Anthony's live Unity 6 HDRP project at A:\Dark Matter Genesis.

Branch: cursor/wip-clean-20260919. HEAD commit 8b09be6c6 ("Scope optics and scoped fire cadence with studio tuning and clean compile"). Play scene: Assets/_Project/Scenes/Dark Matter Genesis v1.6.6.unity.

Read first:
- Assets/_Project/Documentation/Engineering/ChatGPT_Session_Handoff_2026-10-16.md (this session)
- AGENTS.md and .cursor/rules/dark-matter-genesis-core.mdc + unity-agent-workflow.mdc

Recent shipped work: UITK scope overlay with live range readout (??m beyond scoped cap), outer viewport mask outside inner ring, Genesis Studio Scope Overlay + Scope Profile tabs on DM_ScopeProfile.asset. Scoped cadence: profile base interval → ammo ±10% modifier → Rifles scope skills → minScopedFireInterval → post-shot wait; burst disabled while scoped (DMRangedAmmoStats + DMScopeMode).

Rules: No Player_v7 retune. No NavMesh. New UI = UITK only. Auto Refresh off — use Unity MCP refresh_unity after asset/script edits; poll up to 240s; read_console must show zero errors AND zero warnings before done/commit. Never git add -A. Never silent git/depot restore. Do not commit unless I ask.

My task for this session: [DESCRIBE TASK HERE]
```

---

*End of handoff.*
