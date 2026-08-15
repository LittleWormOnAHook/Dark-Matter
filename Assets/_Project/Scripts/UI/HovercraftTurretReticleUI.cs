using Project.Player;
using Project.Vehicles;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Red cross + dot aligned to the hovercraft turret muzzle aim while mounted.
    /// </summary>
    public class HovercraftTurretReticleUI : MonoBehaviour
    {
        private const float DotSize = 8f;
        private const float CrossArm = 12f;
        private const float CrossGap = 4f;
        private const float CrossThickness = 2f;
        private const float AimDistance = 500f;

        private RectTransform rootRect;
        private RectTransform dotRect;
        private Image dotImage;
        private readonly Image[] crossLines = new Image[4];
        private Vector2 reticlePosition;
        private bool reticleInitialized;

        private void Awake()
        {
            BuildReticle();
        }

        private void LateUpdate()
        {
            if (rootRect == null)
                return;

            if (!ShouldShow(out HovercraftTurretController turret, out Camera camera))
            {
                rootRect.gameObject.SetActive(false);
                return;
            }

            UpdateReticlePosition(turret, camera);
            rootRect.gameObject.SetActive(true);
        }

        private bool ShouldShow(out HovercraftTurretController turret, out Camera camera)
        {
            turret = null;
            camera = null;

            if (!PlayerVehicleState.IsMounted || PlayerVehicleState.ActiveCraft == null)
                return false;

            PlayerController player = PlayerVehicleState.MountedPlayer;
            if (player == null || player.BlocksCombatInput)
                return false;

            turret = PlayerVehicleState.ActiveCraft.GetComponent<HovercraftTurretController>();
            if (turret == null || turret.Muzzle == null)
                return false;

            HovercraftCameraRig cameraRig = PlayerVehicleState.ActiveCraft.GetComponent<HovercraftCameraRig>();
            camera = cameraRig != null && cameraRig.IsActive
                ? PlayerVehicleState.ActiveCraft.GetComponentInChildren<Camera>(false)
                : player.GameplayCamera;

            if (camera == null || !camera.enabled)
                camera = Camera.main;

            return camera != null;
        }

        private void UpdateReticlePosition(HovercraftTurretController turret, Camera camera)
        {
            Canvas canvas = rootRect.GetComponentInParent<Canvas>();
            if (canvas == null)
                return;

            RectTransform canvasRect = canvas.transform as RectTransform;
            if (canvasRect == null)
                return;

            Transform muzzle = turret.Muzzle;
            Vector3 aimPoint = muzzle.position + muzzle.forward * AimDistance;
            Vector3 screenPoint = camera.WorldToScreenPoint(aimPoint);
            if (screenPoint.z <= 0f)
            {
                rootRect.gameObject.SetActive(false);
                return;
            }

            Camera canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    screenPoint,
                    canvasCamera,
                    out Vector2 target))
            {
                return;
            }

            if (!reticleInitialized)
            {
                reticlePosition = target;
                reticleInitialized = true;
            }
            else
            {
                HovercraftProfile profile = PlayerVehicleState.ActiveCraft.Profile;
                float smoothSpeed = profile != null ? profile.reticleSmoothSpeed : 8f;
                float smooth = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
                reticlePosition = Vector2.Lerp(reticlePosition, target, smooth);
            }

            rootRect.anchoredPosition = reticlePosition;
        }

        private void BuildReticle()
        {
            GameObject root = new GameObject("HovercraftTurretReticle", typeof(RectTransform));
            root.transform.SetParent(transform, false);

            rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = Vector2.zero;
            rootRect.sizeDelta = new Vector2(CrossArm * 2f + CrossGap * 2f, CrossArm * 2f + CrossGap * 2f);
            root.transform.SetAsLastSibling();

            Color reticleColor = DarkMatterGenesisUiPalette.DangerRed;

            dotRect = CreateLine(rootRect, "Dot", new Vector2(DotSize, DotSize), Vector2.zero);
            dotImage = dotRect.GetComponent<Image>();
            dotImage.sprite = ShiftUiTheme.CircleFilled ?? MapUiSprites.Dot;
            dotImage.color = reticleColor;

            crossLines[0] = CreateLine(rootRect, "CrossLeft", new Vector2(CrossArm, CrossThickness), new Vector2(-CrossGap - CrossArm * 0.5f, 0f)).GetComponent<Image>();
            crossLines[1] = CreateLine(rootRect, "CrossRight", new Vector2(CrossArm, CrossThickness), new Vector2(CrossGap + CrossArm * 0.5f, 0f)).GetComponent<Image>();
            crossLines[2] = CreateLine(rootRect, "CrossUp", new Vector2(CrossThickness, CrossArm), new Vector2(0f, CrossGap + CrossArm * 0.5f)).GetComponent<Image>();
            crossLines[3] = CreateLine(rootRect, "CrossDown", new Vector2(CrossThickness, CrossArm), new Vector2(0f, -CrossGap - CrossArm * 0.5f)).GetComponent<Image>();

            for (int i = 0; i < crossLines.Length; i++)
            {
                MenuUiBuilder.ApplyUiSprite(crossLines[i]);
                crossLines[i].color = reticleColor;
                crossLines[i].raycastTarget = false;
            }
        }

        private static RectTransform CreateLine(RectTransform parent, string name, Vector2 size, Vector2 anchoredPosition)
        {
            GameObject line = new GameObject(name, typeof(RectTransform), typeof(Image));
            line.transform.SetParent(parent, false);

            RectTransform rect = line.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            Image image = line.GetComponent<Image>();
            image.raycastTarget = false;
            return rect;
        }
    }
}
