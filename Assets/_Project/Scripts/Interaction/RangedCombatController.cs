using Project.Core;
using Project.Data;
using Project.Inventory;
using Project.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Project.Interaction
{
    /// <summary>
    /// Legacy Pioneer ranged input surface kept for input binding compatibility.
    /// Invector shooter input owns aim/fire on the player prefab.
    /// </summary>
    [RequireComponent(typeof(EquipmentController))]
    [RequireComponent(typeof(WeaponAmmoState))]
    [DefaultExecutionOrder(101)]
    public class RangedCombatController : MonoBehaviour
    {
        public bool IsAiming => false;
        public bool WantsAimInputHeld => false;
        public bool AimToggledOn => false;

        private EquipmentController equipment;
        private PlayerController playerController;
        private bool combatBlockedByUiPointer;

        private void Awake()
        {
            equipment = GetComponent<EquipmentController>();
            playerController = GetComponent<PlayerController>();
        }

        public void OnAttack(InputAction.CallbackContext context)
        {
        }

        public void OnBlock(InputAction.CallbackContext context)
        {
        }

        private void Update()
        {
            RefreshCombatUiPointerBlock();
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
