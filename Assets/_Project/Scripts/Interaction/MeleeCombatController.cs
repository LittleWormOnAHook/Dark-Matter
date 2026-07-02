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
    [DefaultExecutionOrder(100)]
    public class MeleeCombatController : MonoBehaviour
    {
        [Header("Hit Detection")]
        [SerializeField] private LayerMask hitLayers = ~0;
        [SerializeField] private float fallbackRange = 2.2f;
        [SerializeField] private float fallbackCooldown = 0.8f;
        [SerializeField] private float attackOriginHeight = 1.25f;
        [SerializeField] private float swingHitWindow = 0.45f;

        [Header("Melee Timing")]
        [SerializeField] private float twoHandedCooldownMultiplier = 1.35f;

        [Header("Unarmed")]
        [SerializeField] private float unarmedFallbackCooldown = 0.65f;
        [SerializeField] private float unarmedFallbackRange = 1.6f;
        [SerializeField] private int unarmedFallbackDamage = 8;

        public bool IsBlocking { get; private set; }
        public bool IsAttackInputActive => attackInputHeld;
        public float LastAttackTime => lastComboAttackTime;

        [SerializeField] private float comboResetWindow = 0.85f;
        [SerializeField] private float comboChainCooldown = 0.28f;
        private const int ComboLength = 4;

        private EquipmentController equipment;
        private EquippedItemVisual heldVisual;
        private ResourceGatherer resourceGatherer;
        private PlayerController playerController;
        private SurvivalStats survivalStats;
        private UIManager uiManager;
        private PlayerGkcAnimatorDriver animatorDriver;
        private Character character;

        private float nextAttackTime;
        private int comboIndex;
        private float lastComboAttackTime = float.NegativeInfinity;
        private bool attackInputHeld;
        private bool combatBlockedByUiPointer;

        private void Awake()
        {
            equipment = GetComponent<EquipmentController>();
            heldVisual = GetComponent<EquippedItemVisual>();
            resourceGatherer = GetComponent<ResourceGatherer>();
            playerController = GetComponent<PlayerController>();
            survivalStats = GetComponent<SurvivalStats>();
            uiManager = FindAnyObjectByType<UIManager>();
            character = GetComponent<Character>();
            ResolveAnimatorDriver();
        }

        private void ResolveAnimatorDriver()
        {
            if (animatorDriver != null)
                return;

            animatorDriver = GetComponentInChildren<PlayerGkcAnimatorDriver>(true);
        }

        public void OnBlock(InputAction.CallbackContext context)
        {
            if (!Application.isPlaying || !GameSession.HasStarted)
                return;

            if (context.canceled)
            {
                EndBlock();
                return;
            }

            if (IsCombatInputBlocked())
                return;

            OpticsController optics = GetComponent<OpticsController>();
            if (optics != null && optics.TryHandleBlockInput(context))
                return;

            if (context.started)
                BeginBlock();
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
                EndBlock();

            if (IsCombatInputBlocked())
            {
                attackInputHeld = false;
                return;
            }

            if (context.started)
            {
                attackInputHeld = true;
                return;
            }

            if (!context.canceled || !attackInputHeld)
                return;

            attackInputHeld = false;
            TryAttack();
        }

        private void Update()
        {
            RefreshCombatUiPointerBlock();
            UpdateBlockRelease();
        }

        private void LateUpdate()
        {
            animatorDriver?.TickActions();
        }

        private void UpdateBlockRelease()
        {
            if (!IsBlocking)
                return;

            if (Mouse.current != null && !Mouse.current.rightButton.isPressed)
            {
                EndBlock();
                return;
            }

            if (!CanBlock())
                EndBlock();
        }

        private void BeginBlock()
        {
            if (IsBlocking || !CanBlock())
                return;

            attackInputHeld = false;

            PlayerLootAnimationController lootAnimation = GetComponentInChildren<PlayerLootAnimationController>();
            lootAnimation?.CancelForCombat();

            ResolveAnimatorDriver();
            if (animatorDriver == null)
                return;

            if (animatorDriver.IsActionBlockingLocomotion)
                animatorDriver.EndActiveAction();

            IsBlocking = true;
            animatorDriver.RequestAction(GkcCombatAction.Block, GkcAnimatorConstants.DefaultBlockActionDuration);
        }

        private void EndBlock()
        {
            if (!IsBlocking)
                return;

            IsBlocking = false;
            animatorDriver?.CancelBlock();
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

            return equipment != null && equipment.HasActiveMeleeWeapon();
        }

        private void OnDisable()
        {
            attackInputHeld = false;
            EndBlock();
        }

        private bool CanBeginMeleeInput()
        {
            if (IsCombatInputBlocked())
                return false;

            if (survivalStats != null && survivalStats.IsDead)
                return false;

            return TryResolveMeleeItem(out _, showPrompts: false);
        }

        private bool TryResolveMeleeItem(out ItemData item, bool showPrompts)
        {
            if (equipment == null)
            {
                item = null;
                return true;
            }

            if (equipment.HasActiveMeleeWeapon())
            {
                item = equipment.SelectedHotbarItem;
                return true;
            }

            item = null;
            return true;
        }

        private bool TryGetMeleeWeapon(out ItemData item)
        {
            item = null;

            if (IsCombatInputBlocked())
                return false;

            if (survivalStats != null && survivalStats.IsDead)
                return false;

            return TryResolveMeleeItem(out item, showPrompts: true);
        }

        private static bool IsUnarmedAttack(ItemData item) => item == null;

        private void TryAttack()
        {
            EndBlock();

            if (!CanBeginMeleeInput() || !TryGetMeleeWeapon(out ItemData item))
                return;

            float fullCooldown = IsUnarmedAttack(item)
                ? unarmedFallbackCooldown
                : Mathf.Max(0.05f, item.meleeCooldown > 0 ? item.meleeCooldown : fallbackCooldown);
            if (!IsUnarmedAttack(item) && item.IsTwoHanded)
                fullCooldown *= twoHandedCooldownMultiplier;

            bool continuingCombo = Time.time - lastComboAttackTime <= comboResetWindow;
            float attackCooldown = continuingCombo ? comboChainCooldown : fullCooldown;

            if (Time.time < nextAttackTime)
                return;

            if (!RequestComboAttack(item, out float swingDuration))
                return;

            nextAttackTime = Time.time + attackCooldown;

            if (heldVisual != null)
                heldVisual.PlaySwing(attackCooldown);

            Vector3 swingOrigin = transform.position + Vector3.up * attackOriginHeight;
            if (IsUnarmedAttack(item))
                GameAudioManager.Instance?.PlayPunchSwing(swingOrigin);
            else
                GameAudioManager.Instance?.PlayWeaponSwing(swingOrigin);
            EnemyNoiseEvents.RaiseNoise(transform.position, 6f, gameObject);
            BeginWeaponSwing(item, isCritical: false, swingDuration);
        }

        private bool RequestComboAttack(ItemData item, out float swingDuration)
        {
            swingDuration = swingHitWindow;
            ResolveAnimatorDriver();
            if (animatorDriver == null)
                return false;

            if (Time.time - lastComboAttackTime > comboResetWindow)
                comboIndex = 0;

            GkcCombatAction action = ResolveComboAction(item, comboIndex);
            float attackSpeed = item != null ? item.ResolveAttackAnimationSpeed() : 0.95f;
            swingDuration = animatorDriver.ResolveActionDuration(action, attackSpeed);
            if (!animatorDriver.RequestAction(action, attackSpeed: attackSpeed))
                return false;

            comboIndex = (comboIndex + 1) % ComboLength;
            lastComboAttackTime = Time.time;
            return true;
        }

        private bool RequestComboAttack(ItemData item)
        {
            return RequestComboAttack(item, out _);
        }

        private static GkcCombatAction ResolveComboAction(ItemData item, int index)
        {
            int slot = Mathf.Clamp(index, 0, ComboLength - 1);
            if (item == null)
            {
                return slot switch
                {
                    0 => GkcCombatAction.Punch1,
                    1 => GkcCombatAction.Punch2,
                    2 => GkcCombatAction.Punch3,
                    _ => GkcCombatAction.Punch4
                };
            }

            return item.ResolveGkcWeaponKind() switch
            {
                GkcWeaponKind.TwoHand => slot switch
                {
                    0 => GkcCombatAction.Sword2HCombo1,
                    1 => GkcCombatAction.Sword2HCombo2,
                    2 => GkcCombatAction.Sword2HCombo3,
                    _ => GkcCombatAction.Sword2HCombo4
                },
                GkcWeaponKind.OneHandAxe => slot switch
                {
                    0 => GkcCombatAction.Axe1HCombo1,
                    1 => GkcCombatAction.Axe1HCombo2,
                    2 => GkcCombatAction.Axe1HCombo3,
                    _ => GkcCombatAction.Axe1HCombo4
                },
                _ => slot switch
                {
                    0 => GkcCombatAction.Sword1HCombo1,
                    1 => GkcCombatAction.Sword1HCombo2,
                    2 => GkcCombatAction.Sword1HCombo3,
                    _ => GkcCombatAction.Sword1HCombo4
                }
            };
        }

        private void BeginWeaponSwing(ItemData item, bool isCritical, float duration)
        {
            float hitWindow = Mathf.Max(swingHitWindow, duration);
            WeaponHitbox hitbox = heldVisual != null ? heldVisual.ActiveHandHitbox : null;
            if (hitbox != null && !IsUnarmedAttack(item))
            {
                hitbox.BeginSwing(this, item, isCritical, hitLayers, hitWindow);
                return;
            }

            PerformFallbackHit(item, isCritical);
        }

        public void ProcessWeaponHit(Collider hitCollider, ItemData item, bool isCritical)
        {
            if (hitCollider == null)
                return;

            if (item == null)
            {
                ProcessUnarmedHit(hitCollider, isCritical);
                return;
            }

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

        private void PerformFallbackHit(ItemData item, bool isCritical)
        {
            float range = item != null && item.meleeRange > 0 ? item.meleeRange : IsUnarmedAttack(item) ? unarmedFallbackRange : fallbackRange;
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

        private void ProcessUnarmedHit(Collider hitCollider, bool isCritical)
        {
            float damage = isCritical ? unarmedFallbackDamage * 2 : unarmedFallbackDamage;
            Vector3 attackOrigin = transform.position + Vector3.up * attackOriginHeight;
            Vector3 hitPoint = ResolveHitPoint(hitCollider, attackOrigin);
            Vector3 hitNormal = hitPoint - attackOrigin;
            if (hitNormal.sqrMagnitude < 0.0001f)
                hitNormal = transform.forward;
            hitNormal.Normalize();

            IDamageable damageable = DamageableUtility.GetDamageable(hitCollider);
            if (damageable == null)
                return;

            damageable.TakeDamage(damage, gameObject, isCritical);
            EnemyNoiseEvents.RaiseNoise(transform.position, 8f, gameObject);
            GameAudioManager.Instance?.PlayPunchHit(hitPoint, isCritical);
            CombatHitVfx.SpawnBloodSplatter(hitPoint, hitPoint - transform.position, hitNormal, damage);

            if (damageable is not TrainingDummy)
                ShowDamageNumber(damage, hitCollider, isCritical);
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
