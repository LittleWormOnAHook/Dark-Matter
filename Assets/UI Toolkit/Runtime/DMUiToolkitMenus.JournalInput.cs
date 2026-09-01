using Project.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    public partial class DMUiToolkitMenus
    {
        private JournalWindowId? pendingShowWindow;

        /// <summary>
        /// Switch journal tabs without closing when already on the requested tab (e.g. Blueprints on B).
        /// </summary>
        public static bool TrySwitchJournalTab(JournalWindowId windowId, bool journalHotkey = false)
        {
            if (!DMUiToolkitConfig.IsEnabled || !DMUiToolkitBootstrap.IsRootActive)
                return false;

            if (!GameSession.HasStarted || !IsToolkitWindow(windowId))
                return false;

            JournalPanelUI journal = Object.FindAnyObjectByType<JournalPanelUI>(FindObjectsInactive.Include);
            if (journal == null)
                return false;

            bool switched = journalHotkey
                ? journal.TryToggleJournal()
                : journal.SwitchToTab(windowId);

            if (!switched)
                return false;

            DMUiToolkitMenus host = EnsureHost();
            if (host == null)
                return true;

            if (journal.IsOpen)
            {
                JournalWindowId? active = journal.ActiveJournalWindow ?? windowId;
                if (active.HasValue)
                    host.ForceShow(active.Value);
            }
            else
            {
                host.HideMenus();
            }

            return true;
        }

        /// <summary>
        /// Primary journal hotkey path while UITK drives menus. Toggles closed when the same tab is pressed again.
        /// </summary>
        public static bool TryToggleJournalTab(JournalWindowId windowId, bool journalHotkey = false)
        {
            if (!DMUiToolkitConfig.IsEnabled || !DMUiToolkitBootstrap.IsRootActive)
                return false;

            if (!GameSession.HasStarted || !IsToolkitWindow(windowId))
                return false;

            JournalPanelUI journal = Object.FindAnyObjectByType<JournalPanelUI>(FindObjectsInactive.Include);
            if (journal == null)
                return false;

            bool toggled = journalHotkey
                ? journal.TryToggleJournal()
                : journal.TryToggleTab(windowId);

            if (!toggled)
                return false;

            DMUiToolkitMenus host = EnsureHost();
            if (host == null)
                return true;

            if (journal.IsOpen)
            {
                JournalWindowId? active = journal.ActiveJournalWindow ?? windowId;
                if (active.HasValue)
                    host.ForceShow(active.Value);
            }
            else
            {
                host.HideMenus();
            }

            return true;
        }

        internal void EnsureReady()
        {
            if (document == null)
                document = GetComponent<UIDocument>();

            EnsurePanelSettings();

            if (document != null && !document.gameObject.activeInHierarchy)
                document.gameObject.SetActive(true);

            if (!boundTree)
                BindTree();
        }

        private void EnsurePanelSettings()
        {
            if (document == null)
                return;

            if (document.panelSettings != null)
                return;

            DMUiToolkitBootstrap.EnsureExists();
            DMUiToolkitBootstrap bootstrap = DMUiToolkitBootstrap.Instance;
            if (bootstrap == null)
                return;

            UIDocument hud = bootstrap.HudDocument;
            if (hud != null && hud.panelSettings != null)
            {
                document.panelSettings = hud.panelSettings;
                return;
            }

            UIDocument shell = bootstrap.ShellDocument;
            if (shell != null && shell.panelSettings != null)
                document.panelSettings = shell.panelSettings;
        }

        internal void ForceShow(JournalWindowId window)
        {
            EnsureReady();

            if (menuRoot == null)
            {
                pendingShowWindow = window;
                if (document != null)
                    document.rootVisualElement?.schedule.Execute(FlushPendingShow).ExecuteLater(1);
                return;
            }

            pendingShowWindow = null;
            ShowMenus(window);
        }

        private void FlushPendingShow()
        {
            if (!pendingShowWindow.HasValue)
                return;

            BindTree();
            if (menuRoot == null)
                return;

            JournalWindowId window = pendingShowWindow.Value;
            pendingShowWindow = null;
            ShowMenus(window);
        }
    }
}
