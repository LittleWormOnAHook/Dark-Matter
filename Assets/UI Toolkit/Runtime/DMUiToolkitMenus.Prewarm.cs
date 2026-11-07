using System.Collections;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    // Round4: invisible build-only pre-warm of the journal + inventory bodies (UXML bind, slot build, one
    // layout/text pass at opacity 0). Never touches menusVisible / paintedWindow / GameplayMenuTime /
    // cursor / action maps / audio / legacy uGUI; skipped or aborted if the player opens the journal.
    public partial class DMUiToolkitMenus
    {
        private static readonly ProfilerMarker s_pmJournalPrewarm = new ProfilerMarker("DM.Journal.Prewarm");
        private static readonly ProfilerMarker s_pmInventoryPrewarm = new ProfilerMarker("DM.Inventory.Prewarm");
        private bool menusPrewarmed;
        private bool menusPrewarming;

        public static bool IsMenusPrewarmed => instance != null && instance.menusPrewarmed;

        public static void PrewarmJournalAndInventory()
        {
            if (!Application.isPlaying || IsOpen)
                return;
            DMUiToolkitMenus host;
            using (s_pmJournalPrewarm.Auto())
                host = EnsureHost();
            if (host == null || host.menusPrewarmed || host.menusPrewarming || host.menuRoot == null || IsOpen)
                return;
            host.StartCoroutine(host.PrewarmMenusRoutine());
        }

        private bool PrewarmAborted(VisualElement m) => menusVisible || pendingShowWindow.HasValue || menuRoot != m;

        private IEnumerator PrewarmMenusRoutine()
        {
            VisualElement m = menuRoot;
            if (m == null || PrewarmAborted(m))
                yield break;
            menusPrewarming = true;

            StyleFloat prevOpacity = m.style.opacity;
            PickingMode prevPicking = m.pickingMode;
            StyleEnum<DisplayStyle> prevMenuDisplay = m.style.display;
            StyleEnum<DisplayStyle> prevJournal = journalBody != null ? journalBody.style.display : default;
            StyleEnum<DisplayStyle> prevInventory = inventoryBody != null ? inventoryBody.style.display : default;
            StyleEnum<DisplayStyle> prevMap = mapBody != null ? mapBody.style.display : default;

            using (s_pmJournalPrewarm.Auto())
            {
                m.style.opacity = 0f;
                m.pickingMode = PickingMode.Ignore;
                DMUiToolkitOverlayDocument.SetShown(m, true);
                DMUiToolkitOverlayDocument.SetShown(journalBody, true);
                DMUiToolkitOverlayDocument.SetShown(inventoryBody, false);
                DMUiToolkitOverlayDocument.SetShown(mapBody, false);
                ApplyJournalSectionVisibility();
                RefreshJournalContent();
            }
            yield return null;
            yield return null;

            if (!PrewarmAborted(m))
            {
                using (s_pmInventoryPrewarm.Auto())
                {
                    DMUiToolkitOverlayDocument.SetShown(journalBody, false);
                    DMUiToolkitOverlayDocument.SetShown(inventoryBody, true);
                    RefreshInventory();
                }
                yield return null;
                yield return null;
            }

            using (s_pmInventoryPrewarm.Auto())
            {
                // Always restore opacity; only restore visibility/picking if the player did not open the menus meanwhile.
                m.style.opacity = prevOpacity;
                if (!PrewarmAborted(m))
                {
                    if (journalBody != null) journalBody.style.display = prevJournal;
                    if (inventoryBody != null) inventoryBody.style.display = prevInventory;
                    if (mapBody != null) mapBody.style.display = prevMap;
                    m.style.display = prevMenuDisplay;
                    m.pickingMode = prevPicking;
                    DMUiToolkitOverlayDocument.SetShown(m, false); // closed state: menus hidden regardless of USS default
                }
                menusPrewarming = false;
                menusPrewarmed = true;
            }
        }
    }
}
