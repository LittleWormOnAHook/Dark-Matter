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
        private const float RowVPad = 5f;
        private const float LineGap = 2f;

        /// <summary>Fixed column widths — header and data rows must stay in sync.</summary>
        private static class Col
        {
            public const float Prefabs = 188f;
            public const float Definition = 152f;
            public const float Category = 92f;
            public const float Archetype = 128f;
            public const float BrainOverride = 24f;
            public const float BrainArchetype = 108f;
            public const float Personality = 102f;
            public const float Body = 92f;
            public const float PreferRanged = 36f;
            public const float Overrides = 196f;
            public const float Actions = 104f;

            public static float TableMinWidth =>
                Prefabs + Definition + Category + Archetype + BrainOverride + BrainArchetype
                + Personality * 2f + Body + PreferRanged + Overrides + Actions + 24f;
        }

        private struct Row
        {
            public EnemyDefinition Definition;
            public SerializedObject Serialized;
            public List<GameObject> Prefabs;
        }

        private readonly List<Row> rows = new List<Row>();
        private readonly Dictionary<string, bool> overridesExpanded = new Dictionary<string, bool>();
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
            EditorGUILayout.BeginVertical(GUILayout.MinWidth(Col.TableMinWidth));
            DrawHeaderRow();
            DrawRowSeparator();

            for (int i = 0; i < rows.Count; i++)
            {
                DrawDataRow(rows[i], i);
                DrawRowSeparator();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        private static void DrawHeaderRow()
        {
            float h = EditorGUIUtility.singleLineHeight + 4f;
            Rect rowRect = EditorGUILayout.GetControlRect(false, h);
            float x = rowRect.x;
            HeaderLabel(ref x, rowRect.y, "Prefabs", "Prefab(s) referencing this EnemyDefinition.", Col.Prefabs);
            HeaderLabel(ref x, rowRect.y, "Definition", "EnemyDefinition asset.", Col.Definition);
            HeaderLabel(ref x, rowRect.y, "Category", "Encounter type (Grunt, Ranged, Boss, …).", Col.Category);
            HeaderLabel(ref x, rowRect.y, "Archetype", "Legacy creature vs humanoid Invector pipeline.", Col.Archetype);
            HeaderLabel(ref x, rowRect.y, "Ovr", "Override brain defaults (archetype + personalities below).", Col.BrainOverride);
            HeaderLabel(ref x, rowRect.y, "Brain", "Brain archetype when override is on.", Col.BrainArchetype);
            HeaderLabel(ref x, rowRect.y, "Primary", "Primary personality when override is on.", Col.Personality);
            HeaderLabel(ref x, rowRect.y, "Secondary", "Secondary personality when override is on.", Col.Personality);
            HeaderLabel(ref x, rowRect.y, "Body", "Hit-mark body type at spawn.", Col.Body);
            HeaderLabel(ref x, rowRect.y, "Rng", "Prefer ranged weapon in loadout.", Col.PreferRanged);
            HeaderLabel(ref x, rowRect.y, "Overrides", "Ranged Combat ammo + optional brain / engagement / hit-mark profiles.", Col.Overrides);
            HeaderLabel(ref x, rowRect.y, "Actions", "Ping or select the definition asset.", Col.Actions);
        }

        private static void HeaderLabel(ref float x, float y, string text, string tooltip, float width)
        {
            Rect r = new Rect(x, y, width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(r, new GUIContent(text, tooltip), EditorStyles.miniLabel);
            x += width;
        }

        private static void DrawRowSeparator()
        {
            Rect r = EditorGUILayout.GetControlRect(false, 1f);
            EditorGUI.DrawRect(r, new Color(0.42f, 0.44f, 0.5f, 0.45f));
        }

        private void DrawDataRow(Row row, int rowIndex)
        {
            if (row.Definition == null || row.Serialized == null)
                return;

            row.Serialized.Update();

            float line = EditorGUIUtility.singleLineHeight;
            string defKey = row.Definition.enemyId;
            if (string.IsNullOrEmpty(defKey))
                defKey = AssetDatabase.GetAssetPath(row.Definition);
            if (!overridesExpanded.TryGetValue(defKey, out bool overridesOpen))
                overridesOpen = false;

            SerializedProperty category = row.Serialized.FindProperty("enemyCategory");
            SerializedProperty archetype = row.Serialized.FindProperty("archetype");
            SerializedProperty overrideBrain = row.Serialized.FindProperty("overrideBrain");
            SerializedProperty brainArchetype = row.Serialized.FindProperty("brainArchetype");
            SerializedProperty primary = row.Serialized.FindProperty("primaryPersonality");
            SerializedProperty secondary = row.Serialized.FindProperty("secondaryPersonality");
            SerializedProperty bodyType = row.Serialized.FindProperty("bodyType");
            SerializedProperty preferRanged = row.Serialized.FindProperty("preferRangedWeapon");
            SerializedProperty rangedAmmoType = row.Serialized.FindProperty("rangedAmmoType");
            SerializedProperty brainProfile = row.Serialized.FindProperty("brainProfileOverride");
            SerializedProperty engagementProfile = row.Serialized.FindProperty("engagementProfileOverride");
            SerializedProperty hitMarkProfile = row.Serialized.FindProperty("hitMarkProfileOverride");

            int overrideCount = CountProfileOverrides(brainProfile, engagementProfile, hitMarkProfile);
            int prefabCount = row.Prefabs == null || row.Prefabs.Count == 0 ? 1 : row.Prefabs.Count;
            float prefabBlockHeight = prefabCount * (line + LineGap) - LineGap;

            float overridesHeight = line;
            if (overridesOpen)
            {
                overridesHeight = line * 2f + LineGap;
                if (overrideCount > 0)
                    overridesHeight += overrideCount * (line + LineGap);
            }

            float rowHeight = Mathf.Max(line, prefabBlockHeight, overridesHeight) + RowVPad * 2f;

            Rect rowRect = EditorGUILayout.GetControlRect(false, rowHeight);
            if (rowIndex % 2 == 1)
                EditorGUI.DrawRect(rowRect, new Color(0f, 0f, 0f, 0.06f));

            float x = rowRect.x;
            float yTop = rowRect.y + RowVPad;

            DrawPrefabCell(new Rect(x, yTop, Col.Prefabs, rowRect.height - RowVPad * 2f), row);
            x += Col.Prefabs;

            Rect defRect = CenterRect(new Rect(x, yTop, Col.Definition, rowRect.height - RowVPad * 2f), line);
            row.Definition = (EnemyDefinition)EditorGUI.ObjectField(defRect, row.Definition, typeof(EnemyDefinition), false);
            x += Col.Definition;

            DrawEnumField(CenterRect(new Rect(x, yTop, Col.Category, rowRect.height - RowVPad * 2f), line), category);
            x += Col.Category;

            DrawEnumField(CenterRect(new Rect(x, yTop, Col.Archetype, rowRect.height - RowVPad * 2f), line), archetype);
            x += Col.Archetype;

            Rect ovrToggleRect = CenterRect(new Rect(x, yTop, Col.BrainOverride, rowRect.height - RowVPad * 2f), line);
            if (overrideBrain != null)
                overrideBrain.boolValue = EditorGUI.Toggle(ovrToggleRect, overrideBrain.boolValue);
            x += Col.BrainOverride;

            bool brainFieldsEnabled = overrideBrain == null || overrideBrain.boolValue;
            EditorGUI.BeginDisabledGroup(!brainFieldsEnabled);
            DrawEnumField(CenterRect(new Rect(x, yTop, Col.BrainArchetype, rowRect.height - RowVPad * 2f), line), brainArchetype);
            x += Col.BrainArchetype;
            DrawEnumField(CenterRect(new Rect(x, yTop, Col.Personality, rowRect.height - RowVPad * 2f), line), primary);
            x += Col.Personality;
            DrawEnumField(CenterRect(new Rect(x, yTop, Col.Personality, rowRect.height - RowVPad * 2f), line), secondary);
            EditorGUI.EndDisabledGroup();
            x += Col.Personality;

            DrawEnumField(CenterRect(new Rect(x, yTop, Col.Body, rowRect.height - RowVPad * 2f), line), bodyType);
            x += Col.Body;

            Rect rngRect = CenterRect(new Rect(x, yTop, Col.PreferRanged, rowRect.height - RowVPad * 2f), line);
            if (preferRanged != null)
                preferRanged.boolValue = EditorGUI.Toggle(rngRect, preferRanged.boolValue);
            x += Col.PreferRanged;

            overridesOpen = DrawOverridesCell(
                new Rect(x, yTop, Col.Overrides, rowRect.height - RowVPad * 2f),
                row,
                overridesOpen,
                rangedAmmoType,
                brainProfile,
                engagementProfile,
                hitMarkProfile,
                overrideCount,
                line);
            overridesExpanded[defKey] = overridesOpen;
            x += Col.Overrides;

            DrawActionsCell(CenterRect(new Rect(x, yTop, Col.Actions, rowRect.height - RowVPad * 2f), line), row.Definition);

            if (row.Serialized.ApplyModifiedProperties())
                EditorUtility.SetDirty(row.Definition);
        }

        private static Rect CenterRect(Rect columnRect, float elementHeight)
        {
            float y = columnRect.y + (columnRect.height - elementHeight) * 0.5f;
            return new Rect(columnRect.x, y, columnRect.width, elementHeight);
        }

        /// <summary>Enum popups avoid drawing EnemyDefinition [Header] blocks on isolated PropertyFields.</summary>
        private static void DrawEnumField(Rect rect, SerializedProperty property)
        {
            if (property == null || property.propertyType != SerializedPropertyType.Enum)
                return;

            property.enumValueIndex = EditorGUI.Popup(rect, property.enumValueIndex, property.enumDisplayNames);
        }

        private static bool DrawOverridesCell(
            Rect columnRect,
            Row row,
            bool expanded,
            SerializedProperty rangedAmmoType,
            SerializedProperty brainProfile,
            SerializedProperty engagementProfile,
            SerializedProperty hitMarkProfile,
            int overrideCount,
            float line)
        {
            float x = columnRect.x;

            if (!expanded)
            {
                float cy = columnRect.y + (columnRect.height - line) * 0.5f;
                expanded = EditorGUI.Foldout(new Rect(x, cy, 16f, line), expanded, GUIContent.none, true);
                float innerX = x + 18f;
                float innerW = columnRect.width - 18f;
                float ammoW = innerW * 0.52f;
                DrawEnumField(new Rect(innerX, cy, ammoW - 2f, line), rangedAmmoType);
                DrawProfilesButton(
                    new Rect(innerX + ammoW, cy, innerW - ammoW, line),
                    row,
                    overrideCount,
                    brainProfile,
                    engagementProfile,
                    hitMarkProfile);
                return expanded;
            }

            float y = columnRect.y;
            expanded = EditorGUI.Foldout(new Rect(x, y, 16f, line), expanded, GUIContent.none, true);
            DrawEnumField(new Rect(x + 18f, y, columnRect.width - 18f, line), rangedAmmoType);
            y += line + LineGap;
            DrawProfilesButton(new Rect(x, y, columnRect.width, line), row, overrideCount, brainProfile, engagementProfile, hitMarkProfile);
            y += line + LineGap;

            if (overrideCount > 0)
            {
                DrawObjectField(new Rect(x, y, columnRect.width, line), brainProfile, "Brain");
                y += line + LineGap;
                DrawObjectField(new Rect(x, y, columnRect.width, line), engagementProfile, "Engage");
                y += line + LineGap;
                DrawObjectField(new Rect(x, y, columnRect.width, line), hitMarkProfile, "Hit marks");
            }

            return expanded;
        }

        private static void DrawProfilesButton(
            Rect rect,
            Row row,
            int overrideCount,
            SerializedProperty brainProfile,
            SerializedProperty engagementProfile,
            SerializedProperty hitMarkProfile)
        {
            string label = overrideCount > 0 ? $"Profiles ({overrideCount})" : "Profiles (defaults)";
            Rect btnRect = rect;
            if (overrideCount > 0)
            {
                btnRect.width = Mathf.Max(80f, rect.width - 48f);
                if (GUI.Button(btnRect, label, EditorStyles.miniButton))
                {
                    Selection.activeObject = row.Definition;
                    EditorGUIUtility.PingObject(row.Definition);
                }

                Rect clearRect = new Rect(btnRect.xMax + 4f, rect.y, 44f, rect.height);
                if (GUI.Button(clearRect, "Clear", EditorStyles.miniButton))
                {
                    Undo.RecordObject(row.Definition, "Clear enemy profile overrides");
                    if (brainProfile != null)
                        brainProfile.objectReferenceValue = null;
                    if (engagementProfile != null)
                        engagementProfile.objectReferenceValue = null;
                    if (hitMarkProfile != null)
                        hitMarkProfile.objectReferenceValue = null;
                    EditorUtility.SetDirty(row.Definition);
                }
            }
            else if (GUI.Button(btnRect, label, EditorStyles.miniButton))
            {
                Selection.activeObject = row.Definition;
                EditorGUIUtility.PingObject(row.Definition);
            }
        }

        private static void DrawObjectField(Rect rect, SerializedProperty property, string label)
        {
            if (property == null || property.objectReferenceValue == null)
                return;

            EditorGUI.PropertyField(rect, property, new GUIContent(label, property.tooltip), false);
        }

        private static void DrawActionsCell(Rect rect, EnemyDefinition definition)
        {
            if (definition == null)
                return;

            float half = (rect.width - 4f) * 0.5f;
            Rect ping = new Rect(rect.x, rect.y, half, rect.height);
            Rect select = new Rect(ping.xMax + 4f, rect.y, half, rect.height);
            if (GUI.Button(ping, "Ping"))
                EditorGUIUtility.PingObject(definition);
            if (GUI.Button(select, "Select"))
                Selection.activeObject = definition;
        }

        private static int CountProfileOverrides(
            SerializedProperty brainProfile,
            SerializedProperty engagementProfile,
            SerializedProperty hitMarkProfile)
        {
            int count = 0;
            if (brainProfile != null && brainProfile.objectReferenceValue != null)
                count++;
            if (engagementProfile != null && engagementProfile.objectReferenceValue != null)
                count++;
            if (hitMarkProfile != null && hitMarkProfile.objectReferenceValue != null)
                count++;
            return count;
        }

        private static void DrawPrefabCell(Rect columnRect, Row row)
        {
            float line = EditorGUIUtility.singleLineHeight;
            int count = row.Prefabs == null || row.Prefabs.Count == 0 ? 1 : row.Prefabs.Count;
            float blockHeight = count * (line + LineGap) - LineGap;
            float y = columnRect.y + (columnRect.height - blockHeight) * 0.5f;

            if (row.Prefabs == null || row.Prefabs.Count == 0)
            {
                EditorGUI.LabelField(new Rect(columnRect.x, y, columnRect.width, line), "(no prefab)", EditorStyles.miniLabel);
                return;
            }

            const float pingWidth = 22f;
            float fieldWidth = columnRect.width - pingWidth * 2f - 4f;
            for (int p = 0; p < row.Prefabs.Count; p++)
            {
                GameObject prefab = row.Prefabs[p];
                Rect fieldRect = new Rect(columnRect.x, y, fieldWidth, line);
                EditorGUI.ObjectField(fieldRect, prefab, typeof(GameObject), false);

                Rect pRect = new Rect(fieldRect.xMax + 2f, y, pingWidth, line);
                if (GUI.Button(pRect, "P", EditorStyles.miniButton) && prefab != null)
                    EditorGUIUtility.PingObject(prefab);

                Rect sRect = new Rect(pRect.xMax + 2f, y, pingWidth, line);
                if (GUI.Button(sRect, "S", EditorStyles.miniButton) && prefab != null)
                    Selection.activeObject = prefab;

                y += line + LineGap;
            }
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
