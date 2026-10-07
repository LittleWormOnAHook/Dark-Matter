#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using Project.Combat;
using Project.Data;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Combat
{
    /// <summary>
    /// Editor check for ammo tracer / projectile prefabs. Vendor VFX often ship self-destruct scripts
    /// (AutoDestroyPS schedules Destroy(gameObject) in Awake, which nothing can cancel and which kills
    /// pooled instances), movers, colliders and rigidbodies that fight CombatProjectile. This strips them
    /// from the DM prefabs the ammo items reference (prefab variants record removed-component overrides;
    /// vendor source prefabs are never touched). Also builds the dedicated Ion projectile prefab.
    /// </summary>
    public static class DMAmmoTracerSanitizer
    {
        private const string Menu = DarkMatterGenesisEditorMenus.Combat + "Ammo/";
        public const string ProjectFolder = "Assets/_Project/";
        public const string PlasmaProjectilePath = "Assets/_Project/Prefabs/Combat/Projectiles/Plasma_Projectile.prefab";
        public const string IonProjectilePath = "Assets/_Project/Prefabs/Combat/Projectiles/Ion_Projectile.prefab";
        public const string IonProfilePath = "Assets/_Project/Data/Items/Ammo/DMAmmoFxProfile_Ion.asset";

        private static readonly HashSet<string> StripBehaviourNames = new HashSet<string>
        {
            "AutoDestroyPS",
            "ProjectileMover",
            "ProjectileMover2D",
            "SFX_SimpleProjectile",
            "SFX_PhysicsMotion",
        };

        [MenuItem(Menu + "Report Vendor Tracer Problems", false, 60)]
        private static void ReportMenu()
        {
            Debug.Log("[DM Ammo] " + Run(apply: false));
        }

        [MenuItem(Menu + "Sanitize Vendor Tracers (strip AutoDestroy, colliders, rigidbodies)", false, 61)]
        private static void SanitizeMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[DM Ammo] Exit Play mode before sanitizing tracers.");
                return;
            }

            Debug.Log("[DM Ammo] " + Run(apply: true));
        }

        [MenuItem(Menu + "Build Ion Projectile Prefab", false, 62)]
        private static void IonMenu()
        {
            Debug.Log("[DM Ammo] " + EnsureIonProjectile());
        }

        /// <summary>Lists (and with apply=true strips) offending components on every tracer / projectile prefab used by an ItemData.</summary>
        public static string Run(bool apply)
        {
            var report = new StringBuilder();
            var paths = CollectPrefabPaths(report);
            int dirtyPrefabs = 0;
            int removed = 0;

            foreach (string path in paths)
            {
                if (!path.StartsWith(ProjectFolder))
                {
                    GameObject vendor = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    int count = vendor != null ? CountOffenders(vendor) : 0;
                    if (count > 0)
                        report.AppendLine($"SKIP vendor prefab {path} ({count} offenders) - point the ammo at a DM variant instead.");
                    continue;
                }

                GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset == null || CountOffenders(asset) == 0)
                    continue;

                if (!apply)
                {
                    report.AppendLine($"{path}: {Describe(asset)}");
                    continue;
                }

                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    int stripped = Strip(root, report, path);
                    if (stripped > 0)
                    {
                        PrefabUtility.SaveAsPrefabAsset(root, path, out bool ok);
                        report.AppendLine($"{path}: stripped {stripped} component(s){(ok ? string.Empty : " - SAVE FAILED")}");
                        removed += stripped;
                        dirtyPrefabs++;
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            string head = apply
                ? $"Sanitized {dirtyPrefabs} prefab(s), removed {removed} component(s) across {paths.Count} tracer/projectile prefab(s)."
                : $"Checked {paths.Count} tracer/projectile prefab(s).";
            return head + (report.Length > 0 ? "\n" + report : " Nothing to fix.");
        }

        private static List<string> CollectPrefabPaths(StringBuilder report)
        {
            var set = new SortedSet<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:ItemData"))
            {
                ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(guid));
                if (item == null)
                    continue;

                Add(set, item.tracerPrefab);
                Add(set, item.projectilePrefab);
            }

            return new List<string>(set);
        }

        private static void Add(SortedSet<string> set, GameObject prefab)
        {
            if (prefab == null)
                return;

            string path = AssetDatabase.GetAssetPath(prefab);
            if (!string.IsNullOrEmpty(path) && path.EndsWith(".prefab"))
                set.Add(path);
        }

        private static bool IsStripBehaviour(Component c)
        {
            return c is MonoBehaviour && StripBehaviourNames.Contains(c.GetType().Name);
        }

        private static bool IsPhysics(Component c)
        {
            return c is Collider || c is Collider2D || c is Rigidbody || c is Rigidbody2D;
        }

        private static int CountOffenders(GameObject root)
        {
            int count = 0;
            foreach (Component c in root.GetComponentsInChildren<Component>(true))
            {
                if (c != null && (IsStripBehaviour(c) || IsPhysics(c)))
                    count++;
            }

            return count;
        }

        private static string Describe(GameObject root)
        {
            var parts = new List<string>();
            foreach (Component c in root.GetComponentsInChildren<Component>(true))
            {
                if (c != null && (IsStripBehaviour(c) || IsPhysics(c)))
                    parts.Add(c.gameObject.name + "." + c.GetType().Name);
            }

            return string.Join(", ", parts);
        }

        private static int Strip(GameObject root, StringBuilder report, string path)
        {
            int stripped = 0;
            // Behaviours first (movers RequireComponent(Rigidbody)), then colliders, then bodies.
            stripped += StripWhere(root, IsStripBehaviour, report, path);
            stripped += StripWhere(root, c => c is Collider || c is Collider2D, report, path);
            stripped += StripWhere(root, c => c is Rigidbody || c is Rigidbody2D, report, path);
            return stripped;
        }

        private static int StripWhere(GameObject root, System.Func<Component, bool> match, StringBuilder report, string path)
        {
            int stripped = 0;
            foreach (Component c in root.GetComponentsInChildren<Component>(true))
            {
                if (c == null || !match(c))
                    continue;

                string label = c.gameObject.name + "." + c.GetType().Name;
                try
                {
                    Object.DestroyImmediate(c);
                    stripped++;
                }
                catch (System.Exception e)
                {
                    report.AppendLine($"{path}: could not remove {label}: {e.Message}");
                }
            }

            return stripped;
        }

        /// <summary>
        /// Ion used a VFX prefab as its projectile (CombatProjectile was AddComponent'd at runtime).
        /// Creates Prefabs/Combat/Projectiles/Ion_Projectile (same shape as Plasma_Projectile, radius 0.08)
        /// and points DMAmmoFxProfile_Ion at it; the Ion tracer stays as the visual.
        /// </summary>
        public static string EnsureIonProjectile()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return "Exit Play mode first.";

            if (AssetDatabase.LoadAssetAtPath<GameObject>(IonProjectilePath) == null)
            {
                if (!AssetDatabase.CopyAsset(PlasmaProjectilePath, IonProjectilePath))
                    return "Could not copy " + PlasmaProjectilePath;

                AssetDatabase.ImportAsset(IonProjectilePath);
                GameObject contents = PrefabUtility.LoadPrefabContents(IonProjectilePath);
                try
                {
                    contents.name = "Ion_Projectile";
                    CombatProjectile projectile = contents.GetComponent<CombatProjectile>();
                    if (projectile != null)
                    {
                        var so = new SerializedObject(projectile);
                        SerializedProperty radius = so.FindProperty("radius");
                        if (radius != null)
                            radius.floatValue = 0.08f;
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }

                    PrefabUtility.SaveAsPrefabAsset(contents, IonProjectilePath);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }
            }

            GameObject ion = AssetDatabase.LoadAssetAtPath<GameObject>(IonProjectilePath);
            DMAmmoFxProfile profile = AssetDatabase.LoadAssetAtPath<DMAmmoFxProfile>(IonProfilePath);
            if (profile == null || ion == null)
                return "Ion profile or projectile missing.";

            if (profile.tracerPrefab == null && profile.projectilePrefab != null && profile.projectilePrefab != ion)
                profile.tracerPrefab = profile.projectilePrefab;

            if (profile.projectilePrefab != ion)
            {
                profile.projectilePrefab = ion;
                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssetIfDirty(profile);
            }

            return $"Ion projectile = {IonProjectilePath}, tracer = {(profile.tracerPrefab != null ? AssetDatabase.GetAssetPath(profile.tracerPrefab) : "none")}";
        }
    }
}
#endif
