# Combat system (disk authority)

**Wiki page:** `Combat-System`  
**Repo canonical copy:** `Assets/_Project/Features/Combat/Documentation/Dark_Matter_Combat_System.md`  
**Design roadmap & acceptance:** `Assets/_Project/Documentation/Design/Combat/DMG_Combat_Plan_v2.md` (**Master Plan v2.4**, Oct 8, 2026)

When editing the [GitHub wiki](https://github.com/LittleWormOnAHook/Dark-Matter/wiki), keep **Features doc = engineering/disk map** and **plan v2.4 = phased roadmap & §31 gates** — do not merge them into one page without updating both repo files.

---

## Two-authority model

| Doc | Role |
|-----|------|
| `Dark_Matter_Combat_System.md` | Where code lives, hit flow, profiles, shipped vs planned |
| `DMG_Combat_Plan_v2.md` (**v2.4**) | Phase checklist, reaudit status, acceptance play-tests |

**Agent quick path:** Documentation INDEX **§1b** and **§2.3**.

## Scope (in module)

- **Player melee:** combos, block/charge, poise, hitbox tuning (Invector motor + DM wrappers).
- **Player ranged:** aim/fire, ammo types, shared resolver; Hot Cross UX (UITK).
- **Enemies:** FSM + utility brain (`DMEnemyBrain`), engagement director (melee tokens), hit marks / body zones.
- **FX & tuning:** `DMAmmoFxProfile`, Genesis Studio → Combat, Combat Studio; sandbox builder (forensics — tune in **v1.6.5**).

**Out of scope (plan §31):** NavMesh agents, Player_v7 retune, in-game LLM, §31 #4 Combat Director / Momentum / Sick Stick until prior gates close (Phase 4 is active next gate).

## Phase snapshot (Oct 8, 2026 — plan v2.4)

- **Phase 3 signed off:** utility brain + spacing Part A (`a94d0d899`); hit marks Part B (`6646fdc33`, `e692859f1`, `edfab9a6e`). Acceptance: v1.6.5 play-test + `DM_Enemy_Spacing_And_Hit_Marks_Plan.md` §10 checklists. §31 #3 **Done** in plan Reaudit.
- **Phase 4 next:** **Combat Director (§31 #4)** — encounter intensity, scaled attack slots, flanking, group morale. Phase 3 `DMEnemyEngagementDirector` remains D1 melee spacing; Phase 4 adds encounter-level director (plan §0 **Phase 4** block + §10).
- **Incremental polish (post–Phase 3):** per-ammo camera trauma (below); see plan Reaudit for commit refs.

## Per-ammo camera trauma

`DMAmmoFxProfile` exposes **`fireTrauma`** / **`impactTrauma`** (optional duration, impact radius, min distance). Player shots: `CombatProjectileSpawner` → `DMCombatCameraShake.TryPlayAmmoFire`; impacts: `DMCombatRangedResolver` → `TryPlayAmmoImpact` (same hub as parry/block/charged hits on `DM_CombatCoreProfile`). **Defaults are 0** until tuned in Genesis Studio → Combat → Ammo FX.

## Runtime roots (today)

- `Assets/_Project/Scripts/Combat/` — legacy runtime (`Project.Combat`).
- `Assets/_Project/Scripts/AI/` — enemy brain, engagement director.
- `Assets/_Project/Editor/Combat/` — Combat Studio.

`Features/Combat/` is **documentation + module index** until a deliberate runtime migration.

## Related wiki / design

- `Wiki/Combat-Moving-Melee-Layer-Policy.md` → `Design/Combat/DM_Melee_Locomotion_Layer_Policy.md`
- Plan **Combat-Plan** row in `Wiki/README.md` → full `DMG_Combat_Plan_v2.md`

Full code maps, non-goals, and studio hooks → **canonical Features doc** in repo.
