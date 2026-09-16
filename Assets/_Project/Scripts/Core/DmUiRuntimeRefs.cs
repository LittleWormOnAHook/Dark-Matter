using Project.UI;
using UnityEngine;

namespace Project.Core
{
    /// <summary>
    /// Cached UI singleton lookups for menu/prompt paths (avoids repeated FindAnyObjectByType).
    /// </summary>
    public static class DmUiRuntimeRefs
    {
        public static UIManager UiManager { get; private set; }
        public static InventoryUI InventoryUi { get; private set; }
        public static MapUI MapUi { get; private set; }
        public static JournalPanelUI JournalPanel { get; private set; }
        public static ToolBarUI ToolBar { get; private set; }

        public static UIManager ResolveUiManager(bool includeInactive = false)
        {
            if (UiManager != null)
                return UiManager;

            UiManager = includeInactive
                ? Object.FindAnyObjectByType<UIManager>(FindObjectsInactive.Include)
                : Object.FindAnyObjectByType<UIManager>();
            return UiManager;
        }

        public static InventoryUI ResolveInventoryUi()
        {
            if (InventoryUi != null)
                return InventoryUi;

            InventoryUi = Object.FindAnyObjectByType<InventoryUI>();
            return InventoryUi;
        }

        public static MapUI ResolveMapUi()
        {
            if (MapUi != null)
                return MapUi;

            MapUi = Object.FindAnyObjectByType<MapUI>();
            return MapUi;
        }

        public static JournalPanelUI ResolveJournalPanel()
        {
            if (JournalPanel != null)
                return JournalPanel;

            JournalPanel = Object.FindAnyObjectByType<JournalPanelUI>(FindObjectsInactive.Include);
            return JournalPanel;
        }

        public static ToolBarUI ResolveToolBar()
        {
            if (ToolBar != null)
                return ToolBar;

            ToolBar = Object.FindAnyObjectByType<ToolBarUI>();
            return ToolBar;
        }

        public static void Register(UIManager manager)
        {
            if (manager != null)
                UiManager = manager;
        }

        public static void Unregister(UIManager manager)
        {
            if (UiManager == manager)
                UiManager = null;
        }

        public static void Register(InventoryUI ui)
        {
            if (ui != null)
                InventoryUi = ui;
        }

        public static void Unregister(InventoryUI ui)
        {
            if (InventoryUi == ui)
                InventoryUi = null;
        }

        public static void Register(MapUI ui)
        {
            if (ui != null)
                MapUi = ui;
        }

        public static void Unregister(MapUI ui)
        {
            if (MapUi == ui)
                MapUi = null;
        }

        public static void Register(JournalPanelUI panel)
        {
            if (panel != null)
                JournalPanel = panel;
        }

        public static void Unregister(JournalPanelUI panel)
        {
            if (JournalPanel == panel)
                JournalPanel = null;
        }

        public static void Register(ToolBarUI toolbar)
        {
            if (toolbar != null)
                ToolBar = toolbar;
        }

        public static void Unregister(ToolBarUI toolbar)
        {
            if (ToolBar == toolbar)
                ToolBar = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCaches()
        {
            UiManager = null;
            InventoryUi = null;
            MapUi = null;
            JournalPanel = null;
            ToolBar = null;
        }
    }
}
