using Project.UI;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    public static class PeakScreensDebugMenu
    {
        private const string CrisisMenuPath = "Tools/Survival Pioneer/Debug/Toggle Sulfur Crisis HUD";
        private const string EchoMenuPath = "Tools/Survival Pioneer/Debug/Show Echo Rescue Reveal (Test)";

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
    }
}
