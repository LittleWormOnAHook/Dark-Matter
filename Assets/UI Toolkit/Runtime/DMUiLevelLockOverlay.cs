using Project.Data;
using Project.Progression;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// Level-lock look (loot plan 8.8, D3/D22): the vendor blocked overlay (<c>.dmg-vendor-block</c>,
    /// Deep Magenta wash) on any slot whose item is above the player's equip or use level.
    /// The overlay child is created on first use and only toggled after that.
    /// </summary>
    public static class DMUiLevelLockOverlay
    {
        public const string BlockClass = "dmg-vendor-block";
        private const string BlockName = "level-lock-block";

        /// <summary>Shows the overlay on <paramref name="host"/> when <paramref name="item"/> is level-locked.</summary>
        public static void Apply(VisualElement host, ItemData item, int playerLevel)
        {
            SetLocked(host, item != null && LevelUnlockUtility.IsLevelLocked(item, playerLevel));
        }

        public static void SetLocked(VisualElement host, bool locked)
        {
            if (host == null)
                return;

            VisualElement block = FindBlock(host);
            if (block == null)
            {
                if (!locked)
                    return;

                block = new VisualElement { name = BlockName };
                block.AddToClassList(BlockClass);
                block.pickingMode = PickingMode.Ignore;
                host.Add(block);
            }

            // Keep the wash above icon and amount when children were added after it.
            if (locked && block.parent == host && host.IndexOf(block) != host.childCount - 1)
                block.BringToFront();

            DMUiToolkitOverlayDocument.SetShown(block, locked);
        }

        private static VisualElement FindBlock(VisualElement host)
        {
            for (int i = 0; i < host.childCount; i++)
            {
                VisualElement child = host[i];
                if (child != null && child.name == BlockName)
                    return child;
            }

            return null;
        }
    }
}
