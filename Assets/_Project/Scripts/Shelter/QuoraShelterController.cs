using Project.Core;
using Project.Data;
using Project.Interaction;
using Project.Inventory;
using Project.Player;
using Project.UI;
using UnityEngine;

namespace Project.Shelter
{
    /// <summary>
    /// Deployed Quora Shelter world object: timed lifetime, enter/exit, and hold-E shelter menu.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class QuoraShelterController : MonoBehaviour, IWorldUsable, IHoldWorldUsable
    {
        private const float InteractRange = 3f;
        private const float HoldMenuDurationSeconds = 0.65f;

        [SerializeField] private ItemData shelterItem;
        [SerializeField] private Transform enterPoint;
        [SerializeField] private Transform hiddenCrewHolder;
        [SerializeField] private Transform cameraPivot;

        private QuoraShelterOccupancy occupancy;
        private QuoraShelterOrbitCamera orbitCamera;
        private Collider interactCollider;
        [SerializeField] private float remainingLifetimeSeconds = QuoraShelterStorageState.DefaultLifetimeSeconds;
        private bool holdActive;
        private float holdProgress;
        private bool timerPaused;

        public static QuoraShelterController ActiveOccupiedShelter { get; private set; }

        public float RemainingLifetimeSeconds => remainingLifetimeSeconds;
        public bool IsOccupied => occupancy != null && occupancy.IsOccupied;
        public float HoldDurationSeconds => HoldMenuDurationSeconds;
        public string HoldPromptText => "Hold E — Shelter options";
        public bool IsHoldActive => holdActive;

        private void Awake()
        {
            EnsureRuntimeLifetime();

            interactCollider = GetComponent<Collider>();
            if (interactCollider != null)
                interactCollider.isTrigger = true;

            occupancy = GetComponent<QuoraShelterOccupancy>();
            if (occupancy == null)
                occupancy = gameObject.AddComponent<QuoraShelterOccupancy>();

            orbitCamera = GetComponent<QuoraShelterOrbitCamera>();
            if (orbitCamera == null)
                orbitCamera = gameObject.AddComponent<QuoraShelterOrbitCamera>();

            if (enterPoint == null)
            {
                GameObject enterObject = new GameObject("EnterPoint");
                enterObject.transform.SetParent(transform, false);
                enterObject.transform.localPosition = new Vector3(0f, 0f, 1.8f);
                enterPoint = enterObject.transform;
            }

            if (hiddenCrewHolder == null)
            {
                GameObject holderObject = new GameObject("HiddenCrewHolder");
                holderObject.transform.SetParent(transform, false);
                hiddenCrewHolder = holderObject.transform;
            }

            if (cameraPivot == null)
            {
                GameObject pivotObject = new GameObject("CameraPivot");
                pivotObject.transform.SetParent(transform, false);
                pivotObject.transform.localPosition = new Vector3(0f, 1.6f, 0f);
                cameraPivot = pivotObject.transform;
            }

            occupancy.Configure(hiddenCrewHolder);
            orbitCamera.Configure(cameraPivot);
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

            if (ActiveOccupiedShelter == this)
                ActiveOccupiedShelter = null;

            WorldUseController.Unregister(this);
        }

        private void Update()
        {
            if (timerPaused || !IsOccupied || remainingLifetimeSeconds <= 0f)
                return;

            remainingLifetimeSeconds -= Time.deltaTime;
            if (remainingLifetimeSeconds <= 0f)
                ExpireAndDestroy();
        }

        public void InitializeDeployed(float remainingSeconds)
        {
            remainingLifetimeSeconds = Mathf.Clamp(remainingSeconds, 0f, QuoraShelterStorageState.DefaultLifetimeSeconds);
            timerPaused = true;
        }

        /// <summary>Stops the deploy lifetime countdown while the player is outside or before storing.</summary>
        public void PauseLifetimeTimer()
        {
            timerPaused = true;
        }

        /// <summary>Resumes the deploy lifetime countdown while the player is sheltered inside.</summary>
        public void ResumeLifetimeTimer()
        {
            timerPaused = false;
        }

        private void EnsureRuntimeLifetime()
        {
            if (remainingLifetimeSeconds > 0f)
                return;

            remainingLifetimeSeconds = QuoraShelterStorageState.DefaultLifetimeSeconds;
        }

        public static QuoraShelterController FindAnyDeployed()
        {
            return FindAnyObjectByType<QuoraShelterController>();
        }

        public float GetUsePriority(WorldUseContext context)
        {
            if (!IsWithinRange(context.PlayerPosition))
                return -1f;

            if (IsOccupied && ActiveOccupiedShelter == this)
                return -1f;

            if (IsOccupied)
                return -1f;

            float distance = PlayerInteractionUtility.DistanceToInteractable(
                context.PlayerPosition,
                interactCollider,
                transform.position);

            return 88f - distance;
        }

        public bool TryUse(WorldUseContext context)
        {
            if (IsOccupied || !IsWithinRange(context.PlayerPosition))
                return false;

            PlayerController player = PlayerLocator.FindPlayerController();
            return player != null && TryEnter(player);
        }

        public bool CanBeginHold(WorldUseContext context)
        {
            return IsOccupied && ActiveOccupiedShelter == this;
        }

        public void BeginHold(WorldUseContext context)
        {
            holdActive = true;
            holdProgress = 0f;
        }

        public bool TickHold(WorldUseContext context, float deltaTime, out float progress01)
        {
            if (!holdActive)
            {
                progress01 = 0f;
                return false;
            }

            if (!CanBeginHold(context))
            {
                CancelHold(context);
                progress01 = 0f;
                return false;
            }

            holdProgress += deltaTime / Mathf.Max(0.05f, HoldMenuDurationSeconds);
            progress01 = Mathf.Clamp01(holdProgress);

            if (holdProgress < 1f)
                return false;

            holdActive = false;
            OpenShelterMenu();
            return true;
        }

        public void CancelHold(WorldUseContext context)
        {
            holdActive = false;
            holdProgress = 0f;
        }

        public bool TryEnter(PlayerController player)
        {
            if (player == null || IsOccupied || remainingLifetimeSeconds <= 0f)
                return false;

            if (!occupancy.TryEnter(player))
                return false;

            ActiveOccupiedShelter = this;
            player.SetShelterSessionOpen(true);
            orbitCamera.Activate(player);
            ResumeLifetimeTimer();
            PickupToastUI.Show("Sheltered from the storm.");
            QuoraShelterTimerUI.Show(this);
            return true;
        }

        public bool TryExitShelter(bool storeInInventory)
        {
            PlayerController player = PlayerLocator.FindPlayerController();
            if (player == null || !IsOccupied)
                return false;

            Vector3 exitPosition = enterPoint != null ? enterPoint.position : transform.position + transform.forward * 1.8f;
            Quaternion exitRotation = enterPoint != null ? enterPoint.rotation : transform.rotation;

            orbitCamera.Deactivate();
            occupancy.TryExit(player, exitPosition, exitRotation);
            player.SetShelterSessionOpen(false);
            QuoraShelterTimerUI.Hide();
            ActiveOccupiedShelter = null;
            PauseLifetimeTimer();

            if (!storeInInventory)
                return true;

            InventorySystem inventory = player.GetComponent<InventorySystem>();
            ItemData item = ResolveShelterItem();
            bool stored = QuoraShelterDeploymentUtility.TryStore(this, inventory, item, out string message);
            if (!string.IsNullOrEmpty(message))
                PickupToastUI.Show(message);

            return stored;
        }

        private void OpenShelterMenu()
        {
            Canvas canvas = ResolveGameplayCanvas();
            if (canvas == null)
                return;

            QuoraShelterMenuUI menu = QuoraShelterMenuUI.EnsureExists(canvas.transform);
            menu.Show(this);
        }

        private void ExpireAndDestroy()
        {
            if (IsOccupied)
            {
                PlayerController player = PlayerLocator.FindPlayerController();
                if (player != null)
                    TryExitShelter(storeInInventory: false);
            }

            PickupToastUI.Show("Quora Shelter collapsed.");
            Destroy(gameObject);
        }

        private ItemData ResolveShelterItem()
        {
            if (shelterItem != null)
                return shelterItem;

            return ItemRegistry.Resolve("Quora Shelter");
        }

        private bool IsWithinRange(Vector3 playerPosition)
        {
            Vector3 target = enterPoint != null ? enterPoint.position : transform.position;
            return Vector3.Distance(playerPosition, target) <= InteractRange;
        }

        public static QuoraShelterController FindBestForUse(WorldUseContext context, float minPriority)
        {
            QuoraShelterController[] shelters = SceneComponentCache.GetAll<QuoraShelterController>(FindObjectsInactive.Exclude);
            QuoraShelterController best = null;
            float bestPriority = float.MinValue;

            for (int i = 0; i < shelters.Length; i++)
            {
                QuoraShelterController shelter = shelters[i];
                if (shelter == null || shelter.IsOccupied)
                    continue;

                float priority = shelter.GetUsePriority(context);
                if (priority < minPriority || priority <= bestPriority)
                    continue;

                best = shelter;
                bestPriority = priority;
            }

            return best;
        }

        public static bool TryUseBest(WorldUseContext context, float minPriority)
        {
            QuoraShelterController best = FindBestForUse(context, minPriority);
            return best != null && best.TryUse(context);
        }

        public static string TryGetInteractionPrompt(WorldUseContext context)
        {
            if (ActiveOccupiedShelter != null && ActiveOccupiedShelter.IsOccupied)
                return ActiveOccupiedShelter.HoldPromptText;

            QuoraShelterController best = FindBestForUse(context, 0f);
            return best != null ? "Press E — Enter Quora Shelter" : null;
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
    }
}
