using System.Collections.Generic;
using Project.Combat;
using Project.Core;
using Project.Data;
using Project.Inventory;
using Project.Player;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// World-space mag readout. Follows Ammo Count / DM Jetpack Sub with editable local offset/euler/width.
    /// Edit Mode: select host, move/rotate/scale to bake placement; live mag number stays visible.
    /// Never parented under Player_v7 / Rigidbody. Display-only (no collider, no panel input).
    /// </summary>
    [ExecuteAlways]
    [DefaultExecutionOrder(-340)]
    [DisallowMultipleComponent]
    public class DMWorldAmmoHud : MonoBehaviour
    {
        public static bool Enabled { get; private set; } = false; // Temporarily retired — revisit ammo readout later.
        public const string HostName = "DMWorldAmmoHud";
        public const string UxmlPath = "Assets/UI Toolkit/Screens/WorldAmmo.uxml";
        public const string UssPath = "Assets/UI Toolkit/Screens/WorldAmmo.uss";
        public const string PanelSettingsPath = "Assets/UI Toolkit/WorldAmmoPanelSettings.asset";
        public const string LogStamp = "DMWorldAmmoHud 0904-ammo-visible";

        private static readonly Color ColorGunpowder = new Color(0.42f, 0.78f, 0.48f, 1f); // PositiveGreen / #6BC77A
        private static readonly Color ColorPlasma = new Color(0.55f, 0.28f, 0.98f, 1f);
        private static readonly Color ColorLaser = new Color(0.922f, 0.180f, 0.141f, 1f); // #EB2E24

        private static readonly Dictionary<AmmoType, Color> AmmoColors = new Dictionary<AmmoType, Color>
        {
            { AmmoType.Gunpowder, ColorGunpowder },
            { AmmoType.Plasma, ColorPlasma },
            { AmmoType.Laser, ColorLaser },
        };

        private const float PanelWidthPx = 180f;
        private const float PanelHeightPx = 80f;
        private const float PixelsPerUnit = 100f;
        private const float DefaultWorldWidth = 0.12f;
        private const float MaxWorldWidth = 0.25f;
        private static readonly Vector3 JetpackLocalOffset = new Vector3(0f, 0.22f, -0.20f);

        [Header("Placement")]
        [Tooltip("Local position relative to the Ammo Count plane / jetpack anchor.")]
        [SerializeField] private Vector3 localOffset = new Vector3(0f, 0f, 0.012f);
        [Tooltip("Local euler relative to the anchor. Default Y=180 flips a back-facing plane.")]
        [SerializeField] private Vector3 localEulerOffset = new Vector3(0f, 180f, 0f);
        [Tooltip("World-space width in meters. Drag scale in the Scene view while selected (Edit Mode) to change.")]
        [SerializeField, Range(0.02f, 0.25f)] private float worldWidth = DefaultWorldWidth;
        [SerializeField] private bool followAnchor = true;
        [Tooltip("In Edit Mode, moving/rotating/scaling the selected host bakes into the fields above.")]
        [SerializeField] private bool bakeWhileSelected = true;

        [Header("Glow")]
        [Tooltip("HDRP Unlit emissive multiplier. Raise this to make the mag number bloom.")]
        [SerializeField, Min(0f)] private float emissionIntensity = 6f;

        private static DMWorldAmmoHud instance;
        private static PanelSettings runtimePanelSettings;
        private static bool usesFixedWorldSize;

        private UIDocument document;
        private Label ammoLabel;
        private VisualElement ammoRoot;
        private bool bound;
        private bool lastShown;
        private string lastText;
        private Color lastColor;
        private bool lastColorValid;
        private GameObject fallbackQuad;
        private MeshRenderer fallbackRenderer;
        private Material fallbackMaterial;
        private RenderTexture fallbackRt;
        private bool usingFallback;
        private int fallbackCheckFrame;
        private bool fallbackPendingAfterLoad;
        private bool interactionStrippedClean;
        private int nextStripFrame;
        private int nextHideMeshesFrame;
        private TextMesh textMeshFallback;
        private MeshRenderer textMeshRenderer;
        private int fallbackForceFrame = -1;
        private string stampedRtText;
        private Color stampedRtColor;
        private bool stampedRtColorValid;
        private Font textMeshFont;

        private Transform anchor;
        private bool anchorIsNamedPlane;
        private bool softBillboard;
        private Transform playerRoot;
        private EquipmentController equipment;
        private WeaponAmmoState ammoState;
        private PlayerController player;
        private Camera billboardCamera;
        private int nextRebindFrame;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            runtimePanelSettings = null;
            usesFixedWorldSize = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!Enabled)
            {
                if (Application.isPlaying)
                {
                    DestroyAllHosts();
                    StripAmmoCountCollidersOnPlayer();
                }
                return;
            }
            if (!Application.isPlaying)
                return;
            if (!DMUiToolkitConfig.IsEnabled)
                return;
            EnsureHost();
        }


        private static void DestroyAllHosts()
        {
            instance = null;
            DMWorldAmmoHud[] existing = Object.FindObjectsByType<DMWorldAmmoHud>(FindObjectsInactive.Include);
            for (int i = 0; i < existing.Length; i++)
            {
                if (existing[i] == null)
                    continue;
                if (Application.isPlaying)
                    Object.Destroy(existing[i].gameObject);
                else
                    Object.DestroyImmediate(existing[i].gameObject);
            }

            // Orphan quads / stamps from earlier ammo experiments (can sit under Player and block draws).
            DestroyNamedOrphans("AmmoReadoutQuad", "DMWorldAmmoHud_Stamp", "DMWorldAmmoHud_RT", "AmmoHudTarget");
        }

        private static void DestroyNamedOrphans(params string[] names)
        {
            if (names == null || names.Length == 0)
                return;

            Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform tr = transforms[i];
                if (tr == null)
                    continue;
                string n = tr.name;
                bool match = false;
                for (int j = 0; j < names.Length; j++)
                {
                    if (n == names[j])
                    {
                        match = true;
                        break;
                    }
                }
                if (!match)
                    continue;

                if (Application.isPlaying)
                    Object.Destroy(tr.gameObject);
                else
                    Object.DestroyImmediate(tr.gameObject);
            }
        }


        private static void StripAmmoCountCollidersOnPlayer()
        {
            // Ammo Count mesh on the jetpack must never ray-block weapon draws/traces.
            Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform tr = transforms[i];
                if (tr == null)
                    continue;
                string n = tr.name;
                if (n != "Ammo Count" && n != "Ammo_Count" && n != "AmmoCount")
                    continue;

                Collider[] cols = tr.GetComponentsInChildren<Collider>(true);
                for (int c = 0; c < cols.Length; c++)
                {
                    if (cols[c] != null)
                        cols[c].enabled = false;
                }
            }
        }

        public static DMWorldAmmoHud EnsureHost()
        {
            if (!Enabled)
                return null;
            if (!Application.isPlaying)
                return null;

            if (instance != null)
                return instance;

            DMWorldAmmoHud[] existing = FindObjectsByType<DMWorldAmmoHud>(FindObjectsInactive.Include);
            for (int i = 0; i < existing.Length; i++)
            {
                if (existing[i] != null)
                {
                    instance = existing[i];
                    instance.EnsureDocument();
                    return instance;
                }
            }

            GameObject host = new GameObject(HostName);
            // Scene root - NEVER under Player_v7 / Rigidbody hierarchy.
            host.transform.SetParent(null, false);
            instance = host.AddComponent<DMWorldAmmoHud>();
            instance.EnsureDocument();
            return instance;
        }

        private void Awake()
        {
            if (!Enabled)
            {
                if (Application.isPlaying)
                    Destroy(gameObject);
                return;
            }
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            EnsureDocument();
        }

        private void OnEnable()
        {
            if (!Enabled)
            {
                if (Application.isPlaying)
                    Destroy(gameObject);
                return;
            }
            instance = this;
            EnsureDocument();
            BindVisuals();
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;

            if (fallbackRt != null)
            {
                fallbackRt.Release();
                Object.Destroy(fallbackRt);
                fallbackRt = null;
            }
            if (fallbackMaterial != null)
            {
                Object.Destroy(fallbackMaterial);
                fallbackMaterial = null;
            }
        }

        private void LateUpdate()
        {
            if (!Enabled)
            {
                if (Application.isPlaying)
                    Destroy(gameObject);
                return;
            }
            TickAmmoHud();
        }

#if UNITY_EDITOR
        private double _nextEditTick;

        private void OnRenderObject()
        {
            // Keep Edit Mode preview alive when the Scene view repaints without Play.
            if (Application.isPlaying)
                return;
            double now = UnityEditor.EditorApplication.timeSinceStartup;
            if (now < _nextEditTick)
                return;
            _nextEditTick = now + 0.05; // ~20 Hz
            TickAmmoHud();
        }
#endif

        private void TickAmmoHud()
        {
            if (!bound)
                BindVisuals();

            if (transform.parent != null)
                transform.SetParent(null, true);

            // Strip colliders / panel input only when dirty or periodically - not every frame.
            if (Application.isPlaying && (!interactionStrippedClean || Time.frameCount >= nextStripFrame))
            {
                StripInteractionComponents();
                nextStripFrame = Time.frameCount + 32;
            }

            // Only scan for duplicate quads when we have children / every ~32 frames.
            if ((Time.frameCount & 31) == 0 && transform.childCount > 1)
                CullDuplicateReadoutQuads();

            ResolveAnchorIfNeeded();

            bool authoring = false;
#if UNITY_EDITOR
            if (!Application.isPlaying && bakeWhileSelected
                && UnityEditor.Selection.activeGameObject == gameObject)
            {
                authoring = true;
            }
#endif
            if (authoring && anchor != null)
            {
                BakePlacementFromTransform();
            }
            else if (followAnchor)
            {
                FollowAnchor();
            }

            PullAmmoReadout();
            EnsureVisiblePath();
        }

        private void BakePlacementFromTransform()
        {
            if (anchor == null)
                return;

            localOffset = anchor.InverseTransformPoint(transform.position);
            localEulerOffset = (Quaternion.Inverse(anchor.rotation) * transform.rotation).eulerAngles;

            float s = transform.localScale.x;
            if (Mathf.Abs(s - 1f) > 0.0005f)
            {
                worldWidth = Mathf.Clamp(worldWidth * s, 0.02f, MaxWorldWidth);
                transform.localScale = Vector3.one;
                ApplyWorldSize();
            }
        }

        private void EnsureDocument()
        {
            if (document == null)
                document = GetComponent<UIDocument>();
            if (document == null)
                document = gameObject.AddComponent<UIDocument>();

            // Hard rule: never stay under Player_v7 / its Rigidbody hierarchy.
            if (transform.parent != null)
                transform.SetParent(null, true);

            StripInteractionComponents();

            PanelSettings settings = ResolveWorldPanelSettings();
            if (settings != null && document.panelSettings != settings)
                document.panelSettings = settings;

            VisualTreeAsset tree = DMUiToolkitBootstrap.LoadUxml(UxmlPath);
            if (tree != null && document.visualTreeAsset != tree)
                document.visualTreeAsset = tree;

            ApplyWorldSize();
        }

        private void ApplyWorldSize()
        {
            // Persist authored size; only floor invalid values — never stomp with Default when authored.
            if (worldWidth < 0.02f)
                worldWidth = DefaultWorldWidth;
            worldWidth = Mathf.Clamp(worldWidth, 0.02f, MaxWorldWidth);

            // Host stays unscaled so Fixed size / fallback quad both use the same worldWidth (no double-scale).
            transform.localScale = Vector3.one;

            if (usingFallback)
            {
                // RT -> small emissive quad only; do not drive a giant UITK world-space panel.
                usesFixedWorldSize = true;
                ApplyFallbackQuadScale();
                HideNonFallbackWorldMeshes();
                return;
            }

            TryConfigureWorldSpaceSize(document);
            if (!usesFixedWorldSize)
            {
                float scale = worldWidth / (PanelWidthPx / PixelsPerUnit);
                transform.localScale = new Vector3(scale, scale, scale);
            }

            ApplyFallbackQuadScale();
        }

        private void ApplyFallbackQuadScale()
        {
            if (fallbackQuad == null)
                return;
            fallbackQuad.transform.localScale = new Vector3(
                worldWidth,
                worldWidth * (PanelHeightPx / PanelWidthPx),
                1f);
        }

        private void HideNonFallbackWorldMeshes()
        {
            MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                MeshRenderer mr = renderers[i];
                if (mr == null)
                    continue;
                if (fallbackRenderer != null && mr == fallbackRenderer)
                    continue;
                if (fallbackQuad != null && (mr.gameObject == fallbackQuad || mr.transform.IsChildOf(fallbackQuad.transform)))
                    continue;
                if (textMeshRenderer != null && mr == textMeshRenderer)
                    continue;
                if (textMeshFallback != null && mr.gameObject == textMeshFallback.gameObject)
                    continue;
                mr.enabled = false;
            }
        }

        private void TryConfigureWorldSpaceSize(UIDocument doc)
        {
            usesFixedWorldSize = false;
            if (doc == null)
                return;

            // Unity 6.2+ world-space sizing. Prefer Fixed world size matching pack readout.
            try
            {
                doc.worldSpaceSizeMode = UIDocument.WorldSpaceSizeMode.Fixed;
                doc.worldSpaceSize = new Vector2(worldWidth, worldWidth * (PanelHeightPx / PanelWidthPx));
                usesFixedWorldSize = true;
            }
            catch (System.Exception)
            {
                // Older API shape - transform scale fallback applies in ApplyWorldSize.
                usesFixedWorldSize = false;
            }
        }

        private void StripInteractionComponents()
        {
            try
            {
            // Display only - strip colliders / panel input that block weapon raycasts.
            bool foundDirty = false;
            Collider[] cols = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < cols.Length; i++)
            {
                if (cols[i] == null)
                    continue;
                foundDirty = true;
                cols[i].enabled = false;
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(cols[i]);
                else
#endif
                    Destroy(cols[i]);
            }

            Component[] behaviours = GetComponentsInChildren<Component>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                Component c = behaviours[i];
                if (c == null)
                    continue;
                string typeName = c.GetType().Name;
                if (typeName == "PanelInputConfiguration"
                    || typeName == "PanelEventHandler"
                    || typeName == "PanelRaycaster"
                    || typeName == "PhysicsRaycaster"
                    || typeName == "Physics2DRaycaster")
                {
                    foundDirty = true;
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                        DestroyImmediate(c);
                    else
#endif
                        Destroy(c);
                }
            }

            if (fallbackQuad != null)
            {
                int ignoreRaycast = LayerMask.NameToLayer("Ignore Raycast");
                if (ignoreRaycast >= 0)
                    fallbackQuad.layer = ignoreRaycast;
            }

            // Stay clean until a new primitive / rebind dirties us again.
            interactionStrippedClean = !foundDirty;
            }
            catch (System.Exception)
            {
                // Ignore transient destroy/order issues during domain reload / Play enter.
                interactionStrippedClean = false;
            }
        }

        private static PanelSettings ResolveWorldPanelSettings()
        {
            if (runtimePanelSettings != null)
            {
                ConfigureWorldPanel(runtimePanelSettings);
                return runtimePanelSettings;
            }

            PanelSettings loaded = DMUiToolkitBootstrap.LoadAsset<PanelSettings>(PanelSettingsPath);
            if (loaded != null)
            {
                // Instantiate so we never mutate the shared overlay PanelSettings.asset.
                runtimePanelSettings = Instantiate(loaded);
                runtimePanelSettings.name = "WorldAmmoPanelSettings_Runtime";
            }
            else
            {
                runtimePanelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                runtimePanelSettings.name = "WorldAmmoPanelSettings_RuntimeCreated";
            }

            runtimePanelSettings.hideFlags = HideFlags.HideAndDontSave;
            ConfigureWorldPanel(runtimePanelSettings);
            return runtimePanelSettings;
        }

        private static void ConfigureWorldPanel(PanelSettings settings)
        {
            if (settings == null)
                return;

            // RT fallback must never use Overlay without a targetTexture — that paints a full-screen black panel.
            bool keepFallbackOverlay = instance != null && instance.usingFallback && instance.fallbackRt != null;
            if (keepFallbackOverlay)
            {
                settings.targetTexture = instance.fallbackRt;
                settings.renderMode = PanelRenderMode.ScreenSpaceOverlay;
            }
            else
            {
                settings.renderMode = PanelRenderMode.WorldSpace;
                settings.targetTexture = null;
            }

            settings.clearColor = true;
            settings.colorClearValue = Color.clear;
            settings.sortingOrder = keepFallbackOverlay ? -100 : 200;
            settings.forceGammaRendering = true;
            settings.scaleMode = PanelScaleMode.ConstantPixelSize;

            if (settings.themeStyleSheet == null)
            {
                ThemeStyleSheet theme = DMUiToolkitBootstrap.LoadAsset<ThemeStyleSheet>(DMUiToolkitBootstrap.DefaultThemePath);
                if (theme != null)
                    settings.themeStyleSheet = theme;
            }
        }

        private void BindVisuals()
        {
            if (document == null)
                EnsureDocument();
            if (document == null)
                return;

            VisualElement root = document.rootVisualElement;
            if (root == null)
                return;

            root.pickingMode = PickingMode.Ignore;
            root.style.backgroundColor = Color.clear;
            IgnorePickingRecursive(root);

            ammoRoot = root.Q<VisualElement>("world-ammo-root") ?? root;
            ammoRoot.pickingMode = PickingMode.Ignore;

            ammoLabel = root.Q<Label>("ammo-count");
            if (ammoLabel == null)
            {
                ammoLabel = new Label("0") { name = "ammo-count" };
                ammoLabel.AddToClassList("dmg-world-ammo-count");
                ammoRoot.Add(ammoLabel);
            }

            ammoLabel.pickingMode = PickingMode.Ignore;
            ForceTransparentPanel(root);
            ForceTransparentPanel(ammoRoot);
            ForceTransparentPanel(ammoLabel);
            DMUiToolkitBootstrap.ApplyTheme(document, UssPath);
            // Theme may reintroduce a panel fill — force transparent black panel away.
            ForceTransparentPanel(root);
            ForceTransparentPanel(ammoRoot);
            ForceTransparentPanel(ammoLabel);

            if (!lastShown)
                SetShown(false);

            bound = true;
        }

        private static void ForceTransparentPanel(VisualElement element)
        {
            if (element == null)
                return;
            element.pickingMode = PickingMode.Ignore;
            element.style.backgroundColor = Color.clear;
            element.style.borderTopWidth = 0;
            element.style.borderBottomWidth = 0;
            element.style.borderLeftWidth = 0;
            element.style.borderRightWidth = 0;
            element.style.borderTopColor = Color.clear;
            element.style.borderBottomColor = Color.clear;
            element.style.borderLeftColor = Color.clear;
            element.style.borderRightColor = Color.clear;
        }

        private static void IgnorePickingRecursive(VisualElement element)
        {
            if (element == null)
                return;
            element.pickingMode = PickingMode.Ignore;
            int count = element.childCount;
            for (int i = 0; i < count; i++)
                IgnorePickingRecursive(element[i]);
        }

        private void ResolveAnchorIfNeeded()
        {
            if (anchor != null && Time.frameCount < nextRebindFrame)
                return;

            nextRebindFrame = Time.frameCount + 30;
            ResolveAnchor();
        }

        private void ResolveAnchor()
        {
            playerRoot = FindPlayerRoot();
            if (playerRoot == null)
            {
                anchor = null;
                anchorIsNamedPlane = false;
                softBillboard = true;
                return;
            }

            Transform ammoPlane = FindNamedChild(playerRoot, "Ammo Count", "Ammo_Count", "AmmoCount");
            if (ammoPlane != null)
            {
                anchor = ammoPlane;
                anchorIsNamedPlane = true;
                softBillboard = false;
                return;
            }

            Transform jetpack = FindNamedChild(playerRoot, "DM Jetpack Sub");
            if (jetpack != null)
            {
                anchor = jetpack;
                anchorIsNamedPlane = false;
                softBillboard = false;
                return;
            }

            anchor = playerRoot;
            anchorIsNamedPlane = false;
            softBillboard = true;
        }

        private static Transform FindPlayerRoot()
        {
            PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsInactive.Include);
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i] != null)
                    return players[i].transform;
            }

            Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform t = transforms[i];
                if (t == null)
                    continue;
                if (t.name == "Player_v7" || t.name == "Player")
                    return t;
            }

            return null;
        }

        private static Transform FindNamedChild(Transform root, params string[] names)
        {
            if (root == null || names == null || names.Length == 0)
                return null;

            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int n = 0; n < names.Length; n++)
            {
                string want = names[n];
                if (string.IsNullOrEmpty(want))
                    continue;

                for (int i = 0; i < all.Length; i++)
                {
                    Transform t = all[i];
                    if (t != null && t.name == want)
                        return t;
                }
            }

            return null;
        }

        private void FollowAnchor()
        {
            if (anchor == null)
                return;

            Vector3 offset = localOffset;
            if (!anchorIsNamedPlane && offset.sqrMagnitude < 0.0000001f)
                offset = JetpackLocalOffset;

            Vector3 pos;
            Quaternion rot = anchor.rotation * Quaternion.Euler(localEulerOffset);

            if (anchorIsNamedPlane || !softBillboard)
            {
                pos = anchor.TransformPoint(offset);
            }
            else
            {
                pos = anchor.position + offset;
                rot = SoftBillboardRotation(pos, rot);
            }

            transform.SetPositionAndRotation(pos, rot);
            // Keep authored worldWidth driving the quad; never accumulate host scale under Fixed/fallback.
            if (usingFallback || !usesFixedWorldSize)
                ApplyWorldSize();
        }

        private Quaternion SoftBillboardRotation(Vector3 worldPos, Quaternion fallback)
        {
            if (billboardCamera == null)
                billboardCamera = Camera.main;
            if (billboardCamera == null)
                return fallback;

            Vector3 toCam = billboardCamera.transform.position - worldPos;
            if (toCam.sqrMagnitude < 0.0001f)
                return fallback;

            // Keep upright-ish while facing camera.
            toCam.y = 0f;
            if (toCam.sqrMagnitude < 0.0001f)
                return fallback;

            return Quaternion.LookRotation(-toCam.normalized, Vector3.up);
        }

        private void PullAmmoReadout()
        {
            if (ammoLabel == null)
                return;

            if (equipment == null || !Application.isPlaying || (Time.frameCount & 31) == 0)
                CacheCombatRefs();

            bool show = false;
            int loaded = 0;
            AmmoType ammoType = AmmoType.Gunpowder;

            if (equipment != null)
            {
                if (player == null || !player.BlocksCombatInput)
                {
                    show = equipment.HasActiveRangedWeapon();
                    if (show)
                    {
                        ItemData weapon = equipment.DrawnWeaponItem;
                        show = weapon != null && weapon.IsRangedWeapon;
                    }
                }

                if (ammoState != null && (show || !Application.isPlaying))
                {
                    loaded = ammoState.GetActiveLoadedAmmo();
                    ammoType = ammoState.GetLoadedAmmoType(equipment.ActiveWeaponHotbarSlot);
                }
            }

            // Edit Mode: always show a live (or last) number so placement is visible.
            if (!Application.isPlaying)
            {
                show = true;
                if (ammoState == null)
                    loaded = 0;
            }

            if (Application.isPlaying && GameplayHudVisibility.CinematicChromeHidden)
                show = false;

            if (!show)
            {
                if (lastShown)
                {
                    lastShown = false;
                    lastText = null;
                    SetShown(false);
                }
                return;
            }

            if (!lastShown)
            {
                lastShown = true;
                SetShown(true);
            }

            string text = loaded.ToString();
            if (!string.Equals(text, lastText, System.StringComparison.Ordinal))
            {
                lastText = text;
                ammoLabel.text = text;
                stampedRtText = null; // force RT restamp
            }

            Color color = ResolveAmmoColor(ammoType);
            if (!lastColorValid || lastColor != color)
            {
                lastColor = color;
                lastColorValid = true;
                stampedRtColorValid = false; // force RT restamp
                ammoLabel.style.color = color;
                // Soft glow via text shadow tinted to ammo type.
                ammoLabel.style.textShadow = new TextShadow
                {
                    offset = Vector2.zero,
                    blurRadius = 12f,
                    color = new Color(color.r, color.g, color.b, 0.85f)
                };
            }
        }

        private void CacheCombatRefs()
        {
            if (equipment == null)
                equipment = FindAnyObjectByType<EquipmentController>();
            if (equipment != null)
            {
                if (ammoState == null)
                    ammoState = equipment.GetComponent<WeaponAmmoState>();
                if (player == null)
                    player = equipment.GetComponent<PlayerController>();
            }

            if (ammoState == null)
                ammoState = FindAnyObjectByType<WeaponAmmoState>();
            if (player == null)
                player = FindAnyObjectByType<PlayerController>();
        }

        public static Color ResolveAmmoColor(AmmoType type)
        {
            if (AmmoColors.TryGetValue(type, out Color color))
                return color;

            // Extensible fallback - bright warm white-ish for unknown types until mapped.
            return ColorGunpowder;
        }

        /// <summary>Register additional / override ammo-type colors at runtime.</summary>
        public static void SetAmmoTypeColor(AmmoType type, Color color)
        {
            AmmoColors[type] = color;
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Dark Matter/UI/Select Or Create World Ammo HUD", true)]
        private static bool EditorSelectOrCreateHostValidate() => Enabled;

        [UnityEditor.MenuItem("Dark Matter/UI/Select Or Create World Ammo HUD", false, 1000)]
        private static void EditorSelectOrCreateHost()
        {
            if (!Enabled)
                return;
            DMWorldAmmoHud hud = UnityEngine.Object.FindAnyObjectByType<DMWorldAmmoHud>();
            if (hud == null)
            {
                GameObject host = new GameObject(HostName);
                host.transform.SetParent(null, false);
                hud = host.AddComponent<DMWorldAmmoHud>();
                UnityEditor.Undo.RegisterCreatedObjectUndo(host, "Create World Ammo HUD");
            }

            UnityEditor.Selection.activeGameObject = hud.gameObject;
            UnityEditor.EditorGUIUtility.PingObject(hud.gameObject);
            hud.EnsureDocument();
            hud.BindVisuals();
            hud.ResolveAnchor();
            hud.FollowAnchor();
            hud.PullAmmoReadout();
            hud.EnsureVisiblePath();
            UnityEditor.SceneView.RepaintAll();
        }
#endif


        private void EnsureVisiblePath()
        {
            // Prefer RT + emissive follow-quad: WorldSpace UITK has been silently invisible in play.
            bool ready = document != null && document.rootVisualElement != null;
            bool loading = Application.isPlaying && DMUiToolkitLoadingOverlay.IsShowing;

            // Keep the UITK document alive so Overlay->RT keeps painting digits.
            if (document != null && !document.enabled)
                document.enabled = true;

            // If ActivateFallback was skipped while the load veil was up, keep retrying.
            // After a short grace, force-activate anyway so we never pend forever.
            if (!usingFallback && ready && (!Application.isPlaying || Time.frameCount >= 2))
            {
                if (loading)
                {
                    fallbackPendingAfterLoad = true;
                    if (fallbackForceFrame < 0)
                        fallbackForceFrame = Time.frameCount + 90; // ~1.5s @60
                    if (Time.frameCount >= fallbackForceFrame)
                        ActivateFallback();
                }
                else
                {
                    ActivateFallback();
                }
            }
            else if (fallbackPendingAfterLoad && !usingFallback && ready && !loading)
            {
                ActivateFallback();
            }

            if (usingFallback)
                UpdateFallbackQuad();
        }


        private void ActivateFallback()
        {
            if (usingFallback)
                return;

            // Prefer waiting out the boot veil, but do not pend forever — force after grace.
            if (Application.isPlaying && DMUiToolkitLoadingOverlay.IsShowing)
            {
                fallbackPendingAfterLoad = true;
                fallbackCheckFrame = Time.frameCount;
                if (fallbackForceFrame < 0)
                    fallbackForceFrame = Time.frameCount + 90;
                if (Time.frameCount < fallbackForceFrame)
                    return;
            }

            EnsureFallbackResources();
            if (fallbackRt == null)
            {
                fallbackPendingAfterLoad = true;
                return;
            }

            usingFallback = true;
            fallbackPendingAfterLoad = false;
            fallbackForceFrame = -1;

            if (document != null && !document.enabled)
                document.enabled = true;

            if (runtimePanelSettings != null)
            {
                // Texture first, then Overlay - Overlay with null targetTexture blacks the game view.
                runtimePanelSettings.targetTexture = fallbackRt;
                runtimePanelSettings.renderMode = PanelRenderMode.ScreenSpaceOverlay;
                runtimePanelSettings.clearColor = true;
                runtimePanelSettings.colorClearValue = new Color(0f, 0f, 0f, 0f);
                runtimePanelSettings.sortingOrder = -100; // never above loading / menus if it ever leaks to screen
                if (document != null)
                    document.panelSettings = runtimePanelSettings;
            }

            ApplyWorldSize();
            HideNonFallbackWorldMeshes();
            interactionStrippedClean = false;
            StripInteractionComponents();

            if (fallbackQuad != null)
                fallbackQuad.SetActive(true);

            // Immediately stamp a digit so the quad is never empty clear-alpha.
            SyncFallbackDigits(lastText ?? "0", lastColorValid ? lastColor : ColorGunpowder);
        }


        private void CullDuplicateReadoutQuads()
        {
            Transform kept = fallbackQuad != null ? fallbackQuad.transform : null;
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child == null || child.name != "AmmoReadoutQuad")
                    continue;
                if (kept == null)
                {
                    kept = child;
                    fallbackQuad = child.gameObject;
                    fallbackRenderer = fallbackQuad.GetComponent<MeshRenderer>();
                    continue;
                }
                if (child == kept)
                    continue;
                if (Application.isPlaying)
                    Object.Destroy(child.gameObject);
                else
                    Object.DestroyImmediate(child.gameObject);
            }
        }

        private void EnsureFallbackResources()
        {
            worldWidth = Mathf.Clamp(worldWidth < 0.02f ? DefaultWorldWidth : worldWidth, 0.02f, MaxWorldWidth);

            if (fallbackRt == null)
            {
                fallbackRt = new RenderTexture(256, 128, 0, RenderTextureFormat.ARGB32)
                {
                    name = "DMWorldAmmoHud_RT",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                fallbackRt.Create();
            }

            // Only one readout quad — prior Play sessions / re-Ensure left duplicates.
            CullDuplicateReadoutQuads();

            if (fallbackQuad == null)
            {
                interactionStrippedClean = false;
                fallbackQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                fallbackQuad.name = "AmmoReadoutQuad";
                fallbackQuad.transform.SetParent(transform, false);
                fallbackQuad.transform.localPosition = Vector3.zero;
                fallbackQuad.transform.localRotation = Quaternion.identity;
                ApplyFallbackQuadScale();

                // CreatePrimitive always adds MeshCollider — destroy immediately so weapons are not blocked.
                Collider col = fallbackQuad.GetComponent<Collider>();
                if (col != null)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                        DestroyImmediate(col);
                    else
#endif
                        Destroy(col);
                }

                int ignoreRaycast = LayerMask.NameToLayer("Ignore Raycast");
                if (ignoreRaycast >= 0)
                    fallbackQuad.layer = ignoreRaycast;

                fallbackRenderer = fallbackQuad.GetComponent<MeshRenderer>();
                // Project is Unity 6 HDRP — never URP Unlit.
                Shader shader = Shader.Find("HDRP/Unlit");
                if (shader == null)
                    shader = Shader.Find("HDRP/Lit");
                if (shader == null)
                    shader = Shader.Find("Unlit/Transparent");
                if (shader == null)
                    shader = Shader.Find("Unlit/Color");
                fallbackMaterial = new Material(shader);
                fallbackMaterial.name = "DMWorldAmmoHud_FallbackMat";
                ConfigureFallbackMaterialTransparent();
                ApplyFallbackAppearance(Color.white, forceTextures: true);
                fallbackRenderer.sharedMaterial = fallbackMaterial;
                fallbackRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                fallbackRenderer.receiveShadows = false;
                EnsureTextMeshFallback();
            }
            else
            {
                ApplyFallbackQuadScale();
                StripInteractionComponents();
                EnsureTextMeshFallback();
            }
        }

        private void ConfigureFallbackMaterialTransparent()
        {
            if (fallbackMaterial == null)
                return;

            // Fully transparent panel: HDRP Unlit surface type Transparent + alpha blend.
            if (fallbackMaterial.HasProperty("_SurfaceType"))
                fallbackMaterial.SetFloat("_SurfaceType", 1f); // Transparent
            if (fallbackMaterial.HasProperty("_BlendMode"))
                fallbackMaterial.SetFloat("_BlendMode", 0f); // Alpha
            if (fallbackMaterial.HasProperty("_SrcBlend"))
                fallbackMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (fallbackMaterial.HasProperty("_DstBlend"))
                fallbackMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (fallbackMaterial.HasProperty("_ZWrite"))
                fallbackMaterial.SetFloat("_ZWrite", 0f);
            if (fallbackMaterial.HasProperty("_TransparentCullMode"))
                fallbackMaterial.SetFloat("_TransparentCullMode", 2f); // Off
            if (fallbackMaterial.HasProperty("_CullMode"))
                fallbackMaterial.SetFloat("_CullMode", 0f);
            if (fallbackMaterial.HasProperty("_AlphaCutoffEnable"))
                fallbackMaterial.SetFloat("_AlphaCutoffEnable", 0f);
            if (fallbackMaterial.HasProperty("_EnableFogOnTransparent"))
                fallbackMaterial.SetFloat("_EnableFogOnTransparent", 0f);
            fallbackMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            fallbackMaterial.DisableKeyword("_SURFACE_TYPE_OPAQUE");
            fallbackMaterial.renderQueue = 3000;
            fallbackMaterial.SetOverrideTag("RenderType", "Transparent");
        }

        private void UpdateFallbackQuad()
        {
            if (fallbackQuad == null)
                return;
            ApplyFallbackQuadScale();
            // UITK world meshes can reappear after panel rebuild - throttle, do not scan every frame.
            if (Time.frameCount >= nextHideMeshesFrame)
            {
                HideNonFallbackWorldMeshes();
                nextHideMeshesFrame = Time.frameCount + 16;
            }
            fallbackQuad.SetActive(lastShown);
            if (fallbackMaterial != null)
                ApplyFallbackAppearance(lastColorValid ? lastColor : Color.white, forceTextures: true);

            // Guarantee RT has opaque digits even if UITK->RT is blank/clear-only.
            SyncFallbackDigits(lastShown ? (lastText ?? "0") : null, lastColorValid ? lastColor : ColorGunpowder);

            if (textMeshFallback != null)
            {
                textMeshFallback.gameObject.SetActive(lastShown);
                if (textMeshRenderer != null)
                    textMeshRenderer.enabled = lastShown;
            }
        }

        private void ApplyFallbackAppearance(Color tint, bool forceTextures)
        {
            if (fallbackMaterial == null)
                return;

            float emit = Mathf.Max(1f, emissionIntensity);
            Color emissive = tint * emit;

            // White surface * RT so digit RGB/alpha come from the texture; glow from emissive.
            Color surface = Color.white;
            if (fallbackMaterial.HasProperty("_UnlitColor"))
                fallbackMaterial.SetColor("_UnlitColor", surface);
            if (fallbackMaterial.HasProperty("_BaseColor"))
                fallbackMaterial.SetColor("_BaseColor", surface);
            if (fallbackMaterial.HasProperty("_Color"))
                fallbackMaterial.SetColor("_Color", surface);

            if (fallbackMaterial.HasProperty("_EmissiveColor"))
                fallbackMaterial.SetColor("_EmissiveColor", emissive);
            if (fallbackMaterial.HasProperty("_EmissionColor"))
            {
                fallbackMaterial.EnableKeyword("_EMISSION");
                fallbackMaterial.SetColor("_EmissionColor", emissive);
            }
            if (fallbackMaterial.HasProperty("_EmissiveIntensity"))
                fallbackMaterial.SetFloat("_EmissiveIntensity", emit);
            if (fallbackMaterial.HasProperty("_UseEmissiveIntensity"))
                fallbackMaterial.SetFloat("_UseEmissiveIntensity", 1f);
            if (fallbackMaterial.HasProperty("_EmissiveExposureWeight"))
                fallbackMaterial.SetFloat("_EmissiveExposureWeight", 0f); // do not dim with exposure

            // Transparent so RT alpha (and empty panel) does not draw a black plate.
            if (fallbackMaterial.HasProperty("_SurfaceType"))
                fallbackMaterial.SetFloat("_SurfaceType", 1f); // Transparent
            if (fallbackMaterial.HasProperty("_BlendMode"))
                fallbackMaterial.SetFloat("_BlendMode", 0f); // Alpha
            if (fallbackMaterial.HasProperty("_AlphaCutoffEnable"))
                fallbackMaterial.SetFloat("_AlphaCutoffEnable", 0f);
            fallbackMaterial.SetOverrideTag("RenderType", "Transparent");
            fallbackMaterial.SetInt("_ZWrite", 0);
            fallbackMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            fallbackMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            fallbackMaterial.DisableKeyword("_SURFACE_TYPE_OPAQUE");
            fallbackMaterial.EnableKeyword("_EMISSIVE_COLOR_MAP");

            if (forceTextures || fallbackRt != null)
            {
                if (fallbackMaterial.HasProperty("_UnlitColorMap"))
                    fallbackMaterial.SetTexture("_UnlitColorMap", fallbackRt);
                if (fallbackMaterial.HasProperty("_EmissiveColorMap"))
                    fallbackMaterial.SetTexture("_EmissiveColorMap", fallbackRt);
                if (fallbackMaterial.HasProperty("_BaseColorMap"))
                    fallbackMaterial.SetTexture("_BaseColorMap", fallbackRt);
                if (fallbackMaterial.HasProperty("_BaseMap"))
                    fallbackMaterial.SetTexture("_BaseMap", fallbackRt);
                if (fallbackMaterial.HasProperty("_MainTex"))
                    fallbackMaterial.SetTexture("_MainTex", fallbackRt);
            }
        }


        private Font ResolveAmmoFont()
        {
            if (textMeshFont != null)
                return textMeshFont;

            try
            {
                textMeshFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            catch (System.Exception)
            {
                textMeshFont = null;
            }

            if (textMeshFont == null)
            {
                try
                {
                    textMeshFont = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Segoe UI", "Helvetica" }, 64);
                }
                catch (System.Exception)
                {
                    textMeshFont = null;
                }
            }

            return textMeshFont;
        }

        private void EnsureTextMeshFallback()
        {
            if (textMeshFallback != null)
                return;

            Transform parent = fallbackQuad != null ? fallbackQuad.transform : transform;
            Transform existing = parent.Find("AmmoTextMesh");
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
                textMeshFallback = go.GetComponent<TextMesh>();
            }
            else
            {
                go = new GameObject("AmmoTextMesh");
                go.transform.SetParent(parent, false);
                textMeshFallback = go.AddComponent<TextMesh>();
            }

            go.transform.localPosition = new Vector3(0f, 0f, -0.001f);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            ResolveAmmoFont();

            textMeshFallback.font = textMeshFont;
            textMeshFallback.text = lastText ?? "0";
            textMeshFallback.anchor = TextAnchor.MiddleCenter;
            textMeshFallback.alignment = TextAlignment.Center;
            textMeshFallback.characterSize = 0.035f;
            textMeshFallback.fontSize = 64;
            textMeshFallback.color = lastColorValid ? lastColor : ColorGunpowder;
            textMeshFallback.richText = false;

            textMeshRenderer = go.GetComponent<MeshRenderer>();
            if (textMeshRenderer != null)
            {
                textMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                textMeshRenderer.receiveShadows = false;
                // TextMesh needs the font atlas material — HDRP Unlit would blank the glyphs.
                if (textMeshFont != null && textMeshFont.material != null)
                    textMeshRenderer.sharedMaterial = textMeshFont.material;
            }

            int ignoreRaycast = LayerMask.NameToLayer("Ignore Raycast");
            if (ignoreRaycast >= 0)
                go.layer = ignoreRaycast;

            go.SetActive(lastShown);
        }

        private void SyncFallbackDigits(string text, Color color)
        {
            if (string.IsNullOrEmpty(text))
            {
                if (textMeshFallback != null)
                    textMeshFallback.text = string.Empty;
                return;
            }

            if (textMeshFallback == null)
                EnsureTextMeshFallback();

            if (textMeshFallback != null)
            {
                if (!string.Equals(textMeshFallback.text, text, System.StringComparison.Ordinal))
                    textMeshFallback.text = text;
                textMeshFallback.color = color;
            }

            StampAmmoToRt(text, color);

            // Keep UITK label in sync for RT consumers / editor preview.
            if (ammoLabel != null && !string.Equals(ammoLabel.text, text, System.StringComparison.Ordinal))
            {
                ammoLabel.text = text;
                ammoLabel.style.color = color;
                ammoLabel.MarkDirtyRepaint();
            }
            if (ammoRoot != null)
                ammoRoot.MarkDirtyRepaint();
            if (document != null && document.rootVisualElement != null)
                document.rootVisualElement.MarkDirtyRepaint();
        }

        private void StampAmmoToRt(string text, Color color)
        {
            if (fallbackRt == null || string.IsNullOrEmpty(text))
                return;

            if (stampedRtColorValid
                && string.Equals(stampedRtText, text, System.StringComparison.Ordinal)
                && stampedRtColor == color)
                return;

            stampedRtText = text;
            stampedRtColor = color;
            stampedRtColorValid = true;

            ResolveAmmoFont();
            if (textMeshFont == null)
                return;

            int w = fallbackRt.width;
            int h = fallbackRt.height;
            Texture2D stamp = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = "DMWorldAmmoHud_Stamp",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            Color32 clear = new Color32(0, 0, 0, 0);
            Color32[] pixels = new Color32[w * h];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = clear;

            int fontSize = Mathf.Clamp(h * 3 / 4, 32, 96);
            textMeshFont.RequestCharactersInTexture(text, fontSize, FontStyle.Bold);

            // Measure total width for centering.
            float totalWidth = 0f;
            float maxHeight = 0f;
            CharacterInfo[] infos = new CharacterInfo[text.Length];
            for (int i = 0; i < text.Length; i++)
            {
                if (!textMeshFont.GetCharacterInfo(text[i], out infos[i], fontSize, FontStyle.Bold))
                    textMeshFont.GetCharacterInfo(text[i], out infos[i], fontSize, FontStyle.Normal);
                totalWidth += infos[i].advance;
                float ch = infos[i].maxY - infos[i].minY;
                if (ch > maxHeight)
                    maxHeight = ch;
            }

            float cursorX = (w - totalWidth) * 0.5f;
            float baseline = (h + maxHeight) * 0.5f;
            Color32 ink = color;
            ink.a = 255;

            Texture2D fontTex = textMeshFont.material != null ? textMeshFont.material.mainTexture as Texture2D : null;
            // Builtin font textures are often not readable — draw solid glyph quads as a bulletproof fallback.
            for (int i = 0; i < text.Length; i++)
            {
                CharacterInfo info = infos[i];
                int x0 = Mathf.FloorToInt(cursorX + info.minX);
                int x1 = Mathf.CeilToInt(cursorX + info.maxX);
                int y0 = Mathf.FloorToInt(baseline + info.minY);
                int y1 = Mathf.CeilToInt(baseline + info.maxY);
                x0 = Mathf.Clamp(x0, 0, w - 1);
                x1 = Mathf.Clamp(x1, 0, w);
                y0 = Mathf.Clamp(y0, 0, h - 1);
                y1 = Mathf.Clamp(y1, 0, h);

                bool sampled = false;
                if (fontTex != null)
                {
                    try
                    {
                        for (int y = y0; y < y1; y++)
                        {
                            float v = Mathf.InverseLerp(info.minY, info.maxY, (y + 0.5f) - baseline);
                            float fv = Mathf.Lerp(info.uvBottomLeft.y, info.uvTopLeft.y, v);
                            for (int x = x0; x < x1; x++)
                            {
                                float u = Mathf.InverseLerp(info.minX, info.maxX, (x + 0.5f) - cursorX);
                                float fu = Mathf.Lerp(info.uvBottomLeft.x, info.uvBottomRight.x, u);
                                Color fc = fontTex.GetPixelBilinear(fu, fv);
                                if (fc.a > 0.15f)
                                {
                                    Color32 outC = ink;
                                    outC.a = (byte)Mathf.Clamp(Mathf.RoundToInt(fc.a * 255f), 0, 255);
                                    pixels[y * w + x] = outC;
                                    sampled = true;
                                }
                            }
                        }
                    }
                    catch (System.Exception)
                    {
                        sampled = false;
                    }
                }

                if (!sampled)
                {
                    // Block-digit glyph so the RT is never fully clear.
                    for (int y = y0; y < y1; y++)
                    {
                        for (int x = x0; x < x1; x++)
                            pixels[y * w + x] = ink;
                    }
                }

                cursorX += info.advance;
            }

            stamp.SetPixels32(pixels);
            stamp.Apply(false, false);
            Graphics.Blit(stamp, fallbackRt);
            Object.Destroy(stamp);
        }

        private void SetShown(bool shown)
        {
            if (ammoLabel != null)
                ammoLabel.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
            if (ammoRoot != null)
                ammoRoot.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
            if (textMeshFallback != null)
                textMeshFallback.gameObject.SetActive(shown);
            if (usingFallback && shown)
                SyncFallbackDigits(lastText ?? "0", lastColorValid ? lastColor : ColorGunpowder);
        }
    }
}
