using Project.EditorTools;
using Project.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.EditorTools.UiLayout
{
    public static class PanelUiResetUtility
    {
        [MenuItem(SurvivalPioneerEditorMenus.Ui + "Reset Map UI To Default Layout", false, 120)]
        public static void ResetMapUiToDefaults()
        {
            if (!ConfirmReset(
                    "Reset Map UI",
                    "Rebuild MinimapPanel and FullMapOverlay with default runtime layout? Scene layout edits will be replaced."))
                return;

            PerformMapUiReset(showCompletionDialog: true);
        }

        [MenuItem(SurvivalPioneerEditorMenus.Ui + "Reset Enemy Loot Dialog UI To Default Layout", false, 121)]
        public static void ResetEnemyLootDialogUiToDefaults()
        {
            if (!ConfirmReset(
                    "Reset Loot Dialog UI",
                    Application.isPlaying
                        ? "Rebuild the loot popup with default buttons and layout?"
                        : "Remove any persisted loot dialog objects from the scene and delete saved loot layout profiles?"))
                return;

            PerformEnemyLootDialogReset(showCompletionDialog: true);
        }

        [MenuItem(SurvivalPioneerEditorMenus.Ui + "Reset Quest Giver Dialog UI To Default Layout", false, 122)]
        public static void ResetQuestGiverDialogUiToDefaults()
        {
            if (!ConfirmReset(
                    "Reset Quest Giver Dialog UI",
                    Application.isPlaying
                        ? "Rebuild the quest giver popup with default compact layout?"
                        : "Remove any persisted quest giver dialog objects from the scene and delete saved layout profiles?"))
                return;

            PerformQuestGiverDialogReset(showCompletionDialog: true);
        }

        [MenuItem(SurvivalPioneerEditorMenus.Ui + "Reset Journal UI To Default Layout", false, 123)]
        public static void ResetJournalUiToDefaults()
        {
            if (!ConfirmReset(
                    "Reset Journal UI",
                    "Delete saved journal layout profiles and rebuild the journal overlay, tab rail, and window host with default layout?"))
                return;

            PerformJournalUiReset(showCompletionDialog: true);
        }

        [MenuItem(SurvivalPioneerEditorMenus.Ui + "Reset Map & Loot UI To Default Layout", false, 123)]
        public static void ResetMapAndLootUiToDefaults()
        {
            if (!ConfirmReset(
                    "Reset Map & Loot UI",
                    "Reset both the world map shells and the enemy loot popup to their default layouts?"))
                return;

            PerformMapUiReset(showCompletionDialog: false);
            PerformEnemyLootDialogReset(showCompletionDialog: false);

            EditorUtility.DisplayDialog(
                "UI Studio",
                "Map and loot dialog UI were reset to default layouts. Enter Play Mode to verify both panels.",
                "OK");
        }

        private static bool ConfirmReset(string title, string message)
        {
            return EditorUtility.DisplayDialog(title, message, "Reset", "Cancel");
        }

        private static void PerformMapUiReset(bool showCompletionDialog)
        {
            MapUI mapUi = Object.FindAnyObjectByType<MapUI>(FindObjectsInactive.Include);
            if (mapUi == null)
            {
                EditorUtility.DisplayDialog("UI Studio", "MapUI was not found in open scenes.", "OK");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(mapUi.gameObject, "Reset Map UI Layout");
            UiLayoutEditorPanelRegistry.SetSerializedBoolIfExists(mapUi, "preserveManualLayout", false);
            UiLayoutEditorPanelRegistry.SetSerializedBoolIfExists(mapUi, "applyRuntimeLayout", true);

            SerializedObject serializedMapUi = new SerializedObject(mapUi);
            ClearSerializedProfile(serializedMapUi, "minimapLayoutProfile");
            ClearSerializedProfile(serializedMapUi, "fullMapLayoutProfile");
            SerializedProperty applyProfiles = serializedMapUi.FindProperty("applyLayoutProfiles");
            if (applyProfiles != null)
                applyProfiles.boolValue = false;
            serializedMapUi.ApplyModifiedPropertiesWithoutUndo();

            DeleteLayoutProfileAsset(UiPanelIds.Minimap);
            DeleteLayoutProfileAsset(UiPanelIds.MapFull);

            mapUi.EnsureLayoutShells();

            EditorUtility.SetDirty(mapUi);
            MarkSceneDirty(mapUi.gameObject.scene);

            if (showCompletionDialog)
            {
                EditorUtility.DisplayDialog(
                    "UI Studio",
                    "Map UI rebuilt with default layout. Enter Play Mode to verify the world map texture and pan/zoom.",
                    "OK");
            }
        }

        private static void PerformEnemyLootDialogReset(bool showCompletionDialog)
        {
            DeleteLayoutProfileAsset(UiPanelIds.EnemyLootDialog);

            if (Application.isPlaying)
            {
                EnemyLootDialogUI.ResetToDefaultLayout();
                if (showCompletionDialog)
                {
                    EditorUtility.DisplayDialog(
                        "UI Studio",
                        "Loot dialog rebuilt with default compact popup layout.",
                        "OK");
                }

                return;
            }

            Undo.SetCurrentGroupName("Reset Enemy Loot Dialog UI");
            int undoGroup = Undo.GetCurrentGroup();

            EnemyLootDialogUI[] dialogs = Object.FindObjectsByType<EnemyLootDialogUI>(FindObjectsInactive.Include);
            for (int i = 0; i < dialogs.Length; i++)
            {
                if (dialogs[i] == null)
                    continue;

                Undo.DestroyObjectImmediate(dialogs[i].gameObject);
            }

            RemoveOrphanLootOverlays();

            Undo.CollapseUndoOperations(undoGroup);
            MarkSceneDirty(SceneManager.GetActiveScene());

            if (showCompletionDialog)
            {
                EditorUtility.DisplayDialog(
                    "UI Studio",
                    "Loot dialog layout reset. Enter Play Mode and open a loot bag to verify the compact popup.",
                    "OK");
            }
        }

        private static void PerformQuestGiverDialogReset(bool showCompletionDialog)
        {
            DeleteLayoutProfileAsset(UiPanelIds.QuestGiverDialog);

            if (Application.isPlaying)
            {
                QuestGiverDialogUI.ResetToDefaultLayout();
                if (showCompletionDialog)
                {
                    EditorUtility.DisplayDialog(
                        "UI Studio",
                        "Quest giver dialog rebuilt with default compact popup layout.",
                        "OK");
                }

                return;
            }

            Undo.SetCurrentGroupName("Reset Quest Giver Dialog UI");
            int undoGroup = Undo.GetCurrentGroup();

            QuestGiverDialogUI[] dialogs = Object.FindObjectsByType<QuestGiverDialogUI>(FindObjectsInactive.Include);
            for (int i = 0; i < dialogs.Length; i++)
            {
                if (dialogs[i] == null)
                    continue;

                Undo.DestroyObjectImmediate(dialogs[i].gameObject);
            }

            RemoveOrphanQuestGiverOverlays();

            Undo.CollapseUndoOperations(undoGroup);
            MarkSceneDirty(SceneManager.GetActiveScene());

            if (showCompletionDialog)
            {
                EditorUtility.DisplayDialog(
                    "UI Studio",
                    "Quest giver dialog layout reset. Enter Play Mode and talk to an NPC to verify the compact popup.",
                    "OK");
            }
        }

        private static void PerformJournalUiReset(bool showCompletionDialog)
        {
            DeleteLayoutProfileAsset(UiPanelIds.JournalOverlay);
            DeleteLayoutProfileAsset(UiPanelIds.JournalTabRail);
            DeleteLayoutProfileAsset(UiPanelIds.JournalWindowHost);

            if (Application.isPlaying)
            {
                JournalPanelUI journal = Object.FindAnyObjectByType<JournalPanelUI>();
                if (journal == null)
                {
                    EditorUtility.DisplayDialog("UI Studio", "JournalPanelUI was not found in Play Mode.", "OK");
                    return;
                }

                journal.ResetToDefaultLayout();
                if (showCompletionDialog)
                {
                    EditorUtility.DisplayDialog(
                        "UI Studio",
                        "Journal UI rebuilt with default layout.",
                        "OK");
                }

                return;
            }

            EditorUtility.DisplayDialog(
                "UI Studio",
                "Journal layout profiles deleted. Enter Play Mode — the journal will rebuild with default chrome on start.",
                "OK");
        }

        private static void RemoveOrphanLootOverlays()
        {
            GameObject mainCanvas = GameObject.Find("MainCanvas");
            if (mainCanvas == null)
                return;

            Transform lootOverlay = mainCanvas.transform.Find("LootOverlay");
            if (lootOverlay != null)
                Undo.DestroyObjectImmediate(lootOverlay.gameObject);

            Transform lootHost = mainCanvas.transform.Find("EnemyLootDialogUI");
            if (lootHost != null)
                Undo.DestroyObjectImmediate(lootHost.gameObject);
        }

        private static void RemoveOrphanQuestGiverOverlays()
        {
            GameObject mainCanvas = GameObject.Find("MainCanvas");
            if (mainCanvas == null)
                return;

            Transform dialogOverlay = mainCanvas.transform.Find("DialogOverlay");
            if (dialogOverlay != null)
                Undo.DestroyObjectImmediate(dialogOverlay.gameObject);

            Transform questHost = mainCanvas.transform.Find("QuestGiverDialogUI");
            if (questHost != null)
                Undo.DestroyObjectImmediate(questHost.gameObject);
        }

        private static void ClearSerializedProfile(SerializedObject serializedObject, string propertyName)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
                property.objectReferenceValue = null;
        }

        private static void DeleteLayoutProfileAsset(string panelId)
        {
            string path = UiLayoutProfileResolver.GetAssetPath(panelId);
            if (AssetDatabase.LoadAssetAtPath<UiLayoutProfile>(path) == null)
                return;

            AssetDatabase.DeleteAsset(path);
        }

        private static void MarkSceneDirty(Scene scene)
        {
            if (scene.IsValid())
                EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
