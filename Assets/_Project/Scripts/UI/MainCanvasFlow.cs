using Project.Core;
using UnityEngine;

namespace Project.UI
{
    /// <summary>
    /// Single authority for MainCanvas visibility by session phase.
    /// Call <see cref="Refresh"/> after UI bootstraps or phase changes.
    /// </summary>
    public static class MainCanvasFlow
    {
        private static readonly string[] SceneGameplayHudRoots =
        {
            "SurvivalStatsPanel",
            "Hotbar",
            "MinimapPanel",
            "PickupProximityDots",
            "WorldInteractionDots",
            "PickupAimReticle"
        };

        public static void Refresh()
        {
            if (!Application.isPlaying)
                return;

            SanitizeCanvasHost(MainMenuController.ResolveMainCanvas());

            if (GameSession.HasStarted)
            {
                ApplyGameplayPhase();
                return;
            }

            switch (GameSession.Phase)
            {
                case GamePhase.StartPopup:
                    ApplyStartPopupPhase();
                    break;
                case GamePhase.StarterPioneerSelect:
                case GamePhase.MainMenu:
                default:
                    ApplyMainMenuPhase();
                    break;
            }
        }

        public static void SanitizeCanvasHost(Canvas canvas)
        {
            if (canvas == null)
                return;

            GameObject host = canvas.gameObject;
            if (!host.activeSelf)
                host.SetActive(true);

            // Legacy bug: banner component was added to the canvas root and disabled the whole tree.
            ExposureZoneEntryBannerUI staleBanner = host.GetComponent<ExposureZoneEntryBannerUI>();
            if (staleBanner != null)
                Object.Destroy(staleBanner);
        }

        public static void SetSceneGameplayHudRootsActive(bool active)
        {
            Transform canvasRoot = MainMenuController.ResolveMainCanvas()?.transform;
            if (canvasRoot == null)
                return;

            for (int i = 0; i < SceneGameplayHudRoots.Length; i++)
            {
                Transform child = canvasRoot.Find(SceneGameplayHudRoots[i]);
                if (child == null)
                {
                    for (int c = 0; c < canvasRoot.childCount; c++)
                    {
                        Transform candidate = canvasRoot.GetChild(c);
                        if (candidate.name == SceneGameplayHudRoots[i])
                        {
                            child = candidate;
                            break;
                        }
                    }
                }

                if (child != null)
                    child.gameObject.SetActive(active);
            }
        }

        private static void ApplyMainMenuPhase()
        {
            SetSceneGameplayHudRootsActive(false);
            HideGameplayChrome();

            MainMenuController menu = Object.FindAnyObjectByType<MainMenuController>();
            if (menu == null)
            {
                MainMenuController.EnsureExists();
                menu = Object.FindAnyObjectByType<MainMenuController>();
            }

            menu?.ShowMainMenu();
        }

        private static void ApplyStartPopupPhase()
        {
            SetSceneGameplayHudRootsActive(false);
            HideGameplayChrome();
        }

        private static void ApplyGameplayPhase()
        {
            MainMenuController menu = Object.FindAnyObjectByType<MainMenuController>();
            menu?.HideMenuChrome();

            SetSceneGameplayHudRootsActive(true);

            MainMenuController.RestoreGameplayUiFromMenu();

            InventoryUI inventory = Object.FindAnyObjectByType<InventoryUI>();
            inventory?.SetBottomHudVisible(true);

            inventory?.EnsureSurvivalStatsHudVisible();

            ToolBarUI toolbar = Object.FindAnyObjectByType<ToolBarUI>();
            toolbar?.SetGameplayVisible(true);

            CondensedSurvivalStatsHud statsHud = Object.FindAnyObjectByType<CondensedSurvivalStatsHud>();
            statsHud?.RefreshLayout();

            UIManager uiManager = Object.FindAnyObjectByType<UIManager>();
            uiManager?.SyncSurvivalBars();
            uiManager?.RefreshSurvivalDisplay();

            GameplayHudVisibility.RefreshGameplayHud();

            // RefreshGameplayHud()'s "blocked" branch force-hides CondensedSurvivalStatsHud's own
            // GameObject whenever it reads MainMenuController.BlocksGameplayHud as true — and nothing
            // else re-shows it afterward. If that flag was still momentarily stale on the exact frame
            // a paused/menu session resumes (Esc to main menu, then back), vitals could get stuck
            // hidden forever. Re-assert once more now that the phase transition is fully settled;
            // this is a no-op if everything was already correct.
            Object.FindAnyObjectByType<InventoryUI>()?.EnsureSurvivalStatsHudVisible();
        }

        private static void HideGameplayChrome()
        {
            GameplayHudVisibility.SetGameplayHudVisible(false);

            ToolBarUI toolbar = Object.FindAnyObjectByType<ToolBarUI>();
            if (toolbar != null)
                toolbar.SetGameplayVisible(false);
        }
    }
}
