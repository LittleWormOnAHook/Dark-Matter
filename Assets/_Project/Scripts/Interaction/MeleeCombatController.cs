using Project.Combat;
using Project.Core;
using Project.Data;
using Project.Inventory;
using Project.Player;
using Project.Player.Invector;
using Project.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Project.Interaction
{
    /// <summary>
    /// Pioneer input adapter for melee-related actions on the Invector player.
    /// Combat animations and damage are handled by Invector (PioneerShooterMeleeInput / vMeleeManager).
    /// </summary>
    [RequireComponent(typeof(EquipmentController))]
    [DefaultExecutionOrder(100)]
    public class MeleeCombatController : MonoBehaviour
    {
        public bool IsBlocking => false;
        public bool IsAttackInputActive => false;
        public float LastAttackTime => float.NegativeInfinity;

        private EquipmentController equipment;
        private PlayerController playerController;
        private UIManager uiManager;
        private bool combatBlockedByUiPointer;

        private void Awake()
        {
            equipment = GetComponent<EquipmentController>();
            playerController = GetComponent<PlayerController>();
            uiManager = FindAnyObjectByType<UIManager>();
        }

        public void OnBlock(InputAction.CallbackContext context)
        {
            if (!Application.isPlaying || !GameSession.HasStarted)
                return;

            OpticsController optics = GetComponent<OpticsController>();
            if (optics != null)
                optics.TryHandleBlockInput(context);
        }

        public void OnSwitchWeapon(InputAction.CallbackContext context)
        {
            if (!context.performed || equipment == null || !GameSession.HasStarted)
                return;

            if (IsCombatInputBlocked())
                return;

            equipment.SwitchActiveWeapon();
        }

        public void OnAttack(InputAction.CallbackContext context)
        {
            // Invector shooter/melee input owns attack on the player prefab.
        }

        /// <summary>
        /// Called by <see cref="WeaponHitbox"/> when a legacy swing overlap hits a collider.
        /// Invector combat normally routes damage through <see cref="PioneerInvectorDamageBridge"/>.
        /// </summary>
        public void ProcessWeaponHit(Collider hitCollider, ItemData item, bool isCritical)
        {
            if (hitCollider == null)
                return;

            if (item != null && item.itemType == ItemType.MeleeWeapon)
                isCritical = item.RollCriticalHit();

            float damage = item != null ? item.RollMeleeDamage(isCritical) : 8f;
            Vector3 weaponPoint = hitCollider.bounds.center;
            PioneerInvectorDamageBridge.ApplyPioneerDamageToCollider(
                hitCollider, damage, gameObject, isCritical, weaponPoint);

            CombatHitAudio.PlayWeaponHit(weaponPoint, isCritical, hitCollider);
        }

        private void Update()
        {
            RefreshCombatUiPointerBlock();
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
    }
}
