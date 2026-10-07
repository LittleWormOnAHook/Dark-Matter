using System.Collections.Generic;
using Invector;
using Invector.vMelee;
using Project.Survival;
using UnityEngine;

namespace Project.AI.Invector
{
    /// <summary>
    /// DM humanoid enemy melee sequencing on top of the stock Invector attack states.
    /// Light attacks become 1-3 swing chains by re-firing the WeakAttack trigger inside each swing
    /// (Invector@ShooterMelee chains SwordAttack A -> B at 0.75 and B -> C at 0.85). An occasional
    /// charged attack cross-fades into an existing heavy state, slows its wind-up as a readable tell
    /// and scales the weapon damage for that swing only. The punish rule forces the charged attack
    /// after N qualifying hits inside a short window, then resets.
    /// Every swing is its own Animator state with its own vMeleeAttackControl window, so each swing
    /// opens and closes its own damage window (the weapon trigger is re-enabled per window).
    /// </summary>
    [DefaultExecutionOrder(115)]
    [DisallowMultipleComponent]
    public class DMEnemyMeleeComboDriver : MonoBehaviour
    {
        public enum PunishSource
        {
            /// <summary>This enemy's unblocked melee hits that land on the player.</summary>
            EnemyHitsOnPlayer,
            /// <summary>Player damage taken by this enemy.</summary>
            PlayerHitsOnEnemy
        }

        [Header("Combos")]
        [SerializeField] private bool enableCombos = true;
        [Tooltip("Chance a light attack continues into a second swing.")]
        [SerializeField, Range(0f, 1f)] private float secondHitChance = 0.6f;
        [Tooltip("Chance a two-swing chain continues into a third swing.")]
        [SerializeField, Range(0f, 1f)] private float thirdHitChance = 0.45f;
        [Tooltip("Normalized time inside the current swing when the follow-up is queued. Keep it below the controller's chain exit time (Sword A 0.75, B 0.85).")]
        [SerializeField, Range(0.05f, 0.7f)] private float followUpQueueAt = 0.4f;
        [Tooltip("Latest normalized time a follow-up may still be queued.")]
        [SerializeField, Range(0.1f, 0.74f)] private float followUpQueueLatest = 0.7f;
        [Tooltip("A follow-up only fires while the target is within the enemy attack range times this value.")]
        [SerializeField] private float followUpRangeMultiplier = 1.25f;

        [Header("Charged attack")]
        [SerializeField] private bool enableChargedAttack = true;
        [Tooltip("Full path of an existing heavy attack state on the enemy controller. It must carry a vMeleeAttackControl.")]
        [SerializeField] private string chargedAttackState = "FullBody.Attacks.WeakAttacks.2HandWeaponAttack.C";
        [SerializeField] private string fullBodyLayerName = "FullBody";
        [Tooltip("Chance per attack opportunity to use the charged attack (outside the punish rule).")]
        [SerializeField, Range(0f, 1f)] private float chargedAttackChance = 0.12f;
        [Tooltip("Seconds after a charged attack before another can start (the punish rule ignores this but restarts it).")]
        [SerializeField] private float chargedAttackCooldown = 9f;
        [Tooltip("Weapon damage multiplier for the charged swing only (light swing = 1).")]
        [SerializeField] private float chargedDamageMultiplier = 1.8f;
        [SerializeField] private float chargedCrossFadeSeconds = 0.15f;

        [Header("Charged wind-up tell")]
        [Tooltip("Animator playback speed during the wind-up (1 = no slow-down).")]
        [SerializeField, Range(0.2f, 1f)] private float windupPlaybackSpeed = 0.55f;
        [Tooltip("Normalized time where the wind-up ends and normal speed resumes. The 2HandWeaponAttack.C damage window opens at 0.35.")]
        [SerializeField, Range(0f, 0.6f)] private float windupEndsAt = 0.28f;
        [SerializeField] private bool showWindupText = true;
        [Tooltip("World-space TMP glyph shown above the head during charged wind-up only.")]
        [SerializeField] private string windupText = "▲";
        [SerializeField] private Color windupTextColor = new Color(1f, 0.45f, 0.1f);
        [Tooltip("Extra world Y above the head bone (or root fallback height when no head bone).")]
        [SerializeField] private float windupTellHeadOffset = 0.35f;
        [Tooltip("Used when the humanoid Head bone is missing.")]
        [SerializeField] private float windupTellRootHeight = 2.1f;

        [Header("Punish rule")]
        [SerializeField] private bool enablePunish = true;
        [SerializeField] private PunishSource punishSource = PunishSource.EnemyHitsOnPlayer;
        [Tooltip("Qualifying hits inside the window that force the charged attack.")]
        [SerializeField, Min(1)] private int punishHitCount = 3;
        [SerializeField] private float punishWindowSeconds = 4f;

        [Header("Safety")]
        [Tooltip("A sequence never holds the enemy in its attack longer than this.")]
        [SerializeField] private float maxSequenceSeconds = 4.5f;

        [Header("Debug")]
        [Tooltip("Logs sequence start/end (editor + development builds). Off by default.")]
        [SerializeField] private bool debugLogSequences;

        private static readonly int WeakAttackHash = Animator.StringToHash("WeakAttack");

        private Animator _animator;
        private vMeleeManager _meleeManager;
        private EnemyHealth _health;
        private EnemyCombat _combat;
        private EnemyInvectorRagdollBridge _ragdollBridge;
        private int _fullBodyLayer = -1;
        private int _chargedHash;
        private bool _chargedStateValid;
        private RuntimeAnimatorController _cachedController;
        private readonly Dictionary<int, bool> _attackStateCache = new Dictionary<int, bool>();
        private readonly List<float> _punishHits = new List<float>();

        private bool _sequenceActive;
        private bool _isCharged;
        private int _plannedSwings;
        private int _swingsStarted;
        private int _currentSwingHash;
        private bool _followUpQueued;
        private bool _seenAttackState;
        private float _sequenceStartTime;
        private float _nextChargedTime;

        private float _swingNormalizedTime;
        private bool _hasSwingProgress;

        private vMeleeAttackObject _scaledWeapon;
        private float _scaledWeaponOriginalDamage;
        private bool _windupSpeedApplied;
        private bool _windupSpeedOrphaned;
        private bool _subscribedMelee;
        private bool _subscribedHealth;
        private EnemyFloatingText _windupTell;

        public bool IsSequenceActive => _sequenceActive;
        public bool IsChargedAttackActive => _sequenceActive && _isCharged;

        /// <summary>Punish rule armed (N qualifying hits inside the window). Read by the engagement director for token priority.</summary>
        public bool HasPunishArmed => IsPunishArmed();

        /// <summary>
        /// Normalized time of the swing currently playing (per swing, restarts on each chain swing) and whether it is
        /// the charged attack. Used for the Phase 3 per-swing tracking window. Last frame's value.
        /// </summary>
        public bool TryGetSwingProgress(out float normalizedTime, out bool charged)
        {
            normalizedTime = _swingNormalizedTime;
            charged = _isCharged;
            return _sequenceActive && _hasSwingProgress;
        }

        private void Awake()
        {
            _meleeManager = GetComponent<vMeleeManager>();
            _health = GetComponent<EnemyHealth>();
            _combat = GetComponent<EnemyCombat>();
            _ragdollBridge = GetComponent<EnemyInvectorRagdollBridge>();
            ResolveAnimator();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            EndSequence("disabled");
            ForceRestoreWindupSpeed();
        }

        /// <summary>Holds EnemyCombat's attack-pending flag while the swing sequence is still playing.</summary>
        public bool HoldsAttackPending(float elapsedSinceStart)
        {
            return _sequenceActive && elapsedSinceStart < maxSequenceSeconds;
        }

        /// <summary>Called by EnemyCombat right after a light melee attack was triggered.</summary>
        public void BeginLightSequence()
        {
            if (!ResolveAnimator())
                return;

            // The combat bridge set this swing's WeakAttack trigger earlier in this same frame and the
            // Animator has not consumed it yet. Never reset it here: doing so cancelled every regular
            // swing (only the cross-faded charged attack still played).
            if (_sequenceActive)
                EndSequence("superseded", resetAttackTrigger: false);

            int swings = 1;
            if (enableCombos && Random.value < secondHitChance)
            {
                swings = 2;
                if (Random.value < thirdHitChance)
                    swings = 3;
            }

            StartSequence(charged: false, swings);
            LogSequence("light start, planned swings " + _plannedSwings);
        }

        /// <summary>
        /// Starts the charged attack when the punish rule is armed, or on the random roll once the
        /// cooldown has expired. Requires a drawn melee weapon. Returns a minimum hold duration.
        /// </summary>
        public bool TryBeginChargedAttack(out float minimumDuration)
        {
            minimumDuration = 0f;
            if (!enableChargedAttack || _sequenceActive || !ResolveAnimator() || !_chargedStateValid)
                return false;

            if (_ragdollBridge != null && _ragdollBridge.IsHitStaggerActive)
                return false;

            vMeleeWeapon weapon = _meleeManager != null ? _meleeManager.rightWeapon : null;
            if (weapon == null || !weapon.gameObject.activeInHierarchy || weapon.damage == null)
                return false;

            bool punish = IsPunishArmed();
            if (!punish)
            {
                if (Time.time < _nextChargedTime || Random.value >= chargedAttackChance)
                    return false;
            }

            EnemyInvectorCombatShutdown.EnableMeleeIfSafe(gameObject);
            if (_meleeManager != null && !_meleeManager.enabled)
                _meleeManager.enabled = true;

            _animator.ResetTrigger(WeakAttackHash);
            _animator.CrossFadeInFixedTime(_chargedHash, Mathf.Max(0f, chargedCrossFadeSeconds), _fullBodyLayer);

            _scaledWeapon = weapon;
            _scaledWeaponOriginalDamage = weapon.damage.damageValue;
            weapon.damage.damageValue = _scaledWeaponOriginalDamage * Mathf.Max(0f, chargedDamageMultiplier);

            _nextChargedTime = Time.time + Mathf.Max(0f, chargedAttackCooldown);
            _punishHits.Clear();
            StartSequence(charged: true, swings: 1);
            LogSequence(punish ? "charged start (punish)" : "charged start (roll)");

            ShowWindupTell();

            EnemyNoiseEvents.RaiseNoise(transform.position, 6f, gameObject);
            minimumDuration = 0.5f;
            return true;
        }

        /// <summary>Stagger / guard-break interrupt: drop the chain and restore the charged swing state.</summary>
        public void CancelSequence()
        {
            EndSequence("cancelled");
        }

        private void StartSequence(bool charged, int swings)
        {
            _sequenceActive = true;
            _isCharged = charged;
            _plannedSwings = Mathf.Clamp(swings, 1, 3);
            _swingsStarted = 0;
            _currentSwingHash = 0;
            _followUpQueued = false;
            _seenAttackState = false;
            _sequenceStartTime = Time.time;
            _hasSwingProgress = false;
            _swingNormalizedTime = 0f;
        }

        private void Update()
        {
            TickOrphanedWindupSpeed();

            if (!_sequenceActive)
                return;

            if (!ResolveAnimator() || IsInterrupted() || Time.time - _sequenceStartTime > maxSequenceSeconds)
            {
                EndSequence("interrupted or timed out");
                return;
            }

            AnimatorStateInfo current = _animator.GetCurrentAnimatorStateInfo(_fullBodyLayer);
            bool inTransition = _animator.IsInTransition(_fullBodyLayer);
            AnimatorStateInfo next = inTransition ? _animator.GetNextAnimatorStateInfo(_fullBodyLayer) : current;
            bool currentIsAttack = IsAttackState(current.fullPathHash);
            bool nextIsAttack = inTransition && IsAttackState(next.fullPathHash);

            if (!currentIsAttack && !nextIsAttack)
            {
                // Swing (or chain) finished and the layer left the attack states, or the trigger never took.
                if (_seenAttackState)
                    EndSequence("finished after " + _swingsStarted + " swing(s)");
                else if (Time.time - _sequenceStartTime > 0.75f)
                    EndSequence("no attack state entered");
                return;
            }

            _seenAttackState = true;
            AnimatorStateInfo swing = nextIsAttack ? next : current;
            if (swing.fullPathHash != _currentSwingHash)
            {
                _currentSwingHash = swing.fullPathHash;
                _swingsStarted++;
                _followUpQueued = false;
            }

            _swingNormalizedTime = swing.normalizedTime;
            _hasSwingProgress = true;

            if (_isCharged)
            {
                TickChargedWindup(swing);
                return;
            }

            if (_followUpQueued || inTransition || _swingsStarted >= _plannedSwings)
                return;

            float t = swing.normalizedTime;
            if (t < followUpQueueAt)
                return;

            if (t > followUpQueueLatest || !IsTargetInFollowUpRange())
            {
                _plannedSwings = _swingsStarted;
                return;
            }

            _animator.SetTrigger(WeakAttackHash);
            _followUpQueued = true;
        }

        private void TickChargedWindup(AnimatorStateInfo swing)
        {
            if (swing.fullPathHash != _chargedHash)
                return;

            if (swing.normalizedTime < windupEndsAt)
            {
                if (!_windupSpeedApplied && windupPlaybackSpeed < 0.999f && Mathf.Abs(_animator.speed - 1f) < 0.01f)
                {
                    _animator.speed = windupPlaybackSpeed;
                    _windupSpeedApplied = true;
                }

                return;
            }

            HideWindupTell();
            EndWindupSpeed();
        }

        private void EndWindupSpeed()
        {
            if (!_windupSpeedApplied)
                return;

            _windupSpeedApplied = false;
            if (_animator == null)
                return;

            if (Mathf.Abs(_animator.speed - windupPlaybackSpeed) < 0.001f)
            {
                _animator.speed = 1f;
                return;
            }

            // Hit-stop froze the animator (speed 0) mid wind-up and will restore our slowed value later.
            if (_animator.speed < 0.001f)
                _windupSpeedOrphaned = true;
        }

        private void TickOrphanedWindupSpeed()
        {
            if (!_windupSpeedOrphaned || _animator == null)
                return;

            if (Mathf.Abs(_animator.speed - windupPlaybackSpeed) < 0.001f)
            {
                _animator.speed = 1f;
                _windupSpeedOrphaned = false;
            }
            else if (_animator.speed > 0.001f)
            {
                _windupSpeedOrphaned = false;
            }
        }

        private void ForceRestoreWindupSpeed()
        {
            if (_animator != null && (_windupSpeedApplied || _windupSpeedOrphaned) &&
                Mathf.Abs(_animator.speed - windupPlaybackSpeed) < 0.001f)
            {
                _animator.speed = 1f;
            }

            _windupSpeedApplied = false;
            _windupSpeedOrphaned = false;
        }

        private void EndSequence(string reason, bool resetAttackTrigger = true)
        {
            bool wasActive = _sequenceActive;
            _sequenceActive = false;
            _isCharged = false;
            _followUpQueued = false;
            _currentSwingHash = 0;
            _hasSwingProgress = false;

            // Clears a follow-up trigger that was queued but never consumed (it would start a stray
            // attack from Null.Null later). Skipped when a fresh swing's trigger was just set.
            if (resetAttackTrigger && _animator != null)
                _animator.ResetTrigger(WeakAttackHash);

            HideWindupTell();
            EndWindupSpeed();
            RestoreChargedDamage();

            if (wasActive && _combat != null)
                _combat.NotifyAttackSequenceEnded();

            if (wasActive)
                LogSequence("end: " + reason);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void LogSequence(string message)
        {
            if (debugLogSequences)
                Debug.Log("[DMEnemyCombo] " + name + ": " + message, this);
        }

        private void RestoreChargedDamage()
        {
            if (_scaledWeapon == null)
                return;

            if (_scaledWeapon.damage != null)
                _scaledWeapon.damage.damageValue = _scaledWeaponOriginalDamage;

            _scaledWeapon = null;
        }

        private void ShowWindupTell()
        {
            HideWindupTell();
            if (!showWindupText || string.IsNullOrEmpty(windupText))
                return;

            Transform follow = ResolveWindupTellAnchor(out float heightOffset);
            _windupTell = EnemyFloatingText.ShowFollow(follow, windupText, windupTextColor, heightOffset);
        }

        private void HideWindupTell()
        {
            if (_windupTell == null)
                return;

            _windupTell.Dismiss();
            _windupTell = null;
        }

        private Transform ResolveWindupTellAnchor(out float heightOffset)
        {
            if (_animator != null)
            {
                Transform head = _animator.GetBoneTransform(HumanBodyBones.Head);
                if (head != null)
                {
                    heightOffset = Mathf.Max(0f, windupTellHeadOffset);
                    return head;
                }
            }

            heightOffset = Mathf.Max(0.5f, windupTellRootHeight);
            return transform;
        }

        private bool IsInterrupted()
        {
            if (_health != null && _health.IsDead)
                return true;

            if (_ragdollBridge != null && (_ragdollBridge.IsHitStaggerActive || _ragdollBridge.IsCorpseRagdolled))
                return true;

            return !_animator.isActiveAndEnabled;
        }

        private bool IsTargetInFollowUpRange()
        {
            if (_combat == null || !_combat.HasLivingTarget())
                return false;

            Transform target = _combat.CurrentTarget;
            if (target == null)
                return false;

            Vector3 delta = target.position - transform.position;
            delta.y = 0f;
            float range = _combat.ResolveEffectiveAttackRange(target) * Mathf.Max(1f, followUpRangeMultiplier);
            return delta.sqrMagnitude <= range * range;
        }

        private bool ResolveAnimator()
        {
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>(true);

            if (_animator == null)
                return false;

            if (_cachedController != _animator.runtimeAnimatorController)
            {
                _cachedController = _animator.runtimeAnimatorController;
                _attackStateCache.Clear();
                _fullBodyLayer = _cachedController != null ? _animator.GetLayerIndex(fullBodyLayerName) : -1;
                _chargedHash = string.IsNullOrEmpty(chargedAttackState) ? 0 : Animator.StringToHash(chargedAttackState);
                _chargedStateValid = _fullBodyLayer >= 0 && _chargedHash != 0 &&
                                     _animator.HasState(_fullBodyLayer, _chargedHash) &&
                                     IsAttackState(_chargedHash);
            }

            return _fullBodyLayer >= 0;
        }

        private bool IsAttackState(int fullPathHash)
        {
            if (fullPathHash == 0 || _fullBodyLayer < 0)
                return false;

            if (_attackStateCache.TryGetValue(fullPathHash, out bool cached))
                return cached;

            bool isAttack = false;
            StateMachineBehaviour[] behaviours = _animator.GetBehaviours(fullPathHash, _fullBodyLayer);
            if (behaviours != null)
            {
                for (int i = 0; i < behaviours.Length; i++)
                {
                    if (behaviours[i] is vMeleeAttackControl)
                    {
                        isAttack = true;
                        break;
                    }
                }
            }

            _attackStateCache[fullPathHash] = isAttack;
            return isAttack;
        }

        private bool IsPunishArmed()
        {
            if (!enablePunish)
                return false;

            PrunePunishHits();
            return _punishHits.Count >= Mathf.Max(1, punishHitCount);
        }

        private void RecordPunishHit()
        {
            if (!enablePunish)
                return;

            _punishHits.Add(Time.time);
            PrunePunishHits();
        }

        private void PrunePunishHits()
        {
            float cutoff = Time.time - Mathf.Max(0.1f, punishWindowSeconds);
            for (int i = _punishHits.Count - 1; i >= 0; i--)
            {
                if (_punishHits[i] < cutoff)
                    _punishHits.RemoveAt(i);
            }
        }

        private void Subscribe()
        {
            if (!_subscribedMelee && _meleeManager != null && _meleeManager.onDamageHit != null)
            {
                _meleeManager.onDamageHit.AddListener(HandleMeleeDamageHit);
                _subscribedMelee = true;
            }

            if (!_subscribedHealth && _health != null)
            {
                _health.DamagedWithSource += HandleDamagedWithSource;
                _subscribedHealth = true;
            }
        }

        private void Unsubscribe()
        {
            if (_subscribedMelee && _meleeManager != null && _meleeManager.onDamageHit != null)
                _meleeManager.onDamageHit.RemoveListener(HandleMeleeDamageHit);
            _subscribedMelee = false;

            if (_subscribedHealth && _health != null)
                _health.DamagedWithSource -= HandleDamagedWithSource;
            _subscribedHealth = false;
        }

        private void HandleMeleeDamageHit(vHitInfo hitInfo)
        {
            if (punishSource != PunishSource.EnemyHitsOnPlayer || hitInfo == null || hitInfo.targetIsBlocking)
                return;

            if (_sequenceActive && _isCharged)
                return;

            if (hitInfo.targetCollider != null && IsPlayer(hitInfo.targetCollider.transform))
                RecordPunishHit();
        }

        private void HandleDamagedWithSource(float damage, GameObject source, bool isCritical)
        {
            if (punishSource != PunishSource.PlayerHitsOnEnemy || damage <= 0f || source == null)
                return;

            if (IsPlayer(source.transform))
                RecordPunishHit();
        }

        private static bool IsPlayer(Transform candidate)
        {
            if (candidate == null)
                return false;

            if (candidate.root.CompareTag("Player"))
                return true;

            return candidate.GetComponentInParent<SurvivalStats>() != null;
        }
    }
}
