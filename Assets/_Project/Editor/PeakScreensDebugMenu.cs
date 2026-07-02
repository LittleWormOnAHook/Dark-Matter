using Project.Companions;
using Project.Echoes;
using Project.Pioneers;
using Project.UI;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    public static class PeakScreensDebugMenu
    {
        private const string CrisisMenuPath = "Tools/Survival Pioneer/Debug/Toggle Sulfur Crisis HUD";
        private const string EchoMenuPath = "Tools/Survival Pioneer/Debug/Show Echo Rescue Reveal (Test)";
        private const string SpawnEchoMenuPath = "Tools/Survival Pioneer/Debug/Spawn Test Echo Signal";
        private const string RefreshTrioMenuPath = "Tools/Survival Pioneer/Debug/Refresh Expedition Trio Companions";

        [MenuItem(CrisisMenuPath)]
        public static void ToggleSulfurCrisisHud()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[PeakScreensDebug] Enter Play mode to toggle crisis HUD.");
                return;
            }

            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[PeakScreensDebug] No Canvas found.");
                return;
            }

            EnvironmentalCrisisHudMode crisis = EnvironmentalCrisisHudMode.EnsureExists(canvas.transform);
            bool next = !EnvironmentalCrisisHudMode.IsCrisisActive;
            crisis.SetCrisisActive(next);
            Debug.Log(next ? "[PeakScreensDebug] Crisis HUD enabled." : "[PeakScreensDebug] Crisis HUD disabled.");
        }

        [MenuItem(CrisisMenuPath, true)]
        private static bool ToggleSulfurCrisisHudValidate() => Application.isPlaying;

        [MenuItem(EchoMenuPath)]
        public static void ShowEchoRescueRevealTest()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[PeakScreensDebug] Enter Play mode to preview Echo rescue reveal.");
                return;
            }

            EchoRescueRevealUI.Show(
                "Sulfur-Blooded Kael-9",
                "Infiltrator Scout — Io Hybrid",
                "Phase Step: brief sprint through sulfur vents without stamina drain.");
        }

        [MenuItem(EchoMenuPath, true)]
        private static bool ShowEchoRescueRevealTestValidate() => Application.isPlaying;

        [MenuItem(SpawnEchoMenuPath)]
        public static void SpawnTestEchoSignal()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[PeakScreensDebug] Enter Play mode to spawn a test echo signal.");
                return;
            }

            EchoSignalSpawner spawner = Object.FindAnyObjectByType<EchoSignalSpawner>();
            if (spawner == null)
            {
                GameObject host = new GameObject("EchoSignalSpawner");
                spawner = host.AddComponent<EchoSignalSpawner>();
            }

            EchoWorldEntity entity = spawner.SpawnTestSignalNearPlayer();
            if (entity == null)
            {
                Debug.LogWarning("[PeakScreensDebug] Failed to spawn test echo signal.");
                return;
            }

            Debug.Log($"[PeakScreensDebug] Spawned echo signal: {entity.SignalRecord.displayName}");
        }

        [MenuItem(SpawnEchoMenuPath, true)]
        private static bool SpawnTestEchoSignalValidate() => Application.isPlaying;

        [MenuItem(RefreshTrioMenuPath)]
        public static void RefreshExpeditionTrioCompanions()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[PeakScreensDebug] Enter Play mode to refresh expedition trio companions.");
                return;
            }

            PioneerRosterManager roster = PioneerRosterManager.EnsureExists();
            roster.EnsureDefaultTrioIfNeededPublic();

            CompanionRosterBridge bridge = Object.FindAnyObjectByType<CompanionRosterBridge>();
            if (bridge == null)
            {
                GameObject host = new GameObject("CompanionRosterBridge");
                bridge = host.AddComponent<CompanionRosterBridge>();
            }

            bridge.RefreshCompanions();
            Debug.Log($"[PeakScreensDebug] Refreshed {bridge.ActiveCompanions.Count} expedition companions.");
        }

        [MenuItem(RefreshTrioMenuPath, true)]
        private static bool RefreshExpeditionTrioCompanionsValidate() => Application.isPlaying;
    }
}
