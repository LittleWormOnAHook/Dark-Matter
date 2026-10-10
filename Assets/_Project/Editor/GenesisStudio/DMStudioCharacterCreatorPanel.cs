#if UNITY_EDITOR
using MalbersAnimations.PathCreation;
using Project.AI;
using Project.Data;
using Project.EditorTools;
using Project.UI;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.GenesisStudio
{
    /// <summary>
    /// Genesis Studio → Player → Player Prefab / Enemy Prefab / Definitions.
    /// Single inspector column (Map Calibration chrome). Shared Prefab Creator drawers stay intact.
    /// </summary>
    public sealed class DMStudioCharacterCreatorPanel
    {
        const string PlayerSubtabId = "character-creator-player";
        const string EnemySubtabId = "character-creator-enemy";
        const string DefinitionsSubtabId = "character-creator-definitions";
        const float StudioLabelWidth = 200f;

        readonly PlayerPrefabCreatorPanelState playerState = new PlayerPrefabCreatorPanelState();
        readonly EnemyHumanoidPrefabCreatorPanelState enemyState = new EnemyHumanoidPrefabCreatorPanelState();

        int selectedPlayerDef = -1;
        int selectedEnemyDef = -1;
        PlayerVisualDefinition[] playerDefs = System.Array.Empty<PlayerVisualDefinition>();
        EnemyDefinition[] enemyDefs = System.Array.Empty<EnemyDefinition>();

        PlayerVisualDefinition workingPlayerDef;
        string playerDefinitionAssetFileName = "player_visual_new";

        EnemyDefinition workingEnemyDef;
        string enemyDefinitionAssetFileName = "new_enemy";
        PathCreator patrolPathCreator;

        bool foldMesh = true;
        bool foldCombatAi;
        bool foldLoot;
        bool foldHealth;
        bool foldHealthBar;
        bool foldSenses;
        bool foldPlayerDefinition = true;

        UnityEditor.Editor inlinePlayerDefEditor;

        public void Draw(string subtabId)
        {
            DMCharacterCreatorSharedUi.PreferMeasuredColumnWidth = false;

            if (playerState.templatePrefab == null)
                playerState.templatePrefab = PlayerPrefabVisualSetupUtility.LoadDefaultPlayerPrefab();
            if (enemyState.templatePrefab == null)
                enemyState.templatePrefab = EnemyPrefabVisualSetupUtility.LoadDefaultTemplate();

            EnsureWorkingEnemyDefinition();
            EnsureWorkingPlayerDefinition();
            enemyState.definition = workingEnemyDef;
            RefreshDefinitionLists();

            switch (subtabId)
            {
                case EnemySubtabId:
                    DrawEnemyPage();
                    break;
                case DefinitionsSubtabId:
                    DrawDefinitionsPage();
                    break;
                default:
                    DrawPlayerPage();
                    break;
            }
        }

        void DrawPlayerPage()
        {
            DrawPlayerDefinitionPicker();
            DMCharacterCreatorSharedUi.DrawWrappedHelpBox(
                "Clone Player_Invector to a new prefab, paste a Humanoid Meshy FBX. " +
                DMHumanoidVisualRebuildUtility.BoneRenameHelp,
                MessageType.Info);

            DMStudioStyles.DrawInspectorSectionHeader("Identity");
            using (DMStudioStyles.BeginProfileInspector(StudioLabelWidth))
            {
                playerState.displayName = EditorGUILayout.TextField("Display Name", playerState.displayName);
                playerState.prefabFileName = EditorGUILayout.TextField("Prefab File Name", playerState.prefabFileName);
            }

            DMStudioStyles.DrawInspectorSectionHeader("Template & Output");
            using (DMStudioStyles.BeginProfileInspector(StudioLabelWidth))
                PlayerPrefabCreatorPanel.DrawTemplateAndOutput(playerState);

            DMStudioStyles.DrawInspectorSectionHeader("Visual Source");
            using (DMStudioStyles.BeginProfileInspector(StudioLabelWidth))
                PlayerPrefabCreatorPanel.DrawModelSection(playerState);

            DMStudioStyles.DrawInspectorSectionHeader("Prefab Status");
            using (DMStudioStyles.BeginProfileInspector(StudioLabelWidth))
                PlayerPrefabCreatorPanel.DrawStatus(playerState);

            DMStudioStyles.DrawInspectorSectionHeader("Actions");
            PlayerPrefabCreatorPanel.DrawActions(playerState, compact: true, OnPlayerPrefabCreatedOrRebuilt);

            EditorGUILayout.Space(8f);
            foldPlayerDefinition = DMStudioStyles.DrawInspectorFoldout(foldPlayerDefinition, "Visual Definition");
            if (foldPlayerDefinition)
            {
                using (DMStudioStyles.BeginProfileInspector(StudioLabelWidth))
                {
                    PlayerPrefabCreatorPanel.DrawDefinitionSection(
                        playerState,
                        workingPlayerDef,
                        ref playerDefinitionAssetFileName);
                }

                PlayerPrefabCreatorPanel.DrawSaveDefinitionRow(
                    playerState,
                    workingPlayerDef,
                    playerDefinitionAssetFileName,
                    SavePlayerDefinition,
                    SavePlayerDefinitionCreateAndApply);
            }

            EditorGUILayout.Space(6f);
            if (GUILayout.Button(
                    "Open Player Prefab Creator",
                    GUILayout.Height(26f),
                    GUILayout.ExpandWidth(true)))
                EditorApplication.ExecuteMenuItem(DarkMatterGenesisEditorMenus.PlayerPrefabCreator);
        }

        void DrawEnemyPage()
        {
            DrawEnemyDefinitionPicker();
            EnemyPrefabCreatorPanel.DrawIntroHelpBox(compact: true);

            SyncEnemyStateFromWorking();
            EnemyPrefabCreatorPanelContext panelCtx = BuildEnemyPanelContext();

            DMStudioStyles.DrawInspectorSectionHeader("Identity");
            using (DMStudioStyles.BeginProfileInspector(StudioLabelWidth))
                EnemyPrefabCreatorPanel.DrawIdentity(panelCtx);
            enemyDefinitionAssetFileName = panelCtx.DefinitionAssetFileName;
            SyncEnemyStateFromWorking();

            EditorGUILayout.Space(6f);
            foldMesh = DMStudioStyles.DrawInspectorFoldout(foldMesh, "Mesh & Prefab");
            if (foldMesh)
            {
                using (DMStudioStyles.BeginProfileInspector(StudioLabelWidth))
                {
                    EnemyHumanoidPrefabCreatorPanel.DrawTemplateAndOutput(enemyState);
                    EditorGUILayout.Space(4f);
                    EnemyHumanoidPrefabCreatorPanel.DrawModelSection(enemyState);
                    EditorGUILayout.Space(4f);
                    EnemyHumanoidPrefabCreatorPanel.DrawStatus(enemyState);
                    EditorGUILayout.Space(4f);
                    SyncEnemyStateToWorking();
                    enemyState.definition = workingEnemyDef;
                    EnemyHumanoidPrefabCreatorPanel.DrawActions(enemyState, compact: true, OnEnemyPrefabCreatedOrRebuilt);
                }

                SyncEnemyStateToWorking();
            }

            panelCtx = BuildEnemyPanelContext();

            foldCombatAi = DMStudioStyles.DrawInspectorFoldout(foldCombatAi, "Combat & AI");
            if (foldCombatAi)
            {
                using (DMStudioStyles.BeginProfileInspector(StudioLabelWidth))
                {
                    EnemyPrefabCreatorPanel.DrawHumanoidWeapons(workingEnemyDef);
                    EnemyPrefabCreatorPanel.DrawBehaviorPreset(workingEnemyDef);
                    EnemyPrefabCreatorPanel.DrawMovementAndBehavior(panelCtx);
                    patrolPathCreator = panelCtx.PatrolPathCreator;
                    EnemyPrefabCreatorPanel.DrawHumanoidAnimatorNote(show: true);
                    EnemyPrefabCreatorPanel.DrawCombatStats(workingEnemyDef);
                }
            }

            foldLoot = DMStudioStyles.DrawInspectorFoldout(foldLoot, "Loot");
            if (foldLoot)
            {
                using (DMStudioStyles.BeginProfileInspector(StudioLabelWidth))
                    EnemyPrefabCreatorPanel.DrawLoot(panelCtx);
            }

            foldHealth = DMStudioStyles.DrawInspectorFoldout(foldHealth, "Health");
            if (foldHealth)
            {
                using (DMStudioStyles.BeginProfileInspector(StudioLabelWidth))
                    EnemyPrefabCreatorPanel.DrawHealth(workingEnemyDef);
            }

            foldHealthBar = DMStudioStyles.DrawInspectorFoldout(foldHealthBar, "Health Bar");
            if (foldHealthBar)
            {
                using (DMStudioStyles.BeginProfileInspector(StudioLabelWidth))
                    EnemyPrefabCreatorPanel.DrawHealthBar(workingEnemyDef);
            }

            foldSenses = DMStudioStyles.DrawInspectorFoldout(foldSenses, "Senses");
            if (foldSenses)
            {
                using (DMStudioStyles.BeginProfileInspector(StudioLabelWidth))
                    EnemyPrefabCreatorPanel.DrawSenses(workingEnemyDef);
            }

            EditorGUILayout.Space(8f);
            EnemyPrefabCreatorPanel.DrawSaveDefinitionRow(
                panelCtx,
                SaveEnemyDefinition,
                SaveEnemyDefinitionCreateAndApply);
            EditorGUILayout.Space(6f);
            if (GUILayout.Button(
                    "Open Enemy Prefab Creator (Legacy creature + full build)",
                    GUILayout.Height(26f),
                    GUILayout.ExpandWidth(true)))
                EditorApplication.ExecuteMenuItem(DarkMatterGenesisEditorMenus.EnemyPrefabCreator);
        }

        void DrawDefinitionsPage()
        {
            DrawPlayerDefinitionPicker();
            DrawInlinePlayerDefinitionInspector();

            EditorGUILayout.Space(14f);
            DMStudioStyles.DrawAccentLine(
                EditorGUILayout.GetControlRect(false, 2f),
                DarkMatterGenesisUiPalette.SlateGray);
            EditorGUILayout.Space(10f);

            DrawEnemyDefinitionPicker();
            DrawInlineEnemyDefinitionInspector();

            EditorGUILayout.Space(8f);
            DMCharacterCreatorSharedUi.DrawToolbarButtonsWrapped(
                22f,
                ("Open Player Prefab Creator", () =>
                    EditorApplication.ExecuteMenuItem(DarkMatterGenesisEditorMenus.PlayerPrefabCreator)),
                ("Open Enemy Prefab Creator", () =>
                    EditorApplication.ExecuteMenuItem(DarkMatterGenesisEditorMenus.EnemyPrefabCreator)));

            EditorGUILayout.Space(6f);
            DMCharacterCreatorSharedUi.DrawWrappedHelpBox(
                "Combat → Enemy Types lists brain/category overrides for each EnemyDefinition.",
                MessageType.None);
        }

        void DrawPlayerDefinitionPicker()
        {
            PlayerVisualDefinition libraryAsset = ResolveSelectedPlayerAsset();
            string title = libraryAsset != null
                ? FormatAssetTitle(libraryAsset.displayName, libraryAsset.name)
                : "Custom";
            string subtitle = libraryAsset != null
                ? libraryAsset.name
                : "Unsaved working definition — save to add it to the library.";
            DMStudioStyles.DrawPingSelectHeader(title, subtitle, libraryAsset);

            using (DMStudioStyles.BeginProfileInspector(StudioLabelWidth))
            {
                EditorGUI.BeginChangeCheck();
                PlayerVisualDefinition picked = (PlayerVisualDefinition)EditorGUILayout.ObjectField(
                    "Visual Definition",
                    libraryAsset,
                    typeof(PlayerVisualDefinition),
                    false);
                if (EditorGUI.EndChangeCheck())
                {
                    if (picked == null)
                        StartNewPlayerCustom();
                    else
                        SelectPlayerDefinitionByAsset(picked);
                }

                int popup = DMCharacterCreatorDefinitionSidebar.DrawStudioLibraryPopup(
                    "Library",
                    selectedPlayerDef,
                    playerState.displayName,
                    DMCharacterCreatorDefinitionSidebar.BuildPlayerLibraryLabels(playerDefs));
                if (popup != selectedPlayerDef)
                {
                    if (popup < 0)
                        StartNewPlayerCustom();
                    else if (popup < playerDefs.Length && playerDefs[popup] != null)
                        SelectPlayerDefinition(playerDefs[popup], popup);
                }
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("New Custom", GUILayout.Height(22f), GUILayout.Width(110f)))
                StartNewPlayerCustom();
            if (GUILayout.Button("Refresh Library", GUILayout.Height(22f), GUILayout.Width(120f)))
                RefreshDefinitionLists();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4f);
        }

        void DrawEnemyDefinitionPicker()
        {
            EnemyDefinition libraryAsset = ResolveSelectedEnemyAsset();
            string title = libraryAsset != null
                ? FormatAssetTitle(libraryAsset.displayName, libraryAsset.name)
                : "Custom";
            string subtitle = libraryAsset != null
                ? libraryAsset.name
                : "Unsaved working definition — save to add it to the library.";
            DMStudioStyles.DrawPingSelectHeader(title, subtitle, libraryAsset);

            using (DMStudioStyles.BeginProfileInspector(StudioLabelWidth))
            {
                EditorGUI.BeginChangeCheck();
                EnemyDefinition picked = (EnemyDefinition)EditorGUILayout.ObjectField(
                    "Enemy Definition",
                    libraryAsset,
                    typeof(EnemyDefinition),
                    false);
                if (EditorGUI.EndChangeCheck())
                {
                    if (picked == null)
                        StartNewEnemyCustom();
                    else
                        SelectEnemyDefinitionByAsset(picked);
                }

                int popup = DMCharacterCreatorDefinitionSidebar.DrawStudioLibraryPopup(
                    "Library",
                    selectedEnemyDef,
                    enemyState.displayName,
                    DMCharacterCreatorDefinitionSidebar.BuildEnemyLibraryLabels(enemyDefs));
                if (popup != selectedEnemyDef)
                {
                    if (popup < 0)
                        StartNewEnemyCustom();
                    else if (popup < enemyDefs.Length && enemyDefs[popup] != null)
                        SelectEnemyDefinition(enemyDefs[popup], popup);
                }
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("New Custom", GUILayout.Height(22f), GUILayout.Width(110f)))
                StartNewEnemyCustom();
            if (GUILayout.Button("Refresh Library", GUILayout.Height(22f), GUILayout.Width(120f)))
                RefreshDefinitionLists();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4f);
        }

        void DrawInlinePlayerDefinitionInspector()
        {
            if (selectedPlayerDef < 0 || workingPlayerDef == null)
            {
                DMCharacterCreatorSharedUi.DrawWrappedHelpBox(
                    "Custom — author on Player Prefab, or pick a saved Visual Definition above.",
                    MessageType.None);
                return;
            }

            using (DMStudioStyles.BeginProfileInspector(StudioLabelWidth))
            {
                EditorGUILayout.ObjectField("Asset", ResolveSelectedPlayerAsset(), typeof(PlayerVisualDefinition), false);
                UnityEditor.Editor.CreateCachedEditor(workingPlayerDef, null, ref inlinePlayerDefEditor);
                inlinePlayerDefEditor?.OnInspectorGUI();
            }

            DMCharacterCreatorSharedUi.DrawToolbarButtonsWrapped(
                24f,
                ("Edit in Player Prefab", () => GenesisStudioWindow.OpenTo("player", PlayerSubtabId)),
                ("Ping Output Prefab", () =>
                    PingLinkedPrefab(PlayerPrefabVisualSetupUtility.ResolveOutputPrefabPath(workingPlayerDef))));
        }

        void DrawInlineEnemyDefinitionInspector()
        {
            if (selectedEnemyDef < 0 || workingEnemyDef == null)
            {
                DMCharacterCreatorSharedUi.DrawWrappedHelpBox(
                    "Custom — author on Enemy Prefab, or pick a saved Enemy Definition above.",
                    MessageType.None);
                return;
            }

            EnemyPrefabCreatorPanelContext ctx = BuildEnemyPanelContext();
            using (DMStudioStyles.BeginProfileInspector(StudioLabelWidth))
            {
                EditorGUILayout.ObjectField("Asset", ResolveSelectedEnemyAsset(), typeof(EnemyDefinition), false);
                EnemyPrefabCreatorPanel.DrawHealth(workingEnemyDef);
                EditorGUILayout.Space(4f);
                EnemyPrefabCreatorPanel.DrawHealthBar(workingEnemyDef);
                EditorGUILayout.Space(4f);
                EnemyPrefabCreatorPanel.DrawSenses(workingEnemyDef);
                EditorGUILayout.Space(4f);
                EnemyPrefabCreatorPanel.DrawLoot(ctx);
            }

            DMCharacterCreatorSharedUi.DrawToolbarButtonsWrapped(
                24f,
                ("Edit in Enemy Prefab", () => GenesisStudioWindow.OpenTo("player", EnemySubtabId)),
                ("Ping Output Prefab", () => EnemyPrefabCreatorPanel.PingOutputPrefab(workingEnemyDef)),
                ("Save Definition", SaveEnemyDefinition));
        }

        PlayerVisualDefinition ResolveSelectedPlayerAsset()
        {
            if (selectedPlayerDef >= 0 && selectedPlayerDef < playerDefs.Length)
                return playerDefs[selectedPlayerDef];
            return null;
        }

        EnemyDefinition ResolveSelectedEnemyAsset()
        {
            if (selectedEnemyDef >= 0 && selectedEnemyDef < enemyDefs.Length)
                return enemyDefs[selectedEnemyDef];
            return null;
        }

        void SelectPlayerDefinitionByAsset(PlayerVisualDefinition def)
        {
            if (def == null)
                return;

            RefreshDefinitionLists();
            int index = IndexOf(playerDefs, def);
            if (index < 0)
            {
                index = DMCharacterCreatorDefinitionSidebar.FindPlayerDefinitionIndex(
                    playerDefs,
                    def.name,
                    def.displayName,
                    def.prefabFileName);
            }

            if (index >= 0 && playerDefs[index] != null)
                SelectPlayerDefinition(playerDefs[index], index);
        }

        void SelectEnemyDefinitionByAsset(EnemyDefinition def)
        {
            if (def == null)
                return;

            RefreshDefinitionLists();
            int index = IndexOf(enemyDefs, def);
            if (index < 0)
            {
                index = DMCharacterCreatorDefinitionSidebar.FindEnemyDefinitionIndex(
                    enemyDefs,
                    def.name,
                    def.displayName,
                    def.prefabFileName,
                    def.enemyId);
            }

            if (index >= 0 && enemyDefs[index] != null)
                SelectEnemyDefinition(enemyDefs[index], index);
        }

        void SelectPlayerDefinition(PlayerVisualDefinition def, int index)
        {
            selectedPlayerDef = index;
            workingPlayerDef = Object.Instantiate(def);
            workingPlayerDef.name = def.name;
            playerDefinitionAssetFileName = def.name;
            PlayerPrefabCreatorPanel.SyncFromDefinition(playerState, def);
            PingLinkedPrefab(PlayerPrefabVisualSetupUtility.ResolveOutputPrefabPath(def));
        }

        void SelectEnemyDefinition(EnemyDefinition def, int index)
        {
            selectedEnemyDef = index;
            workingEnemyDef = Object.Instantiate(def);
            workingEnemyDef.name = def.name;
            enemyDefinitionAssetFileName = def.name;
            EnemyHumanoidPrefabCreatorPanel.SyncFromDefinition(enemyState, workingEnemyDef);
            enemyState.definition = workingEnemyDef;
            PingLinkedPrefab(EnemyPrefabVisualSetupUtility.ResolveOutputPrefabPath(def));
        }

        EnemyPrefabCreatorPanelContext BuildEnemyPanelContext()
        {
            return new EnemyPrefabCreatorPanelContext
            {
                Definition = workingEnemyDef,
                DefinitionAssetFileName = enemyDefinitionAssetFileName,
                PatrolPathCreator = patrolPathCreator,
                ShowDefinitionAssetName = true,
                ApplyPatrolPath = () =>
                {
                    if (patrolPathCreator == null)
                    {
                        Debug.LogWarning("Character Creator: assign a Path Creator first.");
                        return;
                    }

                    int applied = DMIPathFollowEditorUtility.ApplyToEnemies(patrolPathCreator, null);
                    Debug.Log(applied > 0
                        ? $"Character Creator: assigned Path Creator to {applied} enemy AI target(s)."
                        : "Character Creator: select a scene enemy, then Apply Patrol Path.");
                },
                ApplyLoot = () => EnemyPrefabCreatorPanel.ApplyLootToExistingPrefab(workingEnemyDef)
            };
        }

        void SaveEnemyDefinition()
        {
            SyncEnemyStateToWorking();
            if (SaveEnemyDefinitionAndSelect())
                enemyState.definition = workingEnemyDef;
        }

        void SavePlayerDefinition()
        {
            SyncPlayerDefinitionAssetFileNameFromIdentity();
            EnsureWorkingPlayerDefinition();
            SavePlayerDefinitionAndSelect();
        }

        void SavePlayerDefinitionCreateAndApply()
        {
            SyncPlayerDefinitionAssetFileNameFromIdentity();
            EnsureWorkingPlayerDefinition();
            if (!SavePlayerDefinitionAndSelect())
                return;

            if (!PlayerPrefabCreatorPanel.TryCreatePrefabFromState(playerState, out bool created) || !created)
                return;

            PlayerPrefabCreatorPanel.SyncToDefinition(playerState, workingPlayerDef);
            SavePlayerDefinitionAndSelect();
        }

        void SaveEnemyDefinitionCreateAndApply()
        {
            SyncEnemyStateToWorking();
            if (!SaveEnemyDefinitionAndSelect())
                return;

            enemyState.definition = workingEnemyDef;
            SyncEnemyStateFromWorking();
            if (!EnemyHumanoidPrefabCreatorPanel.TryCreatePrefabFromState(enemyState, out bool created) || !created)
                return;

            SyncEnemyStateToWorking();
            SyncEnemyDefinitionAssetFileNameFromIdentity();
            if (EnemyPrefabCreatorPanel.SaveDefinitionAsset(ref workingEnemyDef, enemyDefinitionAssetFileName))
            {
                RefreshDefinitionListsAndSelectEnemy(
                    enemyDefinitionAssetFileName,
                    workingEnemyDef.displayName,
                    workingEnemyDef.prefabFileName,
                    workingEnemyDef.enemyId);
            }

            enemyState.definition = workingEnemyDef;
        }

        void OnPlayerPrefabCreatedOrRebuilt()
        {
            SyncPlayerDefinitionAssetFileNameFromIdentity();
            EnsureWorkingPlayerDefinition();
            if (!DMCharacterCreatorActionValidation.TryValidatePlayerSaveDefinition(
                    workingPlayerDef,
                    playerDefinitionAssetFileName,
                    playerState.prefabFileName,
                    playerState.displayName,
                    out string message))
            {
                DMCharacterCreatorActionValidation.ShowValidationDialog(
                    DMCharacterCreatorActionValidation.PlayerDialogTitle,
                    "Prefab was created or rebuilt, but Save Definition could not run:\n\n" + message);
                return;
            }

            SavePlayerDefinitionAndSelect();
        }

        void OnEnemyPrefabCreatedOrRebuilt()
        {
            SyncEnemyStateToWorking();
            SyncEnemyDefinitionAssetFileNameFromIdentity();
            if (!DMCharacterCreatorActionValidation.TryValidateEnemySaveDefinition(
                    workingEnemyDef,
                    enemyDefinitionAssetFileName,
                    out string message))
            {
                DMCharacterCreatorActionValidation.ShowValidationDialog(
                    DMCharacterCreatorActionValidation.EnemyDialogTitle,
                    "Prefab was created or rebuilt, but Save Definition could not run:\n\n" + message);
                return;
            }

            SaveEnemyDefinitionAndSelect();
        }

        void SyncPlayerDefinitionAssetFileNameFromIdentity()
        {
            playerDefinitionAssetFileName = DMCharacterCreatorDefinitionSidebar.SuggestPlayerDefinitionAssetFileName(
                playerState.prefabFileName,
                playerState.displayName);
        }

        void SyncEnemyDefinitionAssetFileNameFromIdentity()
        {
            if (workingEnemyDef != null && string.IsNullOrWhiteSpace(workingEnemyDef.enemyId))
                workingEnemyDef.enemyId = enemyState.prefabFileName;

            enemyDefinitionAssetFileName = DMCharacterCreatorDefinitionSidebar.SuggestEnemyDefinitionAssetFileName(
                enemyState.prefabFileName,
                workingEnemyDef != null ? workingEnemyDef.enemyId : null,
                enemyState.displayName);
        }

        bool SavePlayerDefinitionAndSelect()
        {
            SyncPlayerDefinitionAssetFileNameFromIdentity();
            if (!PlayerPrefabCreatorPanel.SaveDefinitionAsset(
                    playerState,
                    ref workingPlayerDef,
                    playerDefinitionAssetFileName))
            {
                return false;
            }

            RefreshDefinitionListsAndSelectPlayer(
                playerDefinitionAssetFileName,
                playerState.displayName,
                playerState.prefabFileName);
            return true;
        }

        bool SaveEnemyDefinitionAndSelect()
        {
            SyncEnemyDefinitionAssetFileNameFromIdentity();
            if (!EnemyPrefabCreatorPanel.SaveDefinitionAsset(ref workingEnemyDef, enemyDefinitionAssetFileName))
                return false;

            RefreshDefinitionListsAndSelectEnemy(
                enemyDefinitionAssetFileName,
                workingEnemyDef.displayName,
                workingEnemyDef.prefabFileName,
                workingEnemyDef.enemyId);
            return true;
        }

        void RefreshDefinitionListsAndSelectPlayer(string assetFileName, string displayName, string prefabFileName)
        {
            RefreshDefinitionLists();
            int index = DMCharacterCreatorDefinitionSidebar.FindPlayerDefinitionIndex(
                playerDefs,
                assetFileName,
                displayName,
                prefabFileName);
            if (index >= 0 && playerDefs[index] != null)
                SelectPlayerDefinition(playerDefs[index], index);
        }

        void RefreshDefinitionListsAndSelectEnemy(
            string assetFileName,
            string displayName,
            string prefabFileName,
            string enemyId)
        {
            RefreshDefinitionLists();
            int index = DMCharacterCreatorDefinitionSidebar.FindEnemyDefinitionIndex(
                enemyDefs,
                assetFileName,
                displayName,
                prefabFileName,
                enemyId);
            if (index >= 0 && enemyDefs[index] != null)
                SelectEnemyDefinition(enemyDefs[index], index);
        }

        void StartNewPlayerCustom()
        {
            selectedPlayerDef = -1;
            workingPlayerDef = ScriptableObject.CreateInstance<PlayerVisualDefinition>();
            workingPlayerDef.displayName = playerState.displayName;
            workingPlayerDef.prefabFileName = playerState.prefabFileName;
            workingPlayerDef.visualChildName = "Visual";
            workingPlayerDef.templatePrefab = playerState.templatePrefab;
            workingPlayerDef.notes =
                "Clones from Player_Invector (template). Create Prefab writes a new file — never overwrites the template.";
            playerDefinitionAssetFileName = DMCharacterCreatorDefinitionSidebar.SuggestPlayerDefinitionAssetFileName(
                playerState.prefabFileName,
                playerState.displayName);
        }

        void StartNewEnemyCustom()
        {
            selectedEnemyDef = -1;
            workingEnemyDef = ScriptableObject.CreateInstance<EnemyDefinition>();
            workingEnemyDef.enemyId = EnemyPrefabBuilder.SanitizeFileName(enemyState.prefabFileName, "new_enemy");
            workingEnemyDef.displayName = enemyState.displayName;
            workingEnemyDef.prefabFileName = enemyState.prefabFileName;
            workingEnemyDef.archetype = EnemyArchetype.HumanoidInvector;
            workingEnemyDef.visualChildName = "Visual";
            workingEnemyDef.ApplyBehaviorPreset(EnemyBehaviorPreset.AggressiveHunter);
            enemyDefinitionAssetFileName = DMCharacterCreatorDefinitionSidebar.SuggestEnemyDefinitionAssetFileName(
                enemyState.prefabFileName,
                workingEnemyDef.enemyId,
                enemyState.displayName);
            enemyState.definition = workingEnemyDef;
            SyncEnemyStateFromWorking();
        }

        void SyncEnemyStateFromWorking()
        {
            if (workingEnemyDef == null)
                return;

            enemyState.displayName = workingEnemyDef.displayName;
            enemyState.prefabFileName = workingEnemyDef.prefabFileName;
            enemyState.visualChildName = workingEnemyDef.visualChildName;
            enemyState.templatePrefab = workingEnemyDef.templatePrefab != null
                ? workingEnemyDef.templatePrefab
                : EnemyPrefabVisualSetupUtility.LoadDefaultTemplate();
            enemyState.humanoidMeshSource = workingEnemyDef.lastModelSource;
            enemyState.definition = workingEnemyDef;
        }

        void SyncEnemyStateToWorking()
        {
            if (workingEnemyDef == null)
                return;

            EnemyHumanoidPrefabCreatorPanel.SyncToDefinition(enemyState);
            workingEnemyDef.displayName = enemyState.displayName;
            workingEnemyDef.prefabFileName = enemyState.prefabFileName;
            workingEnemyDef.visualChildName = enemyState.visualChildName;
            workingEnemyDef.templatePrefab = enemyState.templatePrefab;
            workingEnemyDef.lastModelSource = enemyState.humanoidMeshSource;
        }

        void EnsureWorkingPlayerDefinition()
        {
            if (workingPlayerDef != null)
                return;

            workingPlayerDef = ScriptableObject.CreateInstance<PlayerVisualDefinition>();
            workingPlayerDef.displayName = playerState.displayName;
            workingPlayerDef.prefabFileName = playerState.prefabFileName;
            workingPlayerDef.visualChildName = "Visual";
            workingPlayerDef.templatePrefab = playerState.templatePrefab;
            workingPlayerDef.notes =
                "Clones from Player_Invector (template). Create Prefab writes a new file — never overwrites the template.";
        }

        void EnsureWorkingEnemyDefinition()
        {
            if (workingEnemyDef != null)
                return;

            workingEnemyDef = ScriptableObject.CreateInstance<EnemyDefinition>();
            workingEnemyDef.enemyId = EnemyPrefabBuilder.SanitizeFileName(enemyState.prefabFileName, "new_enemy");
            workingEnemyDef.displayName = enemyState.displayName;
            workingEnemyDef.prefabFileName = enemyState.prefabFileName;
            workingEnemyDef.archetype = EnemyArchetype.HumanoidInvector;
            workingEnemyDef.visualChildName = "Visual";
            workingEnemyDef.ApplyBehaviorPreset(EnemyBehaviorPreset.AggressiveHunter);
            enemyDefinitionAssetFileName = "new_enemy";
            enemyState.definition = workingEnemyDef;
        }

        static void PingLinkedPrefab(string prefabPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                return;

            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
        }

        void RefreshDefinitionLists()
        {
            playerDefs = PlayerPrefabVisualSetupUtility.LoadAllDefinitions();
            enemyDefs = EnemyPrefabBuilder.LoadAllDefinitions();
        }

        static int IndexOf(PlayerVisualDefinition[] defs, PlayerVisualDefinition def)
        {
            if (defs == null || def == null)
                return -1;
            for (int i = 0; i < defs.Length; i++)
            {
                if (defs[i] == def)
                    return i;
            }

            return -1;
        }

        static int IndexOf(EnemyDefinition[] defs, EnemyDefinition def)
        {
            if (defs == null || def == null)
                return -1;
            for (int i = 0; i < defs.Length; i++)
            {
                if (defs[i] == def)
                    return i;
            }

            return -1;
        }

        static string FormatAssetTitle(string displayName, string assetName)
        {
            return string.IsNullOrEmpty(displayName) ? assetName : displayName;
        }
    }
}
#endif
