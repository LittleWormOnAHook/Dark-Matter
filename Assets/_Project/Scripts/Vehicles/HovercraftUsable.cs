using Project.Core;
using Project.Data;
using Project.Interaction;
using Project.Inventory;
using Project.Player;
using Project.UI;
using UnityEngine;

namespace Project.Vehicles
{
    [DisallowMultipleComponent]
    public class HovercraftUsable : MonoBehaviour, IWorldUsable
    {
        [SerializeField] private HovercraftController controller;
        [SerializeField] private HovercraftOccupancy occupancy;
        [SerializeField] private HovercraftFuelSystem fuelSystem;
        [Tooltip("Hovercraft ItemData used by 'Store in Inventory'. Falls back to ItemRegistry.Resolve(\"Hovercraft\") when empty.")]
        [SerializeField] private ItemData hovercraftItem;

        public HovercraftFuelSystem FuelSystem => fuelSystem;
        public HovercraftController Controller => controller;
        public ItemData HovercraftItem => ResolveHovercraftItem();

        private void Reset()
        {
            WireSerializedRefs();
        }

        private void OnValidate()
        {
            WireSerializedRefs();
        }

        private void WireSerializedRefs()
        {
            if (controller == null)
                controller = GetComponent<HovercraftController>();
            if (occupancy == null)
                occupancy = GetComponent<HovercraftOccupancy>();
            if (fuelSystem == null)
                fuelSystem = GetComponent<HovercraftFuelSystem>();
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
                return;

            WorldUseController.Register(this);
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
                return;

            WorldUseController.Unregister(this);
        }

        public float GetUsePriority(WorldUseContext context)
        {
            if (controller == null || occupancy == null || occupancy.IsOccupied)
                return -1f;

            HovercraftProfile profile = controller.Profile;
            if (profile == null)
                return -1f;

            Transform enterPoint = occupancy.EnterPoint;
            Vector3 target = enterPoint != null ? enterPoint.position : transform.position;
            float distance = Vector3.Distance(context.PlayerPosition, target);
            if (distance > profile.enterRange)
                return -1f;

            float aimBonus = 0f;
            if (enterPoint != null)
            {
                float rayDistance = WorldUseController.GetViewRayDistance(context.ViewRay, target + Vector3.up * 1.2f);
                if (rayDistance <= 1.4f)
                    aimBonus = 120f;
            }

            return 92f - distance + aimBonus;
        }

        public bool TryUse(WorldUseContext context)
        {
            if (controller == null || occupancy == null || occupancy.IsOccupied)
                return false;

            Canvas canvas = ResolveGameplayCanvas();
            if (canvas == null)
                return false;

            HovercraftInteractMenuUI menu = HovercraftInteractMenuUI.EnsureExists(canvas.transform);
            menu.Show(this);
            return true;
        }

        private static Canvas ResolveGameplayCanvas()
        {
            UIManager uiManager = FindAnyObjectByType<UIManager>();
            if (uiManager != null)
            {
                Canvas uiCanvas = uiManager.GetComponent<Canvas>();
                if (uiCanvas != null)
                    return uiCanvas;
            }

            GameObject mainCanvasObject = GameObject.Find("MainCanvas");
            if (mainCanvasObject != null && mainCanvasObject.TryGetComponent(out Canvas mainCanvas))
                return mainCanvas;

            return FindAnyObjectByType<Canvas>();
        }

        /// <summary>Called by HovercraftInteractMenuUI's "Enter" button.</summary>
        public bool TryEnterFromMenu()
        {
            if (controller == null || occupancy == null || occupancy.IsOccupied)
                return false;

            GameObject player = PlayerLocator.FindPlayerObject();
            PlayerController playerController = player != null ? player.GetComponent<PlayerController>() : null;
            return playerController != null && controller.TryEnter(playerController);
        }

        /// <summary>Called by HovercraftInteractMenuUI's "Refuel" button.</summary>
        public bool TryRefuelFromMenu(InventorySystem inventory, out string message)
        {
            if (fuelSystem == null)
            {
                message = "This hovercraft has no fuel tank.";
                return false;
            }

            return fuelSystem.TryRefuelOneUnit(inventory, out message);
        }

        /// <summary>Called by HovercraftInteractMenuUI's "Store in Inventory" button.</summary>
        public bool TryStoreFromMenu(InventorySystem inventory, out string message)
        {
            ItemData item = ResolveHovercraftItem();
            return HovercraftDeploymentUtility.TryStore(controller, inventory, item, out message);
        }

        private ItemData ResolveHovercraftItem()
        {
            if (hovercraftItem != null)
                return hovercraftItem;

            return ItemRegistry.Resolve("Hovercraft");
        }

        public static string TryGetBoardPrompt(WorldUseContext context)
        {
            if (PlayerVehicleState.IsMounted)
                return null;

            HovercraftUsable[] usables = Object.FindObjectsByType<HovercraftUsable>(FindObjectsInactive.Exclude);
            HovercraftUsable best = null;
            float bestPriority = -1f;

            for (int i = 0; i < usables.Length; i++)
            {
                HovercraftUsable usable = usables[i];
                if (usable == null)
                    continue;

                float priority = usable.GetUsePriority(context);
                if (priority <= bestPriority)
                    continue;

                best = usable;
                bestPriority = priority;
            }

            if (best == null || bestPriority < 0f)
                return null;

            return "Press E for hovercraft options";
        }

        public static string TryGetExitPrompt()
        {
            if (!PlayerVehicleState.IsMounted || PlayerVehicleState.ActiveCraft == null)
                return null;

            HovercraftProfile profile = PlayerVehicleState.ActiveCraft.Profile;
            return profile != null && !string.IsNullOrWhiteSpace(profile.exitPrompt)
                ? profile.exitPrompt
                : "Press E to exit hovercraft";
        }
    }
}
