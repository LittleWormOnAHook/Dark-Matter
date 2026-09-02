using Project.Core;
using Project.Interaction;
using Project.Player;
using Project.Survival;
using Project.Vehicles;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// Screen-space pickup aim reticle. World-space Invector combat reticles are left on uGUI.
    /// DMUiToolkit 0901-finish
    /// </summary>
    [DefaultExecutionOrder(-366)]
    [DisallowMultipleComponent]
    public class DMUiToolkitPickupReticle : MonoBehaviour
    {
        private const float FallbackPickupRange = 4f;
        private const float AimSampleInterval = 0.1f;

        private static DMUiToolkitPickupReticle instance;

        private UIDocument document;
        private VisualElement root;
        private VisualElement dot;
        private bool bound;
        private WorldUseController useController;
        private ResourceGatherer gatherer;
        private Camera worldCamera;
        private PlayerController cachedPlayer;
        private SurvivalStats cachedSurvival;
        private float nextAimSampleTime;
        private bool hasAimedPickup;
        private bool uguiHidden;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!Application.isPlaying || !DMUiToolkitConfig.IsEnabled)
                return;

            EnsureHost();
        }

        public static DMUiToolkitPickupReticle EnsureHost()
        {
            if (instance != null)
                return instance;

            UIDocument doc = DMUiToolkitOverlayDocument.Ensure(
                DMUiToolkitOverlayDocument.PickupReticleName,
                DMUiToolkitOverlayDocument.PickupReticleUxml,
                DMUiToolkitOverlayDocument.PickupReticleUss,
                DMUiToolkitOverlayDocument.PickupReticleSort);
            if (doc == null)
                return null;

            DMUiToolkitPickupReticle host = doc.GetComponent<DMUiToolkitPickupReticle>();
            if (host == null)
                host = doc.gameObject.AddComponent<DMUiToolkitPickupReticle>();

            host.document = doc;
            host.BindTree();
            return host;
        }

        private void Awake()
        {
            instance = this;
            if (document == null)
                document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            instance = this;
            BindTree();
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        private void LateUpdate()
        {
            if (!bound)
                BindTree();

            bool show = ShouldShow();
            DMUiToolkitOverlayDocument.SetShown(root, show);
            DMUiToolkitOverlayDocument.SetShown(dot, show);
            if (show)
            {
                UpdatePosition();
                SampleAimedPickupIfDue();
                if (dot != null)
                {
                    if (hasAimedPickup)
                        dot.AddToClassList("dmg-reticle-hot");
                    else
                        dot.RemoveFromClassList("dmg-reticle-hot");
                }
            }

            if (DMUiToolkitHud.IsDriving)
            {
                if (!uguiHidden)
                {
                    HideUgui();
                    uguiHidden = true;
                }
            }
            else
            {
                uguiHidden = false;
            }
        }

        internal void BindTree()
        {
            if (document == null)
                document = GetComponent<UIDocument>();
            if (document == null)
                return;

            VisualElement tree = document.rootVisualElement;
            if (tree == null)
                return;

            root = tree.Q<VisualElement>("reticle-root") ?? tree;
            dot = tree.Q<VisualElement>("reticle-dot");
            DMUiToolkitOverlayDocument.ApplyIgnorePicking(root);
            DMUiToolkitOverlayDocument.ApplyIgnorePicking(dot);
            bound = root != null;
        }

        private bool ShouldShow()
        {
            if (!DMUiToolkitOverlayDocument.GameplayHudWanted())
                return false;
            if (GameplayHudVisibility.CinematicChromeHidden)
                return false;
            if (!GameSession.HasStarted)
                return false;
            if (PlayerVehicleState.IsMounted)
                return false;

            PlayerController player = ResolvePlayer();
            if (player != null && player.BlocksCombatInput)
                return false;

            SurvivalStats survivalStats = ResolveSurvival();
            if (survivalStats != null && survivalStats.IsDead)
                return false;

            return true;
        }

        private void UpdatePosition()
        {
            if (dot == null || !ResolveReferences(out Camera camera, out _))
                return;

            Transform playerTransform = ResolvePlayerTransform();
            if (playerTransform == null || camera == null)
                return;

            Ray viewRay = WorldUseController.BuildScreenCenterRay(camera, playerTransform);
            Vector3 world = viewRay.origin + viewRay.direction * 1f;
            Vector3 screen = camera.WorldToScreenPoint(world);
            if (screen.z <= 0f || dot.panel == null)
                return;

            Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(dot.panel, new Vector2(screen.x, screen.y));
            dot.style.left = panelPos.x - 4f;
            dot.style.top = panelPos.y - 4f;
            dot.style.marginLeft = 0;
            dot.style.marginTop = 0;
        }

        private void SampleAimedPickupIfDue()
        {
            if (Time.unscaledTime < nextAimSampleTime)
                return;

            nextAimSampleTime = Time.unscaledTime + AimSampleInterval;
            hasAimedPickup = HasAimedPickup();
        }

        private bool HasAimedPickup()
        {
            if (!ResolveReferences(out Camera camera, out ResourceGatherer resourceGatherer))
                return false;

            Transform playerTransform = ResolvePlayerTransform();
            if (playerTransform == null)
                return false;

            Ray viewRay = WorldUseController.BuildScreenCenterRay(camera, playerTransform);
            return WorldUseController.TryGetAimedItemPickup(
                viewRay,
                resourceGatherer,
                FallbackPickupRange,
                out _,
                playerTransform.position);
        }

        private PlayerController ResolvePlayer()
        {
            if (cachedPlayer != null)
                return cachedPlayer;
            cachedPlayer = PlayerLocator.FindPlayerController();
            return cachedPlayer;
        }

        private SurvivalStats ResolveSurvival()
        {
            if (cachedSurvival != null)
                return cachedSurvival;
            PlayerController player = ResolvePlayer();
            if (player != null)
                cachedSurvival = player.GetComponent<SurvivalStats>();
            return cachedSurvival;
        }

        private Transform ResolvePlayerTransform()
        {
            if (useController != null)
                return useController.transform;
            Transform cached = PlayerReference.Transform;
            if (cached != null)
                return cached;
            PlayerController player = ResolvePlayer();
            return player != null ? player.transform : null;
        }

        private bool ResolveReferences(out Camera camera, out ResourceGatherer resourceGatherer)
        {
            camera = worldCamera;
            resourceGatherer = gatherer;

            if (useController == null)
            {
                PlayerController player = ResolvePlayer();
                if (player != null)
                    useController = player.GetComponent<WorldUseController>();
            }

            if (useController != null && resourceGatherer == null)
                resourceGatherer = useController.GetComponent<ResourceGatherer>();

            if (camera == null)
            {
                PlayerController player = ResolvePlayer();
                if (player != null && player.GameplayCamera != null)
                    camera = player.GameplayCamera;
                else
                    camera = PlayerReference.ResolveCamera();
            }

            worldCamera = camera;
            gatherer = resourceGatherer;
            return camera != null;
        }

        private static void HideUgui()
        {
            PickupAimReticleUI ui = Object.FindAnyObjectByType<PickupAimReticleUI>(FindObjectsInactive.Include);
            if (ui != null)
                DMUiToolkitOverlayDocument.HideGameObject(ui.gameObject);
        }
    }
}
