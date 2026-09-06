using Project.Core;
using Project.Interaction;
using Project.Player;
using Project.Survival;
using Project.Vehicles;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Reticle ~1m ahead; full horizontal camera follow, 25% vertical arc, biased forward off the player.
    /// </summary>
    public class PickupAimReticleUI : MonoBehaviour
    {
        private const float DotSize = 7f;
        private const float FallbackPickupRange = 4f;
        private const float ReticleSmoothSpeed = 22f;
        private const float AimSampleInterval = 0.1f;

        private static readonly Color IdleColor = new Color(0.92f, 0.96f, 1f, 0.95f);
        private static readonly Color TargetColor = new Color(0.45f, 1f, 0.75f, 0.98f);

        private RectTransform dotRect;
        private Image dotImage;
        private Canvas cachedCanvas;
        private WorldUseController useController;
        private ResourceGatherer gatherer;
        private Camera worldCamera;
        private PlayerController cachedPlayer;
        private SurvivalStats cachedSurvival;
        private Vector2 reticlePosition;
        private bool reticleInitialized;
        private float nextAimSampleTime;
        private bool hasAimedPickup;
        private bool uitkHidden;

        private void Awake()
        {
            BuildReticle();
        }

        private void LateUpdate()
        {
            if (DMUiToolkitHud.IsDriving)
            {
                if (!uitkHidden)
                {
                    if (dotRect != null)
                        dotRect.gameObject.SetActive(false);
                    uitkHidden = true;
                }
                return;
            }

            uitkHidden = false;

            if (dotRect == null || dotImage == null)
                return;

            if (!ShouldShow())
            {
                dotRect.gameObject.SetActive(false);
                return;
            }

            UpdateReticlePosition();
            SampleAimedPickupIfDue();
            dotRect.gameObject.SetActive(true);
            dotImage.color = hasAimedPickup ? TargetColor : IdleColor;
        }

        private void UpdateReticlePosition()
        {
            if (cachedCanvas == null)
                cachedCanvas = dotRect.GetComponentInParent<Canvas>();
            if (cachedCanvas == null)
                return;

            RectTransform canvasRect = cachedCanvas.transform as RectTransform;
            if (canvasRect == null)
                return;

            if (!ResolveReferences(out Camera camera, out _))
                return;

            Transform playerTransform = ResolvePlayerTransform();
            Vector2 target = WorldUseController.GetReticleCanvasOffset(camera, playerTransform, canvasRect);
            if (!reticleInitialized)
            {
                reticlePosition = target;
                reticleInitialized = true;
            }
            else
            {
                float smooth = 1f - Mathf.Exp(-ReticleSmoothSpeed * Time.deltaTime);
                reticlePosition.x = Mathf.Lerp(reticlePosition.x, target.x, smooth);
                reticlePosition.y = Mathf.Lerp(reticlePosition.y, target.y, smooth);
            }

            dotRect.anchoredPosition = reticlePosition;
        }

        private void SampleAimedPickupIfDue()
        {
            if (Time.unscaledTime < nextAimSampleTime)
                return;

            nextAimSampleTime = Time.unscaledTime + AimSampleInterval;
            hasAimedPickup = HasAimedPickup();
        }

        private bool ShouldShow()
        {
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

        private void BuildReticle()
        {
            GameObject root = new GameObject("PickupAimReticle", typeof(RectTransform));
            root.transform.SetParent(transform, false);

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = Vector2.zero;
            rootRect.sizeDelta = new Vector2(DotSize, DotSize);
            root.transform.SetAsLastSibling();

            dotRect = rootRect;

            dotImage = root.AddComponent<Image>();
            dotImage.sprite = ShiftUiTheme.CircleFilled ?? MapUiSprites.Dot;
            dotImage.color = IdleColor;
            dotImage.raycastTarget = false;
            dotImage.preserveAspect = true;
        }
    }
}
