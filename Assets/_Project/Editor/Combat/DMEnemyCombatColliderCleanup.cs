#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using Project.AI.Invector;
using Project.Combat;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Combat
{
    /// <summary>
    /// Strips stale authored hit volumes from enemy prefabs so only runtime <see cref="DMEnemyHitboxRig"/> builds DMHitbox_*.
    /// Ragdoll colliders stay on the prefab but are disabled (death physics unchanged).
    /// </summary>
    public static class DMEnemyCombatColliderCleanup
    {
        private const string Menu = DarkMatterGenesisEditorMenus.Combat + "Hit Marks/";

        private static readonly string[] DefaultPrefabPaths =
        {
            DMEnemyHitMarksBuilder.FredPath,
            DMEnemyHitMarksBuilder.CorruptAndroidPath,
            DMEnemyHitMarksBuilder.RobotPath,
            DMEnemyHitMarksBuilder.HumanoidPath,
            DMEnemyHitMarksBuilder.EnemyFolder + "/The_Evil_One.prefab"
        };

        private static readonly string[] SkipNameContains =
        {
            "TrainingDummy",
            "Gongo"
        };

        [MenuItem(Menu + "Strip Legacy Hit Colliders (enemy prefabs)", false, Project.EditorTools.DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Combat_Hit_Marks_Strip_Legacy_Hit_Colliders_enemy_prefabs)]
        public static void StripDefaultEnemyPrefabsMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[DM Hit Colliders] Exit Play mode first.");
                return;
            }

            string log = StripPrefabs(DefaultPrefabPaths);
            Debug.Log("[DM Hit Colliders] " + log);
        }

        public static string StripPrefabs(string[] prefabPaths)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < prefabPaths.Length; i++)
            {
                string path = prefabPaths[i];
                if (string.IsNullOrEmpty(path) || AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                {
                    sb.Append("skip missing ").Append(path).Append("; ");
                    continue;
                }

                sb.Append(StripPrefab(path));
            }

            AssetDatabase.SaveAssets();
            return sb.ToString();
        }

        public static string StripPrefab(string prefabPath)
        {
            string fileName = Path.GetFileNameWithoutExtension(prefabPath);
            for (int i = 0; i < SkipNameContains.Length; i++)
            {
                if (fileName.IndexOf(SkipNameContains[i], StringComparison.OrdinalIgnoreCase) >= 0)
                    return fileName + " skipped; ";
            }

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                int removedHitboxes = 0;
                int disabledRagdoll = 0;
                int removedProxies = 0;
                int disabledLegacyHitBox = 0;

                Transform[] all = root.GetComponentsInChildren<Transform>(true);
                for (int t = 0; t < all.Length; t++)
                {
                    Transform tr = all[t];
                    if (tr == null || tr == root.transform)
                        continue;

                    if (tr.name.StartsWith(DMEnemyHitboxRig.HitboxObjectPrefix, StringComparison.Ordinal))
                    {
                        UnityEngine.Object.DestroyImmediate(tr.gameObject);
                        removedHitboxes++;
                        continue;
                    }

                    if (IsLegacyInvectorHitBoxNode(tr))
                    {
                        Collider[] legacy = tr.GetComponents<Collider>();
                        for (int c = 0; c < legacy.Length; c++)
                        {
                            if (legacy[c] != null)
                            {
                                legacy[c].enabled = false;
                                disabledLegacyHitBox++;
                            }
                        }

                        continue;
                    }

                    PioneerRagdollBoneDamageProxy proxy = tr.GetComponent<PioneerRagdollBoneDamageProxy>();
                    if (proxy != null && tr.GetComponentInParent<DMEnemyHitboxRig>() == null)
                    {
                        UnityEngine.Object.DestroyImmediate(proxy);
                        removedProxies++;
                    }

                    Rigidbody rb = tr.GetComponent<Rigidbody>();
                    if (rb == null || rb.gameObject == root)
                        continue;

                    Collider[] solids = tr.GetComponents<Collider>();
                    for (int c = 0; c < solids.Length; c++)
                    {
                        Collider col = solids[c];
                        if (col == null || EnemyInvectorHitSetup.IsOutgoingWeaponCollider(col))
                            continue;

                        if (col.GetComponent<DMEnemyHitbox>() != null)
                            continue;

                        if (col.enabled)
                        {
                            col.enabled = false;
                            disabledRagdoll++;
                        }
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                return fileName + " (DMHitbox removed " + removedHitboxes
                    + ", legacy hitBox off " + disabledLegacyHitBox
                    + ", ragdoll off " + disabledRagdoll
                    + ", proxies " + removedProxies + "); ";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static bool IsLegacyInvectorHitBoxNode(Transform tr)
        {
            if (tr == null)
                return false;

            if (tr.name.Equals("hitBox", StringComparison.OrdinalIgnoreCase))
                return true;

            return tr.name.StartsWith("hitBox", StringComparison.OrdinalIgnoreCase)
                && tr.GetComponent<Collider>() != null;
        }
    }
}
#endif
