using Project.Core;
using Project.EditorTools;
using Project.Player;
using Project.World;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Player
{
    /// <summary>
    /// After Play Mode: re-apply World → Terrain Loading and restore the live player bind pose in Scene view.
    /// </summary>
    [InitializeOnLoad]
    public static class DMPlayerEditModeWorldHooks
    {
        private const string ResetPoseMenu =
            DarkMatterGenesisEditorMenus.Player + "Reset Live Player Edit-Mode Bind Pose";

        static DMPlayerEditModeWorldHooks()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.delayCall += ApplyTerrainAuthorityInEditMode;
        }

        private static void ApplyTerrainAuthorityInEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            DmGaiaTerrainStreamingGate.ApplyFromProfiles();
        }

        [MenuItem(ResetPoseMenu, false, Project.EditorTools.DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Player_Reset_Live_Player_Edit_Mode_Bind_Pose)]
        public static void ResetLivePlayerBindPoseMenu()
        {
            if (ResetLivePlayerBindPose())
                Debug.Log("[DMPlayerEditModeWorldHooks] Restored live player edit-mode bind pose.");
            else
                Debug.LogWarning("[DMPlayerEditModeWorldHooks] No live player found to restore.");
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode)
                return;

            EditorApplication.delayCall += () =>
            {
                DmGaiaTerrainStreamingGate.ApplyFromProfiles();
                ResetLivePlayerBindPose();
                // Gaia bind / loader refresh can run on the next editor tick — re-apply once more.
                EditorApplication.delayCall += () => DmGaiaTerrainStreamingGate.ApplyFromProfiles();
            };
        }

        private static bool ResetLivePlayerBindPose()
        {
            GameObject player = PlayerLocator.FindPlayerObject();
            if (player == null)
                return false;

            PlayerPrefabVisualSetupUtility.RepairEditModeAnimator(player);
            return true;
        }
    }
}
