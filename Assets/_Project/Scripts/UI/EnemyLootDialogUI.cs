using Project.AI;
using Project.Events;
using UnityEngine;

namespace Project.UI
{
    /// <summary>
    /// Legacy loot entry point, now a thin forward to the UITK loot window <see cref="DMUiToolkitLoot"/>.
    /// The uGUI Canvas/TMP fallback was removed (loot plan D11). The type stays a MonoBehaviour only so
    /// UI Studio's reset/registry tools that look for stale hosts still compile; it builds nothing.
    /// </summary>
    [AddComponentMenu("")]
    public class EnemyLootDialogUI : MonoBehaviour
    {
        /// <summary>True while the loot window is open (and on the frame it closed).</summary>
        public static bool IsDialogOpen => DMUiToolkitLoot.IsOpenOrClosingThisFrame;

        public static void CloseAnyOpenLoot() => DMUiToolkitLoot.TryHide();

        public static bool Show(IDMLootContainer container) => DMUiToolkitLoot.TryShow(container);

        /// <summary>True while the loot window is open on <paramref name="container"/>.</summary>
        public static bool IsShowing(IDMLootContainer container) => DMUiToolkitLoot.IsShowing(container);

        /// <summary>No uGUI layout any more; closes the window so UI Studio resets stay harmless.</summary>
        public static void ResetToDefaultLayout() => CloseAnyOpenLoot();

        /// <summary>No uGUI dialog to build for the layout editor (UITK window replaces it).</summary>
        public static void EnsureBuiltForLayoutEditor()
        {
        }
    }
}
