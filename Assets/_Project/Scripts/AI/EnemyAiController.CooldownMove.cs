using UnityEngine;

namespace Project.AI
{
    /// <summary>
    /// Slice 3 / C1: engager cooldown movement. While the engager (the one enemy allowed to swing) waits for its attack
    /// cooldown, the brain (DMEnemyBrain) can roll a slow strafe or a small backstep so it does not stand still.
    /// Every number lives in DM_EnemyEngagementProfile "Engager cooldown movement". It stays inside the melee band, never runs
    /// into a swing (ends engagerMoveStopBeforeAttack before the attack is ready), keeps the distance constant while strafing,
    /// and caps the net arc so it cannot orbit. Plain transform steps like the holder sidestep, no NavMesh.
    /// </summary>
    public partial class EnemyAiController
    {
        private enum EngagerMoveKind
        {
            None = 0,
            Strafe = 1,
            Backstep = 2
        }

        private EngagerMoveKind engagerMoveKind;
        private float engagerMoveUntil;
        private float nextEngagerMoveAllowed;
        private float engagerMoveSign = 1f;
        private float engagerMoveNetArc;
        private float engagerMoveArcDone;
        private float engagerMoveMaxArc;
        private float engagerMoveSpeed;
        private float engagerMoveDistanceLeft;
        private float engagerMoveLastSign;
        private float engagerMoveStart;
        private float engagerMoveTotal;
        private Vector3 engagerMoveStartPos;
        private float engagerMovePlanned;

        /// <summary>Quick ease-in (starts ~30% speed) x ease-out (last quarter of the move); 1 = full speed.</summary>
        private static float EaseInOutFactor(float elapsed, float remainingFraction, float easeSeconds)
        {
            if (easeSeconds <= 0.001f)
                return 1f;

            float a = Mathf.SmoothStep(0.3f, 1f, elapsed / easeSeconds);
            float b = Mathf.SmoothStep(0.35f, 1f, remainingFraction / 0.25f);
            return a * b;
        }

        private float quickStepUntil;

        /// <summary>True during any enemy quick step (engager strafe / backstep, holder sidestep, hit backstep). The motor bridge exaggerates leg animation for these.</summary>
        public bool IsQuickStepActive => Time.time < quickStepUntil || IsEngagerCooldownMoving;

        public void NoteQuickStep(float seconds)
        {
            quickStepUntil = Mathf.Max(quickStepUntil, Time.time + Mathf.Max(0f, seconds) + 0.05f);
        }

        public bool IsEngagerCooldownMoving => engagerMoveKind != EngagerMoveKind.None;

        private void ResetEngagerCooldownMove()
        {
            engagerMoveKind = EngagerMoveKind.None;
            engagerMoveNetArc = 0f;
            nextEngagerMoveAllowed = 0f;
        }

        /// <summary>Per-enemy variety factor for cooldown moves and the post-hit backstep (1 = identical enemies).</summary>
        internal float MoveVariation()
        {
            DM_EnemyEngagementProfile p = EngagementProfile;
            float scale = p != null ? p.moveVariationScale : 0f;
            return Mathf.Max(0.3f, 1f + (CrowdingVariationFactor() - 1f) * scale);
        }

        /// <summary>Returns true while a cooldown move is moving the enemy this frame (caller skips creep / standoff logic).</summary>
        private bool TickEngagerCooldownMove(Transform target, float distance, float effective, float minSep, DM_EnemyEngagementProfile p)
        {
            if (p == null || !p.enableEngagerCooldownMove || brain == null || combat == null)
                return false;

            float now = Time.time;
            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.0001f)
            {
                engagerMoveKind = EngagerMoveKind.None;
                return false;
            }

            float maxBand = effective * p.engagerStrafeMaxDistanceFactor;
            bool blocked = IsStationary || IsInAttackSequence || combat.WantsMeleeReposition || combat.WantsForcedMissStep ||
                           (brain != null && brain.IsMissRecoveryActive) ||
                           (ragdollBridge != null && (ragdollBridge.IsHitStaggerActive || ragdollBridge.IsKnockdownActive));

            if (engagerMoveKind != EngagerMoveKind.None)
            {
                if (blocked || now >= engagerMoveUntil || distance > maxBand + 0.4f ||
                    combat.AttackReadyIn <= p.engagerMoveStopBeforeAttack)
                {
                    EndEngagerCooldownMove(p, now, blocked ? "interrupted" : now >= engagerMoveUntil ? "timeout" : distance > maxBand + 0.4f ? "band" : "attack-ready");
                    return false;
                }

                return RunEngagerCooldownMove(target, toTarget, distance, p);
            }

            if (blocked || now < nextEngagerMoveAllowed || combat.AttackReadyIn < p.engagerMoveMinCooldown ||
                distance > maxBand || distance < minSep)
                return false;

            if (!brain.ConsumeEngagerMoveRequest())
                return false;

            float variety = MoveVariation();
            engagerMoveNetArc *= 0.5f;

            // Backstep: only if it keeps the enemy inside the no-creep range, so it never triggers an immediate creep-back.
            if (Random.value < p.engagerBackstepChance)
            {
                float room = effective * p.creepStartFactor * 0.97f - distance;
                float back = Mathf.Min(p.RandomRange(p.engagerBackstepDistance) * variety, room);
                if (back >= 0.15f)
                {
                    float speed = walkSpeed * p.engagerBackstepSpeedFactor * variety;
                    engagerMoveKind = EngagerMoveKind.Backstep;
                    engagerMoveDistanceLeft = back;
                    engagerMoveTotal = back;
                    engagerMoveStart = now;
                    engagerMoveStartPos = transform.position;
                    engagerMovePlanned = back;
                    engagerMoveSpeed = speed;
                    engagerMoveUntil = now + back / Mathf.Max(0.2f, speed) + 0.1f;
                    LogCooldownMove("Backstep", back, distance, p);
                    return RunEngagerCooldownMove(target, toTarget, distance, p);
                }
            }

            // Strafe: slow arc around the target at constant distance, side alternates, net arc capped.
            float seconds = p.RandomRange(p.engagerStrafeSeconds) * variety;
            float strafeSpeed = walkSpeed * p.engagerStrafeSpeedFactor * variety;
            float arcMax = Mathf.Min(p.engagerStrafeMaxArc * variety, strafeSpeed * seconds / Mathf.Max(0.5f, distance) * Mathf.Rad2Deg);
            if (arcMax < 3f)
                return false;

            float sign = engagerMoveLastSign == 0f ? (Random.value < 0.5f ? 1f : -1f) : engagerMoveLastSign;
            if (Random.value < p.engagerStrafeFlipChance)
                sign = -sign;
            if (Mathf.Abs(engagerMoveNetArc + sign * arcMax) > p.engagerStrafeMaxNetArc)
                sign = -sign;

            engagerMoveKind = EngagerMoveKind.Strafe;
            engagerMoveSign = sign;
            engagerMoveLastSign = sign;
            engagerMoveArcDone = 0f;
            engagerMoveMaxArc = arcMax;
            engagerMoveSpeed = strafeSpeed;
            engagerMoveUntil = now + seconds;
            engagerMoveStart = now;
            engagerMoveStartPos = transform.position;
            engagerMovePlanned = arcMax * Mathf.Deg2Rad * Mathf.Max(0.5f, distance);
            LogCooldownMove("Strafe", arcMax * sign, distance, p);
            return RunEngagerCooldownMove(target, toTarget, distance, p);
        }

        private bool RunEngagerCooldownMove(Transform target, Vector3 toTarget, float distance, DM_EnemyEngagementProfile p)
        {
            if (engagerMoveKind == EngagerMoveKind.Backstep)
            {
                float eased = EaseInOutFactor(Time.time - engagerMoveStart, engagerMoveDistanceLeft / Mathf.Max(0.01f, engagerMoveTotal), p.moveEaseSeconds);
                float step = Mathf.Min(engagerMoveSpeed * eased * Time.deltaTime, engagerMoveDistanceLeft);
                if (step <= 0.0005f)
                {
                    EndEngagerCooldownMove(p, Time.time);
                    return false;
                }

                StepFlat(-toTarget, engagerMoveSpeed * eased, step);
                engagerMoveDistanceLeft -= step;
                if (engagerMoveDistanceLeft <= 0.001f)
                    EndEngagerCooldownMove(p, Time.time);
                return true;
            }

            // Strafe: rotate the current offset around the target by this frame's angle, move toward that point.
            float strafeEased = EaseInOutFactor(Time.time - engagerMoveStart, 1f - engagerMoveArcDone / Mathf.Max(1f, engagerMoveMaxArc), p.moveEaseSeconds);
            float dTheta = engagerMoveSpeed * strafeEased * Time.deltaTime / Mathf.Max(0.5f, distance) * Mathf.Rad2Deg;
            dTheta = Mathf.Min(dTheta, engagerMoveMaxArc - engagerMoveArcDone);
            if (dTheta <= 0.01f)
            {
                EndEngagerCooldownMove(p, Time.time);
                return false;
            }

            Vector3 offset = -toTarget;
            Vector3 desired = target.position + Quaternion.AngleAxis(engagerMoveSign * dTheta, Vector3.up) * offset;
            Vector3 dir = desired - transform.position;
            dir.y = 0f;
            float len = dir.magnitude;
            if (len < 0.0005f)
                return true;

            StepFlat(dir, len / Mathf.Max(0.0001f, Time.deltaTime), len);
            engagerMoveArcDone += dTheta;
            engagerMoveNetArc += engagerMoveSign * dTheta;
            if (engagerMoveArcDone >= engagerMoveMaxArc - 0.01f)
                EndEngagerCooldownMove(p, Time.time);
            return true;
        }

        private void LogCooldownMove(string kind, float amount, float distance, DM_EnemyEngagementProfile p)
        {
            Project.Combat.DM_CombatCoreProfile core = Project.Combat.DM_CombatCoreProfile.Live;
            if (core != null && core.logInterruptsInPlay)
                Debug.Log($"[CooldownMove] {name} kind={kind} amount={amount:0.0} dist={distance:0.00} net={engagerMoveNetArc:0} readyIn={(combat != null ? combat.AttackReadyIn : 0f):0.00}", this);
        }

        private void EndEngagerCooldownMove(DM_EnemyEngagementProfile p, float now, string reason = "complete")
        {
            Project.Combat.DM_CombatCoreProfile core = Project.Combat.DM_CombatCoreProfile.Live;
            if (engagerMoveKind != EngagerMoveKind.None && core != null && core.logInterruptsInPlay)
            {
                Vector3 moved = transform.position - engagerMoveStartPos;
                moved.y = 0f;
                Debug.Log($"[CooldownMoveEnd] {name} kind={engagerMoveKind} moved={moved.magnitude:0.00} planned={engagerMovePlanned:0.00} elapsed={now - engagerMoveStart:0.00} reason={reason}", this);
            }

            engagerMoveKind = EngagerMoveKind.None;
            nextEngagerMoveAllowed = now + (p != null ? Mathf.Max(0.2f, p.RandomRange(p.engagerMoveCooldown)) : 1.5f);
            ClearLocomotion();
        }
    
        // ── Dodge roll motion (brain-owned; direction picked in EnemyInvectorCombatBridge.TryBeginDodgeRoll) ──
        // The Invector Roll clip is a forward roll played in place on these kinematic, transform-driven enemies (root motion is off,
        // Rigidbody velocity is ignored on a kinematic body), so the AI moves the body. Movement is driven by the animator state's
        // normalized time (Roll / DM_DodgeBack), so it runs for the whole clip and its ease-out ends with the clip, not on a fixed timer.
        // EnemyAiController.Update calls TickDodgeRoll first, so no state change can cancel the movement.
        private Vector3 dodgeRollDir;
        private float dodgeRollStart;
        private float dodgeRollUntil;          // hard timeout
        private float dodgeRollDistanceTotal;
        private float dodgeRollPlanned;
        private float dodgeRollMoved;
        private Vector3 dodgeRollOrigin;
        private Vector3 dodgeRollToTarget;
        private Transform dodgeRollTarget;
        private bool dodgeRollBackClip;          // true for the "faces the threat" clips (back / left / right)
        private int dodgeRollClipId;               // 0 roll, 1 back, 2 left, 3 right
        private Vector3 dodgeRollFace;
        /// <summary>True while a dodge roll owns the enemy (hit reactions are ignored then when the brain profile says so).</summary>
        public bool IsDodgeRollActive => dodgeRollUntil > 0f;
        public bool DodgeRollBlocksHitReactions => dodgeRollUntil > 0f && DM_EnemyBrainProfile.Live != null && DM_EnemyBrainProfile.Live.dodgeRollBlocksHitReactions;
        private float dodgeRollFadeAt;
        private bool dodgeRollAnimSeen;
        private float dodgeRollLastProgress;
        private float dodgeRollAnimSeenAt;
        private float dodgeRollEndNormalized;
        private float dodgeRollTimerSeconds;
        private string dodgeRollStopReason;

        private void BeginDodgeRollMotion(Transform target, Vector3 direction, float duration, DM_EnemyBrainProfile bp, bool backClip, int clipId = 0)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
                return;

            if (dodgeStrikeUntil > 0f)
                FinishDodgeStrike("roll", DM_EnemyBrainProfile.Live); // a defensive roll always wins over a dodge strike

            dodgeRollDir = direction.normalized;
            dodgeRollStart = Time.time;
            dodgeRollTimerSeconds = Mathf.Max(0.2f, duration);
            dodgeRollUntil = Time.time + dodgeRollTimerSeconds * 2f + 0.5f;
            dodgeRollBackClip = backClip;
            dodgeRollClipId = backClip ? Mathf.Max(1, clipId) : 0;
            dodgeRollFadeAt = float.MaxValue;
            dodgeRollEndNormalized = Mathf.Clamp(dodgeRollClipId == 1 ? bp.dodgeBackMoveEndNormalized : dodgeRollClipId >= 2 ? bp.dodgeSideMoveEndNormalized : bp.dodgeRollMoveEndNormalized, 0.3f, 1f);
            dodgeRollDistanceTotal = Mathf.Max(0f, dodgeRollClipId == 1 ? bp.dodgeBackDistance : dodgeRollClipId >= 2 ? bp.dodgeSideDistance : bp.dodgeRollDistance);
            dodgeRollPlanned = dodgeRollDistanceTotal;
            dodgeRollMoved = 0f;
            dodgeRollAnimSeen = false;
            dodgeRollAnimSeenAt = 0f;
            dodgeRollLastProgress = 0f;
            dodgeRollStopReason = "complete";
            dodgeRollOrigin = transform.position;
            dodgeRollTarget = target;
            dodgeRollToTarget = target != null ? target.position - transform.position : -dodgeRollDir;
            dodgeRollToTarget.y = 0f;
            // The backward dodge clip faces the threat while the body moves away; rolls face the roll direction.
            Vector3 flatToTarget = dodgeRollToTarget.sqrMagnitude > 0.0001f ? dodgeRollToTarget.normalized : -dodgeRollDir;
            dodgeRollFace = dodgeRollClipId == 1 ? -dodgeRollDir : dodgeRollClipId >= 2 ? flatToTarget : dodgeRollDir;
            if (DM_EnemyBrainProfile.Live == null || DM_EnemyBrainProfile.Live.dodgeTurnDegPerSec <= 0f)
                transform.rotation = Quaternion.LookRotation(dodgeRollFace, Vector3.up); // 2026-10-12: else turned smoothly in TickDodgeRoll
            NoteQuickStep(dodgeRollTimerSeconds);
            Project.Combat.DM_CombatCoreProfile core = Project.Combat.DM_CombatCoreProfile.Live;
            if (core != null && core.logInterruptsInPlay)
                Debug.Log($"[DodgeRollStart] {name} clip={Project.AI.Invector.EnemyInvectorCombatBridge.RollClipName(dodgeRollClipId)} planned={dodgeRollPlanned:0.00}", this);
        }

        /// <summary>2026-10-12: dodge turn step (deg this frame) from DM_EnemyBrainProfile.dodgeTurnDegPerSec; fallback when it is 0.</summary>
        private float DodgeTurnStep(float fallbackDegPerSec)
        {
            DM_EnemyBrainProfile b = DM_EnemyBrainProfile.Live;
            float r = b != null ? b.dodgeTurnDegPerSec : 0f;
            return (r > 0f ? r : fallbackDegPerSec) * Time.deltaTime;
        }

        private Quaternion DodgeTurn(Vector3 face)
        {
            Quaternion want = Quaternion.LookRotation(face, Vector3.up);
            DM_EnemyBrainProfile b = DM_EnemyBrainProfile.Live;
            return b == null || b.dodgeTurnDegPerSec <= 0f ? want : Quaternion.RotateTowards(transform.rotation, want, b.dodgeTurnDegPerSec * Time.deltaTime);
        }

        /// <summary>Runs while a dodge roll is active (called first in Update). Returns true while it owns the enemy.</summary>
        private bool TickDodgeRoll()
        {
            if (dodgeRollUntil <= 0f)
                return false;

            float now = Time.time;
            float elapsed = now - dodgeRollStart;

            // Progress 0..1 of the movement: follows the animator state; timer only if the state is never seen.
            float progress;
            if (combatBridge != null && combatBridge.TryGetDodgeProgress(dodgeRollClipId, out float normalized))
            {
                if (!dodgeRollAnimSeen)
                {
                    dodgeRollAnimSeen = true;
                    dodgeRollAnimSeenAt = now;
                }

                progress = Mathf.Clamp01(normalized / dodgeRollEndNormalized);
                dodgeRollLastProgress = progress;
            }
            else if (dodgeRollAnimSeen)
            {
                if (dodgeRollLastProgress < 0.8f)
                {
                    // shakefix: the clip was cut by another animator transition (not finished). Never jump the rest of the distance in one frame: end the roll here.
                    Project.Combat.DM_CombatCoreProfile ccl = Project.Combat.DM_CombatCoreProfile.Live;
                    if (ccl != null && ccl.logInterruptsInPlay)
                    {
                        Animator rollAn = GetComponentInChildren<Animator>();
                        var rci = rollAn != null ? rollAn.GetCurrentAnimatorClipInfo(0) : null;
                        Debug.Log($"[RollClipLeft] {name} clip={Project.AI.Invector.EnemyInvectorCombatBridge.RollClipName(dodgeRollClipId)} lastProgress={dodgeRollLastProgress:0.00} nowClip={(rci != null && rci.Length > 0 && rci[0].clip != null ? rci[0].clip.name : "none")} grounded={(rollAn != null && HasAnimParam(rollAn, "IsGrounded") ? rollAn.GetBool("IsGrounded") ? 1 : 0 : -1)} sliding={(rollAn != null && HasAnimParam(rollAn, "IsSliding") ? rollAn.GetBool("IsSliding") ? 1 : 0 : -1)} state={state} attacking={(IsInAttackSequence ? 1 : 0)} stagger={(ragdollBridge != null && ragdollBridge.IsHitStaggerActive ? 1 : 0)}", this);
                    }

                    FinishDodgeRoll("clipleft");
                    return false;
                }

                progress = 1f; // the clip left its state near its end: the animation is over
            }
            else if (elapsed > 0.35f)
            {
                // clipfix: the dodge clip state was never entered (animator busy). Do not slide the body without an animation: end the roll.
                Project.Combat.DM_CombatCoreProfile ncf = Project.Combat.DM_CombatCoreProfile.Live;
                Animator ncAn = GetComponentInChildren<Animator>();
                if (ncf != null && ncf.logInterruptsInPlay && ncAn != null)
                {
                    var nci = ncAn.GetCurrentAnimatorClipInfo(0);
                    Debug.Log($"[RollClipLeft] {name} NOCLIP clip={Project.AI.Invector.EnemyInvectorCombatBridge.RollClipName(dodgeRollClipId)} nowClip={(nci.Length > 0 && nci[0].clip != null ? nci[0].clip.name : "none")} enabled={(ncAn.isActiveAndEnabled ? 1 : 0)} inTransition={(ncAn.IsInTransition(0) ? 1 : 0)} speed={ncAn.speed:0.00} state={state} attacking={(IsInAttackSequence ? 1 : 0)} stagger={(ragdollBridge != null && ragdollBridge.IsHitStaggerActive ? 1 : 0)}", this);
                }

                FinishDodgeRoll("noclip");
                return false;
            }
            else
            {
                progress = 0f; // crossfade into the state has not been applied yet
            }

            if (progress >= 1f || now >= dodgeRollUntil)
            {
                FinishDodgeRoll(progress >= 1f ? dodgeRollStopReason : "timeout");
                return false;
            }

            if (dodgeRollBackClip)
            {
                if (dodgeRollClipId >= 2 && dodgeRollTarget != null)
                {
                    Vector3 faceNow = dodgeRollTarget.position - transform.position;
                    faceNow.y = 0f;
                    if (faceNow.sqrMagnitude > 0.01f)
                        dodgeRollFace = faceNow.normalized;
                }

                transform.rotation = DodgeTurn(dodgeRollFace);
                float blendOutSeconds = dodgeRollClipId >= 2 ? DM_EnemyBrainProfile.Live.dodgeSideBlendOut : DM_EnemyBrainProfile.Live.dodgeBackBlendOut;
                if (progress > 0.001f && dodgeRollFadeAt == float.MaxValue && elapsed >= dodgeRollTimerSeconds - Mathf.Max(0.01f, blendOutSeconds))
                {
                    dodgeRollFadeAt = now;
                    combatBridge?.EndDodgeBackClip(blendOutSeconds);
                }
            }
            else
            {
                // Side roll: turn back toward the target during the exit blend so the enemy sees it again right after the roll.
                if (progress > 0.7f && dodgeRollTarget != null)
                {
                    Vector3 face = dodgeRollTarget.position - transform.position;
                    face.y = 0f;
                    if (face.sqrMagnitude > 0.01f)
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(face.normalized, Vector3.up), DodgeTurnStep(540f));
                }
                else
                {
                    transform.rotation = DodgeTurn(dodgeRollDir);
                }
            }

            float wanted = Mathf.SmoothStep(0f, dodgeRollDistanceTotal, progress);
            float step = Mathf.Max(0f, wanted - dodgeRollMoved);
            if (step > 0.0001f)
            {
                Vector3 pos = transform.position;
                Vector3 chest = pos + Vector3.up * 0.9f;
                string stop = null;
                if (Physics.Raycast(chest, dodgeRollDir, out RaycastHit hit, step + 0.35f, ~0, QueryTriggerInteraction.Ignore)
                    && hit.transform != null && hit.transform.root != transform.root)
                    stop = "blocked:" + hit.collider.name;

                Vector3 next = pos + dodgeRollDir * step;
                if (stop == null && !Physics.Raycast(next + Vector3.up * 0.6f, Vector3.down, 1.6f, ~0, QueryTriggerInteraction.Ignore))
                    stop = "ledge";

                if (stop != null)
                {
                    dodgeRollDistanceTotal = dodgeRollMoved; // stop at the wall / ledge / body
                    dodgeRollStopReason = stop;
                }
                else
                {
                    transform.position = next;
                    NoteMover("Roll", step);
                    dodgeRollMoved += step;
                }
            }

            currentLocomotionSpeed = 0f;
            currentLocalMoveDirection = Vector3.zero;
            return true;
        }

        private static bool HasAnimParam(Animator an, string paramName)
        {
            foreach (AnimatorControllerParameter ap in an.parameters)
                if (ap.name == paramName)
                    return true;
            return false;
        }

        private void FinishDodgeRoll(string reason)
        {
            float elapsed = Time.time - dodgeRollStart;
            float animSeconds = dodgeRollAnimSeen ? Time.time - dodgeRollAnimSeenAt : 0f;
            dodgeRollUntil = 0f;
            // Direction checks use the direction to the target at roll start (the player may move during the roll); facing uses the target now.
            Vector3 endToTarget = dodgeRollTarget != null ? dodgeRollTarget.position - transform.position : dodgeRollToTarget;
            endToTarget.y = 0f;
            dodgeRollToTarget.y = 0f;

            Project.Combat.DM_CombatCoreProfile core = Project.Combat.DM_CombatCoreProfile.Live;
            if (core == null || !core.logInterruptsInPlay)
                return;

            Vector3 toTarget = dodgeRollToTarget.sqrMagnitude > 0.0001f ? dodgeRollToTarget.normalized : -dodgeRollDir;
            Vector3 disp = transform.position - dodgeRollOrigin;
            disp.y = 0f;
            float dispDot = disp.sqrMagnitude > 0.0004f ? Vector3.Dot(disp.normalized, toTarget) : 0f;
            Debug.Log($"[DodgeRoll] {name} clip={Project.AI.Invector.EnemyInvectorCombatBridge.RollClipName(dodgeRollClipId)} clipSeen={(dodgeRollAnimSeen ? 1 : 0)} dirDot={Vector3.Dot(dodgeRollDir, toTarget):0.00} dispDot={dispDot:0.00} moved={disp.magnitude:0.00} planned={dodgeRollPlanned:0.00} elapsed={elapsed:0.00} animSec={animSeconds:0.00} reason={reason} facingDot={Vector3.Dot(transform.forward, endToTarget.sqrMagnitude > 0.0001f ? endToTarget.normalized : toTarget):0.00}", this);
        }

        // -- Dodge strike (brain decision, HumanM@Dodge01): a short weave around the target that hands over to the normal swing --
        // Owner: DMEnemyBrain.TryRollDodgeStrike (chance / cooldown), DM_EnemyBrainProfile "Dodge strike" (all numbers).
        // Only the engager (attack token holder, so the one-attacker cap holds) in the melee band starts it; the weave keeps the distance.
        private float dodgeStrikeUntil;
        private float dodgeStrikeStart;
        private float dodgeStrikeTotal;
        private float dodgeStrikeMoved;
        private float dodgeStrikeSign;
        private float dodgeStrikeRadius;
        private float dodgeStrikeStartDistance;
        private float dodgeStrikeEndedAt = -99f;
        private bool dodgeStrikeAnimSeen;
        private Vector3 dodgeStrikeStartPos;
        private Transform dodgeStrikeTarget;
        private string dodgeStrikeReason;

        /// <summary>True while the dodge strike weave runs; the token holder is not handed off during it.</summary>
        public bool IsDodgeStrikeActive => dodgeStrikeUntil > 0f;

        private bool TryBeginDodgeStrike(Transform target, float distance, float effective, float minSep)
        {
            DM_EnemyBrainProfile bp = DM_EnemyBrainProfile.Live;
            if (bp == null || !bp.enableDodgeStrike || brain == null || combat == null || combatBridge == null || target == null)
                return false;

            if (dodgeStrikeUntil > 0f || combat.AttackReadyIn > 0.05f || IsStationary || IsInAttackSequence ||
                distance < minSep || distance > effective * bp.dodgeStrikeMaxRangeFactor || distance > ResolveSwingStartDistance(target) + 0.15f ||
                combatBridge.IsArmedRangedPreferred() ||
                (ragdollBridge != null && (ragdollBridge.IsHitStaggerActive || ragdollBridge.IsKnockdownActive)) ||
                brain.IsMissRecoveryActive || combat.WantsMeleeReposition || combat.WantsForcedMissStep)
                return false;

            float scale = AppliedDefinition != null ? AppliedDefinition.dodgeStrikeChanceMultiplier : 1f;
            if (!brain.TryRollDodgeStrike(scale))
                return false;

            if (!combatBridge.TryBeginDodgeStrikeClip(bp.dodgeStrikeSeconds, bp.dodgeStrikeBlendIn))
                return false;

            Vector3 offset = transform.position - target.position;
            offset.y = 0f;
            dodgeStrikeRadius = Mathf.Max(0.5f, offset.magnitude);
            dodgeStrikeTotal = Mathf.Min(Random.Range(bp.dodgeStrikeSideDistance.x, bp.dodgeStrikeSideDistance.y), dodgeStrikeRadius * 1.2f);
            dodgeStrikeSign = Random.value < 0.5f ? 1f : -1f;
            dodgeStrikeMoved = 0f;
            dodgeStrikeStart = Time.time;
            dodgeStrikeUntil = Time.time + bp.dodgeStrikeSeconds * 2f + 0.5f;
            dodgeStrikeAnimSeen = false;
            dodgeStrikeStartPos = transform.position;
            dodgeStrikeStartDistance = distance;
            dodgeStrikeTarget = target;
            dodgeStrikeReason = "handover";
            creeping = false;
            ClearLocomotion();
            NoteQuickStep(bp.dodgeStrikeSeconds);
            return true;
        }

        private bool TickDodgeStrike()
        {
            DM_EnemyBrainProfile bp = DM_EnemyBrainProfile.Live;
            float now = Time.time;
            string abort = null;
            if (dodgeStrikeTarget == null)
                abort = "no-target";
            else if (ragdollBridge != null && (ragdollBridge.IsHitStaggerActive || ragdollBridge.IsKnockdownActive))
                abort = "hit";
            else if (IsInAttackSequence)
                abort = "attack";
            else if (now >= dodgeStrikeUntil)
                abort = "timeout";

            float progress = 0f;
            if (abort == null)
            {
                if (combatBridge != null && combatBridge.TryGetDodgeStrikeProgress(out float normalized))
                {
                    dodgeStrikeAnimSeen = true;
                    progress = normalized;
                }
                else if (dodgeStrikeAnimSeen)
                {
                    progress = 1f;
                }
                else if (now - dodgeStrikeStart > 0.3f)
                {
                    progress = (now - dodgeStrikeStart) / Mathf.Max(0.2f, bp.dodgeStrikeSeconds);
                }

                if (progress >= bp.dodgeStrikeAttackAtNormalized)
                    abort = "handover";
            }

            if (abort != null)
            {
                FinishDodgeStrike(abort, bp);
                return false;
            }

            // Weave: move around the target at constant radius, eased so it ends with moveEnd of the clip.
            Vector3 toTarget = dodgeStrikeTarget.position - transform.position;
            toTarget.y = 0f;
            float wanted = Mathf.SmoothStep(0f, dodgeStrikeTotal, Mathf.Clamp01(progress / Mathf.Max(0.05f, bp.dodgeStrikeMoveEndNormalized)));
            float step = Mathf.Max(0f, wanted - dodgeStrikeMoved);
            if (step > 0.0001f && toTarget.sqrMagnitude > 0.01f)
            {
                // Tangential step only (perpendicular to the target direction): a target that moves does not drag the enemy along, and the distance stays about constant for a 0.6-1.0 m weave.
                Vector3 tangent = Vector3.Cross(Vector3.up, toTarget.normalized) * dodgeStrikeSign;
                Vector3 desired = transform.position + tangent * step;
                Vector3 dir = desired - transform.position;
                float len = dir.magnitude;
                string stop = null;
                if (len > 0.0001f)
                {
                    dir /= len;
                    if (Physics.Raycast(transform.position + Vector3.up * 0.9f, dir, out RaycastHit hit, len + 0.35f, ~0, QueryTriggerInteraction.Ignore) &&
                        hit.transform != null && hit.transform.root != transform.root && hit.transform.root != dodgeStrikeTarget.root)
                        stop = "blocked:" + hit.collider.name;
                    else if (!Physics.Raycast(desired + Vector3.up * 0.6f, Vector3.down, 1.6f, ~0, QueryTriggerInteraction.Ignore))
                        stop = "ledge";
                }

                if (stop != null)
                {
                    dodgeStrikeTotal = dodgeStrikeMoved;
                    dodgeStrikeReason = stop;
                }
                else
                {
                    transform.position = desired;
                    NoteMover("DodgeStrike", step);
                    dodgeStrikeMoved += step;
                }
            }

            if (toTarget.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(toTarget.normalized, Vector3.up), bp.dodgeStrikeTurnRate * Time.deltaTime);

            currentLocomotionSpeed = 0f;
            currentLocalMoveDirection = Vector3.zero;
            return true;
        }

        private void FinishDodgeStrike(string reason, DM_EnemyBrainProfile bp)
        {
            dodgeStrikeUntil = 0f;
            dodgeStrikeEndedAt = Time.time;
            if (reason != "attack")
                combatBridge?.EndDodgeBackClip(bp != null ? bp.dodgeStrikeBlendOut : 0.12f);

            Project.Combat.DM_CombatCoreProfile core = Project.Combat.DM_CombatCoreProfile.Live;
            if (core != null && core.logInterruptsInPlay)
            {
                Vector3 moved = transform.position - dodgeStrikeStartPos;
                moved.y = 0f;
                float dist = dodgeStrikeTarget != null ? HorizontalDistance(transform.position, dodgeStrikeTarget.position) : 0f;
                Debug.Log($"[DodgeStrike] {name} moved={moved.magnitude:0.00} planned={dodgeStrikeTotal:0.00} dist {dodgeStrikeStartDistance:0.00}->{dist:0.00} elapsed={Time.time - dodgeStrikeStart:0.00} reason={(reason == "handover" ? dodgeStrikeReason : reason)} anim={(dodgeStrikeAnimSeen ? 1 : 0)} tokenHeld={(engagementState.Role == DMEngagementRole.Engager ? 1 : 0)}", this);
            }
        }

        /// <summary>Called when a swing is running: logs how soon after a dodge strike the swing started.</summary>
        private void NoteDodgeStrikeSwing()
        {
            if (dodgeStrikeEndedAt < 0f)
                return;

            Project.Combat.DM_CombatCoreProfile core = Project.Combat.DM_CombatCoreProfile.Live;
            if (core != null && core.logInterruptsInPlay && Time.time - dodgeStrikeEndedAt < 1.5f)
                Debug.Log($"[DodgeStrikeSwing] {name} swing started {Time.time - dodgeStrikeEndedAt:0.00}s after the dodge", this);

            dodgeStrikeEndedAt = -99f;
        }
    }
}
