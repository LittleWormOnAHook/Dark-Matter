using System.Collections.Generic;
using Invector.vCharacterController;
using Invector.vMelee;
using Project.Player.Invector;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// One damage application per target root per Invector melee damage window (out-swing and return share a window).
    /// </summary>
    public static class PioneerMeleeSwingHitDedupe
    {
        private static int _trackedSwingId = -1;
        private static readonly HashSet<int> _targetRootsThisSwing = new HashSet<int>();

        public static bool TryAcceptHit(GameObject sender, GameObject hitColliderObject)
        {
            if (sender == null || hitColliderObject == null)
                return true;

            PioneerInvectorBootstrap bootstrap = PioneerInvectorBootstrap.Instance;
            if (bootstrap == null)
                return true;

            Transform playerRoot = bootstrap.transform.root;
            if (!sender.transform.IsChildOf(playerRoot) && sender.transform.root != playerRoot)
                return true;

            // Resolved from the Animator at hit time: OnTriggerEnter runs inside the fixed step,
            // before the tracker's Update/LateUpdate, so CurrentSwingId alone can still be stale.
            int swingId = PioneerMeleeDamageWindowTracker.ResolveSwingIdForHit();
            if (swingId != _trackedSwingId)
            {
                _trackedSwingId = swingId;
                _targetRootsThisSwing.Clear();
            }

            int targetRootId = hitColliderObject.transform.root.gameObject.GetEntityId().GetHashCode();
            return _targetRootsThisSwing.Add(targetRootId);
        }
    }

    internal static class PioneerStrongMeleeAnimStates
    {
        private const string StrongSwordBPath = "Attacks.StrongAttacks.SwordAttack.B";
        private const string StrongSwordBPathWithLayer = "FullBody.Attacks.StrongAttacks.SwordAttack.B";
        private static readonly int StrongSwordBHash = Animator.StringToHash(StrongSwordBPath);
        private static readonly int StrongSwordBHashWithLayer = Animator.StringToHash(StrongSwordBPathWithLayer);
        private const string StrongSwordChargeStatePath = "Attacks.StrongAttacks.SwordCharge";
        private const string StrongSwordChargeStatePathWithLayer = "FullBody.Attacks.StrongAttacks.SwordCharge";
        private static readonly int StrongSwordChargeStateHash = Animator.StringToHash(StrongSwordChargeStatePath);
        private static readonly int StrongSwordChargeStateHashWithLayer =
            Animator.StringToHash(StrongSwordChargeStatePathWithLayer);
        private static readonly int StrongSwordChargeShortHash = Animator.StringToHash("SwordCharge");

        public static int ResolveFullBodyLayer(Animator animator, vThirdPersonMotor motor)
        {
            if (motor != null && motor.fullbodyLayer >= 0)
                return motor.fullbodyLayer;
            return animator != null ? animator.GetLayerIndex("FullBody") : -1;
        }

        public static bool IsStrongChargeState(AnimatorStateInfo state)
        {
            return state.fullPathHash == StrongSwordChargeStateHash
                || state.fullPathHash == StrongSwordChargeStateHashWithLayer
                || state.shortNameHash == StrongSwordChargeShortHash
                || state.IsName(StrongSwordChargeStatePath)
                || state.IsName(StrongSwordChargeStatePathWithLayer)
                || state.IsName("SwordCharge");
        }

        public static bool IsStrongReleaseState(AnimatorStateInfo state, out float normalizedTime)
        {
            normalizedTime = state.normalizedTime;
            return TryGetStrongReleaseSlot(state, out _);
        }

        public static bool IsStrongReleaseState(AnimatorStateInfo state)
        {
            return IsStrongReleaseState(state, out _);
        }

        public static bool LayerHasStrongChargeOrRelease(Animator animator, int layer)
        {
            if (animator == null || layer < 0)
                return false;

            AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(layer);
            if (IsStrongChargeState(current) || IsStrongReleaseState(current))
                return true;

            if (!animator.IsInTransition(layer))
                return false;

            AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(layer);
            return IsStrongChargeState(next) || IsStrongReleaseState(next);
        }

        private const string StrongSwordAPath = "Attacks.StrongAttacks.SwordAttack.A";
        private const string StrongSwordAPathWithLayer = "FullBody.Attacks.StrongAttacks.SwordAttack.A";
        private static readonly int StrongSwordAHash = Animator.StringToHash(StrongSwordAPath);
        private static readonly int StrongSwordAHashWithLayer = Animator.StringToHash(StrongSwordAPathWithLayer);
        private const string StrongSwordCPath = "Attacks.StrongAttacks.SwordAttack.C";
        private const string StrongSwordCPathWithLayer = "FullBody.Attacks.StrongAttacks.SwordAttack.C";
        private static readonly int StrongSwordCHash = Animator.StringToHash(StrongSwordCPath);
        private static readonly int StrongSwordCHashWithLayer = Animator.StringToHash(StrongSwordCPathWithLayer);

        public static bool IsStrongAState(AnimatorStateInfo state) =>
            state.fullPathHash == StrongSwordAHash
            || state.fullPathHash == StrongSwordAHashWithLayer
            || state.IsName(StrongSwordAPath)
            || state.IsName(StrongSwordAPathWithLayer);

        public static bool IsStrongBState(AnimatorStateInfo state) =>
            state.fullPathHash == StrongSwordBHash
            || state.fullPathHash == StrongSwordBHashWithLayer
            || state.IsName(StrongSwordBPath)
            || state.IsName(StrongSwordBPathWithLayer);

        public static bool IsStrongCState(AnimatorStateInfo state) =>
            state.fullPathHash == StrongSwordCHash
            || state.fullPathHash == StrongSwordCHashWithLayer
            || state.IsName(StrongSwordCPath)
            || state.IsName(StrongSwordCPathWithLayer);

        /// <summary>0 = A, 1 = B (late hit window), 2 = C.</summary>
        public static bool TryGetStrongReleaseSlot(AnimatorStateInfo state, out int slot)
        {
            if (IsStrongBState(state))
            {
                slot = 1;
                return true;
            }

            if (IsStrongCState(state))
            {
                slot = 2;
                return true;
            }

            if (IsStrongAState(state))
            {
                slot = 0;
                return true;
            }

            slot = -1;
            return false;
        }

        public static bool TryResolveStrongAnimSpeedSlot(
            Animator animator,
            int layer,
            PioneerShooterMeleeInput shooterInput,
            out float multiplier)
        {
            multiplier = 1f;
            if (animator == null || layer < 0)
                return false;

            AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(layer);
            if (IsStrongChargeState(current) || IsStrongReleaseState(current))
            {
                multiplier = ResolveStrongChargeOrReleaseMultiplier();
                return true;
            }

            if (IsStrongAState(current))
            {
                multiplier = ResolveStrongSlotMultiplier(0);
                return true;
            }

            if (IsStrongCState(current))
            {
                multiplier = ResolveStrongSlotMultiplier(2);
                return true;
            }

            if (!animator.IsInTransition(layer))
                return false;

            AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(layer);
            if (IsStrongChargeState(next) || IsStrongReleaseState(next))
            {
                multiplier = ResolveStrongChargeOrReleaseMultiplier();
                return true;
            }

            if (IsStrongAState(next))
            {
                multiplier = ResolveStrongSlotMultiplier(0);
                return true;
            }

            if (IsStrongCState(next))
            {
                multiplier = ResolveStrongSlotMultiplier(2);
                return true;
            }

            return false;
        }

        private static float ResolveStrongChargeOrReleaseMultiplier()
        {
            DM_CombatCoreProfile profile = DM_CombatCoreProfile.Live;
            float mult = profile != null ? profile.strongMeleeAnimSpeedMultiplier : 1.25f;
            return Mathf.Clamp(mult, 0.75f, 2f);
        }

        private static float ResolveStrongSlotMultiplier(int slotIndex)
        {
            DM_CombatCoreProfile profile = DM_CombatCoreProfile.Live;
            float mult = 1f;
            if (profile != null)
            {
                mult = slotIndex switch
                {
                    0 => profile.strongMeleeAnimSpeedA,
                    2 => profile.strongMeleeAnimSpeedC,
                    _ => profile.strongMeleeAnimSpeedMultiplier
                };
            }

            return Mathf.Clamp(mult, 0.75f, 2f);
        }
    }

    internal static class PioneerLightMeleeAnimStates
    {
        private static readonly string[] LightComboPaths =
        {
            "Attacks.WeakAttacks.SwordAttack.A",
            "Attacks.WeakAttacks.SwordAttack.B",
            "Attacks.WeakAttacks.SwordAttack.C"
        };

        private static readonly string[] LightComboPathsWithLayer =
        {
            "FullBody.Attacks.WeakAttacks.SwordAttack.A",
            "FullBody.Attacks.WeakAttacks.SwordAttack.B",
            "FullBody.Attacks.WeakAttacks.SwordAttack.C"
        };

        private static readonly string[] LightRandomPaths =
        {
            "Attacks.WeakAttacks.SwordRandomAttack.A",
            "Attacks.WeakAttacks.SwordRandomAttack.B",
            "Attacks.WeakAttacks.SwordRandomAttack.C"
        };

        private static readonly string[] LightRandomPathsWithLayer =
        {
            "FullBody.Attacks.WeakAttacks.SwordRandomAttack.A",
            "FullBody.Attacks.WeakAttacks.SwordRandomAttack.B",
            "FullBody.Attacks.WeakAttacks.SwordRandomAttack.C"
        };

        private static readonly int[] LightComboHashes =
        {
            Animator.StringToHash(LightComboPaths[0]),
            Animator.StringToHash(LightComboPaths[1]),
            Animator.StringToHash(LightComboPaths[2])
        };

        private static readonly int[] LightComboHashesWithLayer =
        {
            Animator.StringToHash(LightComboPathsWithLayer[0]),
            Animator.StringToHash(LightComboPathsWithLayer[1]),
            Animator.StringToHash(LightComboPathsWithLayer[2])
        };

        private static readonly int[] LightRandomHashes =
        {
            Animator.StringToHash(LightRandomPaths[0]),
            Animator.StringToHash(LightRandomPaths[1]),
            Animator.StringToHash(LightRandomPaths[2])
        };

        private static readonly int[] LightRandomHashesWithLayer =
        {
            Animator.StringToHash(LightRandomPathsWithLayer[0]),
            Animator.StringToHash(LightRandomPathsWithLayer[1]),
            Animator.StringToHash(LightRandomPathsWithLayer[2])
        };

        private const string InteractHoldComboPath = "Attacks.InteractHoldCombo";
        private const string InteractHoldComboPathWithLayer = "FullBody.Attacks.InteractHoldCombo";
        private static readonly int InteractHoldComboHash = Animator.StringToHash(InteractHoldComboPath);
        private static readonly int InteractHoldComboHashWithLayer =
            Animator.StringToHash(InteractHoldComboPathWithLayer);

        public static bool IsInteractHoldComboState(AnimatorStateInfo state) =>
            state.fullPathHash == InteractHoldComboHash
            || state.fullPathHash == InteractHoldComboHashWithLayer
            || state.IsName(InteractHoldComboPath)
            || state.IsName(InteractHoldComboPathWithLayer)
            || state.IsName("InteractHoldCombo");

        public static bool TryGetLightComboSlot(AnimatorStateInfo state, out int slotIndex)
        {
            slotIndex = -1;
            int hash = state.fullPathHash;
            for (int i = 0; i < LightComboPaths.Length; i++)
            {
                if (hash == LightComboHashes[i]
                    || hash == LightComboHashesWithLayer[i]
                    || state.IsName(LightComboPaths[i])
                    || state.IsName(LightComboPathsWithLayer[i]))
                {
                    slotIndex = i;
                    return true;
                }
            }

            return false;
        }

        public static bool TryGetLightRandomSlot(AnimatorStateInfo state, out int slotIndex)
        {
            slotIndex = -1;
            int hash = state.fullPathHash;
            for (int i = 0; i < LightRandomPaths.Length; i++)
            {
                if (hash == LightRandomHashes[i]
                    || hash == LightRandomHashesWithLayer[i]
                    || state.IsName(LightRandomPaths[i])
                    || state.IsName(LightRandomPathsWithLayer[i]))
                {
                    slotIndex = i;
                    return true;
                }
            }

            return false;
        }

        public static bool TryResolveLightAnimSpeedSlot(Animator animator, int layer, out float multiplier)
        {
            multiplier = 1f;
            if (animator == null || layer < 0)
                return false;

            AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(layer);
            if (TryResolveLightAnimSpeedFromState(current, out multiplier))
                return true;

            if (!animator.IsInTransition(layer))
                return false;

            return TryResolveLightAnimSpeedFromState(animator.GetNextAnimatorStateInfo(layer), out multiplier);
        }

        private static bool TryResolveLightAnimSpeedFromState(AnimatorStateInfo state, out float multiplier)
        {
            multiplier = 1f;
            DM_CombatCoreProfile profile = DM_CombatCoreProfile.Live;

            if (TryGetLightComboSlot(state, out int comboSlot))
            {
                multiplier = ResolveLightComboMultiplier(profile, comboSlot);
                return true;
            }

            if (TryGetLightRandomSlot(state, out int randomSlot))
            {
                multiplier = ResolveLightRandomMultiplier(profile, randomSlot);
                return true;
            }

            if (IsInteractHoldComboState(state))
            {
                multiplier = profile != null ? profile.interactHoldComboAnimSpeed : 1.75f;
                multiplier = Mathf.Clamp(multiplier, 0.75f, 2.5f);
                return true;
            }

            return false;
        }

        private static float ResolveLightComboMultiplier(DM_CombatCoreProfile profile, int slotIndex)
        {
            float mult = 1f;
            if (profile != null)
            {
                mult = slotIndex switch
                {
                    0 => profile.lightComboAnimSpeedA,
                    1 => profile.lightComboAnimSpeedB,
                    2 => profile.lightComboAnimSpeedC,
                    _ => 1f
                };
            }

            return Mathf.Clamp(mult, 0.75f, 2f);
        }

        private static float ResolveLightRandomMultiplier(DM_CombatCoreProfile profile, int slotIndex)
        {
            float mult = 1f;
            if (profile != null)
            {
                mult = slotIndex switch
                {
                    0 => profile.lightRandomAnimSpeedA,
                    1 => profile.lightRandomAnimSpeedB,
                    2 => profile.lightRandomAnimSpeedC,
                    _ => 1f
                };
            }

            return Mathf.Clamp(mult, 0.75f, 2f);
        }
    }

    /// <summary>
    /// Owns the player's melee swing identity and hit windows.
    /// <see cref="CurrentSwingId"/> advances on every attack-state ENTRY (any Animator state carrying a
    /// vMeleeAttackControl, on FullBody or UpperBody) and on every new Strong B window, not on the rising
    /// edge of canApplyDamage. The player Animator runs in AnimatorUpdateMode.Fixed, so vMeleeAttackControl
    /// enables the weapon trigger inside the fixed step and OnTriggerEnter fires before Update; a rising-edge
    /// counter bumped in Update was one physics step late, and a blade that already overlapped the target
    /// when its window opened (Light C thrust, Strong A slam) was deduped against the previous swing.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(520)]
    public sealed class PioneerMeleeDamageWindowTracker : MonoBehaviour
    {
        public static int CurrentSwingId { get; private set; }

        private static PioneerMeleeDamageWindowTracker _instance;

        private vMeleeManager _meleeManager;
        private PioneerShooterMeleeInput _shooterInput;
        private vThirdPersonMotor _motor;
        private Animator _animator;
        private bool _wasDamageActive;
        private bool _swingTrackedByState;
        private int _swingStateLayer = -1;
        private int _swingStateHash;
        private float _swingStateLastNormalized;
        private int _swingWindowIndex = -1;
        private bool _swingInWindow;
        private bool _swingGatesRight;
        private bool _swingGatesLeft;
        private RuntimeAnimatorController _attackControlCacheController;
        private readonly Dictionary<long, vMeleeAttackControl> _attackControlCache =
            new Dictionary<long, vMeleeAttackControl>();
        private const float SwingRestartTolerance = 0.05f;
        private bool _meleeAnimSpeedOverrideApplied;
        private float _savedAnimatorSpeed = 1f;

        private void Awake()
        {
            _instance = this;
            _meleeManager = GetComponent<vMeleeManager>();
            _shooterInput = GetComponent<PioneerShooterMeleeInput>();
            _motor = GetComponent<vThirdPersonMotor>();
            _animator = GetComponent<Animator>();
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        /// <summary>Called when charge/release crossfades start so speed applies the same frame (before tracker LateUpdate).</summary>
        public static void ClearSpeedOverride()
        {
            if (_instance == null || _instance._animator == null)
                return;

            if (_instance._meleeAnimSpeedOverrideApplied)
            {
                float restored = _instance._savedAnimatorSpeed;
                _instance._animator.speed = restored > 0.01f && restored < 1.05f ? restored : 1f;
                _instance._meleeAnimSpeedOverrideApplied = false;
            }
            else if (_instance._animator.speed > 1.05f)
            {
                _instance._animator.speed = 1f;
            }
        }

        public static void ApplyStrongMeleeSpeedOverride(float multiplier)
        {
            if (_instance == null || _instance._animator == null)
                return;

            multiplier = Mathf.Clamp(multiplier, 0.75f, 2f);
            if (!_instance._meleeAnimSpeedOverrideApplied)
            {
                float current = _instance._animator.speed;
                _instance._savedAnimatorSpeed = current > 0.01f ? current : 1f;
                _instance._meleeAnimSpeedOverrideApplied = true;
            }

            _instance._animator.speed = multiplier;
        }

        private void Update()
        {
            // Force-disable runs in Update so hitboxes drop immediately; swing windows tick once in LateUpdate.
            if (!ShouldForceDisableWeaponHitboxes())
                return;

            ForceDisableWeaponHitboxes();
            _wasDamageActive = false;
            ResetSwingStateTracking();
        }

        private void LateUpdate()
        {
            ApplyMeleeAnimSpeedFromProfile();

            // Re-apply the hit window after this frame's Animator steps. vMeleeAttackControl toggles
            // damage from inside the Animator update (Fixed mode), which otherwise leaks damage at its
            // own window edges, e.g. during Strong A's raise or B's windup, or past normalized 1.
            if (ShouldForceDisableWeaponHitboxes())
                return;

            TickSwingAndDamageWindows();

            if (IsMeleeDamageActive())
                PioneerMeleeDmHitboxProbe.ProbeManager(_meleeManager);
        }

        /// <summary>
        /// Swing id for a hit being processed right now (called from the dedupe inside OnTriggerEnter).
        /// Reads the Animator, which already stepped this fixed step, so the first contact of a new
        /// attack state gets the new id even before this component's Update runs.
        /// </summary>
        public static int ResolveSwingIdForHit()
        {
            if (_instance != null && _instance.isActiveAndEnabled)
                _instance.RefreshSwingTracking();

            return CurrentSwingId;
        }

        private void TickSwingAndDamageWindows()
        {
            bool newSwing = RefreshSwingTracking();
            if (_swingTrackedByState && (_swingGatesRight || _swingGatesLeft))
            {
                // A new attack state never inherits the previous swing's open trigger: close it
                // (clears vMeleeAttackObject.targetColliders), then open only inside this state's window.
                if (newSwing && IsMeleeDamageActive())
                    SetWeaponDamageActive(false);

                SetWeaponDamageActive(_swingInWindow, _swingGatesRight, _swingGatesLeft);
                _wasDamageActive = _swingInWindow;
                return;
            }

            if (TryGateStrongReleaseDamage(out bool wantStrongDamage))
            {
                if (wantStrongDamage && !_wasDamageActive)
                    CurrentSwingId++;

                SetWeaponDamageActive(wantStrongDamage);
                _wasDamageActive = wantStrongDamage;
                return;
            }

            // Fallback for damage that no attack state accounts for (rising edge, legacy behaviour).
            bool active = IsMeleeDamageActive();
            if (active && !_wasDamageActive && !_swingTrackedByState)
                CurrentSwingId++;

            _wasDamageActive = active;
        }

        private void ResetSwingStateTracking()
        {
            _swingTrackedByState = false;
            _swingStateLayer = -1;
            _swingStateHash = 0;
            _swingStateLastNormalized = 0f;
            _swingWindowIndex = -1;
            _swingInWindow = false;
            _swingGatesRight = false;
            _swingGatesLeft = false;
        }

        /// <summary>
        /// Follows the newest attack state (FullBody, then UpperBody). Bumps <see cref="CurrentSwingId"/>
        /// when a different attack state is entered, when the same state restarts, or when a Strong B
        /// swing moves into its second window. Returns true when the id changed.
        /// </summary>
        private bool RefreshSwingTracking()
        {
            if (!TryResolveActiveAttackState(out int layer, out AnimatorStateInfo info, out vMeleeAttackControl attackControl))
            {
                ResetSwingStateTracking();
                return false;
            }

            bool newSwing = false;
            float normalized = info.normalizedTime;
            if (!_swingTrackedByState
                || info.fullPathHash != _swingStateHash
                || layer != _swingStateLayer
                || normalized + SwingRestartTolerance < _swingStateLastNormalized)
            {
                CurrentSwingId++;
                newSwing = true;
                _swingStateHash = info.fullPathHash;
                _swingStateLayer = layer;
                _swingWindowIndex = -1;
            }

            _swingTrackedByState = true;
            _swingStateLastNormalized = normalized;
            _swingGatesRight = DrivesBodyPart(attackControl, "RightLowerArm");
            _swingGatesLeft = DrivesBodyPart(attackControl, "LeftLowerArm");

            int window = ResolveHitWindowIndex(info, attackControl);
            _swingInWindow = window >= 0;
            if (window >= 0)
            {
                // Strong B: each sweep window is its own swing (one hit each).
                if (_swingWindowIndex >= 0 && window != _swingWindowIndex && !newSwing)
                {
                    CurrentSwingId++;
                    newSwing = true;
                }

                _swingWindowIndex = window;
            }

            return newSwing;
        }

        private static bool DrivesBodyPart(vMeleeAttackControl attackControl, string bodyPart)
        {
            return attackControl != null
                && attackControl.bodyParts != null
                && attackControl.bodyParts.Contains(bodyPart);
        }

        private bool TryResolveActiveAttackState(
            out int layer,
            out AnimatorStateInfo info,
            out vMeleeAttackControl attackControl)
        {
            layer = -1;
            info = default;
            attackControl = null;
            if (_animator == null || !_animator.isActiveAndEnabled || _animator.runtimeAnimatorController == null)
                return false;

            int fullBody = PioneerStrongMeleeAnimStates.ResolveFullBodyLayer(_animator, _motor);
            if (TryResolveActiveAttackStateOnLayer(fullBody, out info, out attackControl))
            {
                layer = fullBody;
                return true;
            }

            int upper = _animator.GetLayerIndex("UpperBody");
            if (upper >= 0 && upper != fullBody && TryResolveActiveAttackStateOnLayer(upper, out info, out attackControl))
            {
                layer = upper;
                return true;
            }

            return false;
        }

        /// <summary>
        /// The incoming attack state owns the swing as soon as a crossfade into it starts, unless the
        /// outgoing state is still inside its own hit window and the incoming one is not yet.
        /// </summary>
        private bool TryResolveActiveAttackStateOnLayer(
            int layer,
            out AnimatorStateInfo info,
            out vMeleeAttackControl attackControl)
        {
            info = default;
            attackControl = null;
            if (layer < 0 || layer >= _animator.layerCount)
                return false;

            AnimatorStateInfo current = _animator.GetCurrentAnimatorStateInfo(layer);
            vMeleeAttackControl currentControl = ResolveAttackControl(layer, current);
            if (_animator.IsInTransition(layer))
            {
                AnimatorStateInfo next = _animator.GetNextAnimatorStateInfo(layer);
                vMeleeAttackControl nextControl = ResolveAttackControl(layer, next);
                if (nextControl != null)
                {
                    bool nextInWindow = ResolveHitWindowIndex(next, nextControl) >= 0;
                    bool currentInWindow = currentControl != null && ResolveHitWindowIndex(current, currentControl) >= 0;
                    if (nextInWindow || !currentInWindow)
                    {
                        info = next;
                        attackControl = nextControl;
                        return true;
                    }
                }
            }

            if (currentControl == null)
                return false;

            info = current;
            attackControl = currentControl;
            return true;
        }

        private vMeleeAttackControl ResolveAttackControl(int layer, AnimatorStateInfo state)
        {
            RuntimeAnimatorController controller = _animator.runtimeAnimatorController;
            if (controller != _attackControlCacheController)
            {
                _attackControlCache.Clear();
                _attackControlCacheController = controller;
            }

            long key = ((long)layer << 32) | (uint)state.fullPathHash;
            if (_attackControlCache.TryGetValue(key, out vMeleeAttackControl cached)
                && (cached != null || ReferenceEquals(cached, null)))
                return cached;

            vMeleeAttackControl found = null;
            StateMachineBehaviour[] behaviours = _animator.GetBehaviours(state.fullPathHash, layer);
            if (behaviours != null)
            {
                for (int i = 0; i < behaviours.Length; i++)
                {
                    if (behaviours[i] is vMeleeAttackControl attackControl)
                    {
                        found = attackControl;
                        break;
                    }
                }
            }

            _attackControlCache[key] = found;
            return found;
        }

        /// <summary>
        /// 0-based hit window the state is in right now, or -1. Strong releases use the measured
        /// per-slot windows below; every other attack state uses its vMeleeAttackControl window.
        /// Non-looping clips clamp at 1 so a state held past its end cannot reopen its window
        /// (vMeleeAttackControl's own normalizedTime % 1 test does).
        /// </summary>
        private static int ResolveHitWindowIndex(AnimatorStateInfo info, vMeleeAttackControl attackControl)
        {
            float t = info.loop ? info.normalizedTime % 1f : Mathf.Min(info.normalizedTime, 1f);
            if (PioneerStrongMeleeAnimStates.TryGetStrongReleaseSlot(info, out int slot))
                return StrongHitWindowIndex(slot, t);

            if (attackControl == null)
                return -1;

            return t >= attackControl.startDamage && t <= attackControl.endDamage ? 0 : -1;
        }

        /// <summary>
        /// Play-mode source of truth for melee swing Animator.speed (Combat Core profile.Live).
        /// Strong charge + Strong B use <see cref="DM_CombatCoreProfile.strongMeleeAnimSpeedMultiplier"/>.
        /// Weak SwordAttack / SwordRandomAttack A→B→C use per-slot light multipliers.
        /// </summary>
        private void ApplyMeleeAnimSpeedFromProfile()
        {
            if (_animator == null)
                return;

            if (TryResolveMeleeAnimSpeedMultiplier(out float multiplier))
            {
                if (!_meleeAnimSpeedOverrideApplied)
                {
                    _savedAnimatorSpeed = _animator.speed > 0.01f ? _animator.speed : 1f;
                    _meleeAnimSpeedOverrideApplied = true;
                }

                _animator.speed = multiplier;
                return;
            }

            if (_meleeAnimSpeedOverrideApplied)
            {
                _animator.speed = _savedAnimatorSpeed > 0.01f ? _savedAnimatorSpeed : 1f;
                _meleeAnimSpeedOverrideApplied = false;
            }
        }

        private bool TryResolveMeleeAnimSpeedMultiplier(out float multiplier)
        {
            multiplier = 1f;
            if (_animator == null)
                return false;

            int upper = _animator.GetLayerIndex("UpperBody");
            if (upper >= 0)
            {
                if (PioneerStrongMeleeAnimStates.TryResolveStrongAnimSpeedSlot(
                        _animator, upper, _shooterInput, out multiplier))
                    return true;

                if (PioneerLightMeleeAnimStates.TryResolveLightAnimSpeedSlot(_animator, upper, out multiplier))
                    return true;
            }

            int fullBody = PioneerStrongMeleeAnimStates.ResolveFullBodyLayer(_animator, _motor);
            if (PioneerStrongMeleeAnimStates.TryResolveStrongAnimSpeedSlot(
                    _animator, fullBody, _shooterInput, out multiplier))
                return true;

            if (PioneerLightMeleeAnimStates.TryResolveLightAnimSpeedSlot(_animator, fullBody, out multiplier))
                return true;

            return false;
        }

        private bool TryGateStrongReleaseDamage(out bool wantActive)
        {
            wantActive = false;
            // Gate any playing strong release swing, even if the post-release timer disarmed.
            if (_shooterInput == null)
                return false;

            if (!TryGetStrongReleaseNormalizedTime(out float normalizedTime, out int slot))
                return false;

            if (slot < 0)
                slot = _shooterInput.StrongReleaseSlot;

            float t = normalizedTime % 1f;
            wantActive = IsInStrongHitWindow(slot, t);
            return true;
        }

        // Normalized damage windows measured from the clips (right-hand sweep through the
        // front of the body), Oct 6 2026. A: slam only (not the overhead raise); opens at the
        // measured slam start (0.50) so the head/shoulder contact is inside the window.
        // B: two sweeps, each its own window so each lands one hit (half damage each, see
        // PioneerInvectorDamageBridge). C: the single forward sweep.
        private static readonly Vector2[] StrongHitWindowsA = { new Vector2(0.50f, 0.66f) };
        private static readonly Vector2[] StrongHitWindowsB = { new Vector2(0.34f, 0.48f), new Vector2(0.58f, 0.71f) };
        private static readonly Vector2[] StrongHitWindowsC = { new Vector2(0.30f, 0.48f) };

        private static bool IsInStrongHitWindow(int slot, float t)
        {
            return StrongHitWindowIndex(slot, t) >= 0;
        }

        private static int StrongHitWindowIndex(int slot, float t)
        {
            Vector2[] windows = slot == 1 ? StrongHitWindowsB : slot == 2 ? StrongHitWindowsC : StrongHitWindowsA;
            for (int i = 0; i < windows.Length; i++)
            {
                if (t >= windows[i].x && t <= windows[i].y)
                    return i;
            }

            return -1;
        }

        private bool TryGetStrongReleaseNormalizedTime(out float normalizedTime, out int slot)
        {
            normalizedTime = 0f;
            slot = -1;
            if (_animator == null)
                return false;

            int fullBody = PioneerStrongMeleeAnimStates.ResolveFullBodyLayer(_animator, _motor);
            if (TryGetStrongReleaseNormalizedTimeOnLayer(fullBody, out normalizedTime, out slot))
                return true;

            int upper = _animator.GetLayerIndex("UpperBody");
            if (upper >= 0 && TryGetStrongReleaseNormalizedTimeOnLayer(upper, out normalizedTime, out slot))
                return true;

            return false;
        }

        private bool TryGetStrongReleaseNormalizedTimeOnLayer(int layer, out float normalizedTime, out int slot)
        {
            normalizedTime = 0f;
            slot = -1;
            if (_animator == null || layer < 0)
                return false;

            AnimatorStateInfo current = _animator.GetCurrentAnimatorStateInfo(layer);
            if (PioneerStrongMeleeAnimStates.TryGetStrongReleaseSlot(current, out slot))
            {
                normalizedTime = current.normalizedTime;
                return true;
            }

            if (!_animator.IsInTransition(layer))
                return false;

            AnimatorStateInfo next = _animator.GetNextAnimatorStateInfo(layer);
            if (!PioneerStrongMeleeAnimStates.TryGetStrongReleaseSlot(next, out slot))
                return false;

            normalizedTime = next.normalizedTime;
            return true;
        }

        private void SetWeaponDamageActive(bool active)
        {
            SetWeaponDamageActive(active, true, true);
        }

        private void SetWeaponDamageActive(bool active, bool right, bool left)
        {
            if (_meleeManager == null)
                return;

            if (right
                && _meleeManager.rightWeapon != null
                && _meleeManager.rightWeapon.canApplyDamage != active)
            {
                _meleeManager.rightWeapon.SetActiveDamage(active);
            }

            if (left
                && _meleeManager.leftWeapon != null
                && _meleeManager.leftWeapon.canApplyDamage != active)
            {
                _meleeManager.leftWeapon.SetActiveDamage(active);
            }
        }

        private bool ShouldForceDisableWeaponHitboxes()
        {
            if (_shooterInput != null && _shooterInput.IsStrongChargePoseActive)
                return true;

            return false;
        }

        private void ForceDisableWeaponHitboxes()
        {
            if (_meleeManager == null)
                return;

            if (_meleeManager.rightWeapon != null && _meleeManager.rightWeapon.canApplyDamage)
                _meleeManager.rightWeapon.SetActiveDamage(false);

            if (_meleeManager.leftWeapon != null && _meleeManager.leftWeapon.canApplyDamage)
                _meleeManager.leftWeapon.SetActiveDamage(false);
        }

        private bool IsMeleeDamageActive()
        {
            if (_meleeManager == null)
                return false;

            if (_meleeManager.rightWeapon != null && _meleeManager.rightWeapon.canApplyDamage)
                return true;

            if (_meleeManager.leftWeapon != null && _meleeManager.leftWeapon.canApplyDamage)
                return true;

            return false;
        }
    }
}
