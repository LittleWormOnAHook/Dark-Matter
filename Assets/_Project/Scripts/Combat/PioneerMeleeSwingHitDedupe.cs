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

            int swingId = PioneerMeleeDamageWindowTracker.CurrentSwingId;
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
    /// Increments <see cref="CurrentSwingId"/> when Invector enables melee damage on an equipped weapon.
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
            if (ShouldForceDisableWeaponHitboxes())
            {
                ForceDisableWeaponHitboxes();
                _wasDamageActive = false;
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

            bool active = IsMeleeDamageActive();
            if (active && !_wasDamageActive)
                CurrentSwingId++;

            _wasDamageActive = active;
        }

        private void LateUpdate()
        {
            ApplyMeleeAnimSpeedFromProfile();

            // Re-apply the strong hit window after the Animator ran: vMeleeAttackControl toggles
            // damage from inside the Animator update, which otherwise leaks one physics step of
            // damage at its own (wider) window edges, e.g. during Strong A's raise or B's windup.
            if (ShouldForceDisableWeaponHitboxes())
                return;

            if (TryGateStrongReleaseDamage(out bool wantStrongDamage))
            {
                if (wantStrongDamage && !_wasDamageActive)
                    CurrentSwingId++;

                SetWeaponDamageActive(wantStrongDamage);
                _wasDamageActive = wantStrongDamage;
            }
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
        // front of the body), Oct 6 2026. A: slam only (not the overhead raise).
        // B: two sweeps, each its own window so each lands one hit (half damage each, see
        // PioneerInvectorDamageBridge). C: the single forward sweep.
        private static readonly Vector2[] StrongHitWindowsA = { new Vector2(0.52f, 0.66f) };
        private static readonly Vector2[] StrongHitWindowsB = { new Vector2(0.34f, 0.48f), new Vector2(0.58f, 0.71f) };
        private static readonly Vector2[] StrongHitWindowsC = { new Vector2(0.30f, 0.48f) };

        private static bool IsInStrongHitWindow(int slot, float t)
        {
            Vector2[] windows = slot == 1 ? StrongHitWindowsB : slot == 2 ? StrongHitWindowsC : StrongHitWindowsA;
            for (int i = 0; i < windows.Length; i++)
            {
                if (t >= windows[i].x && t <= windows[i].y)
                    return true;
            }

            return false;
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
            if (_meleeManager == null)
                return;

            if (_meleeManager.rightWeapon != null
                && _meleeManager.rightWeapon.canApplyDamage != active)
            {
                _meleeManager.rightWeapon.SetActiveDamage(active);
            }

            if (_meleeManager.leftWeapon != null
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
