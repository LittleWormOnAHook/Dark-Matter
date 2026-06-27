using Project.AI;
using Project.Combat;
using Project.Audio;
using Project.Core;
using Project.Data;
using Project.Inventory;
using Project.Player;
using Project.Survival;
using Project.UI;
using ECM2;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Project.Interaction
{
    [RequireComponent(typeof(EquipmentController))]
    public class MeleeCombatController : MonoBehaviour
    {
        [Header("Hit Detection")]
        [SerializeField] private LayerMask hitLayers = ~0;
        [SerializeField] private float fallbackRange = 2.2f;
        [SerializeField] private float fallbackCooldown = 0.8f;
        [SerializeField] private float attackOriginHeight = 1.25f;
        [SerializeField] private float swingHitWindow = 0.45f;

        [Header("Power Hit")]
        [Tooltip("Hold attack at least this long before release to trigger a critical power hit.")]
        public float powerHitMinHoldTime = 2f;
        [Tooltip("Hold this long before the charge wind-up animation begins. Quick taps stay on normal combo attacks.")]
        [SerializeField] private float powerHitChargeStartDelay = 1.5f;
        [Tooltip("Fallback critical multiplier when the weapon criticalDamageMultiplier is 0.")]
        public float criticalDamageMultiplier = 2f;
        [SerializeField] private string powerHitStateName = "AttackCombo5";
        [SerializeField] private string powerHitChargeStateName = "PowerHitCharge";
        [SerializeField] private int powerHitChargeLayerIndex = 1;
        [SerializeField] private float powerHitChargeBlendTime = 0.2f;
        [SerializeField] private float powerHitCooldownMultiplier = 1.5f;
        [SerializeField] private float upperBodyAttackBlendTime = 0.22f;

        private static readonly int PowerHitCharge = Animator.StringToHash("PowerHitCharge");
        private static readonly int Block = Animator.StringToHash("Block");

        [Header("Block")]
        [SerializeField] private string blockStateName = "Block";
        [SerializeField] private int blockLayerIndex = 1;
        [SerializeField] private float blockBlendTime = 0.18f;

        public bool IsBlocking { get; private set; }
        public bool IsAttackInputActive => attackInputHeld;
        public float LastAttackTime => lastComboAttackTime;

        [Header("Combo")]
        [SerializeField] private float comboResetWindow = 1.35f;
        [SerializeField] private string[] comboStateNames =
        {
            "AttackCombo1",
            "AttackCombo2",
            "AttackCombo3",
            "AttackCombo4",
            "AttackCombo5"
        };

        [Header("Two-Handed Attacks")]
        [SerializeField] private string[] twoHandedAttackStateNames =
        {
            "TwoHandAttack1",
            "TwoHandAttack2",
            "TwoHandAttack3",
            "TwoHandAttack4"
        };
        [SerializeField] private string twoHandedPowerHitStateName = "TwoHandPowerHit";
        [SerializeField] private float twoHandedCooldownMultiplier = 1.35f;

        private EquipmentController equipment;
        private EquippedItemVisual heldVisual;
        private ResourceGatherer resourceGatherer;
        private PlayerController playerController;
        private SurvivalStats survivalStats;
        private UIManager uiManager;
        private Animator animator;
        private float nextAttackTime;
        private int comboIndex;
        private float lastComboAttackTime = float.NegativeInfinity;
        private float attackHoldStartTime;
        private bool attackInputHeld;
        private bool chargePoseStarted;
        private bool combatBlockedByUiPointer;
        private int upperBodyCombatLayerIndex = -1;
        private Character character;

        private void Awake()
        {
            equipment = GetComponent<EquipmentController>();
            heldVisual = GetComponent<EquippedItemVisual>();
            resourceGatherer = GetComponent<ResourceGatherer>();
            playerController = GetComponent<PlayerController>();
            survivalStats = GetComponent<SurvivalStats>();
            uiManager = FindAnyObjectByType<UIManager>();

            Character character = GetComponent<Character>();
            this.character = character;
            animator = character != null ? character.GetAnimator() : null;
            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            upperBodyCombatLayerIndex = PlayerCombatAnimationLayer.ResolveUpperBodyCombatLayer(animator);

            if (animator != null && animator.layerCount > powerHitChargeLayerIndex)
                animator.SetLayerWeight(powerHitChargeLayerIndex, 0f);
        }

        public void OnBlock(InputAction.CallbackContext context)
        {
            if (!Application.isPlaying || !GameSession.HasStarted)
                return;

            if (IsCombatInputBlocked())
                return;

            OpticsController optics = GetComponent<OpticsController>();
            if (optics != null && optics.TryHandleBlockInput(context))
                return;

            if (context.started)
                BeginBlock();
            else if (context.canceled)
                EndBlock();
        }

        public void OnSwitchWeapon(InputAction.CallbackContext context)
        {
            if (!context.performed || equipment == null || !GameSession.HasStarted)
                return;

            if (IsCombatInputBlocked())
                return;

            if (IsBlocking)
                EndBlock();

            equipment.SwitchActiveWeapon();
            heldVisual?.ForceRefresh();
        }

        public void OnAttack(InputAction.CallbackContext context)
        {
            if (!Application.isPlaying || !GameSession.HasStarted)
                return;

            if (IsBlocking)
                return;

            if (IsCombatInputBlocked())
            {
                attackInputHeld = false;
                chargePoseStarted = false;
                return;
            }

            if (context.started)
            {
                attackInputHeld = true;
                chargePoseStarted = false;
                attackHoldStartTime = Time.time;
                return;
            }

            if (!context.canceled || !attackInputHeld)
                return;

            attackInputHeld = false;
            float holdDuration = Time.time - attackHoldStartTime;

            if (holdDuration >= powerHitMinHoldTime)
                TryPowerHit();
            else
            {
                EndPowerHitChargeAnimation();
                heldVisual?.CancelPowerHitCharge();
                TryAttack();
            }

            chargePoseStarted = false;
        }

        private void Update()
        {
            RefreshCombatUiPointerBlock();
            PlayerCombatAnimationLayer.UpdateUpperBodyLayerWeight(animator, upperBodyCombatLayerIndex);

            if (IsBlocking)
                return;

            if (!attackInputHeld || chargePoseStarted || !CanBeginMeleeInput())
                return;

            float chargeDelay = Mathf.Clamp(
                powerHitChargeStartDelay,
                0.05f,
                Mathf.Max(0.1f, powerHitMinHoldTime - 0.05f));

            if (Time.time - attackHoldStartTime < chargeDelay)
                return;

            chargePoseStarted = true;
            if (!BeginPowerHitChargeAnimation())
            {
                ItemData item = equipment != null ? equipment.EquippedItem : null;
                if (item == null || !item.IsTwoHanded)
                    heldVisual?.BeginPowerHitCharge();
            }
        }

        private bool BeginPowerHitChargeAnimation()
        {
            if (animator == null)
                return false;

            if (animator.layerCount <= powerHitChargeLayerIndex)
                return false;

            string stateName = !string.IsNullOrWhiteSpace(powerHitChargeStateName)
                ? powerHitChargeStateName
                : "PowerHitCharge";

            animator.SetLayerWeight(powerHitChargeLayerIndex, 1f);
            animator.SetBool(PowerHitCharge, true);
            animator.CrossFadeInFixedTime(
                stateName,
                powerHitChargeBlendTime,
                powerHitChargeLayerIndex,
                0f);
            return true;
        }

        private void EndPowerHitChargeAnimation()
        {
            if (animator == null)
                return;

            animator.SetBool(PowerHitCharge, false);

            if (animator.layerCount > powerHitChargeLayerIndex && !IsBlocking)
                PlayerCombatAnimationLayer.SetUpperBodyLayerWeight(animator, powerHitChargeLayerIndex, 0f, Time.deltaTime);
        }

        private void BeginBlock()
        {
            if (IsBlocking || !CanBlock())
                return;

            attackInputHeld = false;
            chargePoseStarted = false;
            EndPowerHitChargeAnimation();
            heldVisual?.CancelPowerHitChargeImmediate();

            if (animator == null || animator.layerCount <= blockLayerIndex)
                return;

            IsBlocking = true;
            animator.SetBool(PowerHitCharge, false);
            animator.SetLayerWeight(blockLayerIndex, 1f);
            animator.SetBool(Block, true);
            animator.CrossFadeInFixedTime(blockStateName, blockBlendTime, blockLayerIndex, 0f);
        }

        private void EndBlock()
        {
            if (!IsBlocking)
                return;

            IsBlocking = false;

            if (animator == null)
                return;

            animator.SetBool(Block, false);

            if (animator.layerCount > blockLayerIndex && !animator.GetBool(PowerHitCharge))
                PlayerCombatAnimationLayer.SetUpperBodyLayerWeight(animator, blockLayerIndex, 0f, Time.deltaTime);
        }

        private void RefreshCombatUiPointerBlock()
        {
            combatBlockedByUiPointer = false;

            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
                return;

            if (Mouse.current != null)
            {
                combatBlockedByUiPointer = eventSystem.IsPointerOverGameObject(Mouse.current.deviceId);
                return;
            }

            combatBlockedByUiPointer = eventSystem.IsPointerOverGameObject();
        }

        private bool IsCombatInputBlocked()
        {
            if (playerController != null && playerController.BlocksCombatInput)
                return true;

            return combatBlockedByUiPointer;
        }

        private bool CanBlock()
        {
            if (IsCombatInputBlocked())
                return false;

            if (survivalStats != null && survivalStats.IsDead)
                return false;

            ItemData item = equipment != null ? equipment.EquippedItem : null;
            if (item == null || item.itemType != ItemType.MeleeWeapon)
                return false;

            if (item.IsTwoHanded)
                return false;

            return equipment == null || equipment.IsWeaponDrawn;
        }

        private void OnDisable()
        {
            attackInputHeld = false;
            chargePoseStarted = false;
            EndPowerHitChargeAnimation();
            EndBlock();
            heldVisual?.CancelPowerHitChargeImmediate();
        }

        private bool CanBeginMeleeInput()
        {
            if (IsCombatInputBlocked())
                return false;

            if (survivalStats != null && survivalStats.IsDead)
                return false;

            ItemData item = equipment != null ? equipment.EquippedItem : null;
            if (item == null || item.itemType != ItemType.MeleeWeapon)
                return false;

            return equipment == null || equipment.IsWeaponDrawn;
        }

        private bool TryGetMeleeWeapon(out ItemData item)
        {
            item = null;

            if (IsCombatInputBlocked())
                return false;

            if (survivalStats != null && survivalStats.IsDead)
                return false;

            item = equipment != null ? equipment.EquippedItem : null;
            if (item == null || item.itemType != ItemType.MeleeWeapon)
            {
                ShowTemporaryPrompt("Equip a melee weapon in the hotbar first.");
                return false;
            }

            if (equipment != null && !equipment.IsWeaponDrawn)
            {
                ShowTemporaryPrompt("Press the hotbar key again to draw your weapon.");
                return false;
            }

            return true;
        }

        private void TryAttack()
        {
            if (!TryGetMeleeWeapon(out ItemData item))
                return;

            float cooldown = Mathf.Max(0.05f, item.meleeCooldown > 0 ? item.meleeCooldown : fallbackCooldown);
            if (item.IsTwoHanded)
                cooldown *= twoHandedCooldownMultiplier;

            if (Time.time < nextAttackTime)
            {
                EndPowerHitChargeAnimation();
                heldVisual?.CancelPowerHitCharge();
                return;
            }

            EndPowerHitChargeAnimation();
            nextAttackTime = Time.time + cooldown;
            bool bodyAnimationPlayed = item.IsTwoHanded
                ? PlayRandomTwoHandedAttackAnimation()
                : PlayComboAttackAnimation();
            if (!bodyAnimationPlayed && heldVisual != null)
                heldVisual.PlaySwing(cooldown);

            GameAudioManager.Instance?.PlayWeaponSwing(transform.position + Vector3.up * attackOriginHeight);
            EnemyNoiseEvents.RaiseNoise(transform.position, 6f, gameObject);
            BeginWeaponSwing(item, isCritical: false, cooldown);
        }

        private void TryPowerHit()
        {
            if (!TryGetMeleeWeapon(out ItemData item))
            {
                EndPowerHitChargeAnimation();
                heldVisual?.CancelPowerHitCharge();
                return;
            }

            float baseCooldown = item.meleeCooldown > 0 ? item.meleeCooldown : fallbackCooldown;
            float cooldown = Mathf.Max(0.05f, baseCooldown * powerHitCooldownMultiplier);
            if (item.IsTwoHanded)
                cooldown *= twoHandedCooldownMultiplier;
            if (Time.time < nextAttackTime)
            {
                EndPowerHitChargeAnimation();
                heldVisual?.CancelPowerHitCharge();
                return;
            }

            EndPowerHitChargeAnimation();
            nextAttackTime = Time.time + cooldown;
            if (item.IsTwoHanded)
                PlayTwoHandedPowerHitAnimation();
            else
                PlayPowerHitAnimation();

            if (!item.IsTwoHanded)
                heldVisual?.ReleasePowerHitCharge(cooldown);

            GameAudioManager.Instance?.PlayWeaponSwing(transform.position + Vector3.up * attackOriginHeight);
            EnemyNoiseEvents.RaiseNoise(transform.position, 8f, gameObject);
            BeginWeaponSwing(item, isCritical: true, cooldown);
        }

        private void BeginWeaponSwing(ItemData item, bool isCritical, float cooldown)
        {
            float hitWindow = Mathf.Clamp(swingHitWindow, 0.15f, cooldown);
            WeaponHitbox hitbox = heldVisual != null ? heldVisual.ActiveHandHitbox : null;
            if (hitbox != null)
            {
                hitbox.BeginSwing(this, item, isCritical, hitLayers, hitWindow);
                return;
            }

            PerformFallbackHit(item, isCritical);
        }

        public void ProcessWeaponHit(Collider hitCollider, ItemData item, bool isCritical)
        {
            if (hitCollider == null || item == null)
                return;

            float damage = RollDamage(item, isCritical);
            Vector3 attackOrigin = transform.position + Vector3.up * attackOriginHeight;
            Vector3 hitPoint = ResolveHitPoint(hitCollider, attackOrigin);
            Vector3 hitNormal = hitPoint - attackOrigin;
            if (hitNormal.sqrMagnitude < 0.0001f)
                hitNormal = transform.forward;
            hitNormal.Normalize();

            ResourceNode resourceNode = hitCollider.GetComponentInParent<ResourceNode>();
            if (resourceNode != null)
            {
                int gatherStrength = isCritical ? item.gatherPower * 2 : item.gatherPower;
                resourceNode.Gather(resourceGatherer, Mathf.Max(1, gatherStrength));
                GameAudioManager.Instance?.PlayResourceHit(hitPoint);
                return;
            }

            IDamageable damageable = DamageableUtility.GetDamageable(hitCollider);
            if (damageable == null)
                return;

            damageable.TakeDamage(damage, gameObject, isCritical);
            EnemyNoiseEvents.RaiseNoise(transform.position, 10f, gameObject);
            GameAudioManager.Instance?.PlayWeaponHit(hitPoint, isCritical);
            CombatHitVfx.SpawnBloodSplatter(hitPoint, hitPoint - transform.position, hitNormal, damage);

            if (damageable is not TrainingDummy)
                ShowDamageNumber(damage, hitCollider, isCritical);
        }

        private bool PlayComboAttackAnimation()
        {
            if (animator == null || comboStateNames == null || comboStateNames.Length == 0)
                return false;

            if (Time.time - lastComboAttackTime > comboResetWindow)
                comboIndex = 0;

            string stateName = comboStateNames[Mathf.Clamp(comboIndex, 0, comboStateNames.Length - 1)];
            comboIndex = (comboIndex + 1) % comboStateNames.Length;
            return PlayAttackState(stateName, upperBodyAttackBlendTime);
        }

        private bool PlayRandomTwoHandedAttackAnimation()
        {
            if (animator == null || twoHandedAttackStateNames == null || twoHandedAttackStateNames.Length == 0)
                return false;

            string stateName = twoHandedAttackStateNames[Random.Range(0, twoHandedAttackStateNames.Length)];
            return PlayAttackState(stateName);
        }

        private bool PlayTwoHandedPowerHitAnimation()
        {
            if (string.IsNullOrWhiteSpace(twoHandedPowerHitStateName))
                return PlayRandomTwoHandedAttackAnimation();

            return PlayAttackState(twoHandedPowerHitStateName);
        }

        private bool PlayAttackState(string stateName, float blendTime = 0.1f)
        {
            if (animator == null || string.IsNullOrWhiteSpace(stateName))
                return false;

            bool grounded = character == null || character.IsGrounded();
            PlayerCombatAnimationLayer.EnsureBaseLocomotionState(animator, grounded);

            int stateHash = Animator.StringToHash(stateName);
            int layer = upperBodyCombatLayerIndex >= 0 && animator.HasState(upperBodyCombatLayerIndex, stateHash)
                ? upperBodyCombatLayerIndex
                : 0;

            if (!animator.HasState(layer, stateHash))
                return false;

            if (layer == upperBodyCombatLayerIndex)
                PlayerCombatAnimationLayer.BeginUpperBodyAttack(animator, upperBodyCombatLayerIndex);

            animator.CrossFadeInFixedTime(stateName, blendTime, layer, 0f);
            lastComboAttackTime = Time.time;
            return true;
        }

        private bool PlayPowerHitAnimation()
        {
            if (animator == null || string.IsNullOrWhiteSpace(powerHitStateName))
                return false;

            return PlayAttackState(powerHitStateName, upperBodyAttackBlendTime);
        }

        private void PerformFallbackHit(ItemData item, bool isCritical)
        {
            float range = item.meleeRange > 0 ? item.meleeRange : fallbackRange;
            Vector3 origin = transform.position + Vector3.up * attackOriginHeight;
            Vector3 forward = transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                forward = transform.forward;
            forward.Normalize();

            Collider[] hits = Physics.OverlapSphere(origin + forward * (range * 0.5f), range * 0.45f, hitLayers, QueryTriggerInteraction.Ignore);
            Collider best = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < hits.Length; i++)
            {
                Collider candidate = hits[i];
                if (candidate == null || IsIgnoredCollider(candidate))
                    continue;

                float distance = Vector3.Distance(origin, candidate.bounds.center);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                best = candidate;
            }

            if (best == null)
                return;

            ProcessWeaponHit(best, item, isCritical);
        }

        private static Vector3 ResolveHitPoint(Collider collider, Vector3 fromPosition)
        {
            if (collider == null)
                return fromPosition;

            if (SupportsClosestPoint(collider))
                return collider.ClosestPoint(fromPosition);

            return collider.bounds.center;
        }

        private static bool SupportsClosestPoint(Collider collider)
        {
            if (collider is BoxCollider or SphereCollider or CapsuleCollider)
                return true;

            return collider is MeshCollider meshCollider && meshCollider.convex;
        }

        private bool IsIgnoredCollider(Collider collider)
        {
            if (collider == null)
                return true;

            if (collider.transform.IsChildOf(transform) || collider.gameObject == gameObject)
                return true;

            return collider.CompareTag("Player");
        }

        private float RollDamage(ItemData item, bool isCritical)
        {
            if (isCritical && item.criticalDamageMultiplier <= 0f)
            {
                float baseDamage = item.RollMeleeDamage();
                return baseDamage * Mathf.Max(1f, criticalDamageMultiplier);
            }

            return item.RollMeleeDamage(isCritical);
        }

        private void ShowDamageNumber(float damage, Collider hitCollider, bool isCritical = false)
        {
            if (hitCollider == null)
                return;

            TrainingDummy dummy = hitCollider.GetComponentInParent<TrainingDummy>();
            if (dummy != null)
                return;

            Vector3 spawnPosition = hitCollider.bounds.center + Vector3.up * 0.75f;
            CombatUiSpawner.ShowDamage(damage, spawnPosition, isCritical);
        }

        private void ShowTemporaryPrompt(string message)
        {
            if (uiManager == null)
                return;

            uiManager.ShowInteractionPrompt(message);
            CancelInvoke(nameof(HidePrompt));
            Invoke(nameof(HidePrompt), 1.2f);
        }

        private void HidePrompt()
        {
            if (uiManager != null)
                uiManager.HideInteractionPrompt();
        }
    }
}
