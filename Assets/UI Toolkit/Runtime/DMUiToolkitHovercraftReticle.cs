using Project.Player;
using Project.Vehicles;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// UITK hovercraft turret reticle. Same aim data as HovercraftTurretReticleUI.
    /// Sibling document, same Panel Settings as HUD.
    /// </summary>
    [DefaultExecutionOrder(-376)]
    [DisallowMultipleComponent]
    public sealed class DMUiToolkitHovercraftReticle : MonoBehaviour
    {
        public const string HostName = "UITK_HovercraftReticle";
        public const int SortingOrder = 14;

        private static DMUiToolkitHovercraftReticle instance;

        private UIDocument document;
        private VisualElement root;
        private VisualElement reticle;
        private bool built;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
        }


        public static DMUiToolkitHovercraftReticle EnsureHost()
        {
            if (instance != null)
                return instance;

            UIDocument doc = DMUiToolkitOverlayDocument.Ensure(HostName, null, null, SortingOrder);
            if (doc == null)
                return null;

            instance = doc.GetComponent<DMUiToolkitHovercraftReticle>();
            if (instance == null)
                instance = doc.gameObject.AddComponent<DMUiToolkitHovercraftReticle>();

            instance.document = doc;
            instance.EnsureBuilt();
            return instance;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this);
                return;
            }

            instance = this;
            document = GetComponent<UIDocument>();
            EnsureBuilt();
        }

        private void OnEnable()
        {
            instance = this;
            EnsureBuilt();
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        private bool uguiReticleHidden;

        private void LateUpdate()
        {
            if (!built)
            {
                EnsureBuilt();
                if (!built)
                    return;
            }

            bool driving = DMUiToolkitOverlayDocument.GameplayHudWanted();

            if (!driving)
            {
                if (uguiReticleHidden)
                {
                    RestoreUguiIfNeeded();
                    uguiReticleHidden = false;
                }
            }
            else if (!uguiReticleHidden)
            {
                HideUgui(true);
                uguiReticleHidden = true;
            }

            if (!driving)
            {
                DMUiToolkitOverlayDocument.SetShown(reticle, false);
                return;
            }

            if (!TryAim(out Vector2 panelPos))
            {
                DMUiToolkitOverlayDocument.SetShown(reticle, false);
                return;
            }

            reticle.style.left = panelPos.x;
            reticle.style.top = panelPos.y;
            reticle.style.marginLeft = -20f;
            reticle.style.marginTop = -20f;
            DMUiToolkitOverlayDocument.SetShown(reticle, true);
        }

        private void EnsureBuilt()
        {
            if (built && reticle != null)
                return;

            if (document == null)
                document = GetComponent<UIDocument>();
            if (document == null)
                return;

            root = document.rootVisualElement;
            if (root == null)
                return;

            reticle = root.Q<VisualElement>("hovercraft-reticle");
            if (reticle == null)
            {
                reticle = new VisualElement { name = "hovercraft-reticle" };
                reticle.pickingMode = PickingMode.Ignore;
                reticle.style.position = Position.Absolute;
                reticle.style.width = 40f;
                reticle.style.height = 40f;
                reticle.Add(MakeArm("h", 40f, 2f, 0f, 19f));
                reticle.Add(MakeArm("v", 2f, 40f, 19f, 0f));
                VisualElement dot = new VisualElement { name = "dot" };
                dot.pickingMode = PickingMode.Ignore;
                dot.style.position = Position.Absolute;
                dot.style.left = 16f;
                dot.style.top = 16f;
                dot.style.width = 8f;
                dot.style.height = 8f;
                dot.style.backgroundColor = DarkMatterGenesisUiPalette.DangerRed;
                reticle.Add(dot);
                root.Add(reticle);
            }

            DMUiToolkitOverlayDocument.SetShown(reticle, false);
            built = true;
        }

        private static VisualElement MakeArm(string name, float w, float h, float left, float top)
        {
            VisualElement arm = new VisualElement { name = name };
            arm.pickingMode = PickingMode.Ignore;
            arm.style.position = Position.Absolute;
            arm.style.left = left;
            arm.style.top = top;
            arm.style.width = w;
            arm.style.height = h;
            arm.style.backgroundColor = DarkMatterGenesisUiPalette.DangerRed;
            return arm;
        }

        private bool TryAim(out Vector2 panelPos)
        {
            panelPos = Vector2.zero;
            if (!PlayerVehicleState.IsMounted || PlayerVehicleState.ActiveCraft == null)
                return false;

            PlayerController player = PlayerVehicleState.MountedPlayer;
            if (player == null || player.BlocksCombatInput)
                return false;

            HovercraftTurretController turret = PlayerVehicleState.ActiveCraft.GetComponent<HovercraftTurretController>();
            if (turret == null || turret.Muzzle == null)
                return false;

            HovercraftCameraRig cameraRig = PlayerVehicleState.ActiveCraft.GetComponent<HovercraftCameraRig>();
            Camera camera = cameraRig != null && cameraRig.IsActive
                ? PlayerVehicleState.ActiveCraft.GetComponentInChildren<Camera>(false)
                : player.GameplayCamera;
            if (camera == null || !camera.enabled)
                camera = Camera.main;
            if (camera == null || reticle == null || reticle.panel == null)
                return false;

            Vector3 screenPoint = camera.WorldToScreenPoint(turret.Muzzle.position + turret.Muzzle.forward * 500f);
            if (screenPoint.z <= 0f)
                return false;

            panelPos = RuntimePanelUtils.ScreenToPanel(reticle.panel, screenPoint);
            return true;
        }

        private static void HideUgui(bool uitkDriving)
        {
            HovercraftTurretReticleUI ugui = FindAnyObjectByType<HovercraftTurretReticleUI>(FindObjectsInactive.Include);
            if (ugui == null)
                return;

            if (uitkDriving)
            {
                ugui.enabled = false;
                Transform child = ugui.transform.Find("HovercraftTurretReticle");
                if (child != null && child.gameObject.activeSelf)
                    child.gameObject.SetActive(false);
            }
        }

        private static void RestoreUguiIfNeeded()
        {
            HovercraftTurretReticleUI ugui = FindAnyObjectByType<HovercraftTurretReticleUI>(FindObjectsInactive.Include);
            if (ugui != null && !ugui.enabled)
                ugui.enabled = true;
        }
    }
}
