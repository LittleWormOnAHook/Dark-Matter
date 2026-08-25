using Project.Core;
using Project.Interaction;
using Project.Player;
using Project.UI;
using Project.World;
using UnityEngine;

namespace Project.Interaction
{
    /// <summary>
    /// Press-E interactable for the Walker Drill mining mech. Opens a three-button popup:
    /// Start Mining, Stop Mining, Collect Resources (stub).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DMWalkerDrillUsable : MonoBehaviour, IWorldUsable
    {
        [Header("Interaction")]
        [SerializeField] private string promptText = "Press E — Walker Drill";
        [SerializeField] private float interactRange = 4f;

        [Header("References")]
        [SerializeField] private DMWalkerDrillController drillController;
        [SerializeField] private Collider interactCollider;

        private const float ProximityCheckInterval = 0.15f;
        private float nextProximityCheckTime;

        public DMWalkerDrillController DrillController => drillController;
        public Collider InteractCollider => interactCollider;
        public float InteractRange => interactRange;

        private void Reset()
        {
            WireSerializedRefs();
        }

        private void OnValidate()
        {
            WireSerializedRefs();
        }

        private void Awake()
        {
            WireSerializedRefs();
            EnsureInteractionCollider();
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

            if (WalkerDrillInteractMenuUI.IsShowing(this))
                WalkerDrillInteractMenuUI.CloseAny();

            WorldUseController.Unregister(this);
        }

        private void Update()
        {
            if (!Application.isPlaying)
                return;

            if (Time.unscaledTime < nextProximityCheckTime)
                return;

            nextProximityCheckTime = Time.unscaledTime + ProximityCheckInterval;

            if (WalkerDrillInteractMenuUI.IsShowing(this) && !IsWithinInteractRange(GetPlayerPosition()))
                WalkerDrillInteractMenuUI.CloseAny();
        }

        private void WireSerializedRefs()
        {
            if (drillController == null)
                drillController = GetComponent<DMWalkerDrillController>();
            if (drillController == null)
                drillController = GetComponentInChildren<DMWalkerDrillController>();

            if (interactCollider == null)
                interactCollider = GetComponent<Collider>();
            if (interactCollider == null)
                interactCollider = GetComponentInChildren<Collider>();
        }

        public float GetUsePriority(WorldUseContext context)
        {
            if (!GameSession.HasStarted || !IsWithinInteractRange(context.PlayerPosition))
                return -1f;

            float distance = PlayerInteractionUtility.DistanceToInteractable(
                context.PlayerPosition,
                interactCollider,
                transform.position);

            float aimBonus = 0f;
            if (interactCollider != null)
            {
                Vector3 aimPoint = interactCollider.bounds.center;
                float rayDistance = WorldUseController.GetViewRayDistance(context.ViewRay, aimPoint);
                if (rayDistance <= 1.8f)
                    aimBonus = 90f;
            }

            return 90f - distance + aimBonus;
        }

        public bool TryUse(WorldUseContext context)
        {
            if (!IsWithinInteractRange(context.PlayerPosition))
                return false;

            if (WalkerDrillInteractMenuUI.IsShowing(this))
            {
                WalkerDrillInteractMenuUI.CloseAny();
                return true;
            }

            Canvas canvas = ResolveGameplayCanvas();
            if (canvas == null)
                return false;

            WalkerDrillInteractMenuUI menu = WalkerDrillInteractMenuUI.EnsureExists(canvas.transform);
            menu.Show(this);
            return true;
        }

        public void EnsureInteractionCollider()
        {
            BoxCollider box = GetComponent<BoxCollider>();
            if (box == null)
            {
                Collider existing = GetComponent<Collider>();
                if (existing != null && existing is not BoxCollider)
                {
                    Transform triggerHost = transform.Find("InteractVolume");
                    if (triggerHost == null)
                    {
                        GameObject hostObject = new GameObject("InteractVolume");
                        triggerHost = hostObject.transform;
                        triggerHost.SetParent(transform, false);
                    }

                    box = triggerHost.GetComponent<BoxCollider>();
                    if (box == null)
                        box = triggerHost.gameObject.AddComponent<BoxCollider>();
                }
                else if (existing == null)
                {
                    box = gameObject.AddComponent<BoxCollider>();
                }
                else
                {
                    box = (BoxCollider)existing;
                }
            }

            box.isTrigger = true;
            if (box.size.sqrMagnitude < 0.01f)
            {
                box.center = new Vector3(0f, 1.5f, 0f);
                box.size = new Vector3(3.5f, 3f, 3.5f);
            }

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
            return promptText;
        }

        public static string TryGetPrompt(WorldUseContext context)
        {
            DMWalkerDrillUsable[] usables = Object.FindObjectsByType<DMWalkerDrillUsable>(FindObjectsInactive.Exclude);
            DMWalkerDrillUsable best = null;
            float bestPriority = -1f;

            for (int i = 0; i < usables.Length; i++)
            {
                DMWalkerDrillUsable usable = usables[i];
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

            return best.GetInteractionPromptMessage();
        }

        private static Vector3 GetPlayerPosition()
        {
            return PlayerInteractionUtility.TryGetPlayerPosition(out Vector3 position)
                ? position
                : Vector3.positiveInfinity;
        }

        private static Canvas ResolveGameplayCanvas()
        {
            UIManager uiManager = Object.FindAnyObjectByType<UIManager>();
            if (uiManager != null)
            {
                Canvas uiCanvas = uiManager.GetComponent<Canvas>();
                if (uiCanvas != null)
                    return uiCanvas;
            }

            GameObject mainCanvasObject = GameObject.Find("MainCanvas");
            if (mainCanvasObject != null && mainCanvasObject.TryGetComponent(out Canvas mainCanvas))
                return mainCanvas;

            return Object.FindAnyObjectByType<Canvas>();
        }
    }
}
