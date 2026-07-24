# Combat — Weapon Base Stats

**Status:** Implemented (July 2026)  
**Code:** `Scripts/Data/ItemData.cs`, `Scripts/Combat/`, `Scripts/Progression/`, `Scripts/UI/ItemTooltipFormatter.cs`  
**Authoring:** `Editor/WeaponPrefabCreatorWindow.cs`, `Editor/ProjectileAmmoCreatorWindow.cs`

---

## Model

Each weapon `ItemData` owns **base combat stats**. Category-wide skills and player level multiply/add on top. Per-weapon upgrade modules are **deferred** and will stack onto these bases later.

```
Weapon base → category skill → level damage mult → (ammo ballistic/VFX/status only)
```

**Damage ownership:** the **weapon** rolls damage (`RollMeleeDamage` / `RollRangedDamage`). Loaded ammo no longer replaces the damage roll; ammo still overrides speed/spread/accuracy when authored (>0), plus VFX, splash, and status effects.

---

## Melee base stats

| Field | Role |
|-------|------|
| `meleeDamage` + `meleeDamageRandomRange` | Hit damage band |
| `criticalChance` | 0–1 crit roll (`RollCriticalHit`) |
| `criticalDamageMultiplier` | Crit payload |
| `meleeRange` | Reach |
| `meleeCooldown` / `attackAnimationSpeed` | Tempo |
| `meleeStaminaCost` | Drain per swing (`0` = no drain from this field) |
| `meleeKnockback` | Authored impulse (hit-path force reserved / follow-up) |
| `gatherPower` | Harvest strength |

---

## Ranged base stats

| Field | Role |
|-------|------|
| `rangedDamage` + `rangedDamageRandomRange` | Weapon DPS band |
| `weaponAccuracy` | 0–100 cone tighten |
| `projectileSpreadDegrees` + hip/ADS/close-range fields | Cone |
| `recoilVertical` / `recoilHorizontal` / `recoilFireRateScale` | Camera kick (`PioneerInvectorRecoilUtility`) |
| `fireRate` | ROF |
| `rangedRange` | Effective range / aim ray |
| `magazineSize` | Capacity |
| `reloadTimeSeconds` | Pushed to Invector `vShooterWeapon.reloadTime` when > 0 |
| `projectileSpeed` | Fallback if ammo does not override |
| `aimFovMultiplier` | ADS FOV |

If both recoil fields are ~0, recoil falls back to grip defaults (pistol vs two-hand rifle bands).

---

## Skills (category-wide)

| Skill | Modifier | Effect |
|-------|----------|--------|
| Blade Training | `MeleeDamageFlat` | +2 melee damage / rank (all melee) |
| Marksman Training | `RangedDamageFlat` | +2 ranged damage / rank (all ranged) |
| Weapon Accuracy | `WeaponAccuracyPercent` | +5% accuracy / rank (all ranged) |

Level weapon damage multiplier (+3%/level) still applies to both melee and ranged rolls.

---

## UI

- Inventory hover (`ItemTooltipFormatter`): melee + ranged **base** lines and muted **effective** damage/accuracy.
- Character panel: Melee Damage, Ranged Damage, Accuracy bars (effective values).

---

## Authoring

1. Prefer **Weapon Prefab Creator** for new weapons — fills base-stat fields + presets.
2. Tune velocity primarily on **ammo** `projectileSpeed`; weapon speed is fallback.
3. Seed recoil on each ranged weapon to match intended kick (see pistol ~2.75↑ / rifle ~0.65↑).

---

## Deferred

- Per-weapon upgrade / attachment stat modules stacking on bases
- Full melee knockback physics application from `meleeKnockback`
- Optional recoil-reduction skill
