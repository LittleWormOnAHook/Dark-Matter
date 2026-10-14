# DM Script Audit and Slimming Plan (REPORT ONLY)

Status: report. **Nothing was removed or migrated from this audit.** Only the already-approved Phase 2 sync changes were applied (see DM_Enemy_Script_Sync_Plan.md). Every UNUSED / USED-IN-PART item below needs Anthony's confirmation first.

Method: the loaded contents of all 9 enemy prefabs were enumerated through the Unity API (71 component types), then every one of the 2,888 .cs files under Assets was indexed by identifier. "Referenced" means another .cs file (runtime DM / editor / vendor) names the type; per-member usage means the member name appears in some other file. Animation-event function names from every .anim/.fbx.meta/.controller were treated as references. Private members used only inside their own file are NOT reported as unused (the numbers below only describe the externally referenced surface). Nested weapon prefabs make the YAML byte counts of the enemy prefabs undercount the real component totals, so component counts come from the loaded prefab (per template: root+children).

## 1. Classification table (components on the template; the other enemies match after the sync)

| Script | Class | Per template (root+children) | References | Evidence | Migration / action |
|---|---|---|---|---|---|
| vThirdPersonController | **REQUIRED** | 1+0 | dm 30 / editor 12 / vendor 10 | Locomotion motor the Motor/Bootstrap/Combat bridges drive (30 DM files reference it). | Keep. Later: only if the motor is replaced. |
| vRagdoll | **REQUIRED** | 1+0 | dm 17 / editor 2 / vendor 1 | Ragdoll state used by EnemyInvectorRagdollBridge / HumanoidPerformanceController (17 DM files). | Keep. |
| vMeleeManager | **REQUIRED** | 1+0 | dm 23 / editor 5 / vendor 8 | Melee hit pipeline; LoadoutBridge/Combat bridge call it (23 DM files). | Keep. |
| vMeleeWeapon | **REQUIRED** | 0+8 | dm 11 / editor 5 / vendor 6 | Weapon component on every Drawn_ melee slot; read by the LoadoutBridge and the weapon bridge. | Keep. |
| vMeleeAttackObject | **REQUIRED** | 0+4 | dm 6 / editor 1 / vendor 4 | Base of vMeleeWeapon; hit-box owner. | Keep. |
| vHitBox | **REQUIRED** | 0+18 | dm 11 / editor 5 / vendor 1 | Melee hit volumes (11 DM files, WeaponHitbox/CombatHitResolver). | Keep. |
| vShooterManager | **REQUIRED** | 1+0 | dm 22 / editor 5 / vendor 2 | Ranged manager (22 DM files). The plain vShooterManager now replaces PioneerShooterManager everywhere. | Keep the vendor script; DM-owned ranged state stays in the bridges. |
| vShooterWeapon | **REQUIRED** | 0+2 | dm 19 / editor 2 / vendor 9 | Weapon component on every Drawn_ ranged slot (19 DM files). | Keep. |
| vBodySnappingControl | **REQUIRED** | 0+1 | dm 3 / editor 6 / vendor 1 | EnemyInvectorBodySnapSetup / bootstraps use LoadBones/GetBone to place weapon visuals. | Keep (USED-IN-PART candidate later: 2 members used). |
| vSnapToBody | **REQUIRED** | 0 | dm 4 / editor 4 / vendor 0 | Hand-placed snap data on Humanoid/Robot/Hybrid/Evil One; EnemyInvectorBodySnapSetup uses it. Do not touch. | Keep. |
| vDamageReceiver | **REQUIRED** | 0 | dm 1 / editor 3 / vendor 2 | The_Evil_One only (11 children) - hand data on its body; vShooterDamageReceiver depends on it. | Keep; migrate Evil One to PioneerInvectorDamageReceiver proxies later (needs a decision). |
| vCollectableStandalone | **USED-IN-PART** | 0+10 | dm 1 / editor 0 / vendor 3 | Only `targetEquipPoint` is read (PioneerInvectorWeaponBridge.cs:2185); the bridge disables/destroys the component on every built visual. 10 per prefab. | Move `targetEquipPoint` onto ItemData/EquippedVisualMarker, stop copying the pickup trigger onto slots, then remove. Risk: the weapon bridge and vCollect*Control (vendor) reference it. |
| vWeaponHolder | **UNUSED** | 0+7 | dm 0 / editor 0 / vendor 1 | No DM code references it and no vWeaponHolderManager is on an enemy; weapon visuals are found by slot NAME. 7 per prefab. | Remove after one play test; keep the holder transforms (the bridge looks them up by name). |
| vComment | **UNUSED** | 0+16 | dm 0 / editor 1 / vendor 0 | Editor-only note (only vCommentEditor references it). 15-16 per prefab. | Remove from existing slots; already left off new slot clones. |
| vEntityPreview | **UNUSED** | 0+4 | dm 0 / editor 0 / vendor 0 | Editor gizmo preview; no references. 4 per prefab. | Remove. |
| vInstantiate | **UNUSED** | 0+1 | dm 0 / editor 1 / vendor 0 | Only the editor menu references it. 1 per prefab. | Remove. |
| vWeaponConstrain | **UNUSED** | 0+8 | dm 0 / editor 0 / vendor 0 | Freezes a weapon Rigidbody on drop; enemy weapon visuals have no Rigidbody and no code calls it. 7-8 per prefab. | Remove (left off new slot clones). |
| vHitEffects | **UNUSED** | 0+8 | dm 0 / editor 0 / vendor 0 | No code references it (the hit sound path was not traced; confirm in a play test). 7-8 per prefab. | Remove after checking melee hit SFX in a play test (left off new slot clones). |
| vWeaponCollectableControl | **UNUSED** | 0+2 | dm 0 / editor 0 / vendor 0 | Pickup equip/drop control; nobody calls OnEquip/OnDrop on enemies. 2-3 per prefab. | Remove with vCollectableStandalone migration. |
| vLaserSight | **UNUSED** | 0+2 | dm 3 / editor 0 / vendor 0 | Only player-side DM scripts (mining controller, hitscan follow) reference it; enemies never use a laser sight. 1-3 per prefab. | Remove from enemies. |
| vPlayRandomClip | **USED-IN-PART** | 0+1 | dm 0 / editor 1 / vendor 0 | Editor menu references it; runtime `Play()` may be wired through UnityEvents on weapon prefabs (NOT verified; the check is step 3 below). | Verify the shot SFX path, then migrate to the AmmoFx audio and remove. |
| vAnimatorEventReceiver | **USED-IN-PART** | 0+2 | dm 0 / editor 1 / vendor 0 | Only the editor menu references it; it only matters if the enemy Animator controller uses vAnimatorEvent state behaviours. | Check the enemy controller for vAnimatorEvent behaviours; if none, remove (2 per prefab). |
| UniversalAdditionalLightData | **UNUSED** | 0+2 | dm 0 / editor 1 / vendor 3 | URP component on lights in an HDRP project; nothing in DM uses it. 2-3 per prefab (Gaia URP utils only). | Remove; HDAdditionalLightData is the HDRP equivalent. |
| EquippedVisualMarker | **REQUIRED** | 0+20 | dm 6 / editor 0 / vendor 0 | Weapon bridge / ItemPickup / stripper bind slot visuals to items (6 DM files). 18-22 per prefab. | Keep; could become 1 per slot-root instead of per mesh. |
| PioneerInvectorDamageReceiver | **USED-IN-PART** | 1+0 | dm 6 / editor 1 / vendor 0 | Root hit pipeline (7 DM files) but `ApplyToPioneerHealth`, `PlayMeleeHitAudio`, `SpawnHitVfx`, `applyToChildren` are not referenced. | Rename to DMDamageReceiver and drop the 4 unused members; editor scripts that reference it: EnemyInvectorSetupUtility. Risk: serialized script GUID changes on every prefab (do with a MonoScript rename, not by recreating). |
| PioneerShooterManager | **UNUSED (on enemies)** | 0 | dm 4 / editor 1 / vendor 0 | Player shooter manager; replaced by plain vShooterManager on the template and all enemies in this pass. | Done for enemies; stays for the player. |
| PlayerProgressionManager | **UNUSED (on enemies)** | - | - | Player singleton; removed from the template and the 3 creator outputs. | Done. |
| DMIGrenadeCookController / DMIGrenadeThrowBridge | **OPTIONAL** | - | - | Player grenade scripts. Allowed only when EnemyDefinition.canThrowGrenades is on; brain-driven throwing comes later (the flag is the hook). | Done (flag). Robot and The_Evil_One have it on. |
| DMIMaterialPulseScroll, ProBuilderMesh/Shape, MeshCollider, HDAdditionalLightData, LensFlareComponentSRP | **UNUSED (on enemies)** | 0 | dm 3 / editor 1 / vendor 0 | Mining-tool cone and flashlight leftovers on 3 creator outputs. | Removed with the player-only children (Drawn_/Holstered_DM_Mining_Tool, flashlight mesh). |
| EnemyDeathSequence / EnemyInvectorDeathPresenter | **USED-IN-PART (near-duplicates)** | 1+0 | dm 12 / editor 3 / vendor 0 | Two death scripts on every enemy (214 + 394 lines). Both are referenced (12 and 2 DM files) and both run on death; they overlap on disintegration/ragdoll hand-off. | Merge the overlapping hand-off into EnemyInvectorDeathPresenter; needs a play test of death + disintegration. Not now. |
| EnemyInvectorRagdollBridge / vRagdoll / HumanoidPerformanceController | **REQUIRED (large)** | 1+0 | dm 16 / editor 1 / vendor 0 | 49.8 KB bridge: biggest DM script on the prefab. Public surface is used (16 files); most of the size is private logic. | Keep; split by feature later if wanted. |
| EnemyDisintegrationEffect | **REQUIRED (large)** | 1+0 | dm 5 / editor 3 / vendor 0 | 41 KB; used on death by 5 DM files. | Keep. |
| DMEnemyHitMarks / DMEnemyHitboxRig / CombatPoise | **REQUIRED** | 1+0 | dm 8 / editor 2 / vendor 0 | Hit marks, per-bone hitboxes (layer DMHitbox 26) and poise; Bootstrap Ensure()s the rig and poise at runtime. Now baked on every enemy. | Keep. |

## 2. Near-duplicate pairs

| Pair | Same behaviour? | Parts actually used | Verdict |
|---|---|---|---|
| `vShooterManager` vs `PioneerShooterManager` | Pioneer extends vShooterManager (player ammo via WeaponAmmoState) | Enemies use only the base (112 externally referenced members); Pioneer adds 10 members used by the player and the stripper deletes its ammo state on enemies | Enemies = plain vShooterManager (DONE on template + 7 enemies). Player keeps Pioneer. |
| `vDamageReceiver` vs `PioneerInvectorDamageReceiver` | Both forward damage to health | Pioneer: TakeDamage + 2 events (used by CombatHitResolver, hit rig). vDamageReceiver: TakeDamage/OnReceiveAttack (11 on The_Evil_One only) | Keep Pioneer as the single receiver; migrate Evil One later (decision). |
| `EnemyDeathSequence` vs `EnemyInvectorDeathPresenter` | Overlapping death hand-off | Both run on death | Merge candidate (play test needed). |
| `EnemyHealth` vs `CompanionHealth` | Same pattern for companions | EnemyHealth 39 members used by 60 DM files | Different actors; keep both. |
| `EnemyTerrainRescue` vs `PioneerTerrainRescue` | Same idea (4.3 KB player version) | Enemy version 6 members, all used | Keep; player version is not on enemies. |
| `EnemyInvectorBootstrap` / `PioneerInvectorBootstrap` / `CompanionInvectorBootstrap` | Three bootstraps wiring Invector | Only the Enemy one is on enemies | Project-level twin; unify later (outside the enemy sync). |
| `*InvectorDamageBridge`, `*InvectorProjectileBridge` Pioneer vs Companion | Same bridge per actor type | Not on enemies | Project-level twins; not touched. |
| `vCollectableStandalone` vs `EquippedVisualMarker` | Both tag a weapon visual to an item | Collectable: 1 field read; Marker: item binding | Move the one field, drop the collectable (USED-IN-PART). |

## 3. Estimated savings (if every UNUSED item is removed from the template and the 8 enemies)

Per prefab (template numbers; the other enemies are within 1-4 components): vComment 15, vWeaponConstrain 7-8, vHitEffects 7-8, vWeaponHolder 7, vEntityPreview 4, vWeaponCollectableControl 2-3, vLaserSight 1-3, UniversalAdditionalLightData 2-3, vInstantiate 1, vAnimatorEventReceiver 2 (if the controller does not need it) = about 50 of 690 components (7%), roughly 25-40 KB of serialized YAML per prefab. vCollectableStandalone adds 10 more per prefab after its migration. The sync already removed 77 components from each creator output (865 -> 788, mining tool + flashlight + Pioneer manager) and 23% of the template file (2.20 MB -> 1.69 MB).

## 4. Recommended order (each step after Anthony confirms; play-test one melee + one pistol enemy after each)

1. UNUSED editor-only / inert: vComment, vEntityPreview, vInstantiate, vWeaponConstrain, UniversalAdditionalLightData (zero runtime effect).
2. UNUSED with a one-time check: vWeaponHolder, vHitEffects, vLaserSight, vWeaponCollectableControl.
3. Verify then remove: vAnimatorEventReceiver (enemy controller check), vPlayRandomClip (shot SFX path).
4. USED-IN-PART: vCollectableStandalone -> targetEquipPoint on ItemData/EquippedVisualMarker.
5. USED-IN-PART: PioneerInvectorDamageReceiver rename + drop 4 unused members.
6. Merge EnemyDeathSequence into EnemyInvectorDeathPresenter.
7. Evil One vDamageReceiver -> single receiver (decision).

## 5. Migration steps and risks for the USED-IN-PART items

- **vCollectableStandalone.** Steps: add `targetEquipPoint` to ItemData (or EquippedVisualMarker), set it from the current slot value, change PioneerInvectorWeaponBridge.cs:2185 to read it, delete the 10 components per prefab, delete the destroy/disable loops at :2098/:2131. Dependencies: vCollectShooterMeleeControl, vCollectMeleeControl, vWeaponCollectableControl (vendor), vTriggerGenericAction base; editor scripts that mention it: none besides the vendor inspectors. Risk: the player prefabs still use it, so the vendor script must stay in the project.
- **PioneerInvectorDamageReceiver.** Steps: remove ApplyToPioneerHealth/PlayMeleeHitAudio/SpawnHitVfx/applyToChildren, rename the class with a MonoScript rename (keeps GUID). Referenced by CombatHitResolver, EnemyInvectorHitSetup, PioneerRagdollBoneDamageProxy, CombatHitAudio, TrainingDummy, DMEnemyHitboxRig, EnemyInvectorSetupUtility. Risk: the 66 prefabs that carry it (player included).
- **vPlayRandomClip / vAnimatorEventReceiver.** Steps: confirm no AnimatorController state behaviour of type vAnimatorEvent and no UnityEvent target `Play` on enemy weapons; then remove. Risk: silent loss of weapon fire/footstep SFX.
- **EnemyDeathSequence + EnemyInvectorDeathPresenter.** Steps: list shared responsibilities (disintegration trigger, ragdoll hand-off, loot drop), keep one owner, redirect 12 + 2 DM references. Risk: death flow regressions; needs a death play test.

## 6. What the sync deliberately did NOT add

Scripts in the UNUSED class are never added by Sync to Template and are left off new Blade_Of_Chaos slot clones (3 components per slot left out). Existing instances of them were not removed. The manifest list lives in `DMEnemyTemplateSync.UnusedPendingAudit`.

