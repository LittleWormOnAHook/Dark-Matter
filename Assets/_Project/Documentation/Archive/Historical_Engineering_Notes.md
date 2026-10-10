# Historical Engineering Notes (Archive)

> **Archived:** 2026-10-08. These notes are **not** indexed in `Documentation/INDEX.md` unless the owner explicitly asks to open the archive. **Disk truth** for shipped systems: `Architecture/World_Engine_Disk_Status.md`. **Current melee canon:** `Design/Combat/DM_Melee_Locomotion_Layer_Policy.md`.

Full originals (unchanged text) live alongside this file:

| Original path (stub left behind) | Archive copy |
|----------------------------------|--------------|
| `Documentation/ANIM_PLAN_REVERT.md` | `Archive/ANIM_PLAN_REVERT.md` |
| `Documentation/REPAIR_LOG_2026-08-22.md` | `Archive/REPAIR_LOG_2026-08-22.md` |
| `Documentation/Architecture/HDRP_Migration_Plan.md` | `Archive/HDRP_Migration_Plan.md` |

**Owner confirmations (Oct 8, 2026):** `Design/Quests_And_Story_Plan.md` (0 UEA / 2 Mars survivors) and `Design/Prologue_Playthrough_And_Camp_Bootstrap_Plan.md` §3 remain canonical step-by-step — no doc edits required unless drift is found.

---

## 1. Player v7 animation plan revert (Aug 2026)

**Purpose:** Disable Player animation-plan feature bools in Play Mode without git or Invector controller edits.

**Fast disable:** On `Player_v7`, component **Pioneer Animation Plan Settings** (added at runtime by `PioneerInvectorBootstrap` if missing). Uncheck:

- `Enable Unarmed Hang When Drawn`
- `Enable Draw Holster Anims`
- `Enable Hit Reaction Chance`

With all three off, behavior matches pre-plan stock (OnlyArms / UpperBody, instant equipment swap, 100% Invector hit reactions). Enemy directional hits (`EnemyInvectorRagdollBridge.enableEnemyDirectionalHits`) are independent.

**Files touched in that pass (see archive for git checkout list):** `PioneerAnimationPlanSettings.cs`, `PioneerInvectorBootstrap.cs`, `PioneerShooterMeleeInput.cs`, `PioneerInvectorWeaponBridge.cs`, `PioneerInvectorSurvivalBridge.cs`, `EnemyInvectorRagdollBridge.cs`. **Not modified:** `Invector@ShooterMelee.controller`, prefab YAML, enemy/companion controller assignments.

**Current canon:** Moving-melee layers and locomotion policy — `Design/Combat/DM_Melee_Locomotion_Layer_Policy.md`. Do not retune `Player_v7` physics (`.cursor/rules/dark-matter-genesis-player-physics.mdc`).

---

## 2. Repair log snapshot (2026-08-22)

**Context:** Branch `restore/v16-midnight-local` @ `a76ce96fb`; Unity **6000.4.11f1 HDRP**. Statuses below are **historical** — verify against disk and `World_Engine_Disk_Status.md` before assuming still open.

| # | Topic | Log status | Still relevant? |
|---|--------|------------|-----------------|
| 1 | Inventory drop NRE (Quora Shelter / box2) | DONE | Verify `Quora Shelter.asset` world prefab + `EnsureDroppedPhysicsAndPickup` |
| 2 | Resource nodes break when resized | DONE | Runtime `ResourceNodeInteractionVolume`; scaled harvest/mine reach |
| 3 | Play hitch / mip streaming | DONE / dup FBX delete skipped | Mip streaming enabled on all quality levels; **duplicate Meshy FBX trees** under `Models/` vs `Prefabs/Models/` still listed for a later pass |
| 4 | Settings Apply toast + FastPlay statics | DONE | `PickupToastUI` + `SettingsSceneReloader` |
| 5 | URP leftovers / `Shader.Find` | DONE / **URP package deferred** | Static shader cache in inventory/mining/loot paths; **`com.unity.render-pipelines.universal` still in manifest** (see §3) |
| 6 | Directors/Comms stubs | DEFERRED | World Engine directors — design not v1.6 defect |
| 7 | Mine/harvest SFX in player builds | DONE | Clips under `Resources/Audio/` |
| 8 | IMGUI crosshair double-draw | DONE | `RangedCombatHud` |
| 9 | ItemDataCreatorWindow layout poison | DONE | `ObsoleteEditorWindowLayoutCleanup` v3 |
| 10 | Book of the Dead / Gaia cache | SKIPPED | Not in playable scene |

**Deferred from log (may still be open work):** duplicate FBX cleanup, URP package removal, World Engine director implementation.

---

## 3. HDRP migration — locked decisions + what’s left (Oct 2026)

### Stale paths in archived migration plan

The archived `HDRP_Migration_Plan.md` names **`Assets/Dark Matter Genesis v1.56.unity`** and keeping v1.56 on URP until Phase 6. **Do not use those paths for agent work.**

| Topic | Canonical (Oct 2026) |
|--------|----------------------|
| Playable scene | `Assets/_Project/Scenes/Dark Matter Genesis v1.6.5.unity` |
| Render pipeline | Unity **6 HDRP** `6000.4.11f1` — project default **Genesis_HDRP_High** (Quality index **3**) per Phase 6 pass |
| WebGL | Retired — not a ship target |
| Platforms | PC and Mac first; Xbox and PlayStation later (GDD 5.0) |

### Decisions that remain valid (from migration plan)

- **Five quality tiers:** Performance (0) → Balanced → Quality → High (PC default) → Ultra; assets `Genesis_HDRP_*` under `Assets/Settings/HDRP/`.
- **Ray tracing:** Optional, **player-toggleable**, default **off**; advisories only (no hard gates) — see `System_Requirements.md`.
- **Console:** 60 FPS minimum on default tier; **no RT at ship** unless a future platform pass proves 60 FPS with RT.
- **Custom shaders:** Dual URP+HDRP SubShaders for dissolve, smoke, scanner (`HDRP_Vendor_Material_Audit.md`).

### Phase status (synthesized from migration plan, vendor audit, repair log, settings UI plan)

| Phase | Topic | Status (approx.) |
|-------|--------|------------------|
| 0–1 | HDRP foundation, five assets, editor menus | **Landed** |
| 2 | Quality wiring, bootstrap, settings stubs | **Partial** — tiers wired; many UI controls still plan-only |
| 3 | Code: cameras, post, scanner pass, dissolve | **Mostly landed** — `PostProcessingController` has HDRP paths; fine post controls still debt |
| 4 | `_Project` material bulk convert | **Done** for URP Lit/Unlit batch; customs dual-targeted |
| 5 | Vendor / third-party audit | **Playable scene clean** (Aug 2025 audit) — see remaining artist items below |
| 6 | Global switch to HDRP High | **Applied** (per vendor audit post–Phase 6 pass) |
| 7 | Cinematic HDR tuning + optional RT path | **Open** |
| 8 | 60 FPS certification matrix (PC, Mac, consoles) | **Open** |
| 9 | URP package removal, docs, rule cleanup | **Open** — URP **17.4.0 still in** `Packages/manifest.json` for dual-pipeline rollback |

### Active plans (not archived)

- **`Architecture/HDRP_Settings_UI_Plan.md`** — settings panel expansion (texture/shadow/AA, FOV, HDRP post overrides, RT apply, DX11/DX12 picker). Ray tracing toggle today **persists only**; does not flip pipeline/volume RT.
- **`Architecture/HDRP_Vendor_Material_Audit.md`** — living audit doc; refresh via `Tools/Dark Matter Genesis/HDRP/Audit Vendor Materials`.

### Vendor / shader stragglers (do not mass-convert)

From vendor audit — gameplay-referenced or artist reauthor only:

- QFX distortion (`GO_ScannableObject.mat`) — needs HDRP distortion or custom pass.
- Toon Deserted Temples fire/water custom shaders.
- Gaia **URP Water** leftover (`Shader Graphs/Water`) where used as lava stand-in.
- Unused pack catalogs (Hovl, JMO, Magic Spells demos), TMP examples, SpeedTree / full Gaia biomes — revisit with Io biome bring-up.

### Code / settings debt (grep + settings plan)

- **`PostProcessingController`:** HDRP branch exists; master post toggle must fully drive Genesis HDRP volume profile(s), not URP `UniversalAdditionalCameraData`.
- **Global volume:** Move off URP-era `SampleSceneProfile` to Genesis HDRP volume profile(s) + runtime overrides.
- **Runtime `Shader.Find`:** Reduced via caches (repair log #5); prefer serialized HDRP shader refs on new work.
- **URP package:** Keep until Phase 9 — dual SubShader customs and safe rollback.

### Editor menu reference (still valid)

`Tools/Dark Matter Genesis/HDRP/` — Phase 0/1 foundation, test scene, Phase 6 switch (historical if already run), vendor audit, folder URP→HDRP dry-run/apply, scene particle convert, L.V.E Lava Standard→HDRP.

---

## 4. When to open this archive

- Reverting or understanding the Aug 2026 animation-plan toggles.
- Forensics on the Aug 22 repair pass (branch context, duplicate FBX list).
- HDRP **historical** phase numbering and pre–v1.6.5 scene names.

For day-to-day engineering, use **INDEX §2**, `World_Engine_Disk_Status.md`, `HDRP_Settings_UI_Plan.md`, and `HDRP_Vendor_Material_Audit.md` instead.
