using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>Sibling UIDocument helper for main-menu sub-panels (sort above UITK_MainMenu).</summary>
    internal static class DMUiToolkitMenuDocument
    {
        public static UIDocument Ensure(string objectName, string uxmlPath, int sortingOrder)
        {
            if (!Application.isPlaying || !DMUiToolkitBootstrap.EnsureExists())
                return null;

            DMUiToolkitBootstrap bootstrap = DMUiToolkitBootstrap.Instance;
            Transform parent = bootstrap != null ? bootstrap.transform.parent : null;
            PanelSettings settings = bootstrap != null && bootstrap.ShellDocument != null
                ? bootstrap.ShellDocument.panelSettings
                : null;

            GameObject host = DMUiToolkitOverlayDocument.FindNamed(objectName);
            if (host == null)
            {
                host = new GameObject(objectName);
                host.transform.SetParent(parent, false);
            }

            UIDocument document = host.GetComponent<UIDocument>();
            if (document == null)
                document = host.AddComponent<UIDocument>();

            if (settings != null && document.panelSettings != settings)
                document.panelSettings = settings;

            document.sortingOrder = sortingOrder;

            VisualTreeAsset tree = DMUiToolkitBootstrap.LoadUxml(uxmlPath);
            if (tree != null && document.visualTreeAsset != tree)
                document.visualTreeAsset = tree;

            DMUiToolkitBootstrap.ApplyTheme(document, DMUiToolkitBootstrap.ThemeUssPath);
            DMUiToolkitBootstrap.ApplyTheme(document, "Assets/UI Toolkit/Screens/MenuModal.uss");
            return document;
        }
    }
}
