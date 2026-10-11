# DM Combat Slice 0 Tuning Log (2026-10-11)

Source: `combat_reference_plan.md` section 6A, Tier 1 ("profiles and definitions only, no code"). Applied by editing the ScriptableObject assets through the Unity editor (SerializedObject), so GUIDs and formatting are kept. No code, prefab, scene or Invector asset was changed. Savepoint to restore from: `rc_deploy/savepoint_20261011_combat_slice0/` (see its RESTORE.md; the original files are under `files/`).

Definitions stay the authority: speeds, turn and attack cooldown are edited on each `EnemyDefinition`; cadence and spacing timers are edited on the shared Resources/Combat profiles. Per-enemy profile overrides (`engagementProfileOverride` / `brainProfileOverride`) on GreySpire Soldier, Humadroid, Lisa_Hybrid and Robot_Enemy point at the same shared profile assets (same GUIDs), so they follow automatically.

## Kept unchanged on purpose
Phase 3 D1 spacing: one melee engager, hold-ring radii and hold bands, slot counts, `maxSimultaneousAttackers`, `holderMaxDriftDegPer10s`, `sidestepMaxArc`, `postSwingLockSeconds` 0.6, charged wind-up pace (`windupPlaybackSpeed` 0.55, `chargedTrackRate`/`chargedTrackUntil`), `missRate`, Player_v7, all Invector assets. Not edited: GreySpire Soldier, Test, and the Non Human Enemies definitions (not in the Tier 1 list). Axe_Droid keeps run 5.2, Robot_Enemy keeps run 3.6 (they stay slower on purpose: Axe turn 9, Robot turn 8, Robot cooldown 1.3).

Template sync: `DMEnemyTemplateSync` Tuning item only follows prefab-component fields (chase stamina pause, standoff, ragdoll stagger/reaction chance, poise). None of those were touched and every field changed here is definition/profile data that the sync never reads, so the Tuning item has nothing to update and reports no new mismatches.

## Old -> new

| Asset | Field | Old | New |
|---|---|---|---|
| Humanoid_Enemy | runSpeed | 4.8 | 5.2 |
| Humanoid_Enemy | chaseSpeedMultiplier | 0.88 | 0.95 |
| Humanoid_Enemy | attackCooldown | 1.4 | 1 |
| The_Evil_One | turnSpeed | 8 | 12 |
| The_Evil_One | runSpeed | 4.8 | 5.2 |
| The_Evil_One | chaseSpeedMultiplier | 0.88 | 0.95 |
| The_Evil_One | attackCooldown | 1.4 | 1 |
| FRED_Android | turnSpeed | 8 | 12 |
| FRED_Android | runSpeed | 4.8 | 5.2 |
| FRED_Android | chaseSpeedMultiplier | 0.88 | 0.95 |
| FRED_Android | attackCooldown | 1.4 | 1 |
| Humadroid | turnSpeed | 8 | 12 |
| Humadroid | runSpeed | 4.8 | 5.2 |
| Humadroid | chaseSpeedMultiplier | 0.88 | 0.95 |
| Humadroid | attackCooldown | 1.4 | 1 |
| Lisa_Hybrid | turnSpeed | 8 | 12 |
| Lisa_Hybrid | runSpeed | 4.8 | 5.2 |
| Lisa_Hybrid | chaseSpeedMultiplier | 0.88 | 0.95 |
| Lisa_Hybrid | attackCooldown | 1.4 | 1 |
| Axe_Droid | turnSpeed | 5 | 9 |
| Axe_Droid | chaseSpeedMultiplier | 0.88 | 0.95 |
| Axe_Droid | attackCooldown | 1.2 | 0.9 |
| corrupt_patrol_android | turnSpeed | 8 | 12 |
| corrupt_patrol_android | chaseSpeedMultiplier | 0.88 | 0.95 |
| corrupt_patrol_android | attackCooldown | 1.2 | 0.9 |
| Robot_Enemy | turnSpeed | 5.6 | 8 |
| Robot_Enemy | chaseSpeedMultiplier | 0.88 | 0.95 |
| Robot_Enemy | attackCooldown | 1.61 | 1.3 |
| DM_EnemyEngagementProfile | engagerTurnRate | 200 | 300 |
| DM_EnemyEngagementProfile | nonAttackTurnRate | 260 | 360 |
| DM_EnemyEngagementProfile | lightTrackRate | 120 | 180 |
| DM_EnemyEngagementProfile | lightTrackUntil | 0.3 | 0.4 |
| DM_EnemyEngagementProfile | holderTurnRate | 90 | 150 |
| DM_EnemyEngagementProfile | holderWalkSpeed | 1.4 | 1.9 |
| DM_EnemyEngagementProfile | creepSpeedFactor | 0.62 | 0.75 |
| DM_EnemyEngagementProfile | handoffGraceBeforeSwing | 0.6-1.2 | 0.3-0.7 |
| DM_EnemyEngagementProfile | tokenMinHoldSeconds | 2.5 | 1.8 |
| DM_EnemyEngagementProfile | minSecondsBetweenHandoffs | 1.5 | 1 |
| DM_EnemyEngagementProfile | reawardCooldown | 1.5 | 1 |
| DM_EnemyEngagementProfile | switchConfirmSeconds | 0.35 | 0.25 |
| DM_EnemyEngagementProfile | holderActionInterval | 3.5-6 | 2-4 |
| DM_EnemyEngagementProfile | sidestepCooldown | 4 | 2.5 |
| DM_CombatDirectorProfile | slotMinHoldSeconds | 1.5 | 1 |
| DM_CombatDirectorProfile | slotEntryGrace | 0.5-1 | 0.3-0.6 |
| DM_CombatDirectorProfile | minSecondsBetweenSlotChanges | 0.6 | 0.4 |
| DM_CombatDirectorProfile | slotReawardCooldown | 1.5 | 1 |
| DM_EnemyBrainProfile | decisionInterval | 0.8 | 0.4 |
| DM_CombatCoreProfile | enemyMeleeMissStepSeconds | 0.55 | 0.4 |

Notes: `turnSpeed` converts to deg/s as clamp(turnSpeed x 18, 40, 220): 12 = 216 deg/s, 9 = 162, 8 = 144. Unity also wrote a few missing-field defaults into some definition assets on save (e.g. Axe_Droid `rangedHoldDistance` 0, `rangedRetreatFraction` 0.6, `kiterRetreatBoost` 1.5; CombatCoreProfile `enableGrenades` 0). These equal the in-code defaults the assets were already using at runtime, so behavior is unchanged.
