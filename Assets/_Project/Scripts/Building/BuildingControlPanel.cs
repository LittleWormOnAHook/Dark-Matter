using Project.Core;
using Project.Crafting;
using Project.Interaction;
using Project.Inventory;
using Project.UI;
using UnityEngine;

namespace Project.Building
{
    [RequireComponent(typeof(Collider))]
    public class BuildingControlPanel : MonoBehaviour, IWorldUsable
    {
        [Header("Building")]
        [SerializeField] private string buildingDisplayName = "Command Center";
        [SerializeField] private string buildingId = "command_center";

        [Header("Craft Tab")]
        [SerializeField] private bool hasCraftStation;
        [SerializeField] private CraftingStationType craftStationType = CraftingStationType.Workbench;

        [Header("Interaction")]
        [SerializeField] private string promptText = "Press E to use";
        [SerializeField] private float interactRange = 3.5f;

        private UIManager uiManager;
        private Collider interactCollider;
        private bool playerInRange;

        public string BuildingDisplayName => buildingDisplayName;
        public string BuildingId => buildingId;
        public bool HasCraftStation => hasCraftStation;
        public CraftingStationType CraftStationType => craftStationType;
        public Collider InteractCollider => interactCollider;
        public bool IsPlayerInRange => playerInRange;

        public void Configure(
            string displayName,
            string id,
            bool bindCraftStation = false,
            CraftingStationType stationType = CraftingStationType.Workbench,
            string prompt = "Press E to use",
            float range = 3.5f)
        {
            buildingDisplayName = displayName;
            buildingId = id;
            hasCraftStation = bindCraftStation;
            craftStationType = stationType;
            if (!string.IsNullOrEmpty(prompt))
                promptText = prompt;
            interactRange = range;
            EnsureInteractionCollider();
        }

        private void Awake()
        {
            interactCollider = GetComponent<Collider>();
            if (interactCollider == null)
                interactCollider = GetComponentInChildren<Collider>();
            EnsureInteractionCollider();
        }

        private void Start()
        {
            uiManager = FindAnyObjectByType<UIManager>();
            playerInRange = IsPlayerNearby();
        }

        private void Update()
        {
            RefreshProximityState();
        }

        private void OnEnable()
        {
            WorldUseController.Register(this);
        }

        private void OnDisable()
        {
            WorldUseController.Unregister(this);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!PlayerInteractionUtility.IsPlayerCollider(other))
                return;

            playerInRange = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (!PlayerInteractionUtility.IsPlayerCollider(other))
                return;

            if (!IsPlayerNearby())
                playerInRange = false;
        }

        private void RefreshProximityState()
        {
            if (!GameSession.HasStarted)
                return;

            playerInRange = IsPlayerNearby();
        }

        public void EnsureInteractionCollider()
        {
            BoxCollider box = GetComponent<BoxCollider>();
            if (box == null)
            {
                Collider existing = GetComponent<Collider>();
                if (existing != null && existing is not BoxCollider)
                {
                    Transform triggerHost = transform.Find("PanelInteract");
                    if (triggerHost == null)
                    {
                        GameObject hostObject = new GameObject("PanelInteract");
                        triggerHost = hostObject.transform;
                        triggerHost.SetParent(transform, false);
                    }

                    box = triggerHost.GetComponent<BoxCollider>();
                    if (box == null)
                        box = triggerHost.gameObject.AddComponent<BoxCollider>();
                }
                else if (existing == null)
                    box = gameObject.AddComponent<BoxCollider>();
                else
                    box = (BoxCollider)existing;
            }

            if (box == null)
            {
                Debug.LogWarning($"[BuildingControlPanel] Could not ensure BoxCollider on '{name}'.", this);
                return;
            }

            box.isTrigger = true;
            box.center = new Vector3(0f, 1.1f, 0f);
            box.size = new Vector3(1.6f, 2.2f, 1.2f);

            if (interactCollider == null || interactCollider != box)
                interactCollider = box;
        }

        public bool IsWithinInteractRange(Vector3 playerPosition)
        {
            return PlayerInteractionUtility.DistanceToInteractable(
                playerPosition,
                interactCollider,
                transform.position) <= interactRange;
        }

        public string GetInteractionPromptMessage()
        {
            string label = string.IsNullOrEmpty(buildingDisplayName) ? "Building Control" : buildingDisplayName;
            return $"{promptText} — {label}";
        }

        public float GetUsePriority(WorldUseContext context)
        {
            if (WorldUseController.IsPlayerFocusedOnPickup(context)
                || WorldUseController.HasCompetingNearbyItemPickup(context))
                return -1f;

            if (!WorldUseController.IsAimedAtBuildingControlPanel(context, this, interactCollider))
                return -1f;

            if (!GameSession.HasStarted || !IsWithinInteractRange(context.PlayerPosition))
                return -1f;

            float distance = PlayerInteractionUtility.DistanceToInteractable(
                context.PlayerPosition,
                interactCollider,
                transform.position);
            return 90f - distance;
        }

        public bool TryUse(WorldUseContext context)
        {
            return TryInteract();
        }

        public bool TryInteract()
        {
            GameObject player = PlayerLocator.FindPlayerObject();
            if (player == null || !IsWithinInteractRange(player.transform.position) || !GameSession.HasStarted)
                return false;

            ResourceGatherer gatherer = player.GetComponent<ResourceGatherer>();
            float pickupRange = gatherer != null ? gatherer.pickupRange : 4f;
            if (WorldUseController.ShouldBlockNonPickupUse(player.transform, gatherer, pickupRange))
                return false;

            Camera camera = WorldUseController.ResolveViewCameraForInteract(player.transform);
            if (camera == null)
                return false;

            Ray viewRay = WorldUseController.BuildScreenCenterRay(camera, player.transform);
            RaycastHit? aimHit = null;
            if (gatherer != null
                && Physics.Raycast(viewRay, out RaycastHit hit, gatherer.gatherRange, gatherer.resourceLayer, QueryTriggerInteraction.Collide))
            {
                aimHit = hit;
            }

            WorldUseContext context = new WorldUseContext(
                player.transform,
                player.transform.position,
                camera,
                player.GetComponent<InventorySystem>(),
                gatherer,
                pickupRange,
                viewRay,
                aimHit);

            if (!WorldUseController.IsAimedAtBuildingControlPanel(context, this, interactCollider))
                return false;

            playerInRange = true;
            uiManager?.HideInteractionPrompt();
            BuildingControlPanelUI.Show(this);
            return true;
        }

        private bool IsPlayerNearby()
        {
            return PlayerInteractionUtility.TryGetPlayerPosition(out Vector3 playerPosition)
                && IsWithinInteractRange(playerPosition);
        }
    }
}
