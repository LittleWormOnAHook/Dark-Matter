using Project.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    public partial class DMUiToolkitMenus
    {
        private JournalWindowId? pendingShowWindow;

        /// <summary>True while ForceShow is waiting on menuRoot bind (do not treat as ghost-closed).</summary>
        public static bool HasPendingShow =>
            instance != null && instance.pendingShowWindow.HasValue;

        /// <summary>
        /// Navigator reports open but Toolkit menus were force-hidden / never painted (climb Detach recovery).
        /// Re-paint from the live navigator window — do not CloseAll.
        /// </summary>
        public static void RepairOpenNavigatorChrome()
        {
            if (!DMUiToolkitConfig.IsEnabled || !DMUiToolkitBootstrap.IsRootActive)
                return;

            FullscreenUiNavigator nav = FullscreenUiNavigator.Instance;
            if (nav == null || !nav.IsAnyOpen)
                return;

            JournalWindowId? window = nav.CurrentWindow;
            if (!window.HasValue || !IsToolkitWindow(window))
            {
                // Fall back to journal panel active window if navigator CurrentWindow is unset.
                JournalPanelUI journal = Object.FindAnyObjectByType<JournalPanelUI>(FindObjectsInactive.Include);
                window = journal != null ? journal.ActiveJournalWindow : null;
            }

            if (!window.HasValue || !IsToolkitWindow(window))
                window = JournalWindowId.JournalQuest;

            EnsureHost()?.ForceShow(window.Value);
            GameplayHudVisibility.ClearCinematicChrome();
        }


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

            bool wasOpen = journal.IsOpen;
            JournalWindowId? wasWindow = journal.ActiveJournalWindow;

            bool toggled = journalHotkey
                ? journal.TryToggleJournal()
                : journal.TryToggleTab(windowId);

            if (!toggled)
                return false;

            GameplayHudVisibility.ClearCinematicChrome();

            DMUiToolkitMenus host = EnsureHost();
            if (host == null)
                return true;

            // Close only when the press was a real toggle-off of the open journal/tab.
            bool closedSameTab = wasOpen && (
                journalHotkey
                || (wasWindow.HasValue && wasWindow.Value == windowId));

            if (journal.IsOpen || (!closedSameTab && !wasOpen))
            {
                JournalWindowId show = journal.ActiveJournalWindow ?? windowId;
                host.ForceShow(show);
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
