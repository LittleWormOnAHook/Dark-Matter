using MalbersAnimations.Controller;
using Project.AI;
using Project.Creatures;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Project.EditorTools.Creatures
{
    /// <summary>
    /// Phase 5 world wire-up: B1 Lifeform encounter table + quadruped NavMesh/collider checks.
    /// </summary>
    public static class DMICreatureWorldWireUtility
    {
        public const string B1LifeformTablePath =
            "Assets/_Project/Data/Encounters/SurfaceEncounterTable_B1_SulfurPlains.asset";

        public const string SulfurHoundPrefabPath =
            "Assets/_Project/Prefabs/Creatures/Sulfur_Hound.prefab";

        /// <summary>
        /// Ensures B1 Sulfur Plains surface encounter table includes Sulfur Hound as Lifeform.
        /// </summary>
        public static SurfaceEncounterTable EnsureB1LifeformEncounterTable(out string message)
        {
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.EncountersData);

            GameObject sulfurHound = AssetDatabase.LoadAssetAtPath<GameObject>(SulfurHoundPrefabPath);
            if (sulfurHound == null)
            {
                DMICreatureDefinition definition = DMICreaturePrefabBuilder.EnsureSulfurHoundDefinition();
                sulfurHound = DMICreaturePrefabBuilder.BuildCreature(definition, null, out _);
            }

            if (sulfurHound == null)
            {
                message = "Sulfur_Hound.prefab missing — build it from Creatures Manager first.";
                return null;
            }

            if (!EnemyPrefabResolver.IsSpawnReady(sulfurHound))
            {
                message =
                    "Sulfur Hound prefab failed spawn-ready check (needs EnemyHealth + DMICreatureBridge + NavMeshAgent).";
                Debug.LogWarning(message, sulfurHound);
            }

            SurfaceEncounterTable table = AssetDatabase.LoadAssetAtPath<SurfaceEncounterTable>(B1LifeformTablePath);
            if (table == null)
            {
                table = ScriptableObject.CreateInstance<SurfaceEncounterTable>();
                AssetDatabase.CreateAsset(table, B1LifeformTablePath);
            }

            SerializedObject so = new SerializedObject(table);
            SerializedProperty entries = so.FindProperty("entries");
            if (entries == null)
            {
                message = "SurfaceEncounterTable.entries property not found.";
                return null;
            }

            int existingIndex = FindEntryIndex(entries, sulfurHound);
            bool isNewEntry = existingIndex < 0;
            if (isNewEntry)
            {
                entries.arraySize++;
                existingIndex = entries.arraySize - 1;
            }

            SerializedProperty entry = entries.GetArrayElementAtIndex(existingIndex);
            entry.FindPropertyRelative("threatKind").enumValueIndex = (int)SurfaceThreatKind.Lifeform;
            entry.FindPropertyRelative("prefab").objectReferenceValue = sulfurHound;
            int weight = entry.FindPropertyRelative("weight").intValue;
            entry.FindPropertyRelative("weight").intValue = isNewEntry ? 3 : Mathf.Max(1, weight);
            entry.FindPropertyRelative("behaviorPreset").enumValueIndex = (int)EnemyBehaviorPreset.Custom;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();

            message =
                $"B1 Lifeform encounter table ready at {B1LifeformTablePath} " +
                $"(Sulfur Hound, SurfaceThreatKind.Lifeform, weight={entry.FindPropertyRelative("weight").intValue}).";
            return table;
        }

        /// <summary>
        /// Verifies quadruped collider footprint + child NavMeshAgent on a creature root (or prefab asset).
        /// </summary>
        public static bool ValidateQuadrupedSetup(GameObject root, out string report)
        {
            if (root == null)
            {
                report = "Root is null.";
                return false;
            }

            GameObject instance = root;
            bool createdTemp = false;
            if (PrefabUtility.IsPartOfPrefabAsset(root))
            {
                instance = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(root));
                createdTemp = true;
            }

            try
            {
                NavMeshAgent agent = instance.GetComponentInChildren<NavMeshAgent>(true);
                Collider bodyCollider = FindPrimaryBodyCollider(instance);
                DMICreatureBridge bridge = instance.GetComponent<DMICreatureBridge>();
                EnemyHealth health = instance.GetComponent<EnemyHealth>();
                MAnimal animal = instance.GetComponent<MAnimal>() ?? instance.GetComponentInChildren<MAnimal>(true);

                bool ok = agent != null && bodyCollider != null && bridge != null && health != null && animal != null;
                report =
                    $"NavMeshAgent={(agent != null ? agent.gameObject.name : "MISSING")} " +
                    $"Collider={(bodyCollider != null ? bodyCollider.GetType().Name : "MISSING")} " +
                    $"Bridge={(bridge != null)} Health={(health != null)} MAnimal={(animal != null)} " +
                    $"Result={(ok ? "OK" : "FAIL")}";

                if (agent != null && bodyCollider is CapsuleCollider capsule)
                {
                    // Keep agent radius from drifting far below visual footprint.
                    float visualRadius = EstimateHorizontalRadius(instance);
                    if (visualRadius > 0.05f && agent.radius < visualRadius * 0.35f)
                    {
                        report += $" | note: agent.radius={agent.radius:F2} vs visual~{visualRadius:F2}";
                    }

                    _ = capsule;
                }

                return ok;
            }
            finally
            {
                if (createdTemp)
                    PrefabUtility.UnloadPrefabContents(instance);
            }
        }

        public static bool EnsureQuadrupedCollider(GameObject root)
        {
            if (root == null)
                return false;

            CapsuleCollider capsule = root.GetComponent<CapsuleCollider>();
            if (capsule == null)
            {
                Collider existing = FindPrimaryBodyCollider(root);
                if (existing is CapsuleCollider existingCapsule && existing.transform == root.transform)
                    capsule = existingCapsule;
            }

            if (capsule == null)
            {
                capsule = root.AddComponent<CapsuleCollider>();
                capsule.isTrigger = false;
            }

            // Quadruped body: Z-forward capsule fitted to current skinned visual.
            capsule.direction = 2;
            FitCapsuleToRenderers(root, capsule);
            return true;
        }

        private static int FindEntryIndex(SerializedProperty entries, GameObject prefab)
        {
            for (int i = 0; i < entries.arraySize; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("prefab").objectReferenceValue == prefab)
                    return i;
            }

            return -1;
        }

        private static Collider FindPrimaryBodyCollider(GameObject root)
        {
            Collider[] colliders = root.GetComponents<Collider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider != null && !collider.isTrigger)
                    return collider;
            }

            // Malbers often keeps body capsule on root or deep child; prefer non-trigger capsule/box.
            CapsuleCollider[] capsules = root.GetComponentsInChildren<CapsuleCollider>(true);
            for (int i = 0; i < capsules.Length; i++)
            {
                if (!capsules[i].isTrigger)
                    return capsules[i];
            }

            BoxCollider[] boxes = root.GetComponentsInChildren<BoxCollider>(true);
            for (int i = 0; i < boxes.Length; i++)
            {
                if (!boxes[i].isTrigger && boxes[i].transform == root.transform)
                    return boxes[i];
            }

            return null;
        }

        private static float EstimateHorizontalRadius(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
                return 0f;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return Mathf.Max(bounds.extents.x, bounds.extents.z);
        }

        private static void FitCapsuleToRenderers(GameObject root, CapsuleCollider capsule)
        {
            Bounds bounds;
            SkinnedMeshRenderer smr = null;
            Transform meshTransform = root.transform.Find("Mesh");
            if (meshTransform != null)
                smr = meshTransform.GetComponent<SkinnedMeshRenderer>();
            if (smr == null)
                smr = root.GetComponentInChildren<SkinnedMeshRenderer>(true);

            if (smr != null && smr.sharedMesh != null)
            {
                bounds = smr.bounds;
            }
            else
            {
                Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
                if (renderers == null || renderers.Length == 0)
                    return;

                bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    if (renderers[i] != null && renderers[i].enabled)
                        bounds.Encapsulate(renderers[i].bounds);
                }
            }

            Vector3 localCenter = root.transform.InverseTransformPoint(bounds.center);
            float bodyWidth = bounds.size.x;
            float bodyLength = bounds.size.z;
            float bodyHeight = bounds.size.y;
            capsule.center = new Vector3(0f, Mathf.Max(0.12f, localCenter.y), localCenter.z);
            capsule.radius = Mathf.Clamp(Mathf.Max(bodyWidth, bodyHeight) * 0.28f, 0.12f, 0.55f);
            capsule.height = Mathf.Max(capsule.radius * 2.2f, bodyLength * 0.92f);
            capsule.direction = 2;
        }
    }
}
