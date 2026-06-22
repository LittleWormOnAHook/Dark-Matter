using Project.Core;
using Project.Interaction;
using Project.Inventory;
using Project.UI;
using UnityEngine;

namespace Project.Crafting
{
    [RequireComponent(typeof(Collider))]
    public class CraftingStation : MonoBehaviour, IWorldUsable
    {
        [Header("Station")]
        [SerializeField] private CraftingStationType stationType = CraftingStationType.Cooking;

        [Header("Interaction")]
        [SerializeField] private string promptText = "Press E to use";
        [SerializeField] private float interactRange = 3.5f;

        private UIManager uiManager;
        private CraftingManager craftingManager;
        private Collider interactCollider;
        private bool playerInRange;

        public CraftingStationType StationType => stationType;
        public bool IsPlayerInRange => playerInRange;
        public Collider InteractCollider => interactCollider;

        public void Configure(CraftingStationType type, string prompt = "Press E to use", float range = 3.5f)
        {
            stationType = type;
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

        private void Update()
        {
            RefreshProximityState();
        }

        private void RefreshProximityState()
        {
            if (!GameSession.HasStarted)
                return;

            bool nearby = IsPlayerNearby();
            if (nearby == playerInRange)
                return;

            playerInRange = nearby;
            if (playerInRange)
            {
                ShowPrompt();
                if (craftingManager == null)
                    craftingManager = CraftingManager.Instance ?? FindAnyObjectByType<CraftingManager>();

                if (craftingManager != null)
                {
                    craftingManager.CurrentStation = stationType;
                    FindAnyObjectByType<CraftingUI>()?.RefreshRecipeList();
                }
            }
            else
            {
                ResolveUiManager()?.HideInteractionPrompt();
                if (craftingManager != null && craftingManager.CurrentStation == stationType)
                    craftingManager.CurrentStation = null;
            }
        }

        private void OnEnable()
        {
            WorldUseController.Register(this);
        }

        private void OnDisable()
        {
            WorldUseController.Unregister(this);
        }

        public float GetUsePriority(WorldUseContext context)
        {
            if (!WorldUseController.IsAimedAtCraftingStation(context, this, interactCollider))
                return -1f;

            if (!GameSession.HasStarted || !IsWithinInteractRange(context.PlayerPosition))
                return -1f;

            float distance = PlayerInteractionUtility.DistanceToInteractable(
                context.PlayerPosition,
                interactCollider,
                transform.position);
            return 88f - distance;
        }

        public bool TryUse(WorldUseContext context)
        {
            return TryInteract();
        }

        public void EnsureInteractionCollider()
        {
            BoxCollider box = GetComponent<BoxCollider>();
            if (box == null)
            {
                Collider existing = GetComponent<Collider>();
                if (existing != null && existing is not BoxCollider)
                {
                    Transform triggerHost = transform.Find("CraftInteract");
                    if (triggerHost == null)
                    {
                        GameObject hostObject = new GameObject("CraftInteract");
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

            box.isTrigger = true;
            box.center = new Vector3(0f, 1.1f, 0f);
            box.size = stationType == CraftingStationType.Cooking
                ? new Vector3(2f, 2.5f, 2f)
                : new Vector3(2.2f, 2.2f, 2.2f);

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

        private bool IsPlayerNearby()
        {
            return PlayerInteractionUtility.TryGetPlayerPosition(out Vector3 playerPosition)
                && IsWithinInteractRange(playerPosition);
        }

        private UIManager ResolveUiManager()
        {
            if (uiManager == null)
                uiManager = FindAnyObjectByType<UIManager>();
            return uiManager;
        }

        private void ShowPrompt()
        {
            UIManager manager = ResolveUiManager();
            if (manager == null)
                return;

            string stationLabel = stationType == CraftingStationType.Cooking ? "Cooking Pot" : "Workbench";
            manager.ShowInteractionPrompt($"{promptText} — {stationLabel}");
        }

        private void Start()
        {
            uiManager = FindAnyObjectByType<UIManager>();
            craftingManager = CraftingManager.Instance ?? FindAnyObjectByType<CraftingManager>();

            playerInRange = IsPlayerNearby();
            if (playerInRange)
                ShowPrompt();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!PlayerInteractionUtility.IsPlayerCollider(other))
                return;

            playerInRange = true;
            ShowPrompt();

            if (craftingManager == null)
                craftingManager = CraftingManager.Instance ?? FindAnyObjectByType<CraftingManager>();

            if (craftingManager != null)
            {
                craftingManager.CurrentStation = stationType;
                FindAnyObjectByType<CraftingUI>()?.RefreshRecipeList();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!PlayerInteractionUtility.IsPlayerCollider(other))
                return;

            if (!IsPlayerNearby())
            {
                playerInRange = false;
                ResolveUiManager()?.HideInteractionPrompt();

                if (craftingManager == null)
                    craftingManager = CraftingManager.Instance ?? FindAnyObjectByType<CraftingManager>();

                if (craftingManager != null && craftingManager.CurrentStation == stationType)
                    craftingManager.CurrentStation = null;
            }
        }

        public static CraftingStation GetInteractable(Vector3 playerPosition, float range)
        {
            CraftingStation[] stations = FindObjectsByType<CraftingStation>(FindObjectsInactive.Exclude);
            CraftingStation best = null;
            float bestDistance = range;

            for (int i = 0; i < stations.Length; i++)
            {
                CraftingStation station = stations[i];
                if (station == null || !station.playerInRange || !station.IsWithinInteractRange(playerPosition))
                    continue;

                float distance = Vector3.Distance(playerPosition, station.transform.position);
                if (distance <= bestDistance)
                {
                    best = station;
                    bestDistance = distance;
                }
            }

            return best;
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

            if (!WorldUseController.IsAimedAtCraftingStation(context, this, interactCollider))
                return false;

            playerInRange = true;

            if (craftingManager == null)
                craftingManager = CraftingManager.Instance ?? FindAnyObjectByType<CraftingManager>();

            if (craftingManager == null)
            {
                if (player != null)
                    craftingManager = player.GetComponent<CraftingManager>() ?? player.AddComponent<CraftingManager>();
            }

            if (craftingManager == null)
                return false;

            craftingManager.CurrentStation = stationType;
            uiManager?.HideInteractionPrompt();

            JournalPanelUI journal = FindAnyObjectByType<JournalPanelUI>();
            if (journal != null)
            {
                journal.OpenToCraftTab(stationType);
                return true;
            }

            return false;
        }
    }
}
