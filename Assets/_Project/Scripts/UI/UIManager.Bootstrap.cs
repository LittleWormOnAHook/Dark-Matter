using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;
using Project.Achievements;
using Project.Core;
using Project.Pioneers;
using Project.Player;
using Project.Quests;
using Project.Progression;
using Project.Survival;
using Project.Survival.Exposure;

namespace Project.UI
{
    public partial class UIManager
    {
        public const string RuntimeHostName = "DM_UiRuntimeHost";

        /// <summary>
        /// Journal navigator still needs a live UIManager after MainCanvas was removed.
        /// UITK path: plain host, no Canvas / GraphicRaycaster.
        /// </summary>
        public static UIManager EnsureExists()
        {
            UIManager existing = Object.FindAnyObjectByType<UIManager>(FindObjectsInactive.Include);
            if (existing != null)
            {
                if (!existing.gameObject.activeSelf)
                    existing.gameObject.SetActive(true);
                StripUguiChromeIfToolkit(existing.gameObject);
                return existing;
            }

            if (!Application.isPlaying)
                return null;

            GameObject host = FindRuntimeHost();
            if (host == null)
                host = new GameObject(RuntimeHostName);

            host.layer = 5;
            if (!host.activeSelf)
                host.SetActive(true);

            if (DMUiToolkitConfig.IsEnabled)
            {
                StripUguiChromeIfToolkit(host);
            }
            else
            {
                Canvas canvas = host.GetComponent<Canvas>();
                if (canvas == null)
                    canvas = host.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 40;

                if (host.GetComponent<CanvasScaler>() == null)
                {
                    CanvasScaler scaler = host.AddComponent<CanvasScaler>();
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1920f, 1080f);
                }

                if (host.GetComponent<GraphicRaycaster>() == null)
                    host.AddComponent<GraphicRaycaster>();
            }

            existing = host.GetComponent<UIManager>();
            if (existing == null)
                existing = host.AddComponent<UIManager>();
            return existing;
        }

        private static GameObject FindRuntimeHost()
        {
            GameObject named = GameObject.Find(RuntimeHostName);
            if (named != null)
                return named;

            Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] != null && transforms[i].name == RuntimeHostName)
                    return transforms[i].gameObject;
            }

            return null;
        }

        private static void StripUguiChromeIfToolkit(GameObject host)
        {
            if (host == null || !DMUiToolkitConfig.IsEnabled)
                return;

            GraphicRaycaster hostRaycaster = host.GetComponent<GraphicRaycaster>();
            if (hostRaycaster != null)
                hostRaycaster.enabled = false;

            CanvasScaler scaler = host.GetComponent<CanvasScaler>();
            if (scaler != null)
                scaler.enabled = false;

            Canvas canvas = host.GetComponent<Canvas>();
            if (canvas != null)
                canvas.enabled = false;
        }

        private void EnsureJournalPanelUi()
        {
            if (GetComponent<JournalPanelUI>() == null)
                gameObject.AddComponent<JournalPanelUI>();
        }

        private void EnsureProgressionHud()
        {
            GameObject player = PlayerLocator.FindPlayerObject();
            if (player != null)
            {
                if (player.GetComponent<PlayerProgressionManager>() == null)
                    player.AddComponent<PlayerProgressionManager>();
                if (player.GetComponent<ProgressionStatScaler>() == null)
                    player.AddComponent<ProgressionStatScaler>();
                return;
            }

            if (GetComponent<PlayerProgressionManager>() == null)
                gameObject.AddComponent<PlayerProgressionManager>();
        }

        private void EnsureQuestManager()
        {
            QuestManager.EnsureExists();
        }

        private void EnsureAchievementSystems()
        {
            AchievementManager.EnsureExists();
            if (GetComponent<AchievementProgressBridge>() == null)
                gameObject.AddComponent<AchievementProgressBridge>();
        }

        private void EnsureCraftingUi()
        {
            if (GetComponent<CraftingUI>() == null)
                gameObject.AddComponent<CraftingUI>();
        }

        private void EnsurePeakScreenUi()
        {
            if (DMUiToolkitConfig.IsEnabled)
                return;

            EnvironmentalCrisisHudMode.EnsureExists(transform);
        }

        private void EnsurePickupProximityDotUi()
        {
            if (GetComponent<PickupProximityDotUI>() == null)
                gameObject.AddComponent<PickupProximityDotUI>();
        }

        private void EnsureWorldInteractionDotUi()
        {
            if (GetComponent<WorldInteractionDotUI>() == null)
                gameObject.AddComponent<WorldInteractionDotUI>();
        }

        private void EnsurePickupAimReticleUi()
        {
            if (GetComponent<PickupAimReticleUI>() == null)
                gameObject.AddComponent<PickupAimReticleUI>();

            if (GetComponent<HovercraftTurretReticleUI>() == null)
                gameObject.AddComponent<HovercraftTurretReticleUI>();
        }

        private void EnsureShiftHudBootstrap()
        {
            if (GetComponent<ShiftHudBootstrap>() == null)
                gameObject.AddComponent<ShiftHudBootstrap>();
        }

        private void EnsureMapUi()
        {
            if (GetComponent<MapUI>() == null)
                gameObject.AddComponent<MapUI>();
        }

        private void EnsureToolBarUi()
        {
            if (GetComponent<ToolBarUI>() == null)
                gameObject.AddComponent<ToolBarUI>();
        }

        private void EnsureGameplayUiHelpers()
        {
            PickupToastUI.EnsureExists(transform);
            XpToastUI.EnsureExists(transform);
            DMILevelUpPopupUI.EnsureExists(transform);
            AchievementUnlockPopupUI.EnsureExists(transform);
            AchievementProgressBridge.EnsureExists();
            QuestGiverDialogUI.EnsureExists(transform);
            ActiveQuestHudUI.EnsureExists(transform);
            EngagedEnemyHealthHud.EnsureExists(transform);
            WeaponModeSwitchMenuUI.EnsureExists(transform);
            UiFrontLayer.Get(transform);
        }

        private void EnsureProgressionLevelUpFeedback()
        {
            trackedProgression = PlayerProgressionManager.EnsureExists();
            if (trackedProgression == null)
                return;

            trackedProgression.OnLevelUp -= HandleProgressionLevelUp;
            trackedProgression.OnLevelUp += HandleProgressionLevelUp;
        }

        private void EnsureCombatUiReady()
        {
            if (combatPopupParent == null)
                combatPopupParent = popupParent != null ? popupParent : transform;

            if (floatingDamagePrefab == null)
            {
                floatingDamagePrefab = Resources.Load<GameObject>("Combat/FloatingDamageNumber");
#if UNITY_EDITOR
                if (floatingDamagePrefab == null)
                {
                    floatingDamagePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                        "Assets/_Project/Prefabs/UI/FloatingDamageNumber.prefab");
                }
#endif
            }
        }

        private void EnsureOxygenDeprivationFx()
        {
            if (GetComponent<OxygenDeprivationFx>() == null)
                gameObject.AddComponent<OxygenDeprivationFx>();
        }

        private void EnsureSurvivalPanelBinder()
        {
            ResolveSurvivalUiReferences();

            if (healthSlider == null)
                return;

            Transform current = healthSlider.transform;
            while (current != null && current.name != "SurvivalStatsPanel")
                current = current.parent;

            if (current == null)
                return;

            if (current.GetComponent<SurvivalStatsPanelBinder>() == null)
                current.gameObject.AddComponent<SurvivalStatsPanelBinder>();

        }

        private void EnsureInteractionPrompt()
        {
            if (DMUiToolkitConfig.IsEnabled)
                return;

            if (interactionPrompt != null)
                return;

            Transform existing = transform.Find("InteractionPrompt");
            if (existing != null)
            {
                interactionPrompt = existing.GetComponent<TextMeshProUGUI>();
                if (interactionPrompt != null)
                    return;
            }

            GameObject promptObject = new GameObject("InteractionPrompt", typeof(RectTransform));
            promptObject.transform.SetParent(transform, false);
            interactionPrompt = promptObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(interactionPrompt);
            ShiftUiTheme theme = ShiftUiTheme.Current;
            if (theme != null)
                theme.ApplyFont(interactionPrompt, semiBold: true);
            interactionPrompt.raycastTarget = false;
            interactionPrompt.gameObject.SetActive(false);
        }

    }
}
