using Project.Data;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Editor window for creating player prefab variants with Meshy/custom humanoid meshes.
    /// Clones from Player_Invector (template) into a new named prefab — never overwrites the template.
    /// Menu: Tools → Dark Matter Genesis → Prefab Creator → Player Prefab Creator
    /// </summary>
    public class PlayerPrefabCreatorWindow : EditorWindow
    {
        private PlayerVisualDefinition[] definitionAssets = System.Array.Empty<PlayerVisualDefinition>();
        private int selectedDefinitionIndex = -1;
        private PlayerVisualDefinition workingDefinition;

        private GameObject templatePrefab;
        private GameObject humanoidMeshSource;
        private string visualChildName = "Visual";
        private string definitionAssetFileName = "Player_Default";
        private string prefabFileName = "Player_Custom";

        private Vector2 listScroll;
        private Vector2 editorScroll;

        [MenuItem(DarkMatterGenesisEditorMenus.PlayerPrefabCreator + "Repair Player_v7 Prefab", false, 14)]
        public static void RepairPlayerV7Prefab()
        {
            const string path = "Assets/_Project/Prefabs/Players/Player_v7.prefab";
            if (!PlayerPrefabVisualSetupUtility.RepairVisualAtPath(path))
            {
                EditorUtility.DisplayDialog("Player Prefab Creator", $"Could not repair {path}.", "OK");
                return;
            }

            AssetDatabase.SaveAssets();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
            }

            Debug.Log($"[Player Prefab Creator] Repaired {path} (BodySnaps, ragdoll remount, frozen VBOT physics strip).");
        }

        [MenuItem(DarkMatterGenesisEditorMenus.PlayerPrefabCreator, false, 13)]
        public static void Open()
        {
            PlayerPrefabCreatorWindow window = GetWindow<PlayerPrefabCreatorWindow>("Player Prefab Creator");
            window.minSize = new Vector2(780f, 560f);
        }

        private void OnEnable()
        {
            RefreshDefinitionList();
            EnsureWorkingDefinition();
            if (templatePrefab == null)
                templatePrefab = PlayerPrefabVisualSetupUtility.LoadDefaultPlayerPrefab();
        }

        private void RefreshDefinitionList()
        {
            definitionAssets = PlayerPrefabVisualSetupUtility.LoadAllDefinitions();
            if (definitionAssets.Length == 0)
            {
                PlayerVisualDefinition created = PlayerPrefabVisualSetupUtility.EnsureDefaultDefinitionAsset();
                definitionAssets = PlayerPrefabVisualSetupUtility.LoadAllDefinitions();
                if (created != null)
                    LoadDefinition(created, 0);
            }
        }

        private void EnsureWorkingDefinition()
        {
            if (workingDefinition != null)
                return;

            if (definitionAssets != null && definitionAssets.Length > 0 && definitionAssets[0] != null)
            {
                LoadDefinition(definitionAssets[0], 0);
                return;
            }

            StartNewDefinition();
        }

        private void OnGUI()
        {
            EnsureWorkingDefinition();

            EditorGUILayout.LabelField("Player Prefab Creator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Creates a NEW player prefab variant — Player_Invector.prefab is the clone TEMPLATE and is never overwritten.\n" +
                "Assign a Meshy Humanoid Model FBX → set Prefab File Name (e.g. Player_MeshyAndroid) → Create Prefab / Rebuild.\n" +
                "Swaps the root Animator avatar on the new prefab, hides the stock VBOT body (weapons stay), rebinds BodySnaps / " +
                "Drawn_/Holstered_ holders onto the Meshy Visual bones (same as Corrupt Patrol), normalizes hand sockets, " +
                "disables ranged support-hand IK (prevents pretzel arms until grips are retargeted), and repairs PioneerVisual slots. " +
                "Player controller, camera, inventory, health, and input stay intact.\n" +
                "Menu: Tools → Dark Matter Genesis → Prefab Creator → Player Prefab Creator",
                MessageType.Info);
            EditorGUILayout.Space(6f);

            EditorGUILayout.BeginHorizontal();
            DrawDefinitionListPanel();
            DrawEditorPanel();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawDefinitionListPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(220f));
            EditorGUILayout.LabelField("Visual Definitions", EditorStyles.boldLabel);

            listScroll = EditorGUILayout.BeginScrollView(listScroll, GUILayout.ExpandHeight(true));
            for (int i = 0; i < definitionAssets.Length; i++)
            {
                PlayerVisualDefinition asset = definitionAssets[i];
                if (asset == null)
                    continue;

                string label = string.IsNullOrEmpty(asset.displayName) ? asset.name : asset.displayName;
                bool selected = i == selectedDefinitionIndex;
                if (GUILayout.Toggle(selected, label, "Button") && selectedDefinitionIndex != i)
                    LoadDefinition(asset, i);
            }
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("New Definition", GUILayout.Height(28f)))
                StartNewDefinition();

            if (GUILayout.Button("Refresh List", GUILayout.Height(24f)))
                RefreshDefinitionList();

            EditorGUILayout.EndVertical();
        }

        private void DrawEditorPanel()
        {
            editorScroll = EditorGUILayout.BeginScrollView(editorScroll);

            DrawIdentitySection();
            EditorGUILayout.Space(8f);
            DrawTemplateAndOutputSection();
            EditorGUILayout.Space(8f);
            DrawModelSection();
            EditorGUILayout.Space(12f);
            DrawStatusSection();
            EditorGUILayout.Space(8f);
            DrawActionButtons();

            EditorGUILayout.EndScrollView();
        }

        private void DrawIdentitySection()
        {
            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
            workingDefinition.displayName = EditorGUILayout.TextField("Display Name", workingDefinition.displayName);
            prefabFileName = EditorGUILayout.TextField("Prefab File Name", prefabFileName);
            workingDefinition.prefabFileName = prefabFileName;
            definitionAssetFileName = EditorGUILayout.TextField("Definition Asset Name", definitionAssetFileName);
            workingDefinition.notes = EditorGUILayout.TextArea(workingDefinition.notes, GUILayout.MinHeight(40f));

            string outputPath = PlayerPrefabVisualSetupUtility.ResolveOutputPrefabPath(
                prefabFileName, workingDefinition.displayName);
            if (PlayerPrefabVisualSetupUtility.IsProtectedTemplatePath(outputPath))
            {
                EditorGUILayout.HelpBox(
                    "Prefab File Name resolves to Player_Invector — that is the protected template. " +
                    "Rename the output (e.g. Player_MeshyAndroid) before Create / Rebuild.",
                    MessageType.Error);
            }
        }

        private void DrawTemplateAndOutputSection()
        {
            EditorGUILayout.LabelField("Template & Output", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            templatePrefab = (GameObject)EditorGUILayout.ObjectField(
                "Template Prefab",
                templatePrefab,
                typeof(GameObject),
                false);
            if (EditorGUI.EndChangeCheck() && workingDefinition != null)
                workingDefinition.templatePrefab = templatePrefab;

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Use Player_Invector Template", GUILayout.Height(22f)))
            {
                templatePrefab = PlayerPrefabVisualSetupUtility.LoadDefaultPlayerPrefab();
                if (workingDefinition != null)
                    workingDefinition.templatePrefab = templatePrefab;
            }

            if (GUILayout.Button("Ping Template", GUILayout.Height(22f)) && templatePrefab != null)
            {
                Selection.activeObject = templatePrefab;
                EditorGUIUtility.PingObject(templatePrefab);
            }
            EditorGUILayout.EndHorizontal();

            string templatePath = PlayerPrefabVisualSetupUtility.ResolveTemplatePath(templatePrefab);
            EditorGUILayout.LabelField("Template: " + templatePath, EditorStyles.miniLabel);

            string outputPath = PlayerPrefabVisualSetupUtility.ResolveOutputPrefabPath(
                prefabFileName, workingDefinition.displayName);
            EditorGUILayout.LabelField("Output: " + outputPath, EditorStyles.miniLabel);

            GameObject outputPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(outputPath);
            if (outputPrefab != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField("Existing Output", outputPrefab, typeof(GameObject), false);
                if (GUILayout.Button("Ping Output", GUILayout.Width(90f)))
                {
                    Selection.activeObject = outputPrefab;
                    EditorGUIUtility.PingObject(outputPrefab);
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "No output prefab yet. Create Prefab will clone the template to the output path.",
                    MessageType.None);
            }
        }

        private void DrawModelSection()
        {
            EditorGUILayout.LabelField("Visual Source", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            humanoidMeshSource = (GameObject)EditorGUILayout.ObjectField(
                "Model FBX / Prefab",
                humanoidMeshSource,
                typeof(GameObject),
                false);
            if (EditorGUI.EndChangeCheck() && humanoidMeshSource != null)
            {
                if (string.IsNullOrWhiteSpace(visualChildName) || visualChildName == "Visual")
                    visualChildName = PlayerPrefabVisualSetupUtility.SuggestVisualChildName(humanoidMeshSource);
                if (workingDefinition != null)
                    workingDefinition.lastModelSource = humanoidMeshSource;
            }

            visualChildName = EditorGUILayout.TextField("Visual Child Name", visualChildName);
            if (workingDefinition != null)
                workingDefinition.visualChildName = visualChildName;

            DrawModelInspectionPanel(humanoidMeshSource);

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = humanoidMeshSource != null;
            if (GUILayout.Button("Prepare Model Import", GUILayout.Height(22f)))
                PrepareAssignedModelImport();
            if (GUILayout.Button("Auto-Detect", GUILayout.Height(22f)))
                ApplyModelAutoDetect(humanoidMeshSource);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawModelInspectionPanel(GameObject model)
        {
            EnemyModelAvatarUtility.ModelInspection inspection = EnemyModelAvatarUtility.Inspect(model);
            MessageType messageType = MessageType.None;
            if (!inspection.HasModel)
                messageType = MessageType.None;
            else if (inspection.IsHumanoidAvatar && inspection.IsAvatarValid && inspection.LooksHumanoidSized)
                messageType = MessageType.Info;
            else if (inspection.HasModel)
                messageType = MessageType.Warning;

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Model Inspection", EditorStyles.miniBoldLabel);
            if (!inspection.HasModel)
            {
                EditorGUILayout.LabelField("Assign a Meshy Humanoid FBX to inspect rig, avatar, and scale.");
                EditorGUILayout.HelpBox(
                    "You can still Repair Visual / holders on an existing OUTPUT prefab without a new mesh.",
                    MessageType.None);
            }
            else
            {
                EditorGUILayout.LabelField(inspection.Summary);
                if (!string.IsNullOrEmpty(inspection.AssetPath))
                    EditorGUILayout.LabelField(inspection.AssetPath, EditorStyles.miniLabel);

                string recommendation = inspection.Recommendation;
                if (recommendation != null &&
                    recommendation.IndexOf("Enemy Prefab Creator", System.StringComparison.Ordinal) >= 0)
                {
                    recommendation = recommendation.Replace(
                        "Enemy Prefab Creator",
                        "Player Prefab Creator");
                    recommendation = recommendation.Replace(
                        "use Archetype HumanoidInvector and Create Prefab",
                        "set Prefab File Name and click Create Prefab / Rebuild");
                }

                EditorGUILayout.HelpBox(recommendation, messageType);
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawStatusSection()
        {
            EditorGUILayout.LabelField("Prefab Status", EditorStyles.boldLabel);
            string outputPath = PlayerPrefabVisualSetupUtility.ResolveOutputPrefabPath(
                prefabFileName, workingDefinition.displayName);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(outputPath);
            if (prefab == null)
            {
                EditorGUILayout.HelpBox(
                    $"Output prefab not created yet at {outputPath}.",
                    MessageType.None);
                return;
            }

            Animator animator = prefab.GetComponent<Animator>();
            Transform stock = prefab.transform.Find("3D Model");
            Transform visual = prefab.transform.Find(
                string.IsNullOrWhiteSpace(visualChildName) ? "Visual" : visualChildName);

            string avatarLabel = animator != null && animator.avatar != null
                ? $"{animator.avatar.name} (human={animator.avatar.isHuman}, valid={animator.avatar.isValid})"
                : "none";

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Avatar", avatarLabel);
            EditorGUILayout.LabelField("Stock 3D Model", stock != null ? "present" : "missing");
            EditorGUILayout.LabelField("Visual Child", visual != null ? visual.name : "not applied yet");
            EditorGUILayout.LabelField(
                "Edit-mode Animator",
                animator != null ? (animator.enabled ? "enabled" : "disabled (bind pose)") : "n/a");
            EditorGUILayout.EndVertical();
        }

        private void DrawActionButtons()
        {
            string outputPath = PlayerPrefabVisualSetupUtility.ResolveOutputPrefabPath(
                prefabFileName, workingDefinition.displayName);
            bool outputExists = AssetDatabase.LoadAssetAtPath<GameObject>(outputPath) != null;
            bool blocked = PlayerPrefabVisualSetupUtility.IsProtectedTemplatePath(outputPath);
            string createLabel = outputExists ? "Rebuild Prefab" : "Create Prefab";

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Definition Asset", GUILayout.Height(30f)))
                SaveDefinitionAsset();

            GUI.enabled = outputExists && !blocked;
            if (GUILayout.Button("Repair Visual (No Mesh Change)", GUILayout.Height(30f)))
                RepairWithoutMesh();
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            GUI.enabled = !blocked && (humanoidMeshSource != null || !outputExists);
            if (GUILayout.Button(
                    humanoidMeshSource != null
                        ? (outputExists ? "Apply Visual / Rebuild" : "Create Prefab + Apply Visual")
                        : createLabel,
                    GUILayout.Height(34f)))
            {
                if (humanoidMeshSource != null)
                    ApplyVisualRebuild();
                else
                    CreatePrefabFromTemplateOnly();
            }
            GUI.enabled = true;

            if (blocked)
            {
                EditorGUILayout.HelpBox(
                    "Create / Rebuild / Repair are blocked while Prefab File Name is Player_Invector.",
                    MessageType.Warning);
            }
        }

        private void PrepareAssignedModelImport()
        {
            if (humanoidMeshSource == null)
                return;

            string path = AssetDatabase.GetAssetPath(humanoidMeshSource);
            if (string.IsNullOrEmpty(path))
                path = EnemyModelAvatarUtility.FindPrimaryModelAssetPath(humanoidMeshSource);

            if (!EnemyModelAvatarUtility.TryPrepareModelImport(path, out string message))
            {
                EditorUtility.DisplayDialog("Prepare Model Import", message, "OK");
                return;
            }

            humanoidMeshSource = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            ApplyModelAutoDetect(humanoidMeshSource);
            EditorUtility.DisplayDialog("Prepare Model Import", message, "OK");
        }

        private void ApplyModelAutoDetect(GameObject model)
        {
            if (model == null)
                return;

            EnemyModelAvatarUtility.ModelInspection inspection = EnemyModelAvatarUtility.Inspect(model);
            if (inspection.IsHumanoidAvatar && inspection.IsAvatarValid)
            {
                if (string.IsNullOrWhiteSpace(visualChildName))
                    visualChildName = "Visual";
            }

            if (workingDefinition != null)
            {
                workingDefinition.lastModelSource = model;
                if (string.IsNullOrWhiteSpace(workingDefinition.displayName) ||
                    workingDefinition.displayName == "Player" ||
                    workingDefinition.displayName == "New Player Visual")
                {
                    workingDefinition.displayName = model.name.Replace('_', ' ');
                }

                if (string.IsNullOrWhiteSpace(prefabFileName) ||
                    prefabFileName == "Player_Custom" ||
                    prefabFileName == "NewPlayer")
                {
                    prefabFileName = PlayerPrefabVisualSetupUtility.SanitizeFileName(
                        "Player_" + model.name, "Player_Custom");
                    workingDefinition.prefabFileName = prefabFileName;
                }
            }
        }

        private bool TryGetValidatedOutputPath(out string outputPath)
        {
            SyncWorkingDefinitionFields();
            outputPath = PlayerPrefabVisualSetupUtility.ResolveOutputPrefabPath(
                prefabFileName, workingDefinition.displayName);
            if (!PlayerPrefabVisualSetupUtility.TryValidateOutputPath(outputPath, out string error))
            {
                EditorUtility.DisplayDialog("Player Prefab Creator", error, "OK");
                return false;
            }

            return true;
        }

        private void SyncWorkingDefinitionFields()
        {
            if (workingDefinition == null)
                return;

            workingDefinition.displayName = string.IsNullOrWhiteSpace(workingDefinition.displayName)
                ? "Player Custom"
                : workingDefinition.displayName;
            workingDefinition.prefabFileName = prefabFileName;
            workingDefinition.templatePrefab = templatePrefab;
            workingDefinition.visualChildName = visualChildName;
            workingDefinition.lastModelSource = humanoidMeshSource;
        }

        private void ApplyVisualRebuild()
        {
            if (humanoidMeshSource == null)
            {
                EditorUtility.DisplayDialog(
                    "Player Prefab Creator",
                    "Assign a Model FBX / Prefab first, or use Create Prefab without a mesh / Repair Visual.",
                    "OK");
                return;
            }

            if (!TryGetValidatedOutputPath(out string outputPath))
                return;

            GameObject created = PlayerPrefabVisualSetupUtility.CreateOrRebuildPlayerPrefab(
                outputPath,
                humanoidMeshSource,
                visualChildName,
                templatePrefab);

            if (created == null)
            {
                EditorUtility.DisplayDialog(
                    "Player Prefab Creator",
                    $"Could not create/rebuild player prefab at {outputPath}.",
                    "OK");
                return;
            }

            if (workingDefinition != null)
                workingDefinition.playerPrefab = created;

            AssetDatabase.SaveAssets();
            Selection.activeObject = created;
            EditorGUIUtility.PingObject(created);
            Debug.Log(
                $"[Player Prefab Creator] Wrote visual from '{humanoidMeshSource.name}' to {outputPath} " +
                $"(template preserved: {PlayerPrefabVisualSetupUtility.ResolveTemplatePath(templatePrefab)})");
        }

        private void CreatePrefabFromTemplateOnly()
        {
            if (!TryGetValidatedOutputPath(out string outputPath))
                return;

            GameObject created = PlayerPrefabVisualSetupUtility.CreateOrRebuildPlayerPrefab(
                outputPath,
                null,
                visualChildName,
                templatePrefab);

            if (created == null)
            {
                EditorUtility.DisplayDialog(
                    "Player Prefab Creator",
                    $"Could not create player prefab at {outputPath}.",
                    "OK");
                return;
            }

            if (workingDefinition != null)
                workingDefinition.playerPrefab = created;

            AssetDatabase.SaveAssets();
            Selection.activeObject = created;
            EditorGUIUtility.PingObject(created);
            Debug.Log(
                $"[Player Prefab Creator] Created {outputPath} from template " +
                $"(template preserved: {PlayerPrefabVisualSetupUtility.ResolveTemplatePath(templatePrefab)})");
        }

        private void RepairWithoutMesh()
        {
            if (!TryGetValidatedOutputPath(out string outputPath))
                return;

            if (!System.IO.File.Exists(outputPath))
            {
                EditorUtility.DisplayDialog(
                    "Player Prefab Creator",
                    $"Output prefab missing at {outputPath}. Create Prefab first.",
                    "OK");
                return;
            }

            if (!PlayerPrefabVisualSetupUtility.RepairVisualAtPath(outputPath))
            {
                EditorUtility.DisplayDialog("Player Prefab Creator", $"Could not repair {outputPath}.", "OK");
                return;
            }

            GameObject repaired = AssetDatabase.LoadAssetAtPath<GameObject>(outputPath);
            if (workingDefinition != null)
                workingDefinition.playerPrefab = repaired;

            AssetDatabase.SaveAssets();
            Selection.activeObject = repaired;
            EditorGUIUtility.PingObject(repaired);
            Debug.Log($"[Player Prefab Creator] Repaired holders / weapon visuals / edit-mode animator at {outputPath}");
        }

        private void StartNewDefinition()
        {
            selectedDefinitionIndex = -1;
            workingDefinition = CreateInstance<PlayerVisualDefinition>();
            workingDefinition.displayName = "New Player Visual";
            workingDefinition.prefabFileName = "Player_Custom";
            workingDefinition.visualChildName = "Visual";
            workingDefinition.templatePrefab = PlayerPrefabVisualSetupUtility.LoadDefaultPlayerPrefab();
            workingDefinition.playerPrefab = null;
            workingDefinition.notes =
                "Clones from Player_Invector (template). Create Prefab writes a new file — never overwrites the template.";
            templatePrefab = workingDefinition.templatePrefab;
            visualChildName = "Visual";
            humanoidMeshSource = null;
            prefabFileName = "Player_Custom";
            definitionAssetFileName = "player_visual_new";
        }

        private void LoadDefinition(PlayerVisualDefinition asset, int index)
        {
            if (asset == null)
            {
                StartNewDefinition();
                return;
            }

            selectedDefinitionIndex = index;
            workingDefinition = Instantiate(asset);
            workingDefinition.name = asset.name;
            definitionAssetFileName = asset.name;
            prefabFileName = string.IsNullOrWhiteSpace(asset.prefabFileName)
                ? "Player_Custom"
                : asset.prefabFileName;
            templatePrefab = asset.templatePrefab != null
                ? asset.templatePrefab
                : PlayerPrefabVisualSetupUtility.LoadDefaultPlayerPrefab();
            visualChildName = string.IsNullOrWhiteSpace(asset.visualChildName) ? "Visual" : asset.visualChildName;
            humanoidMeshSource = asset.lastModelSource;

            // Migrate old defs that pointed playerPrefab at the protected template.
            if (asset.playerPrefab != null)
            {
                string linked = AssetDatabase.GetAssetPath(asset.playerPrefab);
                if (PlayerPrefabVisualSetupUtility.IsProtectedTemplatePath(linked))
                    workingDefinition.playerPrefab = null;
            }
        }

        private void SaveDefinitionAsset()
        {
            EnsureWorkingDefinition();
            SyncWorkingDefinitionFields();
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PlayersData);

            string fileName = SanitizeFileName(definitionAssetFileName, "Player_Default");
            string path = $"{ProjectAssetPaths.PlayersData}/{fileName}.asset";

            string outputPath = PlayerPrefabVisualSetupUtility.ResolveOutputPrefabPath(
                prefabFileName, workingDefinition.displayName);
            GameObject outputPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(outputPath);
            workingDefinition.playerPrefab = outputPrefab;

            PlayerVisualDefinition existing = AssetDatabase.LoadAssetAtPath<PlayerVisualDefinition>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(workingDefinition, path);
            }
            else
            {
                EditorUtility.CopySerialized(workingDefinition, existing);
                EditorUtility.SetDirty(existing);
                workingDefinition = existing;
            }

            AssetDatabase.SaveAssets();
            RefreshDefinitionList();
            Debug.Log($"Saved player visual definition to {path} (output={outputPath})");
        }

        private static string SanitizeFileName(string preferred, string fallback)
        {
            return PlayerPrefabVisualSetupUtility.SanitizeFileName(preferred, fallback);
        }
    }
}
