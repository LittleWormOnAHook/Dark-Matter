using Project.AI;
using Project.Companions;
using Project.Data;
using Project.Inventory;
using Project.Survival;
using UnityEngine;
using ECM2;

namespace Project.Player
{
    /// <summary>
    /// Sole owner of GKC animator parameters and combat action requests for the player character model.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    [DisallowMultipleComponent]
    public class PlayerGkcAnimatorDriver : MonoBehaviour
    {
        [SerializeField] private GkcActionCatalog actionCatalog;

        private Character _character;
        private PlayerController _playerController;
        private SurvivalStats _survivalStats;
        private EquipmentController _equipment;
        private CompanionFollowController _companionFollow;
        private CompanionEquipmentVisual _companionEquipment;
        private Animator _animator;

        private GkcCombatAction _activeAction = GkcCombatAction.None;
        private GkcActionCatalogEntry _activeEntry;
        private float _actionEndTime;
        private float _actionIdClearTime;
        private float _nextEngagementScanTime;
        private bool _cachedUnarmedEngaged;

        private bool _movementSpeedAvailable;
        private bool _movingBoolAvailable;
        private bool _capabilitiesCached;

        private float _prevBodyYaw;
        private bool _prevBodyYawInitialized;

        private bool _actionRequiredStrafeMode;
        private bool _actionRequiredActionActive;
        private bool _actionUsedUpperBodyActive;

        private float _hitReactionOverlayEndTime;

        private const float ActionEndBufferSeconds = 0.2f;
        private const float ActionTimeoutExtensionSeconds = 0.35f;

        public bool IsActionBlockingLocomotion =>
            _activeAction == GkcCombatAction.Charge1H
            || _activeAction == GkcCombatAction.Charge2H
            || _activeAction == GkcCombatAction.ChargeAxe
            || _activeAction == GkcCombatAction.HitReactionArmed
            || _activeAction == GkcCombatAction.HitReactionUnarmed
            || (IsMeleeActionActive() && _activeEntry != null && _activeEntry.requiresActionActive);

        public GkcActionCatalog Catalog => actionCatalog;

        public void ConfigureForCompanion(
            CompanionFollowController followController,
            CompanionEquipmentVisual equipmentVisual,
            GkcActionCatalog catalogOverride = null)
        {
            _companionFollow = followController;
            _companionEquipment = equipmentVisual;
            if (catalogOverride != null)
                actionCatalog = catalogOverride;

            EnsureActionCatalogLoaded();
            EnsureCompanionAnimatorReady();
        }

        private void EnsureCompanionAnimatorReady()
        {
            if (_animator == null)
            {
                _animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>(true);
                if (_animator == null)
                    return;
            }

            if (_animator.runtimeAnimatorController == null)
                return;

            _capabilitiesCached = false;
            Transform body = transform.parent != null ? transform.parent : transform;
            _companionLastSamplePosition = body.position;
            EnsureGroundedLocomotionState();
            InitializeAnimator();
        }

        private void EnsureGroundedLocomotionState()
        {
            if (_animator == null)
                return;

            int groundedHash = Animator.StringToHash(GkcAnimatorConstants.GroundedState);
            if (!_animator.HasState(0, groundedHash))
                return;

            AnimatorStateInfo current = _animator.GetCurrentAnimatorStateInfo(0);
            if (!current.IsName(GkcAnimatorConstants.GroundedState))
                _animator.CrossFadeInFixedTime(GkcAnimatorConstants.GroundedState, 0.12f, 0, 0f);
        }

        public bool RequestHitReaction(bool armedMelee)
        {
            if (_animator == null || actionCatalog == null)
                return false;

            if (ShouldPreserveCombatActionForHitReaction())
                return RequestHitReactionOverlay();

            if (_activeAction != GkcCombatAction.None)
                EndActiveAction();

            EnsureGkcDefaultLayerWeights();
            SetBool(GkcAnimatorConstants.StrafeModeActive, false);
            SetBool(GkcAnimatorConstants.AimingModeActive, false);
            SetFloat(GkcAnimatorConstants.StrafeId, 0f);
            SetBool(GkcAnimatorConstants.ActionActive, false);
            SetBool(GkcAnimatorConstants.ActionActiveUpperBody, false);
            SetInteger(GkcAnimatorConstants.ActionId, 0);

            GkcCombatAction action = armedMelee
                ? GkcCombatAction.HitReactionArmed
                : GkcCombatAction.HitReactionUnarmed;

            return RequestAction(action, GkcAnimatorConstants.DefaultHitReactionDuration);
        }

        private bool ShouldPreserveCombatActionForHitReaction() =>
            IsBlockActive()
            || IsMeleeActionActive()
            || IsBaseLayerAttackPlaying();

        private bool RequestHitReactionOverlay()
        {
            int layer = ResolveLayer(GkcAnimatorConstants.UpperBodyCombatLayer);
            if (layer < 0)
                return false;

            EnsureLayerWeight(GkcAnimatorConstants.UpperBodyCombatLayer, 1f);

            const float crossFade = 0.06f;
            if (!TryCrossFadeState(GkcAnimatorConstants.HitReactionOverlayState, crossFade, layer)
                && !TryCrossFadeState("Block Hit Reaction Sword 2 Hands", crossFade, layer))
            {
                return false;
            }

            _hitReactionOverlayEndTime = Time.time + GkcAnimatorConstants.DefaultHitReactionOverlayDuration;
            return true;
        }

        private void TickHitReactionOverlay()
        {
            if (_hitReactionOverlayEndTime <= 0f || Time.time < _hitReactionOverlayEndTime)
                return;

            _hitReactionOverlayEndTime = 0f;

            if (IsMeleeActionActive())
                ApplyMeleeUpperBodyLayerSuppression();
            else if (!IsBlockActive())
                EnsureGkcDefaultLayerWeights();
        }

        private void Awake()
        {
            _character = GetComponentInParent<Character>();
            _playerController = GetComponentInParent<PlayerController>();
            _survivalStats = GetComponentInParent<SurvivalStats>();
            _equipment = GetComponentInParent<EquipmentController>();
            _companionFollow = GetComponentInParent<CompanionFollowController>();
            _companionEquipment = GetComponentInParent<CompanionEquipmentVisual>();
            _animator = GetComponent<Animator>();
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>(true);
            if (_animator == null && _character != null)
                _animator = _character.GetAnimator();

            EnsureActionCatalogLoaded();
        }

        private void Start()
        {
            if (IsCompanionMode())
                EnsureCompanionAnimatorReady();
        }

        private void EnsureActionCatalogLoaded()
        {
            if (actionCatalog != null)
                return;

            actionCatalog = Resources.Load<GkcActionCatalog>("Animation/GkcActionCatalog");
            if (actionCatalog != null)
                return;

            CompanionGkcAnimationAssets assets = Resources.Load<CompanionGkcAnimationAssets>(
                PioneerCompanionDefaults.GkcAnimationAssetsResourcesPath);
            if (assets != null)
                actionCatalog = assets.actionCatalog;
        }

        private bool IsCompanionMode() => _companionFollow != null && _character == null;

        private void OnEnable()
        {
            InitializeAnimator();
        }

        private void InitializeAnimator()
        {
            if (_animator == null || _animator.runtimeAnimatorController == null)
                return;

            _animator.SetLayerWeight(0, 1f);
            EnsureGkcDefaultLayerWeights();

            SetFloat(GkcAnimatorConstants.PlayerModeId, GkcAnimatorConstants.PlayerModeTankMovement);
            SetFloat(GkcAnimatorConstants.IdleId, GkcAnimatorConstants.IdleIdDefault);
            SetFloat(GkcAnimatorConstants.PlayerStatusId, GkcAnimatorConstants.PlayerStatusNormal);
            SetFloat(GkcAnimatorConstants.Forward, 0f);
            SetFloat(GkcAnimatorConstants.Turn, 0f);
            SetBool(GkcAnimatorConstants.ActionActive, false);
            SetBool(GkcAnimatorConstants.ActionActiveUpperBody, false);
            SetBool(GkcAnimatorConstants.StrafeModeActive, false);
            SetBool(GkcAnimatorConstants.Dead, false);
            SetFloat(GkcAnimatorConstants.WeaponId, GkcAnimatorConstants.WeaponIdUnarmed);
            SetBool(GkcAnimatorConstants.CarryingWeapon, false);
            SetInteger(GkcAnimatorConstants.RightArmId, 0);
            SetInteger(GkcAnimatorConstants.LeftArmId, 0);
        }

        private void Update()
        {
            if (_animator == null || _animator.runtimeAnimatorController == null)
                return;

            if (_survivalStats != null && _survivalStats.IsDead)
            {
                SetFloat(GkcAnimatorConstants.Forward, 0f);
                SetFloat(GkcAnimatorConstants.Turn, 0f);
                return;
            }

            TickActions();
            TickHitReactionOverlay();

            if (IsCompanionMode())
                return;

            UpdateLocomotion();
        }

        private void LateUpdate()
        {
            if (!IsCompanionMode() || _animator == null || _animator.runtimeAnimatorController == null)
                return;

            UpdateCompanionLocomotion();
        }

        public bool RequestAction(GkcCombatAction action, float? durationOverride = null, float attackSpeed = 1f)
        {
            if (_animator == null || action == GkcCombatAction.None || actionCatalog == null)
                return false;

            if (IsPowerAttackAction(action) && IsChargeAction(_activeAction))
                EndActiveAction();

            GkcWeaponKind weaponKind = ResolveCurrentWeaponKind();
            if (!actionCatalog.TryGet(action, weaponKind, out GkcActionCatalogEntry entry))
                return false;

            if (action == GkcCombatAction.Death)
                SetBool(GkcAnimatorConstants.Dead, true);
            else if (action == GkcCombatAction.GetUp)
                SetBool(GkcAnimatorConstants.Dead, false);

            ApplyCombatContext(weaponKind, entry);

            if (entry.requiresStrafeMode)
                SetBool(GkcAnimatorConstants.StrafeModeActive, true);

            if (entry.useDirectCrossFade && !string.IsNullOrWhiteSpace(entry.stateName))
            {
                PlayCrossFade(entry);
            }

            if (entry.actionId > 0)
            {
                SetInteger(GkcAnimatorConstants.ActionId, entry.actionId);
                if (entry.clearActionIdAfterTrigger)
                    _actionIdClearTime = Time.time + 0.12f;
            }

            if (entry.requiresActionActive)
                SetBool(GkcAnimatorConstants.ActionActive, true);

            if (entry.useActionActiveUpperBody)
            {
                SetBool(GkcAnimatorConstants.ActionActiveUpperBody, true);
                if (action == GkcCombatAction.Block)
                    SetFloat(GkcAnimatorConstants.ShieldActive, 1f);
            }

            if (entry.actionId > 0 && !entry.useDirectCrossFade && action != GkcCombatAction.Block)
                SetBool(GkcAnimatorConstants.AimingModeActive, false);

            if (HasFloat(GkcAnimatorConstants.AttackAnimSpeed))
                _animator.SetFloat(GkcAnimatorConstants.AttackAnimSpeed, attackSpeed);

            float duration = (durationOverride ?? entry.defaultDuration) / Mathf.Max(0.1f, attackSpeed);
            _activeAction = action;
            _activeEntry = entry;
            _actionRequiredStrafeMode = entry.requiresStrafeMode;
            _actionRequiredActionActive = entry.requiresActionActive;
            _actionUsedUpperBodyActive = entry.useActionActiveUpperBody;
            _actionEndTime = Time.time + Mathf.Max(0.35f, duration + ActionEndBufferSeconds);
            return true;
        }

        private bool IsMeleeActionActive()
        {
            if (_activeAction == GkcCombatAction.Block)
                return false;

            if (_activeEntry == null || _activeAction == GkcCombatAction.None)
                return false;

            if (_activeEntry.useDirectCrossFade)
                return false;

            return _activeEntry.requiresActionActive || _activeEntry.actionId > 0;
        }

        private bool IsBlockActive() => _activeAction == GkcCombatAction.Block;

        private bool ShouldFreezeLocomotionForMeleeAction()
        {
            if (_activeEntry == null || _activeAction == GkcCombatAction.None)
                return false;

            if (_activeAction == GkcCombatAction.Block)
                return false;

            if (_activeEntry.useDirectCrossFade)
                return false;

            if (_activeEntry.requiresActionActive)
                return true;

            return _activeEntry.actionId > 0;
        }

        public float ResolveActionDuration(GkcCombatAction action, float attackSpeed = 1f)
        {
            if (actionCatalog == null || action == GkcCombatAction.None)
                return GkcAnimatorConstants.DefaultActionDuration / Mathf.Max(0.1f, attackSpeed);

            GkcWeaponKind weaponKind = ResolveCurrentWeaponKind();
            if (!actionCatalog.TryGet(action, weaponKind, out GkcActionCatalogEntry entry))
                return GkcAnimatorConstants.DefaultActionDuration / Mathf.Max(0.1f, attackSpeed);

            return entry.defaultDuration / Mathf.Max(0.1f, attackSpeed);
        }

        private bool IsBaseLayerAttackPlaying()
        {
            if (_animator == null)
                return false;

            if (_activeEntry != null && !string.IsNullOrWhiteSpace(_activeEntry.stateName))
                return TryGetActionLayerStateInfo(out AnimatorStateInfo attackState)
                    && attackState.normalizedTime < 0.98f;

            int layer = 0;
            if (_animator.IsInTransition(layer))
            {
                AnimatorStateInfo nextState = _animator.GetNextAnimatorStateInfo(layer);
                if (nextState.IsTag("Attack"))
                    return nextState.normalizedTime < 0.98f;
            }

            AnimatorStateInfo baseState = _animator.GetCurrentAnimatorStateInfo(layer);
            return baseState.IsTag("Attack") && baseState.normalizedTime < 0.98f;
        }

        public void TickActions()
        {
            if (_animator == null)
                return;

            if (_actionIdClearTime > 0f && Time.time >= _actionIdClearTime)
            {
                SetInteger(GkcAnimatorConstants.ActionId, 0);
                _actionIdClearTime = 0f;
            }

            if (_activeAction == GkcCombatAction.None)
                return;

            if (TryCompleteActiveActionByAnimatorState())
                return;

            if (Time.time >= _actionEndTime)
            {
                if (_activeAction == GkcCombatAction.Block)
                {
                    _actionEndTime = Time.time + 2f;
                    return;
                }

                if (IsMeleeActionActive() && IsBaseLayerAttackPlaying())
                {
                    ExtendActionEndTimeIfNeeded();
                    return;
                }

                EndActiveAction();
            }
        }

        public void EndActiveAction()
        {
            _activeAction = GkcCombatAction.None;
            _activeEntry = null;
            _actionEndTime = 0f;
            _actionIdClearTime = 0f;
            SetInteger(GkcAnimatorConstants.ActionId, 0);

            if (_actionRequiredActionActive)
                SetBool(GkcAnimatorConstants.ActionActive, false);

            if (_actionUsedUpperBodyActive)
            {
                SetBool(GkcAnimatorConstants.ActionActiveUpperBody, false);
                SetFloat(GkcAnimatorConstants.ShieldActive, 0f);
            }

            if (_actionRequiredStrafeMode)
                SetBool(GkcAnimatorConstants.StrafeModeActive, false);

            _actionRequiredStrafeMode = false;
            _actionRequiredActionActive = false;
            _actionUsedUpperBodyActive = false;

            EnsureGkcDefaultLayerWeights();
        }

        public void CancelBlock()
        {
            if (_activeAction == GkcCombatAction.Block)
                EndActiveAction();
        }

        private void UpdateLocomotion()
        {
            if (_animator == null)
                return;

            if (IsCompanionMode())
            {
                UpdateCompanionLocomotion();
                return;
            }

            if (_character == null)
                return;

            PlayerLootAnimationController lootAnimation = GetComponent<PlayerLootAnimationController>();
            if (lootAnimation != null && lootAnimation.IsLooting)
            {
                SetFloat(GkcAnimatorConstants.Forward, 0f);
                SetFloat(GkcAnimatorConstants.Turn, 0f);
                return;
            }

            bool blockActive = IsBlockActive();
            bool meleeActionActive = IsMeleeActionActive();

            if (blockActive)
            {
                EnsureGkcDefaultLayerWeights();
                MaintainBlockHoldContext();
            }
            else if (meleeActionActive)
            {
                ApplyMeleeUpperBodyLayerSuppression();
                MaintainActionCombatContext();
            }

            if (IsActionBlockingLocomotion)
            {
                ApplyMinimalCombatLocomotionHold();
                ApplyMinimalEnvironmentState();
                if (!meleeActionActive && !blockActive)
                    EnsureGkcDefaultLayerWeights();
                return;
            }

            bool freezeLocomotion = ShouldFreezeLocomotionForMeleeAction() || IsBaseLayerAttackPlaying();

            if (!meleeActionActive)
                EnsureGkcDefaultLayerWeights();

            CacheAnimatorCapabilities();

            PlayerLocomotionAnimationSettings locomotion = _playerController != null
                ? _playerController.LocomotionAnimations
                : null;

            float deltaTime = Time.deltaTime;
            float forwardSmoothTime = locomotion != null ? locomotion.locomotionSmoothTime : 0.18f;
            float leanSmoothTime = locomotion != null ? locomotion.leanBlendSmoothTime : 0.24f;
            float animSpeedSmoothTime = locomotion != null ? locomotion.locomotionAnimSpeedSmoothTime : 0.12f;

            bool isMoving = _character.GetSpeed() > GkcAnimatorConstants.MovingSpeedThreshold;
            bool isSprinting = _playerController != null && _playerController.IsSprinting;
            bool hasActiveMelee = _equipment != null && _equipment.HasActiveMeleeWeapon();
            ItemData equippedItem = hasActiveMelee ? _equipment.SelectedHotbarItem : null;
            bool useArmedLocomotion = hasActiveMelee;

            ResolveLocomotionBlend(
                locomotion,
                isMoving,
                isSprinting,
                out float forwardAmount,
                out float turnAmount,
                out bool driveStrafeTurnAxis);

            if (_movementSpeedAvailable && !freezeLocomotion)
            {
                float gkcMovementSpeed = ResolveGkcMovementSpeed(isMoving, isSprinting, hasActiveMelee);
                _animator.SetFloat(GkcAnimatorConstants.MovementSpeed, gkcMovementSpeed, animSpeedSmoothTime, deltaTime);
            }

            if (_movingBoolAvailable && !freezeLocomotion)
            {
                SetBool(
                    GkcAnimatorConstants.Moving,
                    isMoving || _character.GetSpeed() > GkcAnimatorConstants.MovingSpeedThreshold);
            }

            bool unarmedCombatEngaged = IsUnarmedCombatEngaged();
            Vector2 moveInput = _playerController != null ? _playerController.MoveInput : Vector2.zero;
            if (moveInput.sqrMagnitude > 1f)
                moveInput.Normalize();

            if (!freezeLocomotion)
            {
                if (useArmedLocomotion)
                {
                    UpdateArmedLocomotion(
                        equippedItem,
                        isMoving,
                        isSprinting,
                        moveInput,
                        forwardAmount,
                        turnAmount,
                        forwardSmoothTime,
                        leanSmoothTime,
                        deltaTime);
                }
                else
                {
                    UpdateUnarmedLocomotion(
                        locomotion,
                        isMoving,
                        isSprinting,
                        unarmedCombatEngaged,
                        moveInput,
                        forwardAmount,
                        turnAmount,
                        forwardSmoothTime,
                        leanSmoothTime,
                        deltaTime);
                }
            }

            ApplyMinimalEnvironmentState();

            if (_character.IsGrounded() && !freezeLocomotion)
            {
                float runCycle = Mathf.Repeat(_animator.GetCurrentAnimatorStateInfo(0).normalizedTime + 0.2f, 1f);
                float legForward = forwardAmount;
                float jumpLeg = (runCycle < 0.5f ? 1f : -1f) * legForward;
                _animator.SetFloat(GkcAnimatorConstants.JumpLeg, jumpLeg);
            }
        }

        private void UpdateUnarmedLocomotion(
            PlayerLocomotionAnimationSettings locomotion,
            bool isMoving,
            bool isSprinting,
            bool unarmedCombatEngaged,
            Vector2 moveInput,
            float forwardAmount,
            float turnAmount,
            float forwardSmoothTime,
            float leanSmoothTime,
            float deltaTime)
        {
            float strafeCutoff = locomotion != null ? locomotion.strafeForwardCutoff : 0.12f;
            bool driveStrafe = ShouldDriveOneHandStrafeParams(moveInput, strafeCutoff);

            PlayerGkcLocomotionSnapshot snapshot = ResolveLocomotionSnapshot(
                isMoving,
                unarmedCombatEngaged,
                forwardAmount,
                turnAmount,
                moveInput);

            SetFloat(GkcAnimatorConstants.PlayerModeId, driveStrafe
                ? GkcAnimatorConstants.PlayerModeFreeMovement
                : snapshot.PlayerModeId);
            SetFloat(GkcAnimatorConstants.PlayerStatusId, snapshot.PlayerStatusId);
            SetFloat(GkcAnimatorConstants.IdleId, snapshot.IdleId);
            SetFloat(GkcAnimatorConstants.WeaponId, GkcAnimatorConstants.WeaponIdUnarmed);
            SetBool(GkcAnimatorConstants.CarryingWeapon, false);
            SetInteger(GkcAnimatorConstants.RightArmId, 0);
            SetInteger(GkcAnimatorConstants.LeftArmId, 0);
            SetFloat(GkcAnimatorConstants.MovementId, 0f);

            if (_activeAction != GkcCombatAction.Block)
            {
                SetBool(GkcAnimatorConstants.StrafeModeActive, driveStrafe);
                SetBool(GkcAnimatorConstants.AimingModeActive, false);
                SetFloat(GkcAnimatorConstants.StrafeId, 0f);
            }

            if (driveStrafe)
            {
                ApplyArmedStrafeParams(
                    isMoving,
                    isSprinting,
                    moveInput,
                    forwardSmoothTime,
                    leanSmoothTime,
                    deltaTime,
                    out _,
                    out _);
            }
            else
            {
                SetFloat(GkcAnimatorConstants.Horizontal, 0f);
                SetFloat(GkcAnimatorConstants.Vertical, 0f);
                SetFloat(GkcAnimatorConstants.HorizontalStrafe, 0f);
                SetFloat(GkcAnimatorConstants.VerticalStrafe, 0f);
            }

            _animator.SetFloat(GkcAnimatorConstants.Forward, snapshot.Forward, forwardSmoothTime, deltaTime);
            _animator.SetFloat(GkcAnimatorConstants.Turn, snapshot.Turn, leanSmoothTime, deltaTime);

            bool hasLocomotionInput = isMoving
                || Mathf.Abs(snapshot.Forward) > 0.01f
                || Mathf.Abs(snapshot.Turn) > 0.01f
                || moveInput.sqrMagnitude > 0.01f;
            SetBool(GkcAnimatorConstants.MovementInputActive, hasLocomotionInput);
            SetBool(GkcAnimatorConstants.MovementRelativeToCamera, true);
        }

        private void UpdateArmedLocomotion(
            ItemData equippedItem,
            bool isMoving,
            bool isSprinting,
            Vector2 moveInput,
            float forwardAmount,
            float turnAmount,
            float forwardSmoothTime,
            float leanSmoothTime,
            float deltaTime)
        {
            SetFloat(GkcAnimatorConstants.WeaponId, equippedItem.ResolveGkcWeaponId());
            SetBool(GkcAnimatorConstants.CarryingWeapon, true);
            SetInteger(GkcAnimatorConstants.RightArmId, equippedItem.ResolveGkcRightArmId());
            SetInteger(GkcAnimatorConstants.LeftArmId, equippedItem.ResolveGkcLeftArmId());
            SetFloat(GkcAnimatorConstants.PlayerStatusId, ResolvePlayerStatusId());
            SetFloat(GkcAnimatorConstants.IdleId, GkcAnimatorConstants.IdleIdDefault);
            SetFloat(GkcAnimatorConstants.MovementId, 0f);

            GkcWeaponKind weaponKind = equippedItem.ResolveGkcWeaponKind();
            bool isTwoHanded = weaponKind == GkcWeaponKind.TwoHand;

            PlayerLocomotionAnimationSettings locomotion = _playerController != null
                ? _playerController.LocomotionAnimations
                : null;
            float strafeCutoff = locomotion != null ? locomotion.strafeForwardCutoff : 0.12f;
            bool driveOneHandStrafe = !isTwoHanded
                && ShouldDriveOneHandStrafeParams(moveInput, strafeCutoff);
            bool driveArmedStrafe = ShouldDriveArmedStrafeParams(moveInput, strafeCutoff, isMoving);
            bool driveTwoHandForward = isTwoHanded
                && isMoving
                && moveInput.y > strafeCutoff
                && !driveArmedStrafe;
            bool useTwoHandStrafeContext = isTwoHanded && (driveArmedStrafe || driveTwoHandForward);

            if (isTwoHanded)
            {
                SetBool(GkcAnimatorConstants.StrafeModeActive, useTwoHandStrafeContext);
                SetBool(GkcAnimatorConstants.AimingModeActive, false);
                SetFloat(
                    GkcAnimatorConstants.StrafeId,
                    useTwoHandStrafeContext ? GkcAnimatorConstants.StrafeIdMeleeTwoHand : 0f);
            }
            else if (_activeAction != GkcCombatAction.Block)
            {
                SetBool(GkcAnimatorConstants.StrafeModeActive, driveOneHandStrafe);
                SetBool(GkcAnimatorConstants.AimingModeActive, false);
                SetFloat(
                    GkcAnimatorConstants.StrafeId,
                    driveOneHandStrafe ? GkcAnimatorConstants.StrafeIdMeleeOneHand : 0f);
            }

            if (isTwoHanded)
            {
                UpdateTwoHandArmedLocomotion(
                    isMoving,
                    isSprinting,
                    moveInput,
                    forwardAmount,
                    turnAmount,
                    forwardSmoothTime,
                    leanSmoothTime,
                    strafeCutoff,
                    useTwoHandStrafeContext,
                    driveTwoHandForward,
                    deltaTime);
                return;
            }

            PlayerGkcLocomotionSnapshot snapshot = ResolveLocomotionSnapshot(
                isMoving,
                unarmedCombatEngaged: false,
                forwardAmount,
                turnAmount,
                moveInput);

            SetFloat(GkcAnimatorConstants.PlayerModeId, driveOneHandStrafe
                ? GkcAnimatorConstants.PlayerModeFreeMovement
                : snapshot.PlayerModeId);

            if (driveOneHandStrafe)
            {
                ApplyArmedStrafeParams(
                    isMoving,
                    isSprinting,
                    moveInput,
                    forwardSmoothTime,
                    leanSmoothTime,
                    deltaTime,
                    out float strafeForward,
                    out float strafeTurn);
                forwardAmount = strafeForward;
                turnAmount = strafeTurn;
            }
            else
            {
                SetFloat(GkcAnimatorConstants.Horizontal, 0f);
                SetFloat(GkcAnimatorConstants.Vertical, 0f);
                SetFloat(GkcAnimatorConstants.HorizontalStrafe, 0f);
                SetFloat(GkcAnimatorConstants.VerticalStrafe, 0f);
            }

            _animator.SetFloat(GkcAnimatorConstants.Forward, snapshot.Forward, forwardSmoothTime, deltaTime);
            _animator.SetFloat(GkcAnimatorConstants.Turn, snapshot.Turn, leanSmoothTime, deltaTime);

            bool hasLocomotionInput = isMoving
                || Mathf.Abs(snapshot.Forward) > 0.01f
                || Mathf.Abs(snapshot.Turn) > 0.01f;
            SetBool(GkcAnimatorConstants.MovementInputActive, hasLocomotionInput);
            SetBool(GkcAnimatorConstants.MovementRelativeToCamera, true);
        }

        private void UpdateTwoHandArmedLocomotion(
            bool isMoving,
            bool isSprinting,
            Vector2 moveInput,
            float forwardAmount,
            float turnAmount,
            float forwardSmoothTime,
            float leanSmoothTime,
            float strafeCutoff,
            bool useStrafeContext,
            bool driveForwardLocomotion,
            float deltaTime)
        {
            if (useStrafeContext)
            {
                SetFloat(GkcAnimatorConstants.PlayerModeId, GkcAnimatorConstants.PlayerModeFreeMovement);

                if (ShouldDriveArmedStrafeParams(moveInput, strafeCutoff, isMoving))
                {
                    ApplyArmedStrafeParams(
                        isMoving,
                        isSprinting,
                        moveInput,
                        forwardSmoothTime,
                        leanSmoothTime,
                        deltaTime,
                        out forwardAmount,
                        out turnAmount);
                }
                else if (driveForwardLocomotion)
                {
                    ApplyTwoHandForwardLocomotionParams(
                        isMoving,
                        isSprinting,
                        moveInput,
                        forwardAmount,
                        turnAmount,
                        forwardSmoothTime,
                        leanSmoothTime,
                        deltaTime);
                }
                else
                {
                    SetFloat(GkcAnimatorConstants.Horizontal, 0f);
                    SetFloat(GkcAnimatorConstants.Vertical, 0f);
                    SetFloat(GkcAnimatorConstants.HorizontalStrafe, 0f);
                    SetFloat(GkcAnimatorConstants.VerticalStrafe, 0f);
                    _animator.SetFloat(GkcAnimatorConstants.Forward, forwardAmount, forwardSmoothTime, deltaTime);
                    _animator.SetFloat(GkcAnimatorConstants.Turn, turnAmount, leanSmoothTime, deltaTime);
                }
            }
            else
            {
                SetFloat(GkcAnimatorConstants.PlayerModeId, GkcAnimatorConstants.PlayerModeTankMovement);
                SetFloat(GkcAnimatorConstants.Horizontal, 0f);
                SetFloat(GkcAnimatorConstants.Vertical, 0f);
                SetFloat(GkcAnimatorConstants.HorizontalStrafe, 0f);
                SetFloat(GkcAnimatorConstants.VerticalStrafe, 0f);
                _animator.SetFloat(GkcAnimatorConstants.Forward, forwardAmount, forwardSmoothTime, deltaTime);
                _animator.SetFloat(GkcAnimatorConstants.Turn, turnAmount, leanSmoothTime, deltaTime);
            }

            bool hasLocomotionInput = isMoving || moveInput.sqrMagnitude > 0.01f;
            SetBool(GkcAnimatorConstants.MovementInputActive, hasLocomotionInput);
            SetBool(GkcAnimatorConstants.MovementRelativeToCamera, true);
        }

        private void ApplyTwoHandForwardLocomotionParams(
            bool isMoving,
            bool isSprinting,
            Vector2 moveInput,
            float forwardAmount,
            float turnAmount,
            float forwardSmoothTime,
            float leanSmoothTime,
            float deltaTime)
        {
            ResolveTwoHandStrafeBlend(isMoving, isSprinting, moveInput, out _, out float vertical);
            float forwardBlend = Mathf.Max(0f, vertical);

            _animator.SetFloat(GkcAnimatorConstants.Horizontal, 0f, forwardSmoothTime, deltaTime);
            _animator.SetFloat(GkcAnimatorConstants.Vertical, forwardBlend, forwardSmoothTime, deltaTime);
            _animator.SetFloat(GkcAnimatorConstants.HorizontalStrafe, 0f, leanSmoothTime, deltaTime);
            _animator.SetFloat(GkcAnimatorConstants.VerticalStrafe, forwardBlend, leanSmoothTime, deltaTime);
            _animator.SetFloat(GkcAnimatorConstants.Forward, forwardAmount, forwardSmoothTime, deltaTime);
            _animator.SetFloat(GkcAnimatorConstants.Turn, turnAmount, leanSmoothTime, deltaTime);
        }

        private void ApplyArmedStrafeParams(
            bool isMoving,
            bool isSprinting,
            Vector2 moveInput,
            float forwardSmoothTime,
            float leanSmoothTime,
            float deltaTime,
            out float forwardAmount,
            out float turnAmount)
        {
            ResolveTwoHandStrafeBlend(isMoving, isSprinting, moveInput, out float horizontal, out float vertical);
            _animator.SetFloat(GkcAnimatorConstants.Horizontal, horizontal, forwardSmoothTime, deltaTime);
            _animator.SetFloat(GkcAnimatorConstants.Vertical, vertical, forwardSmoothTime, deltaTime);
            _animator.SetFloat(GkcAnimatorConstants.HorizontalStrafe, horizontal, leanSmoothTime, deltaTime);
            _animator.SetFloat(GkcAnimatorConstants.VerticalStrafe, vertical, leanSmoothTime, deltaTime);
            forwardAmount = 0f;
            turnAmount = 0f;
            _animator.SetFloat(GkcAnimatorConstants.Forward, 0f, forwardSmoothTime, deltaTime);
            _animator.SetFloat(GkcAnimatorConstants.Turn, 0f, leanSmoothTime, deltaTime);
        }

        private void ResolveTwoHandStrafeBlend(
            bool isMoving,
            bool isSprinting,
            Vector2 moveInput,
            out float horizontal,
            out float vertical)
        {
            horizontal = 0f;
            vertical = 0f;

            if (moveInput.sqrMagnitude < 0.0001f && !isMoving)
                return;

            Vector2 input = moveInput.sqrMagnitude > 0.0001f
                ? Vector2.ClampMagnitude(moveInput, 1f)
                : Vector2.zero;

            if (input.sqrMagnitude < 0.0001f && _playerController != null && isMoving)
            {
                Vector3 worldMove = _playerController.GetCameraRelativeMoveDirection();
                if (worldMove.sqrMagnitude > 0.0001f)
                {
                    worldMove.Normalize();
                    Transform body = _character.transform;
                    input = Vector2.ClampMagnitude(
                        new Vector2(Vector3.Dot(worldMove, body.right), Vector3.Dot(worldMove, body.forward)),
                        1f);
                }
            }

            float walkRunScale = isSprinting ? 1f : 0.5f;
            horizontal = input.x * walkRunScale;
            vertical = input.y * walkRunScale;
        }

        private void EnsureGkcDefaultLayerWeights()
        {
            if (_animator == null)
                return;

            _animator.SetLayerWeight(0, 1f);
            EnsureLayerWeight(GkcAnimatorConstants.ArmsLayer, 0f);
            EnsureLayerWeight(GkcAnimatorConstants.UpperBodyLayer, 1f);
            EnsureLayerWeight(GkcAnimatorConstants.UpperBodyCombatLayer, 1f);
        }

        private void ApplyMeleeUpperBodyLayerSuppression()
        {
            EnsureLayerWeight(GkcAnimatorConstants.UpperBodyLayer, 0f);
            EnsureLayerWeight(GkcAnimatorConstants.UpperBodyCombatLayer, 0f);
        }

        private void MaintainActionCombatContext()
        {
            if (_actionRequiredActionActive)
                SetBool(GkcAnimatorConstants.ActionActive, true);

            if (_actionUsedUpperBodyActive)
                SetBool(GkcAnimatorConstants.ActionActiveUpperBody, true);

            if (_actionRequiredStrafeMode)
                SetBool(GkcAnimatorConstants.StrafeModeActive, true);

            ItemData equippedItem = ResolveActiveMeleeItem();
            if (equippedItem != null)
            {
                SetFloat(GkcAnimatorConstants.WeaponId, equippedItem.ResolveGkcWeaponId());
                SetBool(GkcAnimatorConstants.CarryingWeapon, true);
                SetInteger(GkcAnimatorConstants.RightArmId, equippedItem.ResolveGkcRightArmId());
                SetInteger(GkcAnimatorConstants.LeftArmId, equippedItem.ResolveGkcLeftArmId());

                if (IsMeleeActionActive())
                    SetBool(GkcAnimatorConstants.AimingModeActive, false);

                return;
            }

            MaintainUnarmedCombatContext(forAttack: true);
        }

        private ItemData ResolveActiveMeleeItem()
        {
            if (_equipment != null && _equipment.HasActiveMeleeWeapon())
                return _equipment.SelectedHotbarItem;

            if (_companionEquipment == null || _companionEquipment.EquippedWeapon == null)
                return null;

            bool engaged = CompanionCombatCoordinator.Instance != null
                && CompanionCombatCoordinator.Instance.IsCombatEngaged;
            return engaged ? _companionEquipment.EquippedWeapon : null;
        }

        private void MaintainBlockHoldContext()
        {
            PlayerLocomotionAnimationSettings locomotion = _playerController != null
                ? _playerController.LocomotionAnimations
                : null;
            float strafeCutoff = locomotion != null ? locomotion.strafeForwardCutoff : 0.12f;
            Vector2 moveInput = _playerController != null ? _playerController.MoveInput : Vector2.zero;
            if (moveInput.sqrMagnitude > 1f)
                moveInput.Normalize();

            bool isMoving = _character != null
                && _character.GetSpeed() > GkcAnimatorConstants.MovingSpeedThreshold;
            bool blockStrafe = ShouldDriveOneHandStrafeParams(moveInput, strafeCutoff);
            ItemData blockItem = ResolveActiveMeleeItem();
            if (blockItem != null && blockItem.IsTwoHanded)
                blockStrafe = ShouldDriveArmedStrafeParams(moveInput, strafeCutoff, isMoving);

            SetBool(GkcAnimatorConstants.StrafeModeActive, blockStrafe);
            SetBool(GkcAnimatorConstants.AimingModeActive, false);
            SetFloat(
                GkcAnimatorConstants.StrafeId,
                blockStrafe
                    ? blockItem != null && blockItem.IsTwoHanded
                        ? GkcAnimatorConstants.StrafeIdMeleeTwoHand
                        : GkcAnimatorConstants.StrafeIdMeleeOneHand
                    : 0f);

            SetBool(GkcAnimatorConstants.ActionActive, true);
            SetBool(GkcAnimatorConstants.ActionActiveUpperBody, true);
            SetFloat(GkcAnimatorConstants.ShieldActive, 1f);

            if (_activeEntry != null && _activeEntry.actionId > 0)
                SetInteger(GkcAnimatorConstants.ActionId, _activeEntry.actionId);

            if (blockItem != null)
            {
                SetFloat(GkcAnimatorConstants.WeaponId, blockItem.ResolveGkcWeaponId());
                SetBool(GkcAnimatorConstants.CarryingWeapon, true);
                SetInteger(GkcAnimatorConstants.RightArmId, blockItem.ResolveGkcRightArmId());
                SetInteger(GkcAnimatorConstants.LeftArmId, blockItem.ResolveGkcLeftArmId());
            }
        }

        private void UpdateCompanionLocomotion()
        {
            if (_companionFollow == null)
                return;

            bool blockActive = IsBlockActive();
            bool meleeActionActive = IsMeleeActionActive();

            if (blockActive)
            {
                EnsureGkcDefaultLayerWeights();
                MaintainBlockHoldContext();
            }
            else if (meleeActionActive)
            {
                ApplyMeleeUpperBodyLayerSuppression();
                MaintainActionCombatContext();
            }

            if (IsActionBlockingLocomotion)
            {
                ApplyMinimalCombatLocomotionHold();
                SetBool(GkcAnimatorConstants.Ground, true);
                SetBool(GkcAnimatorConstants.Crouch, false);
                if (!meleeActionActive && !blockActive)
                    EnsureGkcDefaultLayerWeights();
                return;
            }

            bool freezeLocomotion = ShouldFreezeLocomotionForMeleeAction() || IsBaseLayerAttackPlaying();
            if (!meleeActionActive)
                EnsureGkcDefaultLayerWeights();

            CacheAnimatorCapabilities();

            float deltaTime = Time.deltaTime;
            float forwardSmoothTime = 0.18f;
            float leanSmoothTime = 0.24f;
            float animSpeedSmoothTime = 0.12f;

            float speed = _companionFollow.CurrentSpeed;
            bool isMoving = speed > 0.05f;
            bool isSprinting = speed > 6f;
            ItemData equippedItem = ResolveActiveMeleeItem();
            bool useArmedLocomotion = equippedItem != null;
            Vector2 moveInput = ResolveCompanionMoveInput(isMoving);

            ResolveCompanionLocomotionBlend(isMoving, isSprinting, moveInput, out float forwardAmount, out float turnAmount, out _);

            if (_movementSpeedAvailable && !freezeLocomotion)
            {
                float gkcMovementSpeed = ResolveGkcMovementSpeed(isMoving, isSprinting, useArmedLocomotion);
                _animator.SetFloat(GkcAnimatorConstants.MovementSpeed, gkcMovementSpeed, animSpeedSmoothTime, deltaTime);
            }

            if (_movingBoolAvailable && !freezeLocomotion)
                SetBool(GkcAnimatorConstants.Moving, isMoving);

            bool unarmedCombatEngaged = equippedItem == null && IsCompanionCombatEngaged();

            if (!freezeLocomotion)
            {
                if (useArmedLocomotion)
                {
                    UpdateArmedLocomotion(
                        equippedItem,
                        isMoving,
                        isSprinting,
                        moveInput,
                        forwardAmount,
                        turnAmount,
                        forwardSmoothTime,
                        leanSmoothTime,
                        deltaTime);
                }
                else
                {
                    UpdateUnarmedLocomotion(
                        null,
                        isMoving,
                        isSprinting,
                        unarmedCombatEngaged,
                        moveInput,
                        forwardAmount,
                        turnAmount,
                        forwardSmoothTime,
                        leanSmoothTime,
                        deltaTime);
                }
            }

            SetBool(GkcAnimatorConstants.Ground, true);
            SetBool(GkcAnimatorConstants.Crouch, false);
        }

        private Vector2 ResolveCompanionMoveInput(bool isMoving)
        {
            Vector3 worldMove = _companionFollow.CurrentMoveDirection;
            worldMove.y = 0f;

            if (worldMove.sqrMagnitude < 0.0001f && isMoving)
            {
                Transform body = transform.parent != null ? transform.parent : transform;
                Vector3 delta = body.position - _companionLastSamplePosition;
                _companionLastSamplePosition = body.position;
                delta.y = 0f;
                if (delta.sqrMagnitude > 0.0001f)
                    worldMove = delta.normalized;
            }

            if (worldMove.sqrMagnitude < 0.0001f)
                return Vector2.zero;

            worldMove.Normalize();
            Transform reference = transform.parent != null ? transform.parent : transform;
            return Vector2.ClampMagnitude(
                new Vector2(Vector3.Dot(worldMove, reference.right), Vector3.Dot(worldMove, reference.forward)),
                1f);
        }

        private Vector3 _companionLastSamplePosition;

        private void ResolveCompanionLocomotionBlend(
            bool isMoving,
            bool isSprinting,
            Vector2 moveInput,
            out float forwardAmount,
            out float turnAmount,
            out bool driveStrafeTurnAxis)
        {
            forwardAmount = 0f;
            turnAmount = 0f;
            driveStrafeTurnAxis = false;

            if (!isMoving || moveInput.sqrMagnitude < 0.0001f)
                return;

            float walkRunForward = isSprinting ? 1f : 0.5f;
            float localForward = moveInput.y;
            float localTurn = moveInput.x;

            if (localForward > 0.12f)
            {
                forwardAmount = Mathf.Clamp01(localForward) * walkRunForward;
                return;
            }

            if (Mathf.Abs(localTurn) > 0.08f && localForward <= 0.12f)
            {
                turnAmount = Mathf.Sign(localTurn) * Mathf.Clamp01(Mathf.Abs(localTurn)) * walkRunForward;
                driveStrafeTurnAxis = true;
            }
        }

        private static bool IsCompanionCombatEngaged()
        {
            return CompanionCombatCoordinator.Instance != null
                && CompanionCombatCoordinator.Instance.IsCombatEngaged;
        }

        private static bool ShouldDriveArmedStrafeParams(Vector2 moveInput, float strafeCutoff, bool isMoving) =>
            ShouldDriveOneHandStrafeParams(moveInput, strafeCutoff);

        private static bool ShouldDriveOneHandStrafeParams(Vector2 moveInput, float strafeCutoff)
        {
            if (moveInput.sqrMagnitude < 0.0001f)
                return false;

            Vector2 input = Vector2.ClampMagnitude(moveInput, 1f);
            bool hasLateral = Mathf.Abs(input.x) > 0.08f;
            bool pureStrafe = hasLateral && input.y <= strafeCutoff;
            bool backward = input.y < -strafeCutoff;
            return pureStrafe || backward;
        }

        private void ExtendActionEndTimeIfNeeded()
        {
            float minEnd = Time.time + ActionTimeoutExtensionSeconds;
            if (_actionEndTime < minEnd)
                _actionEndTime = minEnd;
        }

        private bool TryGetActionLayerStateInfo(out AnimatorStateInfo stateInfo)
        {
            stateInfo = default;
            if (_animator == null || _activeEntry == null)
                return false;

            int layer = ResolveLayer(_activeEntry.layerName);
            if (layer < 0)
                layer = 0;

            if (_animator.IsInTransition(layer))
            {
                AnimatorStateInfo nextState = _animator.GetNextAnimatorStateInfo(layer);
                if (IsAttackStateInfo(nextState, layer))
                {
                    stateInfo = nextState;
                    return true;
                }
            }

            AnimatorStateInfo currentState = _animator.GetCurrentAnimatorStateInfo(layer);
            if (!IsAttackStateInfo(currentState, layer))
                return false;

            stateInfo = currentState;
            return true;
        }

        private bool IsAttackStateInfo(AnimatorStateInfo stateInfo, int layer)
        {
            if (_activeEntry != null && !string.IsNullOrWhiteSpace(_activeEntry.stateName))
            {
                if (stateInfo.IsName(_activeEntry.stateName))
                    return true;

                string shortName = _activeEntry.stateName;
                int lastDot = shortName.LastIndexOf('.');
                if (lastDot >= 0)
                    shortName = shortName[(lastDot + 1)..];

                return stateInfo.IsName(shortName);
            }

            return layer == 0 && stateInfo.IsTag("Attack");
        }

        private static float ResolveGkcMovementSpeed(bool isMoving, bool isSprinting, bool armedMelee)
        {
            if (!isMoving)
                return armedMelee ? 1f : 0f;

            return isSprinting ? 2f : 1f;
        }

        private PlayerGkcLocomotionSnapshot ResolveLocomotionSnapshot(
            bool isMoving,
            bool unarmedCombatEngaged,
            float forwardAmount,
            float turnAmount,
            Vector2 moveInput)
        {
            bool useCloseCombatIdle = unarmedCombatEngaged && !isMoving;
            bool useTankIdle = !isMoving && !useCloseCombatIdle;

            float playerModeId = useTankIdle
                ? GkcAnimatorConstants.PlayerModeTankMovement
                : GkcAnimatorConstants.PlayerModeFreeMovement;

            float idleId = useCloseCombatIdle
                ? GkcAnimatorConstants.IdleIdCloseCombat
                : GkcAnimatorConstants.IdleIdDefault;

            float forward = forwardAmount;
            float turn = turnAmount;

            if (useTankIdle)
            {
                forward = 0f;
                if (Mathf.Abs(moveInput.x) > 0.08f)
                    turn = Mathf.Sign(moveInput.x) * Mathf.Clamp01(Mathf.Abs(moveInput.x));
                else if (Mathf.Abs(turnAmount) > 0.01f)
                    turn = turnAmount;
                else
                    turn = 0f;
            }
            else if (isMoving && Mathf.Abs(forward) < 0.01f && Mathf.Abs(turn) < 0.01f && moveInput.sqrMagnitude > 0.01f)
            {
                forward = moveInput.y;
                turn = moveInput.x;
            }

            return new PlayerGkcLocomotionSnapshot
            {
                PlayerModeId = playerModeId,
                PlayerStatusId = ResolvePlayerStatusId(),
                IdleId = idleId,
                Forward = forward,
                Turn = turn
            };
        }

        private float ResolvePlayerStatusId()
        {
            if (_survivalStats == null || _survivalStats.maxHealth <= 0f)
                return GkcAnimatorConstants.PlayerStatusNormal;

            float healthRatio = _survivalStats.CurrentHealth / _survivalStats.maxHealth;
            if (healthRatio <= 0.15f)
                return GkcAnimatorConstants.PlayerStatusInjuredLegs;
            if (healthRatio <= 0.4f)
                return GkcAnimatorConstants.PlayerStatusInjured;
            return GkcAnimatorConstants.PlayerStatusNormal;
        }

        private bool IsUnarmedCombatEngaged()
        {
            if (_companionFollow != null && _character == null)
                return IsCompanionCombatEngaged();

            Transform player = _character != null ? _character.transform : transform;
            if (player == null)
                return false;

            if (Time.time >= _nextEngagementScanTime)
            {
                _cachedUnarmedEngaged = ScanForEngagement(player, 14f);
                _nextEngagementScanTime = Time.time + 0.2f;
            }

            return _cachedUnarmedEngaged;
        }

        private static bool ScanForEngagement(Transform player, float engageRange)
        {
            float rangeSqr = engageRange * engageRange;
            Vector3 playerPosition = player.position;
            EnemyAiController[] enemies = Object.FindObjectsByType<EnemyAiController>();
            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyAiController enemy = enemies[i];
                if (enemy == null || !enemy.isActiveAndEnabled || !enemy.IsEngagedWithTarget)
                    continue;

                if ((enemy.transform.position - playerPosition).sqrMagnitude <= rangeSqr)
                    return true;
            }

            return false;
        }

        private void ApplyCombatContext(GkcWeaponKind weaponKind, GkcActionCatalogEntry entry)
        {
            ItemData equippedItem = ResolveActiveMeleeItem();
            if (weaponKind != GkcWeaponKind.Unarmed && equippedItem != null)
            {
                SetFloat(GkcAnimatorConstants.WeaponId, equippedItem.ResolveGkcWeaponId());
                SetBool(GkcAnimatorConstants.CarryingWeapon, true);
                SetInteger(GkcAnimatorConstants.RightArmId, equippedItem.ResolveGkcRightArmId());
                SetInteger(GkcAnimatorConstants.LeftArmId, equippedItem.ResolveGkcLeftArmId());
                return;
            }

            if (weaponKind == GkcWeaponKind.Unarmed)
                MaintainUnarmedCombatContext(forAttack: IsUnarmedMeleeAction(entry.combatAction));
        }

        private void MaintainUnarmedCombatContext(bool forAttack)
        {
            SetFloat(GkcAnimatorConstants.WeaponId, GkcAnimatorConstants.WeaponIdUnarmed);
            SetBool(GkcAnimatorConstants.CarryingWeapon, false);
            SetInteger(GkcAnimatorConstants.RightArmId, 0);
            SetInteger(GkcAnimatorConstants.LeftArmId, 0);
            SetFloat(GkcAnimatorConstants.PlayerModeId, GkcAnimatorConstants.PlayerModeFreeMovement);

            bool closeCombat = forAttack || IsUnarmedCombatEngaged();
            SetFloat(
                GkcAnimatorConstants.IdleId,
                closeCombat ? GkcAnimatorConstants.IdleIdCloseCombat : GkcAnimatorConstants.IdleIdDefault);
        }

        private static bool IsUnarmedMeleeAction(GkcCombatAction action) =>
            action is GkcCombatAction.Punch1
                or GkcCombatAction.Punch2
                or GkcCombatAction.Punch3
                or GkcCombatAction.Punch4
                or GkcCombatAction.Punch5;

        private static bool IsChargeAction(GkcCombatAction action) =>
            action is GkcCombatAction.Charge1H
                or GkcCombatAction.Charge2H
                or GkcCombatAction.ChargeAxe;

        private static bool IsPowerAttackAction(GkcCombatAction action) =>
            action is GkcCombatAction.Sword1HPower
                or GkcCombatAction.Axe1HPower
                or GkcCombatAction.Sword2HPower
                or GkcCombatAction.Punch5;

        private void PlayCrossFade(GkcActionCatalogEntry entry)
        {
            int layer = ResolveLayer(entry.layerName);
            if (layer < 0)
                layer = 0;

            if (TryCrossFadeState(entry.stateName, entry.crossFadeDuration, layer))
                return;

            int lastDot = entry.stateName.LastIndexOf('.');
            if (lastDot >= 0
                && TryCrossFadeState(entry.stateName[(lastDot + 1)..], entry.crossFadeDuration, layer))
            {
                return;
            }

            Debug.LogWarning(
                $"[PlayerGkcAnimatorDriver] Animator state '{entry.stateName}' was not found on layer '{entry.layerName}'.",
                this);
        }

        private bool TryCrossFadeState(string stateName, float crossFadeDuration, int layer)
        {
            if (_animator == null || string.IsNullOrWhiteSpace(stateName))
                return false;

            int stateHash = Animator.StringToHash(stateName);
            if (!_animator.HasState(layer, stateHash))
                return false;

            _animator.CrossFadeInFixedTime(stateHash, crossFadeDuration, layer);
            return true;
        }

        private bool TryCompleteActiveActionByAnimatorState()
        {
            if (_activeEntry == null)
                return false;

            if (_activeAction == GkcCombatAction.Block)
                return false;

            int layer = ResolveLayer(_activeEntry.layerName);
            if (layer < 0)
                layer = 0;

            if (_animator.IsInTransition(layer))
            {
                AnimatorStateInfo nextState = _animator.GetNextAnimatorStateInfo(layer);
                AnimatorStateInfo currentState = _animator.GetCurrentAnimatorStateInfo(layer);
                bool inNextAttack = IsAttackStateInfo(nextState, layer);
                bool inCurrentAttack = IsAttackStateInfo(currentState, layer);

                if (inNextAttack)
                {
                    ExtendActionEndTimeIfNeeded();
                    if (nextState.normalizedTime >= 0.95f && !nextState.loop)
                    {
                        EndActiveAction();
                        return true;
                    }

                    return true;
                }

                if (inCurrentAttack)
                {
                    ExtendActionEndTimeIfNeeded();
                    if (currentState.normalizedTime >= 0.95f && !currentState.loop)
                    {
                        EndActiveAction();
                        return true;
                    }

                    return true;
                }

                ExtendActionEndTimeIfNeeded();
                return true;
            }

            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(layer);
            if (!IsAttackStateInfo(stateInfo, layer))
                return false;

            ExtendActionEndTimeIfNeeded();
            if (stateInfo.normalizedTime >= 0.95f && !stateInfo.loop)
            {
                EndActiveAction();
                return true;
            }

            return true;
        }

        private void ApplyMinimalEnvironmentState()
        {
            if (_character == null)
                return;

            SetBool(GkcAnimatorConstants.Ground, _character.IsGrounded());
            SetBool(GkcAnimatorConstants.Crouch, _character.IsCrouched());

            if (_character.IsFalling())
                _animator.SetFloat(GkcAnimatorConstants.Jump, _character.GetVelocity().y, 0.1f, Time.deltaTime);
        }

        private void ApplyMinimalCombatLocomotionHold()
        {
            SetFloat(GkcAnimatorConstants.Forward, 0f);
            SetFloat(GkcAnimatorConstants.Turn, 0f);
            SetFloat(GkcAnimatorConstants.Horizontal, 0f);
            SetFloat(GkcAnimatorConstants.Vertical, 0f);
            SetFloat(GkcAnimatorConstants.HorizontalStrafe, 0f);
            SetFloat(GkcAnimatorConstants.VerticalStrafe, 0f);
            SetBool(GkcAnimatorConstants.Moving, false);
            SetBool(GkcAnimatorConstants.MovementInputActive, false);
        }

        private GkcWeaponKind ResolveCurrentWeaponKind()
        {
            ItemData item = ResolveActiveMeleeItem();
            return item != null ? item.ResolveGkcWeaponKind() : GkcWeaponKind.Unarmed;
        }

        private void ResolveLocomotionBlend(
            PlayerLocomotionAnimationSettings locomotion,
            bool isMoving,
            bool isSprinting,
            out float forwardAmount,
            out float turnAmount,
            out bool driveStrafeTurnAxis)
        {
            forwardAmount = 0f;
            turnAmount = 0f;
            driveStrafeTurnAxis = false;

            if (!isMoving || _playerController == null)
            {
                _prevBodyYawInitialized = false;
                return;
            }

            Vector3 worldMove = _playerController.GetCameraRelativeMoveDirection();
            if (worldMove.sqrMagnitude < 0.0001f)
            {
                _prevBodyYawInitialized = false;
                return;
            }

            worldMove.Normalize();
            Vector2 moveInput = _playerController.MoveInput;
            if (moveInput.sqrMagnitude > 1f)
                moveInput.Normalize();

            float strafeCutoff = locomotion != null ? locomotion.strafeForwardCutoff : 0.12f;
            float walkRunForward = locomotion != null
                ? locomotion.ResolveBlendForward(isSprinting, 1f)
                : isSprinting ? 1f : 0.5f;

            if (moveInput.y > strafeCutoff)
            {
                float forwardInput = Mathf.Clamp01(moveInput.y);
                forwardAmount = forwardInput * walkRunForward;

                float turnMinForward = locomotion != null ? locomotion.forwardTurnMinForward : 0.15f;
                if (forwardAmount > turnMinForward)
                {
                    float bodyYaw = _character.transform.eulerAngles.y;
                    if (!_prevBodyYawInitialized)
                    {
                        _prevBodyYaw = bodyYaw;
                        _prevBodyYawInitialized = true;
                    }
                    float bodyYawDelta = Mathf.DeltaAngle(_prevBodyYaw, bodyYaw);
                    _prevBodyYaw = bodyYaw;

                    float bodyYawRate = bodyYawDelta / Mathf.Max(0.0001f, Time.deltaTime);
                    float turnRateDegrees = locomotion != null ? locomotion.forwardTurnRateDegrees : 75f;
                    float turnYawMix = locomotion != null ? locomotion.forwardTurnYawMix : 1f;
                    float lookTurnScale = locomotion != null ? locomotion.forwardLookTurnScale : 0.38f;
                    float leanCap = isSprinting 
                        ? (locomotion != null ? locomotion.forwardLeanTurnCap : 0.92f)
                        : (locomotion != null ? locomotion.forwardWalkLeanTurnCap : 1.05f);

                    float bodyLean = (bodyYawRate / turnRateDegrees) * turnYawMix;
                    float lookLean = 0f;
                    if (_playerController != null)
                    {
                        float lookYawRate = _playerController.LastLookYawDelta / Mathf.Max(0.0001f, Time.deltaTime);
                        lookLean = (lookYawRate / turnRateDegrees) * lookTurnScale;
                    }

                    float targetLean = Mathf.Clamp(bodyLean + lookLean, -leanCap, leanCap);
                    turnAmount = targetLean;

                    float forwardReduction = locomotion != null ? locomotion.forwardLeanForwardReduction : 0.18f;
                    forwardAmount = Mathf.Max(0.1f, forwardAmount - Mathf.Abs(turnAmount) * forwardReduction);
                }
                else
                {
                    _prevBodyYawInitialized = false;
                }
                return;
            }

            float strafeScale = locomotion != null ? locomotion.strafeBlendScale : 1f;
            float lateral = moveInput.x;
            float forward = moveInput.y;
            bool hasLateralInput = Mathf.Abs(lateral) > 0.08f;
            bool hasBackwardInput = forward < -strafeCutoff;

            if (hasLateralInput && forward <= strafeCutoff)
            {
                _prevBodyYawInitialized = false;
                turnAmount = Mathf.Sign(lateral) * Mathf.Clamp01(Mathf.Abs(lateral)) * walkRunForward * strafeScale;
                driveStrafeTurnAxis = Mathf.Abs(turnAmount) > 0.01f;
                return;
            }

            if (!hasBackwardInput)
            {
                _prevBodyYawInitialized = false;
                return;
            }

            _prevBodyYawInitialized = false;
            Transform body = _character.transform;
            float localForward = Vector3.Dot(worldMove, body.forward);
            float localTurn = Vector3.Dot(worldMove, body.right);
            Vector2 localMove = Vector2.ClampMagnitude(new Vector2(localTurn, localForward), 1f);
            localMove.x *= strafeScale;
            float moveIntensity = localMove.magnitude;
            if (moveIntensity < 0.01f)
                return;

            float backwardBlendForward = locomotion != null
                ? locomotion.ResolveBlendForward(isSprinting, moveIntensity)
                : isSprinting ? 1f : 0.5f;
            forwardAmount = localMove.y * backwardBlendForward;
            turnAmount = localMove.x * backwardBlendForward;
            driveStrafeTurnAxis = Mathf.Abs(turnAmount) > 0.01f;
        }

        private void CacheAnimatorCapabilities()
        {
            if (_animator == null || _animator.runtimeAnimatorController == null)
                return;

            if (_capabilitiesCached)
                return;

            _movementSpeedAvailable = HasFloat(GkcAnimatorConstants.MovementSpeed);
            _movingBoolAvailable = HasBool(GkcAnimatorConstants.Moving);
            _capabilitiesCached = true;
        }

        private void EnsureLayerWeight(string layerName, float weight)
        {
            int layer = ResolveLayer(layerName);
            if (layer >= 0)
                _animator.SetLayerWeight(layer, weight);
        }

        private int ResolveLayer(string layerName)
        {
            if (_animator == null || string.IsNullOrWhiteSpace(layerName))
                return -1;

            for (int i = 0; i < _animator.layerCount; i++)
            {
                if (_animator.GetLayerName(i) == layerName)
                    return i;
            }

            return -1;
        }

        private bool HasBool(int hash) => HasParameter(hash, AnimatorControllerParameterType.Bool);
        private bool HasFloat(int hash) => HasParameter(hash, AnimatorControllerParameterType.Float);
        private bool HasInteger(int hash) => HasParameter(hash, AnimatorControllerParameterType.Int);

        private bool HasParameter(int hash, AnimatorControllerParameterType type)
        {
            if (_animator == null)
                return false;

            AnimatorControllerParameter[] parameters = _animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                AnimatorControllerParameter parameter = parameters[i];
                if (parameter.type == type && parameter.nameHash == hash)
                    return true;
            }

            return false;
        }

        private void SetFloat(int hash, float value) { if (HasFloat(hash)) _animator.SetFloat(hash, value); }
        private void SetBool(int hash, bool value) { if (HasBool(hash)) _animator.SetBool(hash, value); }
        private void SetInteger(int hash, int value) { if (HasInteger(hash)) _animator.SetInteger(hash, value); }
    }

    public struct PlayerGkcLocomotionSnapshot
    {
        public float PlayerModeId;
        public float PlayerStatusId;
        public float IdleId;
        public float Forward;
        public float Turn;
    }
}
