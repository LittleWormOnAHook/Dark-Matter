using Project.AI;
using Project.Combat;
using Project.Core;
using Project.Data;
using Project.Inventory;
using Project.Player;
using Project.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Project.Interaction
{
    [RequireComponent(typeof(EquipmentController))]
    [RequireComponent(typeof(WeaponAmmoState))]
    [DefaultExecutionOrder(101)]
    public class RangedCombatController : MonoBehaviour
    {
        [SerializeField] private LayerMask aimLayers = ~0;
        [SerializeField] private float adsSpreadMultiplier = 0.35f;

        public bool IsAiming { get; private set; }
        public bool WantsAimInputHeld { get; private set; }
        public bool AimToggledOn { get; private set; }

        private EquipmentController equipment;
        private WeaponAmmoState ammoState;
        private EquippedItemVisual heldVisual;
        private PlayerController playerController;
        private PlayerGkcAnimatorDriver animatorDriver;
        private CombatFocusController combatFocus;
        private float nextFireTime;
        private bool attackInputHeld;
        private bool combatBlockedByUiPointer;

        private void Awake()
        {
            equipment = GetComponent<EquipmentController>();
            ammoState = GetComponent<WeaponAmmoState>();
            heldVisual = GetComponent<EquippedItemVisual>();
            playerController = GetComponent<PlayerController>();
            animatorDriver = GetComponentInChildren<PlayerGkcAnimatorDriver>(true);
            combatFocus = GetComponent<CombatFocusController>();
        }

        private void Update()
        {
            RefreshCombatUiPointerBlock();
            PollAimToggleInput();
            UpdateAimState();

            if (!CanUseRangedCombat())
            {
                if (IsAiming)
                    EndAim();
                return;
            }

            if (attackInputHeld && Time.time >= nextFireTime)
                TryFire();
        }

        public void OnAttack(InputAction.CallbackContext context)
        {
            if (!Application.isPlaying || !GameSession.HasStarted)
                return;

            if (!CanUseRangedCombat())
            {
                attackInputHeld = false;
                return;
            }

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

            if (context.canceled)
                attackInputHeld = false;
        }

        public void OnBlock(InputAction.CallbackContext context)
        {
            if (!Application.isPlaying || !GameSession.HasStarted)
                return;

            if (!CanUseRangedCombat())
            {
                WantsAimInputHeld = false;
                EndAim();
                return;
            }

#if UNITY_EDITOR
            if (context.canceled && PreserveAimDuringEditorPause())
                return;
#endif

            if (context.canceled)
            {
                WantsAimInputHeld = false;
                if (!AimToggledOn)
                    EndAim();
                return;
            }

            if (!context.started && !context.performed)
                return;

            if (IsCombatInputBlocked())
            {
                WantsAimInputHeld = false;
                return;
            }

            WantsAimInputHeld = true;
            BeginAim();
        }

        private void PollAimToggleInput()
        {
            if (!Application.isPlaying || !GameSession.HasStarted)
                return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.zKey.wasPressedThisFrame)
                return;

            if (!CanUseRangedCombat() || IsCombatInputBlocked())
            {
                AimToggledOn = false;
                return;
            }

            AimToggledOn = !AimToggledOn;
        }

        private void UpdateAimState()
        {
            if (!CanUseRangedCombat())
            {
                AimToggledOn = false;
                if (!PreserveAimDuringEditorPause())
                    EndAim();
                return;
            }

            if (PreserveAimDuringEditorPause())
                return;

            if ((WantsAimInputHeld || AimToggledOn) && !IsCombatInputBlocked())
                BeginAim();
            else
                EndAim();
        }

        private void BeginAim()
        {
            if (IsAiming)
                return;

            IsAiming = true;
            playerController?.SetRangedAimActive(true, equipment.DrawnWeaponItem);
            animatorDriver?.SetRangedAimActive(true);
        }

        private void EndAim()
        {
            if (!IsAiming)
                return;

            if (PreserveAimDuringEditorPause())
                return;

            IsAiming = false;
            playerController?.SetRangedAimActive(false, null);
            animatorDriver?.SetRangedAimActive(false);
        }

        private bool TryFire()
        {
            ItemData weapon = equipment.DrawnWeaponItem;
            if (weapon == null || !weapon.IsRangedWeapon)
                return false;

            Transform muzzle = heldVisual != null ? heldVisual.GetMuzzleTransform() : null;
            if (muzzle == null)
                return false;

            if (!ammoState.TryConsumeActiveRound())
            {
                nextFireTime = Time.time + 0.2f;
                return false;
            }

            Vector3 cameraDirection = ResolveFireDirection(muzzle.position);
            Vector3 direction = RangedFireSolver.ResolveDirection(
                cameraDirection,
                muzzle.forward,
                IsAiming,
                weapon.hipFireMaxDeviationDegrees);
            float spreadMultiplier = IsAiming
                ? adsSpreadMultiplier
                : Mathf.Max(0.01f, weapon.hipFireSpreadMultiplier);
            float spread = weapon.projectileSpreadDegrees * spreadMultiplier;
            ItemData ammoItem = ResolveLoadedAmmoItem(weapon);

            CombatProjectile projectile = CombatProjectileSpawner.Spawn(
                gameObject, muzzle, weapon, ammoItem, direction, spread);
            if (projectile == null)
            {
                nextFireTime = Time.time + 0.2f;
                return false;
            }

            animatorDriver?.RequestRangedFire(weapon);

            float interval = weapon.fireRate > 0.01f ? 1f / weapon.fireRate : 0.25f;
            nextFireTime = Time.time + interval;
            EnemyNoiseEvents.RaiseNoise(transform.position, 0.55f, gameObject);
            return true;
        }

        private ItemData ResolveLoadedAmmoItem(ItemData weapon)
        {
            if (equipment == null || inventoryLookupFailed())
                return null;

            AmmoType loadedType = ammoState.GetLoadedAmmoType(equipment.ActiveWeaponHotbarSlot);
            return FindAmmoItemByType(loadedType);
        }

        private bool inventoryLookupFailed() => false;

        private ItemData FindAmmoItemByType(AmmoType type)
        {
            InventorySystem inventory = GetComponent<InventorySystem>();
            if (inventory == null)
                return null;

            for (int i = 0; i < inventory.slots.Count; i++)
            {
                InventorySystem.InventorySlot slot = inventory.slots[i];
                if (slot == null || slot.IsEmpty || slot.item == null || !slot.item.CountsAsAmmo)
                    continue;

                if (slot.item.ammoType == type)
                    return slot.item;
            }

            return null;
        }

        private Vector3 ResolveFireDirection(Vector3 origin)
        {
            Camera camera = playerController != null ? playerController.GameplayCamera : Camera.main;
            if (camera == null)
                return transform.forward;

            if (combatFocus != null
                && combatFocus.IsLocked
                && combatFocus.TryGetAimDirection(origin, out Vector3 focusDirection))
            {
                return focusDirection;
            }

            Ray viewRay = new Ray(camera.transform.position, camera.transform.forward);
            if (Physics.Raycast(
                    viewRay,
                    out RaycastHit hit,
                    120f,
                    aimLayers,
                    QueryTriggerInteraction.Ignore))
            {
                Vector3 toHit = hit.point - origin;
                if (toHit.sqrMagnitude > 0.0001f)
                    return toHit.normalized;
            }

            Vector3 aimPoint = viewRay.GetPoint(120f);
            Vector3 direction = aimPoint - origin;
            if (direction.sqrMagnitude < 0.0001f)
                return camera.transform.forward.normalized;

            return direction.normalized;
        }

        private bool CanUseRangedCombat()
        {
            return equipment != null && equipment.HasActiveRangedWeapon();
        }

        private bool IsCombatInputBlocked()
        {
            if (playerController != null && playerController.BlocksCombatInput)
                return true;

            return combatBlockedByUiPointer;
        }

#if UNITY_EDITOR
        private bool PreserveAimDuringEditorPause()
        {
            return EditorApplication.isPaused && IsAiming;
        }
#else
        private bool PreserveAimDuringEditorPause() => false;
#endif

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying || equipment == null || !equipment.HasActiveRangedWeapon())
                return;

            Transform muzzle = heldVisual != null ? heldVisual.GetMuzzleTransform() : null;
            if (muzzle == null)
                return;

            ItemData weapon = equipment.DrawnWeaponItem;
            Vector3 cameraDirection = ResolveFireDirection(muzzle.position);
            Vector3 resolved = RangedFireSolver.ResolveDirection(
                cameraDirection,
                muzzle.forward,
                IsAiming,
                weapon != null ? weapon.hipFireMaxDeviationDegrees : RangedFireSolver.DefaultHipMaxDeviationDegrees);

            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(muzzle.position, muzzle.forward * 6f);
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(muzzle.position, cameraDirection * 6f);
            Gizmos.color = Color.red;
            Gizmos.DrawRay(muzzle.position, resolved * 6.5f);
        }
#endif

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
    }
}
