# DM Player Strafe Locomotion Plan (Rev 2, decisions recorded)

Date: 2026-10-19. Owner: Anthony.
**Status: Parked - resume later.** Decisions taken (all defaults, see §8). Nothing implemented yet; start at P0.
Scope: let the player side-step, backpedal and move diagonally using the existing
`Base Layer/Locomotion/Strafe Locomotion` trees, without retuning `Player_v7`.

Rules honoured: dark-matter-genesis-player-physics.mdc (no capsule / layer / rigidbody / physics retune,
no root motion + code together, no NavMesh, no motion matching), dark-matter-genesis-core.mdc,
dm-naming-no-invector.mdc (new names are `DM*`). All runtime work goes in small DM scripts on
`Player_v7 Variant` and a Studio profile section. `Player_v7.prefab` is not edited.

---

## 1. What exists today (read-only findings)

**Controller.** `Player_v7` uses `Assets/_Project/Animations/Player/Invector@ShooterMelee_Jetpack.controller`.
`vThirdPersonController`: `_locomotionType = FreeWithStrafe` (the Variant overrides it with the same value 0), `useRootMotion = 0` (code-driven
speed), `rotateByWorld = 0` (camera-relative input), `sprintOnlyFree = 1`, `useContinuousSprint = 1`,
`useTurnOnSpotAnim = 1`, and `freeSpeed` / `strafeSpeed` both set to rotationSpeed 10, `rotateWithCamera = 0`.

**Gait.** `DMLocomotionGaitController` (Features/Locomotion) owns speeds through `DM_ClimbDashProfile`
("Locomotion: walk / run / sprint"):
- default **slow walk**: run speed 4 × 0.5 = **2.0 m/s**
- Shift-hold **jog**: **4.0 m/s**
- Shift double-tap **burst sprint**: 6 × 1.2 = **7.2 m/s**
- the gamepad L3 auto-run latch

It writes `motor.isSprinting` directly (Invector `Sprint()` is skipped).

**Base Layer / Locomotion state machine.**
- *Free Locomotion / Free Movement*: a 1D blend on `InputMagnitude`. Idle (0), Walk (0.5, with turn-lean on
  `RotationMagnitude`), Run (1, turn-lean) and Sprint (1.5). Forward only. Free Crouch: 1D.
- *Strafe Locomotion / Strafing Movement* (tag HeadTrack): a 1D blend on `MoveSet_ID`, then on `InputMagnitude`, then a
  2D Freeform Cartesian blend on `InputHorizontal` / `InputVertical`.

| MoveSet | Walk (0.5) | Run (1) | Sprint (1.5) | Idle turn-in-place |
|---|---|---|---|---|
| 0 Unarmed | 8-dir (F, B, L/R, 4 diagonals, mirrored) | 8-dir | forward Sprint clip | yes (WalkRight_InPlace ±) |
| 1 Shooter | 4-dir (no diagonals) | 4-dir | forward Sprint clip | yes (5 steps) |
| 2 Melee 1H | 4-dir | **missing** | missing | yes |
| 3 Melee 2H | 4-dir | **missing** | missing | idle only |
| 4 Bow | 4-dir | **missing** | missing | yes |

- *Strafing Crouch*: unarmed 8-dir, shooter 4-dir, melee 8-dir.
- *Transitions*: Free ↔ Strafe sub-machines switch on `IsStrafing` (state exits 0.25 s, no exit time).
  Crouch inside each sub-machine switches on `IsCrouching`. Entry picks by `IsStrafing` / `IsCrouching`.
  **The animator side is complete. Nothing in the controller blocks strafing.**

**Why A / D / S turn the body (root cause).**
1. The player is in free mode. `strafeInput.useInput = 0` (the Tab / R3 toggle is disabled), so `isStrafing` stays false,
   and `vThirdPersonController.ControlRotationType()` rotates the body toward `moveDirection`. A camera-relative A, D or S
   therefore turns him and walks him that way. This is by design of Invector free mode.
2. The existing backpedal helper is cancelled in the same frame.
   `PioneerShooterMeleeInput.MoveInput()` → `UpdateBackwardStrafeWithoutTurnaround()` sets `cc.isStrafing = true`
   on S. Later in the same `InputHandle()`, `vShooterMeleeInput` calls the **toggle** `cc.Strafe()` whenever
   `!IsAiming && isStrafing && !lockInStrafe`. That happens in the no-weapon branch (~L566), and on KBM in `base.AimInput()`
   (~L618, ~L713–719), which `PioneerShooterMeleeInput.AimInput` calls. The motor and animator only read the flag
   later, in `FixedUpdate` (`UpdateMotor` → `ControlLocomotionType` → `ControlRotation` → `UpdateAnimator`), so they see
   `false`, and S still turns him around. This is static analysis; confirm it with the P0 check below.
3. `vThirdPersonMotor.isStrafing` returns **false while `isSprinting` and `sprintOnlyFree`**, so burst sprint is
   always free (wanted, see §4.1).

**Other writers / readers of strafe state** (the new director must coexist with them):
- Shooter aim: `PioneerShooterMeleeInput.AimInput` forces strafe and `rotateWithCamera` while ADS. Mining scan aim
  hold goes through the same path.
- Lock-on (`vLockOn*`) uses `lockInStrafe`.
- The light-combo attack move sets `isStrafing = true` (L1657 / L1663).
- `DMIGrenadeCookController:602` calls `SetStrafeLocomotion(false)`.
- `DMMeleeBlockThreatFacing` auto-face (`meleeAttackAutoFaceHalfAngle` 22°).
- `DMLandingDirector:1346` picks the strafe landing state.
- `DMLocomotionLeanDriver` (`leanSpineRollWhileStrafing`).
- `DMDashController` (dash direction from the `rotateTarget` pivot).
- Roll (root motion, `isRolling`), jetpack (airborne, so strafe rotation is already ignored when not grounded), climb (`DMClimbController`).

---

## 2. Options

**A) Context mode.** Strafe while a weapon is drawn, aiming, locked on, or in combat (an enemy engaged within N m). Free while
exploring. *Pro:* the classic action-RPG feel and the smallest behaviour change. *Con:* it does **not** fix Anthony's
complaint while exploring unarmed. Melee 1H / 2H / Bow strafe trees have no run node (foot sliding at jog).

**B) Always strafe.** The body always faces camera yaw while moving, and turns in place when idle and the camera passes a threshold.
Free tree only for burst sprint. *Pro:* full 8-dir everywhere, shooter-like precision. *Con:* it loses the free
tree's turn-lean run and the cinematic "run toward the camera" shot. Forward running looks stiffer, and the 180° camera
spin always swings the body. It is a large feel change for exploration.

**C) Hybrid by input angle (RECOMMENDED, with A's combat context layered on top).**
Measure the input angle relative to camera forward.
- Inside the **forward cone** (default ±30°, so W only on KBM): use **Free** (turn-lean run, sprint).
- Outside it (W+A/W+D diagonals, A/D, S, S-diagonals): use **Strafe**. The body holds camera yaw and the 2D tree plays
  side-step, diagonal and backpedal clips.
- Hysteresis plus a minimum hold time stop flicker on analog sticks.
- **Combat context** (weapon drawn + enemy engaged, lock-on, ADS) widens the cone to 0°, so it is full strafe, like A.

*Why C:* in free forward the body already faces camera forward, so switching into strafe needs at most a
~30–45° body correction. The handoff is nearly seamless, which B and A can't both give. It keeps the exploration
feel, gives every direction, and costs no controller edits in phase 1.

---

## 3. Recommended architecture (C)

New runtime component: **`DMStrafeLocomotionDirector`** (Features/Locomotion/Runtime), added to `Player_v7 Variant`.
- It is the single owner of `cc.isStrafing` for non-aim, non-lock-on cases. It **assigns** the value and never calls the
  `Strafe()` toggle. It runs **after** `base.InputHandle()` in the same `Update`, so Invector's toggles earlier in the frame
  are overwritten before `FixedUpdate` reads the flag. Hook: one call at the end of `PioneerShooterMeleeInput.InputHandle()`
  (preferred), or from `DMLocomotionGaitController.TickLocomotion()` (end of `Update`, which avoids touching the WIP file).
- It retires `UpdateBackwardStrafeWithoutTurnaround()` (it becomes a no-op behind the director, then is deleted).
- **Yields (does not write) while:** ADS / mining aim, `lockInStrafe`, `isRolling`, dash active, `customAction`
  (except the light-combo move, which keeps its own strafe), climbing, jetpack active / not grounded, ragdolled, dead,
  or UI input locked.
- Desired mode = f(input angle, cone, hysteresis, combat context, gait). Burst sprint is allowed only inside the
  sprint cone (§4.1).
- Speed: it applies a directional multiplier (§4.3) through `DMLocomotionGaitController`, so speeds keep a single writer.
- Rotation: `strafeSpeed.rotateWithCamera` stays false outside ADS. When strafing with input, Invector rotates toward
  `rotateTarget.forward` (the camera). When idle, the body stays put (no idle camera chase).

Invector source files are not edited. All behaviour sits on DM scripts.

---

## 4. Behaviour details

### 4.1 Sprint (forward only)
- Burst sprint is allowed only when the input is within **±50°** of camera forward. Outside it the burst request is ignored
  (it stays jog-strafe), and burst ends if the input leaves the cone for more than 0.1 s.
- While bursting, `sprintOnlyFree` forces the free tree. The body turns toward the input, same as today.
- The strafe-tree Sprint node (a forward clip at 1.5) is never reached, because the director clamps strafe magnitude to run.

### 4.2 Turn-in-place
- Idle: no camera chase (current behaviour). Turn-on-spot clips stay in use for ADS / lock-on, where they already work.
- Starting a strafe or backpedal with the camera far off body yaw (e.g. pressing S while looking at his face): the body
  turns at `strafeStartTurnSpeed`. When `turnBeforeMoveAngle` (default 100°) is exceeded, translation is scaled down
  until yaw error is under 60°, so he doesn't moonwalk while spinning. Idle `RotationMagnitude` drives the existing
  in-place turn clips during that turn.

### 4.3 Foot sliding (code speed vs clip speed)
`useRootMotion = 0` stays. Root motion is not mixed with code (physics rule). Effective clip speed = averageSpeed × child timeScale:

| Unarmed | Walk clip m/s | vs slow walk 2.0 | Run clip m/s | vs jog 4.0 |
|---|---|---|---|---|
| Forward | 1.73 | ok | 3.49 | ok (free tree today) |
| Side | 1.67 | ok | 2.66 | −33% |
| Back | 1.67 | ok | 1.83 | **−54%** |
| Fwd-diag | 2.07 | ok | 4.25 | ok |
| Back-diag | 1.84 | ok | 3.18 | −20% |

Melee 1H walk ≈1.25–1.6, 2H ≈0.84–1.07, Bow ≈0.83–1.06, Shooter walk ≈1.4 / run ≈3.0–3.1.
Plan:
- **Directional speed multipliers** (gameplay-sensible and they hide the sliding): side 0.75, back 0.55, back-diagonal 0.7,
  forward-diagonal 1.0. This applies at jog. At slow walk the multipliers are blended toward 1 (`walkDirectionalBlend` 0.3).
- **Per-moveset strafe cap** for MoveSets 2 / 3 / 4 (no run node): clamp strafe `InputMagnitude` to 0.5 and the speed to
  `meleeStrafeMaxSpeed` 1.6 m/s, until phase 3 adds run nodes.
- Optional phase 3: an animator playback-speed parameter on Strafing Movement (`DM_StrafeAnimSpeed`), set as code speed ÷
  clip speed and clamped to 0.85–1.25. This is a controller edit done by an editor builder, so it is reversible.

### 4.4 Transitions between trees
- Use the existing `IsStrafing` sub-machine transitions (0.25 s). No controller change in phase 1.
- Flicker guards: hysteresis ±10°, `modeMinHoldSeconds` 0.15. Free → Strafe happens only when grounded and not mid-roll or mid-landing.
- Phase 3 option: shorten the Free↔Strafe exits to 0.18 s if the 0.25 s blend reads as mushy (builder-driven edit).

### 4.5 Camera
- No camera code change. `rotateTarget` stays the main camera, and PioneerPlayerInputBinder / Dash keep using that pivot.
- When backpedalling toward the camera, he walks into the lens. The existing follow-cam collision handles it. Watch the near clip in tight caves.
- Optional: `strafeCameraShoulderBias` 0 (off). Leave framing alone in phase 1.

### 4.6 Gamepad and input abstraction
- Input arrives from `PlayerController.MoveInput` (Input System, KBM + pad), with the keyboard fallback in `ReadMoveVector()`.
  `DMInputSchemeRouter` picks the scheme. The director reads `cc.input` after MoveInput, so both schemes go through one path.
- Pad: an analog angle and magnitude. Apply `padAngleDeadzone` (ignore angle below magnitude 0.2) and hysteresis.
  L3 auto-run latch: pulling back while latched means jog backpedal at the back multiplier. Pulling sideways means jog strafe.
- KBM: only 0 / 45 / 90 / 135 / 180°. With a 30° cone, W is free and W+A / W+D are strafe diagonals.
- Optional input: re-enable a **DM strafe-hold** binding (e.g. KBM Left Alt / a free pad button; binding TBD) that forces full strafe.
  Default off, and it is not the Invector toggle.

### 4.7 Interactions
- **Jetpack**: yield while active or not grounded. Air rotation keeps today's behaviour. On landing, `DMLandingDirector`
  already picks the strafe landing when `isStrafing`.
- **Climb / mantle**: yield.
- **Mining**: scan aim already strafes through the aim path, so yield.
- **Crouch**: works through Strafing Crouch (unarmed / melee 8-dir, shooter 4-dir). Crouch speed 2 vs the crouch strafe
  clip ≈1.0–1.6 m/s, so add `crouchStrafeSpeedMul` 0.75.
- **Roll / dodge / dash**: yield while active. Dash direction stays camera-pivot based. On exit, the director re-resolves next frame.
- **Melee**: attack / block auto-face and the light-combo strafe keep priority (yield during customAction / attack). Combat
  context widens to full strafe, so the body faces the camera between swings.
- **Grenade cook**: `SetStrafeLocomotion(false)` on release, then the director re-resolves next frame.
- **Lean driver**: the spine roll while strafing is controlled by `leanSpineRollWhileStrafing`. Check how it looks.

---

## 5. Studio knobs (new section "Locomotion: strafe / backpedal" in DM_ClimbDashProfile, Genesis Studio → Survival)

| Field | Default | Notes |
|---|---|---|
| `strafeMode` | Hybrid | Off / CombatContext / Hybrid / AlwaysStrafe (covers options A, B, C) |
| `hybridForwardConeHalfAngle` | 30° | ≤ cone uses the free tree |
| `hybridHysteresis` | 10° | |
| `modeMinHoldSeconds` | 0.15 | |
| `combatContextFullStrafe` | true | drawn weapon + engaged enemy → cone 0 |
| `combatContextEngageRadius` | 12 m | |
| `sprintForwardConeHalfAngle` | 50° | burst only inside |
| `sideSpeedMul` / `backSpeedMul` / `backDiagonalSpeedMul` / `forwardDiagonalSpeedMul` | 0.75 / 0.55 / 0.70 / 1.0 | jog |
| `walkDirectionalBlend` | 0.3 | 0 = no penalty at slow walk |
| `crouchStrafeSpeedMul` | 0.75 | |
| `meleeStrafeMaxSpeed` | 1.6 m/s | MoveSets 2–4 until run nodes exist |
| `strafeStartTurnSpeed` | 10 | matches the current rotationSpeed |
| `turnBeforeMoveAngle` | 100° | translation scaled while the yaw error is large |
| `padAngleDeadzoneMagnitude` | 0.2 | |
| `strafeHoldInputEnabled` | false | |
| `debugStrafeOverlay` | false | on-screen mode / angle / yaw error |

---

## 6. Phases

- **P0, 5 min, no code (Anthony in Play):** hold S and watch Animator `IsStrafing` and the Free/Strafe sub-machine.
  Expected: false (or flicker), which confirms §1 cause 2.
- **P1, director (Hybrid, unarmed + shooter):**
  - Add `DMStrafeLocomotionDirector` + profile fields + Studio section and the one-line hook.
  - Retire the backpedal helper.
  - Add the sprint cone gate in `DMLocomotionGaitController.CanBurstSprint`.
  - Validate: Refresh with 0 errors / 0 warnings. Back up to `.pre<date>` in `rc_deploy\…\files\` and log both logs.
- **P2, speed matching and edge cases:** directional multipliers, moveset caps, turn-before-move, crouch, pad hysteresis.
  Then a Play pass over jetpack, climb, roll, dash, mining, grenade, melee combo, lock-on and landing.
- **P3, optional animator polish (builder-driven, reversible):**
  - Add run nodes to the Melee 1H / 2H / Bow strafe trees (reuse unarmed 8-dir run or shooter run clips).
  - Add `DM_StrafeAnimSpeed`.
  - Tune the Free↔Strafe exit duration.
- **P4:** the CombatContext tuning pass with Slice 2b hit reactions (player facing during hits) (INDEX row already added).

## 7. Risks

- **Invector overrides.** `vShooterMeleeInput` toggles `Strafe()` every frame. The director must run last in `Update` and
  assign, not toggle. Any future Invector update or new caller of `Strafe()` / `SetStrafeLocomotion` can fight it, so log
  writes when debugging.
- **Player physics rule.** No capsule, layer, rigidbody or physics edits. Speeds change at runtime through the existing gait path only.
  No root motion is added, and no NavMesh or motion matching.
- **Anthony's WIP** in `PioneerShooterMeleeInput.cs`. The hook needs a one-line edit there, coordinated with him. The fallback
  is the gait-controller hook.
- **Foot sliding** on MoveSets 2–4 and on the unarmed jog backpedal until P2 / P3.
- **Feel risk.** Hybrid diagonals hold camera yaw, so W+D no longer arcs the body. That is mitigated by the cone knob
  (raise it to 50° to keep diagonals free).
- **Shooter 4-dir trees** have no diagonals. Freeform blending of F + R reads acceptably but less crisply.

## 8. Decisions (DECIDED 2026-10-19 by Anthony: all defaults)

1. **Mode:** Hybrid C, with full strafe in combat context. Forward zone **±30°** (W = free; W+A / W+D = strafe diagonals).
2. **Backpedal:** jog backpedal at **0.55×** (≈2.2 m/s).
3. **Sprint:** **forward-only**, burst allowed within **±50°** of camera forward.
4. **Melee / Bow strafe:** **capped at walk** (1.6 m/s, magnitude 0.5) until P3 adds run nodes.

The Studio defaults in §5 already match these. Resume: P0 check, then P1.

Related: the combat jitter investigation (2026-10-19) found that the `IsStrafing` toggle fight (§1 cause 2) also feeds
melee facing jitter. P1's single-owner director is part of that fix (see the jitter report delivered with the player hit-reaction plan).

---

Files expected to change when implemented (not now):
- new `Features/Locomotion/Runtime/DMStrafeLocomotionDirector.cs`
- `DM_ClimbDashProfile.cs` + `.asset`
- `DMLocomotionGaitController.cs`
- `PioneerShooterMeleeInput.cs` (hook + helper retire)
- the Studio section filter / window
- `Player_v7 Variant.prefab` (add the component)
- P3 only: `Invector@ShooterMelee_Jetpack.controller`, through a DM editor builder

Documentation INDEX: row added 2026-10-19 (Player / climb / dash / jetpack §2.2).

## Note 2026-10-19 d (facing fix landed)

The light combo no longer forces isStrafing (DM_CombatCoreProfile.lightComboForcesStrafe = false) and attack facing is owned by DMMeleeAttackFacing (combo target lock). Re-check both when this plan resumes.
