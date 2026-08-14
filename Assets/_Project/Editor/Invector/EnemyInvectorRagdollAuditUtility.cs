#if UNITY_EDITOR
using System.Text;
using Project.AI.Invector;
using Project.EditorTools;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Invector
{
    public static class EnemyInvectorRagdollAuditUtility
    {
        [MenuItem(DarkMatterGenesisEditorMenus.Combat + "Audit Humanoid Ragdoll Setup", false, 132)]
        public static void AuditHumanoidRagdollSetup()
        {
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { ProjectAssetPaths.PrefabsCombat });
            StringBuilder summary = new StringBuilder();
            int healthy = 0;
            int needsRepair = 0;

            for (int i = 0; i < prefabGuids.Length; i++)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefabAsset == null || prefabAsset.GetComponent<EnemyInvectorBootstrap>() == null)
                    continue;

                GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
                if (root == null)
                    continue;

                try
                {
                    EnemyInvectorRagdollAudit.Report report = EnemyInvectorRagdollAudit.Audit(root);
                    if (report.IsHealthy)
                    {
                        healthy++;
                        continue;
                    }

                    needsRepair++;
                    summary.AppendLine(prefabPath);
                    for (int issueIndex = 0; issueIndex < report.Issues.Count; issueIndex++)
                        summary.AppendLine("  - " + report.Issues[issueIndex]);
                    summary.AppendLine(
                        $"  bones={report.BoneRigidbodyCount}, wrongLayer={report.WrongLayerBoneCount}, " +
                        $"missingColliders={report.MissingRigidbodyColliderCount}, missingJoints={report.MissingJointBoneCount}, " +
                        $"oversizedColliders={report.ImplausiblySizedColliderCount}");
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            string message = needsRepair == 0
                ? $"All {healthy} humanoid combat prefab(s) passed ragdoll audit."
                : summary.ToString();

            EditorUtility.DisplayDialog("Humanoid Ragdoll Audit", message, "OK");
            if (needsRepair > 0)
                Debug.LogWarning($"Humanoid ragdoll audit found issues on {needsRepair} prefab(s).\n{message}");
            else
                Debug.Log(message);
        }

        [MenuItem(DarkMatterGenesisEditorMenus.Combat + "Audit Selected Ragdoll Setup", false, 134)]
        public static void AuditSelectedRagdollSetup()
        {
            GameObject[] selected = Selection.gameObjects;
            if (selected == null || selected.Length == 0)
            {
                EditorUtility.DisplayDialog("Ragdoll Audit", "Select a humanoid enemy prefab or instance.", "OK");
                return;
            }

            StringBuilder summary = new StringBuilder();
            for (int i = 0; i < selected.Length; i++)
            {
                GameObject root = selected[i];
                if (root == null)
                    continue;

                EnemyInvectorRagdollAudit.Report report = EnemyInvectorRagdollAudit.Audit(root);
                summary.AppendLine(root.name + (report.IsHealthy ? " OK" : " NEEDS ATTENTION"));
                for (int issueIndex = 0; issueIndex < report.Issues.Count; issueIndex++)
                    summary.AppendLine("  - " + report.Issues[issueIndex]);
            }

            EditorUtility.DisplayDialog("Selected Ragdoll Audit", summary.ToString(), "OK");
        }
    }
}
#endif
