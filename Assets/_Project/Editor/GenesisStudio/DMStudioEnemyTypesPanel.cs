#if UNITY_EDITOR
using System.Collections.Generic;
using Project.AI;
using Project.AI.Invector;
using Project.Combat;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.GenesisStudio
{
    /// <summary>
    /// Combat → AI &amp; awareness → Enemy Types: all EnemyDefinition assets and prefabs that reference them.
    /// </summary>
    public sealed class DMStudioEnemyTypesPanel
    {
        private const string DefinitionsFolder = "Assets/_Project/Data/Enemies";
        private const string PrefabSearchFolder = "Assets/_Project/Prefabs";

        private struct Row
        {
            public EnemyDefinition Definition;
            public SerializedObject Serialized;
            public List<GameObject> Prefabs;
        }

        private readonly List<Row> rows = new List<Row>();
        private Vector2 scroll;
        private bool needsRebuild = true;

        public void Draw()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh table", GUILayout.Width(120f)))
                needsRebuild = true;
            EditorGUILayout.LabelField($"{rows.Count} definition(s)", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4f);

            if (needsRebuild)
            {
                Rebuild();
                needsRebuild = false;
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawHeaderRow();

            for (int i = 0; i < rows.Count; i++)
                DrawDataRow(rows[i]);

            EditorGUILayout.EndScrollView();
        }

        private static void DrawHeaderRow()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Prefabs", GUILayout.Width(160f));
            GUILayout.Label("Definition", GUILayout.Width(140f));
            GUILayout.Label("Type", GUILayout.Width(72f));
            GUILayout.Label("Archetype", GUILayout.Width(100f));
            GUILayout.Label("Personalities", GUILayout.Width(120f));
            GUILayout.Label("Body", GUILayout.Width(72f));
            GUILayout.Label("Ranged", GUILayout.Width(44f));
            GUILayout.Label("Overrides", GUILayout.MinWidth(80f));
            GUILayout.Label("", GUILayout.Width(88f));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawDataRow(Row row)
        {
            if (row.Definition == null || row.Serialized == null)
                return;

            row.Serialized.Update();

            EditorGUILayout.BeginHorizontal();
            DrawPrefabCell(row);
            EditorGUILayout.ObjectField(row.Definition, typeof(EnemyDefinition), false, GUILayout.Width(140f));

            SerializedProperty category = row.Serialized.FindProperty("enemyCategory");
            SerializedProperty archetype = row.Serialized.FindProperty("archetype");
            SerializedProperty overrideBrain = row.Serialized.FindProperty("overrideBrain");
            SerializedProperty brainArchetype = row.Serialized.FindProperty("brainArchetype");
            SerializedProperty primary = row.Serialized.FindProperty("primaryPersonality");
            SerializedProperty secondary = row.Serialized.FindProperty("secondaryPersonality");
            SerializedProperty bodyType = row.Serialized.FindProperty("bodyType");
            SerializedProperty preferRanged = row.Serialized.FindProperty("preferRangedWeapon");

            if (category != null)
                EditorGUILayout.PropertyField(category, GUIContent.none, GUILayout.Width(72f));
            if (archetype != null)
                EditorGUILayout.PropertyField(archetype, GUIContent.none, GUILayout.Width(100f));

            EditorGUI.BeginDisabledGroup(overrideBrain != null && !overrideBrain.boolValue);
            if (brainArchetype != null)
                EditorGUILayout.PropertyField(brainArchetype, GUIContent.none, GUILayout.Width(56f));
            if (primary != null)
                EditorGUILayout.PropertyField(primary, GUIContent.none, GUILayout.Width(56f));
            if (secondary != null)
                EditorGUILayout.PropertyField(secondary, GUIContent.none, GUILayout.Width(56f));
            EditorGUI.EndDisabledGroup();

            if (overrideBrain != null)
                overrideBrain.boolValue = EditorGUILayout.ToggleLeft("", overrideBrain.boolValue, GUILayout.Width(18f));

            if (bodyType != null)
                EditorGUILayout.PropertyField(bodyType, GUIContent.none, GUILayout.Width(72f));
            if (preferRanged != null)
                EditorGUILayout.PropertyField(preferRanged, GUIContent.none, GUILayout.Width(44f));

            string overrideLabel = row.Definition.HasAnyProfileOverride ? "Profiles" : "—";
            EditorGUILayout.LabelField(overrideLabel, GUILayout.MinWidth(80f));

            if (GUILayout.Button("Ping", GUILayout.Width(40f)))
                EditorGUIUtility.PingObject(row.Definition);
            if (GUILayout.Button("Select", GUILayout.Width(48f)))
                Selection.activeObject = row.Definition;

            EditorGUILayout.EndHorizontal();

            if (row.Serialized.ApplyModifiedProperties())
                EditorUtility.SetDirty(row.Definition);
        }

        private static void DrawPrefabCell(Row row)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(160f));
            if (row.Prefabs == null || row.Prefabs.Count == 0)
            {
                EditorGUILayout.LabelField("(no prefab)", EditorStyles.miniLabel);
            }
            else
            {
                for (int p = 0; p < row.Prefabs.Count; p++)
                {
                    GameObject prefab = row.Prefabs[p];
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.ObjectField(prefab, typeof(GameObject), false);
                    if (GUILayout.Button("P", GUILayout.Width(22f)) && prefab != null)
                        EditorGUIUtility.PingObject(prefab);
                    if (GUILayout.Button("S", GUILayout.Width(22f)) && prefab != null)
                        Selection.activeObject = prefab;
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void Rebuild()
        {
            rows.Clear();
            var byDefinition = new Dictionary<EnemyDefinition, Row>();

            string[] defGuids = AssetDatabase.FindAssets("t:EnemyDefinition", new[] { DefinitionsFolder });
            for (int i = 0; i < defGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(defGuids[i]);
                var definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
                if (definition == null)
                    continue;

                var row = new Row
                {
                    Definition = definition,
                    Serialized = new SerializedObject(definition),
                    Prefabs = new List<GameObject>()
                };
                byDefinition[definition] = row;
                rows.Add(row);
            }

            rows.Sort((a, b) => string.Compare(a.Definition.displayName, b.Definition.displayName, System.StringComparison.OrdinalIgnoreCase));

            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabSearchFolder });
            for (int i = 0; i < prefabGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                    continue;

                EnemyDefinition def = EnemyPrefabResolver.GetDefinition(prefab);
                if (def == null)
                {
                    EnemySpawner spawner = prefab.GetComponent<EnemySpawner>();
                    if (spawner != null && spawner.Settings.definition != null)
                        def = spawner.Settings.definition;
                }

                if (def == null || !byDefinition.TryGetValue(def, out Row row))
                    continue;

                if (!row.Prefabs.Contains(prefab))
                    row.Prefabs.Add(prefab);
            }
        }
    }
}
#endif
