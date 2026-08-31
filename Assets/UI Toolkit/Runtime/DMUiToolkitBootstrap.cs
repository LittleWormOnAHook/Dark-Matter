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
        public const int ShellSortingOrder = 100;
        public const int LoadingSortingOrder = 32000;

        public const string ShellUxmlPath = "Assets/UI Toolkit/Screens/Shell.uxml";
        public const string LoadingUxmlPath = "Assets/UI Toolkit/Screens/LoadingOverlay.uxml";
        public const string ThemeUssPath = "Assets/UI Toolkit/Themes/DarkMatterGenesis.uss";
        public const string LoadingUssPath = "Assets/UI Toolkit/Screens/LoadingOverlay.uss";
        public const string ExistingPanelSettingsPath = "Assets/UI Toolkit/PanelSettings.asset";
        public const string LoadingPanelSettingsPath = "Assets/UI Toolkit/LoadingPanelSettings.asset";
        public const string DefaultThemePath = "Assets/UI Toolkit/UnityThemes/UnityDefaultRuntimeTheme.tss";

        private static DMUiToolkitBootstrap instance;
        private static PanelSettings runtimeShellSettings;
        private static PanelSettings runtimeLoadingSettings;
        private static bool stamped;

        [SerializeField] private UIDocument shellDocument;
        [SerializeField] private UIDocument loadingDocument;

        public static DMUiToolkitBootstrap Instance => instance;

        public UIDocument ShellDocument => shellDocument;
        public UIDocument LoadingDocument => loadingDocument;

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
            if (shellDocument == null)
                shellDocument = GetComponent<UIDocument>();
            if (shellDocument == null)
                shellDocument = gameObject.AddComponent<UIDocument>();

            if (shellDocument.panelSettings == null)
                shellDocument.panelSettings = ResolvePanelSettings(LoadingPanelSettingsPath, ShellSortingOrder, ref runtimeShellSettings);

            VisualTreeAsset shellTree = LoadUxml(ShellUxmlPath);
            if (shellTree != null && shellDocument.visualTreeAsset != shellTree)
                shellDocument.visualTreeAsset = shellTree;

            ApplyTheme(shellDocument, ThemeUssPath);

            Transform loadingTransform = transform.Find(LoadingChildName);
            GameObject loadingObject = loadingTransform != null ? loadingTransform.gameObject : null;
            if (loadingObject == null)
            {
                loadingObject = new GameObject(LoadingChildName);
                loadingObject.transform.SetParent(transform, false);
            }

            if (loadingDocument == null)
                loadingDocument = loadingObject.GetComponent<UIDocument>();
            if (loadingDocument == null)
                loadingDocument = loadingObject.AddComponent<UIDocument>();

            if (loadingDocument.panelSettings == null)
                loadingDocument.panelSettings = ResolvePanelSettings(LoadingPanelSettingsPath, LoadingSortingOrder, ref runtimeLoadingSettings);

            // Keep sort at overlay level even if we reused a shared asset instance.
            if (loadingDocument.panelSettings != null)
                loadingDocument.panelSettings.sortingOrder = LoadingSortingOrder;

            VisualTreeAsset loadingTree = LoadUxml(LoadingUxmlPath);
            if (loadingTree != null && loadingDocument.visualTreeAsset != loadingTree)
                loadingDocument.visualTreeAsset = loadingTree;

            ApplyTheme(loadingDocument, ThemeUssPath);
            ApplyTheme(loadingDocument, LoadingUssPath);

            if (!DMUiToolkitLoadingOverlay.IsShowing)
                loadingObject.SetActive(false);
        }

        internal static PanelSettings ResolvePanelSettings(string assetPath, int sortingOrder, ref PanelSettings runtimeCache)
        {
            PanelSettings loaded = LoadAsset<PanelSettings>(assetPath);
            if (loaded != null)
            {
                if (loaded.sortingOrder != sortingOrder)
                {
                    if (runtimeCache == null)
                    {
                        runtimeCache = Instantiate(loaded);
                        runtimeCache.name = loaded.name + "_Runtime_" + sortingOrder;
                        runtimeCache.hideFlags = HideFlags.HideAndDontSave;
                    }

                    runtimeCache.sortingOrder = sortingOrder;
                    return runtimeCache;
                }

                return loaded;
            }

            if (runtimeCache != null)
                return runtimeCache;

            runtimeCache = ScriptableObject.CreateInstance<PanelSettings>();
            runtimeCache.name = "DMUiToolkitPanelSettings_Runtime_" + sortingOrder;
            runtimeCache.hideFlags = HideFlags.HideAndDontSave;
            runtimeCache.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            runtimeCache.referenceResolution = new Vector2Int(1920, 1080);
            runtimeCache.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            runtimeCache.match = 0.5f;
            runtimeCache.sortingOrder = sortingOrder;

            ThemeStyleSheet theme = LoadAsset<ThemeStyleSheet>(DefaultThemePath);
            if (theme != null)
                runtimeCache.themeStyleSheet = theme;

            return runtimeCache;
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
