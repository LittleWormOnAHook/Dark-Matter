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
            HotbarXpHud.EnsureExists(transform);
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

            if (current.GetComponent<CondensedSurvivalStatsHud>() == null)
                current.gameObject.AddComponent<CondensedSurvivalStatsHud>();
        }

        private void EnsureInteractionPrompt()
        {
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
