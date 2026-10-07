using System.Collections.Generic;
using System.Text;
using Project.Events;
using Project.Storage;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.EditorTools.Loot
{
    /// <summary>
    /// Loot plan phase 6: builds the DM chest prefabs and their per-type tint material variants (D9, D10, D20).
    /// <list type="bullet">
    /// <item><c>DM_LootChest_Base</c>: variant of the vendor chest prefab (left untouched, D10) with the NavMeshObstacle
    /// and the uGUI prompt children removed, the IO_Ancient_Cache lid clip / look, a <see cref="DMChestLid"/> and
    /// World Reloot mode. Loot window = UITK, dissolve / timers = DM_LootChestProfile (no per-prefab parts needed).</item>
    /// <item><c>DM_Story_Crate</c> (Story): built from the scene's IO_Ancient_Cache (its loot, labels, scanner), amber tint.</item>
    /// <item><c>DM_Salvage_Cache</c> (Single Loot): a copy of that same cache with a rust tint.</item>
    /// <item><c>DM_Storage_Crate</c> (camp storage): built from SI_storage_crate (DMStorageCrate settings and its
    /// own materials), no loot table.</item>
    /// </list>
    /// Create-only: existing prefabs / materials are kept (rebuilding would change object ids under placed instances).
    /// Works in a preview scene, so open scenes are never touched or saved.
    /// </summary>
    public static class DMLootChestPrefabBuilder
    {
        public const string MenuPath = "Tools/Dark Matter Genesis/Loot/Build Chest Prefabs";

        public const string WorldFolder = "Assets/_Project/Prefabs/World";
        public const string BasePath = WorldFolder + "/DM_LootChest_Base.prefab";
        public const string StoryPath = WorldFolder + "/DM_Story_Crate.prefab";
        public const string SalvagePath = WorldFolder + "/DM_Salvage_Cache.prefab";
        public const string StoragePath = WorldFolder + "/DM_Storage_Crate.prefab";
        public const string MaterialFolder = "Assets/_Project/Materials/Loot";

        const string VendorChestGuid = "833597b2707e3cf429fb53b2f04334d2";
        const string LidClipGuid = "4a3b7161da371ea4b989ba129f46d4f1";
        const string SmokeMaterialGuid = "111ee638dd846464d8a9e6fe3d5f0865";
        const string StorySourcePath = WorldFolder + "/IO_Ancient_Cache.prefab";
        const string StorageSourcePath = "Assets/_Project/Prefabs/Buildings/Library/Equipment/SI_storage_crate.prefab";
        const string MetalScrapPath = "Assets/_Project/Data/Items/Components/metal_scrap.asset";
        const string ElectronicScrapPath = "Assets/_Project/Data/Items/Components/electronic_scrap.asset";
        const string OxygenMiniPath = "Assets/_Project/Data/Items/Consumables/Oxygen Tank Mini.asset";

        // World look of the placed chests: IO_Ancient_Cache (cache shape) and SI_storage_crate / Storage Crate.
        static readonly Vector3 ChestEuler = new Vector3(270f, 0f, 0f);
        static readonly Vector3 CacheScale = new Vector3(0.4253f, 0.59f, 0.8296f);
        static readonly Vector3 StorageScale = new Vector3(0.59f, 0.59f, 0.59f);

        static readonly Color StoryTint = new Color(1f, 0.8f, 0.42f, 1f);
        static readonly Color SalvageTint = new Color(0.95f, 0.48f, 0.3f, 1f);
        static readonly Color SalvageScanColor = new Color(0.95f, 0.48f, 0.3f, 1f);

        [MenuItem(MenuPath, true)]
        static bool ValidateBuildAll()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        [MenuItem(MenuPath)]
        public static void BuildAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[DM Loot] Build Chest Prefabs: exit Play Mode first.");
                return;
            }

            GameObject vendor = LoadByGuid<GameObject>(VendorChestGuid);
            if (vendor == null)
            {
                Debug.LogError("[DM Loot] Build Chest Prefabs: vendor chest prefab not found (guid " + VendorChestGuid + ").");
                return;
            }

            EnsureFolder(MaterialFolder);
            StringBuilder log = new StringBuilder("[DM Loot] Build Chest Prefabs:");
            Scene preview = EditorSceneManager.NewPreviewScene();
            try
            {
                GameObject basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BasePath);
                if (basePrefab == null)
                    basePrefab = BuildBase(vendor, preview, log);
                else
                    log.Append("\n  kept ").Append(BasePath);

                if (basePrefab == null)
                {
                    Debug.LogError(log.Append("\n  base prefab failed; variants skipped.").ToString());
                    return;
                }

                BuildIfMissing(StoryPath, log, () => BuildStory(basePrefab, preview, log));
                BuildIfMissing(SalvagePath, log, () => BuildSalvage(basePrefab, preview, log));
                BuildIfMissing(StoragePath, log, () => BuildStorage(basePrefab, preview, log));
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
            }

            AssetDatabase.SaveAssets();
            Debug.Log(log.ToString());
        }

        static void BuildIfMissing(string path, StringBuilder log, System.Func<GameObject> build)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                log.Append("\n  kept ").Append(path);
                return;
            }

            build();
        }

        // ---------------------------------------------------------------- base

        static GameObject BuildBase(GameObject vendor, Scene preview, StringBuilder log)
        {
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(vendor, preview);
            inst.name = "DM_LootChest_Base";
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localEulerAngles = ChestEuler;
            inst.transform.localScale = CacheScale;

            int nav = StripNavMesh(inst);
            int ui = StripUGui(inst);

            AnimationClip clip = LoadByGuid<AnimationClip>(LidClipGuid);
            Animation anim = inst.GetComponent<Animation>();
            if (anim != null && clip != null)
            {
                SerializedObject so = new SerializedObject(anim);
                so.FindProperty("m_Animation").objectReferenceValue = clip;
                SerializedProperty clips = so.FindProperty("m_Animations");
                clips.arraySize = 1;
                clips.GetArrayElementAtIndex(0).objectReferenceValue = clip;
                so.FindProperty("m_PlayAutomatically").boolValue = false;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // Same open-smoke material as IO_Ancient_Cache.
            ParticleSystemRenderer smoke = inst.GetComponentInChildren<ParticleSystemRenderer>(true);
            Material smokeMaterial = LoadByGuid<Material>(SmokeMaterialGuid);
            if (smoke != null && smokeMaterial != null)
            {
                Material[] mats = smoke.sharedMaterials;
                if (mats.Length > 1)
                {
                    mats[1] = smokeMaterial;
                    smoke.sharedMaterials = mats;
                }
            }

            DmEvents events = inst.GetComponent<DmEvents>();
            if (events != null)
            {
                SerializedObject so = new SerializedObject(events);
                SetString(so, "cacheDisplayName", "Loot Chest");
                SetString(so, "scanLabel", "Loot Chest");
                SerializedProperty slots = so.FindProperty("lootSlots");
                if (slots != null)
                    slots.arraySize = 0;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            SetScanLabel(inst, "Loot Chest", null);

            DMItemCollection collection = inst.GetComponentInChildren<DMItemCollection>(true);
            if (collection != null)
            {
                SerializedObject so = new SerializedObject(collection);
                SetEnum(so, "mode", (int)DMLootChestMode.WorldReloot);
                SetString(so, "chestId", string.Empty);
                SetString(so, "openAnimationName", clip != null ? clip.name : DMItemCollection.DefaultOpenAnimationName);
                SetObject(so, "chestAnimation", anim);
                SetObject(so, "dmEvents", events);
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            DMChestLid lid = inst.GetComponent<DMChestLid>();
            if (lid == null)
                lid = inst.AddComponent<DMChestLid>();
            ConfigureLid(lid, anim, clip != null ? clip.name : DMItemCollection.DefaultOpenAnimationName);

            GameObject saved = Save(inst, BasePath, log);
            if (saved != null)
                log.Append(" (removed ").Append(nav).Append(" NavMesh component(s), ").Append(ui).Append(" uGUI object(s))");
            return saved;
        }

        // ---------------------------------------------------------------- variants

        static GameObject BuildStory(GameObject basePrefab, Scene preview, StringBuilder log)
        {
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab, preview);
            inst.name = "DM_Story_Crate";

            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(StorySourcePath);
            DmEvents events = inst.GetComponent<DmEvents>();
            if (source != null)
            {
                CopyFrom<DmEvents>(source, inst);
                CopyFrom<Project.Interaction.ScannableTarget>(source, inst);
                CopyFrom<OutlineController>(source, inst);
            }
            else
            {
                log.Append("\n  warning: ").Append(StorySourcePath).Append(" missing, Story crate keeps base loot / labels");
            }

            SetMode(inst, DMLootChestMode.Story, events);
            ApplyTint(inst, "Story", StoryTint, log);
            return Save(inst, StoryPath, log);
        }

        static GameObject BuildSalvage(GameObject basePrefab, Scene preview, StringBuilder log)
        {
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab, preview);
            inst.name = "DM_Salvage_Cache";

            DmEvents events = inst.GetComponent<DmEvents>();
            if (events != null)
            {
                SerializedObject so = new SerializedObject(events);
                SetString(so, "cacheDisplayName", "Salvage Cache");
                SetString(so, "scanLabel", "Salvage Cache");
                SerializedProperty color = so.FindProperty("scanColor");
                if (color != null)
                    color.colorValue = SalvageScanColor;
                SerializedProperty slots = so.FindProperty("lootSlots");
                if (slots != null)
                {
                    slots.arraySize = 0;
                    AddSlot(slots, MetalScrapPath, 40, log);
                    AddSlot(slots, ElectronicScrapPath, 15, log);
                    AddSlot(slots, OxygenMiniPath, 1, log);
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            SetScanLabel(inst, "Salvage Cache", SalvageScanColor);
            SetMode(inst, DMLootChestMode.SingleLoot, events);
            ApplyTint(inst, "Salvage", SalvageTint, log);
            return Save(inst, SalvagePath, log);
        }

        static GameObject BuildStorage(GameObject basePrefab, Scene preview, StringBuilder log)
        {
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab, preview);
            inst.name = "DM_Storage_Crate";
            inst.transform.localScale = StorageScale;

            // Storage has no loot table and never registers as a loot chest (plan 7.5).
            DMItemCollection collection = inst.GetComponentInChildren<DMItemCollection>(true);
            if (collection != null)
                Object.DestroyImmediate(collection);
            DmEvents events = inst.GetComponent<DmEvents>();
            if (events != null)
                Object.DestroyImmediate(events);

            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(StorageSourcePath);
            DMStorageCrate sourceCrate = source != null ? source.GetComponentInChildren<DMStorageCrate>(true) : null;
            DMStorageCrate crate = inst.GetComponent<DMStorageCrate>();
            if (crate == null)
                crate = inst.AddComponent<DMStorageCrate>();
            if (sourceCrate != null)
                EditorUtility.CopySerialized(sourceCrate, crate);

            Animation anim = inst.GetComponent<Animation>();
            Transform particle = inst.transform.Find("Particle System");
            Transform trigger = inst.transform.Find("collection");
            SerializedObject so = new SerializedObject(crate);
            SetString(so, "crateId", "camp_storage_01");
            SerializedProperty slotCount = so.FindProperty("slotCount");
            if (slotCount != null && slotCount.intValue <= 0)
                slotCount.intValue = 20;
            SerializedProperty build = so.FindProperty("assignIdOnBuild");
            if (build != null)
                build.boolValue = false;
            SetObject(so, "chestAnimation", anim);
            SetString(so, "openAnimationName", DMStorageCrate.DefaultOpenAnimationName);
            SetObject(so, "openParticle", particle != null ? particle.gameObject : null);
            SetObject(so, "interactCollider", trigger != null ? trigger.GetComponent<Collider>() : null);
            so.ApplyModifiedPropertiesWithoutUndo();

            if (sourceCrate != null)
            {
                GameObject sourceRoot = sourceCrate.gameObject;
                CopyFrom<Project.Interaction.ScannableTarget>(sourceRoot, inst);
                CopyFrom<OutlineController>(sourceRoot, inst);

                // SI_storage_crate's own materials (body and lid), referenced as they are.
                CopyMaterials(sourceRoot.GetComponent<MeshRenderer>(), inst.GetComponent<MeshRenderer>());
                Transform sourceLid = sourceRoot.transform.Find(DMItemCollection.CacheLidChildName);
                Transform lid = inst.transform.Find(DMItemCollection.CacheLidChildName);
                if (sourceLid != null && lid != null)
                    CopyMaterials(sourceLid.GetComponent<MeshRenderer>(), lid.GetComponent<MeshRenderer>());
            }
            else
            {
                log.Append("\n  warning: ").Append(StorageSourcePath).Append(" has no DMStorageCrate, defaults used");
            }

            return Save(inst, StoragePath, log);
        }

        // ---------------------------------------------------------------- helpers

        static GameObject Save(GameObject inst, string path, StringBuilder log)
        {
            // Direct edits (transforms, materials, CopySerialized) must be recorded as instance overrides.
            Transform[] transforms = inst.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(transforms[i].gameObject);
                Component[] components = transforms[i].GetComponents<Component>();
                for (int c = 0; c < components.Length; c++)
                {
                    if (components[c] != null)
                        PrefabUtility.RecordPrefabInstancePropertyModifications(components[c]);
                }
            }

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(inst, path, out bool ok);
            Object.DestroyImmediate(inst);
            log.Append(ok && saved != null ? "\n  created " : "\n  FAILED ").Append(path);
            return ok ? saved : null;
        }

        static int StripNavMesh(GameObject root)
        {
            int removed = 0;
            Component[] components = root.GetComponentsInChildren<Component>(true);
            for (int i = 0; i < components.Length; i++)
            {
                Component c = components[i];
                if (c == null || c is Transform)
                    continue;
                if (c.GetType().Name.Contains("NavMesh"))
                {
                    Object.DestroyImmediate(c);
                    removed++;
                }
            }
            return removed;
        }

        /// <summary>Removes the top-most uGUI objects (Canvas / RectTransform roots), children go with them.</summary>
        static int StripUGui(GameObject root)
        {
            List<GameObject> doomed = new List<GameObject>();
            RectTransform[] rects = root.GetComponentsInChildren<RectTransform>(true);
            for (int i = 0; i < rects.Length; i++)
            {
                RectTransform rect = rects[i];
                if (rect == null || rect.gameObject == root)
                    continue;
                Transform parent = rect.transform.parent;
                if (parent != null && parent is RectTransform)
                    continue;
                doomed.Add(rect.gameObject);
            }

            for (int i = 0; i < doomed.Count; i++)
                Object.DestroyImmediate(doomed[i]);
            return doomed.Count;
        }

        static void SetMode(GameObject inst, DMLootChestMode mode, DmEvents events)
        {
            DMItemCollection collection = inst.GetComponentInChildren<DMItemCollection>(true);
            if (collection == null)
                return;
            SerializedObject so = new SerializedObject(collection);
            SetEnum(so, "mode", (int)mode);
            SetString(so, "chestId", string.Empty);
            if (events != null)
                SetObject(so, "dmEvents", events);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void ConfigureLid(DMChestLid lid, Animation anim, string clipName)
        {
            SerializedObject so = new SerializedObject(lid);
            SetEnum(so, "mode", (int)DMChestLid.LidMode.LegacyClip);
            SetObject(so, "chestAnimation", anim);
            SetString(so, "clipName", clipName);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetScanLabel(GameObject inst, string label, Color? color)
        {
            Project.Interaction.ScannableTarget scan = inst.GetComponent<Project.Interaction.ScannableTarget>();
            if (scan == null)
                return;
            SerializedObject so = new SerializedObject(scan);
            SetString(so, "scanLabel", label);
            if (color.HasValue)
            {
                SerializedProperty c = so.FindProperty("scanColor");
                if (c != null)
                    c.colorValue = color.Value;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void CopyFrom<T>(GameObject source, GameObject target) where T : Component
        {
            T from = source.GetComponent<T>();
            T to = target.GetComponent<T>();
            if (from != null && to != null)
                EditorUtility.CopySerialized(from, to);
        }

        static void CopyMaterials(MeshRenderer from, MeshRenderer to)
        {
            if (from != null && to != null)
                to.sharedMaterials = from.sharedMaterials;
        }

        /// <summary>Per-type tint (D9): HDRP/Lit material variants of the chest's own materials, originals untouched.</summary>
        static void ApplyTint(GameObject inst, string type, Color tint, StringBuilder log)
        {
            Dictionary<Material, Material> variants = new Dictionary<Material, Material>();
            MeshRenderer[] renderers = inst.GetComponentsInChildren<MeshRenderer>(true);
            for (int r = 0; r < renderers.Length; r++)
            {
                Material[] mats = renderers[r].sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    Material source = mats[i];
                    if (source == null)
                        continue;
                    if (!variants.TryGetValue(source, out Material variant))
                    {
                        variant = GetOrCreateTintVariant(source, type, tint, log);
                        variants[source] = variant;
                    }
                    if (variant != null)
                    {
                        mats[i] = variant;
                        changed = true;
                    }
                }
                if (changed)
                    renderers[r].sharedMaterials = mats;
            }
        }

        static Material GetOrCreateTintVariant(Material source, string type, Color tint, StringBuilder log)
        {
            string suffix = SanitizeMaterialSuffix(source.name);
            string path = MaterialFolder + "/DM_LootChest_" + type + "_" + suffix + ".mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
                return existing;

            Material variant = new Material(source.shader);
            variant.parent = source;
            variant.name = "DM_LootChest_" + type + "_" + suffix;
            if (variant.HasProperty("_BaseColor"))
            {
                Color baseColor = source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor") : Color.white;
                variant.SetColor("_BaseColor", new Color(baseColor.r * tint.r, baseColor.g * tint.g, baseColor.b * tint.b, baseColor.a));
            }
            AssetDatabase.CreateAsset(variant, path);
            log.Append("\n  material ").Append(path);
            return variant;
        }

        /// <summary>"Invector-Chest-01" -> "01": no vendor names in DM asset names.</summary>
        static string SanitizeMaterialSuffix(string name)
        {
            string trimmed = name;
            int dash = trimmed.LastIndexOf('-');
            if (dash >= 0 && dash < trimmed.Length - 1)
                trimmed = trimmed.Substring(dash + 1);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < trimmed.Length; i++)
            {
                char c = trimmed[i];
                if (char.IsLetterOrDigit(c))
                    sb.Append(c);
            }
            return sb.Length > 0 ? sb.ToString() : "Mat";
        }

        static void AddSlot(SerializedProperty slots, string itemPath, int amount, StringBuilder log)
        {
            Object item = AssetDatabase.LoadMainAssetAtPath(itemPath);
            if (item == null)
            {
                log.Append("\n  warning: item missing ").Append(itemPath);
                return;
            }

            int index = slots.arraySize;
            slots.arraySize = index + 1;
            SerializedProperty slot = slots.GetArrayElementAtIndex(index);
            SerializedProperty itemProp = slot.FindPropertyRelative("item");
            SerializedProperty amountProp = slot.FindPropertyRelative("amount");
            if (itemProp != null)
                itemProp.objectReferenceValue = item;
            if (amountProp != null)
                amountProp.intValue = amount;
        }

        static void SetString(SerializedObject so, string path, string value)
        {
            SerializedProperty p = so.FindProperty(path);
            if (p != null)
                p.stringValue = value;
        }

        static void SetEnum(SerializedObject so, string path, int value)
        {
            SerializedProperty p = so.FindProperty(path);
            if (p != null)
                p.intValue = value;
        }

        static void SetObject(SerializedObject so, string path, Object value)
        {
            SerializedProperty p = so.FindProperty(path);
            if (p != null)
                p.objectReferenceValue = value;
        }

        static T LoadByGuid<T>(string guid) where T : Object
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<T>(path);
        }

        static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;
            string parent = System.IO.Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(folder));
        }
    }
}
