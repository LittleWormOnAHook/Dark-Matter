#if UNITY_EDITOR
using System;
using Project.AI;
using Project.Data;
using Project.EditorTools.GenesisStudio;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    [Flags]
    public enum DMCharacterCreatorDefinitionSidebarSections
    {
        Player = 1,
        Enemy = 2,
        Both = Player | Enemy
    }

    /// <summary>
    /// Left-column definition list for Character Creator and Prefab Creator windows.
    /// Always includes a Custom row per visible section; saved assets appear by display name.
    /// </summary>
    public static class DMCharacterCreatorDefinitionSidebar
    {
        public const float Width = 240f;

        public static void Draw(
            DMCharacterCreatorDefinitionSidebarSections sections,
            ref Vector2 scroll,
            ref int selectedPlayerIndex,
            ref int selectedEnemyIndex,
            PlayerVisualDefinition[] playerDefs,
            EnemyDefinition[] enemyDefs,
            string customPlayerHint,
            string customEnemyHint,
            Action onSelectCustomPlayer,
            Action<PlayerVisualDefinition, int> onSelectPlayer,
            Action onSelectCustomEnemy,
            Action<EnemyDefinition, int> onSelectEnemy,
            Action refreshList)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(Width), GUILayout.ExpandHeight(true));
            scroll = EditorGUILayout.BeginScrollView(
                scroll,
                GUILayout.Width(Width),
                GUILayout.ExpandHeight(true));

            if ((sections & DMCharacterCreatorDefinitionSidebarSections.Player) != 0)
            {
                EditorGUILayout.LabelField("Player", EditorStyles.boldLabel);
                DrawCustomRow(
                    selectedPlayerIndex < 0,
                    "Custom",
                    customPlayerHint,
                    onSelectCustomPlayer);

                if (playerDefs != null)
                {
                    for (int i = 0; i < playerDefs.Length; i++)
                    {
                        PlayerVisualDefinition def = playerDefs[i];
                        if (def == null)
                            continue;

                        string label = FormatDefinitionLabel(def.displayName, def.name);
                        if (DrawListToggle(selectedPlayerIndex == i, label) && selectedPlayerIndex != i)
                            onSelectPlayer?.Invoke(def, i);
                    }
                }

                EditorGUILayout.Space(6f);
            }

            if ((sections & DMCharacterCreatorDefinitionSidebarSections.Enemy) != 0)
            {
                EditorGUILayout.LabelField("Enemy", EditorStyles.boldLabel);
                DrawCustomRow(
                    selectedEnemyIndex < 0,
                    "Custom",
                    customEnemyHint,
                    onSelectCustomEnemy);

                if (enemyDefs != null)
                {
                    for (int i = 0; i < enemyDefs.Length; i++)
                    {
                        EnemyDefinition def = enemyDefs[i];
                        if (def == null)
                            continue;

                        string label = FormatDefinitionLabel(def.displayName, def.name);
                        if (DrawListToggle(selectedEnemyIndex == i, label) && selectedEnemyIndex != i)
                            onSelectEnemy?.Invoke(def, i);
                    }
                }
            }

            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("Refresh List", GUILayout.Height(24f)))
                refreshList?.Invoke();

            EditorGUILayout.EndVertical();
        }

        public static int FindPlayerDefinitionIndex(
            PlayerVisualDefinition[] defs,
            string assetFileName,
            string displayName,
            string prefabFileName)
        {
            if (defs == null || defs.Length == 0)
                return -1;

            if (!string.IsNullOrWhiteSpace(assetFileName))
            {
                for (int i = 0; i < defs.Length; i++)
                {
                    PlayerVisualDefinition def = defs[i];
                    if (def != null && string.Equals(def.name, assetFileName, StringComparison.OrdinalIgnoreCase))
                        return i;
                }
            }

            if (!string.IsNullOrWhiteSpace(displayName))
            {
                for (int i = 0; i < defs.Length; i++)
                {
                    PlayerVisualDefinition def = defs[i];
                    if (def != null && string.Equals(def.displayName, displayName, StringComparison.OrdinalIgnoreCase))
                        return i;
                }
            }

            if (!string.IsNullOrWhiteSpace(prefabFileName))
            {
                for (int i = 0; i < defs.Length; i++)
                {
                    PlayerVisualDefinition def = defs[i];
                    if (def != null && string.Equals(def.prefabFileName, prefabFileName, StringComparison.OrdinalIgnoreCase))
                        return i;
                }
            }

            return -1;
        }

        public static int FindEnemyDefinitionIndex(
            EnemyDefinition[] defs,
            string assetFileName,
            string displayName,
            string prefabFileName,
            string enemyId)
        {
            if (defs == null || defs.Length == 0)
                return -1;

            if (!string.IsNullOrWhiteSpace(assetFileName))
            {
                for (int i = 0; i < defs.Length; i++)
                {
                    EnemyDefinition def = defs[i];
                    if (def != null && string.Equals(def.name, assetFileName, StringComparison.OrdinalIgnoreCase))
                        return i;
                }
            }

            if (!string.IsNullOrWhiteSpace(enemyId))
            {
                for (int i = 0; i < defs.Length; i++)
                {
                    EnemyDefinition def = defs[i];
                    if (def != null && string.Equals(def.enemyId, enemyId, StringComparison.OrdinalIgnoreCase))
                        return i;
                }
            }

            if (!string.IsNullOrWhiteSpace(displayName))
            {
                for (int i = 0; i < defs.Length; i++)
                {
                    EnemyDefinition def = defs[i];
                    if (def != null && string.Equals(def.displayName, displayName, StringComparison.OrdinalIgnoreCase))
                        return i;
                }
            }

            if (!string.IsNullOrWhiteSpace(prefabFileName))
            {
                for (int i = 0; i < defs.Length; i++)
                {
                    EnemyDefinition def = defs[i];
                    if (def != null && string.Equals(def.prefabFileName, prefabFileName, StringComparison.OrdinalIgnoreCase))
                        return i;
                }
            }

            return -1;
        }

        public static string SuggestPlayerDefinitionAssetFileName(string prefabFileName, string displayName)
        {
            return PlayerPrefabVisualSetupUtility.SanitizeFileName(prefabFileName, displayName);
        }

        public static string SuggestEnemyDefinitionAssetFileName(string prefabFileName, string enemyId, string displayName)
        {
            if (!string.IsNullOrWhiteSpace(prefabFileName))
                return EnemyPrefabBuilder.SanitizeFileName(prefabFileName, displayName);
            return EnemyPrefabBuilder.SanitizeFileName(enemyId, displayName);
        }

        public static string[] BuildPlayerLibraryLabels(PlayerVisualDefinition[] defs)
        {
            if (defs == null || defs.Length == 0)
                return Array.Empty<string>();

            string[] labels = new string[defs.Length];
            for (int i = 0; i < defs.Length; i++)
            {
                PlayerVisualDefinition def = defs[i];
                labels[i] = def == null ? "(missing)" : FormatDefinitionLabel(def.displayName, def.name);
            }

            return labels;
        }

        public static string[] BuildEnemyLibraryLabels(EnemyDefinition[] defs)
        {
            if (defs == null || defs.Length == 0)
                return Array.Empty<string>();

            string[] labels = new string[defs.Length];
            for (int i = 0; i < defs.Length; i++)
            {
                EnemyDefinition def = defs[i];
                labels[i] = def == null ? "(missing)" : FormatDefinitionLabel(def.displayName, def.name);
            }

            return labels;
        }

        /// <summary>
        /// Script-row library popup: Custom first, then saved definition display names.
        /// Returns -1 for Custom, otherwise the library index.
        /// </summary>
        public static int DrawStudioLibraryPopup(
            string label,
            int selectedIndex,
            string customHint,
            string[] libraryLabels)
        {
            int count = libraryLabels != null ? libraryLabels.Length : 0;
            string[] options = new string[count + 1];
            options[0] = string.IsNullOrWhiteSpace(customHint) ? "Custom" : "Custom — " + customHint;
            for (int i = 0; i < count; i++)
                options[i + 1] = string.IsNullOrEmpty(libraryLabels[i]) ? "(unnamed)" : libraryLabels[i];

            int popup = 0;
            if (selectedIndex >= 0 && selectedIndex < count)
                popup = selectedIndex + 1;

            int next = EditorGUILayout.Popup(label, popup, options);
            return next <= 0 ? -1 : next - 1;
        }

        static void DrawCustomRow(bool selected, string title, string hint, Action onSelect)
        {
            string label = string.IsNullOrWhiteSpace(hint) ? title : $"{title}\n{hint}";
            if (DrawListToggle(selected, label) && !selected)
                onSelect?.Invoke();
        }

        static bool DrawListToggle(bool selected, string label)
        {
            GUIStyle baseStyle = selected ? DMStudioStyles.ListButtonSelected : DMStudioStyles.ListButton;
            var style = new GUIStyle(baseStyle) { wordWrap = true };
            float innerWidth = Width - 12f;
            float height = style.CalcHeight(new GUIContent(label), innerWidth);
            height = Mathf.Max(24f, height + 4f);
            return GUILayout.Toggle(
                selected,
                label,
                style,
                GUILayout.Width(Width),
                GUILayout.MinHeight(height));
        }

        static string FormatDefinitionLabel(string displayName, string assetName)
        {
            return string.IsNullOrEmpty(displayName) ? assetName : displayName;
        }
    }
}
#endif
