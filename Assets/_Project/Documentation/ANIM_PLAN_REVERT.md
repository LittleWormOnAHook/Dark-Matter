# Player v7 Animation Plan — Revert Guide

This change is reversible without touching `Assets/Invector-3rdPersonController/Shooter/Animator/Invector@ShooterMelee.controller` and without changing enemy/companion controller assignments.

## Fast disable (Play Mode, no git)

Select the Pioneer player (`Player_v7`). Find **Pioneer Animation Plan Settings**.

Uncheck all three feature bools:

- `Enable Unarmed Hang When Drawn`
- `Enable Draw Holster Anims`
- `Enable Hit Reaction Chance`

When those three are false, player behavior matches the pre-plan live game (stock OnlyArms / UpperBody, instant equipment swap, 100% Invector hit reactions).

The component is added at runtime by `PioneerInvectorBootstrap` if it is missing. Values reset to script defaults on Play unless you add the component to the prefab yourself.

Enemy HitDirection mapping (`enableEnemyDirectionalHits`, default on) is a separate bugfix on `EnemyInvectorRagdollBridge`. Turn that bool off on an enemy prefab if you need the old 0/1/2/3 values. It is independent of the player toggles.

## Files changed

- `Assets/_Project/Scripts/Player/Invector/PioneerAnimationPlanSettings.cs` *(new)*
- `Assets/_Project/Scripts/Player/Invector/PioneerInvectorBootstrap.cs`
- `Assets/_Project/Scripts/Player/Invector/PioneerShooterMeleeInput.cs`
- `Assets/_Project/Scripts/Player/Invector/PioneerInvectorWeaponBridge.cs`
- `Assets/_Project/Scripts/Player/Invector/PioneerInvectorSurvivalBridge.cs`
- `Assets/_Project/Scripts/AI/Invector/EnemyInvectorRagdollBridge.cs`
- `Assets/_Project/Documentation/ANIM_PLAN_REVERT.md` *(this file)*

Not modified:

- `Assets/Invector-3rdPersonController/Shooter/Animator/Invector@ShooterMelee.controller`
- Player / enemy / companion prefab YAML
- Enemy or companion animator controller assignments

## Git checkout (full revert of these files)

From the repo root:

```
git checkout -- "Assets/_Project/Scripts/Player/Invector/PioneerInvectorBootstrap.cs"
git checkout -- "Assets/_Project/Scripts/Player/Invector/PioneerShooterMeleeInput.cs"
git checkout -- "Assets/_Project/Scripts/Player/Invector/PioneerInvectorWeaponBridge.cs"
git checkout -- "Assets/_Project/Scripts/Player/Invector/PioneerInvectorSurvivalBridge.cs"
git checkout -- "Assets/_Project/Scripts/AI/Invector/EnemyInvectorRagdollBridge.cs"
git checkout -- "Assets/_Project/Documentation/ANIM_PLAN_REVERT.md"
```

Then delete the new settings script (and its `.meta` if Unity created one):

```
git clean -n -- "Assets/_Project/Scripts/Player/Invector/PioneerAnimationPlanSettings.cs"
```

If it is untracked, remove the file:

```
del "Assets\_Project\Scripts\Player\Invector\PioneerAnimationPlanSettings.cs"
del "Assets\_Project\Scripts\Player\Invector\PioneerAnimationPlanSettings.cs.meta"
```

Do **not** checkout the shared shooter melee controller. It was not edited.

## How to verify

1. **Walk with pistol drawn, not aiming** — arms should hang unarmed (OnlyArms weight toward 0, UpperBody_ID 0). Toggle `Enable Unarmed Hang When Drawn` off: armed OnlyArms pose returns.
2. **ADS / reload / melee** — aiming restores OnlyArms + weapon UpperBody_ID. Reload and melee still play. Two-hand rifles hang only while `Include Two Hand Ranged In Hang` is on.
3. **Holster / draw pistol** — `LowBack` CrossFade, mesh swap delayed ~0.2–0.4s. **Rifle** uses `HighBack`. Toggle `Enable Draw Holster Anims` off: instant swap as before.
4. **Get hit at full HP** — small `TriggerReaction` (ReactionID 0) about 10–25% of hits; most hits have no animator reaction. **HP below 50% or crit** prefers big ReactionID 1, chance lerping toward 25% as HP → 0. HitDirection is 0 / 90 / -90 / 180 (front / right / left / back). Toggle `Enable Hit Reaction Chance` off: every Invector hit reacts (stock).
5. **Enemy hits** — humanoid `HitDirection` is 0 / 90 / -90 / 180 even when player toggles are off (unless `Enable Enemy Directional Hits` is unchecked on that enemy).
