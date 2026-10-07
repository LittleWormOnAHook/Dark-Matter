# Moving melee — layer policy & lessons learned

**Project:** Dark Matter: Genesis (Unity 6 HDRP 6000.4.11f1)  
**Status:** Design canon — Phase 2 melee (Jetpack controller)  
**Audience:** Combat animation, Combat Studio / Build And Apply, agent studio edits  
**Last updated:** Oct 6, 2026  

**Related:** `DM_Melee_Animation_Library_Plan.md` **§10**; `DMG_Combat_Plan_v2.md` §0 / §15; controller `Assets/_Project/Animations/Player/Invector@ShooterMelee_Jetpack.controller`.

**GitHub wiki mirror:** [Combat-Moving-Melee-Layer-Policy](https://github.com/LittleWormOnAHook/Dark-Matter/wiki/Combat-Moving-Melee-Layer-Policy) — keep in sync with this file when publishing wiki updates.

---

## 1. Industry / feasibility (brief)

Lower-body locomotion on the **Base Layer** plus upper-body–masked attacks on an **UpperBody** (or equivalent) layer is standard in third-person action RPGs (Invector’s UpperBody mask, many ARPGs). **Full-body** attack clips while moving are also shipped in production via:

- Short **full-body override** states that temporarily take over locomotion,
- **Root motion** on the attack clip,
- Or a **hybrid**: light taps upper-body / masked; heavies and charged releases use full-body when the swing needs hip commitment.

Failures on this project were **implementation**, not invalid design:

| Failure mode | What went wrong |
|---|---|
| Base Layer crossfade | Full Mixamo melee clips crossfaded on **Base** without mask → legs stopped / hip sink |
| FullBody weight | Layer weight left at **0** → attacks never visible |
| Extra `DM_MeleeUpper` layer | Added without proper exit transitions → stuck states, no blend back to locomotion |

**Takeaway:** Moving melee is feasible and aligned with GDD combat feel; enforce layer policy and weight restoration on every swing.

---

## 2. Dark Matter canon — animator layers (Jetpack controller)

**Controller:** `Invector@ShooterMelee_Jetpack.controller`  
**Runtime authority:** clip refs via `DM_MeleeAnimationSet` + **Build And Apply** (`DMMeleeAnimationSetApplier`); tunables via `DM_CombatCoreProfile.Live` (Genesis **Combat Core**).

### 2.1 Base Layer

- **Locomotion only** (walk, run, sprint, jump, climb handoff, etc.).
- **Never** crossfade full Mixamo / vendor **melee attack** clips here.
- Base may carry additive lean / aim offsets already authored on the Jetpack graph — not substitute for attack states.

### 2.2 UpperBody (masked spine / arms / head)

Use when the player should **keep moving** and the clip must not steal hips:

| Use case | Example clip / wiring |
|---|---|
| Hold **E** + **LMB** combo lane | **One Hand Sword Combo** @ **1.75** playback (catalog / applier slot) |
| Other upper-only swings | When a full-body clip would **hip-sink** or kill gait on Io slopes |

UpperBody attacks must **exit cleanly** (transitions back to default upper pose or empty upper state) so locomotion Base is uninterrupted.

### 2.3 FullBody

| Context | Policy |
|---|---|
| **Standing** attacks | Default home for strong telegraphs and committed swings |
| **While moving** (hybrid — Anthony direction) | **Light tap**, **charge hold**, and **charged release A/B/C** when design needs a full swing arc (commitment, reach, VFX timing) |
| **Weight** | FullBody layer weight must **never stay 0** during an active attack; **restore** default weight after **every** swing (including cancels and hit reactions) |
| **Return to Null** | Every FullBody attack Pioneer CrossFades into (Weak/Strong/Random A/B/C, Parry, Hold E upper) must dest-transition to **Idle_Empty** (exitTime ~0.98, no extra conditions) so the stick watchdog can CrossFade from the live attack at ~0.95. Runtime **`RestoreMeleeFullBodyIdle()`** parks `Idle_Empty` (motion null) — nested `Attacks.Null` keeps the last swing pose. Do **not** leave the last swing frame on FullBody. **SwordCharge** uses **1HandSwordChargeUp** frozen at the draw-back frame while the button is held (no looped swing, no max hold, no auto-swing); release plays weighted Strong A/B/C then parks empty. |

Do not leave FullBody suppressed “to fix” foot sliding — fix clip choice, transition timing, or use UpperBody for that swing instead.

### 2.4 `DM_MeleeUpper` — deprecated

- Custom **`DM_MeleeUpper`** layer was **removed** from the Jetpack controller.
- **Do not reintroduce** a parallel melee upper layer without a full transition audit and Combat Studio sign-off.
- Use Invector **`UpperBody`** + **`FullBody`** only.

---

## 3. Clip assignments (Phase 2 baseline)

| Slot / role | Clip (vendor path under project) |
|---|---|
| **Strong B** (charge release primary) | Mixamo **Axe Standing Melee Combo Ver. 1** |
| **Strong C** | Mixamo **Axe Standing Melee Attack 360 Low** |
| **Parry** | PROTOFACTOR **RightHand@Parry01** / **Parry01_Hit** |

Weak chain, random pool, and charge hold remain as wired in `DM_MeleeAnimationSet` + applier (see animation library plan §2–§3).

### 3.1 Charged release — A / B / C

- On charged release, pick **Strong A**, **B**, or **C** via **weighted random** (favor **A**).
- **Damage windows** are per slot (animation events / `vMeleeAttackControl`):
  - **A** and **C**: earlier active frames
  - **B**: later active window (matches longer wind-up on **Axe Standing Melee Combo Ver. 1**)

Tune weights and windows in Combat Core + event copy from reference states after **Build And Apply**.

---

## 4. Agent workflow (studio / combat edits)

Before non-trivial animator or applier changes:

1. **Recall** — git diff on controller YAML, `DMMeleeAnimationSetApplier`, profiles; read prior attempts (worked vs failed). See `.cursor/rules/dark-matter-genesis-studio-system-edit-recall.mdc`.
2. **Edit** — assign clips in Genesis **Combat → Melee Animations**; push with Combat Studio **Build And Apply**; tune numbers in **Combat Core** (`Profile.Live`).
3. **Unity** — after disk edits under `Assets/`, MCP **`refresh_unity`** (`if_dirty`, `compile: request` when scripts/asmdef change, `wait_for_ready: true`). See `.cursor/rules/unity-agent-workflow.mdc`.
4. **Verify** — console **zero errors**; Play in `Dark Matter Genesis v1.6.5.unity`: move + light tap, move + charge hold + release A/B/C, stand strongs, Hold E + LMB combo, parry — confirm FullBody weight restores and Base never plays raw melee FBX.

---

*Canonical repo path: `Assets/_Project/Documentation/Design/Combat/DM_Melee_Locomotion_Layer_Policy.md`*
