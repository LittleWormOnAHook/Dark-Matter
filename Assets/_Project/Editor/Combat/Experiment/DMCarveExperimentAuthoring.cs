using Project.Combat;
using Project.Combat.Experiment.Carve;
using Project.Data;
using Project.EditorTools;
using Project.Interaction;
using Project.SurfaceCarve;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Combat.Experiment
{
    /// <summary>Creates carve experiment ammo, FX prefabs, pickup, and optional scene placement.</summary>
    public static class DMCarveExperimentAuthoring
    {
        public const string AmmoAssetPath = "Assets/_Project/Data/Items/Ammo/DMAmmoFxProfile_CarveExperiment.asset";

        private const string MenuBuild =
            DarkMatterGenesisEditorMenus.CombatExperiment + "Build Carve Experiment Ammo";
        private const string MenuPlace =
            DarkMatterGenesisEditorMenus.CombatExperiment + "Place Carve Pickup In Open Scene";
        public const string PickupPrefabPath = "Assets/_Project/Prefabs/Items/Ammo/CarveExperiment_Pickup.prefab";
        public const string MuzzlePrefabPath = "Assets/_Project/Prefabs/Combat/Experiment/DM_CarveExperiment_MuzzleFlash.prefab";
        public const string TracerPrefabPath = "Assets/_Project/Prefabs/Combat/Experiment/DM_CarveExperiment_Tracer.prefab";

        [MenuItem(MenuBuild, false, DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Combat_Experiment_Build_Carve_Experiment_Ammo)]
        public static void BuildAll()
        {
            EnsureFolders();
            GameObject muzzle = EnsureMuzzlePrefab();
            GameObject tracer = EnsureTracerPrefab();
            DMCarveExperimentAmmoProfile ammo = EnsureAmmoProfile(muzzle, tracer);
            EnsurePickupPrefab(ammo);
            RegisterInItemRegistry(ammo);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[DMCarveExperiment] Built carve experiment ammo + pickup. " +
                "Equip a Gunpowder-compatible weapon, pick up CarveExperiment_Pickup, shoot carvable meshes.");
        }

        [MenuItem(MenuPlace, false, DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Combat_Experiment_Place_Carve_Pickup_In_Open_Scene)]
        public static void PlacePickupInScene()
        {
            BuildAll();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PickupPrefabPath);
            if (prefab == null)
                return;

            Vector3 pos = SceneView.lastActiveSceneView != null
                ? SceneView.lastActiveSceneView.pivot
                : new Vector3(-680f, 22f, -100f);

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = pos;
            Undo.RegisterCreatedObjectUndo(instance, "Place Carve Experiment Pickup");
            Selection.activeGameObject = instance;
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs/Combat/Experiment"))
                AssetDatabase.CreateFolder("Assets/_Project/Prefabs/Combat", "Experiment");
        }

        private static GameObject EnsureMuzzlePrefab()
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(MuzzlePrefabPath);
            if (existing != null)
                return existing;

            GameObject root = new GameObject("DM_CarveExperiment_MuzzleFlash");
            ParticleSystem ps = root.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.12f;
            main.startLifetime = 0.08f;
            main.startSpeed = 4f;
            main.startSize = 0.15f;
            main.maxParticles = 24;
            main.loop = false;
            main.playOnAwake = true;
            main.startColor = new Color(1f, 0.72f, 0.35f, 1f);

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 16) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 18f;
            shape.radius = 0.02f;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, MuzzlePrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject EnsureTracerPrefab()
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(TracerPrefabPath);
            if (existing != null)
                return existing;

            GameObject root = new GameObject("DM_CarveExperiment_Tracer");
            TrailRenderer trail = root.AddComponent<TrailRenderer>();
            trail.time = 0.12f;
            trail.startWidth = 0.06f;
            trail.endWidth = 0.01f;
            trail.minVertexDistance = 0.02f;
            trail.numCornerVertices = 2;
            trail.numCapVertices = 2;
            trail.material = new Material(Shader.Find("HDRP/Unlit"));
            if (trail.material != null)
                trail.material.SetColor("_UnlitColor", new Color(1f, 0.55f, 0.2f, 0.9f));
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, TracerPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static DMCarveExperimentAmmoProfile EnsureAmmoProfile(GameObject muzzle, GameObject tracer)
        {
            DMCarveExperimentAmmoProfile ammo =
                AssetDatabase.LoadAssetAtPath<DMCarveExperimentAmmoProfile>(AmmoAssetPath);
            if (ammo == null)
            {
                ammo = ScriptableObject.CreateInstance<DMCarveExperimentAmmoProfile>();
                AssetDatabase.CreateAsset(ammo, AmmoAssetPath);
            }

            ammo.itemName = "Carve Experiment";
            ammo.itemType = ItemType.Ammo;
            ammo.ammoType = AmmoType.Gunpowder;
            ammo.fxProfile = ammo;
            ammo.tooltipDescription =
                "Lab ammo: slow projectile, orange tracer, aggressive surface carve (Blast). " +
                "Hits all physics layers except the player. For mesh/carve experiments only.";
            ammo.rangedDamage = 8f;
            ammo.rangedRange = 120f;
            ammo.projectileSpeed = 95f;
            ammo.projectileSpreadDegrees = 0.8f;
            ammo.weaponAccuracy = 92f;
            ammo.ammoPerPickup = 200;
            ammo.ammoPickupGrant = 100f;
            ammo.isHitscanBeam = false;
            ammo.delivery = DMAmmoDeliveryMode.Projectile;
            ammo.muzzleFlashPrefab = muzzle;
            ammo.tracerPrefab = tracer;
            ammo.useHitMarks = true;
            ammo.fallBackToCatalog = false;
            ammo.useAllPhysicsLayers = true;
            ammo.forceMeshDeformation = true;
            ammo.allowCarveOnEnemyReceivers = true;
            ammo.bypassWorldImpactCharacterBlocks = true;

            ammo.surfaceDamage.mode = DMSurfaceDamageMode.Custom;
            ammo.surfaceDamage.custom = DMSurfaceDamageSettings.Make(
                DMCarveStyle.Blast,
                radius: 0.42f,
                depth: 0.72f,
                roughness: 1.35f,
                debris: 4,
                debrisScale: 0.42f);

            ItemData standard = AssetDatabase.LoadAssetAtPath<ItemData>(
                "Assets/_Project/Data/Items/ammo/DMAmmoFxProfile_Standard.asset");
            if (standard != null)
            {
                ammo.icon = standard.icon;
                ammo.fireSound = standard.fireSound;
                ammo.projectilePrefab = standard.projectilePrefab;
            }

            EditorUtility.SetDirty(ammo);
            return ammo;
        }

        private static void EnsurePickupPrefab(DMCarveExperimentAmmoProfile ammo)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PickupPrefabPath);
            if (existing != null)
            {
                ItemPickup pickup = existing.GetComponent<ItemPickup>();
                if (pickup != null)
                {
                    pickup.itemData = ammo;
                    EditorUtility.SetDirty(pickup);
                }

                return;
            }

            GameObject ionPickup = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/Items/Ammo/Ion_Pickup.prefab");
            if (ionPickup == null)
            {
                Debug.LogError("[DMCarveExperiment] Ion_Pickup template missing.");
                return;
            }

            GameObject clone = PrefabUtility.InstantiatePrefab(ionPickup) as GameObject;
            clone.name = "CarveExperiment_Pickup";
            ItemPickup itemPickup = clone.GetComponent<ItemPickup>();
            if (itemPickup != null)
            {
                itemPickup.itemData = ammo;
                itemPickup.amount = 200;
            }

            PrefabUtility.SaveAsPrefabAsset(clone, PickupPrefabPath);
            Object.DestroyImmediate(clone);
            ammo.worldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PickupPrefabPath);
            EditorUtility.SetDirty(ammo);
        }

        private static void RegisterInItemRegistry(DMCarveExperimentAmmoProfile ammo)
        {
            ItemRegistry registry = AssetDatabase.LoadAssetAtPath<ItemRegistry>(
                "Assets/_Project/Resources/ItemRegistry.asset");
            if (registry == null || ammo == null)
                return;

            SerializedObject so = new SerializedObject(registry);
            SerializedProperty items = so.FindProperty("items");
            for (int i = 0; i < items.arraySize; i++)
            {
                if (items.GetArrayElementAtIndex(i).objectReferenceValue == ammo)
                {
                    so.ApplyModifiedPropertiesWithoutUndo();
                    return;
                }
            }

            items.InsertArrayElementAtIndex(items.arraySize);
            items.GetArrayElementAtIndex(items.arraySize - 1).objectReferenceValue = ammo;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(registry);
        }
    }
}
