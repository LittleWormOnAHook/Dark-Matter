# Dismemberment Plan (draft, 2026-10-31)

Status: PLAN ONLY. Nothing implemented. Owner: Anthony. Written by Grok Bot. Updated 2026-10-31 3:01 PM with Anthony's answers (see Decisions).

## Goal
Shots carve wounds into enemies (blood spray + small flesh chunks). Limb hits can sever an arm/leg, which tumbles to the ground. A severed weapon arm drops its weapon. Leg loss = enemy falls and crawls away (disarmed, won't die until killed). Precise scoped hits can shoot a weapon out of a hand; disarmed enemies flee or hide behind cover.

## What we already have (read 2026-10-31)
- Unified ranged hit path: `CombatHitResolver` sets `DamageInfo.BodyPart` from `DMEnemyHitbox.Zone` (DMHitbox layer 26, `DMEnemyHitboxRig`), with fallback `DMCombatBodyPartUtility.ResolveFromCollider`. Zone multipliers + `DMScopeMode.DamageMultiplier(part)` are already applied.
- `CombatBodyPart` = None/Head/Torso/Arm/Leg. **No left/right and no upper/lower**, which we need for sever.
- Blood: `DMVolumetricBlood.TrySpawn` (KriptoFX BFX) for every blood path, `DMEnemyHitMarks` decals, `DM_EnemyHitMarkProfile` (31j blood brightness). Reuse for spray and stumps.
- Health: `EnemyHealth` (single pool, `TakeDamage`, `Died`, respawn/loot hold). Brain: `DMEnemyBrain` utility actions Press/Defend/**Retreat**/Hold, `DMEnemyCondition` Healthy..Critical, personalities incl. Cowardly.
- Ragdoll: `EnemyInvectorRagdollBridge` (`ActivateCorpseRagdoll`, `RestoreForRespawn`), `DMEnemyKnockdown`, `DMEnemyHitFlinch`, `DMEnemyBodyWeight` (spine/chest only; already stands down while down/ragdolled).
- Weapons: `EnemyInvectorWeaponHolderRebind` reparents holders onto live hand bones. **Rule: never move weapon placements.** A drop detaches a *copy/pickup*, not the holder.
- Scope: `DMScopeMode.IsScopedRifle`, `ReticleSway`, `ZoomBlend`. These are enough to gate "precise scoped hit".
- Animations on disk: Mixamo `Zombie Crawl`, `Crawl Backwards`, `Injured Walk/Run/Idle`, `Falling Down`, `Fall Flat`, `Stumble Backwards`; PROTOFACTOR GoToKneel/SitInjured. **A forward crawl exists (Zombie Crawl)** plus a backward crawl, so the crawl state is feasible without new mocap.
- Humadroid/robot types are referenced in brain/engagement profiles. They need a "mechanical" gore variant (sparks/oil, no blood).

## How other games do it (short)
- **Dead Space**: limbs are the main target. Pre-authored cut points per limb, per-limb health, enemies keep coming crawling. Lesson: pre-split meshes > runtime slicing.
- **Soldier of Fortune (GHOUL)**: 26 gore zones, damage-mapped wound textures. Lesson: zone wounds read great without geometry cuts.
- **Fallout VATS**: per-limb HP and crippled states (crippled leg = slow, crippled arm = drop/aim penalty). Lesson: crippled before severed.
- **Sniper Elite (X-ray)**: rewards precise shots. Lesson: tie sever/disarm to scoped precision and give feedback (slow-mo optional).
- **Dying Light / Dead Island 2 (FLESH)**: layered skin/flesh/bone masks + sever. Lesson: wound shader masks are the cheap high-impact part.
- **Metal Gear Rising**: free runtime slicing. Spectacular but expensive and art-unfriendly. Not for us.
- **Zombie Army / Ghost of Tsushima / Doom**: limited gore budgets, quick despawn, gore toggles; Doom/Mortal Kombat use authored finishers.
- **Unity options**: *Dismemberment* asset-store kits (pre-split bone-weighted parts), *Mesh Slicer / EzySlice* (runtime plane cut; skinned meshes need baking, so slow), *PuppetMaster* (active ragdoll, nice for falls but heavy and overlaps Invector ragdoll), *Gore Simulator*-style kits (bone-weight split + stump caps).

## Approach options
| Option | How | Pros | Cons |
|---|---|---|---|
| A. Wound masks + decals only | Shader/decal "carved" holes, blood, chunks | Cheapest, no mesh work | No real limb loss |
| **B. Bone-weight split at load (recommended)** | On first sever, bake the limb's triangles (by bone weight) into a separate skinned/rigid mesh; collapse those bones to scale 0 on the body; add a stump cap | Works with any humanoid, deterministic, fast after first bake, cacheable per mesh | Needs stump caps + seam tuning |
| C. Pre-split authored meshes | Artist splits each enemy mesh in DCC | Best quality | Art cost per enemy, Meshy pipeline friction |
| D. Runtime plane slicing | EzySlice on baked mesh | Cut anywhere | Expensive, ugly UVs, skinned problems |

**Recommendation: A + B.** Carving = wound masks/decals + blood + chunks (A). Sever = bone-weight split at fixed joints (shoulder/elbow, hip/knee, neck), with a pre-warmed cache per enemy mesh (B). Keep C as an upgrade for hero enemies.

## Slices (order, effort)
1. **S1 Zone detail + limb health** (S, ~1 day): extend the zone to side+segment (`LeftUpperArm`...) in `DMEnemyHitbox`/rig; add `DMEnemyLimbHealth` (per-limb pool fed from the same `DamageInfo`, *no change to EnemyHealth totals*). Crippled state at a threshold. Debug overlay.
2. **S2 Carved wounds + spray + chunks** (M, 2 days): wound decal/mask at the hit point (reuse `DMEnemyHitMarks`), directional spray via `DMVolumetricBlood`, pooled flesh-chunk prefab (5-8 small rigidbodies, 3-6 s life, shared pool cap).
3. **S3 Sever** (L, 3-4 days): bone-weight split + stump cap (red cap mesh/decal on both sides) + blood burst/short bleed. The severed part gets a Rigidbody/collider, inherits bone velocity + hit impulse + small random angular velocity (tumble, not straight down), sleeps after landing, despawns via pool.
4. **S4 Weapon drop + re-grab** (M, 1.5-2 days): arm with weapon severed, or disarm shot → spawn a *visual-only physics copy* (not a player pickup) at the hand pose and hide the held weapon renderer; holders/placements untouched. Enemy enters `Disarmed`. Re-grab: a disarmed enemy that still has a usable hand looks for its own dropped weapon or a nearby dropped weapon within reach radius, walks to it (no NavMesh: direct path + obstacle probe + timeout), plays a pickup, then un-hides its held weapon (same holder, nothing moved) and the dropped copy despawns. Copies despawn after the lifetime only when no enemy is going for them.
5. **S5 Leg loss → crawl** (M, 2-3 days): `Falling Down` → `Zombie Crawl` (forward, away from player) / `Crawl Backwards` (facing the player); custom locomotion speed ~0.4-0.8 m/s; hitbox rig follows the animation. **Crawlers cannot attack** (attacks/aim disabled, no re-grab). Limb loss and bleed never kill: HP floor of 1 from sever/bleed; only normal damage from the player kills a crawler (or a head sever). Body weight stays off (already off while down).
6. **S6 Scoped disarm + flee/cover** (M, 2-3 days): disarm only if `IsScopedRifle` + hit lands on the hand/weapon hitbox + sway below a threshold (+ optional range minimum). Brain: `Disarmed` condition boosts Retreat; new cover search (raycast/overlap for rocks/walls that break line of sight to the player, sampled ring, no NavMesh) → hide/peek; cowardly personality flees farther.
7. **S7 Polish** (M): decapitation (head sever = instant death; the ONLY sever that kills), mechanical variant for Humadroids/robots (full dismemberment: sparks, oil spray/decals, exposed wiring stump caps, metal chunks), audio (squelch, bone crack, limb thud, metal clang/servo whine), Studio panel, gore toggle.

## Studio settings (new `DM_DismembermentProfile`, Studio > Combat > Dismemberment)
Presets **Subtle / Default / Extreme** (Default selected):
| Setting | Range | Subtle | Default | Extreme |
|---|---|---|---|---|
| Gore master | Off/Low/Full | Low | Full | Full |
| Limb HP % of max HP | 5-200 | 60 | 35 | 15 |
| Sever needs crippled first | bool | yes | yes | no |
| Sever chance on killing limb hit | 0-1 | 0.25 | 0.6 | 1 |
| Min damage per hit to sever | 0-500 | 40 | 20 | 5 |
| Flesh chunks per hit | 0-20 | 1 | 3 | 8 |
| Limb tumble (angular vel deg/s) | 0-1080 | 90 | 240 | 540 |
| Limb impulse multiplier | 0-5 | 0.5 | 1 | 2 |
| Stump bleed seconds | 0-30 | 2 | 6 | 15 |
| Crawl speed m/s | 0.1-2 | 0.4 | 0.6 | 0.9 |
| Disarm max reticle sway (deg) | 0-5 | 0.3 | 0.6 | 1.2 |
| Disarm needs scope | bool | yes | yes | yes |
| Re-grab weapon allowed | bool | yes | yes | yes |
| Re-grab search radius m | 0-30 | 4 | 8 | 15 |
| Re-grab give-up seconds | 1-30 | 4 | 8 | 15 |
| Dropped weapon lifetime s (if unclaimed) | 5-300 | 20 | 45 | 120 |
| Max severed parts alive | 0-64 | 6 | 12 | 32 |
| Max chunks alive | 0-256 | 24 | 64 | 160 |
| Part/chunk lifetime s | 1-120 | 10 | 30 | 60 |
Plus per-enemy-type overrides (organic/mechanical, can sever, can crawl).

## What we should add (you didn't list these)
- **Per-limb health and crippled state** before sever (slowed leg, weak/inaccurate arm).
- **Sever thresholds** (minimum damage, weapon type, e.g. shotgun/sniper/melee blade > SMG).
- **Stump caps** on both pieces, and a short **bleed-out** VFX (cosmetic; decide whether it does damage).
- **Kill rule**: only a head sever kills via dismemberment; limb severs and bleed never kill directly.
- **Ragdoll handoff**: death while crawling/limbless → Invector corpse ragdoll with the missing bones collapsed.
- **Decapitation** = instant kill + head part.
- **Mechanical enemies** spark, leak oil, drop metal bits instead of blood.
- **Body-weight balance shift**: one arm lost → spine leans toward the remaining side, sway increases; one leg lost → falls. Fits the body-weight rule (visual only, spine/chest).
- **Audio**, light haptic/hit-stop on sever.
- **Gore toggle/settings** (off/low/full) and an option to disable for streaming.
- **Pooling/perf caps**, cleanup on despawn/respawn: `RestoreForRespawn` must restore bones/meshes; loot hold must keep parts until looting ends.
- **Save/despawn**: don't persist parts; respawned enemies come back whole.
- **Dropped weapons** are visual only (no player pickup); enemies may re-grab them.
- **Friendly fire/companions**: keep gore off companions and the player unless desired.

## Risks and perf
- Skinned mesh split cost: bake once per mesh type and cache (do it in a loading pass); never bake per hit.
- Meshy avatars/weapon-holder rebind: bone collapse (scale 0) must not touch holder transforms, so hide the weapon renderer instead. Test with the rebind.
- Hitbox rig: disable hitboxes of removed bones; the crawl pose must keep hitboxes aligned.
- Animator: crawl needs a new layer/state in the enemy controller; Mixamo clips need humanoid retarget checks (Zombie Crawl root motion vs custom locomotion).
- KriptoFX blood cost: share the existing pool and caps; HDRP decal count cap.
- Physics: severed parts on their own layer (no player collision, no pushing enemies), sleep fast.
- Stuck crawlers: no NavMesh, so crawl/cover need simple obstacle probing plus a timeout to give up and stop.

## Decisions (resolved 2026-10-31 3:01 PM)
1. Crawling enemies **cannot attack**. RESOLVED (S5).
2. Player **cannot pick up** dropped weapons; they are visual and despawn. RESOLVED (S4).
3. Decapitation **is in**; **only a head sever kills**; limb severs never kill directly. RESOLVED (S3/S5/S7).
4. Robots/Humadroids **can be dismembered** (sparks/oil/wiring variant). RESOLVED (S7).
5. Disarmed enemies **can re-grab** their own or a nearby dropped weapon if reachable. RESOLVED (S4/S6).
