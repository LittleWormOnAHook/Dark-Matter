#if UNITY_EDITOR
using System.Collections.Generic;
using Project.AI;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    [CustomEditor(typeof(EnemySpawner))]
    public class EnemySpawnerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EnemySpawner spawner = (EnemySpawner)target;
            List<EnemySpawnEntry> entries = BuildPreviewEntries(spawner);
            if (entries.Count == 0)
                return;

            EditorGUILayout.Space(8f);
            int totalCount = 0;
            bool allReady = true;

            for (int i = 0; i < entries.Count; i++)
            {
                EnemySpawnEntry entry = entries[i];
                totalCount += Mathf.Max(1, entry.count);
                if (!EnemyPrefabResolver.IsSpawnReady(entry.prefab))
                    allReady = false;
            }

            string summary = entries.Count == 1
                ? $"{entries[0].prefab.name} x{entries[0].count}"
                : $"{entries.Count} enemy types, {totalCount} total";

            EditorGUILayout.HelpBox(
                allReady
                    ? $"Spawn-ready: {summary}. Press Play to spawn — no extra setup needed."
                    : $"Some prefabs are missing baked gameplay components ({summary}). Run Tools → Survival Pioneer → Combat → Repair All Humanoid Combat Prefabs.",
                allReady ? MessageType.Info : MessageType.Warning);
        }

        private static List<EnemySpawnEntry> BuildPreviewEntries(EnemySpawner spawner)
        {
            List<EnemySpawnEntry> entries = new List<EnemySpawnEntry>();
            IReadOnlyList<EnemySpawnEntry> configured = spawner.Entries;
            if (configured != null)
            {
                for (int i = 0; i < configured.Count; i++)
                {
                    if (configured[i]?.prefab != null)
                        entries.Add(configured[i]);
                }
            }

            if (entries.Count == 0 && spawner.EnemyPrefab != null)
            {
                entries.Add(new EnemySpawnEntry
                {
                    prefab = spawner.EnemyPrefab,
                    count = 1,
                });
            }

            return entries;
        }
    }
}
#endif
