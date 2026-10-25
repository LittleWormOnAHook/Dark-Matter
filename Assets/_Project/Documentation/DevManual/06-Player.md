# Player — Genesis Studio

**Status:** Stub (generated tables; notes pending)  
**Open:** Tools → Dark Matter Genesis → Genesis Studio → **Player**  
Player/enemy prefab authoring, climb, dash, jetpack, jump/landing, footsteps, camera, and live player wiring.

## Tabs

- [Player Prefab](#player-prefab)
- [Enemy Prefab](#enemy-prefab)
- [Definitions](#definitions)
- [Climb](#climb)
- [Dash](#dash)
- [Jetpack](#jetpack)
- [Jump/Landing](#jumplanding)
- [Pickup Items](#pickup-items)
- [Footsteps](#footsteps)
- [Camera](#camera)
- [Player Systems](#player-systems)

## Player Prefab

**What it does:** Clone Player_Invector, paste a Humanoid Meshy FBX, and save a PlayerVisualDefinition.

**Where:**
- Studio: `Genesis Studio / Player / Player Prefab` (panel mode `CharacterCreatorCombined`)

## Enemy Prefab

**What it does:** Clone HumanoidEnemy_Invector, paste mesh, then tune combat, AI, loot, health, and senses.

**Where:**
- Studio: `Genesis Studio / Player / Enemy Prefab` (panel mode `CharacterCreatorCombined`)

## Definitions

**What it does:** Inspect saved PlayerVisualDefinition and EnemyDefinition assets. Custom starts an unsaved working copy.

**Where:**
- Studio: `Genesis Studio / Player / Definitions` (panel mode `CharacterCreatorCombined`)

## Climb

**What it does:** Wall attach, mantle, climb/sprint stamina, surface layers, and ClingSense.

**Where:**
- Studio: `Genesis Studio / Player / Climb` (panel mode `SingletonAsset`)
- Asset: `Assets/_Project/Resources/Climb/DM_ClimbDashProfile.asset`
- Script: `Assets/_Project/Features/Climb/Runtime/DM_ClimbDashProfile.cs`

**Key settings** (from code):

| Group | Field | Type | Default | Range | Notes |
|---|---|---|---|---|---|
| Slope cutoffs (degrees from up) | `walkMaxSlopeDeg` | float | 75f | 20f–89f | At or under this, the player walks/runs even if the mesh is Climbable. Matches Invector slopeLimit (75). |
| Slope cutoffs (degrees from up) | `climbMinSlopeDeg` | float | 75f | 20f–89f | Must be steeper than walkMax to climb. Usually the same number. |
| Slope cutoffs (degrees from up) | `climbMaxSlopeDeg` | float | 115f | 90f–170f | Past this, the face is too inverted to cling. |
| Raycast distances | `attachRange` | float | 1.4f | 0.4f–4f | Chest ray / spherecast that grabs a wall you are facing. |
| Raycast distances | `climbJumpRange` | float | 3f | 0.8f–8f | How far a climb-jump can reach another Climbable. |
| Raycast distances | `probeRadius` | float | 0.18f | 0.05f–0.5f |  |
| Raycast distances | `faceDotMin` | float | 0.15f | 0.15f–1f | Must face the wall at least this much (1 = dead-on). |
| Attach | `standOff` | float | 0.35f | 0.1f–1.2f |  |
| Attach | `startClimbNeedsAirborne` | bool | true |  | Start climb only after a jump (second Space). First Space is the jump. |
| Attach | `startClimbNeedsForward` | bool | true |  | Start climb only while holding forward (W). |
| Hand grab | `handPalmOffset` | float | 0.11f | 0.02f–0.25f | How far the wrist sits off the wall so fingers rest on the surface instead of through it. |
| Hand grab | `handSpread` | float | 0.18f | 0.08f–0.4f | Left/right hand spacing on the wall. |
| Hand grab | `autoGrabInAir` | bool |  |  | Grab when airborne and a climbable is in attachRange. Off â€” start climb is second Space + forward. |
| Motion | `moveSpeed` | float | 2.6f | 0.2f–6f |  |
| Motion | `climbShiftMul` | float | 1.35f | 1f–2.5f | Hold Shift while clinging to multiply crawl speed. |
| Motion | `climbInputDamp` | float | 0.1f | 0.01f–0.5f |  |
| Release / drop-to-hang | `jumpReleases` | bool | true |  | Jump while climbing or hanging always lets go. |
| Release / drop-to-hang | `releasePush` | float | 1.6f | 0f–6f | How hard jump-release pushes you off the wall. |
| Release / drop-to-hang | `climbLeapSpeed` | float | 1.45f | 2f–12f | Short climb-leap speed (m/s) along the climb direction. |
| Release / drop-to-hang | `clingHop` | float | 0.72f | 0.5f–12f | Space cling hop distance (meters). ~1 body-width; hold Space only slight stretch. |
| Release / drop-to-hang | `climbLeapRegrab` | float | 0.55f | 0.12f–1.2f | After one Space leap, WASD toward a Climbable snaps back. Seconds. |
| Release / drop-to-hang | `dropPush` | float | 2.4f | 0.5f–6f | E-drop shove away from the wall (m/s). |
| Release / drop-to-hang | `detachBuffer` | float | 1.15f | 0.4f–3f | Must leave the wall this far before a NEW start-climb can stick. Leap regrab ignores this. |
| Release / drop-to-hang | `highFallGrabMeters` | float | 6f | 3f–20f | Unused auto-cling distance. Start climb is second Space + forward. |
| Release / drop-to-hang | `airControlSeconds` | float | 0.95f | 0.2f–2f | Air steer time after an E-drop or hop. |
| Release / drop-to-hang | `dropToHang` | bool | true |  | From a walkable ledge top: walk forward into the void, hold S toward the drop, or press E near the lip â€” reverse-mantles into hang (180 to face the wall). Standing still at the edge does not trigger. |
| Release / drop-to-hang | `dropToHangRange` | float | 1.8f | 0.4f–4f | How far below the lip we look for a climbable face when drop-to-hang / reverse mantle starts. |
| Overhang lip | `enableOverhangGrab` | bool | true |  | When climb-up stops under a soffit or rock shelf, climb onto the nearest lip. |
| Overhang lip | `overhangReachUp` | float | 1.15f | 0.4f–2.2f | How far above the hands to look for a soffit or rock shelf. |
| Overhang lip | `overhangReachBack` | float | 1.4f | 0.4f–2.2f | How far back from the wall the nearest lip may be (slabs and cliff shelves). |
| Overhang lip | `overhangGrabSeconds` | float | 0.78f | 0.2f–1.4f | Minimum seconds for the climb-up onto the lip. Longer grabs use climb speed. |
| Overhang lip | `overhangMinProbeOut` | float | 0.05f | 0.02f–0.25f | Outward shelf probe start (meters from climb wall). Lower catches thin/short lips. |
| Overhang lip | `overhangDeepProtrusion` | float | 0.65f | 0.25f–1.6f | Planar lip distance from the climb wall that counts as deep protrusion. Drives short-hop grab â€” not vertical thickness. |
| Overhang lip | `overhangShortHopPull` | float | 0.94f | 0.35f–0.98f | Deep lips: fraction of planar travel done as a short hop toward the lip. Higher = less arm stretch. |
| Overhang lip | `overhangShortHopArc` | float | 0.22f | 0f–0.45f | Extra hop arc height (meters) on deep short-hop lip grabs. |
| Overhang lip | `overhangIkBlendStart` | float | 0.72f | 0f–0.9f | Deep grab: IK weight ramps in after this fraction of the hop. Higher delays hand IK until the body is closer. |
| Overhang lip | `overhangHangInset` | float | 0.18f | 0.05f–0.4f | Body hang inset from the lip along the wall normal. |
| Overhang lip | `overhangShortHopSeconds` | float | 0.85f | 0.12f–1.25f | Seconds for a deep short-hop lip grab. Deep hop uses this only (no speedDur shorten). |
| Mantle | `enableMantle` | bool | true |  |  |
| Mantle | `mantleRequiresOpenLip` | bool | true |  | W only starts a mantle when there is no climbable wall left above the hands. |
| Mantle | `handHeight` | float | 1.18f | 0.9f–1.6f | Hand / lip height from the feet. Wall-above probes start at the chest under this. |
| Mantle | `mantleForward` | float | 0.42f | -0.5f–1.2f | Meters onto the top from the lip hit. 0 plants on the lip. Negative pulls back toward the wall. |
| Mantle | `mantleProbeUp` | float | 1.5f | 0.8f–2.2f | How high above the hang the down-probe starts. |
| Mantle | `mantleProbeDown` | float | 1.7f | 0.6f–2.4f | How far the lip/floor probe searches down. |
| Mantle | `mantlePlantHeight` | float | 0.04f | -0.2f–0.2f | 0 = flush with the found top. Positive lifts the plant, negative sinks feet into the ledge. |
| Mantle | `mantleStandPad` | float | 0.04f | -0.2f–0.2f | Legacy alias. Ignored; plant uses mantlePlantHeight including 0 and negatives. |
| Mantle | `mantleSeconds` | float | 1.4f | 0.5f–2.4f | Seconds to lerp hang â†’ lip â†’ stand. |
| Mantle | `mantleIgnoreLands` | float | 2.6f | 0.5f–4f | Ignore fall-land / ragdoll this long after a mantle. |
| Landing | `heroDropMeters` | float | 2.6f | 0.5f–8f | Jump/Landing: drops shorter than this use short bounce (LandLow). From this height up to the damage band uses hero LandHigh; higher still hero + damage (jetpack grace on lethal). |
| Landing | `lethalDropMeters` | float | 100f | 8f–200f | Fall this far or more (while falling) is death ragdoll + retry, unless jetpack grace applies. |
| Landing | `fallDamageStartMeters` | float | 40f | 8f–80f | No fall damage below this height. Damage starts at the greater of this and (lethal minus fallDamageLethalPercent). |
| Landing | `fallDamageLethalPercent` | float | 0.3f | 0.1f–0.6f | Damage starts this fraction below lethal (0.3 = 30% less than lethal). Combined with fallDamageStartMeters via max(). |
| Landing | `fallDamageHealthFraction` | float | 0.5f | 0.1f–1f | Health fraction removed when landing between the no-damage floor and lethal height. |
| Landing | `jetpackLethalDelay` | float | 6f | 0f–20f | After jetpack Space/boost is released, wait this many seconds before a lethal-height fall can kill. Still boosting, or land inside this window, is always hero. |
| Locomotion â€” walk / run / sprint | `slowWalkSpeedMultiplier` | float | 0.5f | 0.25f–1f | Default slow walk = Invector runningSpeed Ã— this (0.5 = half prior jog speed). |
| Locomotion â€” walk / run / sprint | `jogSpeedMultiplier` | float | 1f | 0.5f–1.5f | Shift hold jog = Invector runningSpeed Ã— this. |
| Locomotion â€” walk / run / sprint | `sprintBurstSpeedMultiplier` | float | 1.2f | ≥1f | Double-tap Shift burst = Invector sprintSpeed Ã— this (+20% = 1.2). Stamina drain scales by the same factor. |
| Locomotion â€” walk / run / sprint | `shiftDoubleTapWindow` | float | 0.28f | ≥0.05f | Max seconds between Shift taps to arm the sprint burst. |
| Locomotion - strafe / backpedal | `strafeLocoMode` | DMStrafeLocomotionMode | DMStrafeLocomotionMode.Hybrid |  | Off = legacy free locomotion. Hybrid = free inside the forward cone, strafe outside (side steps / diagonals / backpedal). CombatContext = strafe only with a drawn weapon near enemies. AlwaysStrafe = always. |
| Locomotion - strafe / backpedal | `strafeLocoForwardConeHalfAngle` | float | 67f | 0f–90f | Hybrid: input within this angle of camera forward uses the free tree (turn-lean run). 67 = W, W+A, W+D on keyboard; A/D/S strafe. |
| Locomotion - strafe / backpedal | `strafeLocoHysteresis` | float | 10f | 0f–30f | Degrees of hysteresis around the forward cone (stops analog flicker). |
| Locomotion - strafe / backpedal | `strafeLocoMinHoldSeconds` | float | 0.15f | 0f–1f | Minimum seconds between free/strafe switches. |
| Locomotion - strafe / backpedal | `strafeLocoCombatFullStrafe` | bool | true |  | Weapon drawn + enemy within the engage radius = combat context (holds camera yaw while idle; see Combat Ignores Forward Cone). |
| Locomotion - strafe / backpedal | `strafeLocoCombatIgnoresForwardCone` | bool |  |  | 2026-10-20e: ON = legacy full strafe in combat (W also strafes, slower). OFF = combat keeps the forward free cone (W / W+A / W+D free at normal speed). |
| Locomotion - strafe / backpedal | `strafeLocoCombatEngageRadius` | float | 12f | ≥0f | Enemy distance (m) that counts as engaged for combat context. |
| Locomotion - strafe / backpedal | `strafeLocoSprintConeHalfAngle` | float | 50f | 0f–180f | Burst sprint (double-tap Shift) only arms when input is within this angle of camera forward. |
| Locomotion - strafe / backpedal | `strafeLocoPadDeadzoneMagnitude` | float | 0.2f | 0f–0.9f | Stick magnitude below which the input angle is ignored (pad). |
| Locomotion - strafe / backpedal | `strafeLocoDebugOverlay` | bool |  |  | On-screen label: director mode / yield reason / input angle. |
| Body weight (ground secondary motion, 2026-10-20l/n) | `bodyWeightEnabled` | bool |  |  | Master toggle: grounded spine/chest lag, lean, stop carry, arm trailing, landing hip drop (like the jetpack limb dangle). Visual only. |
| Body weight (ground secondary motion, 2026-10-20l/n) | `bodyWeightPreset` | BodyWeightPreset | BodyWeightPreset.Custom |  | Intensity preset (2026-10-22b, all based on the fair Custom snapshot). Custom = your values. Subtle x0.65, Visible x1.2, Extreme x6 (stress test). Only magnitudes scale; timings, springs, deadzones and ratios stay. |
| Body weight (ground secondary motion, 2026-10-20l/n) | `bodyWeightIntensity` | float | 1f | 0f–10f | Global multiplier on every body-weight offset (rotations, hip drop, CoM shift, swing). 1 = authored values. Only NaN/explosion guards clamp the result. |
| Body weight (ground secondary motion, 2026-10-20l/n) | `bodyWeightDebugExaggerate` | float | 1f | 1f–10f | Verification multiplier stacked on top of intensity (e.g. 4). Set back to 1 when done. |
| Body weight (ground secondary motion, 2026-10-20l/n) | `bodyWeightDebugReadout` | bool |  |  | On-screen live readout: attached / enabled / weight / gates / offsets. |
| Body weight (ground secondary motion, 2026-10-20l/n) | `bodyWeightAimScale` | float | 0.15f | 0f–1f | Effect scale while aiming (keeps aim IK clean). 0 = off while aiming. |
| Body weight (ground secondary motion, 2026-10-20l/n) | `bodyWeightAccelSmooth` | float | 0.08f | 0.01f–0.5f | Smoothing (s) of the measured body acceleration / turn rate. |
| Body weight (ground secondary motion, 2026-10-20l/n) | `bodyWeightIdleSpeedDeadzone` | float | 0.2f | 0f–1f | 2026-10-21a: planar speed (m/s) below which body velocity counts as zero when there is no move input (idle contact noise). |
| Body weight (ground secondary motion, 2026-10-20l/n) | `bodyWeightYawRateDeadzone` | float | 20f | 0f–90f | 2026-10-21a: turn rate (deg/s) below which yaw counts as zero (idle / tiny in-place turns). |
| Body weight (ground secondary motion, 2026-10-20l/n) | `bodyWeightAccelDeadzone` | float | 1.5f | 0f–10f | 2026-10-21a: acceleration (m/s^2) below which the lean / arm-trail input is zeroed. |
| Body weight (ground secondary motion, 2026-10-20l/n) | `bodyWeightLeanAccel` | float | 0.6f | 0f–10f | Spine pitch (deg per m/s^2) leaning into forward acceleration. |
| Body weight (ground secondary motion, 2026-10-20l/n) | `bodyWeightStopCarry` | float | 0.6f | 0f–10f | Spine pitch (deg per m/s^2) carried forward when braking / stopping. |
| Body weight (ground secondary motion, 2026-10-20l/n) | `bodyWeightLeanTurn` | float | 0.9f | 0f–10f | Spine roll (deg per m/s^2) leaning into lateral acceleration / turns. |
| Body weight (ground secondary motion, 2026-10-20l/n) | `bodyWeightYawLag` | float | 4f | 0f–30f | Spine/chest yaw lag behind body turns (deg per 100 deg/s). |
| Body weight (ground secondary motion, 2026-10-20l/n) | `bodyWeightMaxSpineDegrees` | float | 14f | 0f–90f | Clamp for spine + chest offsets before intensity (deg). |
| Body weight (ground secondary motion, 2026-10-20l/n) | `bodyWeightSpineSmooth` | float | 0.2f | 0.02f–1f | Spine spring smoothing (s). Higher = heavier, slower. |
| Body weight (ground secondary motion, 2026-10-20l/n) | `bodyWeightSettleDamping` | float | 0.4f | 0.1f–1.2f | Trunk settle spring damping ratio (1 = no overshoot; lower = sway past and settle after stops). |
| Body weight (ground secondary motion, 2026-10-20l/n) | `bodyWeightChestLag` | float | 0.12f | 0f–0.6f | Chest phase lag behind spine/hips (s). Breaks up the rigid-block torso on turns. |
| Body weight (ground secondary motion, 2026-10-20l/n) | `bodyWeightArmTrail` | float | 1.3f | 0f–10f | Arm trailing strength (deg per m/s^2), like the jetpack dangle inertia. |
| Body weight (ground secondary motion, 2026-10-20l/n) | `bodyWeightMaxArmDegrees` | float | 18f | 0f–90f | Clamp for arm offsets before intensity (deg). |
| Body weight (ground secondary motion, 2026-10-20l/n) | `bodyWeightArmSmooth` | float | 0.15f | 0.02f–1f | Arm spring smoothing (s). Elbows/wrists use progressively more lag. |
| Body weight (ground secondary motion, 2026-10-20l/n) | `bodyWeightElbowTrail` | float | 1.5f | 0f–4f | Elbow (forearm) whip relative to the upper arm. |
| Body weight (ground secondary motion, 2026-10-20l/n) | `bodyWeightWristTrail` | float | 2.3f | 0f–4f | Wrist (hand) whip relative to the upper arm. |
| Body weight (ground secondary motion, 2026-10-20l/n) | `bodyWeightLandingDrop` | float | 0.08f | 0f–1f | Hip drop impulse on landing, scaled by fall speed. |
| Body weight (ground secondary motion, 2026-10-20l/n) | `bodyWeightLandingRecovery` | float | 0.4f | 0.05f–1f | Landing recovery speed (fraction of hip spring used while rising back up). Lower = slower recovery. |
| Body weight (ground secondary motion, 2026-10-20l/n) | `bodyWeightDirChangeDrop` | float | 0.03f | 0f–1f | Hip dip per sharp direction change. |
| Body weight (ground secondary motion, 2026-10-20l/n) | `bodyWeightDirChangeAccel` | float | 8f | 1f–30f | Acceleration (m/s^2) that counts as a sharp direction change. |
| Body weight (ground secondary motion, 2026-10-20l/n) | `bodyWeightHipSpring` | float | 90f | 10f–400f | Hip spring stiffness (compression side). |
| Body weight (ground secondary motion, 2026-10-20l/n) | `bodyWeightMaxHipDrop` | float | 0.1f | 0f–0.6f | Max hip drop before intensity (m). |
| Body weight: regions (biomechanics) | `bodyWeightPelvisLead` | float | 3f | 0f–20f | Pelvis: leads body turns (deg per 100 deg/s of turn). |
| Body weight: regions (biomechanics) | `bodyWeightPelvisSmooth` | float | 0.12f | 0.02f–1f | Pelvis / CoM spring smoothing (s). |
| Body weight: regions (biomechanics) | `bodyWeightCounterRotation` | float | 0.8f | 0f–3f | Shoulder-hip counter-rotation: chest turns against the pelvis lead (0-1 of pelvis yaw). |
| Body weight: regions (biomechanics) | `bodyWeightComShift` | float | 0.012f | 0f–0.1f | Centre-of-mass / weight shift: hips slide toward lateral acceleration (m per m/s^2). |
| Body weight: regions (biomechanics) | `bodyWeightMaxComShift` | float | 0.05f | 0f–0.4f | Max CoM shift before intensity (m). |
| Body weight: regions (biomechanics) | `bodyWeightAnticipation` | float | 4f | 0f–30f | Anticipation: lean toward the input direction before velocity catches up (deg). |
| Body weight: regions (biomechanics) | `bodyWeightDecelCompression` | float | 0.02f | 0f–0.3f | Knee/hip compression when braking (feet stay planted by foot IK). |
| Body weight: regions (biomechanics) | `bodyWeightHeadStabilize` | float | 0.5f | 0f–1f | Head stabilisation: neck+head cancel this share of trunk offsets (gaze stays level). 0-1. |
| Body weight: regions (biomechanics) | `bodyWeightHeadSmooth` | float | 0.12f | 0.02f–0.6f | Neck/head spring smoothing (s). |
| Body weight: melee swing spring | `bodyWeightSwingAmount` | float | 1f | 0f–10f | Torso/arm/pelvis follow-through from the swinging hand's angular velocity (deg per 100 deg/s). Visual only; hitboxes/timing unchanged. 0 = off. |
| Body weight: melee swing spring | `bodyWeightSwingStiffness` | float | 60f | 10f–400f | Swing spring stiffness. Higher = snappier settle. |
| Body weight: melee swing spring | `bodyWeightSwingDamping` | float | 0.5f | 0.1f–1.2f | Swing spring damping ratio (1 = no overshoot, lower = more overshoot). |
| Body weight: melee swing spring | `bodyWeightSwingMaxDegrees` | float | 12f | 0f–90f | Clamp for swing offsets before intensity (deg). |
| Body weight: weapon weight (2026-10-20p, visual only; scaled by preset/intensity) | `bodyWeightWeaponEnabled` | bool | true |  | Master toggle for melee windup / follow-through / impact jolt, ranged recoil absorption and draw/holster weight. |
| Body weight: weapon weight (2026-10-20p, visual only; scaled by preset/intensity) | `bodyWeightWeaponUnarmed` | float | 0.6f | 0f–5f | Weight factor: unarmed melee. |
| Body weight: weapon weight (2026-10-20p, visual only; scaled by preset/intensity) | `bodyWeightWeaponMelee` | float | 1f | 0f–5f | Weight factor: melee weapon in hand. |
| Body weight: weapon weight (2026-10-20p, visual only; scaled by preset/intensity) | `bodyWeightWeaponHeavyMult` | float | 1.6f | 1f–5f | Extra multiplier during strong / charged swings. |
| Body weight: weapon weight (2026-10-20p, visual only; scaled by preset/intensity) | `bodyWeightWeaponRanged` | float | 1f | 0f–5f | Weight factor: ranged weapon in hand. |
| Body weight: weapon weight (2026-10-20p, visual only; scaled by preset/intensity) | `bodyWeightWeaponSwingGain` | float | 4f | 0f–20f | Follow-through gain on the swing spring (x Swing Amount). Round 1 was effectively 1. |
| Body weight: weapon weight (2026-10-20p, visual only; scaled by preset/intensity) | `bodyWeightWeaponWindup` | float | 3f | 0f–20f | Windup anticipation: torso counter-twists against the hand's angular acceleration at swing start (deg per 1000 deg/s^2). |
| Body weight: weapon weight (2026-10-20p, visual only; scaled by preset/intensity) | `bodyWeightWeaponHipPull` | float | 0.6f | 0f–3f | Share of the swing that pulls the hips round (pelvis yaw). |
| Body weight: weapon weight (2026-10-20p, visual only; scaled by preset/intensity) | `bodyWeightWeaponImpactJolt` | float | 8f | 0f–60f | Impact jolt on melee contact (hit-stop start): torso bounce back (deg). |
| Body weight: weapon weight (2026-10-20p, visual only; scaled by preset/intensity) | `bodyWeightWeaponImpactHipDrop` | float | 0.03f | 0f–0.3f | Impact hip dip on melee contact (m). |
| Body weight: weapon weight (2026-10-20p, visual only; scaled by preset/intensity) | `bodyWeightWeaponRecoilKick` | float | 4f | 0f–40f | Ranged: per-shot spine kick (deg). Chest counter-rotates so the shoulders / aim stay on target. |
| Body weight: weapon weight (2026-10-20p, visual only; scaled by preset/intensity) | `bodyWeightWeaponRecoilHipPush` | float | 0.02f | 0f–0.2f | Ranged: per-shot hip push back (m). |
| Body weight: weapon weight (2026-10-20p, visual only; scaled by preset/intensity) | `bodyWeightWeaponRecoilStiffness` | float | 180f | 20f–600f | Recoil / draw spring stiffness. |
| Body weight: weapon weight (2026-10-20p, visual only; scaled by preset/intensity) | `bodyWeightWeaponRecoilDamping` | float | 0.45f | 0.1f–1.2f | Recoil / draw spring damping ratio (lower = more settle wobble). |
| Body weight: weapon weight (2026-10-20p, visual only; scaled by preset/intensity) | `bodyWeightWeaponDrawDip` | float | 6f | 0f–45f | Draw / holster: forward torso dip + right arm sag (deg). |
| Body weight: weapon weight (2026-10-20p, visual only; scaled by preset/intensity) | `bodyWeightWeaponDrawHipDrop` | float | 0.025f | 0f–0.3f | Draw / holster hip drop (m). |
| Body weight: hit reactions (2026-10-20w, visual only) | `bodyWeightHitEnabled` | bool | true |  | Player takes damage: directional torso impulse (bend away), hip shove, head whip, spring recovery. |
| Body weight: hit reactions (2026-10-20w, visual only) | `bodyWeightHitImpulse` | float | 7f | 0f–60f | Peak torso bend away from the hit (deg) at reference damage. |
| Body weight: hit reactions (2026-10-20w, visual only) | `bodyWeightHitHipShove` | float | 0.025f | 0f–0.3f | Hip shove in the push direction (m) at reference damage. |
| Body weight: hit reactions (2026-10-20w, visual only) | `bodyWeightHitHeadWhip` | float | 5f | 0f–60f | Head whip (deg) at reference damage; lags the torso and settles. |
| Body weight: hit reactions (2026-10-20w, visual only) | `bodyWeightHitRecovery` | float | 110f | 30f–600f | Recovery spring stiffness (higher = snappier return). |
| Body weight: hit reactions (2026-10-20w, visual only) | `bodyWeightHitDamping` | float | 0.5f | 0.15f–1.5f | Recovery damping ratio (lower = more wobble). |
| Body weight: hit reactions (2026-10-20w, visual only) | `bodyWeightHitDamageRef` | float | 20f | 1f–300f | Damage that gives scale 1. |
| Body weight: hit reactions (2026-10-20w, visual only) | `bodyWeightHitPoiseWeight` | float | 0.5f | 0f–5f | Poise damage counted as extra damage (x this). |
| Body weight: hit reactions (2026-10-20w, visual only) | `bodyWeightHitMinScale` | float | 0.35f | 0f–2f | Smallest / largest hit scale. |
| Body weight: hit reactions (2026-10-20w, visual only) | `bodyWeightHitMaxScale` | float | 2f | 0.1f–6f |  |
| Body weight: foot planting (2026-10-20w, no NavMesh) | `bodyWeightFootPlantEnabled` | bool | true |  | Pins planted feet during stops / slow turns with a two-bone leg IK (capped), lowers the hips when the plant is out of reach. Off while climbing/hanging/airborne. |
| Body weight: foot planting (2026-10-20w, no NavMesh) | `bodyWeightFootPlantMaxSpeed` | float | 1.5f | 0f–10f | Planting only below this body speed (m/s). |
| Body weight: foot planting (2026-10-20w, no NavMesh) | `bodyWeightFootPlantHeight` | float | 0.06f | 0.01f–0.5f | Foot counts as planted within this height above its lowest pose (m). |
| Body weight: foot planting (2026-10-20w, no NavMesh) | `bodyWeightFootPlantMaxCorrection` | float | 0.1f | 0f–0.5f | Max distance a foot is held away from the animation before it releases (m). |
| Body weight: foot planting (2026-10-20w, no NavMesh) | `bodyWeightFootPlantBlend` | float | 0.1f | 0.01f–1f | Plant blend in/out time (s). |
| Body weight: foot planting (2026-10-20w, no NavMesh) | `bodyWeightFootPlantHipLower` | float | 0.03f | 0f–0.3f | Max hip lowering so an out-of-reach planted foot still lands (m). |
| Body weight: foot planting (2026-10-20w, no NavMesh) | `bodyWeightMaxDt` | float | 0.05f | 0.01f–0.2f | 2026-10-24h: max frame time fed to the body-weight springs (s); longer hitches are clamped (springs are sub-stepped). |
| Footsteps: trigger fit (2026-10-24h, runtime only, prefab untouched) | `bodyWeightFootstepTriggerFit` | bool | true |  | Moves the Invector foot-step trigger spheres from the ankle down to the sole at runtime (Kade/mixamo ankles sit ~9-10 cm above the sole, so the 10 cm spheres barely touched the ground). |
| Footsteps: trigger fit (2026-10-24h, runtime only, prefab untouched) | `bodyWeightFootstepTriggerSoleReach` | float | 0.03f | 0f–0.3f | How far the trigger sphere reaches below the measured sole (m). |
| Footsteps: trigger fit (2026-10-24h, runtime only, prefab untouched) | `bodyWeightFootstepTriggerRadius` | float | 0.08f | 0.02f–0.5f | Trigger sphere radius after fit (m). |
| Footsteps: trigger fit (2026-10-24h, runtime only, prefab untouched) | `bodyWeightFootstepSoleOffset` | float | 0f | -0.2f–0.2f | Extra sole height correction (m, + = sole is lower than the toe-bone estimate). |
| Wall hand touch (2026-10-24o, runtime only, prefab untouched) | `bodyWeightWallHandEnabled` | bool | true |  | Free wall-side hand touches flat, near-vertical walls while walking/jogging (not sprint/aim/attack/reload/dodge/jetpack/climb/actions). |
| Wall hand touch (2026-10-24o, runtime only, prefab untouched) | `bodyWeightWallHandLeft` | bool | true |  |  |
| Wall hand touch (2026-10-24o, runtime only, prefab untouched) | `bodyWeightWallHandRight` | bool | true |  |  |
| Wall hand touch (2026-10-24o, runtime only, prefab untouched) | `bodyWeightWallHandReach` | float | 0.7f | 0.2f–1.5f | Max shoulder-to-wall distance (m). |
| Wall hand touch (2026-10-24o, runtime only, prefab untouched) | `bodyWeightWallHandHeight` | float | 0f | -0.6f–0.6f | Hand height relative to the shoulder (m). |
| Wall hand touch (2026-10-24o, runtime only, prefab untouched) | `bodyWeightWallHandForwardOffset` | float | 0.25f | -0.3f–0.9f | How far ahead of the shoulder the hand lands, along the wall (m). |
| Wall hand touch (2026-10-24o, runtime only, prefab untouched) | `bodyWeightWallHandBlendTime` | float | 0.25f | 0.05f–1.5f | Blend in/out time (s). |
| Wall hand touch (2026-10-24o, runtime only, prefab untouched) | `bodyWeightWallHandMaxAngleDeg` | float | 40f | 5f–85f | Max wall angle away from straight sideways (deg). Small = only walls you walk parallel to. |
| Wall hand touch (2026-10-24o, runtime only, prefab untouched) | `bodyWeightWallHandMaxTiltDeg` | float | 15f | 0f–45f | Max wall tilt from vertical (deg). |
| Wall hand touch (2026-10-24o, runtime only, prefab untouched) | `bodyWeightWallHandFlatnessDeg` | float | 8f | 1f–45f | Max normal disagreement between probe rays (deg). Lower = flatter walls only. |
| Wall hand touch (2026-10-24o, runtime only, prefab untouched) | `bodyWeightWallHandMinWidth` | float | 0.5f | 0.1f–2f | Minimum wall width along the walk direction (m). |
| Wall hand touch (2026-10-24o, runtime only, prefab untouched) | `bodyWeightWallHandLayers` | LayerMask | (1 << 0) \| (1 << 23) |  | Wall layers the hand may touch (2026-10-24q default: Default + Climbable). A hit matching EITHER these layers OR the tag list is accepted. |
| Wall hand touch (2026-10-24o, runtime only, prefab untouched) | `bodyWeightWallHandTags` | string[] | { "Climbable" } |  | Wall tag allow-list (any layer). Hit accepted if its collider (or root) has one of these tags. Unknown/empty tags are ignored. |
| Wall hand touch (2026-10-24o, runtime only, prefab untouched) | `bodyWeightWallHandStandoff` | float | 0.15f | 0f–0.6f | 24ab: extra clearance (m) kept between the body capsule and the wall while a hand touch is active, so the arm has room (0 = off). |
| Wall hand touch (2026-10-24o, runtime only, prefab untouched) | `bodyWeightWallHandStandoffBlendSpeed` | float | 5f | 0.5f–20f | 24ab: how fast the standoff blends in/out and pushes the body away (per second). Higher = firmer. |
| Wall hand touch (2026-10-24o, runtime only, prefab untouched) | `bodyWeightWallHandElbowAway` | float | 0.6f | 0f–1.5f | 24ab: how strongly the elbow bends out away from the wall (0 = old elbow-out/down hint). |
| Wall hand touch (2026-10-24o, runtime only, prefab untouched) | `bodyWeightWallHandDebug` | bool | false |  |  |
| Body weight: breathing / idle (2026-10-20w) | `bodyWeightBreathEnabled` | bool | true |  |  |
| Body weight: breathing / idle (2026-10-20w) | `bodyWeightBreathRate` | float | 0.25f | 0.05f–2f | Resting breaths per second. |
| Body weight: breathing / idle (2026-10-20w) | `bodyWeightBreathDepth` | float | 0.8f | 0f–10f | Resting chest rise (deg). |
| Body weight: breathing / idle (2026-10-20w) | `bodyWeightBreathShoulder` | float | 0.6f | 0f–10f | Resting shoulder lift (deg). |
| Body weight: breathing / idle (2026-10-20w) | `bodyWeightBreathExertRate` | float | 0.45f | 0f–3f | Extra breaths per second at full exertion (sprint / jetpack). |
| Body weight: breathing / idle (2026-10-20w) | `bodyWeightBreathExertDepth` | float | 1.8f | 0f–15f | Extra chest rise at full exertion (deg). |
| Body weight: breathing / idle (2026-10-20w) | `bodyWeightBreathExertRise` | float | 4f | 0.05f–30f | Seconds of sprint / jetpack to reach full exertion. |
| Body weight: breathing / idle (2026-10-20w) | `bodyWeightBreathExertDecay` | float | 5f | 0.1f–60f | Seconds to recover from exertion (time constant). |
| Body weight: breathing / idle (2026-10-20w) | `bodyWeightBreathAimScale` | float | 0.25f | 0f–1f | Breathing kept while aiming (0-1). |
| Body weight: breathing / idle (2026-10-20w) | `bodyWeightAimUpperBodyScale` | float | 0f | 0f–1f | 2026-10-20y aim fade: upper-body channels (pelvis yaw, spine, chest, shoulders, arms, hands, neck, head; incl. breathing, hit reactions, swing/settle springs, chest lag) while aiming. 0 = rock steady. |
| Body weight: breathing / idle (2026-10-20w) | `bodyWeightAimFadeTime` | float | 0.12f | 0.01f–1f | 2026-10-20y aim fade: time (s) to fade the upper body in/out when aim starts/ends. |
| Body weight: breathing / idle (2026-10-20w) | `bodyWeightAimHipSwayScale` | float | 0f | 0f–1f | 2026-10-20y aim fade: hip sway (COM shift / hit shove) kept while aiming (moves the whole gun). |
| Body weight: breathing / idle (2026-10-20w) | `bodyWeightAimRecoilScale` | float | 0.6f | 0f–2f | 2026-10-20y aim recoil: kick kept while aiming (impulse on the shot frame only). |
| Body weight: breathing / idle (2026-10-20w) | `bodyWeightAimRecoilSettle` | float | 3f | 1f–8f | 2026-10-20y aim recoil: settle speed multiplier while aiming (stiffness x this; critically damped) so the kick is gone in a few frames. |
| Body weight: enemies (2026-10-20w, visual only, never touches weapons) | `bodyWeightEnemyEnabled` | bool | true |  |  |
| Body weight: enemies (2026-10-20w, visual only, never touches weapons) | `bodyWeightEnemyLean` | float | 0.8f | 0f–10f | Lean into acceleration (deg per m/s^2). |
| Body weight: enemies (2026-10-20w, visual only, never touches weapons) | `bodyWeightEnemyStopCarry` | float | 1.1f | 0f–10f | Forward carry when stopping (deg per m/s^2). |
| Body weight: enemies (2026-10-20w, visual only, never touches weapons) | `bodyWeightEnemyMaxDegrees` | float | 12f | 0f–90f | Lean clamp (deg). |
| Body weight: enemies (2026-10-20w, visual only, never touches weapons) | `bodyWeightEnemySettleSmooth` | float | 0.22f | 0.02f–2f | Settle spring time (s). |
| Body weight: enemies (2026-10-20w, visual only, never touches weapons) | `bodyWeightEnemySettleDamping` | float | 0.45f | 0.1f–1.5f | Settle damping ratio. |
| Body weight: enemies (2026-10-20w, visual only, never touches weapons) | `bodyWeightEnemySwing` | float | 4f | 0f–40f | Attack windup / follow-through torso pitch (deg). |
| Body weight: enemies (2026-10-20w, visual only, never touches weapons) | `bodyWeightEnemyHitImpulse` | float | 8f | 0f–60f | Hit reaction bend away (deg) at reference damage. |
| Body weight: enemies (2026-10-20w, visual only, never touches weapons) | `bodyWeightEnemyHitDamageRef` | float | 25f | 1f–300f |  |
| Body weight: enemies (2026-10-20w, visual only, never touches weapons) | `bodyWeightEnemyCullDistance` | float | 40f | 0f–300f | No body weight beyond this camera distance (m); off-screen enemies are skipped too. |
| Body weight: enemies (2026-10-20w, visual only, never touches weapons) | `BodyWeightScale` | float | > Mathf.Max(0f, bodyWeightIntensity) * Mathf.Max(1f, bodyWeightDebugExaggerate) |  |  |
| Body weight: enemies (2026-10-20w, visual only, never touches weapons) | `HasBodyWeightCustomStash` | bool | > bodyWeightCustomStashNames != null && bodyWeightCustomStashNames.Count > 0 |  |  |
| Locomotion â€” jump height (gait) | `standingJumpHeight` | float | 2.5f | ≥0.35f | Standing still and slow-walk jumps (Invector jumpHeight for this hop). |
| Locomotion â€” jump height (gait) | `walkJumpHeightMultiplier` | float | 1f | ≥0.5f | Slow-walk + move uses standingJumpHeight Ã— this (1 = same as standing hop). |
| Locomotion â€” jump height (gait) | `jogJumpHeightMultiplier` | float | 1.4f | ≥1f | Shift-hold jog jump = standingJumpHeight Ã— this. |
| Locomotion â€” jump height (gait) | `sprintJumpHeightMultiplier` | float | 1.9f | ≥1f | Double-tap Shift sprint burst jump = standingJumpHeight Ã— this. |
| Locomotion â€” jump height (gait) | `jumpHeightSpeedExponent` | float | 1.35f | ≥0f | Extra scale from horizontal speed: mult *= exp(exponent Ã— speed01) / exp(exponent). 0 = gait tiers only. |
| Locomotion - turn lean | `leanOverrideEnabled` | bool | true |  | Use these lean settings instead of Invector's raw lean. Off = stock Invector lean. |
| Locomotion - turn lean | `leanWalkStrength` | float | 1.6f | 0f–4f | Lean multiplier while walking (1 = stock Invector). Higher = deeper lean into turns. |
| Locomotion - turn lean | `leanRunStrength` | float | 1.8f | 0f–4f | Lean multiplier while running / sprinting (1 = stock Invector). |
| Locomotion - turn lean | `leanSmoothTime` | float | 0.12f | 0.01f–0.6f | Seconds to ease into and out of the lean. Lower = snappier. |
| Locomotion - turn lean | `leanMaxValue` | float | 1f | 0.2f–2f | Cap on the lean value sent to the animator (the lean clips top out around 1). |
| Locomotion - turn lean | `leanSpineRollDegrees` | float | 6f | 0f–25f | Extra procedural upper-body roll (degrees) at full lean, on top of the lean clips. 0 = clips only. |
| Locomotion - turn lean | `leanSpineRollWhileStrafing` | bool | false |  | Also apply the extra spine roll while strafing / aiming. |
| Steps & slopes (on foot) | `stepAssistEnabled` | bool | true |  | Master switch for walking up stairs, curbs, rocks and building steps without jumping. |
| Steps & slopes (on foot) | `stepMaxHeight` | float | 0.5f | 0.05f–1.2f | Tallest step (meters) the player walks up. Pushed to Invector stepOffsetMaxHeight. |
| Steps & slopes (on foot) | `stepMinHeight` | float | 0f | 0f–0.5f | Ledges lower than this (meters) are ignored by the step probe. Invector stepOffsetMinHeight. |
| Steps & slopes (on foot) | `stepProbeDistance` | float | 0.1f | 0.02f–0.8f | How far past the capsule edge (meters) the step probe looks ahead. Invector stepOffsetDistance. |
| Steps & slopes (on foot) | `stepOnGroundLayers` | bool | true |  | Step onto everything you can stand on (ground layers + Building), not only Default/Climbable. Needed for built stairs. |
| Steps & slopes (on foot) | `walkableSlopeDegrees` | float | 75f | 10f–85f | Steepest ground (degrees from flat) you can walk up and stand on. Pushed to Invector slopeLimit. |
| Steps & slopes (on foot) | `stepLiftHelper` | bool | true |  | Extra lift helper for steps with angled, chamfered or rough front faces, as long as the top of the step is walkable. |
| Steps & slopes (on foot) | `stepRiserMinAngle` | float | 50f | 20f–90f | A face steeper than this (degrees from flat) counts as a step riser. Gentler faces are left to normal ramp walking. |
| Steps & slopes (on foot) | `stepLiftSpeed` | float | 4f | 0.5f–10f | How fast (m/s) the helper lifts the player onto a step. |
| Steps & slopes (on foot) | `stepLiftForwardPush` | float | 0.8f | 0f–3f | Forward push (m/s) while lifting so the feet land on the step top. |
| Steps & slopes (on foot) | `stepIgnoreSlopeBlock` | bool | true |  | Ignore Invector's steep-slope block while a valid step is ahead, so angled riser faces don't stop you. |
| Survival â€” pools (authority) | `maxHealth` | float | 100f |  |  |
| Survival â€” pools (authority) | `maxEnergy` | float | 100f |  |  |
| Survival â€” pools (authority) | `maxStamina` | float | 100f |  |  |
| Survival â€” pools (authority) | `maxOxygen` | float | 2400f |  |  |
| Survival - passive drain | `energyDrainPerSecond` | float |  |  | Passive walk/idle energy drain. Tool use (jetpack, mining, scanner) uses toolEnergyDrainPerSecond instead. |
| Survival - passive drain | `toolEnergyDrainPerSecond` | float | 0.5f |  | Energy drained per second while jetpack, mining tool, or scanner is active. |
| Survival - passive drain | `handCraftEnergyPerItem` | float | 0.5f |  | Energy spent per output item when crafting away from a workbench. |
| Survival - passive drain | `oxygenDrainPerSecond` | float | 4f |  |  |
| Survival - low oxygen warning | `lowOxygenWarningPercent` | float | 10f | 1f–50f | Show the center-screen Low Oxygen toast when oxygen is at or below this percent (0-100). |
| Survival - low oxygen warning | `lowOxygenFlashPerSecond` | float | 1.5f | ≥0.25f | Flash the Low Oxygen toast this many times per second while below the threshold. |
| Survival - low oxygen warning | `lowOxygenWarningText` | string | "Low Oxygen" |  | Center-screen warning copy. |
| Survival - low oxygen warning | `lowOxygenWarningEnabled` | bool | true |  |  |
| Survival - low oxygen warning | `healthDrainPerSecond` | float | 2f |  | Health drained per second while energy is below lowStatThresholdPercent. |
| Survival - low oxygen warning | `lowStatThresholdPercent` | float | 25f | 1f–99f |  |
| Survival - low oxygen warning | `oxygenDepletedHealthDrainMultiplier` | float | 5f |  |  |
| Survival - health regen | `enableHealthRegen` | bool |  |  |  |
| Survival - health regen | `healthRegenPerSecond` | float | 1f |  |  |
| Survival - health regen | `healthRegenDelayAfterDamage` | float | 5f |  |  |
| Survival â€” exposure caps & recovery | `maxThermalStress` | float | 100f |  |  |
| Survival â€” exposure caps & recovery | `maxRadiation` | float | 100f |  |  |
| Survival â€” exposure caps & recovery | `maxSulfur` | float | 100f |  |  |
| Survival â€” exposure caps & recovery | `maxVolcano` | float | 100f |  |  |
| Survival â€” exposure caps & recovery | `exposureRecoveryPerSecond` | float | 8f |  |  |
| Survival â€” exposure caps & recovery | `thermalRecoveryPerSecond` | float | 12f |  |  |
| Stamina â€” idle | `idleStaminaRegenPerSecond` | float | 3f |  | Passive regen when not sprinting, climbing, dashing, or in dash recovery. |
| Stamina â€” sprint | `sprintStaminaDrainPerSecond` | float | 12f |  |  |
| Stamina â€” sprint | `sprintStaminaRegenPerSecond` | float |  |  | Regen while sprinting (usually 0). |
| Stamina â€” climb | `climbStartStaminaCost` | float | 5f |  | Flat stamina spent when attaching to a wall. |
| Stamina â€” climb | `climbMoveStaminaDrainPerSecond` | float | 8f |  |  |
| Stamina â€” climb | `climbHangStaminaDrainPerSecond` | float | 4f |  |  |
| Stamina â€” climb | `climbStaminaRegenPerSecond` | float |  |  | Regen while climbing (usually 0). |
| Stamina â€” dash | `dashStaminaCost` | float | 22f |  | Flat stamina spent when a dash starts. |
| Stamina â€” dash | `dashStaminaTickExtraPercent` | float | 0.20f | 0f–1f | Extra stamina drained over the dash duration, as a fraction of dashStaminaCost (0.2 = +20%). |
| Stamina â€” dash | `dashStaminaRegenPerSecond` | float |  |  | Regen while the dash slide is active (usually 0). |
| Stamina â€” dash | `dashRecoveryStaminaRegenPerSecond` | float | 1.5f |  | Regen per second for a short window after the dash ends. |
| Stamina â€” dash | `dashStaminaRecoverySeconds` | float | 2.5f |  | Seconds after dash ends where only dashRecoveryStaminaRegenPerSecond applies. |
| Dash â€” input & motion | `dashDoubleTapWindow` | float | 0.28f |  | Max seconds between two taps of the same WASD key. |
| Dash â€” input & motion | `dashDistance` | float | 4.5f | 1f–25f | How far the double-tap slide travels, in meters. |
| Dash â€” input & motion | `dashSpeed` | float | 15.4f | 4f–40f | Dash move speed in meters per second. |
| Dash â€” input & motion | `dashDuration` | float | 0.18f |  | Fallback only when distance and speed are unset. Live duration is Distance / Speed. |
| Dash â€” input & motion | `dashCooldown` | float | 0.55f |  | Seconds before another dash can start. |
| Dash â€” input & motion | `dashAllowAirDash` | bool |  |  | Allow double-tap dash while airborne without the Air Dash skill. |
| Dash â€” input & motion | `dashAnimationSpeed` | float |  | 0f–1f | 0 freezes walk/run anim speed during the slide. 1 is normal. |
| Dash â€” input & motion | `dashCollisionSkin` | float | 0.08f |  | Skin applied when capsule-casting so dashes stop before penetrating colliders. |
| Dash â€” input & motion | `dashDetachesFromClimb` | bool | true |  | Double-tap dash while climbing cancels climb and slides off the wall. |
| Dash â€” input & motion | `dashAllowedWhileClimbing` | bool | true |  | Allow double-tap dash input while clinging to a wall. |
| Dash â€” hologram | `dashHologramColor` | Color | new Color(0.25f, 0.85f, 1f, 0.42f) |  |  |
| Dash â€” hologram | `dashHologramEmission` | float | 4f |  |  |
| Dash â€” hologram | `dashHologramMaterial` | Material |  |  | Optional override. If empty, an HDRP unlit hologram is built at runtime. |
| Dash â€” speed streaks | `dashStreakColor` | Color | new Color(0.55f, 0.95f, 1f, 0.85f) |  |  |
| Dash â€” speed streaks | `dashStreakCount` | int | 18 |  |  |
| Dash â€” speed streaks | `dashStreakLifetime` | float | 0.22f |  |  |
| Dash â€” speed streaks | `dashStreakSize` | float | 0.08f |  |  |
| Dash â€” speed streaks | `dashStreakStretch` | float | 3.5f |  |  |
| Dash â€” speed streaks | `dashStreakRadius` | float | 0.55f |  |  |
| Dash â€” speed streaks | `dashStreakMaterial` | Material |  |  |  |
| Dash â€” speed streaks | `dashStreakPrefab` | GameObject |  |  |  |
| Dash â€” smoke | `dashSmokeColor` | Color | new Color(0.55f, 0.62f, 0.7f, 0.45f) |  |  |
| Dash â€” smoke | `dashSmokeCount` | int | 14 |  |  |
| Dash â€” smoke | `dashSmokeLifetime` | float | 0.55f |  |  |
| Dash â€” smoke | `dashSmokeSize` | float | 0.7f |  |  |
| Dash â€” smoke | `dashSmokeMaterial` | Material |  |  |  |
| Dash â€” smoke | `dashSmokePrefab` | GameObject |  |  |  |
| Surface | `climbableLayerName` | string | "Climbable" |  |  |
| Surface | `climbableTag` | string | "Climbable" |  |  |
| Surface | `climbableLayers` | LayerMask |  |  | Only these layers. 0 = resolve from climbableLayerName. |
| ClingSense bubble | `enableClingSense` | bool | true |  | Body-centered overlap + ray fan for face/soffit/ground/stub-lip/sides (Dune-style volume feel). |
| ClingSense bubble | `drawClingSenseGizmos` | bool | true |  | Draw ClingSense bubble in Scene view while climbing (Gizmos must be enabled). Also draws when the climb controller is selected. |
| Baked probes | `preferBakedProbes` | bool | false |  | When a DMClimbProbeSet is on/near the climbable, prefer nearest baked probe for attach + StickAndMove. Mesh lip fallback remains. |
| Baked probes | `probeReach` | float | 1.55f | 0.4f–3.5f | Max distance from body/hands to consider a baked probe for attach and cling. |
| Baked probes | `probeStepMax` | float | 1.6f | 0.35f–3.5f | Max step between neighboring stance pairs while climbing (WASD). Auto-raises to nearest neighbor if bake spacing is wider. Runtime also caps at 0.85m. |

**Preset / apply methods:** `ApplyBodyWeightPreset()`, `ApplyToSurvivalStats()`, `ApplyWallHandPreset()`

## Dash

**What it does:** Double-tap slide, stamina cost, hologram, streaks, and smoke VFX.

**Where:**
- Studio: `Genesis Studio / Player / Dash` (panel mode `SingletonAsset`)
- Asset: `Assets/_Project/Resources/Climb/DM_ClimbDashProfile.asset`
- Script: `Assets/_Project/Features/Climb/Runtime/DM_ClimbDashProfile.cs`

**Key settings:** same script as [Climb](#climb).

## Jetpack

**What it does:** Boost, hover, and jetpack energy drain.

**Where:**
- Studio: `Genesis Studio / Player / Jetpack` (panel mode `SingletonAsset`)
- Asset: `Assets/_Project/Features/Jetpack/Data/DMJetpackProfile.asset`
- Script: `Assets/_Project/Features/Jetpack/Data/DMJetpackProfile.cs`

**Key settings** (from code):

| Group | Field | Type | Default | Range | Notes |
|---|---|---|---|---|---|
| Fuel | `maxBoostSeconds` | float | 8f | ≥1f | How long a full tank lasts (seconds). Player variant uses Starfield. |
| Fuel | `fullThrustEndSeconds` | float | 8f | ≥0f | Seconds of full thrust before fade. Set equal to Max Boost Seconds for no early cutoff. |
| Fuel | `releaseRegenBonusSeconds` | float | 1f | ≥0f |  |
| Fuel | `regenSecondsPerSecond` | float | 0.35f | ≥0f |  |
| Fuel | `energyDrainPerSecond` | float | 0.5f | ≥0f | Survival energy (power) spent per second while boosting. |
| Vertical Thrust | `upwardThrustForce` | float | 6.5f | ≥0f | Target upward speed while holding boost (m/s). |
| Vertical Thrust | `descendForce` | float | 3.5f | ≥0f | Target downward speed when holding S during boost (m/s, positive value). |
| Vertical Thrust | `descendInputThreshold` | float | 0.35f | 0f–1f |  |
| Vertical Thrust | `descendBrakeStrength` | float | 0.85f | 0f–1f |  |
| Vertical Thrust | `verticalResponse` | float | 3f | ≥0.5f | How quickly vertical velocity eases toward the thrust target. |
| Vertical Thrust | `hoverGravityScale` | float | 0.12f | ≥0f | Gravity scale while hovering at full boost with no vertical input. |
| Vertical Thrust | `boostDescendGravityScale` | float | 0.85f | ≥0f | Gravity scale while intentionally descending during boost. |
| Vertical Thrust | `fadeFallGravityScale` | float | 0.22f | ≥0f | Gravity scale during the late-tank thrust fade. |
| Vertical Thrust | `freeFallGravityScale` | float | 1f | ≥0f | Gravity scale after fuel is empty. |
| Vertical Thrust | `coastFallGravityScale` | float | 0.75f | ≥0f | Gravity scale when boost is released mid-air. |
| Vertical Thrust | `releaseUpwardBleed` | float | 5f | ≥0f | Bleeds leftover upward velocity after releasing boost. |
| Directed Boost (WASD) | `jetpackAirSpeed` | float | 3.25f | ≥0.5f | Target planar speed while a direction is held during boost (m/s). |
| Directed Boost (WASD) | `jetpackPlanarResponse` | float | 22f | ≥1f | How quickly planar velocity lerps toward the held WASD heading. |
| Directed Boost (WASD) | `directedVerticalScale` | float | 0.35f | 0f–1f | Vertical thrust left at full WASD (0 = flatten, 1 = same climb as hover). |
| Directed Boost (WASD) | `directedRedirect` | float | 10f | ≥0f | How quickly leftover upward speed converts into the held WASD heading (m/s per second). |
| Directed Boost (WASD) | `jetpackSteerInputSmoothTime` | float | 0.11f | 0.04f–0.35f | Brief delay before steer direction catches new WASD input, then planar response takes over. |
| Directed Boost (WASD) | `jetpackAirSmooth` | float | 18f | ≥1f | Invector airSmooth override during boost (higher = snappier input follow). |
| Directed Boost (WASD) | `defaultAirSpeed` | float | 5f | ≥0.5f | Restored airSpeed when not in jetpack boost/fade. |
| Animation | `jetpackMoveSmoothTime` | float | 0.22f | 0.05f–0.5f |  |
| Animation | `animBlendGain` | float | 1.35f | 0.5f–2.5f |  |
| Animation | `animLeanStrength` | float | 0.1f | 0.05f–1f | Scales directional fly pose lean in the blend tree (0.1 = 10% of full tilt). |
| Animation | `animInputDeadzone` | float | 0.1f | 0f–0.35f |  |
| Animation | `flyAnimSpeed` | float | 1f | 0.5f–2f |  |
| Animation | `animStrafeThreshold` | float | 0.5f | 0.25f–0.85f |  |
| Animation | `landCrossFadeSeconds` | float | 0.12f | 0.05f–0.35f |  |
| Thruster VFX | `thrusterAlphaMin` | float | 0.1f | 0f–1f |  |
| Thruster VFX | `thrusterAlphaMax` | float | 1f | 0f–1f |  |
| Thruster VFX | `thrusterPowerSmoothTime` | float | 0.2f | 0.05f–0.5f |  |
| Thruster VFX | `flareFactorMultiplier` | float | 0.85f | ≥0.1f |  |
| Thruster VFX | `slowSparksFactorMultiplier` | float | 0.8f | ≥0.1f |  |
| Thruster VFX | `fastSparksFactorMultiplier` | float | 0.8f | ≥0.1f |  |
| Thruster VFX | `distortionFactorMultiplier` | float | 0.85f | ≥0.1f |  |
| Thruster VFX | `particleSpeedMultiplier` | float | 0.7f | ≥0.1f |  |
| Thruster VFX | `particleLifetimeMultiplier` | float | 1.15f | ≥0.1f |  |
| Thruster Audio | `thrusterAudioSmooth` | float | 0.12f | 0.02f–0.4f | How quickly volume/pitch catch thrust. |
| Thruster Audio | `thrusterAudioReleaseSeconds` | float | 1f | ≥0.1f | SmoothDamp time when thrust ends — loop layers fade out over roughly this many seconds. |
| Thruster Audio / Layer 1 (Rumble) | `thrusterLayer1` | AudioClip |  |  |  |
| Thruster Audio / Layer 1 (Rumble) | `thrusterLayer1Volume` | float | 0.55f | 0f–1f |  |
| Thruster Audio / Layer 1 (Rumble) | `thrusterLayer1Pitch` | Vector2 | new Vector2(0.92f, 1.04f) |  |  |
| Thruster Audio / Layer 2 (Bright) | `thrusterLayer2` | AudioClip |  |  | Comes in as thrust rises. |
| Thruster Audio / Layer 2 (Bright) | `thrusterLayer2Volume` | float | 0.4f | 0f–1f |  |
| Thruster Audio / Layer 2 (Bright) | `thrusterLayer2Pitch` | Vector2 | new Vector2(0.98f, 1.12f) |  |  |
| Thruster Audio / Layer 2 (Bright) | `thrusterLayer2Start` | float | 0.25f | 0f–1f |  |

## Jump/Landing

**What it does:** Regular jump bounce, early land timing, foot snap, probes, clip lengths, 3-tier height bands, and clip library.

**Where:**
- Studio: `Genesis Studio / Player / Jump/Landing` (panel mode `LandingCombined`)
- Asset: `Assets/_Project/Resources/Landing/DMLandingProfile.asset`
- Script: `Assets/_Project/Scripts/Player/DMLandingProfile.cs`

**Key settings** (from code):

| Group | Field | Type | Default | Range | Notes |
|---|---|---|---|---|---|
| Jump / short bounce | `ownedBounceOnRegularJump` | bool | true |  | Play owned LandLow bounce on regular jumps below the bounce ceiling (heroDropMeters unless overridden below). Off = leave regular jump land to Invector. |
| Jump / short bounce | `shortBounceMaxDropMeters` | float | 0f | 0f–20f | When > 0, max drop meters that still use short bounce instead of hero LandHigh. When 0, uses ClimbDash heroDropMeters from the Jump/Landing height band. |
| Jump / short bounce | `preferBounceStateForShortLand` | bool | true |  | Prefer Bounce animator state for short bounce when LandLow is missing. |
| Early land timing | `landAnticipateMaxDist` | float | 0.52f | 0.05f–1.2f | Start owned land when feet are within this distance of walkable ground (before touch). |
| Early land timing | `landAnticipateMinFallSpeed` | float | 1.35f | 0.5f–12f | Minimum downward speed (m/s, positive number) required before anticipating a land. |
| Foot snap | `landFeetGroundSkin` | float | 0.03f | 0f–0.12f | Gap left between foot bottom and ground after snap. |
| Foot snap | `landMaxFeetSnap` | float | 0.85f | 0.1f–1.5f | Max upward correction per snap. Should be >= landAnticipateMaxDist to avoid air-gap hero lands. |
| Ground probe | `walkableDist` | float | 0.45f | 0.1f–1f | Pivot-to-ground distance treated as on walkable ground. |
| Ground probe | `walkableNormalY` | float | 0.55f | 0.2f–0.95f | Minimum hit normal Y to count as walkable. |
| Ground probe | `groundCommitSeconds` | float | 0.06f | 0.02f–0.4f | Seconds on walkable ground before committing a land from air tracking. |
| Ground probe | `probeRayOriginUp` | float | 0.2f | 0.05f–0.6f | Ray origin offset above pivot for down probes. |
| Ground probe | `probeRayMaxDown` | float | 5f | 1f–12f | Max downward ray length for foot/ground probes. |
| Ground probe | `probeValidMaxDist` | float | 3.5f | 0.5f–8f | Ignore probe hits farther than this from pivot (anticipate path). |
| Ground probe | `probePivotBelowReject` | float | -0.25f | -0.5f–0.2f | Reject hits when pivot is this far below the hit point (negative = allow slightly below). |
| Owned clip durations (seconds) | `landLowDuration` | float | 0.45f | 0.15f–2f |  |
| Owned clip durations (seconds) | `landHighDuration` | float | 1f | 0.3f–2.5f |  |
| Owned clip durations (seconds) | `jetpackLandDuration` | float | 1.25f | 0.4f–3f |  |
| Owned clip durations (seconds) | `bounceLandDuration` | float | 0.65f | 0.2f–2f |  |
| Animator | `landCrossFadeSeconds` | float | 0.03f | 0.01f–0.2f |  |
| Finish land / anti double-land | `landGroundedEndGraceSeconds` | float | 0.35f | 0.05f–1f | After owned land ends, ignore new lands for at least this long. |
| Finish land / anti double-land | `landMaxExtraWaitSeconds` | float | 0.55f | 0.1f–1.5f | After clip timeout, keep waiting for walkable ground up to this long before forcing end. |
| Jump micro-contact skip | `minDropMetersToLand` | float | 0.2f | 0.05f–1f | Ignore owned land when drop and fall speed are below these (tiny contacts). Regular jump bounce still plays when ownedBounceOnRegularJump is on. |
| Jump micro-contact skip | `minFallSpeedYToLand` | float | -2f | -6f–0f |  |
| Drop clamp (apex vs speed) | `clampDropSpeedEpsilon` | float | 0.5f | 0.1f–2f |  |
| Drop clamp (apex vs speed) | `clampDropShortCap` | float | 1.5f | 0.5f–4f |  |
| Drop clamp (apex vs speed) | `clampDropPhysicsSlack` | float | 6f | 1f–12f |  |
| Jetpack land state | `preferJetpackLandState` | bool | true |  | When jetpack was used this airtime and LandLow/Bounce are missing, prefer Jetpack Land animator state for short bounce. |
| Planar slide / brake | `zeroPlanarVelocityOnLand` | bool | true |  | When ON with Hold Planar: anti-drift mode — slider limits how long drift is suppressed (0 = whole clip). |
| Planar slide / brake | `holdPlanarPositionDuringLand` | bool | true |  | When ON with Zero Planar: anti-drift mode — also pin X/Z while the guard is active. |
| Planar slide / brake | `slidingLandingHoldSeconds` | float | 1.1f | 0f–2.5f | Both toggles OFF: directional slide mode — keep horizontal momentum this many seconds after touch, then brake. Either toggle ON: anti-drift guard for this many seconds (0 = whole land clip). |

## Pickup Items

**What it does:** Hold-E time, prompt range, focus cone and toast; the library of world pickup prefabs; and the item + pickup prefab creator.

**Where:**
- Studio: `Genesis Studio / Player / Pickup Items` (panel mode `PickupItemsCombined`)
- Asset: `Assets/_Project/Resources/Interaction/DM_PickupProfile.asset`
- Script: `Assets/_Project/Scripts/Interaction/DMPickupProfile.cs`

**Key settings** (from code):

| Group | Field | Type | Default | Range | Notes |
|---|---|---|---|---|---|
| Hold E | `holdSeconds` | float | 0.5f | ≥0.05f | Seconds E must be held to take a focused item or blueprint. |
| Prompt & Focus | `closePromptPlanarMeters` | float | 1.25f | ≥0.1f | Planar distance (m) at which the yellow dot becomes the Hold-E prompt and pickup is allowed. |
| Prompt & Focus | `pickupConeFovDegrees` | float | 70f | 1f–179f | View cone (degrees) used to choose the focused pickup. |
| Prompt & Focus | `defaultPromptText` | string | DefaultPrompt |  | Prompt shown when a pickup prefab has no prompt text of its own. |
| Pickup Toast | `toastSlideInSeconds` | float | 0.35f | ≥0.01f |  |
| Pickup Toast | `toastHoldSeconds` | float | 2.3f | ≥0f |  |
| Pickup Toast | `toastFadeOutSeconds` | float | 0.35f | ≥0.01f |  |
| Pickup Toast | `toastSlideDistance` | float | 28f |  |  |

## Footsteps

**What it does:** Look (marks, dust, tags, terrain 0-10) and audio clip libraries in one place.

**Where:**
- Studio: `Genesis Studio / Player / Footsteps` (panel mode `FootstepsCombined`)
- Asset: `Assets/_Project/Resources/Player/DM_FootstepProfile.asset`
- Script: `Assets/_Project/Scripts/Player/DMFootstepProfile.cs`

**Key settings** (from code):

| Group | Field | Type | Default | Range | Notes |
|---|---|---|---|---|---|
| Triggers | `volume` | float | 1f | 0f–1f | Default Invector footstep volume when no tag or terrain layer overrides. |
| Triggers | `spawnParticle` | bool | true |  | Default: spawn dust VFX on each step. |
| Triggers | `spawnStepMark` | bool | true |  | Default: spawn the footprint quad on each step. |
| Triggers | `useTriggerEnter` | bool | true |  | Fire effects from foot trigger enter. Off = animation events only. |
| Triggers | `minSecondsBetweenFootVfx` | float | 0.28f | ≥0.05f | Ignore a second dust/mark/audio fire on the same foot inside this window (trigger + anim event, or blended clips). |
| Triggers | `debugTextureName` | bool |  |  | Log the resolved tag / terrain layer index on each step. |
| Walkable layers | `includeDefaultLayer` | bool | true |  | Allow footprints and dust on Default. |
| Walkable layers | `includeClimbableLayer` | bool | true |  | Allow footprints and dust on Climbable (live Gaia tile). |
| Walkable layers | `extraWalkableLayers` | LayerMask |  |  | Any extra physics layers that should accept footprints and dust. |
| Footprint mark | `stepMarkPrefab` | GameObject |  |  |  |
| Footprint mark | `stepMarkLifetime` | float | 3f | ≥0.1f | Seconds before a spawned footprint is destroyed. |
| Dust VFX | `dustPrefab` | GameObject |  |  |  |
| Dust VFX | `dustMaterial` | Material |  |  |  |
| Dust VFX | `dustLifetimeSeconds` | float | 1f | ≥0.05f | Unscaled seconds a dust puff stays alive. |
| Dust VFX | `maxLiveDust` | int | 16 | ≥1 | Max live dust puffs. Oldest is recycled first. |
| Unity tags | `tagEntries` | DMFootstepTagEntry[] |  |  | Used when the step hits a mesh (or a terrain with no enabled splat slot). First matching tag wins. |
| Terrain texture layers (0-10) | `terrainTextureLayers` | DMFootstepTerrainLayerEntry[] |  |  | Splat slots on Gaia tiles. Dominant layer index picks the row. Disabled rows fall through to tag / defaults. |
| Terrain texture layers (0-10) | `tag` | string | "Untagged" |  | Unity tag on the collider / terrain (Terrain, Untagged, Climbable, Default). |
| Terrain texture layers (0-10) | `enabled` | bool | true |  |  |
| Terrain texture layers (0-10) | `volume` | float | 1f | 0f–1f |  |
| Terrain texture layers (0-10) | `spawnParticle` | bool | true |  |  |
| Terrain texture layers (0-10) | `spawnStepMark` | bool | true |  |  |
| Terrain texture layers (0-10) | `stepMarkPrefab` | GameObject |  |  |  |
| Terrain texture layers (0-10) | `dustPrefab` | GameObject |  |  |  |
| Terrain texture layers (0-10) | `dustMaterial` | Material |  |  |  |
| Terrain texture layers (0-10) | `layerIndex` | int |  | 0–10 | Splat index on the terrain (0-10). |
| Terrain texture layers (0-10) | `label` | string | "Terrain Layer" |  | Authoring label. Live tiles: 0-1 sand, 2-5 rock/cliff, 6 mud, 7 snow. |
| Terrain texture layers (0-10) | `enabled` | bool | true |  | Off = this splat falls through to tag / default settings. |
| Terrain texture layers (0-10) | `volume` | float | 1f | 0f–1f |  |
| Terrain texture layers (0-10) | `spawnParticle` | bool | true |  |  |
| Terrain texture layers (0-10) | `spawnStepMark` | bool | true |  |  |
| Terrain texture layers (0-10) | `stepMarkPrefab` | GameObject |  |  |  |
| Terrain texture layers (0-10) | `dustPrefab` | GameObject |  |  |  |
| Terrain texture layers (0-10) | `dustMaterial` | Material |  |  |  |

## Camera

**What it does:** Mouse zoom, aim/sprint pull, wall collision push, near clip, optics, and prefab camera wiring.

**Where:**
- Studio: `Genesis Studio / Player / Camera` (panel mode `CameraCombined`)
- Asset: `Assets/_Project/Resources/Player/DM_CameraProfile.asset`
- Script: `Assets/_Project/Scripts/Player/DMCameraProfile.cs`

**Key settings** (from code):

| Group | Field | Type | Default | Range | Notes |
|---|---|---|---|---|---|
| Mouse Zoom (follow distance) | `minDistance` | float | 2.25f |  | Closest free-look follow distance (scroll all the way in). Keep >= Collision Min Follow. |
| Mouse Zoom (follow distance) | `maxDistance` | float | 12f |  | Farthest free-look follow distance (scroll all the way out). |
| Mouse Zoom (follow distance) | `defaultDistance` | float | 2.25f |  | Follow distance applied when an expedition starts. |
| Mouse Zoom (follow distance) | `zoomClickLevels` | int | 10 | 2–24 | Discrete mouse-wheel stops between min and max. |
| Aim / Sprint Zoom | `aimZoomPullInMeters` | float | 0.55f |  | How much closer Aiming pulls vs free-look preferred zoom. |
| Aim / Sprint Zoom | `sprintZoomOutMeters` | float | 0.85f |  | Extra follow distance while sprinting (slight pull-out only). |
| Aim / Sprint Zoom | `aimMinCameraDistance` | float | 0.78f |  | Closest follow distance allowed while aiming (ADS). Can be below scroll min. |
| Collision Push (walls / terrain) | `minFollow` | float | 2.15f |  | Never pull the lens closer than this while colliding (meters from pivot). |
| Collision Push (walls / terrain) | `climbMinFollow` | float | 2.55f |  |  |
| Collision Push (walls / terrain) | `mantleMinFollow` | float | 2.9f |  |  |
| Collision Push (walls / terrain) | `pullSpeed` | float | 16f |  |  |
| Collision Push (walls / terrain) | `releaseSpeed` | float | 5.5f |  |  |
| Collision Push (walls / terrain) | `collisionHysteresis` | float | 0.12f |  |  |
| Collision Push (walls / terrain) | `climbPullSpeed` | float | 5.5f |  |  |
| Collision Push (walls / terrain) | `climbReleaseSpeed` | float | 2.8f |  |  |
| Collision Push (walls / terrain) | `mantlePullSpeed` | float | 3.0f |  |  |
| Collision Push (walls / terrain) | `mantleReleaseSpeed` | float | 2.2f |  |  |
| Collision Push (walls / terrain) | `climbNearRadius` | float | 4.25f |  |  |
| Collision Push (walls / terrain) | `sphereRadius` | float | 0.2f |  |  |
| Collision Push (walls / terrain) | `extraSkin` | float | 0.08f |  |  |
| Collision Push (walls / terrain) | `floorProbe` | float | 3.0f |  |  |
| Tight spaces (tents, corners, single walls) | `tightSpaceMinFollow` | float | 0.4f |  | When geometry blocks the lens closer than Min Follow, allow pulling in to this distance (meters from pivot). |
| Tight spaces (tents, corners, single walls) | `openAirMinFollow` | float | 1.35f |  | Minimum lens distance (meters from pivot) when a wall/block behind the camera pushes it in out in the open (NOT inside a tent/room). Keeps the lens outside the player's head/body mesh. Also the floor for any sudden Invector culling pull-in (charge wind-up, weapon triggers). |
| Tight spaces (tents, corners, single walls) | `playerMeshHideDistance` | float | 0.85f |  | Hide the player mesh when the lens is closer than this to the pivot (prevents clipping through the body). |
| Tight spaces (tents, corners, single walls) | `playerMeshShowHysteresis` | float | 0.18f |  | Hysteresis before re-showing the player mesh after a hide. |
| Tight spaces (tents, corners, single walls) | `tightSpaceReleaseSpeed` | float | 7.5f |  | When leaving a tight push, blend back toward the last comfortable follow distance. |
| Tight spaces (tents, corners, single walls) | `interiorMaxFollow` | float | 1.35f |  | Max follow distance while the pivot is inside an enclosed shell (tent, room). |
| Tight spaces (tents, corners, single walls) | `interiorFirstPersonDistance` | float | 0.35f |  | First-person-style follow when interior push cannot keep the lens inside the shell. |
| Tight spaces (tents, corners, single walls) | `interiorEnclosureProbeDistance` | float | 2.75f |  | Horizontal ray length used to detect enclosed spaces around the pivot. |
| Characters in the camera path (2026-10-12) | `absoluteMinDistance` | float | 0.4f | 0.2f–1.5f | Hard floor for the lens distance (m) outside interiors; interior first-person keeps interiorFirstPersonDistance. |
| Characters in the camera path (2026-10-12) | `fadeBlockingCharacters` | bool | true |  | Enemies / companions / animals between the player and the lens (or around the lens) are hidden, shadows kept, instead of the lens clipping into them. Characters never pull the camera in. |
| Characters in the camera path (2026-10-12) | `characterBlockRadius` | float | 0.2f | 0.05f–0.6f | Sphere radius of the player->lens character probe (m). |
| Characters in the camera path (2026-10-12) | `characterLensHideDistance` | float | 0.35f | 0f–1f | Characters within this distance of the lens are hidden too (lens inside an enemy). |
| Lens | `nearClipPlane` | float | 0.05f |  | Gameplay camera near clip plane. Too small + max zoom-in can make world UI swim. |

## Player Systems

**What it does:** Toggle DM modules on Player_v7 Variant (MonoBehaviour — not a profile asset).

**Where:**
- Studio: `Genesis Studio / Player / Player Systems` (panel mode `PlayerSystemsLink`)

