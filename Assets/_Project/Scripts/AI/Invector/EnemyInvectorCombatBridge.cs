using Invector;
using Invector.vCharacterController;
using Invector.vMelee;
using Invector.vShooter;
using Project.AI;
using Project.Combat;
using Project.Data;
using UnityEngine;

namespace Project.AI.Invector
{
    /// <summary>
    /// Drives Invector melee/shooter attacks for humanoid enemies. Damage flows through Invector hitboxes.
    /// </summary>
    [DefaultExecutionOrder(110)]
    [DisallowMultipleComponent]
    public class EnemyInvectorCombatBridge : MonoBehaviour
    {
        [SerializeField] private float defaultMeleeDuration = 0.85f;
        [SerializeField] private float defaultRangedDuration = 0.35f;
        [SerializeField] private float defaultUnarmedDuration = 0.55f;
        [Tooltip("Distance at which ranged enemies stop chasing and start shooting.")]
        [SerializeField] private float rangedEngageRange = 12f;
        [Tooltip("Ranged hold rule: distance a ranged-preferred enemy holds from the player. 0 = use the EnemyDefinition value, or auto (80% of Ranged Engage Range). Always kept beyond melee reach.")]
        [SerializeField] private float rangedHoldDistance;
        [Tooltip("Ranged enemies back off when the player is closer than hold distance x this fraction (the EnemyDefinition value wins when set).")]
        [SerializeField] [Range(0.2f, 0.95f)] private float rangedRetreatFraction = 0.6f;
        [Tooltip("Kiting archetypes (brain archetype Kiter) multiply the retreat fraction by this, so they retreat sooner.")]
        [SerializeField] [Range(1f, 2f)] private float kiterRetreatBoost = 1.5f;
        [Tooltip("Backpedal speed while retreating, as a fraction of walk speed.")]
        [SerializeField] [Range(0.3f, 1.5f)] private float rangedRetreatSpeedMultiplier = 0.8f;
        [Tooltip("Seconds the aim pose is held after the enemy leaves a ranged engagement. Also the aim-in time: the first shot only fires once the aim pose has been held this long.")]
        [SerializeField] private float aimHoldDuration = 1.5f;
        [Tooltip("Max degrees the chest bone tilts up/down to track the target vertically while aiming. Keeps the rifle pointed at the player's torso rather than straight ahead.")]
        [SerializeField] [Range(0f, 40f)] private float aimChestPitchLimit = 25f;
        [Tooltip("How fast the chest pitch blends in and out (degrees per second).")]
        [SerializeField] private float aimChestPitchSpeed = 90f;
        [Tooltip("0 = perfect aim every shot, 1 = always misses. Scales a random lateral+vertical offset applied to each shot's aim point.")]
        [SerializeField] [Range(0f, 1f)] private float missRate = 0.25f;
        [SerializeField] private ItemData enemyPistolAmmo;
        [SerializeField] private ItemData enemyRifleAmmo;

        private const string StandardAmmoPath = "Assets/_Project/Data/Items/Ammo/DMAmmoFxProfile_Standard.asset";
        private const string PlasmaAmmoPath = "Assets/_Project/Data/Items/Ammo/DMAmmoFxProfile_Plasma.asset";

        private vThirdPersonController _controller;
        private vMeleeManager _meleeManager;
        private vShooterManager _shooterManager;
        private EnemyInvectorLoadoutBridge _loadoutBridge;
        private EnemyInvectorBootstrap _bootstrap;
        private EnemyCombat _enemyCombat;
        private EnemyAiController _aiController;
        private Animator _animator;
        private bool _isAimStanceActive;
        private float _aimHoldTimer;
        private float _aimStanceOnTime = -1f;
        private float _aimRequestUntil;
        private const float AimRequestWindow = 0.5f;
        private float _chestPitchCurrent;
        private Transform _chestBone;

        private static readonly int AttackIdHash    = Animator.StringToHash("AttackID");
        private static readonly int WeakAttackHash  = Animator.StringToHash("WeakAttack");
        private static readonly int MoveSetIdHash   = Animator.StringToHash("MoveSet_ID");
        private static readonly int IsAimingHash    = Animator.StringToHash("IsAiming");
        private static readonly int CanAimHash      = Animator.StringToHash("CanAim");
        private static readonly int UpperBodyIdHash = Animator.StringToHash("UpperBody_ID");
        private static readonly int ShotIdHash      = Animator.StringToHash("Shot_ID");

        public bool UsesInvectorDamageApplication => true;
        public bool LastAttackWasRanged { get; private set; }

        private void Awake()
        {
            _bootstrap = GetComponent<EnemyInvectorBootstrap>();
            _controller = GetComponent<vThirdPersonController>();
            _meleeManager = GetComponent<vMeleeManager>();
            _shooterManager = GetComponent<vShooterManager>();
            _loadoutBridge = GetComponent<EnemyInvectorLoadoutBridge>();
            _enemyCombat = GetComponent<EnemyCombat>();
            _aiController = GetComponent<EnemyAiController>();
            _animator = _controller != null ? _controller.animator : GetComponentInChildren<Animator>(true);


            _bootstrap?.EnsureInvectorInitialized();
            EnsureEnemyAmmoReferences();
        }

        private void EnsureEnemyAmmoReferences()
        {
#if UNITY_EDITOR
            if (enemyPistolAmmo == null)
                enemyPistolAmmo = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>(StandardAmmoPath);
            if (enemyRifleAmmo == null)
                enemyRifleAmmo = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>(PlasmaAmmoPath);
#endif
        }

        private void Update()
        {
            UpdateRangedAimStance();
            TickGuardStamina();
        }

        // ---- 2026-10-13 guard stamina (DM_EnemyBrainProfile "Enemy guard stamina") ----
        private float _guardStamina = -1f;
        private float _guardLastSpend = -999f;
        private float _guardHoldStart = -1f;

        private Project.AI.DM_EnemyBrainProfile GuardProfile
        {
            get
            {
                var b = GetComponent<Project.AI.DMEnemyBrain>();
                return b != null && b.Profile != null ? b.Profile : Project.AI.DM_EnemyBrainProfile.Live;
            }
        }

        public float GuardStaminaMax
        {
            get
            {
                var ai = GetComponent<EnemyAiController>();
                var def = ai != null ? ai.AppliedDefinition : null;
                if (def != null && def.guardStaminaMax > 0f) return def.guardStaminaMax;
                var p = GuardProfile;
                return p != null ? p.enemyGuardStaminaMax : 60f;
            }
        }

        public float GuardStamina { get { if (_guardStamina < 0f) _guardStamina = GuardStaminaMax; return _guardStamina; } }

        /// <summary>Spends guard stamina; empty = the guard breaks (and stays down until it refills past the block minimum).</summary>
        public void SpendGuardStamina(float amount)
        {
            if (amount <= 0f) return;
            _guardStamina = Mathf.Max(0f, GuardStamina - amount);
            _guardLastSpend = Time.time;
            if (_guardStamina <= 0.01f && _blockActive)
            {
                var ai = GetComponent<EnemyAiController>();
                if (ai != null) ai.BreakGuard(); else EndBlock();
            }
        }

        private void TickGuardStamina()
        {
            var p = GuardProfile;
            if (p == null) return;
            float max = GuardStaminaMax;
            if (_guardStamina < 0f) _guardStamina = max;
            if (_blockActive)
            {
                if (_guardHoldStart < 0f) _guardHoldStart = Time.time;
                float held = Time.time - _guardHoldStart;
                float rate = Mathf.Min(p.enemyBlockHoldCostBase * Mathf.Pow(Mathf.Max(1f, p.enemyBlockHoldCostGrowth), held), p.enemyBlockHoldCostCap);
                SpendGuardStamina(rate * Time.deltaTime);
            }
            else
            {
                _guardHoldStart = -1f;
                if (Time.time - _guardLastSpend >= p.enemyGuardRegenDelay)
                    _guardStamina = Mathf.Min(max, _guardStamina + p.enemyGuardRegenPerSec * Time.deltaTime);
            }
        }


        // Runs after the Animator so we can post-process the chest bone pitch without
        // fighting the animator each frame. Only active while the aim stance is held.
        private void LateUpdate()
        {
            UpdateChestAimPitch();
        }

        // Keeps isStrafing, UpperBody_ID, IsAiming and CanAim in sync with the AI's engagement
        // state. The aim pose is held for aimHoldDuration seconds after the engagement ends so
        // the enemy doesn't snap out of the aiming animation immediately after each shot.
        private void UpdateRangedAimStance()
        {
            if (_controller == null || _animator == null) return;

            // A deferred shot (TryDeferRangedAttack) also raises the stance so the aim-in can complete.
            bool inEngagement = (_aiController != null && _aiController.IsInRangedEngagement)
                || (Time.time < _aimRequestUntil && HasRangedWeaponEquipped());

            if (inEngagement)
            {
                // Reset the hold timer whenever actively engaged — it only counts down after.
                _aimHoldTimer = aimHoldDuration;
            }
            else if (_aimHoldTimer > 0f)
            {
                _aimHoldTimer -= Time.deltaTime;
            }

            bool shouldAim = inEngagement || _aimHoldTimer > 0f;

            if (shouldAim == _isAimStanceActive) return;
            _isAimStanceActive = shouldAim;

            _aimStanceOnTime = shouldAim ? Time.time : -1f;

            if (shouldAim)
            {
                float weaponId = ResolveUpperBodyId();
                _animator.SetFloat(UpperBodyIdHash, weaponId);
                _animator.SetFloat(ShotIdHash,      weaponId);
                _animator.SetBool(IsAimingHash, true);
                _animator.SetBool(CanAimHash,   true);
                _controller.isStrafing = true;
            }
            else
            {
                _animator.SetBool(IsAimingHash, false);
                _animator.SetBool(CanAimHash,   false);
                _controller.isStrafing = false;
            }
        }

        // Post-animator chest pitch so the weapon visually tracks the target vertically.
        // Only the chest bone is rotated (no spine) to avoid the full-body lean seen previously.
        private void UpdateChestAimPitch()
        {
            if (_animator == null) return;

            if (_chestBone == null)
                _chestBone = _animator.GetBoneTransform(HumanBodyBones.Chest);
            if (_chestBone == null) return;

            float targetPitch = 0f;

            if (_isAimStanceActive)
            {
                Transform target = _enemyCombat?.CurrentTarget;
                if (target != null)
                {
                    Vector3 aimOrigin = _chestBone.position;
                    Vector3 toTarget  = ResolveAimPoint(target) - aimOrigin;
                    if (toTarget.sqrMagnitude > 0.01f)
                    {
                        float vertical   = toTarget.y;
                        float horizontal = new Vector2(toTarget.x, toTarget.z).magnitude;
                        float angle      = Mathf.Atan2(vertical, horizontal) * Mathf.Rad2Deg;
                        targetPitch      = Mathf.Clamp(angle, -aimChestPitchLimit, aimChestPitchLimit);
                    }
                }
            }

            _chestPitchCurrent = Mathf.MoveTowards(
                _chestPitchCurrent, targetPitch, aimChestPitchSpeed * Time.deltaTime);

            // 2026-10-13 (e1): barrel-to-target correction (yaw + pitch) replaces the pitch-only tilt when enabled.
            DM_EnemyBrainProfile acp = GuardProfile;
            if (acp != null && acp.enemyAimCorrectionEnabled)
            {
                Quaternion want = Quaternion.identity;
                Transform aimTarget = _isAimStanceActive ? _enemyCombat?.CurrentTarget : null;
                Transform barrel = aimTarget != null && HasRangedWeaponEquipped() ? ResolveProjectileMuzzle() : null;
                if (barrel != null && barrel != fallbackMuzzle)
                {
                    // 2026-10-13 (i): solve about the chest pivot, not the barrel. The correction rotates the chest, which also
                    // moves the barrel; up close the barrel-only FromTo over-rotated (pistols pointed at the feet).
                    Vector3 pivot = _chestBone.position;
                    Vector3 tgt = ResolveAimPoint(aimTarget);
                    Vector3 to = tgt - barrel.position;
                    float dist = Mathf.Max(0.5f, to.magnitude);
                    Vector3 pivotToAim = barrel.position + barrel.forward * dist - pivot;
                    Vector3 pivotToTgt = tgt - pivot;
                    if (to.sqrMagnitude > 0.04f && pivotToTgt.sqrMagnitude > 0.04f && pivotToAim.sqrMagnitude > 0.04f)
                    {
                        Quaternion full = Quaternion.FromToRotation(pivotToAim, pivotToTgt);
                        full.ToAngleAxis(out float ang, out Vector3 axis);
                        if (ang > 180f) ang -= 360f;
                        ang = Mathf.Clamp(ang * Mathf.Clamp01(acp.enemyAimCorrectionStrength), -acp.enemyAimCorrectionMaxAngle, acp.enemyAimCorrectionMaxAngle);
                        if (axis.sqrMagnitude > 0.0001f) want = Quaternion.AngleAxis(ang, axis);
                    }
                }
                // Smoothed in world space; re-solved every frame against the fresh animator pose, so it converges instead of accumulating.
                _aimCorrection = Quaternion.RotateTowards(_aimCorrection, want, Mathf.Max(1f, acp.enemyAimCorrectionSpeed) * Time.deltaTime);
                Quaternion applied = Quaternion.Slerp(_aimCorrectionPrevFull, _aimCorrection, 1f);
                _aimCorrectionPrevFull = applied;
                if (Quaternion.Angle(applied, Quaternion.identity) > 0.1f)
                    _chestBone.rotation = applied * _chestBone.rotation;
                return;
            }

            if (Mathf.Abs(_chestPitchCurrent) > 0.1f)
                _chestBone.rotation = Quaternion.AngleAxis(-_chestPitchCurrent, transform.right) * _chestBone.rotation;
        }

        /// <summary>2026-10-13 (f): when DM_EnemyBrainProfile.reconcileRangedEngageToVision is on, a ranged enemy may fire out to
        /// min(weapon rangedRange, vision range) if that is beyond its configured engage range (it can see and hit you, so it shoots).</summary>
        public float RangedEngageRange
        {
            get
            {
                DM_EnemyBrainProfile p = GuardProfile;
                if (p == null || !p.reconcileRangedEngageToVision)
                    return rangedEngageRange;
                ItemData w = _loadoutBridge != null ? _loadoutBridge.ResolvedRanged : null;
                if (_senses == null) _senses = GetComponent<EnemySenses>();
                if (w == null || w.rangedRange <= 0f || _senses == null)
                    return rangedEngageRange;
                float engage = Mathf.Max(rangedEngageRange, Mathf.Min(w.rangedRange, _senses.VisionRange));
                if (IsRifle(w)) // 2026-10-13 (g): rifle base engage (25 m) + upgrade hook, capped by the weapon range
                    engage = Mathf.Max(engage, Mathf.Min(w.rangedRange, p.rifleEngageRangeBase + RifleRangeBonus));
                return engage;
            }
        }
        private EnemySenses _senses;
        /// <summary>2026-10-13 (g): hook for skill/upgrade systems to extend enemy rifle range (metres added to rifleEngageRangeBase / rifleVisionFloor).</summary>
        public static float RifleRangeBonus;
        public static bool IsRifle(ItemData w) => w != null && w.IsRangedWeapon && w.weaponGrip == WeaponGrip.TwoHanded && !w.isMiningTool;
        /// <summary>Vision floor for rifle users (EnemySenses reads it).</summary>
        public float RifleVisionFloor
        {
            get
            {
                DM_EnemyBrainProfile p = GuardProfile;
                ItemData w = _loadoutBridge != null ? _loadoutBridge.ResolvedRanged : null;
                return p != null && IsRifle(w) ? p.rifleVisionFloor + RifleRangeBonus : 0f;
            }
        }

        /// <summary>Ranged hold distance (definition field when set, else this bridge's field, else 80% of the engage range), kept beyond melee reach.</summary>
        public float RangedHoldDistance
        {
            get
            {
                EnemyDefinition d = _loadoutBridge != null ? _loadoutBridge.ResolveDefinition() : null;
                float v = d != null && d.rangedHoldDistance > 0.01f ? d.rangedHoldDistance : rangedHoldDistance;
                if (v <= 0.01f)
                    v = rangedEngageRange * 0.8f;
                return Mathf.Clamp(v, 4f, Mathf.Max(4f, rangedEngageRange));
            }
        }

        public float RangedRetreatFraction
        {
            get
            {
                EnemyDefinition d = _loadoutBridge != null ? _loadoutBridge.ResolveDefinition() : null;
                return d != null && d.rangedRetreatFraction > 0.01f ? d.rangedRetreatFraction : rangedRetreatFraction;
            }
        }

        public float KiterRetreatBoost
        {
            get
            {
                EnemyDefinition d = _loadoutBridge != null ? _loadoutBridge.ResolveDefinition() : null;
                return d != null && d.kiterRetreatBoost > 0.01f ? d.kiterRetreatBoost : kiterRetreatBoost;
            }
        }

        public float RangedRetreatSpeedMultiplier => rangedRetreatSpeedMultiplier;

        public bool HasRangedWeaponEquipped()
        {
            ItemData weapon = _loadoutBridge != null ? _loadoutBridge.ActiveItem : null;
            return weapon != null && weapon.IsRangedWeapon;
        }

        /// <summary>2026-10-13 (g): true when this enemy would pick its gun for the target at the current distance (even before the gun is drawn).</summary>
        public bool WouldUseRangedFor(Transform target)
        {
            if (target == null || _loadoutBridge == null || !_loadoutBridge.PrefersRangedAtRange || _loadoutBridge.ResolvedRanged == null)
                return false;
            ItemData w = ResolveWeaponForTarget(target);
            return w != null && w.IsRangedWeapon;
        }

        public bool IsArmedRangedPreferred()
        {
            // A gun-only enemy (no melee weapon) is always a ranged fighter, even without preferRanged.
            return HasRangedWeaponEquipped()
                && _loadoutBridge != null
                && (_loadoutBridge.PrefersRangedAtRange || _loadoutBridge.MeleeWeaponItem == null);
        }

        /// <summary>True once the aim pose has been held for <see cref="aimHoldDuration"/> seconds.</summary>
        public bool IsAimReady =>
            _isAimStanceActive && _aimStanceOnTime >= 0f && Time.time - _aimStanceOnTime >= aimHoldDuration;

        /// <summary>
        /// Called by EnemyCombat before an attack is committed. Returns true when the shot must wait:
        /// the gun was just picked from a melee branch (draw it and let the ranged Attack state take over),
        /// or the aim pose has not been held long enough yet. Never fires and never starts a melee fallback.
        /// </summary>
        public bool TryDeferRangedAttack(Transform target)
        {
            if (target == null || _loadoutBridge == null)
                return false;

            ItemData weapon = ResolveWeaponForTarget(target);
            if (weapon == null || !weapon.IsRangedWeapon)
                return false;

            _aimRequestUntil = Time.time + AimRequestWindow;
            if (_loadoutBridge.ActiveItem != weapon)
            {
                _loadoutBridge.EquipSpecificWeapon(weapon);
                return true;
            }

            return !IsAimReady;
        }

        public bool TryBeginBlock(float blockDuration, out float duration)
        {
            duration = blockDuration > 0f ? blockDuration : defensiveBlockDurationFallback;
            if (_controller == null || _animator == null)
                return false;
            var gp = GuardProfile;
            if (gp != null && GuardStamina < gp.enemyMinGuardStaminaToBlock)
                return false; // 2026-10-13: too tired to guard

            _controller.isStrafing = true;
            // 2026-10-13 (d5): melee weapon drawn -> weapon block (DefenseID >= 1, same as the player), never Defense_Unarmed (0).
            if (!HasRangedWeaponEquipped() && _loadoutBridge != null && _loadoutBridge.ActiveItem != null)
            {
                int defId = _meleeManager != null ? _meleeManager.GetDefenseID() : 1;
                _animator.SetInteger(vAnimatorParameters.DefenseID, Mathf.Max(1, defId));
            }
            _animator.SetBool(vAnimatorParameters.IsBlocking, true);
            _blockActive = true;
            return true;
        }

        private bool _blockActive;

        /// <summary>True while the brain-driven block (TryBeginBlock .. EndBlock) is up. Player melee hits read it (DMEnemyBlock).</summary>
        public bool IsBlockActive => _blockActive;

        public void EndBlock()
        {
            _blockActive = false;
            if (_animator != null)
                _animator.SetBool(vAnimatorParameters.IsBlocking, false);

            if (_controller != null)
                _controller.isStrafing = false;
        }

        /// <summary>World direction (flat) chosen by the last TryBeginDodgeRoll.</summary>
        public Vector3 LastRollDirection { get; private set; }

        public bool TryBeginDodgeRoll(Transform threat, out float duration)
        {
            duration = DM_EnemyBrainProfile.Live.dodgeRollSeconds;
            if (_controller == null || threat == null)
                return false;

            // Animator off / not initialised (ragdoll or hit-stagger recovery): HasState is false for every clip, which used to fall back to
            // the forward Roll and slide the capsule with no animation. Skip; the brain tries again on its next decision.
            if (_animator == null || !_animator.isActiveAndEnabled || !_animator.isInitialized)
            {
                if (Project.Combat.DM_CombatCoreProfile.Live != null && Project.Combat.DM_CombatCoreProfile.Live.logInterruptsInPlay)
                    Debug.Log($"[DodgeSkipped] {name} reason=animatorNotReady enabled={(_animator != null && _animator.isActiveAndEnabled ? 1 : 0)} init={(_animator != null && _animator.isInitialized ? 1 : 0)}", this);
                return false;
            }

            Vector3 toThreat = threat.position - transform.position;
            toThreat.y = 0f;
            if (toThreat.sqrMagnitude < 0.01f)
                toThreat = transform.forward;
            toThreat.Normalize();

            Vector3 away = -toThreat;
            Vector3 right = Vector3.Cross(Vector3.up, toThreat);
            if (right.sqrMagnitude < 0.0001f)
                right = transform.right;
            right.Normalize();

            DM_EnemyBrainProfile bp = DM_EnemyBrainProfile.Live;
            float wSide = Mathf.Max(0f, bp.dodgeRollSideWeight), wBack = Mathf.Max(0f, bp.dodgeRollBackWeight), wFwd = Mathf.Max(0f, bp.dodgeRollForwardWeight);
            float wTotal = wSide * 2f + wBack + wFwd;
            Vector3 rollDir;
            if (wTotal <= 0.0001f)
            {
                rollDir = away;
            }
            else
            {
                float pickW = Random.value * wTotal;
                if (pickW < wBack) rollDir = away;
                else if (pickW < wBack + wSide) rollDir = right;
                else if (pickW < wBack + wSide * 2f) rollDir = -right;
                else rollDir = wFwd > 0.0001f ? toThreat : away;
            }

            // Safety (runs BEFORE the facing snap / back clip so the body can never be turned toward the threat): never roll toward it unless the profile weights it.
            if (wFwd <= 0.0001f && Vector3.Dot(rollDir, toThreat) > 0.05f)
                rollDir = away;

            // Clear path first: a roll that would hit a wall / body / the player stops dead mid-clip (the "invisible wall"). Try the other side / back
            // (never toward the threat) when the picked direction has less room than planned.
            float plannedMax = Mathf.Max(bp.dodgeSideDistance, Mathf.Max(bp.dodgeBackDistance, bp.dodgeRollDistance));
            float pickedClear = RollClearance(rollDir, plannedMax);
            if (pickedClear < plannedMax - 0.05f)
            {
                Vector3 bestDir = rollDir;
                float bestClear = pickedClear;
                Vector3[] alts = { right, -right, away };
                for (int ai = 0; ai < alts.Length; ai++)
                {
                    float cd = RollClearance(alts[ai], plannedMax);
                    if (cd > bestClear + 0.05f)
                    {
                        bestClear = cd;
                        bestDir = alts[ai];
                    }
                }

                rollDir = bestDir;
            }

            LastRollUsedBackClip = false;
            LastRollClipId = 0;
            bool backward = Vector3.Dot(rollDir, away) > 0.9f;
            bool sideRight = Vector3.Dot(rollDir, right) > 0.9f;
            bool sideLeft = Vector3.Dot(rollDir, -right) > 0.9f;
            Vector3 faceThreat = new Vector3(toThreat.x, 0f, toThreat.z);
            if (backward && bp.dodgeBackUseClip && _animator != null && _animator.HasState(0, DodgeBackHash))
            {
                // Backward dodge clip: the body faces the threat and the AI moves it away (EnemyAiController.TickDodgeRoll).
                LastRollUsedBackClip = true;
                LastRollClipId = 1;
                duration = Mathf.Max(0.3f, bp.dodgeBackSeconds);
                _animator.SetFloat(DodgeBackSpeedHash, DodgeBackClipSeconds / duration);
                if (faceThreat.sqrMagnitude > 0.0001f && bp.dodgeTurnDegPerSec <= 0f) // 2026-10-12: >0 = AI turns smoothly (TickDodgeRoll)
                    transform.rotation = Quaternion.LookRotation(faceThreat.normalized, Vector3.up);
            }
            else if ((sideRight || sideLeft) && bp.dodgeSideUseClip && _animator != null && _animator.HasState(0, sideRight ? DodgeRightHash : DodgeLeftHash))
            {
                // Side dodge clip: the body faces the threat and sidesteps (never the forward Roll clip turned sideways, which looks like a roll toward the player).
                LastRollUsedBackClip = true; // "faces the threat" family (back / left / right)
                LastRollClipId = sideRight ? 3 : 2;
                duration = Mathf.Max(0.3f, bp.dodgeSideSeconds);
                _animator.SetFloat(DodgeSideSpeedHash, DodgeSideClipSeconds / duration);
                if (faceThreat.sqrMagnitude > 0.0001f && bp.dodgeTurnDegPerSec <= 0f)
                    transform.rotation = Quaternion.LookRotation(faceThreat.normalized, Vector3.up);
            }
            else if (bp.dodgeRollSnapFacing)
            {
                Vector3 flat = new Vector3(rollDir.x, 0f, rollDir.z);
                if (flat.sqrMagnitude > 0.0001f && bp.dodgeTurnDegPerSec <= 0f)
                    transform.rotation = Quaternion.LookRotation(flat.normalized, Vector3.up);
            }

            if (LastRollClipId == 0 && !bp.allowForwardRollFallback)
            {
                if (Project.Combat.DM_CombatCoreProfile.Live != null && Project.Combat.DM_CombatCoreProfile.Live.logInterruptsInPlay)
                    Debug.Log($"[DodgeSkipped] {name} reason=noDodgeClip backward={(backward ? 1 : 0)} right={(sideRight ? 1 : 0)} left={(sideLeft ? 1 : 0)} dotAway={Vector3.Dot(rollDir, away):0.00}", this);
                return false;
            }

            if (LastRollClipId == 0)
            {
                Project.Combat.DM_CombatCoreProfile ccf = Project.Combat.DM_CombatCoreProfile.Live;
                if (ccf != null && ccf.logInterruptsInPlay)
                    Debug.Log($"[RollFallback] {name} no dodge clip chosen: backward={(backward ? 1 : 0)} right={(sideRight ? 1 : 0)} left={(sideLeft ? 1 : 0)} hasBack={(_animator != null && _animator.HasState(0, DodgeBackHash) ? 1 : 0)} hasLeft={(_animator != null && _animator.HasState(0, DodgeLeftHash) ? 1 : 0)} hasRight={(_animator != null && _animator.HasState(0, DodgeRightHash) ? 1 : 0)} useBack={(bp.dodgeBackUseClip ? 1 : 0)} useSide={(bp.dodgeSideUseClip ? 1 : 0)} dotAway={Vector3.Dot(rollDir, away):0.00} dotRight={Vector3.Dot(rollDir, right):0.00} animEnabled={(_animator != null && _animator.isActiveAndEnabled ? 1 : 0)} init={(_animator != null && _animator.isInitialized ? 1 : 0)} layers={(_animator != null ? _animator.layerCount : -1)} ctrl={(_animator != null && _animator.runtimeAnimatorController != null ? _animator.runtimeAnimatorController.name : "none")} inTransition={(_animator != null && _animator.IsInTransition(0) ? 1 : 0)}", this);
            }

            LastRollDirection = rollDir.normalized;
            // (old hard-coded pick, now weights above) Side and backward only — never roll toward the threat.
            rollDir.Normalize();
            _controller.input = transform.InverseTransformDirection(rollDir);
            _controller.moveDirection = rollDir;
            _controller.Roll();
            if (LastRollClipId == 1)
                _animator.CrossFadeInFixedTime(DodgeBackHash, bp.dodgeBackBlendIn, 0);
            else if (LastRollClipId == 2)
                _animator.CrossFadeInFixedTime(DodgeLeftHash, Mathf.Max(0.01f, bp.dodgeSideBlendIn), 0);
            else if (LastRollClipId == 3)
                _animator.CrossFadeInFixedTime(DodgeRightHash, Mathf.Max(0.01f, bp.dodgeSideBlendIn), 0);
            if (Project.Combat.DM_CombatCoreProfile.Live != null && Project.Combat.DM_CombatCoreProfile.Live.logInterruptsInPlay)
                Debug.Log($"[DodgePlayed] {name} state={(LastRollClipId == 1 ? "DM_DodgeBack" : LastRollClipId == 2 ? "DM_DodgeLeft" : LastRollClipId == 3 ? "DM_DodgeRight" : "Roll")} clipOk={(LastRollClipId != 0 ? 1 : 0)}", this);
            return true;
        }

        /// <summary>Free distance (m, capped at max) along dir from the chest, ignoring this enemy's own colliders.</summary>
        private float RollClearance(Vector3 dir, float max)
        {
            Vector3 chest = transform.position + Vector3.up * 0.9f;
            const float pad = 0.35f;
            RaycastHit[] hits = Physics.RaycastAll(chest, dir.normalized, max + pad, ~0, QueryTriggerInteraction.Ignore);
            float nearest = max + pad;
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider == null || hits[i].transform.root == transform.root)
                    continue;
                nearest = Mathf.Min(nearest, hits[i].distance);
            }

            return Mathf.Clamp(nearest - pad, 0f, max);
        }

        /// <summary>0 = forward Roll clip, 1 = DM_DodgeBack, 2 = DM_DodgeLeft, 3 = DM_DodgeRight (set by the last TryBeginDodgeRoll).</summary>
        public int LastRollClipId { get; private set; }

        public static string RollClipName(int id) => id == 1 ? "back" : id == 2 ? "left" : id == 3 ? "right" : "roll";

        public bool TryGetDodgeProgress(int clipId, out float normalizedTime)
        {
            normalizedTime = 0f;
            if (_animator == null)
                return false;

            int hash = clipId == 1 ? DodgeBackHash : clipId == 2 ? DodgeLeftHash : clipId == 3 ? DodgeRightHash : RollStateHash;
            return TryGetStateProgress(hash, out normalizedTime);
        }

        private static readonly int DodgeLeftHash = Animator.StringToHash("DM_DodgeLeft");
        private static readonly int DodgeRightHash = Animator.StringToHash("DM_DodgeRight");
        private static readonly int DodgeSideSpeedHash = Animator.StringToHash("DMDodgeSideSpeed");
        private const float DodgeSideClipSeconds = 0.9667f; // Standing Dodge Left / Right clip length

        /// <summary>Tip reach when known. False while a weapon is assigned but not drawn yet (reach unknown, do not cap).</summary>
        public bool TryGetKnownTipReach(DM_EnemyEngagementProfile p, out float tip)
        {
            tip = 0f;
            if (p == null)
                return false;
            if (_loadoutBridge != null && _loadoutBridge.ActiveDrawnInstance == null && _loadoutBridge.ActiveItem != null)
                return false;
            tip = WeaponTipReach(p);
            return true;
        }

        private GameObject _reachInstance;
        private DMEnemyHitMarks _reachMarks;
        private float _reachLength;

        /// <summary>Tip reach (m from the enemy root) of the equipped melee weapon: arm extension + the weapon's hitbox length from its grip. Unarmed: profile value.</summary>
        public float WeaponTipReach(DM_EnemyEngagementProfile p)
        {
            GameObject inst = _loadoutBridge != null ? _loadoutBridge.ActiveDrawnInstance : null;
            if (inst == null)
                return p.unarmedTipReach;

            if (inst != _reachInstance)
            {
                _reachInstance = inst;
                _reachLength = 0f;
                Vector3 grip = inst.transform.position;
                foreach (vHitBox hb in inst.GetComponentsInChildren<vHitBox>(true))
                {
                    BoxCollider bc = hb.GetComponent<BoxCollider>();
                    if (bc == null)
                        continue;

                    for (int cx = -1; cx <= 1; cx += 2)
                        for (int cy = -1; cy <= 1; cy += 2)
                            for (int cz = -1; cz <= 1; cz += 2)
                            {
                                Vector3 corner = bc.transform.TransformPoint(bc.center + Vector3.Scale(bc.size * 0.5f, new Vector3(cx, cy, cz)));
                                _reachLength = Mathf.Max(_reachLength, Vector3.Distance(corner, grip));
                            }
                }
            }

            // Weapon length: the weapon definition (ItemData.meleeBladeLength) wins; else the measured hitbox; else the mesh bounds. Never prefab placements.
            if (_reachLength <= 0.05f && inst != null)
            {
                Vector3 grip = inst.transform.position;
                foreach (Renderer r in inst.GetComponentsInChildren<Renderer>(true))
                {
                    if (r is ParticleSystemRenderer) continue;
                    Bounds b = r.bounds;
                    for (int cx = -1; cx <= 1; cx += 2) for (int cy = -1; cy <= 1; cy += 2) for (int cz = -1; cz <= 1; cz += 2)
                        _reachLength = Mathf.Max(_reachLength, Vector3.Distance(b.center + Vector3.Scale(b.extents, new Vector3(cx, cy, cz)), grip));
                }
            }
            Project.Data.ItemData def = _loadoutBridge != null ? _loadoutBridge.ActiveItem : null;
            float len = def != null && def.meleeBladeLength > 0.05f ? def.meleeBladeLength : _reachLength;

            float arm = p.weaponReachArm;
            if (_reachMarks == null)
                _reachMarks = GetComponentInParent<DMEnemyHitMarks>();
            if (_reachMarks != null && _reachMarks.BodyType == DMEnemyBodyType.Robot)
                arm = p.weaponReachArmRobot;
            else if (_reachMarks != null && _reachMarks.BodyType != DMEnemyBodyType.Humanoid)
                arm = p.weaponReachArmMech;
            return len > 0.05f ? arm + len : p.unarmedTipReach;
        }

        public float LastWeaponLength => _reachLength;

        private static readonly int RollStateHash = Animator.StringToHash("Roll");

        /// <summary>
        /// Normalized time of the roll / backward-dodge animator state (current state, or the state being blended in).
        /// False while the state has not started yet or after it has been left. The roll movement follows this instead of a fixed timer.
        /// </summary>
        public bool TryGetDodgeProgress(bool back, out float normalizedTime)
        {
            normalizedTime = 0f;
            if (_animator == null)
                return false;

            return TryGetStateProgress(back ? DodgeBackHash : RollStateHash, out normalizedTime);
        }

        /// <summary>Starts the HumanM@Dodge01 dodge-strike clip (DM_DodgeStrike state) at clipSeconds total length. False if the controller lacks the state.</summary>
        public bool TryBeginDodgeStrikeClip(float clipSeconds, float blendIn)
        {
            if (_animator == null || !_animator.HasState(0, DodgeStrikeHash))
                return false;

            _animator.SetFloat(DodgeStrikeSpeedHash, DodgeStrikeClipSeconds / Mathf.Max(0.2f, clipSeconds));
            _animator.CrossFadeInFixedTime(DodgeStrikeHash, Mathf.Max(0.01f, blendIn), 0);
            return true;
        }

        public bool TryGetDodgeStrikeProgress(out float normalizedTime)
        {
            return TryGetStateProgress(DodgeStrikeHash, out normalizedTime);
        }

        private static readonly int DodgeStrikeHash = Animator.StringToHash("DM_DodgeStrike");
        private static readonly int DodgeStrikeSpeedHash = Animator.StringToHash("DMDodgeStrikeSpeed");
        private const float DodgeStrikeClipSeconds = 1.0667f;

        private bool TryGetStateProgress(int hash, out float normalizedTime)
        {
            normalizedTime = 0f;
            if (_animator == null)
                return false;

            AnimatorStateInfo cur = _animator.GetCurrentAnimatorStateInfo(0);
            if (cur.shortNameHash == hash)
            {
                normalizedTime = cur.normalizedTime;
                return true;
            }

            if (_animator.IsInTransition(0))
            {
                AnimatorStateInfo next = _animator.GetNextAnimatorStateInfo(0);
                if (next.shortNameHash == hash)
                {
                    normalizedTime = next.normalizedTime;
                    return true;
                }
            }

            return false;
        }

        private static readonly int DodgeBackHash = Animator.StringToHash("DM_DodgeBack");
        private static readonly int DodgeBackSpeedHash = Animator.StringToHash("DMDodgeBackSpeed");
        private static readonly int FreeMovementHash = Animator.StringToHash("Free Movement");
        private static readonly int StrafingMovementHash = Animator.StringToHash("Strafing Movement");
        private const float DodgeBackClipSeconds = 1.4f; // Humanoid@DodgeBackwardsCombat clip length

        /// <summary>True when the last TryBeginDodgeRoll played the backward dodge clip (body faces the threat, moves away).</summary>
        public bool LastRollUsedBackClip { get; private set; }

        /// <summary>Cross-fades the backward dodge clip out to locomotion (blend-out time from the brain profile).</summary>
        public void EndDodgeBackClip(float blendOut)
        {
            if (_animator == null)
                return;

            bool strafing = _animator.GetBool("IsStrafing");
            _animator.CrossFadeInFixedTime(strafing ? StrafingMovementHash : FreeMovementHash, Mathf.Max(0.01f, blendOut), 0);
        }

        private const float defensiveBlockDurationFallback = 1.2f;

        public bool TryBeginAttack(Transform target, out float duration)
        {
            duration = defaultMeleeDuration;
            if (target == null)
                return false;

            EnemyInvectorRagdollBridge ragdollBridge = GetComponent<EnemyInvectorRagdollBridge>();
            if (ragdollBridge != null && ragdollBridge.IsHitStaggerActive)
                return false;

            DMSpawnPhysicsStabilizer stabilizer = GetComponent<DMSpawnPhysicsStabilizer>();
            if (stabilizer != null && stabilizer.IsSpawnSettleActive)
                return false;

            ItemData weapon = ResolveWeaponForTarget(target);
            if (weapon == null && _loadoutBridge != null)
                weapon = _loadoutBridge.ActiveItem ?? _loadoutBridge.ResolvePreferredWeapon();

            if (weapon != null && weapon.itemType == ItemType.MeleeWeapon)
                return TryBeginMeleeAttack(weapon, out duration);

            if (weapon != null && weapon.IsRangedWeapon)
                return TryBeginRangedAttack(target, weapon, out duration);

            return TryBeginUnarmedMeleeAttack(out duration);
        }

        private ItemData ResolveWeaponForTarget(Transform target)
        {
            // Same reach EnemyCombat uses to allow a melee swing (profile multiplier + pioneer grace),
            // so a swing-range TryAttack can never resolve to the gun.
            float meleeRange = _enemyCombat != null ? _enemyCombat.ResolveEffectiveAttackRange(target) : 1.8f;
            float distance = HorizontalDistance(transform.position, target.position);
            if (_loadoutBridge != null)
                return _loadoutBridge.ResolveWeaponForTargetDistance(distance, meleeRange);

            return null;
        }

        private bool TryBeginUnarmedMeleeAttack(out float duration)
        {
            duration = defaultUnarmedDuration;
            if (_animator == null)
                return false;

            LastAttackWasRanged = false;
            SyncMeleeAnimatorParams(useWeaponMoveSet: false);
            _animator.SetInteger(AttackIdHash, 0);
            _animator.SetTrigger(WeakAttackHash);
            return true;
        }

        private bool TryBeginMeleeAttack(ItemData weapon, out float duration)
        {
            duration = defaultMeleeDuration;
            if (_animator == null || _meleeManager == null)
                return false;

            LastAttackWasRanged = false;
            if (_loadoutBridge != null &&
                (_loadoutBridge.ActiveItem != weapon || _loadoutBridge.ActiveDrawnInstance == null))
            {
                _loadoutBridge.EquipSpecificWeapon(weapon);
            }

            ApplyMeleeAnimatorIds(weapon);
            EnemyInvectorCombatShutdown.EnableMeleeIfSafe(gameObject);
            if (_meleeManager != null && !_meleeManager.enabled)
                _meleeManager.enabled = true;
            _animator.SetTrigger(WeakAttackHash);
            duration = ResolveMeleeDuration(weapon);
            EnemyNoiseEvents.RaiseNoise(transform.position, 5f, gameObject);
            return true;
        }

        private bool TryBeginRangedAttack(Transform target, ItemData weapon, out float duration)
        {
            duration = defaultRangedDuration;
            DM_EnemyBrainProfile trp = GuardProfile;
            Project.Combat.CombatProjectileSpawner.DebugTrace = trp != null && trp.logRangedFireGates;
            if (Project.Combat.CombatProjectileSpawner.DebugTrace)
                Debug.Log($"[DMRangedShot] {name} TryBeginRangedAttack weapon {(weapon != null ? weapon.name : "null")} shooterMgr {(_shooterManager != null)} dist {(target != null ? HorizontalDistance(transform.position, target.position) : -1f):0.0}");
            if (_shooterManager == null || target == null)
                return false;

            LastAttackWasRanged = true;

            _loadoutBridge?.EquipSpecificWeapon(weapon);
            SnapBodyToward(target);

            // Ensure both UpperBody and Shot layers use the correct weapon-type animation.
            if (_animator != null)
            {
                float weaponId = ResolveUpperBodyId();
                _animator.SetFloat(UpperBodyIdHash, weaponId);
                _animator.SetFloat(ShotIdHash,      weaponId);
            }

            PrepareShooterWeaponForFire();
            Transform shotOrigin = ResolveProjectileMuzzle();
            Vector3 originPoint = shotOrigin != null ? shotOrigin.position : transform.position + Vector3.up * 1.35f;
            { DM_EnemyBrainProfile hp = GuardProfile; _aimHeadThisShot = hp != null && Random.value < hp.enemyHeadshotChance; } // 2026-10-13 (d2)
            Vector3 aimPoint = ApplyMissOffset(ResolveAimPoint(target), target, originPoint);
            _shooterManager.Shoot(aimPoint, applyHipfirePrecision: false);
            SpawnRangedProjectile(weapon, aimPoint);
            EnemyNoiseEvents.RaiseNoise(transform.position, 12f, gameObject);

            return true;
        }

        private void PrepareShooterWeaponForFire()
        {
            vShooterWeapon weapon = _shooterManager != null ? _shooterManager.CurrentWeapon : null;
            if (weapon == null)
                return;

            weapon.projectile = null;
            weapon.fireClip = null;
            weapon.emittShurykenParticle = null;
            weapon.lightOnShot = null;
            weapon.isInfinityAmmo = true;
            weapon.dontUseReload = true;

            int clip = weapon.clipSize > 0 ? weapon.clipSize : 999;
            if (weapon.ammo <= 0)
                weapon.AddAmmo(clip);
        }

        private Transform fallbackMuzzle;

        private void SpawnRangedProjectile(ItemData weapon, Vector3 aimPoint)
        {
            if (weapon == null || !weapon.IsRangedWeapon)
            {
                if (Project.Combat.CombatProjectileSpawner.DebugTrace) Debug.Log($"[DMRangedShot] {name} SpawnRangedProjectile skipped: weapon not ranged");
                return;
            }

            Transform muzzle = ResolveProjectileMuzzle();
            if (muzzle == null)
                muzzle = EnsureFallbackMuzzle();

            Vector3 direction = aimPoint - muzzle.position;
            if (direction.sqrMagnitude < 0.0001f)
                direction = muzzle.forward;
            float aimDistance = direction.magnitude;
            direction.Normalize();

            ItemData ammoItem = ResolveEnemyAmmoItem(weapon);
            float damage = ResolveRangedProjectileDamage(weapon, ammoItem);
            float spread = RangedFireSolver.ResolveEffectiveSpreadDegrees(
                weapon,
                ammoItem,
                isAiming: true,
                aimDistance,
                applyPlayerSkillBonus: false);
            CombatProjectileSpawner.Spawn(
                gameObject,
                muzzle,
                weapon,
                ammoItem,
                direction,
                spread,
                damage);
        }

        private ItemData ResolveEnemyAmmoItem(ItemData weapon)
        {
            if (weapon == null)
                return null;

            // Enemies use Standard for pistols and Plasma for rifles.
            if (weapon.weaponGrip == WeaponGrip.TwoHanded)
                return enemyRifleAmmo != null ? enemyRifleAmmo : weapon.defaultAmmoItem;

            return enemyPistolAmmo != null ? enemyPistolAmmo : weapon.defaultAmmoItem;
        }

        private float ResolveRangedProjectileDamage(ItemData weaponItem, ItemData ammoItem)
        {
            float rolled = ammoItem != null ? ammoItem.RollRangedDamage() : weaponItem.RollRangedDamage();
            if (_enemyCombat != null)
                rolled = Mathf.Max(rolled, _enemyCombat.AttackDamage);

            return Mathf.Max(1f, rolled);
        }

        private Transform ResolveProjectileMuzzle()
        {
            vShooterWeapon shooterWeapon = _shooterManager != null ? _shooterManager.CurrentWeapon : null;
            if (shooterWeapon != null && shooterWeapon.muzzle != null)
                return shooterWeapon.muzzle;

            GameObject drawn = _loadoutBridge != null ? _loadoutBridge.ActiveDrawnInstance : null;
            if (drawn != null)
            {
                vShooterWeapon nestedWeapon = drawn.GetComponentInChildren<vShooterWeapon>(true);
                if (nestedWeapon != null && nestedWeapon.muzzle != null)
                    return nestedWeapon.muzzle;

                ItemData activeItem = _loadoutBridge != null ? _loadoutBridge.ActiveItem : null;
                string socketName = activeItem != null && !string.IsNullOrWhiteSpace(activeItem.muzzleSocketName)
                    ? activeItem.muzzleSocketName
                    : "Muzzle";

                Transform socket = drawn.transform.Find(socketName);
                if (socket == null)
                    socket = FindDeepChild(drawn.transform, socketName);

                if (socket != null)
                    return socket;

                return drawn.transform;
            }

            return EnsureFallbackMuzzle();
        }

        private Transform EnsureFallbackMuzzle()
        {
            if (fallbackMuzzle == null)
            {
                GameObject host = new GameObject("EnemyProjectileMuzzleFallback");
                fallbackMuzzle = host.transform;
                fallbackMuzzle.SetParent(transform, false);
            }

            fallbackMuzzle.position = transform.position + Vector3.up * 1.35f + transform.forward * 0.45f;
            fallbackMuzzle.rotation = transform.rotation;
            return fallbackMuzzle;
        }

        private static Transform FindDeepChild(Transform parent, string childName)
        {
            if (parent == null)
                return null;

            if (parent.name == childName)
                return parent;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindDeepChild(parent.GetChild(i), childName);
                if (found != null)
                    return found;
            }

            return null;
        }

        // Returns the UpperBody_ID / Shot_ID value for the active ranged weapon.
        // Matches the "Aiming Fire Weapon" and Shot blend tree thresholds:
        //   1 = pistol (one-handed)
        //   2 = rifle / shotgun (two-handed)
        private float ResolveUpperBodyId()
        {
            ItemData weapon = _loadoutBridge?.ActiveItem;
            if (weapon == null) return 1f;
            return weapon.weaponGrip == WeaponGrip.TwoHanded ? 2f : 1f;
        }

        private void SnapBodyToward(Transform target)
        {
            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.01f)
                return;

            transform.rotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
        }

        private bool _aimHeadThisShot;
        private Quaternion _aimCorrection = Quaternion.identity, _aimCorrectionPrevFull = Quaternion.identity;

        /// <summary>2026-10-13 (d2): aim at the target's humanoid bone (DM_EnemyBrainProfile.enemyAimTargetBone, default Chest;
        /// head on an enemyHeadshotChance roll per shot). Falls back to the old capsule centre when the target has no humanoid rig.</summary>
        private Vector3 ResolveAimPoint(Transform target)
        {
            if (target != null)
            {
                Animator ta = target.GetComponentInChildren<Animator>();
                if (ta != null && ta.isHuman)
                {
                    DM_EnemyBrainProfile bp = GuardProfile;
                    HumanBodyBones bone = _aimHeadThisShot ? HumanBodyBones.Head
                        : (bp != null ? bp.enemyAimTargetBone : HumanBodyBones.Chest);
                    Transform bt = bone != HumanBodyBones.LastBone ? ta.GetBoneTransform(bone) : null;
                    if (bt == null && bone == HumanBodyBones.Chest) bt = ta.GetBoneTransform(HumanBodyBones.Spine);
                    if (bt != null) return bt.position;
                }
            }
            return ResolveCapsuleAimPoint(target);
        }

        private static Vector3 ResolveCapsuleAimPoint(Transform target)
        {
            // Prefer the root CapsuleCollider (character controller body) — its bounds.center
            // sits at mid-torso. Avoid trigger colliders and child hitboxes.
            CapsuleCollider capsule = target.GetComponent<CapsuleCollider>();
            if (capsule != null && capsule.enabled && !capsule.isTrigger)
                return capsule.bounds.center;

            // Fallback: scan children for a non-trigger capsule (some rigs put it one level down).
            CapsuleCollider[] caps = target.GetComponentsInChildren<CapsuleCollider>();
            for (int i = 0; i < caps.Length; i++)
            {
                if (caps[i].enabled && !caps[i].isTrigger)
                    return caps[i].bounds.center;
            }

            // Last resort: fixed offset that lands at roughly mid-chest height.
            return target.position + Vector3.up * 1.0f;
        }

        // Decides whether the shot misses and deflects the aim point accordingly.
        // A "miss" rolls against missRate; if it hits, a smaller natural spread is still applied.
        private Vector3 ApplyMissOffset(Vector3 aimPoint, Transform target, Vector3 origin)
        {
            if (missRate <= 0f) return aimPoint;

            float distance = HorizontalDistance(transform.position, target.position);
            bool isMiss = Random.value < missRate;

            // Offsets are applied in the shot's own frame (right / up perpendicular to the shot line),
            // so a miss always clears the target sideways or vertically regardless of facing.
            Vector3 shot = aimPoint - origin;
            if (shot.sqrMagnitude < 0.0001f)
                shot = transform.forward;
            shot.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, shot);
            if (right.sqrMagnitude < 0.0001f)
                right = transform.right;
            right.Normalize();
            Vector3 up = Vector3.Cross(shot, right).normalized;

            if (isMiss)
            {
                // Guarantee the shot clears the target by pushing well outside body width (~0.5 m).
                float deflect = Mathf.Lerp(0.6f, 2f, missRate) * Mathf.Max(1f, distance / 10f);
                Vector2 dir   = Random.insideUnitCircle.normalized * deflect;
                EnemyFloatingText.ShowMiss(target);
                return aimPoint + right * dir.x + up * dir.y;
            }

            // Small natural jitter even on hit shots.
            float spread = 0.08f * Mathf.Max(1f, distance / 10f);
            Vector2 jitter = Random.insideUnitCircle * spread;
            return aimPoint + right * jitter.x + up * jitter.y;
        }

        private void SyncMeleeAnimatorParams(bool useWeaponMoveSet)
        {
            if (_animator == null)
                return;

            if (!useWeaponMoveSet)
            {
                _animator.SetInteger(AttackIdHash, 0);
                _animator.SetFloat(MoveSetIdHash, 0f);
                return;
            }

            ApplyMeleeAnimatorIds(null);
        }

        /// <summary>
        /// Invector WeakAttacks: AttackID 0 = unarmed punch, 1 = one-hand sword, 2 = two-hand.
        /// GetAttackID returns 0 when the weapon GO is inactive or its attackID was never authored.
        /// </summary>
        private void ApplyMeleeAnimatorIds(ItemData weapon)
        {
            if (_animator == null)
                return;

            int attackId = 0;
            int moveSetId = 0;
            vMeleeWeapon rightWeapon = _meleeManager != null ? _meleeManager.rightWeapon : null;
            if (rightWeapon != null)
            {
                attackId = rightWeapon.attackID;
                moveSetId = rightWeapon.movesetID;
            }

            if (attackId <= 0 && _meleeManager != null)
                attackId = _meleeManager.GetAttackID();
            if (moveSetId <= 0 && _meleeManager != null)
                moveSetId = _meleeManager.GetMoveSetID();

            if (attackId <= 0 && weapon != null)
                attackId = weapon.weaponGrip == WeaponGrip.TwoHanded ? 2 : 1;
            if (moveSetId <= 0 && weapon != null)
                moveSetId = weapon.weaponGrip == WeaponGrip.TwoHanded ? 2 : 1;

            // Equipped melee must never fall through to AttackID 0 (unarmed punch SMBs).
            if (weapon != null && weapon.itemType == ItemType.MeleeWeapon && attackId <= 0)
                attackId = 1;
            if (weapon != null && weapon.itemType == ItemType.MeleeWeapon && moveSetId <= 0)
                moveSetId = 1;

            _animator.SetInteger(AttackIdHash, attackId);
            _animator.SetFloat(MoveSetIdHash, moveSetId);
        }

        private float ResolveMeleeDuration(ItemData weapon)
        {
            float speed = weapon != null ? weapon.ResolveAttackAnimationSpeed() : 0.95f;
            return defaultMeleeDuration / Mathf.Max(0.35f, speed);
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
