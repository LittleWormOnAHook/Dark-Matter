using System.Collections.Generic;
using System.IO;
using Project.Data;
using Project.Quests;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    public static class QuestGiverSetup
    {
        private const string PrefabPath = ProjectAssetPaths.QuestGiverPrefab;

        [MenuItem(SurvivalPioneerEditorMenus.Quests + "Quest Giver NPC", false, 10)]
        public static void PlaceQuestGiverNpc()
        {
            GameObject existing = GameObject.Find("QuestGiver_PioneerGuide");
            if (existing != null)
            {
                Selection.activeGameObject = existing;
                EditorGUIUtility.PingObject(existing);
                Debug.Log("QuestGiver_PioneerGuide already exists in the scene.");
                return;
            }

            Vector3 spawnPosition = Vector3.zero;
            if (Selection.activeTransform != null)
                spawnPosition = Selection.activeTransform.position;
            else if (Camera.main != null)
                spawnPosition = Camera.main.transform.position + Camera.main.transform.forward * 4f;

            GameObject root = new GameObject("QuestGiver_PioneerGuide");
            root.transform.position = new Vector3(spawnPosition.x, spawnPosition.y + 1f, spawnPosition.z);

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = Vector3.up;
            Object.DestroyImmediate(body.GetComponent<Collider>());

            CapsuleCollider trigger = root.AddComponent<CapsuleCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 1f, 0f);
            trigger.radius = 1.2f;
            trigger.height = 2.4f;

            root.AddComponent<QuestGiverNpc>();

            EnsurePrefabFolder();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);

            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);
            Undo.RegisterCreatedObjectUndo(root, "Create Quest Giver NPC");
            EditorSceneManagerMarkDirty();

            Debug.Log("Created QuestGiver_PioneerGuide. Talk with E near the NPC to accept and turn in the Supply Run quest.");
        }

        private static void EnsurePrefabFolder()
        {
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsNpcs);
        }

        private static void EditorSceneManagerMarkDirty()
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }
    }

    public static class QuestEditorUtility
    {
        public const string QuestsFolder = ProjectAssetPaths.ResourcesQuests;
        public const string QuestRegistryPath = ProjectAssetPaths.QuestRegistry;

        public static void EnsureFolder(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || AssetDatabase.IsValidFolder(folderPath))
                return;

            folderPath = folderPath.Replace('\\', '/');
            string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent))
                EnsureFolder(parent);

            string folderName = Path.GetFileName(folderPath);
            if (string.IsNullOrEmpty(folderName) || string.IsNullOrEmpty(parent))
                return;

            if (!AssetDatabase.IsValidFolder(folderPath))
                AssetDatabase.CreateFolder(parent, folderName);
        }

        public static string SanitizeAssetName(string rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName))
                return string.Empty;

            char[] invalid = Path.GetInvalidFileNameChars();
            string sanitized = rawName.Trim();
            foreach (char character in invalid)
                sanitized = sanitized.Replace(character, '_');

            return sanitized.Replace('/', '_').Replace('\\', '_');
        }

        public static QuestDefinition[] LoadAllQuestAssets()
        {
            EnsureFolder(QuestsFolder);
            string[] guids = AssetDatabase.FindAssets("t:QuestDefinition", new[] { QuestsFolder });
            List<QuestDefinition> quests = new List<QuestDefinition>(guids.Length);

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                QuestDefinition quest = AssetDatabase.LoadAssetAtPath<QuestDefinition>(path);
                if (quest != null && path != QuestRegistryPath)
                    quests.Add(quest);
            }

            quests.Sort((a, b) => string.Compare(a != null ? a.title : string.Empty, b != null ? b.title : string.Empty, System.StringComparison.OrdinalIgnoreCase));
            return quests.ToArray();
        }

        public static ItemData[] LoadAllItems()
        {
            return CraftingEditorUtility.LoadAllItems();
        }

        public static QuestDefinition SaveQuestAsset(QuestDefinition source, string assetFileName)
        {
            if (source == null)
                return null;

            string safeName = SanitizeAssetName(assetFileName);
            if (string.IsNullOrEmpty(safeName))
                return null;

            EnsureFolder(QuestsFolder);
            string path = $"{QuestsFolder}/{safeName}.asset";

            QuestDefinition existing = AssetDatabase.LoadAssetAtPath<QuestDefinition>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(source, path);
                existing = source;
            }
            else
            {
                existing.questId = source.questId;
                existing.title = source.title;
                existing.description = source.description;
                existing.objectives = source.objectives != null
                    ? new List<QuestObjectiveDefinition>(source.objectives)
                    : new List<QuestObjectiveDefinition>();
                existing.rewards = source.rewards != null
                    ? new List<QuestRewardDefinition>(source.rewards)
                    : new List<QuestRewardDefinition>();
            }

            EditorUtility.SetDirty(existing);
            return existing;
        }

        public static void AddQuestToRegistry(QuestDefinition quest)
        {
            if (quest == null)
                return;

            QuestRegistry registry = AssetDatabase.LoadAssetAtPath<QuestRegistry>(QuestRegistryPath);
            if (registry == null)
            {
                Debug.LogWarning($"QuestEditorUtility: Registry not found at {QuestRegistryPath}");
                return;
            }

            SerializedObject serialized = new SerializedObject(registry);
            SerializedProperty questsProperty = serialized.FindProperty("quests");
            if (questsProperty == null)
                return;

            for (int i = 0; i < questsProperty.arraySize; i++)
            {
                QuestDefinition existing = questsProperty.GetArrayElementAtIndex(i).objectReferenceValue as QuestDefinition;
                if (existing == quest)
                {
                    serialized.ApplyModifiedProperties();
                    return;
                }
            }

            questsProperty.InsertArrayElementAtIndex(questsProperty.arraySize);
            questsProperty.GetArrayElementAtIndex(questsProperty.arraySize - 1).objectReferenceValue = quest;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(registry);
        }

        public static void DrawObjectiveListEditor(ref List<QuestObjectiveDefinition> objectives)
        {
            if (objectives == null)
                objectives = new List<QuestObjectiveDefinition>();

            EditorGUILayout.LabelField("Objectives", EditorStyles.boldLabel);
            for (int i = 0; i < objectives.Count; i++)
            {
                QuestObjectiveDefinition objective = objectives[i];
                if (objective == null)
                {
                    objectives[i] = new QuestObjectiveDefinition();
                    objective = objectives[i];
                }

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Objective {i + 1}", EditorStyles.boldLabel);
                if (GUILayout.Button("Remove", GUILayout.Width(70f)))
                {
                    objectives.RemoveAt(i);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }
                EditorGUILayout.EndHorizontal();

                objective.type = (QuestObjectiveType)EditorGUILayout.EnumPopup("Type", objective.type);
                objective.targetId = EditorGUILayout.TextField("Target Id", objective.targetId);
                objective.requiredCount = Mathf.Max(1, EditorGUILayout.IntField("Required Count", objective.requiredCount));
                objective.description = EditorGUILayout.TextField("Description", objective.description);

                switch (objective.type)
                {
                    case QuestObjectiveType.CollectItem:
                        EditorGUILayout.HelpBox("Target Id = item asset name or itemName (e.g. Mushroom).", MessageType.None);
                        break;
                    case QuestObjectiveType.CraftItem:
                        EditorGUILayout.HelpBox("Target Id = output item name, recipe id, or recipe output item asset name.", MessageType.None);
                        break;
                    case QuestObjectiveType.ReachLocation:
                        EditorGUILayout.HelpBox("Target Id = QuestLocationTrigger locationId.", MessageType.None);
                        break;
                    case QuestObjectiveType.TalkToNpc:
                        EditorGUILayout.HelpBox("Target Id = QuestGiverNpc npcId.", MessageType.None);
                        break;
                    case QuestObjectiveType.Custom:
                        EditorGUILayout.HelpBox("Target Id = activity key. Call QuestManager.NotifyActivity(id, amount) from gameplay code.", MessageType.None);
                        break;
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(4f);
            }

            if (GUILayout.Button("Add Objective", GUILayout.Height(24f)))
                objectives.Add(new QuestObjectiveDefinition());
        }

        public static void DrawRewardListEditor(ref List<QuestRewardDefinition> rewards, ItemData[] itemOptions)
        {
            if (rewards == null)
                rewards = new List<QuestRewardDefinition>();

            EditorGUILayout.LabelField("Rewards", EditorStyles.boldLabel);
            for (int i = 0; i < rewards.Count; i++)
            {
                QuestRewardDefinition reward = rewards[i];
                if (reward == null)
                {
                    rewards[i] = new QuestRewardDefinition();
                    reward = rewards[i];
                }

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Reward {i + 1}", EditorStyles.boldLabel);
                if (GUILayout.Button("Remove", GUILayout.Width(70f)))
                {
                    rewards.RemoveAt(i);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }
                EditorGUILayout.EndHorizontal();

                reward.type = (QuestRewardType)EditorGUILayout.EnumPopup("Type", reward.type);
                reward.amount = Mathf.Max(1, EditorGUILayout.IntField("Amount", reward.amount));

                if (reward.type == QuestRewardType.Item)
                    reward.item = (ItemData)EditorGUILayout.ObjectField("Item", reward.item, typeof(ItemData), false);
                else if (reward.type == QuestRewardType.StatUpgrade)
                    reward.statUpgradeId = EditorGUILayout.TextField("Stat Upgrade Id", reward.statUpgradeId);

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(4f);
            }

            if (GUILayout.Button("Add Reward", GUILayout.Height(24f)))
                rewards.Add(new QuestRewardDefinition { type = QuestRewardType.Pi, amount = 10 });
        }
    }

    public class QuestCreatorWindow : EditorWindow
    {
        private QuestDefinition[] questAssets = System.Array.Empty<QuestDefinition>();
        private ItemData[] itemOptions = System.Array.Empty<ItemData>();
        private int selectedQuestIndex = -1;

        private string questId = "new_quest";
        private string questTitle = "New Quest";
        private string description = string.Empty;
        private string assetFileName = "new_quest";
        private bool addToQuestRegistry = true;
        private List<QuestObjectiveDefinition> objectives = new List<QuestObjectiveDefinition>();
        private List<QuestRewardDefinition> rewards = new List<QuestRewardDefinition>();

        private Vector2 listScroll;
        private Vector2 editorScroll;

        [MenuItem(SurvivalPioneerEditorMenus.Quests + "Quest Creator", false, 0)]
        public static void Open()
        {
            GetWindow<QuestCreatorWindow>("Quest Creator").minSize = new Vector2(780f, 560f);
        }

        private void OnEnable()
        {
            RefreshLists();
        }

        private void RefreshLists()
        {
            questAssets = QuestEditorUtility.LoadAllQuestAssets();
            itemOptions = QuestEditorUtility.LoadAllItems();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Quest Creator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Author quests with objectives (collect, craft, talk, location, custom activity) and rewards (Pi, items). " +
                "Assign saved quests to QuestGiverNpc quest offers in the Inspector.",
                MessageType.Info);
            EditorGUILayout.Space(6f);

            EditorGUILayout.BeginHorizontal();
            DrawQuestListPanel();
            DrawQuestEditorPanel();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawQuestListPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(240f));
            EditorGUILayout.LabelField("Quests", EditorStyles.boldLabel);

            listScroll = EditorGUILayout.BeginScrollView(listScroll, GUILayout.ExpandHeight(true));
            for (int i = 0; i < questAssets.Length; i++)
            {
                QuestDefinition quest = questAssets[i];
                if (quest == null)
                    continue;

                string label = string.IsNullOrEmpty(quest.title) ? quest.name : quest.title;
                bool selected = i == selectedQuestIndex;
                if (GUILayout.Toggle(selected, label, "Button") && selectedQuestIndex != i)
                    LoadQuest(quest, i);
            }
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("New Quest", GUILayout.Height(28f)))
                StartNewQuest();

            if (GUILayout.Button("Refresh List", GUILayout.Height(24f)))
                RefreshLists();

            EditorGUILayout.EndVertical();
        }

        private void DrawQuestEditorPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            editorScroll = EditorGUILayout.BeginScrollView(editorScroll);

            questId = EditorGUILayout.TextField("Quest Id", questId);
            assetFileName = EditorGUILayout.TextField("Asset File Name", assetFileName);
            questTitle = EditorGUILayout.TextField("Title", questTitle);
            description = EditorGUILayout.TextArea(description, GUILayout.MinHeight(56f));
            addToQuestRegistry = EditorGUILayout.Toggle("Add To Quest Registry", addToQuestRegistry);

            EditorGUILayout.Space(8f);
            QuestEditorUtility.DrawObjectiveListEditor(ref objectives);

            EditorGUILayout.Space(8f);
            QuestEditorUtility.DrawRewardListEditor(ref rewards, itemOptions);

            EditorGUILayout.Space(12f);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Quest", GUILayout.Height(34f)))
                SaveCurrentQuest();

            using (new EditorGUI.DisabledScope(selectedQuestIndex < 0 || selectedQuestIndex >= questAssets.Length))
            {
                if (GUILayout.Button("Delete Quest", GUILayout.Height(34f)))
                    DeleteSelectedQuest();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void StartNewQuest()
        {
            selectedQuestIndex = -1;
            questId = "new_quest";
            assetFileName = "new_quest";
            questTitle = "New Quest";
            description = string.Empty;
            objectives = new List<QuestObjectiveDefinition>();
            rewards = new List<QuestRewardDefinition> { new QuestRewardDefinition { type = QuestRewardType.Pi, amount = 25 } };
            addToQuestRegistry = true;
            Repaint();
        }

        private void LoadQuest(QuestDefinition quest, int index)
        {
            selectedQuestIndex = index;
            questId = quest.ResolvedId;
            assetFileName = quest.name;
            questTitle = quest.title;
            description = quest.description;
            objectives = quest.objectives != null
                ? new List<QuestObjectiveDefinition>(quest.objectives)
                : new List<QuestObjectiveDefinition>();
            rewards = quest.rewards != null
                ? new List<QuestRewardDefinition>(quest.rewards)
                : new List<QuestRewardDefinition>();
            Repaint();
        }

        private void SaveCurrentQuest()
        {
            if (string.IsNullOrWhiteSpace(questId) || string.IsNullOrWhiteSpace(questTitle))
            {
                EditorUtility.DisplayDialog("Quest Creator", "Quest id and title are required.", "OK");
                return;
            }

            string safeFileName = QuestEditorUtility.SanitizeAssetName(string.IsNullOrWhiteSpace(assetFileName) ? questId : assetFileName);
            if (string.IsNullOrEmpty(safeFileName))
            {
                EditorUtility.DisplayDialog("Quest Creator", "Asset file name is invalid.", "OK");
                return;
            }

            QuestDefinition draft = ScriptableObject.CreateInstance<QuestDefinition>();
            draft.questId = questId.Trim();
            draft.title = questTitle.Trim();
            draft.description = description;
            draft.objectives = new List<QuestObjectiveDefinition>(objectives);
            draft.rewards = new List<QuestRewardDefinition>(rewards);

            QuestDefinition saved = QuestEditorUtility.SaveQuestAsset(draft, safeFileName);
            if (saved == null)
            {
                EditorUtility.DisplayDialog("Quest Creator", "Failed to save quest.", "OK");
                return;
            }

            if (addToQuestRegistry)
                QuestEditorUtility.AddQuestToRegistry(saved);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshLists();

            for (int i = 0; i < questAssets.Length; i++)
            {
                if (questAssets[i] == saved)
                {
                    selectedQuestIndex = i;
                    break;
                }
            }

            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
        }

        private void DeleteSelectedQuest()
        {
            if (selectedQuestIndex < 0 || selectedQuestIndex >= questAssets.Length)
                return;

            QuestDefinition quest = questAssets[selectedQuestIndex];
            if (quest == null)
                return;

            if (!EditorUtility.DisplayDialog("Quest Creator", $"Delete quest asset '{quest.name}'?", "Delete", "Cancel"))
                return;

            string path = AssetDatabase.GetAssetPath(quest);
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.SaveAssets();
            RefreshLists();
            StartNewQuest();
        }
    }
}
