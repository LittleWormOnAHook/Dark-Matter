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
                    // Starter select is its own full-screen step. Do not call ShowMainMenu —
                    // that bounced New Expedition back to the menu under UITK.
                    ApplyStarterSelectPhase();
                    break;
                case GamePhase.MainMenu:
                default:
                    ApplyMainMenuPhase();
                    break;
            }
        }

        private static bool UitkOwnsGameplayHud =>
            DMUiToolkitConfig.IsEnabled && DMUiToolkitHud.IsDriving;

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

            // UITK owns HUD visuals. Keep InventoryUI/ToolBarUI hosts; do not re-enable retired Graphics.
            if (UitkOwnsGameplayHud)
                DisableSceneGameplayHudVisuals();
        }

        public static void SetSceneGameplayHudRootsActive(bool active)
        {
            if (active && UitkOwnsGameplayHud)
            {
                DisableSceneGameplayHudVisuals();
                return;
            }

            Transform canvasRoot = MainMenuController.ResolveMainCanvas()?.transform;
            if (canvasRoot == null)
                return;

            for (int i = 0; i < SceneGameplayHudRoots.Length; i++)
            {
                Transform child = FindSceneHudRoot(canvasRoot, SceneGameplayHudRoots[i]);
                if (child != null)
                    child.gameObject.SetActive(active);
            }
        }

        private static void DisableSceneGameplayHudVisuals()
        {
            Transform canvasRoot = MainMenuController.ResolveMainCanvas()?.transform;
            if (canvasRoot == null)
                return;

            for (int i = 0; i < SceneGameplayHudRoots.Length; i++)
            {
                Transform child = FindSceneHudRoot(canvasRoot, SceneGameplayHudRoots[i]);
                if (child != null)
                    DMUiToolkitOverlayDocument.DisableUguiVisuals(child.gameObject);
            }
        }

        private static Transform FindSceneHudRoot(Transform canvasRoot, string name)
        {
            if (canvasRoot == null)
                return null;

            Transform child = canvasRoot.Find(name);
            if (child != null)
                return child;

            for (int c = 0; c < canvasRoot.childCount; c++)
            {
                Transform candidate = canvasRoot.GetChild(c);
                if (candidate.name == name)
                    return candidate;
            }

            return null;
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

        private static void ApplyStarterSelectPhase()
        {
            SetSceneGameplayHudRootsActive(false);
            HideGameplayChrome();
            MainMenuController menu = Object.FindAnyObjectByType<MainMenuController>();
            menu?.HideMenuChrome();
        }

        private static void ApplyGameplayPhase()
        {
            MainMenuController menu = Object.FindAnyObjectByType<MainMenuController>();
            menu?.HideMenuChrome();

            if (UitkOwnsGameplayHud)
                DisableSceneGameplayHudVisuals();
            else
                SetSceneGameplayHudRootsActive(true);

            MainMenuController.RestoreGameplayUiFromMenu();

            InventoryUI inventory = Object.FindAnyObjectByType<InventoryUI>(FindObjectsInactive.Include);
            if (inventory != null && !inventory.gameObject.activeSelf)
                inventory.gameObject.SetActive(true);
            inventory?.SetBottomHudVisible(true);
            inventory?.EnsureSurvivalStatsHudVisible();

            ToolBarUI toolbar = Object.FindAnyObjectByType<ToolBarUI>(FindObjectsInactive.Include);
            if (toolbar != null && !toolbar.gameObject.activeSelf)
                toolbar.gameObject.SetActive(true);
            if (!UitkOwnsGameplayHud)
                toolbar?.SetGameplayVisible(true);

            if (!UitkOwnsGameplayHud)
            {
                CondensedSurvivalStatsHud statsHud = Object.FindAnyObjectByType<CondensedSurvivalStatsHud>(FindObjectsInactive.Include);
                statsHud?.RefreshLayout();

                HotbarXpHud xpHud = Object.FindAnyObjectByType<HotbarXpHud>(FindObjectsInactive.Include);
                if (xpHud == null)
                {
                    Canvas canvas = MainMenuController.ResolveMainCanvas()
                        ?? Object.FindAnyObjectByType<Canvas>();
                    if (canvas != null)
                        xpHud = HotbarXpHud.EnsureExists(canvas.transform);
                }
                xpHud?.SetVisible(true);

                ActiveQuestHudUI questHud = Object.FindAnyObjectByType<ActiveQuestHudUI>(FindObjectsInactive.Include);
                if (questHud == null)
                {
                    Canvas canvas = MainMenuController.ResolveMainCanvas()
                        ?? Object.FindAnyObjectByType<Canvas>();
                    if (canvas != null)
                        questHud = ActiveQuestHudUI.EnsureExists(canvas.transform);
                }
                else
                {
                    questHud.SetGameplayVisible(true);
                }
            }

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
            Object.FindAnyObjectByType<InventoryUI>(FindObjectsInactive.Include)?.EnsureSurvivalStatsHudVisible();
            if (!UitkOwnsGameplayHud)
            {
                Object.FindAnyObjectByType<HotbarXpHud>(FindObjectsInactive.Include)?.SetVisible(true);
                Object.FindAnyObjectByType<ActiveQuestHudUI>(FindObjectsInactive.Include)?.SetGameplayVisible(true);
            }
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
