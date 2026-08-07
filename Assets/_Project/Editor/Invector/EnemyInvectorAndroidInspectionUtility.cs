#if UNITY_EDITOR
using System.Text;
using Project.AI;
using Project.AI.Invector;
using Project.EditorTools.Invector;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Invector
{
    /// <summary>
    /// Editor menus for the humanoid Android inspection checklist.
    /// </summary>
    public static class EnemyInvectorAndroidInspectionUtility
    {
        [MenuItem(SurvivalPioneerEditorMenus.AuditAndroidEnemyChecklist, false, 131)]
        public static void AuditAllAndroidEnemies()
        {
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { ProjectAssetPaths.PrefabsCombat });
            StringBuilder summary = new StringBuilder();
            int passed = 0;
            int failed = 0;
            int skipped = 0;

            for (int i = 0; i < prefabGuids.Length; i++)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefabAsset == null || prefabAsset.GetComponent<EnemyInvectorBootstrap>() == null)
                    continue;

                EnemyDefinition definition = EnemyPrefabResolver.GetDefinition(prefabAsset);
                if (definition == null || definition.surfaceThreatKind != SurfaceThreatKind.Android)
                {
                    skipped++;
                    continue;
                }

                GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
                if (root == null)
                    continue;

                try
                {
                    EnemyInvectorAndroidInspection.Report report = EnemyInvectorAndroidInspection.Audit(root);
                    if (report.IsHealthy)
                    {
                        passed++;
                        continue;
                    }

                    failed++;
                    summary.AppendLine(prefabPath);
                    summary.AppendLine(EnemyInvectorAndroidInspection.FormatReport(report));
                    summary.AppendLine();
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            string message = failed == 0
                ? $"All {passed} Android combat prefab(s) passed the inspection checklist." +
                  (skipped > 0 ? $" ({skipped} non-Android humanoid prefab(s) skipped.)" : string.Empty)
                : summary.ToString();

            EditorUtility.DisplayDialog("Android Inspection Checklist", message, "OK");
            if (failed > 0)
                Debug.LogWarning($"Android inspection checklist found issues on {failed} prefab(s).\n{message}");
            else
                Debug.Log(message);
        }

        [MenuItem(SurvivalPioneerEditorMenus.AuditSelectedAndroidEnemyChecklist, false, 133)]
        public static void AuditSelectedAndroidEnemy()
        {
            GameObject[] selected = Selection.gameObjects;
            if (selected == null || selected.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "Android Inspection Checklist",
                    "Select a humanoid enemy prefab or scene instance.",
                    "OK");
                return;
            }

            StringBuilder summary = new StringBuilder();
            for (int i = 0; i < selected.Length; i++)
            {
                GameObject root = selected[i];
                if (root == null)
                    continue;

                EnemyInvectorAndroidInspection.Report report = EnemyInvectorAndroidInspection.Audit(root);
                if (summary.Length > 0)
                    summary.AppendLine();
                summary.AppendLine(EnemyInvectorAndroidInspection.FormatReport(report));
            }

            string text = summary.ToString();
            EditorUtility.DisplayDialog("Android Inspection Checklist", text, "OK");
            Debug.Log(text);
        }

        [MenuItem(SurvivalPioneerEditorMenus.RepairSelectedAndroidEnemyChecklist, false, 135)]
        public static void RepairSelectedAndroidEnemy()
        {
            GameObject root = Selection.activeGameObject;
            if (root == null)
            {
                EditorUtility.DisplayDialog(
                    "Repair Android Enemy",
                    "Select a humanoid enemy prefab or scene instance.",
                    "OK");
                return;
            }

            EnemyDefinition definition = EnemyPrefabResolver.GetDefinition(root);
            if (definition == null)
            {
                EnemyInvectorBootstrap bootstrap = root.GetComponent<EnemyInvectorBootstrap>();
                if (bootstrap != null)
                    definition = bootstrap.Definition;
            }

            if (definition == null)
            {
                EditorUtility.DisplayDialog(
                    "Repair Android Enemy",
                    "No EnemyDefinition found on selection.",
                    "OK");
                return;
            }

            string assetPath = PrefabUtility.IsPartOfPrefabAsset(root)
                ? AssetDatabase.GetAssetPath(root)
                : PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root);

            if (!string.IsNullOrEmpty(assetPath))
            {
                bool repaired = EnemyInvectorSetupUtility.RebuildHumanoidEnemyAtPath(assetPath, definition);
                if (repaired)
                {
                    AssetDatabase.SaveAssets();
                    EnemyInvectorAndroidInspection.Report after = EnemyInvectorAndroidInspection.Audit(
                        AssetDatabase.LoadAssetAtPath<GameObject>(assetPath));
                    EditorUtility.DisplayDialog(
                        "Repair Android Enemy",
                        $"Repaired prefab at:\n{assetPath}\n\n{EnemyInvectorAndroidInspection.FormatReport(after)}",
                        "OK");
                    return;
                }
            }

            EnemyInvectorSetupUtility.RepairHumanoidRoot(root, definition);
            EnemyInvectorRagdollAudit.Repair(root);
            EditorUtility.SetDirty(root);

            EnemyInvectorAndroidInspection.Report report = EnemyInvectorAndroidInspection.Audit(root);
            EditorUtility.DisplayDialog(
                "Repair Android Enemy",
                EnemyInvectorAndroidInspection.FormatReport(report),
                "OK");
        }
    }
}
#endif
