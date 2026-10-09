# Dark Matter Genesis — Enemy Spacing & Bullet Hit Marks: Plan (no code yet)

**Status:** PLAN ONLY, rev 3 (2026-10-07). This revision applies Anthony's answers to the 3 remaining open questions (D6–D8) on top of rev 2's D1–D5. Nothing in the project has been changed.

**Build status (2026-10-08):** Part A + Part B **shipped and accepted** — Combat Plan **v2.4** / §31 #3 signed off Oct 8, 2026 after v1.6.5 play-test (this doc §10 checklists). Commits: brain + spacing `a94d0d899`; hit marks `6646fdc33`, `e692859f1`, `edfab9a6e`. **Next combat gate:** Phase 4 Combat Director (§31 #4) — not spacing re-work unless regression.

**Historical (2026-10-07 build notes):** Part B built first; Part A merged into Phase 3 same week. Detail below reflects pre-acceptance disk state; defer to plan Reaudit for current truth.
- A1 + A2 are on disk: `AI/Brain/DMEnemyEngagementDirector.cs`, `DM_EnemyEngagementProfile.cs` (+ `Resources/Combat/DM_EnemyEngagementProfile.asset`), `AI/EnemyAiController.Engagement.cs` (Hold state, hysteresis, facing, holder actions), and edits to `EnemyAiController*.cs`, `EnemyCombat.cs`, `DMEnemyMeleeComboDriver.cs` and `DMILocomotionFacing.cs`.
- A3 is partial. The profile shows in Genesis Studio → Combat → "Director & tokens", and there are selected-enemy gizmos plus a "DM/Log Brain + Engagement Report" context menu. The UI Toolkit dev overlay is deferred.
- Differences from this plan:
  - The current token holder keeps the token through stagger and knockdown. Only challengers are blocked by them.
  - There is no LOS raycast in eligibility.
  - The taunt is a guard raise, since there are no taunt clips yet.
  - Role-aware dodge and the Engager cooldown strafe are deferred.
  - The ranged pool is defined but unused, so ranged enemies keep the legacy logic.
- Rollback: set `enableEngagementDirector` to off. The legacy ring rotation sits behind `legacyRingSlotRotation`.
**Project:** `A:\Dark Matter Genesis` · Unity 6000.4.11f1 HDRP · branch `cursor/wip-clean-20260919`
**Scene:** `Assets/_Project/Scenes/Dark Matter Genesis v1.6.5.unity` · Reference enemy: `Assets/_Project/Prefabs/Combat/Enemies/FRED.prefab`
**Locks respected:** no NavMesh · `Player_v7` untouched · UI Toolkit only · DM/DMI naming · AAA semi-HD · pooled and capped FX · no per-frame allocations · PC/Mac first.
All script paths below are relative to `Assets/_Project/Scripts/`. Line numbers come from the working tree on 2026-10-06.


## 0. Decisions (Anthony, 2026-10-07)

| # | Question | Decision | Where it lands |
|---|---|---|---|
| D1 | Attackers at once | **Exactly one melee attacker per target, at all group sizes.** The token **rotates** to whichever enemy the player is facing, targeting or hitting, or the best-positioned one. A minimum hold time stops thrashing. The "2 engagers for 5+" option is removed. | §5.1, §5.4 (hand-off rules), §7, A2, §10 |
| D2 | Blood/burn rules | **Decided per enemy body type:**<br>• **Humanoid:** blood + burn mark.<br>• **Android:** glowing burn mark (reuses the existing Laser burn look) + green coolant splatter + sparks.<br>• **Robot/Machine:** sparks + glowing burn mark only.<br>**FRED = Android.** | §6.2-6.3, §6.5, §7, B2/B3 |
| D3 | Project settings | **Approved:**<br>• HDRP Decal Layers on in **all 5** quality assets (+ frame settings).<br>• New physics layer **26 `DMHitbox`** (TagManager checked read-only: user layers 26-30 are empty, so 26 is free), set to collide with nothing. | §6.1, §6.2, §8 |
| D4 | Zone multipliers | **Deferred.** Head/chest/limb multipliers stay **1.0**. B4 stays a later phase. Hitboxes still record their zone now, so B4 is data-only later. | §6.1, §7, §9 |
| D5 | Holders' idle life | **Occasional sidestep or taunt** with strict cadence, distance and cooldown limits. It can never chain into continuous circling. | §5.5, §7, §10 |
| D6 | §16 vs body type | **Keep Combat Plan §16 element rules.** Laser never bleeds; Ion never burns. These **override** body type. Body type still picks the material response (blood / coolant / sparks / burn style) when the element allows it. | §6.3, §6.5, §7, B2 |
| D7 | Robot / Humanoid test targets | **Yes — plan text only for now.** Clone `FRED.prefab` (Android) twice under `Assets/_Project/Prefabs/Combat/Enemies/` following DM naming:<br>• **`Robot.prefab`**: rename to Robot, give the body mesh a different mech color, set `bodyType = Robot`.<br>• **`Humanoid.prefab`**: rename to Humanoid, set `bodyType = Humanoid`.<br>Place both in a **test scene** (not the play scene). Do **not** create these prefabs until build time. | §6.5, §8, §10 |
| D8 | Humanoid bullet hit mark | **Bright red blood spot with a circular char** around it (no glowing android-style burn). Humanoid hits still get the separate red blood splatter. | §6.2, §6.5, B3 |

Open questions: **none.** All of D1–D8 are decided (2026-10-07).

---

## 1. Summary

| Part | Problem | Root cause (short) | Fix (short) |
|---|---|---|---|
| **A — spacing** | Enemies keep circling and re-facing while the player turns and moves. | Every enemy recomputes a *rotated* ring point every frame (±42° slot angle). There's no hysteresis on Chase/Attack/stop bands, facing is forced twice per frame (even mid-swing), and there's no group coordination, so everyone repositions at once. | Add a small **DM Engagement Director** with attack tokens: exactly **1 engager** faces and attacks the player, and the token rotates by player focus (D1), and the others **hold** on an outer ring at world-anchored points. Add range/facing hysteresis, an attack facing cone, and per-swing tracking windows. Remove the per-frame ring rotation. |
| **B — hits & marks** | Shots should hit the visible enemy body only and always register. We also want a new hit mark: a direction-aware blood splatter plus up to 5 burn marks per enemy that stick to the animated body. | Living enemies only have **one coarse root capsule**, sized from renderer bounds (weapons included). Bone colliders are turned off. So hits land in empty air around the body and miss limbs that stick out. The blood "normal" is actually the travel direction. Enemy decals and burns are skipped on purpose. | Add per-bone **trigger hitboxes on a new dedicated physics layer** that collides with nothing. Ranged queries refine onto those hitboxes and no longer accept the root capsule. Marks are **HDRP Decal Projectors parented to the hit bone**, kept in a per-enemy ring buffer of 5 (oldest rotates out). Clear them on death and respawn. Hook for future regen. |

---

## 2. Diagnosis — Part A (enemy circling / over-facing)

Computed for FRED vs. the player: effective melee range = `attackRange 1.8 × enemyMeleeAttackRangeMultiplier 1.22` (`Resources/Combat/DM_CombatCoreProfile.asset`) = **2.196 m**. Standoff = `min(max(1.35, 1.206)+0.35, max(1.35, 2.196×0.86))` = **1.70 m**. Stop band ≤ 1.70×1.05 = **1.785 m**.

1. **Perpetual orbit from a rotating ring point (main cause).**
   - `AI/EnemyAiController.CombatPositioning.cs:55-79` (`ResolveCombatRingDirection`) takes the *current* bearing to the player every frame and rotates it by `combatRingSlotAngle` (`:67`).
   - That angle is a per-enemy hash in ±`combatRingSlotSpread` = ±42° (`AI/EnemyAiController.cs:134`, `:375-381`).
   - Because the target point is re-derived from the current bearing each frame, the enemy never arrives. Up to sin 42° ≈ 67% of every step is tangential, so it spirals and orbits.
   - When the player turns or moves, the ring point jumps with them, and so does every enemy.
2. **No hysteresis anywhere, so state and move thrash.**
   - Visible-threat path: Attack↔Chase flips at exactly 2.196 m (`AI/EnemyAiController.cs:471-483`).
   - Aggro path (common once the player has shot them): leaves Attack as soon as distance > `engageDistance×1.05` ≈ **1.785 m** (`AI/EnemyAiController.States.cs:496-510`). A single player step re-enters Chase, which runs at chase speed toward the *rotated* ring point (`States.cs:204`, `:221`).
   - `MoveTowardsCombatRing` only stops inside ring×1.05, a 5% band (`CombatPositioning.cs:131-147`, `:138`). Any player step restarts movement.
3. **Over-facing, with two rotations per frame.**
   - `MoveTowards` rotates toward the *move target* (the rotated ring point) (`AI/EnemyAiController.Movement.cs:264-318`, `:316-317`). The state code then calls `FaceTowards(target)` again in the same frame (`States.cs:171`, `:264`, `:415-430`, `:438`, `:459`, `:466`).
   - `FaceTowards` runs **during swings** too (`States.cs:429`), at `turnSpeed 8 → 144°/s` (`AI/DMILocomotionFacing.cs:13-16`). Attacks home in on the player for the whole animation.
   - The motor bridge flips between forward and strafe clips at a 40° move/facing threshold (`AI/Invector/EnemyInvectorMotorBridge.cs:33`, `:181-237`). With two competing rotations, the visible result is shuffling.
4. **No facing gate on attacks.** `AI/EnemyCombat.cs:138-195` (`TryAttack`) checks range and cooldown but not facing, so enemies swing while still turning.
5. **No group coordination.**
   - Every enemy runs the same Chase→Attack logic and attacks whenever it's in range.
   - The separation push (`CombatPositioning.cs:69-76`, `:81-125`) is added to the ring *direction*. One enemy moving changes its neighbours' targets, which causes chain reactions.
   - `DMG_Combat_Plan_v2.md` §0 lists "Combat Director / attack tokens — Not started".
6. **Extra lateral motion.**
   - After a swing, `WantsMeleeReposition` (`EnemyCombat.cs:219-229`) causes a double `MoveTowards` (`States.cs:424-426`).
   - Dodge rolls pick sideways rolls (`AI/Invector/EnemyInvectorCombatBridge.cs:216-251`, `:234-244`).

**Combined effect:** when the player turns and walks, each enemy's ring point rotates with the player's bearing (1). The tiny bands then trigger Chase (2), the enemy turns toward the offset point and then back to the player (3), and neighbours push each other (5). The result is the constant circling Anthony describes.

---

## 3. Diagnosis — Part B (hit registration & marks)

1. **Only a coarse root capsule exists while alive.**
   - `AI/Invector/EnemyInvectorHitSetup.cs:15-33` (`Apply`) disables **all** child colliders except outgoing weapon colliders (`:94-109`).
   - The root capsule is fitted from **renderer bounds, including weapons and holsters**: radius = max(extents.x, z)×0.35, clamped 0.25-0.75; height clamped 0.5-3.5 (`:140-200`).
   - Results:
     - **False positives:** hits in air beside the torso or between the legs.
     - **False negatives:** arms, weapon arm or head outside the capsule during swings or leans are missed.
     - **Hit points sit on the capsule surface**, not the skin, so FX float off the body.
   - Called from `AI/Invector/EnemyInvectorBootstrap.cs:65-73`, `:123`, `:151-157`.
2. **Inconsistent results during stagger and knockdown.**
   - `EnemyInvectorRagdollBridge.cs` `HitStaggerRoutine :764-809` and `HitKnockdownRoutine :814-891` enable bone colliders (`EnemyInvectorPhysicsCache.cs:90-111`, BodyPart layer 15) but keep the **upright root capsule enabled**.
   - So a knocked-down enemy is hit through an invisible standing capsule as well as through its bones. The capsule is only disabled on the corpse ragdoll (`RagdollBridge.cs:636-638`).
3. **Projectile query is coarse.**
   - `Combat/CombatProjectile.cs:242-283` (`SweepForHit`) uses a single `SphereCast` with radius 0.08 and `QueryTriggerInteraction.Ignore`. That adds 8 cm of "air hit" forgiveness on top of the capsule.
   - `hitLayers` is ~0 (everything) on all projectile prefabs.
   - The overlap fallback (`:285-330`) uses closest-point on whatever it finds.
   - Hitscan (`Combat/CombatProjectileSpawner.cs:92-135`) and the reticle (`Combat/RangedFireSolver.cs:188-236`, `:412-439`) raycast against the same capsule, ignoring triggers.
4. **Blood direction is wrong.** `Combat/CombatHitResolver.cs:141-142` passes `normal = travelDirection`, so the spray goes "with the bullet" instead of out of the wound toward the shooter. Blood also spawns on corpses (no IsDead check before the VFX).
5. **Enemy marks are deliberately suppressed.** `Combat/DMCombatFx.cs:184` (`ShouldSkipImpactDecal`, used at `:144`, `:230`) skips decals, burns and molten impacts on EnemyHealth / Enemy-tag targets. The existing `DMILaserBurnMark*` is a world quad (scorch + char + glow, 4.5 s) that can't wrap a skinned body. There is no `DecalProjector` usage anywhere in project scripts.
6. **Stuck FX are parented to the enemy root.**
   - `CombatProjectile.ResolveHit :348-390` → `StickProjectileVisualsAtImpact` (4 s), and `CombatVfxUtility.ResolveImpactAttachTransform` returns `receiver.transform`.
   - These FX don't follow limbs, and pooled instances die with the enemy.
7. **Death cleanup constraint.** `Combat/EnemyDisintegrationEffect.cs:199`, `:205` collects **all child Renderers** and BakeMeshes skinned ones. Any child mark must be cleared *at `Died`*, before disintegration starts, or it gets dissolved and baked. `AI/EnemyDeathSequence.cs` runs 5 s pre-delay → disintegrate → loot → destroy shell. `AI/EnemyDeathRuntimeCleanup.cs` sweeps scene-root leftovers by name.
8. **No body-part data.** `Combat/CombatBodyPart.cs` has only `None` ("Reserved for body-damage phase"). Ranged `DamageInfo` carries a hitPoint but no part.
9. **No regen yet.** `AI/EnemyHealth.cs` has no `Heal()`. Events: `HealthChanged`, `Damaged`, `DamagedBy`, `DamagedWithSource`, `Died`, `Respawned` (`TakeDamage :68-87`, `HandleDeath :89-128`, `Respawn :191-222`).
10. **HDRP decal settings.**
    - The active asset `Assets/Settings/HDRP/Genesis_HDRP_Quality.asset` has `supportDecals 1`, **`supportDecalLayers 0`**, maxDecalsOnScreen 512, 4096² atlas.
    - QualitySettings references 5 HDRP assets; only that one was inspected.
    - Without Decal Layers, a projector paints *every* receiving surface inside its box (ground near the feet, an overlapping player). Also, **Angle Fade is only available when Decal Layers are enabled** (Unity docs, below).

---

## 4. Research findings (sources actually found)

**Melee crowd management, "only one faces you"**
- **Kingdoms of Amalur — "Beyond the Kung-Fu Circle"**, Game AI Pro ch. 28 (Michael Dawe): https://www.gameaipro.com/GameAIPro/GameAIPro_Chapter28_Beyond_the_Kung-Fu_Circle_A_Flexible_System_for_Managing_NPC_Attacks.pdf
  - The classic Kung-Fu Circle lets one attacker in at a time.
  - Amalur's "Belgian AI" adds a **grid capacity** (weighted positions around the player) and an **attack capacity** (weighted attack budget). Examples: soldier 4, troll 8, grid capacity 12, attack capacity 10.
  - A central **stage manager** owns positioning. There are two rings: an **approach circle** and an **attack circle**. Creatures without permission stand outside, in front of unoccupied slots, which produces natural flanking without orbiting.
  - Attackers **lock** their slot during an attack, release it right after, and slots can be stolen or shifted.
  - Per-creature and global attack cooldowns; difficulty scales the capacities.
- **DOOM (2016) token system:** https://www.gamedeveloper.com/design/cyber-demons-the-ai-of-doom-2016- and GDC 2018 "Embracing Push Forward Combat in DOOM": https://www.gdcvault.com/play/1024940/Embracing-Push-Forward-Combat-in
  - Per-attack-type token pools with request/release and a cooldown on each token.
  - Token counts per difficulty.
  - **Token stealing**, so the demon in front of the player is the one that attacks.
- **God of War (2018) — "Evolving Combat in God of War"** (Mihir Sheth, GDC 2019): https://www.gdcvault.com/play/1026423/Evolving-Combat-in-God-of · video https://www.youtube.com/watch?v=hE5tWF-Ou2k
  - An aggression-token budget (e.g., 10 tokens, an enemy takes 4), static per difficulty.
  - Off-screen enemies keep their quadrant and are telegraphed with on-screen indicators.
  - Interrupted aggressors temporarily keep their token.
- **The Last of Us**, Game AI Pro 2 ch. 34 (Travis McIntosh): https://www.gameaipro.com/GameAIPro2/GameAIPro2_Chapter34_Human_Enemy_AI_in_The_Last_of_Us.pdf
  - A Combat Coordinator assigns roles (Flanker, Approacher, Investigator, StayUpAndAimer, OpportunisticShooter) via request/acknowledge.
  - Only one NPC needs to be shooting at a time; the others hold or flank.
  - There is a single ideal flanker.
- **Ghost of Tsushima — "Mastering the katana"** (Chris Zimmerman, PlayStation Blog): https://blog.playstation.com/2020/06/23/ghost-of-tsushima-mastering-the-katana/
  - Enemy attacks can't be faster than human reaction (~0.3 s) for the *first* hit of a string; follow-ups can be fast because they can be anticipated.
  - Attacks **overlap**: "while one enemy attacks, another enemy can be winding up". Often 2-3 enemies are mid-sequence.
  - Design takeaway: one front attacker is the baseline, and a studio knob can allow a second staggered attacker for intensity.
- **Batman: Arkham** — only a player-guide-level source was found (https://www.ludo.guide/guide/batman-arkham-asylum/3e-combat-tips-enemies, "only one enemy attacks at a time"). It's weak, so I don't rely on it.
- **Battle-circle tutorial** (Tuts+): https://code.tutsplus.com/battle-circle-ai-let-your-player-feel-like-theyre-fighting-lots-of-enemies--gamedev-13535t. The page is behind Cloudflare, and I couldn't re-verify its content today.
- **Not found:** reliable primary sources for Shadow of Mordor, Assassin's Creed, Sekiro or Hellblade crowd/turn-taking. They're not cited.

**Facing and attack tracking (why enemies shouldn't home in mid-swing)**
- FromSoftware-style per-animation turn speed with a **tracking window just before the hitbox goes active**, so moving at the right time evades: https://eldenringpvp.net/balance-suggestion-articles/thrusting-attacks-have-lost-their-weaknesses (community analysis).
- Motion warping and tracking windows in melee: https://signalsandlight.substack.com/p/how-does-enemy-ranged-and-melee-combat

**Hit detection and decals on characters (Unity/HDRP)**
- HDRP decals:
  - Decal Projector reference: https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@17.1/manual/decal-projector-reference.html
  - Understand decals: https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@17.6/manual/understand-decals.html
  - Key facts:
    - Projectors are instanced and cheap when they share a material.
    - **Decal Layers** control which renderers receive a projector.
    - **Angle Fade requires Decal Layers.**
    - Emissive decals render regardless of a material's Receive Decals setting unless Decal Layers are used.
    - The decal shader can be stripped if Instancing Variants = Strip All.
    - Use decals / rendering layers: https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@16.0/manual/use-decals.html, https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@16.0/manual/Rendering-Layers.html
- `SkinnedMeshRenderer.BakeMesh` is a CPU snapshot, and feeding a MeshCollider forces a re-cook, so it's too costly per hit: https://docs.unity3d.com/ScriptReference/SkinnedMeshRenderer.BakeMesh.html, https://docs.unity3d.com/6000.6/Documentation/Manual/mesh-colliders-introduction.html. The industry norm is bone-attached primitive hitboxes.

---

## 5. Proposed design — Part A: DM Engagement Director (tokens, holds, hysteresis)

### 5.1 Behaviour model (what the player sees)
- **One Engager per target, by default.** It holds the melee token, walks or runs **straight** to its attack range on its *current* bearing (no slot rotation), squares up, and attacks with its 1-3 combo or charged attack. Because the player naturally turns toward whoever is hitting them, the pair "gravitates to facing each other" without the enemy chasing the player's front.
- **Everyone else is a Holder (waiter).**
  - Each Holder gets a **world-anchored hold point** on an outer ring (3.2-5.5 m) and walks there at walk pace.
  - At the hold point it **stands its ground**: guard idle, small weight shifts or taunts, head and torso look-at the player. It does a body turn only when the player is >35° off its facing, and settles to <10°.
  - The hold point is **not** re-derived every frame. It's re-anchored only when:
    - the player has moved >2 m from the anchor origin,
    - the Holder is outside the [3.0, 6.5] m band for >0.6 s,
    - it's overlapping another Holder (<1.6 m), or
    - it's standing in the line between the player and the Engager.
  - Re-anchors are rate-limited to one per 2.5-4 s per Holder. That's the fix for "they keep circling when I turn and move".
- **Token hand-off.** The token follows the player's focus. Full rules are in §5.4.
- **Player turns away from the Engager.** The Engager does **not** run around to the front. It attacks from where it stands, but an off-screen attack gets +0.25 s of wind-up plus the existing audio tell (GoW-style fairness). Holders behind the player stay put and never attack without a token.
- **Single attacker, always (D1).** There is no second simultaneous melee attacker at any group size. This differs from `DMG_Combat_Plan_v2.md` §10 (2-4 enemies = 1-2 attackers, 5-8 = 2-3), and §10 needs updating (see §8). Intensity in large groups comes from fast token rotation and Holder pressure (sidesteps, taunts, feints), not from overlapping swings.
- **Charged attack** uses its own global pool of 1 (only one heavy at a time on a target). **Punish rule** (3 hits in 4 s): the combo driver asks the director for priority, so the punished enemy becomes the next Engager when the token frees. No mid-swing steal.
- **Ranged enemies (designed now, used later):** a separate `RangedShot` pool (DOOM-style), default 2 concurrent shooters per target.

### 5.2 Hysteresis and facing rules (per enemy)
- **Range bands (Engager):**
  - Enter Attack at d ≤ `effectiveRange` (FRED 2.196 m).
  - Leave Attack only when d > `effectiveRange × 1.35` (2.96 m) **for ≥ 0.4 s**.
  - Creep in only when d > `effectiveRange × 0.95` for ≥ 0.25 s.
  - Back off only when d < `minCombatSeparation` (1.35 m).
  - One shared transition function replaces both the visible path (`EnemyAiController.cs:471-483`) and the aggro path (`States.cs:496-510`).
- **Facing (one resolve per frame).** Movement no longer rotates in combat. A single "desired yaw" is computed at the end of the AI update, and `DMILocomotionFacing` applies it with role rates:
  - Engager out of swing: 200°/s.
  - Holder: 90°/s with a 35°/10° dead zone.
- **Attack facing cone:** `TryAttack` requires the target within ±25° of facing. Otherwise the enemy turns first (max 0.5 s), then swings.
- **Per-swing tracking window (FromSoft-style):**
  - Light swings track at ≤120°/s until normalized time 0.30, then 0°/s through active and recovery.
  - Charged attack tracks at 60°/s until 0.28, matching the existing wind-up slow window (`windupSpeed 0.55 until 0.28` in `DMEnemyMeleeComboDriver`).
  - This replaces `FaceTowards` during `IsAttacking` (`States.cs:429`).
- **Lateral motion trimmed.**
  - `WantsMeleeReposition` becomes a single straight step-in (no double move).
  - Dodge rolls for non-Engagers prefer back rolls; side rolls only when the back is blocked. The current roll logic stays for the Engager.
  - Strafing is allowed only during the Engager's attack cooldown, in arcs <30°, at walk pace.
- **Separation** pushes the *hold point*, not the per-frame move direction, which stops chain reactions.
- **Perf:**
  - Director ticks at 5 Hz.
  - Per-enemy role checks run at 10 Hz on the existing phase offset.
  - Fixed arrays, no LINQ, no per-tick allocations. Separation keeps its existing NonAlloc call every 3 frames.

### 5.3 Architecture
- `DMEnemyEngagementDirector` (scene singleton, auto-created like other DM runtime services):
  - `Dictionary<Transform, DMEngagementRing>` keyed by target, so the player and later pioneers each get one. Plan §10 says companions share the player side's pool, so there's an optional "player-side cap" flag.
  - Token pools: `Melee`, `Charged`, `Ranged`.
  - API: `Register/Unregister(agent)`, `RequestToken(agent, type, priority)`, `ReleaseToken(agent, type, cooldown)`, `HasToken`, `GetHoldPoint(agent)`, `NotifyAttackBegin/End`, `RequestPriority(agent)`.
- `EnemyAiController` gets a role (`None / Engager / Holder`) and a new `AiState.Hold`. `Attack`/`Chase` keep their names. This stays small and replaceable by the upcoming §31 #3 utility brain, which can call the same director API.
- Profile `DM_EnemyEngagementProfile` (ScriptableObject in `Resources/Combat/`, live-tuned like `DM_CombatCoreProfile`). It's exposed in Combat Studio's existing "Director & tokens" placeholder (UI Toolkit). Per-enemy overrides are optional.
- Debug: scene gizmos (rings, hold points, token holder, facing cones) plus a UI Toolkit dev overlay line in the existing dev HUD. Editor/dev builds only.

---

### 5.4 Token hand-off rules (D1)
The director keeps one melee token per target and re-scores candidates at 5 Hz.

**Who should hold it.** Each candidate gets a focus score; the highest wins:

| Signal | Weight | Definition |
|---|---|---|
| `playerHitThis` | +5.0 | The player damaged this enemy (melee or ranged) in the last 1.5 s. It decays linearly. |
| `playerTargeting` | +4.0 | This enemy is the player's lock-on/aim target (`CombatFocusController`, or the reticle ray's DMHitbox hit owner). |
| `playerFacing` | +3.0 × facing01 | facing01 = 1 inside ±20° of the player's facing/camera forward, falling to 0 at ±60°. |
| `positioning` | +2.0 × (1 − d/6 m) | Closer is better; only counts when there's a clear line (no other enemy in between, cheap 2D segment test). |
| `waitTime` | +0.25 per second since its last token, max +2.0 | Fairness, so nobody waits forever. |
| `punishPriority` | +3.0 | Set by `DMEnemyMeleeComboDriver` punish rule. |
| penalties | −∞ | Staggered, knocked down, mid dodge-roll, out of 8 m, or no line of sight. Not eligible. |

**When it may move (anti-thrash).**
- **Lock:** never during a swing, combo or charged attack (`IsSequenceActive`), and never within 0.25 s after one ends (recovery).
- **Minimum hold:** the current holder keeps the token for at least **`tokenMinHoldSeconds` = 2.5 s** unless it becomes ineligible.
- **Switch margin:** a challenger needs score ≥ holder score + **`switchScoreMargin` = 2.0**, sustained for **`switchConfirmSeconds` = 0.35 s**.
- **Immediate override:** if the player **hits** a Holder, it can take the token once the holder's lock clears, ignoring the margin but still respecting the 2.5 s minimum hold. This is the "I'm fighting this one" case.
- **Global switch rate limit:** ≤ 1 hand-off per **`minSecondsBetweenHandoffs` = 1.5 s** per target.
- **Max hold:** **6 s** or **2 attack sequences**. The holder then releases the token, and it goes to the best other candidate with a **0.6-1.2 s** grace before the new Engager may swing. If nobody else is eligible, the holder keeps it.
- The old holder becomes a Holder: it gets a hold point on the outer ring near its current bearing, backs off at walk pace, and has a 1.5 s pause before it can be re-awarded the token.
- **Handover tell:** the new Engager plays a short "step in" or "aggro bark" (0.3-0.5 s) before its first wind-up, so the switch reads as intentional.
- The off-screen attack rule stays: +0.25 s wind-up and an audio tell.

**Charged attack** uses a separate pool of 1 per target, and only the melee token holder may use it.

### 5.5 Holder idle life: sidesteps and taunts (D5)
Holders stay planted at their world-anchored hold point. On a randomized timer they perform **one discrete action**, then return to stillness:
- **Cadence:** each Holder rolls an action every **`holderActionInterval` = 3.5-6.0 s** (random per roll, per-enemy phase offset). Group cap: at most **1 Holder acting at once per target**, plus a **1.0 s** group gap between Holder actions.
- **Actions and weights:**
  - **Sidestep:** weight 0.45. One lateral step of **0.6-1.2 m**, walk pace, **≤ 15° of arc** around the player. It then *returns toward its anchor* or **re-anchors there** (the anchor moves at most 1.2 m per step). Direction alternates or picks the side away from the nearest Holder.
  - **Taunt:** weight 0.30. In-place weapon raise / chest beat / bark, 1-2 s, no translation.
  - **Feint step-in:** weight 0.15. A 0.5 m step toward the player and back, no attack. Only allowed if > 3.5 m from the player.
  - **Nothing:** weight 0.10.
- **Anti-circling guarantees:**
  1. Net angular drift around the player is capped at **≤ 30° per 10 s** per Holder. If it's exceeded, sidesteps are forced to the opposite side (or disabled) until the drift decays.
  2. Never two sidesteps in a row in the same direction.
  3. No sidestep while the player is moving > 1.5 m/s; the Holder just turns, using the dead zone.
  4. Sidesteps don't happen in the 2 s after a re-anchor.
  5. Sidestep cooldown is **4.0 s** per Holder, taunt cooldown **6.0 s**.
- **Facing:** Holders face the player only via the 35°/10° dead-zone turn. Sidesteps use strafe locomotion and keep facing (MotorBridge strafe clips).

---

## 6. Proposed design — Part B: mesh-accurate hits + bullet hit marks

### 6.1 Hit registration: per-bone trigger hitboxes on a dedicated layer
- **New physics layer** `DMHitbox` = **layer 26** (approved D3; verified empty in `ProjectSettings/TagManager.asset`, with 26-30 all unused). In the collision matrix it **collides with nothing**: no shoving, no melee trigger interference, and the `Player_v7` capsule and layers are unchanged.
- `DMEnemyHitboxRig`:
  - At bootstrap, after `EnemyInvectorRagdollRigRepair` and `EnemyInvectorHitSetup`, it creates one child GameObject per ragdoll bone collider (~12-15 shapes: head, neck, chest, spine, hips, upper/lower arms, hands, upper/lower legs).
  - It copies the shape (sphere/capsule/box, handling the cm scale) inflated ×1.05, sets `isTrigger`, layer `DMHitbox`, and a `DMEnemyHitbox` tag component holding the bone, zone (Head / Torso / Arm / Leg) and an optional damage multiplier.
  - The children live under the bones, inside the hips, so the rig-repair orphan strip doesn't touch them.
  - An editor "Bake hitboxes into prefab" button is optional (Phase B1b).
- **Query flow** (one helper `DMEnemyHitQuery`, static NonAlloc buffers):
  1. Run the normal world query as today (`Ignore` triggers).
  2. Run a second query on `1<<DMHitbox` with `QueryTriggerInteraction.Collide`.
  3. The nearest valid result wins. **An enemy root capsule is skipped as a ranged hit when that enemy has an active hitbox rig**, so the capsule stays only for movement, blocking and melee.
  - Applied in:
    - `CombatProjectile.SweepForHit` (SphereCast → SphereCastNonAlloc).
    - `TryResolveOverlapHit`.
    - `RangedFireSolver.TryRaycastAim`, which covers hitscan beams, the reticle aim point and the muzzle aim point, so the crosshair converges on the real body.
  - The projectile sweep radius against hitboxes drops to ~0.02 m (world sweep keeps 0.08 m), which removes the "air hit" forgiveness.
- **States:**
  - Rig is enabled while alive, including stagger and knockdown (the hitboxes ride the bones, so a downed body is hit where it lies).
  - During stagger and knockdown the root capsule is also excluded from ranged queries.
  - The rig is disabled on `Died`. Corpse shots then fall through to the bone colliders, with no damage and no blood.
  - Optionally disabled when the performance controller distance-culls the enemy.
- `EnemyInvectorHitSetup.DisableChildSolidColliders` must **skip** `DMHitbox` objects, because it runs several times from the bootstrap.
- **Body parts (optional, off by default):** extend `CombatBodyPart` (Head, Torso, Arm, Leg) and pass it in `DamageInfo` for ranged hits. Multipliers are **deferred (D4)** and stay 1.0; the zone is recorded now for B4 later.

### 6.2 Burn marks: HDRP Decal Projector on the hit bone, ring buffer of 5
- `DMEnemyHitMarks` (per enemy, added at bootstrap) owns **5 lazily created slot GameObjects** with a `DecalProjector`. They're children of the enemy, so they die with the enemy and can never become scene-root clones.
- **On a valid ranged hit:**
  - Take the oldest free slot, or recycle the oldest active one (**oldest rotates out**).
  - Reparent it to the hit bone, place it at the hit point, and orient it along −surface normal (projection into the skin).
  - Size 6-10 cm with a random roll and one of 4 atlas variants (UV offset, so instancing is kept). Projection depth 8 cm, centred, so it only reaches ~4 cm into the skin. Angle fade 50-80°.
  - Zero allocations per hit.
- **Look (AAA semi-HD):**
  - **Humanoid (D8):** HDRP Decal material `DM_Decal_BulletBurn` — **bright red blood spot with a circular char ring** (no glow). Albedo = red spot + charred ring + soot falloff; normal = pitted crater; mask = low smoothness, slight AO. 1024² atlas of 4 variants. No ember particle on humanoids.
  - **Android / Robot:** HDRP Decal material `DM_Decal_GlowBurn` reuses the existing Laser burn textures (see §6.5). The **hot ember glow** is a tiny pooled emissive particle attached to the same bone, fading over ~1.5 s. It's not decal emission, which keeps a single instanced decal material and avoids emissive bleed.
- **Lifetime:** marks are **persistent while the enemy lives** (no timer). Removal:
  - Rotated out by the 6th hit.
  - Cleared on `Died`. The handler runs on the event, before the 5 s disintegration delay, so `EnemyDisintegrationEffect` never sees them.
  - Cleared on `Respawned`, `OnDisable` and `OnDestroy`.
- **Future regen hook:** each slot stores `healthAtStamp`. When `EnemyHealth` gains `Heal()` / `Healed` (later), any mark whose stamp ≤ current health fades out over 1.5 s. Marks then disappear newest-last as health returns, and all of them are gone at full health. Until regen exists, nothing changes.
- **Decal Layers (recommended):**
  - Enable `supportDecalLayers` on all 5 HDRP quality assets plus Default Frame Settings.
  - Name rendering layer "DM Enemy Marks". Enemy skinned renderers get that bit (set at runtime by `DMEnemyHitMarks`, or baked into prefabs), and projectors target only that bit.
  - This stops marks painting the floor or the player and enables angle fade.
  - Verify enemy materials have Receive Decals on.
  - **Approved (D3)**: all 5 HDRP quality assets.

### 6.3 Direction-aware blood splatter
- (Humanoid only; Android uses the same rig as green coolant, Robot uses none. See §6.5.) New pooled `DM_FX_BloodHit_Small`: semi-HD flipbook mist puff, 6-12 droplets with gravity, and a short wet sheen. ~0.25-0.4 s. Pool of 24.
- Spray direction:
  - **Entry:** `Slerp(surfaceNormal, −travelDir, 0.35)` plus a ±15° cone.
  - **Exit/back-spray** on high-damage hits: a small cone along +travelDir, 30% strength.
  - Scaled by damage, clamped small.
- Replaces the `normal = travelDirection` call at `CombatHitResolver.cs:141-142`. No blood if the target is dead.
- **Caps:** at most 1 blood burst per enemy per 0.06 s (automatic fire), a global pool cap, and LOD skip beyond 40 m.
- Per-ammo rules live on `DMAmmoFxProfile` (new fields: `enemyBlood`, `enemyBurnMark`, `burnTint`). Defaults follow `DMG_Combat_Plan_v2.md` §16, and **§16 overrides body type (D6)**:
  - Laser: cauterised glowing burn, no blood (never bleeds).
  - Ion: blood / coolant / sparks as body type allows, no burn (never burns).
  - Standard/plasma: body-type matrix in §6.5 (D2) as written.
- Existing world impacts on enemies stay suppressed (`DMCombatFx.cs:184`). The new system is the enemy path.
- Stuck tracer and impact visuals get parented to the **hit bone** (via `DMEnemyHitbox`) instead of the root, and unparented back to the pool before death.

### 6.5 Enemy body type → hit FX (D2)
**How the type is determined.** There's no existing enemy body/material type. I checked read-only: no enemy definition SO, and enums exist only for `EnemyNoiseKind`, `CombatDamageType`, `StatusEffectType` and `AmmoType`. The tag is "Untagged" on FRED's children, and only comments mention "android" (`AI/Invector/EnemyInvectorRagdollRigRepair.cs:15`, `AI/Encounters/SurfaceEncounterZone.cs:11`). So:
- New enum `DMEnemyBodyType { Humanoid, Android, Robot }` (Robot = robots/machines).
- Serialized field `bodyType` on the new per-enemy `DMEnemyHitMarks` component (added to enemy prefabs; default **Humanoid**).
- Optional override on `DM_EnemyHitMarkProfile` per prefab, plus a Studio dropdown.
- **FRED.prefab = Android.** The Corrupt Patrol Android = Android.
- Missing component (spawned at runtime) = Humanoid fallback plus a dev warning.
- When a future enemy-definition asset arrives, the field moves there and the component reads it.
- **Test targets (D7 — plan text only; do not create prefabs yet):** clone `FRED.prefab` twice under `Assets/_Project/Prefabs/Combat/Enemies/` (DM naming):
  - `Robot.prefab` — rename to Robot, recolor the body mesh to a distinct mech color, set `bodyType = Robot`.
  - `Humanoid.prefab` — rename to Humanoid, set `bodyType = Humanoid`.
  - Place both in a dedicated **test scene** (not `Dark Matter Genesis v1.6.5`). Use them to verify the Robot/Humanoid FX paths until real enemies of those types exist.

**FX matrix** (all pooled and capped; at most 5 burn marks per enemy for every type):

| Body type | Burn mark | Splatter | Sparks |
|---|---|---|---|
| Humanoid | **Bright red blood-spot decal with a circular char ring** (no glow; D8) | Red blood (`DM_FX_BloodHit_Small`) | No |
| Android | **Glowing burn**: reuse the existing Laser burn look (see below) as a decal + glow fade | **Green coolant** (`DM_FX_CoolantHit_Small`: same rig as blood, green emissive-tinted droplets, slightly more viscous) | Small spark burst (`DM_FX_SparkHit_Small`, 6-10 streaks, 0.2 s) |
| Robot / machine | **Glowing burn** (same as Android) | None | Larger spark burst + tiny smoke wisp |

**Existing glowing burn asset reused** (found read-only):
- `Assets/_Project/Prefabs/Combat/VFX/Laser_Burn_Mark.prefab` (+ Resources fallback `Assets/_Project/Resources/Combat/VFX/Laser_Burn_Mark.prefab`). Driven by `Scripts/Combat/DMILaserBurnMark.cs`, `DMILaserBurnMarkSpawner.cs`, `DMILaserBurnMarkHost.cs`; built by `Editor/DMILaserBurnMarkPrefabBuilder.cs`.
- Materials: `Assets/_Project/Materials/Combat/LaserBurn_Scorch.mat`, `LaserBurn_Char.mat`, `LaserBurn_Glow.mat`.
- Textures: `Assets/_Project/Art/Combat/LaserBurn_Scorch.png`, `LaserBurn_Char.png`, `LaserBurn_Glow.png`.
- That prefab is a **world quad** (3 stacked renderers, MPB glow, 4.5 s), which can't wrap skin. The plan reuses its **textures and colour/glow values** in a new HDRP decal material, `DM_Decal_GlowBurn`:
  - scorch + char in albedo/normal/mask;
  - glow texture in decal emissive, faded from hot to a dim residual over ~2 s via the projector `fadeFactor` on a second "glow" projector in the same slot, or a material swap hot→cold.
  - Option chosen during B3 profiling. Both keep shared materials, so instancing still works.
- The quad prefab itself stays for world surfaces, unchanged.
- Shader of the LaserBurn materials: guid `c4edd00f…`, not found as a `.shader` under Assets (likely a package shader). It needs no change.

**Fit with Combat Plan §16 per-ammo rules (D6 — decided).** Body type picks the **material response**, and ammo/element **overrides** it. Order: element rule → body type → result. §16 always wins.
- **Laser** (§16: "cauterised glowing cuts, no blood"):
  - Humanoid: glowing burn, **no blood**. Overrides D2's humanoid "blood".
  - Android/Robot: unchanged (no blood anyway; sparks/coolant stay, since §16 only bans blood).
- **Ion** (§16: "blood splatter, clean cuts, no burning"):
  - Humanoid: blood, **no burn**.
  - Android: coolant + sparks, **no burn**.
  - Robot: sparks only.
  - Overrides D2 "burn on androids/robots" for Ion only.
- **Standard/plasma/other:** D2 matrix as written (Humanoid = blood spot + circular char + blood splatter per D8).
- **Implementation:** one flag pair per ammo on `DMAmmoFxProfile` (`allowsBlood`, `allowsBurn`). Update Combat Plan §16 to add the body-type column (see §8).

### 6.4 Alternatives considered (and why not now)
| Option | Verdict |
|---|---|
| Reuse ragdoll bone colliders while alive (toggle trigger/layer) | Rejected. It fights Invector `vRagdoll.setCollider` (`disableColliders`) and DM's stagger/ragdoll physics paths, with a regression risk on the recently stabilised ragdoll. |
| BakeMesh + MeshCollider per hit | Rejected. Milliseconds per hit for a CPU bake + cook, plus allocations. |
| Analytic narrow-phase inside the root capsule | Rejected. Misses limbs outside the capsule, which is exactly the false-negative case. |
| GPU skinned raycast (asset) | Overkill for 5 marks. |
| Skinned-mesh decals (cut triangles, skin to bones) | Perfect stick, but needs a pose bake per hit, adds 5 extra skinned draws per enemy, and per-LOD duplication (the VBOT rig has LOD1/LOD3). |
| Shader damage mask (material properties, rest-pose positions in a UV channel) | Best long-term and matches §21 "visual damage via material properties", but needs a custom HDRP Shader Graph on every enemy material plus a baking pipeline. **Recommended for the later body-damage phase**, not now. |
| Bone-parented quad (like `DMILaserBurnMark`) | Rejected. Floats and clips on curved, deforming skin; not AAA. |

---

## 7. Parameters (defaults; all Studio-tunable)

**`DM_EnemyEngagementProfile`**
| Field | Default | Notes |
|---|---|---|
| meleeEngagersPerTarget | 1 (fixed) | D1 |
| tokenMinHoldSeconds | 2.5 s | anti-thrash |
| switchScoreMargin / switchConfirmSeconds | 2.0 / 0.35 s | |
| minSecondsBetweenHandoffs | 1.5 s | per target |
| postSwingLockSeconds | 0.25 s | |
| tokenHoldMaxSequences / Seconds | 2 / 6 s | |
| handoffGraceBeforeSwing | 0.6-1.2 s random | |
| reawardCooldown (old holder) | 1.5 s | |
| focus weights hit/target/facing/position/wait/punish | 5 / 4 / 3 / 2 / 0.25 per s (max 2) / 3 | §5.4 |
| playerHitMemory | 1.5 s | |
| facingConeFull / Zero | 20° / 60° | |
| candidateMaxDistance | 8 m | |
| holderActionInterval | 3.5-6.0 s | D5 |
| holderActionWeights side/taunt/feint/none | .45/.30/.15/.10 | |
| sidestepDistance / maxArc | 0.6-1.2 m / 15° | |
| sidestepCooldown / tauntCooldown | 4.0 / 6.0 s | |
| holderGroupActionGap / maxConcurrent | 1.0 s / 1 | |
| holderMaxDriftDegPer10s | 30° | anti-circling |
| noSidestepPlayerSpeed | 1.5 m/s | |
| chargedTokensPerTarget | 1 | |
| rangedShotTokensPerTarget | 2 | Future ranged enemies. |
| holdRingRadius (min/max) | 4.0 (3.2-5.5) m | |
| holdBandMin / Max (re-anchor if outside) | 3.0 / 6.5 m, 0.6 s grace | |
| holdReanchorPlayerMove | 2.0 m | |
| holdReanchorCooldown | 2.5-4 s | |
| holderMinSpacing | 1.6 m | |
| holderWalkSpeed | 1.4 m/s | |
| engageInFactor / engageOutFactor / outGrace | 1.0 / 1.35 / 0.4 s | × effective range. |
| creepStartFactor / creepGrace | 0.95 / 0.25 s | |
| attackFacingConeHalfAngle | 25° | |
| engagerTurnRate / holderTurnRate | 200 / 90 °/s | |
| holderFacingDeadZone / settle | 35° / 10° | |
| lightTrackRate / trackUntil | 120 °/s / 0.30 | |
| chargedTrackRate / trackUntil | 60 °/s / 0.28 | |
| offscreenAttackExtraWindup | 0.25 s | |
| cooldownStrafeMaxArc | 30° | |
| directorTickHz / agentRoleTickHz | 5 / 10 | |

**`DM_EnemyHitMarkProfile` / `DMAmmoFxProfile` additions**
| Field | Default |
|---|---|
| maxBurnMarksPerEnemy | 5 (rotate oldest) |
| burnSize | 0.06-0.10 m |
| burnProjectionDepth | 0.08 m (centred) |
| burnAngleFade | 50-80° |
| emberGlowSeconds | 1.5 (Android/Robot only; Humanoid has no ember, D8) |
| bloodPoolSize / perEnemyMinInterval / maxDistance | 24 / 0.06 s / 40 m |
| bloodEntryNormalBlend / exitSprayDamageThreshold | 0.35 / ≥ 25 dmg |
| hitboxInflate / hitboxSweepRadius | 1.05 / 0.02 m |
| zoneDamageMultipliers (Head/Torso/Arm/Leg) | 1/1/1/1 (deferred, D4) |
| bodyType (per enemy) | Humanoid default; FRED = Android |
| sparkPoolSize / coolantPoolSize | 24 / 24 |
| ammo allowsBlood / allowsBurn | Laser F/T, Ion T/F, others T/T (§16 override) |
| enemyDecalRenderingLayer | "DM Enemy Marks" |

---

## 8. Files to create / modify

**Create (Part A)**
- `AI/Combat/DMEnemyEngagementDirector.cs`: singleton, token pools, ring and hold-point assignment.
- `AI/Combat/DMEngagementRing.cs`: per-target data (slots, holders, anchors).
- `AI/Combat/DM_EnemyEngagementProfile.cs` + `Resources/Combat/DM_EnemyEngagementProfile.asset`.
- `AI/EnemyAiController.Engagement.cs`: role, Hold state, shared hysteresis transition, single facing resolve.
- `Editor/.../DMEngagementDebugGizmos.cs` and the Studio "Director & tokens" panel binding (UI Toolkit).

**Modify (Part A)**
- `AI/EnemyAiController.cs`: `AiState.Hold`, role fields, unify transitions at `:471-483`, register with the director.
- `AI/EnemyAiController.States.cs`:
  - `UpdateChase :147-227` and `UpdateAttack :361-468`: no FaceTowards mid-swing, tracking window, single step-in.
  - `UpdateDefensive :229-282`.
  - `UpdateAggroCombat :470-511`: hysteresis.
- `AI/EnemyAiController.CombatPositioning.cs`: remove the per-frame slot rotation (`:55-79`); separation applies to hold points; stop band 5% → profile band (`:131-147`).
- `AI/EnemyAiController.Movement.cs`: no rotation in `MoveTowards` during combat roles (`:316-317`).
- `AI/DMILocomotionFacing.cs`: direct deg/s plus a dead-zone helper.
- `AI/EnemyCombat.cs`: token + facing-cone gate in `TryAttack :138-195`; attack begin/end notifications; `WantsMeleeReposition` tweak (`:219-229`).
- `AI/Invector/DMEnemyMeleeComboDriver.cs`: expose swing normalized time and tracking params; punish → `RequestPriority`; hold the token through the sequence; charged token.
- `AI/Invector/EnemyInvectorCombatBridge.cs`: role-aware dodge direction (`:234-244`).
- `AI/Invector/EnemyInvectorMotorBridge.cs`: verify only; possibly widen the strafe latch hysteresis (`:181-237`).

**Create (Part B)**
- `Combat/HitMarks/DMEnemyHitboxRig.cs`, `DMEnemyHitbox.cs`, `DMEnemyHitQuery.cs`, `DMEnemyHitMarks.cs`, `DM_EnemyHitMarkProfile.cs` (+ asset).
- `DMEnemyBodyType.cs` (enum).
- Art: `DM_Decal_GlowBurn.mat` (HDRP Decal reusing the `LaserBurn_*.png` textures), `DM_FX_CoolantHit_Small.prefab`, `DM_FX_SparkHit_Small.prefab`, `DM_Decal_BulletBurn.mat` (HDRP Decal, humanoid: bright red blood spot + circular char, D8), 1024² albedo/normal/mask atlas (4 variants), `DM_FX_BloodHit_Small.prefab`, `DM_FX_BurnEmber_Small.prefab` (Android/Robot only).
- Editor (optional): `DMEnemyHitboxRigBaker`; Studio Ranged → "Enemy hit marks" subsection.

**Modify (Part B)**
- `AI/Invector/EnemyInvectorHitSetup.cs`: skip `DMHitbox` in `DisableChildSolidColliders` (`:94-109`).
- `AI/Invector/EnemyInvectorBootstrap.cs`: add the rig + marks after rig repair (`:65-73`, `:123`, `:151-157`).
- `Combat/CombatProjectile.cs`: `SweepForHit :242-283` NonAlloc + refine; `TryResolveOverlapHit :285-330`; `ResolveHit :348-390` passes hitbox/bone and sticks visuals to the bone.
- `Combat/RangedFireSolver.cs`: `TryRaycastAim :188-236` refine.
- `Combat/CombatProjectileSpawner.cs`: hitscan passes hitbox info (`:92-135`).
- `Combat/CombatHitResolver.cs`: route to `DMEnemyHitMarks`; fix the blood normal (`:141-142`); skip FX on the dead. Also fix the splash `new List<>` allocation per pulse (`:284`), a perf note found in passing.
- `Combat/CombatHitVfx.cs`: new direction-aware spawn variant.
- `Combat/CombatBodyPart.cs`, `Combat/DamageInfo.cs`: zones (optional).
- `Combat/DMAmmoFxProfile` (+ assets): enemyBlood / enemyBurnMark fields.
- `AI/EnemyHealth.cs`: (later) `Heal()` + `Healed` event; spec only now.
- `FRED.prefab` (+ other enemy prefabs): add `DMEnemyHitMarks` with `bodyType` (FRED = Android).
- **Test clones (D7, build time only):** `Enemies/Robot.prefab` and `Enemies/Humanoid.prefab` (FRED clones; bodyType + rename + Robot mech recolor). Place in a test scene, not the play scene.
- **Project settings (approved D3):** TagManager layer 26 `DMHitbox`; physics collision matrix row all-off; 5 HDRP assets `supportDecalLayers 1` + frame settings; HDRP global rendering-layer name.

**Docs to update (no contradictions)**
- `Documentation/Design/Combat/DMG_Combat_Plan_v2.md`:
  - §0 shipped table, line 64: "Combat Director / attack tokens" → "Partial: Engagement Director (melee tokens, hold ring)".
  - §10, line 172: replace "Others circle, reposition…" with "Others hold at world-anchored points on the approach ring; reposition only on hysteresis triggers; no orbiting".
  - §12: add the Hold state.
  - §16: add the body-type column and the element-override rule (§6.5). §10: 1 attacker at all sizes with focus-based rotation (D1).
  - §21, lines 344/348: add "bullet burn decals (5/enemy)" under Partial.
  - Roadmap #4 (lines 433-434): note the director-lite exists.
- `Architecture/CursorPlans/combat_master_plan_handoff.md` and the canonical copies.
- §0 says "Next = §31 #3 utility brain". This work is a scoped prerequisite the brain will call into, not the full Phase 4 Director.

---

## 9. Phased steps

| # | Phase | Size | Content |
|---|---|---|---|
| A1 | Stop the orbit (no tokens yet) | **S** | Remove the per-frame slot rotation; single facing resolve; no rotation in MoveTowards in combat; range hysteresis + dwell; facing cone in TryAttack; tracking window during swings. Already removes most of the visible circling for 1v1. |
| A2 | Engagement Director + Hold state | **M/L** | Director singleton, single melee token + charged pool, focus scoring and anti-thrash hand-off (§5.4), world-anchored hold points, re-anchor rules, Holder sidestep/taunt scheduler with drift cap (§5.5), combo-driver integration, role-aware dodge. |
| A3 | Studio + debug + docs | **S** | Profile asset, Studio "Director & tokens" panel (UI Toolkit), gizmos/overlay, doc updates. |
| B1 | Mesh-accurate hits | **M** | DMHitbox layer + matrix, hitbox rig from ragdoll colliders, `DMEnemyHitQuery` in projectile/hitscan/reticle, capsule exclusion, stagger/knockdown/death handling, HitSetup skip. |
| B2 | Splatter + sparks by body type | **M** | `DMEnemyBodyType` + FRED = Android. Blood, green coolant and spark pooled FX, direction math, caps, dead check, ammo allowsBlood/allowsBurn (§16 override). |
| B3 | Burn marks | **M** | Decal Layers on all 5 HDRP assets, `DM_Decal_GlowBurn` from the existing LaserBurn textures (Android/Robot) and `DM_Decal_BulletBurn` (Humanoid: bright red blood spot + circular char, D8), decal material + atlas, `DMEnemyHitMarks` ring buffer of 5, bone parenting, ember FX (Android/Robot only), clear on Died/Respawned/disable, regen hook stub, stuck visuals to bone. Create `Robot.prefab` / `Humanoid.prefab` test clones (D7) in a test scene. |
| B4 | Zone multipliers (**deferred**, D4) | **S** | Head/chest/limb multipliers via CombatBodyPart; data only, since zones are already recorded in B1. Later. |

Suggested order: **A1 → B1 → A2 → B2 → B3 → A3 → B4.** A1 and B1 give the biggest felt improvement fastest. Each phase gets its own commit and is play-verified before the next. All of it waits until the loot-chest agent's work has landed, to avoid merge conflicts.

---

## 10. Verification checklist (play mode, `Dark Matter Genesis v1.6.5`)

**Part A**
- [ ] 1v1 FRED: player strafes and turns in place for 20 s. Enemy yaw changes only as the player crosses ±35°, and it does **not** orbit (total angular travel around the player < 45° in 20 s unless the player walks around it).
- [ ] 1v1: player backs off 0.5 m repeatedly. No Attack↔Chase flicker (state log shows ≤1 transition per 0.4 s).
- [ ] Swings: sidestepping during the late wind-up makes the attack miss (tracking stops at 0.30). The first swing never starts with the player > 25° off facing.
- [ ] 3 and 5 FREDs: exactly 1 enemy in the attack band at all times; the rest hold at 3.2-5.5 m and idle. Hold points stay fixed while the player turns in place. They re-anchor only after the player moves > 2 m.
- [ ] Token follows focus: hitting, targeting or facing a Holder hands it the token within ~0.35-2.5 s, never mid-swing.
- [ ] Anti-thrash: sweeping the camera quickly between two enemies causes ≤ 1 hand-off per 1.5 s and no hold under 2.5 s (director log).
- [ ] Max hold 6 s / 2 sequences, then rotation. The new Engager plays a step-in tell first.
- [ ] Holders: at most 1 acting at a time; sidesteps 0.6-1.2 m, ≤ 15°; net drift ≤ 30° per 10 s; no sidesteps while the player runs. Over 60 s with 4 Holders, none completes a quarter orbit.
- [ ] Punish rule: the punished enemy becomes the next Engager. Charged attacks are never simultaneous.
- [ ] Player turns their back: the Engager attacks from behind with extra wind-up and a tell. Holders behind never attack.
- [ ] Defensive (block/dodge) still works. Non-Engagers roll backward.
- [ ] Profiler: director + roles < 0.1 ms with 8 enemies, 0 B GC alloc per frame.

**Part B**
- [ ] Shots that pass visibly beside the torso or between the legs **miss** (no damage, no blood).
- [ ] Shots on an extended arm or head during swings **hit**.
- [ ] Every visible body hit registers (100 shots at mixed ranges, 0 visual mismatches), including hitscan, projectile, reticle convergence, and pioneers' shots.
- [ ] Knocked-down enemy is hit where the body lies. The standing-capsule air above it does not register.
- [ ] Melee (player and enemy) unaffected. Player can't be shoved by hitboxes. `Player_v7` untouched.
- [ ] `Humanoid.prefab` test clone (D7): bright red blood-spot decal with circular char (D8) + red blood splatter. FRED (Android): glowing burn + green coolant + sparks, no red blood. `Robot.prefab` test clone: sparks + glowing burn only.
- [ ] Splatter sprays out of the wound toward the shooter; no FX on corpses. Laser = no blood on humanoids; Ion = no burn on any type (§16 override, D6).
- [ ] The glowing burn visibly matches the existing Laser burn look and cools to a dim residual.
- [ ] Burn mark appears at the impact point, stays on the body through walk/swing/stagger/knockdown (minor swim near joints acceptable), doesn't paint the floor or the player, and has angle fade.
- [ ] The 6th hit removes the oldest mark. Never more than 5 per enemy.
- [ ] Stop shooting: marks persist indefinitely while alive.
- [ ] Death: all marks vanish on `Died`, before ragdoll and disintegration. No marks in the dissolve. No leftover objects at scene root after the shell is destroyed (Hierarchy search `DM_HitMark`, `DMHitbox`). Respawn starts clean.
- [ ] 8 enemies × 5 marks: decal count ≤ 40, GPU decal pass < 0.3 ms, 0 B GC per hit. Ranged query cost < 0.05 ms per shot.
- [ ] Decal Layers enabled and consistent in all 5 HDRP quality assets (switch quality levels in play). Layer 26 `DMHitbox` collision row all off.
- [ ] Builds: decal shader not stripped (check Graphics > Instancing Variants); Mac Metal build renders decals.

---

## 11. Risks
- **Decal swim near joints.** Projectors are rigid per bone, so marks near elbows and knees can slide slightly. Mitigation: small size, 8 cm depth, angle fade, hitbox zone picks the bone.
- **Projection through thin parts** (an arm in front of the torso). Shallow centred depth limits it. Accepted for 6-10 cm marks.
- **Decal Layers (approved) are a global HDRP change.** Small GPU cost for the rendering-layer buffer. All 5 quality assets plus frame settings must match.
- **Token rotation feel.** Too responsive looks like tag-teaming, too sticky ignores the player's focus. Tune min hold, margin and confirm time in Studio.
- **Body-type vs §16 (resolved D6).** §16 element rules override body type (Laser never bleeds, Ion never burns). Enforced via `allowsBlood` / `allowsBurn` on `DMAmmoFxProfile`.
- **Trigger shapes on dynamic ragdoll bones** (stagger/death). PhysX normally excludes trigger shapes from mass/inertia, but this needs verifying in play. Fallback: disable the rig during ragdoll phases and use bone colliders for those.
- **Invector/DM collider passes.** `EnemyInvectorHitSetup` re-runs at bootstrap, rig repair strips physics outside the hips, and vRagdoll toggles bodypart colliders. The hitbox children must be skipped by all three; this needs thorough testing.
- **Enemy weapon colliders** kept enabled under the hierarchy may still be hit by bullets. Decide to exclude them from ranged queries (recommended).
- **Behaviour regressions.** The token gate could make fights feel passive. Tune `tokenHoldMax` and the cooldown; the 2-engager option exists.
- **§31 order.** The docs say the utility brain comes next. This adds a small director early, so the brain must call into it rather than duplicate it.
- **Concurrent work.** The loot-chest agent touches the death/loot flow (`EnemyDeathSequence`). Mark clearing hooks `Died` directly to stay independent of it.

---

## 12. Open questions

**None.** All questions are decided (see §0, dated 2026-10-07).

| # | Was open | Decision |
|---|---|---|
| D1 | Attackers at once | Exactly one melee attacker; token rotates by focus. |
| D2 | Blood/burn rules | Per body type (Humanoid / Android / Robot); FRED = Android. |
| D3 | Project settings | Decal Layers on all 5 HDRP assets; layer 26 `DMHitbox`. |
| D4 | Zone multipliers | Deferred; stay 1.0. |
| D5 | Holders' idle life | Occasional sidestep / taunt with anti-circling caps. |
| D6 | §16 vs body type | **Keep §16.** Laser never bleeds; Ion never burns. Element rules override body type. |
| D7 | Robot / Humanoid test targets | **Yes.** Clone FRED → `Robot.prefab` (mech recolor, bodyType Robot) and `Humanoid.prefab` (bodyType Humanoid); put in a test scene. Plan text only until build. |
| D8 | Humanoid bullet hit mark | **Bright red blood spot with a circular char** (no android glow). Blood splatter still plays. |
