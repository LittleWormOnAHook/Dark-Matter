# DM Melee Animation Library Plan

**Project:** Dark Matter: Genesis (Unity 6 HDRP 6000.4.11f1)  
**Status:** Research / shopping plan only — no purchases or imports in this pass  
**Audience:** Phase 2 polish → blade/control weapons (sword, knife, baton) + Phase 3 enemy `Humanoid_Enemy` mirror  
**Last updated:** Oct 5, 2026 (skill-gated clips, special-hold input model, catalog ↔ skill IDs — doc only)

**Reconcile:** Prior subagent `9da186c5` transcript was not found on disk; this file merges the earlier sword-focused draft with Anthony’s addendum (**regular melee attacks** for **sword**, **knives**, and **batons** — not only 2–3 hit chains, finishers, and charge).

---

## 1. Goal

Build a **licensed, humanoid-retargetable** melee clip library that covers **everyday combat motion**, not just spectacle:

| Motion class | Player expectation | Invector / DM wiring |
|---|---|---|
| **Regular / idle attacks** | Single-tap weak swings that do **not** force a 3-hit chain; variety when the player spam-clicks or mixes direction | `SwordRandomAttack` A/B/C (AttackID **2** on Jetpack controller) + optional extra singles |
| **Light chains (2–3)** | Deliberate combo route A→B→C | `WeakAttacks/SwordAttack` A/B/C ← `DMMeleeClipSlots` lightA/B/C |
| **Heavy / charge** | Hold-to-charge release + alternate heavies | `StrongAttacks/SwordAttack` A/B/C + `SwordCharge` hold ← strongA/B/C, chargeHold |
| **Finishers** | Momentum / execution pairs (later) | **Not** in Invector SMBs yet → future `DM_MeleeFinisherCatalog` |
| **Special attacks** | Signature hold (weapon/element), not tap combo or charge release | **Not wired** — dedicated Input System hold (TBD; ask-before-assign) + future AttackID or Special sub-SM (§9.4) |

**Unlock policy:** Some imported clips stay in the catalog for Studio and applier, but **playback is skill-gated** at runtime (combo extensions, finishers, weapon styles, knife/baton families). Base-tier clips always play when the weapon is equipped.

**Runtime authority split (do not blur):**

- **Clip references + animator YAML** → `DM_MeleeAnimationSet` + **Build And Apply** (`DMMeleeAnimationSetApplier`).
- **Charge seconds, strong anim speed, hitbox scale, parry window, etc.** → `DM_CombatCoreProfile.Live` (Genesis **Combat Core**).

**Design canon (combat plan):** Knife = fast / short / backstab; Baton = stagger / knockdown / control (no cutting). Sick Stick remains a separate future weapon.

---

## 2. Verified AttackID map (Jetpack controller, Oct 5 2026)

Source of truth: `Assets/_Project/Animations/Player/Invector@ShooterMelee_Jetpack.controller` → `FullBody` → `Attacks` → `WeakAttacks` entry transitions.

| AttackID | WeakAttacks sub-state machine | Shipped on disk today | DM weapon target |
|---:|---|---|---|
| **0** | Unarmed punch SMB | Yes | Fists / no weapon |
| **1** | `SwordAttack` (A→B→C combo) | Yes — **Build And Apply** wires `oneHandSword` | **One-hand sword** (all `_Project` melee prefabs use `attackID: 1` today) |
| **2** | Enters `SwordRandomAttack` (parallel A/B/C, **not** combo chain) | Yes — Mixamo paths hardcoded in applier | **Regular sword singles** / tap variety |
| **3** | *(unused)* | **No** | **Proposed: knife / dagger** |
| **4** | `2HandWeaponAttack` | Invector states exist; clips not DM-cataloged | Two-hand greatsword / polearm |
| **5** | `Dual SwordAttack` | Invector default dual swords | Dual wield |

**StrongAttacks** mirror the same AttackID routing for `SwordAttack` A/B/C and `SwordCharge` (currently applied only for **AttackID 1** one-hand sword via applier).

**Doc correction:** `EnemyInvectorCombatBridge` comment “AttackID 2 = two-hand” does **not** match the Jetpack controller (2 = random weak). Treat the **controller YAML** as authoritative when wiring new weapons.

**Recommended DM assignment (when knife/baton items ship):**

| Weapon family | `vMeleeWeapon.attackID` | New animator work |
|---|---:|---|
| One-hand sword | **1** (keep) | None — Phase 2 baseline |
| Knife / dagger | **3** | Duplicate `SwordAttack` + `StrongAttacks` subtree; transitions `AttackID == 3`; extend applier `ApplyKnifeToController` |
| Baton / club | **6** *(free ID)* | Same pattern as knife; shorter reach, more vertical strikes |
| Regular sword taps (optional split) | **2** | Keep random pool; assign `randomA/B/C` on catalog when applier exposes slots |

Knife and baton items are **Missing** in combat plan §12; catalog slots `knife` exist but **baton** needs a new `DMMeleeClipSlots baton` field on `DM_MeleeAnimationSet` (only `torch` today for blunt-adjacent).

---

## 3. Weapon-family slot tables (`DM_MeleeAnimationSet`)

Each family uses `DMMeleeClipSlots`: `lightA/B/C`, `strongA/B/C`, `chargeHold`.  
**Phase 2 code gap:** applier only **pushes** `oneHandSword` to controllers; `knife` is populated in the asset but not applied. **Planned:** `randomA/B/C` on slots for AttackID 2 pool (today hardcoded paths in applier).

### 3.1 One-hand sword (AttackID **1** + random **2**)

| Slot | Role | Disk today | Gap / next acquisition |
|---|---|---|---|
| lightA/B/C | Regular **chain** lights | Invector `WeakAttack_SwordA/B/C` | Optional polish from Sword Combat Pack (58) or Quaternius UAL2 splits |
| *(random A/B/C)* | Regular **non-chain** taps | Mixamo `Sword And Shield Attack/Slash` (applier constants) | Move to catalog fields; add 2–3 more distinct singles |
| strongA/C | Heavy variants | Mixamo slashes | Extra telegraph heavies from store combo packs |
| strongB | Charge **release** | PROTOFACTOR `Humanoid@AttackC1hMelee` | Keep; tune via profile speed |
| chargeHold | Charge loop pose | Same as strongB fallback | Optional dedicated loop from Mocap Sword & Shield “Force Attack” packs (Tier 2) |
| Finisher | Execution | ~3 finisher-tagged clips on disk | Tier 2–3 paired packs (RamsterZ, Longsword Vol.2) when Momentum ships |

### 3.2 Knife / dagger (AttackID **3** — proposed)

| Slot | Role | Disk today | Gap / next acquisition |
|---|---|---|---|
| lightA/B/C | Fast 2–3 hit chain | **Partial:** Mixamo mislabeled sword slashes in `knife` folder (2 clips only) | Replace with true dagger stabs/slashes; need **lightC** |
| random* | Quick stabs (non-chain) | None | Mixamo search: `Standing Melee Attack`, `Mutant Punch` (short); GDH one-stage dagger singles |
| strongA/C | Heavy stabs / riposte | None | Dagger Anim Set 1 heavies; GDH assassination heavies |
| strongB / chargeHold | Charge or held thrust | None | Optional shorter charge than sword; many dagger packs use heavy-as-release without hold |
| Finisher | Backstab / execution | None in knife tag count | GDH execution set; `Knife_MocapAnimPack` melee set; Tier 2 `Daggers Animation Pack` executions |

**On-disk keyword scan (audit):** ~17 knife-tagged clips — not yet mapped to profiles (`DMG_Combat_Animation_Tag_Sheet.md`).

### 3.3 Baton / club (AttackID **6** — proposed)

| Slot | Role | Disk today | Gap / next acquisition |
|---|---|---|---|
| lightA/B/C | Quick strikes / jabs | **~2** baton/club keyword hits | Critical gap — almost entire family missing |
| random* | Single-tap control hits | None | Kevin Iglesias **club** singles (Human Melee); Mixamo `Standing Melee Attack*` + `Heavy Weapon Swing` (retarget as baton) |
| strongA/C | Wide swings / knockdown setup | None | Riot Control **Baton Techniques**; Human Melee club/heavy attacks |
| strongB / chargeHold | Held baton strike / power hit | None | Tonfa/baton packs with wind-up; or reuse sword charge timing with baton clips |
| Finisher | Non-lethal takedown | None | Riot Control less-lethal; later Sick Stick overlap **not** baton |

Control branch design: prioritize **stagger** timing and poise over slice VFX — animation events must match shorter hit windows than sword.

---

## 4. Source evaluation matrix (sword, knife, baton)

Legend: **Ship** = commercial game OK under stated terms; verify EULA at purchase. **Effort:** Low = Humanoid in-place clip; Med = clip pick + events; High = new AttackID subtree + paired finishers.

### 4.1 Free / CC0 (all families)

| Source | URL | License | Sword | Knife | Baton | Notes |
|---|---|---|---|---|---|---|
| **Mixamo** | https://www.mixamo.com | Free tier; [commercial OK, no raw redistribution](https://community.adobe.com/questions-696/mixamo-faq-licensing-royalties-ownership-eula-and-tos-589400) | Combos, S&S singles, heavies | Generic melee / short slashes (verify prop) | `Standing Melee Attack*`, `Heavy Weapon Swing`, boxing-adjacent | Already in repo under `Assets/Animations/Mixamo Animations/` |
| **Quaternius UAL 2** | https://quaternius.itch.io/universal-animation-library-2 | CC0 | Combo splits | Short melee | Unarmed + prop baton | Unity 6 friendly |
| **KayKit Character Animations** | https://kaylousberg.itch.io/kaykit-character-animations | CC0 | Melee category | Short 1H | Unarmed / dual | Io low-poly enemy flavor |
| **Kevin Iglesias Human Melee FREE** | https://assetstore.unity.com/packages/3d/animations/human-melee-animations-free-165785 | Unity EULA | 1 sample attack | 1 sample | 1 sample club path in full pack | Block/parry/hit diversity |
| **MoCap Online (sampler / blog)** | https://mocaponline.com/products/tc-sword | Commercial cap on some SKUs | TC Sword sampler | Browse [store combat](https://mocaponline.com/) | Melee / punching packs | Humanoid Unity exports |

### 4.2 Paid — sword (existing plan + regular attacks)

| Source | URL | ~Price | Regular singles | Combos | Charge | Finishers |
|---|---|---:|---|---|---|---|
| **Sword Combat Animation Pack (58)** | https://assetstore.unity.com/packages/3d/animations/sword-combat-animation-pack-320870 | ~$19 | Telegraphs + singles | 8×2-hit, 5×3-hit | Heavy clips | — |
| **PROTOFACTOR 1 Handed** (owned) | https://assetstore.unity.com/packages/3d/animations/animset-1-handed-melee-weapon-195757 | $19.99 | AttackA/B/C singles | Chain with Invector | — | — |
| **One Handed Sword Attacks and Finishers** | https://assetstore.unity.com/packages/3d/animations/one-handed-sword-attacks-and-finishers-223503 | Store | 11 attacks + locomotion | Chains | — | 8 paired |
| **GDH One Handed Sword V2** | https://gamedev-hero.itch.io/onehanded-sword-animation-v2 | Paid | 30 one-stage + 10 attacks | Combos | — | Execution reactions |
| **Sword Combat Animations (112)** | https://assetstore.unity.com/packages/3d/animations/sword-combat-animations-350620 | Store | In attack set | 24 combos | Heavies | — |

### 4.3 Paid — knife / dagger

| Source | URL | ~Price | Regular singles | Combos | Charge / heavy | Finishers |
|---|---|---:|---|---|---|---|
| **Dagger Animations Set 1** | https://assetstore.unity.com/packages/3d/animations/dagger-animations-set-1-256482 | Store | 4 light (incl. finisher in light set) | 3-hit loop | 2 heavy | 4th light as finisher |
| **Knife_MocapAnimPack 1.1** | https://assetstore.unity.com/packages/3d/animations/knife-mocapanimpack-1-1-223863 | Store | MeleeSet mixed strength | Stand + crouch styles | Strong attacks in MeleeSet | Death/hit rich |
| **TwinDaggers Animset** | https://assetstore.unity.com/packages/3d/animations/twindaggers-animset-160069 | $49.99 | Many singles | Combos | — | — |
| **GDH Dagger Combat V1** | https://gamedev-hero.itch.io/dagger-combat-animation-v1-assassination-theme | $49.99+ | **94** one-stage + separate stages | 23 combo | Heavies in combo set | 83 execution/reaction |
| **Daggers Animation Pack (575)** | https://assetstore.unity.com/packages/3d/animations/daggers-animation-pack-331055 | Store | Attack + run attack | Combo + air | Skills / ultimate | Execution |
| **Anime Dual Dagger (48)** | https://assetstore.unity.com/packages/3d/animations/anime-dual-dagger-animation-pack-369394 | Store | Fast singles | Stylized chains | Dash attacks | Finishers | Stylized — Infiltrator optional |

### 4.4 Paid — baton / club / control

| Source | URL | ~Price | Regular singles | Combos | Charge / heavy | Control / finishers |
|---|---|---:|---|---|---|---|
| **Kevin Iglesias Human Melee full** | https://assetstore.unity.com/packages/3d/animations/human-melee-animations-151650 | ~$23 | 5× RH (clubs/dagger/sword) | Mix into A/B/C | — | Stun, parry |
| **Riot Control Police Animation** | https://assetstore.unity.com/packages/3d/animations/riot-control-police-animation-376010 | Store | **Baton Techniques (6)** | Locomotion sets | Less-lethal | Shield/baton procedural |
| **Dual Tonfa (Motion Cast 21)** | https://assetstore.unity.com/packages/3d/animations/dual-tonfa-animations-motion-cast-21-vol-2-321927 | Store | Punches + combos | Tonfa chains | — | Dodges — retarget as security baton |
| **Mixamo + baton mesh** | https://www.mixamo.com | $0 | Melee + boxing | Manual chain | Heavy Weapon Swing | — |

**Not recommended as primary Kade source:** Synty/POLYGON-first packs for the **player** unless companions/enemies share POLYGON rigs — use for **corrupt android / low-poly enemy** variants ([Synty Sword Combat](https://assetstore.unity.com/packages/3d/animations/synty-animation-sword-combat-291806)).

**Already on disk (baseline):** Invector weak sword ABC, PROTOFACTOR AttackC, Mixamo folders, ~334 sword / ~17 knife / ~2 baton keyword files (untagged for profiles).

---

## 5. Recommended library architecture

### 5.1 Keep one catalog asset (expand slots, don’t fork)

**Recommendation:** Continue **`DM_MeleeAnimationSet`** (`Resources/Combat/DM_MeleeAnimationSet`).

| Change | Priority | Notes |
|---|---|---|
| Add `DMMeleeClipSlots baton` | **P0** for control branch | Symmetry with `knife`, `axe`, `torch` |
| Add optional `randomA/B/C` per family | P1 | Removes hardcoded Mixamo paths from applier |
| Add `inPlaceCombatIdle` optional clip ref | P2 | Per-family combat idle (Knife pack has stand/crouch) |
| `enemyMirror` nested under each family OR shared until diverge | P1 Phase 3 | Same AttackID layout on `The_Evil_OneController` |

**Phase 3+ finishers:** `DM_MeleeFinisherCatalog` (paired attacker/victim, momentum gates) — combat plan: finishers **Not started**.

**Skill ↔ catalog (planned data, not shipped):** Extend slot entries (or parallel metadata rows) with optional `SkillDefinition` / skill-branch id + `unlockTier` (**Base** | **SkillGated**). Genesis **Combat → Melee Animations** still assigns every clip; gameplay filters what the player (or AI archetype) may request. See **§9**.

### 5.2 Applier extensions (implementation order)

1. `ApplyOneHandSwordToController` — **shipped**
2. `ApplyKnifeToController` — AttackID **3** weak/strong/charge states (duplicate sword SMB, swap clips from `set.knife`)
3. `ApplyBatonToController` — AttackID **6**
4. Combat Studio toggle: **Apply to Player | Enemy | Both** (enemy mirror)
5. Never assign combo `lightA/B/C` into `SwordRandomAttack` (regression called out in applier remarks)

### 5.3 Naming & folders

| Item | Convention |
|---|---|
| Import folder | `Assets/_Project/Animations/Combat/<Family>/` (`OneHandSword`, `Knife`, `Baton`, `EnemyHumanoid`) |
| Clip names | `DM_Anim_Knife_LightA`, `DM_Anim_Baton_StrongB`, … (no vendor brands in `_Project` names) |
| Vendor drops | Stay under `Assets/PROTOFACTOR/`, Mixamo, etc. — gameplay refs only via catalog SO |

### 5.4 Genesis Studio + Combat Studio

| Step | Tool |
|---|---|
| Assign clips | Genesis **Combat → Melee Animations** (`DM_MeleeAnimationSet`) |
| Push to controllers | **Build And Apply Melee Animation Set** |
| Tune charge / strong speed | **Combat Core** profile |
| Play test | `Dark Matter Genesis v1.6.5.unity` — per family: tap singles, chain, charge, enemy mirror |

---

## 6. Enemy mirror library

| Phase | Behavior |
|---|---|
| **Now** | Applier writes **same** `oneHandSword` clips to player Jetpack + `The_Evil_OneController` |
| **Phase 3** | `Humanoid_Enemy` uses mirror slots; optional ±5–10% anim speed via profile, not duplicate FBX |
| **Archetypes** | `corrupt_patrol_android` → POLYGON/Synty subset; catalog metadata `archetypeId` without renaming SMB states |
| **AI bridge** | `EnemyInvectorCombatBridge` must set `AttackID` from equipped weapon — never fall through to **0** when melee drawn |

Enemies need the **same** regular-vs-chain distinction: AttackID **2** random pool for unpredictable single swings, AttackID **1** (or 3/6) for telegraphed combos.

---

## 7. Prioritized acquisition list (free → paid)

### Tier 0 — Free (do first, all three families)

1. [Mixamo](https://www.mixamo.com) — sword: fill random pool + combos; knife: short melee / stab searches; baton: `Standing Melee Attack*`, `Heavy Weapon Swing`, boxing crosses.  
2. [Quaternius UAL 2](https://quaternius.itch.io/universal-animation-library-2) — CC0 combo splits + melee prototyping.  
3. [KayKit Character Animations](https://kaylousberg.itch.io/kaykit-character-animations) — CC0 enemy/android melee.  
4. [Kevin Iglesias Human Melee FREE](https://assetstore.unity.com/packages/3d/animations/human-melee-animations-free-165785) — validate club/dagger/sword retarget on Kade.  
5. [MoCap Online TC Sword sampler](https://mocaponline.com/products/tc-sword) — pipeline test before knife/baton mocap buys.

### Tier 1 — Low cost, high fit (regular attacks + chains)

6. [Sword Combat Animation Pack (58)](https://assetstore.unity.com/packages/3d/animations/sword-combat-animation-pack-320870) — singles + 2/3-hit combos + telegraphs.  
7. [Kevin Iglesias Human Melee full](https://assetstore.unity.com/packages/3d/animations/human-melee-animations-151650) — **club + dagger** singles in one buy (~$23).  
8. [Dagger Animations Set 1](https://assetstore.unity.com/packages/3d/animations/dagger-animations-set-1-256482) — budget knife chain + heavies.  
9. **PROTOFACTOR 1 Handed** — extend **owned** Ultimate folder before duplicate sword vendors.  
10. [Riot Control Police Animation](https://assetstore.unity.com/packages/3d/animations/riot-control-police-animation-376010) — baton-specific if Mixamo + Iglesias insufficient.

### Tier 2 — Depth (finishers, charge loops, mocap knife)

11. [Knife_MocapAnimPack 1.1](https://assetstore.unity.com/packages/3d/animations/knife-mocapanimpack-1-1-223863) — realistic infiltrator kit.  
12. [GDH Dagger Combat V1](https://gamedev-hero.itch.io/dagger-combat-animation-v1-assassination-theme) — large one-stage + execution library (Mixamo FBX included).  
13. [Dual Tonfa Motion Cast 21](https://assetstore.unity.com/packages/3d/animations/dual-tonfa-animations-motion-cast-21-vol-2-321927) — baton-adjacent combos.  
14. [Sword And Shield Animation Pack (258347)](https://assetstore.unity.com/packages/3d/animations/sword-and-shield-animation-pack-258347) — 2 charge + 3 executions (boss/enemy).  
15. [One Handed Sword Attacks and Finishers](https://assetstore.unity.com/packages/3d/animations/one-handed-sword-attacks-and-finishers-223503) — paired sword finishers.

### Tier 3 — Breadth / cinematic / stylized

16. [RamsterZ Sword and Shield (256766)](https://assetstore.unity.com/packages/3d/animations/sword-and-shield-animations-256766) — 18 paired finishers.  
17. [MoCap Central Longsword Vol.2 (362498)](https://assetstore.unity.com/packages/3d/animations/mc-longsword-vol-2-believable-3d-animations-by-mocap-central-362498) — HEMA + paired finishers.  
18. [Daggers Animation Pack (575)](https://assetstore.unity.com/packages/3d/animations/daggers-animation-pack-331055) — ARPG-scale knife library.  
19. [503 Sword & Shield mega pack (335332)](https://assetstore.unity.com/packages/3d/animations/sword-and-shield-combat-animation-pack-blade-shield-animations-335332) — HDRP-friendly volume.  
20. [Synty ANIMATION Sword Combat (291806)](https://assetstore.unity.com/packages/3d/animations/synty-animation-sword-combat-291806) — POLYGON enemy mirror.

---

## 8. Mixamo search cheat sheets

### Sword (AttackID 1 + 2)

| Role | Mixamo queries |
|---|---|
| Chain lightA/B/C | `One Hand Sword Combo` (split frames), Invector ABC (keep) |
| Regular singles (random) | `Sword And Shield Attack`, `Stable Sword Outward Slash`, `Standing Melee Attack Horizontal` |
| strongA/C | `Heavy Weapon Swing`, heavy `Sword And Shield Slash` variants |
| strongB / release | PROTOFACTOR AttackC or matched heavy wind-up |

### Knife (AttackID 3 — after SMB duplicate)

| Role | Mixamo queries |
|---|---|
| lightA/B/C | `Standing Melee Attack` (short), `Mutant Punch` (fast close — retarget), `Knife` if listed |
| Regular singles | Distinct short stabs — avoid long sword arcs |
| Heavy / finisher stand-ins | `Standing Melee Attack Downward`, assassination-style crouch attacks |

### Baton (AttackID 6 — after SMB duplicate)

| Role | Mixamo queries |
|---|---|
| lightA/B/C | `Standing Melee Attack Horizontal/Downward`, `Boxing`/`Cross Punch` (retarget + baton prop) |
| Regular singles | `Heavy Weapon Swing` (shortened), `Elbow Punch` |
| Heavy / control | `Heavy Weapon Swing`, wide `Standing Melee Combo` |

---

## 9. Skill unlock tiers, input model, and playback gating

**Scope:** Design + alignment with GDD §19 and combat plan §17 / §19 only. **Do not** implement skill-tree hooks, new Input System bindings, or runtime clip filters in this pass.

### 9.1 Unlock tiers

| Tier | When clips play | Typical clip classes | Skill tree (combat plan §19) |
|---|---|---|---|
| **Base (always)** | Weapon equipped + valid combat state | Default `lightA/B/C`, AttackID **2** random singles, baseline heavies, charge hold/release (`strongB`, `SwordCharge`) | None |
| **Skill-gated** | Base tier **plus** unlocked node(s) on save | Extra chain links, finisher pairs, weapon-style variants, **knife/baton family** unlocks, signature specials | **Blade**, **Control**, elemental mastery side panel |

Acquisition/import order is independent of unlock: a paid pack can land on disk early; **runtime** (future) rejects gated clips until the matching skill id is allocated.

### 9.2 Input model — three lanes (do not collapse)

| Lane | Player input | Current / planned wiring | Must not be confused with |
|---|---|---|---|
| **Light / combo** | **Tap** attack (RT / LMB today) | Weak `SwordAttack` A→B→C; AttackID **2** random pool | Special hold |
| **Strong charge** | **Hold** attack past `strongMeleeChargeSeconds` on `DM_CombatCoreProfile.Live` | `SwordCharge` → Strong `SwordAttack.B` | Special hold (charge is same button, different threshold — specials use a **different** binding) |
| **Special** | **Dedicated hold** on separate KBM key **and** controller button | Input action **TBD** (proposed placeholder id: `Combat/SpecialHold` — **not** added until Anthony approves per combat plan binding gaps) | Light tap; charge-only on attack button |

**Policy:** Same as §0 combat bindings — **ask-before-assign**. Document the slot; do not add actions to `InputSystem_Actions` (or Pioneer input bridges) without explicit approval.

### 9.3 Catalog architecture and runtime filter

| Layer | Responsibility |
|---|---|
| **`DM_MeleeAnimationSet`** | Canonical clip refs per weapon family (`DMMeleeClipSlots` + planned `randomA/B/C`, `baton`). Studio + **Build And Apply** push clips to animator YAML. |
| **`DM_MeleeFinisherCatalog` (future)** | Paired attacker/victim rows; each entry references finisher/skill ids + momentum rules (§17). |
| **`SkillDefinition` / save allocations (existing hex tree)** | Today: stat modifiers only. **Future:** nodes expose catalog keys or `skillId` lists that unlock clip rows (§19 animation bullet). |
| **Runtime gate (future code)** | On attack/finisher/special request: resolve equipped weapon family → eligible clips = catalog ∩ unlocked skills (player) or archetype profile (enemy). **Does not** remove clips from the controller — rejects or reroutes the **request**. |

**Enemy mirror:** Same AttackID layout; AI picks from archetype-allowed subset, not player skill save.

**Studio workflow unchanged:** Assign all clips in Genesis **Combat → Melee Animations** and Combat Studio **Build And Apply**; tune charge/strong speed in **Combat Core**. Gating is a gameplay layer above Invector attack triggers.

### 9.4 Jetpack controller plan — specials (Phase 2 polish → pre–Phase 3)

Player controller: `Invector@ShooterMelee_Jetpack.controller` (`FullBody` → `Attacks`). Weak/strong/charge paths exist for AttackID **1**; specials **not** started.

| Approach | Use when | Plan |
|---|---|---|
| **AttackID branch** | One-shot specials share weak/strong cancel rules | Free id (e.g. **7**) + duplicate SMB entry transitions; dedicated hold fires `AttackID == 7` |
| **Special sub-SM** | Distinct i-frames, cancels, or paired timing | New `Attacks/SpecialAttacks` under `FullBody`; trigger via bool/int from input layer |

**Sequencing (doc only):** Finish knife/baton SMB duplication (§5.2) → document one proof special in catalog → controller YAML + gated playback lands with §31 **#6–#8** (Momentum / finishers / specials), **after** §31 **#3** utility brain — not before.

---

## 10. Integration checklist (per clip batch)

- [ ] Import FBX → Rig **Humanoid**, **In Place** for gameplay (root motion off unless enemy AI needs RM).
- [ ] Duplicate clip → `DM_Anim_*` under `Assets/_Project/Animations/Combat/<Family>/`.
- [ ] Assign `DM_MeleeAnimationSet` slots (+ `baton` field when added in code).
- [ ] **Build And Apply** → player + enemy controllers for **each AttackID** touched.
- [ ] Copy **animation events** from reference sword states (`vMeleeAttackControl` windows).
- [ ] Play: **regular taps** (random pool), **chain** A→B→C, **charge**, family-specific reach.
- [ ] Update animation tag sheet counts / profile ids (`DMG_Combat_Animation_Tag_Sheet.md`).
- [ ] Record vendor + license in source manifest.
- [ ] MCP refresh + console clean before commit.

---

## 11. Open decisions for Anthony

1. **Knife/baton AttackID:** Approve **3** (knife) and **6** (baton) with duplicated Invector SMBs vs temporary **reuse AttackID 1** with clip-only swaps (faster but wrong semantics for dual-wield + random pool).  
2. **Visual tone:** Tactical mocap (Knife_Mocap, Riot Control) vs CC0 stylized for Kade.  
3. **Regular attacks:** Expand AttackID **2** random pool only for sword, or add per-family random SMBs.  
4. **Finishers:** Buy paired packs now vs wait for Momentum UI + bindings.  
5. **Budget:** Tier 0–1 only (~$0–60) vs add GDH dagger (~$50) in one pass.  
6. **Special hold binding:** Approve KBM + gamepad actions for the dedicated special lane (placeholder `Combat/SpecialHold`) vs reuse an existing spare binding.  
7. **Special controller shape:** AttackID **7** branch vs `SpecialAttacks` sub-SM for the first sword signature.

---

*Related: `DMG_Combat_Plan_v2.md` §0, §17, §19, §31; `DMG_Combat_Animation_Tag_Sheet.md`, `DM_MeleeAnimationSet.cs`, `DMMeleeAnimationSetApplier.cs`, Genesis **Combat → Melee Animations**.*
