using Project.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// Dual-run Toolkit shell. Finds MainCanvas and parents UITK_Root as a sibling.
    /// Does not destroy Canvas / MainCanvas / EventSystem. No second EventSystem.
    /// </summary>
    [DefaultExecutionOrder(-450)]
    [DisallowMultipleComponent]
    public class DMUiToolkitBootstrap : MonoBehaviour
    {
        public const string RootName = "UITK_Root";
        public const string LoadingChildName = "UITK_Loading";
        public const string HudChildName = "UITK_Hud";
        public const int ShellSortingOrder = 100;
        public const int LoadingSortingOrder = 32000;
        // Above MenusSort (90) so hotbar/tools stay visible during inventory/journal.
        public const int HudSortingOrder = 95;

        public const string ShellUxmlPath = "Assets/UI Toolkit/Screens/Shell.uxml";
        public const string LoadingUxmlPath = "Assets/UI Toolkit/Screens/LoadingOverlay.uxml";
        public const string ThemeUssPath = "Assets/UI Toolkit/Themes/DarkMatterGenesis.uss";
        public const string LoadingUssPath = "Assets/UI Toolkit/Screens/LoadingOverlay.uss";
        public const string HudUxmlPath = "Assets/UI Toolkit/Screens/Hud.uxml";
        public const string HudUssPath = "Assets/UI Toolkit/Screens/Hud.uss";
        public const string ExistingPanelSettingsPath = "Assets/UI Toolkit/PanelSettings.asset";
        public const string LoadingPanelSettingsPath = "Assets/UI Toolkit/LoadingPanelSettings.asset";
        public const string DefaultThemePath = "Assets/UI Toolkit/UnityThemes/UnityDefaultRuntimeTheme.tss";

        private static DMUiToolkitBootstrap instance;
        private static PanelSettings runtimeShellSettings;
        private static PanelSettings runtimeLoadingSettings;
        private static bool stamped;

        [SerializeField] private UIDocument shellDocument;
        [SerializeField] private UIDocument loadingDocument;
        [SerializeField] private UIDocument hudDocument;

        public static DMUiToolkitBootstrap Instance => instance;

        public UIDocument ShellDocument => shellDocument;
        public UIDocument LoadingDocument => loadingDocument;
        public UIDocument HudDocument => hudDocument;

        public static bool IsRootActive
        {
            get
            {
                if (instance != null)
                    return instance.isActiveAndEnabled && instance.gameObject.activeInHierarchy;

                GameObject existing = FindRoot(includeInactive: true);
                return existing != null && existing.activeInHierarchy;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            runtimeShellSettings = null;
            runtimeLoadingSettings = null;
            stamped = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!Application.isPlaying)
                return;

            if (!DMUiToolkitConfig.IsEnabled)
            {
                Stamp("disabled (config) — uGUI path only");
                return;
            }

            EnsureExists();
        }

        /// <summary>
        /// Creates UITK_Root as a sibling of MainCanvas when missing.
        /// Returns false when config is off or the hierarchy kill-switch (inactive root) is set.
        /// </summary>
        public static bool EnsureExists()
        {
            if (!Application.isPlaying)
                return false;

            if (!DMUiToolkitConfig.IsEnabled)
                return false;

            if (instance != null)
                return instance.isActiveAndEnabled && instance.gameObject.activeInHierarchy;

            GameObject existing = FindRoot(includeInactive: true);
            if (existing != null)
            {
                instance = existing.GetComponent<DMUiToolkitBootstrap>();
                if (instance == null)
                    instance = existing.AddComponent<DMUiToolkitBootstrap>();

                instance.EnsureDocuments();
                bool active = existing.activeInHierarchy;
                Stamp(active ? "using scene UITK_Root" : "UITK_Root disabled in Hierarchy — uGUI path");
                return active;
            }

            GameObject root = new GameObject(RootName);
            PlaceAsMainCanvasSibling(root);
            instance = root.AddComponent<DMUiToolkitBootstrap>();
            instance.EnsureDocuments();
            Stamp("runtime UITK_Root created (dual-run, MainCanvas kept)");
            return true;
        }

        private static GameObject FindRoot(bool includeInactive)
        {
            if (!includeInactive)
            {
                GameObject named = GameObject.Find(RootName);
                return named;
            }

            DMUiToolkitBootstrap[] found = FindObjectsByType<DMUiToolkitBootstrap>(FindObjectsInactive.Include);
            for (int i = 0; i < found.Length; i++)
            {
                if (found[i] != null && found[i].gameObject.name == RootName)
                    return found[i].gameObject;
            }

            Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] != null && transforms[i].name == RootName)
                    return transforms[i].gameObject;
            }

            return null;
        }

        private static void PlaceAsMainCanvasSibling(GameObject root)
        {
            Canvas canvas = MainMenuController.ResolveMainCanvas();
            if (canvas == null)
            {
                GameObject named = GameObject.Find("MainCanvas");
                if (named != null)
                    canvas = named.GetComponent<Canvas>();
            }

            if (canvas != null && canvas.transform.parent != null)
                root.transform.SetParent(canvas.transform.parent, false);
            else if (canvas != null)
                root.transform.SetParent(null, false);
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            EnsureDocuments();
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        public void EnsureDocuments()
        {
            // Unity 6 nested UIDocuments MUST share the exact same PanelSettings instance.
            // UITK_Loading used to be a child of UITK_Root, so assigning sort 32000 vs 100
            // asserted in UIDocument.set_panelSettings and the overlay never came up.
            // Keep them siblings (same parent as MainCanvas) so each can have its own panel.
            GameObject loadingObject = EnsureLoadingSibling();

            if (shellDocument == null)
                shellDocument = GetComponent<UIDocument>();
            if (shellDocument == null)
                shellDocument = gameObject.AddComponent<UIDocument>();

            AssignPanelSettings(shellDocument, ResolvePanelSettings(ExistingPanelSettingsPath, ShellSortingOrder, ref runtimeShellSettings));

            VisualTreeAsset shellTree = LoadUxml(ShellUxmlPath);
            if (shellTree != null && shellDocument.visualTreeAsset != shellTree)
                shellDocument.visualTreeAsset = shellTree;

            ApplyTheme(shellDocument, ThemeUssPath);

            if (loadingDocument == null)
                loadingDocument = loadingObject.GetComponent<UIDocument>();
            if (loadingDocument == null)
                loadingDocument = loadingObject.AddComponent<UIDocument>();

            AssignPanelSettings(loadingDocument, ResolvePanelSettings(LoadingPanelSettingsPath, LoadingSortingOrder, ref runtimeLoadingSettings));

            VisualTreeAsset loadingTree = LoadUxml(LoadingUxmlPath);
            if (loadingTree != null && loadingDocument.visualTreeAsset != loadingTree)
                loadingDocument.visualTreeAsset = loadingTree;

            ApplyTheme(loadingDocument, ThemeUssPath);
            ApplyTheme(loadingDocument, LoadingUssPath);

            if (!DMUiToolkitLoadingOverlay.IsShowing)
                loadingObject.SetActive(false);

            BindHudDocument();
        }

        private void OnEnable()
        {
            SyncHudHostActive(true);
        }

        private void OnDisable()
        {
            SyncHudHostActive(false);
        }

        private void SyncHudHostActive(bool rootEnabled)
        {
            if (hudDocument == null)
                return;

            bool want = rootEnabled && isActiveAndEnabled && gameObject.activeInHierarchy && DMUiToolkitConfig.IsEnabled;
            if (hudDocument.gameObject.activeSelf != want)
                hudDocument.gameObject.SetActive(want);
        }

        private void BindHudDocument()
        {
            GameObject hudObject = EnsureHudSibling();
            if (hudObject == null)
                return;

            if (hudDocument == null)
                hudDocument = hudObject.GetComponent<UIDocument>();
            if (hudDocument == null)
                hudDocument = hudObject.AddComponent<UIDocument>();

            // Same Panel Settings instance as UITK_Root. Do not Instantiate a second panel.
            AssignPanelSettings(hudDocument, shellDocument != null ? shellDocument.panelSettings : null);
            hudDocument.sortingOrder = HudSortingOrder;

            VisualTreeAsset hudTree = LoadUxml(HudUxmlPath);
            if (hudTree != null && hudDocument.visualTreeAsset != hudTree)
                hudDocument.visualTreeAsset = hudTree;

            ApplyTheme(hudDocument, ThemeUssPath);
            ApplyTheme(hudDocument, HudUssPath);

            bool rootLive = isActiveAndEnabled && gameObject.activeInHierarchy;
            if (hudObject.activeSelf != rootLive)
                hudObject.SetActive(rootLive);

            if (rootLive)
            {
                DMUiToolkitHud.Bind(hudDocument);
                DMUiToolkitMinimap.Bind(hudDocument);
            }
        }

        private GameObject EnsureHudSibling()
        {
            GameObject hudObject = FindNamedSiblingOrNested(HudChildName);
            if (hudObject == null)
            {
                hudObject = new GameObject(HudChildName);
                hudObject.transform.SetParent(transform.parent, false);
                int rootIndex = transform.GetSiblingIndex();
                hudObject.transform.SetSiblingIndex(rootIndex + 1);
                return hudObject;
            }

            if (hudObject.transform != transform && hudObject.transform.IsChildOf(transform))
            {
                hudObject.transform.SetParent(transform.parent, false);
                int rootIndex = transform.GetSiblingIndex();
                hudObject.transform.SetSiblingIndex(rootIndex + 1);
            }

            return hudObject;
        }

        private GameObject FindNamedSiblingOrNested(string childName)
        {
            if (transform.parent != null)
            {
                Transform parent = transform.parent;
                for (int i = 0; i < parent.childCount; i++)
                {
                    Transform sibling = parent.GetChild(i);
                    if (sibling != null && sibling != transform && sibling.name == childName)
                        return sibling.gameObject;
                }
            }

            GameObject nested = null;
            Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform candidate = transforms[i];
                if (candidate == null || candidate.name != childName || candidate == transform)
                    continue;
                if (candidate.parent == transform)
                {
                    nested = candidate.gameObject;
                    continue;
                }
                return candidate.gameObject;
            }

            return nested;
        }

        private GameObject EnsureLoadingSibling()
        {
            GameObject loadingObject = FindLoadingObject();
            if (loadingObject == null)
            {
                loadingObject = new GameObject(LoadingChildName);
                loadingObject.transform.SetParent(transform.parent, false);
                int rootIndex = transform.GetSiblingIndex();
                loadingObject.transform.SetSiblingIndex(rootIndex + 1);
                return loadingObject;
            }

            if (loadingObject.transform != transform && loadingObject.transform.IsChildOf(transform))
            {
                loadingObject.transform.SetParent(transform.parent, false);
                int rootIndex = transform.GetSiblingIndex();
                loadingObject.transform.SetSiblingIndex(rootIndex + 1);
            }

            return loadingObject;
        }

        private GameObject FindLoadingObject()
        {
            // Prefer the sibling (including inactive). Transform.Find skips inactive children,
            // and a leftover nested child must not win over the sort-32000 sibling.
            if (transform.parent != null)
            {
                Transform parent = transform.parent;
                for (int i = 0; i < parent.childCount; i++)
                {
                    Transform sibling = parent.GetChild(i);
                    if (sibling != null && sibling != transform && sibling.name == LoadingChildName)
                        return sibling.gameObject;
                }
            }

            GameObject nested = null;
            Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform candidate = transforms[i];
                if (candidate == null || candidate.name != LoadingChildName || candidate == transform)
                    continue;
                if (candidate.parent == transform)
                {
                    nested = candidate.gameObject;
                    continue;
                }
                return candidate.gameObject;
            }

            return nested;
        }

        internal static void ReleaseLoadingClearColor()
        {
            if (runtimeLoadingSettings == null)
                return;

            runtimeLoadingSettings.clearColor = false;
            runtimeLoadingSettings.colorClearValue = Color.clear;
        }

        internal static void DeactivateLoadingHosts()
        {
            Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform host = transforms[i];
                if (host == null || host.name != LoadingChildName)
                    continue;
                if (host.name == RootName || host.name == "MainCanvas" || host.name == "OpticsOverlayCanvas")
                    continue;

                UIDocument doc = host.GetComponent<UIDocument>();
                if (doc != null)
                {
                    if (doc.panelSettings != null)
                    {
                        doc.panelSettings.clearColor = false;
                        doc.panelSettings.colorClearValue = Color.clear;
                    }

                    doc.panelSettings = null;
                    doc.enabled = false;
                }

                host.gameObject.SetActive(false);
            }
        }

        private static void AssignPanelSettings(UIDocument document, PanelSettings settings)
        {
            if (document == null || settings == null)
                return;

            if (document.panelSettings == settings)
                return;

            document.panelSettings = settings;
        }

        internal static PanelSettings ResolvePanelSettings(string assetPath, int sortingOrder, ref PanelSettings runtimeCache)
        {
            if (runtimeCache != null)
            {
                ConfigureRuntimePanel(runtimeCache, sortingOrder);
                return runtimeCache;
            }

            PanelSettings loaded = LoadAsset<PanelSettings>(assetPath);
            if (loaded == null)
                loaded = LoadAsset<PanelSettings>(ExistingPanelSettingsPath);

            if (loaded != null)
            {
                runtimeCache = Instantiate(loaded);
                runtimeCache.name = loaded.name + "_Runtime_" + sortingOrder;
            }
            else
            {
                runtimeCache = ScriptableObject.CreateInstance<PanelSettings>();
                runtimeCache.name = "DMUiToolkitPanelSettings_Runtime_" + sortingOrder;
            }

            runtimeCache.hideFlags = HideFlags.HideAndDontSave;
            ConfigureRuntimePanel(runtimeCache, sortingOrder);
            return runtimeCache;
        }

        internal static void ConfigureRuntimePanel(PanelSettings settings, int sortingOrder)
        {
            if (settings == null)
                return;

            settings.sortingOrder = sortingOrder;
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            ApplyUiScaleToPanel(settings, sortingOrder >= LoadingSortingOrder ? 1f : GameSettings.UiScale);
            settings.forceGammaRendering = true;

            if (sortingOrder >= LoadingSortingOrder)
            {
                settings.clearColor = true;
                settings.colorClearValue = Color.black;
            }
            else
            {
                settings.clearColor = false;
                settings.colorClearValue = Color.clear;
            }

            if (settings.themeStyleSheet == null)
            {
                ThemeStyleSheet theme = LoadAsset<ThemeStyleSheet>(DefaultThemePath);
                if (theme != null)
                    settings.themeStyleSheet = theme;
            }
        }

        internal static VisualTreeAsset LoadUxml(string assetPath)
        {
            return LoadAsset<VisualTreeAsset>(assetPath);
        }

        internal static StyleSheet LoadUss(string assetPath)
        {
            return LoadAsset<StyleSheet>(assetPath);
        }

        internal static void ApplyTheme(UIDocument document, string ussPath)
        {
            if (document == null)
                return;

            StyleSheet sheet = LoadUss(ussPath);
            if (sheet == null)
                return;

            VisualElement root = document.rootVisualElement;
            if (root == null)
                return;

            bool already = false;
            for (int i = 0; i < root.styleSheets.count; i++)
            {
                if (root.styleSheets[i] == sheet)
                {
                    already = true;
                    break;
                }
            }

            if (!already)
                root.styleSheets.Add(sheet);
        }

        internal static T LoadAsset<T>(string assetPath) where T : Object
        {
            if (string.IsNullOrEmpty(assetPath))
                return null;

#if UNITY_EDITOR
            T editorAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (editorAsset != null)
                return editorAsset;
#endif

            string fileName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            return Resources.Load<T>(fileName);
        }

        public static void ApplyUiScale(float uiScale)
        {
            float scale = Mathf.Clamp(uiScale, GameSettings.UiScaleMin, GameSettings.UiScaleMax);
            ApplyUiScaleToPanel(runtimeShellSettings, scale);
            if (instance == null)
                return;

            if (instance.shellDocument != null)
                ApplyUiScaleToPanel(instance.shellDocument.panelSettings, scale);
            if (instance.hudDocument != null)
                ApplyUiScaleToPanel(instance.hudDocument.panelSettings, scale);
        }

        private static void ApplyUiScaleToPanel(PanelSettings settings, float uiScale)
        {
            if (settings == null)
                return;
            if (settings.sortingOrder >= LoadingSortingOrder)
            {
                settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                settings.referenceResolution = new Vector2Int(1920, 1080);
                settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
                settings.match = 0.5f;
                return;
            }

            float scale = Mathf.Clamp(uiScale, GameSettings.UiScaleMin, GameSettings.UiScaleMax);
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(
                Mathf.RoundToInt(1920f / Mathf.Max(0.01f, scale)),
                Mathf.RoundToInt(1080f / Mathf.Max(0.01f, scale)));
            settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            settings.match = 0.5f;
        }

        internal static void Stamp(string detail)
        {
            if (stamped && string.IsNullOrEmpty(detail))
                return;

            stamped = true;
            if (string.IsNullOrEmpty(detail))
                Debug.Log(DMUiToolkitConfig.LogStamp);
            else
                Debug.Log(DMUiToolkitConfig.LogStamp + " " + detail);
        }
    }
}
