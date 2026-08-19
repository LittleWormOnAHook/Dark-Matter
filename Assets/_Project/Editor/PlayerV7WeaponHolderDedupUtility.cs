#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using Invector.vCharacterController.vActions;
using Invector.vMelee;
using Invector.vShooter;
using Project.Player.Invector;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Removes stacked duplicate Invector weapon-holder subtrees on Player_v7 (Visual skeleton)
    /// while preserving serialized Pioneer weapon slot references, then strips hidden VBOT armature.
    /// </summary>
    public static class PlayerV7WeaponHolderDedupUtility
    {
        public const string DefaultPlayerV7Path = "Assets/_Project/Prefabs/Players/Player_v7.prefab";
        public const string BackupPlayerV7Path = "Assets/_Project/Prefabs/Players/Player_v7_backup.prefab";

        public struct DedupReport
        {
            public int RemovedRightHandStacks;
            public int RemovedSpine2Stacks;
            public int RemovedNamedDuplicates;
            public int RemovedRootHitBoxes;
            public int RemovedVbotNodes;
            public int TotalRemoved;

            public override string ToString()
            {
                return new StringBuilder()
                    .Append("RightHand stacks=").Append(RemovedRightHandStacks)
                    .Append(", Spine2 stacks=").Append(RemovedSpine2Stacks)
                    .Append(", named dupes=").Append(RemovedNamedDuplicates)
                    .Append(", root hitBox=").Append(RemovedRootHitBoxes)
                    .Append(", VBOT nodes=").Append(RemovedVbotNodes)
                    .Append(", total=").Append(TotalRemoved)
                    .ToString();
            }
        }

        public static bool EnsureFileBackup(string prefabPath)
        {
            if (string.IsNullOrEmpty(prefabPath) || !System.IO.File.Exists(prefabPath))
                return false;

            if (AssetDatabase.LoadAssetAtPath<GameObject>(BackupPlayerV7Path) != null)
                return true;

            if (!AssetDatabase.CopyAsset(prefabPath, BackupPlayerV7Path))
            {
                Debug.LogWarning($"[PlayerV7Dedup] Could not copy backup to {BackupPlayerV7Path}.");
                return false;
            }

            Debug.Log($"[PlayerV7Dedup] Created file backup at {BackupPlayerV7Path}.");
            return true;
        }

        public static bool DedupAndRepair(string prefabPath, out DedupReport report, bool createFileBackup = true)
        {
            report = default;
            if (string.IsNullOrEmpty(prefabPath) || !System.IO.File.Exists(prefabPath))
            {
                Debug.LogError($"[PlayerV7Dedup] Prefab not found: {prefabPath}");
                return false;
            }

            if (createFileBackup && prefabPath == DefaultPlayerV7Path)
                EnsureFileBackup(prefabPath);

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null)
            {
                Debug.LogError($"[PlayerV7Dedup] Failed to load prefab contents: {prefabPath}");
                return false;
            }

            try
            {
                if (!DedupHolderTrees(root, ref report))
                    return false;

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            if (!PlayerPrefabVisualSetupUtility.RepairVisualAtPath(prefabPath))
            {
                Debug.LogError($"[PlayerV7Dedup] RepairVisualAtPath failed for {prefabPath}.");
                return false;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[PlayerV7Dedup] Completed on {prefabPath}. {report}");
            return true;
        }

        public static bool DedupHolderTrees(GameObject root, ref DedupReport report)
        {
            if (root == null)
                return false;

            HashSet<GameObject> protectedObjects = BuildProtectionSet(root);
            Transform visual = root.transform.Find("Visual");
            if (visual == null)
            {
                Debug.LogError("[PlayerV7Dedup] Missing Visual child on player prefab.");
                return false;
            }

            report.RemovedRightHandStacks += DedupSiblingStacks(
                visual,
                "R_Hand",
                "RightHand",
                protectedObjects,
                ref report.TotalRemoved);

            report.RemovedSpine2Stacks += DedupSiblingStacks(
                visual,
                "Spine02",
                "Spine2",
                protectedObjects,
                ref report.TotalRemoved);

            report.RemovedNamedDuplicates += DedupNamedDuplicatesUnder(
                visual,
                protectedObjects,
                ref report.TotalRemoved);

            report.RemovedRootHitBoxes += DedupRootHitBoxes(root.transform, protectedObjects, ref report.TotalRemoved);
            report.RemovedVbotNodes += StripHiddenVbotArmature(root.transform, protectedObjects, ref report.TotalRemoved);
            return true;
        }

        private static HashSet<GameObject> BuildProtectionSet(GameObject root)
        {
            var set = new HashSet<GameObject>();
            AddProtectedHierarchy(set, root);

            PioneerInvectorWeaponBridge bridge = root.GetComponent<PioneerInvectorWeaponBridge>();
            if (bridge != null)
            {
                SerializedObject bridgeObject = new SerializedObject(bridge);
                AddWeaponSlotsFromProperty(bridgeObject.FindProperty("meleeWeaponSlots"), set);
                AddWeaponSlotsFromProperty(bridgeObject.FindProperty("rangedWeaponSlots"), set);
            }

            vCollectMeleeControl collect = root.GetComponent<vCollectMeleeControl>();
            if (collect != null)
            {
                AddHandlerTree(set, collect.rightHandler);
                AddHandlerTree(set, collect.leftHandler);
            }

            vShooterManager shooter = root.GetComponent<vShooterManager>();
            if (shooter != null && shooter.rWeapon != null)
                AddProtectedHierarchy(set, shooter.rWeapon.gameObject);

            vMeleeManager melee = root.GetComponent<vMeleeManager>();
            if (melee != null && melee.rightWeapon != null)
                AddProtectedHierarchy(set, melee.rightWeapon.gameObject);

            return set;
        }

        private static void AddWeaponSlotsFromProperty(SerializedProperty arrayProperty, HashSet<GameObject> set)
        {
            if (arrayProperty == null || !arrayProperty.isArray)
                return;

            for (int i = 0; i < arrayProperty.arraySize; i++)
            {
                SerializedProperty element = arrayProperty.GetArrayElementAtIndex(i);
                AddIfAssigned(set, element.FindPropertyRelative("drawnInstance"));
                AddIfAssigned(set, element.FindPropertyRelative("holsteredInstance"));
            }
        }

        private static void AddIfAssigned(HashSet<GameObject> set, SerializedProperty objectProperty)
        {
            if (objectProperty == null || objectProperty.propertyType != SerializedPropertyType.ObjectReference)
                return;

            if (objectProperty.objectReferenceValue is GameObject go)
                AddProtectedHierarchy(set, go);
        }

        private static void AddHandlerTree(HashSet<GameObject> set, vHandler handler)
        {
            if (handler == null)
                return;

            AddProtectedHierarchy(set, handler.defaultHandler != null ? handler.defaultHandler.gameObject : null);

            if (handler.customHandlers == null)
                return;

            for (int i = 0; i < handler.customHandlers.Count; i++)
            {
                Transform custom = handler.customHandlers[i];
                if (custom != null)
                    AddProtectedHierarchy(set, custom.gameObject);
            }
        }

        private static void AddProtectedHierarchy(HashSet<GameObject> set, GameObject go)
        {
            if (go == null)
                return;

            Transform walk = go.transform;
            while (walk != null)
            {
                set.Add(walk.gameObject);
                walk = walk.parent;
            }
        }

        private static int DedupSiblingStacks(
            Transform visualRoot,
            string parentBoneName,
            string socketName,
            HashSet<GameObject> protectedObjects,
            ref int totalRemoved)
        {
            int removedStacks = 0;
            Transform[] all = visualRoot.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                Transform parentBone = all[i];
                if (parentBone == null || !parentBone.name.Equals(parentBoneName, StringComparison.Ordinal))
                    continue;

                List<Transform> sockets = CollectNamedChildren(parentBone, socketName);
                if (sockets.Count <= 1)
                    continue;

                Transform keep = ChooseBestTransform(sockets, protectedObjects);
                for (int s = 0; s < sockets.Count; s++)
                {
                    Transform candidate = sockets[s];
                    if (candidate == null || candidate == keep)
                        continue;

                    if (HasProtectedDescendant(candidate, protectedObjects))
                    {
                        Debug.LogWarning(
                            $"[PlayerV7Dedup] Skipped deleting protected duplicate {GetTransformPath(candidate)}.");
                        continue;
                    }

                    DestroyTransformTree(candidate, ref totalRemoved);
                    removedStacks++;
                }
            }

            return removedStacks;
        }

        private static int DedupNamedDuplicatesUnder(
            Transform visualRoot,
            HashSet<GameObject> protectedObjects,
            ref int totalRemoved)
        {
            int removed = 0;
            var groups = new Dictionary<string, List<Transform>>(StringComparer.Ordinal);

            Transform[] all = visualRoot.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                Transform t = all[i];
                if (t == null)
                    continue;

                if (!ShouldDedupByName(t.name))
                    continue;

                if (!groups.TryGetValue(t.name, out List<Transform> list))
                {
                    list = new List<Transform>(4);
                    groups[t.name] = list;
                }

                list.Add(t);
            }

            foreach (KeyValuePair<string, List<Transform>> pair in groups)
            {
                List<Transform> list = pair.Value;
                if (list.Count <= 1)
                    continue;

                Transform keep = ChooseBestTransform(list, protectedObjects);
                for (int i = 0; i < list.Count; i++)
                {
                    Transform candidate = list[i];
                    if (candidate == null || candidate == keep)
                        continue;

                    if (HasProtectedDescendant(candidate, protectedObjects) || protectedObjects.Contains(candidate.gameObject))
                    {
                        Debug.LogWarning(
                            $"[PlayerV7Dedup] Skipped deleting protected duplicate {GetTransformPath(candidate)}.");
                        continue;
                    }

                    DestroyTransformTree(candidate, ref totalRemoved);
                    removed++;
                }
            }

            return removed;
        }

        private static int DedupRootHitBoxes(
            Transform root,
            HashSet<GameObject> protectedObjects,
            ref int totalRemoved)
        {
            int removed = 0;
            Transform keep = null;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child == null || !child.name.Equals("hitBox", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (keep == null)
                {
                    keep = child;
                    continue;
                }

                if (protectedObjects.Contains(child.gameObject) || HasProtectedDescendant(child, protectedObjects))
                    continue;

                DestroyTransformTree(child, ref totalRemoved);
                removed++;
            }

            return removed;
        }

        private static int StripHiddenVbotArmature(
            Transform root,
            HashSet<GameObject> protectedObjects,
            ref int totalRemoved)
        {
            Transform model3d = root.Find("3D Model");
            if (model3d == null)
                return 0;

            int removed = 0;
            Transform armature = model3d.Find("Armature");
            if (armature != null)
            {
                if (!HasProtectedDescendant(armature, protectedObjects))
                {
                    removed += CountTransformTree(armature);
                    UnityEngine.Object.DestroyImmediate(armature.gameObject);
                    totalRemoved += removed;
                }
                else
                {
                    Debug.LogWarning("[PlayerV7Dedup] Skipped VBOT Armature — protected descendant found.");
                }
            }

            PruneEmptyTransform(model3d);
            return removed;
        }

        private static void PruneEmptyTransform(Transform node)
        {
            if (node == null || node.childCount > 0)
                return;

            if (node.GetComponents<Component>().Length > 1)
                return;

            UnityEngine.Object.DestroyImmediate(node.gameObject);
        }

        private static bool ShouldDedupByName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            return name.StartsWith("Drawn_", StringComparison.Ordinal)
                   || name.StartsWith("Holstered_", StringComparison.Ordinal)
                   || name.Equals("WeaponHitbox", StringComparison.OrdinalIgnoreCase)
                   || name.Equals("Handlers", StringComparison.Ordinal)
                   || name.Equals("defaultHandler", StringComparison.OrdinalIgnoreCase)
                   || name.Equals("meleeHandler", StringComparison.OrdinalIgnoreCase)
                   || name.Equals("shieldHandler", StringComparison.OrdinalIgnoreCase)
                   || name.Equals("WeaponHolders", StringComparison.Ordinal);
        }

        private static List<Transform> CollectNamedChildren(Transform parent, string childName)
        {
            var list = new List<Transform>(4);
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child != null && child.name.Equals(childName, StringComparison.Ordinal))
                    list.Add(child);
            }

            return list;
        }

        private static Transform ChooseBestTransform(List<Transform> candidates, HashSet<GameObject> protectedObjects)
        {
            Transform best = candidates[0];
            int bestScore = ScoreTransformTree(best, protectedObjects);
            for (int i = 1; i < candidates.Count; i++)
            {
                Transform candidate = candidates[i];
                if (candidate == null)
                    continue;

                int score = ScoreTransformTree(candidate, protectedObjects);
                if (score > bestScore)
                {
                    best = candidate;
                    bestScore = score;
                }
            }

            return best;
        }

        private static int ScoreTransformTree(Transform root, HashSet<GameObject> protectedObjects)
        {
            if (root == null)
                return int.MinValue;

            int score = 0;
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                Transform t = all[i];
                if (t == null)
                    continue;

                if (protectedObjects.Contains(t.gameObject))
                    score += 1000;

                if (t.name.StartsWith("Drawn_", StringComparison.Ordinal))
                    score += 20;
                else if (t.name.StartsWith("Holstered_", StringComparison.Ordinal))
                    score += 15;
                else if (t.name.Equals("WeaponHolders", StringComparison.Ordinal))
                    score += 10;
                else if (t.name.Equals("RightHandlers", StringComparison.Ordinal))
                    score += 8;
            }

            return score;
        }

        private static bool HasProtectedDescendant(Transform root, HashSet<GameObject> protectedObjects)
        {
            if (root == null)
                return false;

            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                Transform t = all[i];
                if (t != null && protectedObjects.Contains(t.gameObject))
                    return true;
            }

            return false;
        }

        private static void DestroyTransformTree(Transform root, ref int totalRemoved)
        {
            if (root == null)
                return;

            totalRemoved += CountTransformTree(root);
            UnityEngine.Object.DestroyImmediate(root.gameObject);
        }

        private static int CountTransformTree(Transform root)
        {
            if (root == null)
                return 0;

            return root.GetComponentsInChildren<Transform>(true).Length;
        }

        private static string GetTransformPath(Transform t)
        {
            if (t == null)
                return string.Empty;

            return AnimationUtility.CalculateTransformPath(t, t.root);
        }
    }
}
#endif
