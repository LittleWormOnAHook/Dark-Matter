using FORGE3D;
using Project.Combat;
using Project.Combat.Turrets;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Combat
{
    /// <summary>
    /// Bakes FORGE3D constructor meshes into placeable DM turret prefabs (building equipment + combat library).
    /// </summary>
    public static class DMTurretPrefabBakeUtility
    {
        public const string ConstructorBasePath =
            "Assets/FORGE3D/Sci-Fi Effects/Turrets/Prefabs/ConstructorBase.prefab";
        public const string DatabasePath =
            "Assets/FORGE3D/Sci-Fi Effects/Turrets/Database/database.asset";
        public const string SkeletonTemplatePath =
            "Assets/FORGE3D/Sci-Fi Effects/Turrets/Prefabs/TURRET_NEW_SKELETON_TEMPLATE.prefab";
        public const string EquipmentFolder =
            "Assets/_Project/Prefabs/Buildings/Library/Equipment";
        public const string CombatTurretFolder =
            "Assets/_Project/Prefabs/Combat/Turrets";
        public const string DefaultEquipmentPrefabPath = EquipmentFolder + "/SI_turret.prefab";
        public const string EquipmentPartToken = "si_turret";

        public static bool TryGetTemplateInfo(int templateIndex, out string displayName, out string slug)
        {
            displayName = "Turret";
            slug = "turret";
            F3DTurretScriptable db = AssetDatabase.LoadAssetAtPath<F3DTurretScriptable>(DatabasePath);
            if (db == null || db.Turrets == null || db.Turrets.Count == 0)
                return false;

            int index = Mathf.Clamp(templateIndex, 0, db.Turrets.Count - 1);
            displayName = string.IsNullOrWhiteSpace(db.Turrets[index].Name)
                ? "Turret"
                : db.Turrets[index].Name.Trim();
            slug = SlugFromTemplateDisplayName(displayName);
            return true;
        }

        public static string EquipmentPartSuffix(int templateIndex)
        {
            TryGetTemplateInfo(templateIndex, out _, out string slug);
            return EquipmentPartToken + "_" + slug;
        }

        public static string EquipmentPrefabPath(int templateIndex)
        {
            TryGetTemplateInfo(templateIndex, out _, out string slug);
            return EquipmentFolder + "/SI_turret_" + slug + ".prefab";
        }

        public static string CombatPrefabObjectName(int templateIndex)
        {
            TryGetTemplateInfo(templateIndex, out string displayName, out string slug);
            if (slug == "laser")
                return "Ion Turret";

            string core = displayName.Trim();
            if (core.StartsWith("Turret ", System.StringComparison.OrdinalIgnoreCase))
                core = core.Substring(7).Trim();
            if (core.EndsWith(" Turret", System.StringComparison.OrdinalIgnoreCase))
                return core;
            return core + " Turret";
        }

        public static string CombatPrefabPath(int templateIndex)
        {
            return CombatTurretFolder + "/" + CombatPrefabObjectName(templateIndex) + ".prefab";
        }

        public static GameObject BakeCombatPrefabFromTemplateIndex(int templateIndex)
        {
            string path = CombatPrefabPath(templateIndex);
            string objectName = CombatPrefabObjectName(templateIndex);
            DMTurretRuntime settingsDonor = TryCloneRuntimeFromPrefab(path);

            GameObject prefab = BakePrefabFromTemplateIndex(templateIndex, path, objectName);
            if (prefab == null)
                return null;

            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (settingsDonor != null)
                    PreserveRuntimeSettings(settingsDonor, contents);
                else
                    ApplyTemplateCombatDefaults(contents, templateIndex);
                WireRuntimeReferences(contents);
                PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
                if (settingsDonor != null)
                    Object.DestroyImmediate(settingsDonor.gameObject);
            }

            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        static DMTurretRuntime TryCloneRuntimeFromPrefab(string prefabPath)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existing == null)
                return null;

            GameObject clone = Object.Instantiate(existing);
            clone.hideFlags = HideFlags.HideAndDontSave;
            return clone.GetComponent<DMTurretRuntime>();
        }

        public static int SyncAllCombatTurretsFromForge3D()
        {
            F3DTurretScriptable db = AssetDatabase.LoadAssetAtPath<F3DTurretScriptable>(DatabasePath);
            if (db == null || db.Turrets == null || db.Turrets.Count == 0)
            {
                Debug.LogWarning("[DM Turret Bake] FORGE3D database empty — cannot sync Combat/Turrets.");
                return 0;
            }

            EnsureFolderForAsset(CombatTurretFolder + "/placeholder.prefab");
            int ok = 0;
            for (int i = 0; i < db.Turrets.Count; i++)
            {
                if (BakeCombatPrefabFromTemplateIndex(i) != null)
                    ok++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[DM Turret Bake] Synced " + ok + " / " + db.Turrets.Count + " combat turrets → "
                + CombatTurretFolder);
            return ok;
        }

        public static void WireRuntimeReferences(GameObject root)
        {
            if (root == null)
                return;

            F3DTurret forge = root.GetComponent<F3DTurret>();
            F3DFXController fx = root.GetComponent<F3DFXController>() ?? root.GetComponentInChildren<F3DFXController>(true);
            if (forge != null)
            {
                if (forge.Swivel == null)
                    forge.Swivel = FindNamed(root.transform, "SWIVEL", "Swivel")?.gameObject;
                if (forge.Mount == null)
                    forge.Mount = FindNamed(root.transform, "MOUNT", "Mount")?.gameObject;
                if (!forge.enabled && forge.Swivel != null && forge.Mount != null)
                    forge.enabled = true;
            }

            Transform barrel = ResolveBarrelTransform(root.transform, forge);
            Camera seat = DMTurretPlayerCameraSlot.Ensure(root);

            DMTurretRuntime runtime = root.GetComponent<DMTurretRuntime>();
            if (runtime == null)
                return;

            SerializedObject so = new SerializedObject(runtime);
            so.FindProperty("forgeTurret").objectReferenceValue = forge;
            so.FindProperty("fxController").objectReferenceValue = fx;
            so.FindProperty("barrel").objectReferenceValue = barrel;
            so.FindProperty("playerSeatCamera").objectReferenceValue = seat;
            SerializedProperty databaseProp = so.FindProperty("barrelDatabase");
            if (databaseProp != null && databaseProp.objectReferenceValue == null)
                databaseProp.objectReferenceValue = AssetDatabase.LoadAssetAtPath<F3DTurretScriptable>(DatabasePath);
            so.ApplyModifiedPropertiesWithoutUndo();
            runtime.ApplyDesignerVisuals();
            // Authority → FORGE only; never read F3D back into tracking (Capture is explicit in inspector).
            runtime.ApplyTrackingToForge();
            runtime.ApplyVendorFxToController();
            WireAlarmLight(root);
        }

        /// <summary>Optional swivel Alarm sphere → <see cref="DMTurretAlarmLight"/> on runtime root.</summary>
        public static void WireAlarmLight(GameObject root)
        {
            if (root == null)
                return;

            DMTurretRuntime runtime = root.GetComponent<DMTurretRuntime>();
            if (runtime == null)
                return;

            DMTurretAlarmEnsure.EnsureAlarmSphere(root);

            DMTurretAlarmLight alarm = root.GetComponent<DMTurretAlarmLight>();
            if (alarm == null)
                alarm = root.AddComponent<DMTurretAlarmLight>();

            Renderer alarmRenderer = DMTurretAlarmResolve.TryFindAlarmRenderer(root.transform);
            if (alarmRenderer == null)
                return;

            SerializedObject alarmSo = new SerializedObject(alarm);
            alarmSo.FindProperty("targetRenderer").objectReferenceValue = alarmRenderer;
            alarmSo.ApplyModifiedPropertiesWithoutUndo();
            alarm.SetTargetRenderer(alarmRenderer);
            EditorUtility.SetDirty(alarm);
        }

        static Transform ResolveBarrelTransform(Transform root, F3DTurret forge)
        {
            Transform barrel = FindNamed(root, "BARREL", "SOCKET_WEAPON");
            if (barrel != null)
                return barrel;
            if (forge != null && forge.Mount != null)
                return forge.Mount.transform;
            return root;
        }

        static void ApplyTemplateCombatDefaults(GameObject root, int templateIndex)
        {
            TryGetTemplateInfo(templateIndex, out _, out string slug);
            DMTurretRuntime runtime = root.GetComponent<DMTurretRuntime>();
            if (runtime == null)
                return;

            SerializedObject so = new SerializedObject(runtime);
            so.FindProperty("faction").enumValueIndex = (int)DMTurretFaction.Player;
            so.FindProperty("controlMode").enumValueIndex = (int)DMTurretControlMode.Auto;
            so.FindProperty("placedByPlayer").boolValue = true;
            so.FindProperty("useProfileDamage").boolValue = true;

            AmmoType ammo = ResolveTemplateAmmoType(slug);
            so.FindProperty("ammoType").enumValueIndex = (int)ammo;
            SerializedProperty ammoMode = so.FindProperty("ammoMode");
            if (ammoMode != null)
                ammoMode.enumValueIndex = (int)DMTurretAmmoMode.Forge3DVfx;
            SerializedProperty vendorFx = so.FindProperty("vendorFxType");
            if (vendorFx != null)
                vendorFx.enumValueIndex = (int)ResolveTemplateVendorFx(slug);
            SerializedProperty barrelKind = so.FindProperty("barrelKind");
            if (barrelKind != null)
                barrelKind.enumValueIndex = (int)ResolveTemplateBarrel(slug);

            F3DTurretScriptable db = AssetDatabase.LoadAssetAtPath<F3DTurretScriptable>(DatabasePath);
            SerializedProperty databaseProp = so.FindProperty("barrelDatabase");
            if (databaseProp != null)
                databaseProp.objectReferenceValue = db;
            GameObject templateBarrel = ResolveTemplateBarrelPrefab(db, templateIndex);
            SerializedProperty barrelId = so.FindProperty("databaseBarrelId");
            SerializedProperty barrelPrefab = so.FindProperty("databaseBarrelPrefab");
            if (barrelId != null)
                barrelId.stringValue = templateBarrel != null ? templateBarrel.name : string.Empty;
            if (barrelPrefab != null)
                barrelPrefab.objectReferenceValue = templateBarrel;

            so.ApplyModifiedPropertiesWithoutUndo();
            runtime.ApplyDesignerVisuals();
        }

        static GameObject ResolveTemplateBarrelPrefab(F3DTurretScriptable db, int templateIndex)
        {
            if (db == null || db.Turrets == null || db.Turrets.Count == 0)
                return null;

            int index = Mathf.Clamp(templateIndex, 0, db.Turrets.Count - 1);
            TurretStructure structure = db.Turrets[index];
            if (structure == null || structure.WeaponBarrels == null)
                return null;

            for (int i = 0; i < structure.WeaponBarrels.Count; i++)
            {
                if (structure.WeaponBarrels[i] != null)
                    return structure.WeaponBarrels[i];
            }

            return null;
        }

        static F3DFXType ResolveTemplateVendorFx(string slug)
        {
            if (string.IsNullOrEmpty(slug))
                return F3DFXType.Vulcan;
            if (slug == "laser")
                return F3DFXType.LaserImpulse;
            if (slug.StartsWith("electric", System.StringComparison.Ordinal))
                return F3DFXType.LightningGun;
            if (slug.StartsWith("mortar", System.StringComparison.Ordinal)
                || slug == "mlrs"
                || slug == "heavymissile")
                return F3DFXType.Seeker;
            if (slug.StartsWith("repeater", System.StringComparison.Ordinal))
                return F3DFXType.Vulcan;
            return F3DFXType.Vulcan;
        }

        static DMTurretBarrelKind ResolveTemplateBarrel(string slug)
        {
            if (string.IsNullOrEmpty(slug))
                return DMTurretBarrelKind.Auto;
            if (slug == "laser")
                return DMTurretBarrelKind.Laser;
            if (slug.StartsWith("electric", System.StringComparison.Ordinal))
                return DMTurretBarrelKind.Electric;
            if (slug.StartsWith("mortar", System.StringComparison.Ordinal))
                return DMTurretBarrelKind.Mortar;
            if (slug == "mlrs" || slug == "heavymissile")
                return DMTurretBarrelKind.HeavyMissile;
            if (slug.StartsWith("repeater", System.StringComparison.Ordinal))
                return DMTurretBarrelKind.Repeater;
            return DMTurretBarrelKind.Auto;
        }

        static AmmoType ResolveTemplateAmmoType(string slug)
        {
            if (string.IsNullOrEmpty(slug))
                return AmmoType.Gunpowder;
            if (slug == "laser")
                return AmmoType.Ion;
            if (slug.StartsWith("electric", System.StringComparison.Ordinal))
                return AmmoType.Electricity;
            if (slug.StartsWith("mortar", System.StringComparison.Ordinal)
                || slug == "mlrs"
                || slug == "heavymissile")
                return AmmoType.Explosive;
            if (slug.StartsWith("repeater", System.StringComparison.Ordinal))
                return AmmoType.Gunpowder;
            return AmmoType.Gunpowder;
        }

        public static int WireAllSavedTurretPrefabReferences()
        {
            int ok = 0;
            ok += WirePrefabsInFolder(EquipmentFolder);
            ok += WirePrefabsInFolder(CombatTurretFolder);
            AssetDatabase.SaveAssets();
            Debug.Log("[DM Turret Bake] Wired DMTurretRuntime references on " + ok + " prefab(s).");
            return ok;
        }

        static int WirePrefabsInFolder(string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder))
                return 0;

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
            int ok = 0;
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null || prefab.GetComponent<DMTurretRuntime>() == null)
                    continue;

                GameObject contents = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    WireRuntimeReferences(contents);
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                    ok++;
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }
            }

            return ok;
        }

        static void PreserveRuntimeSettings(DMTurretRuntime previous, GameObject bakedContentsRoot)
        {
            DMTurretRuntime baked = bakedContentsRoot.GetComponent<DMTurretRuntime>();
            if (previous == null || baked == null)
                return;

            EditorUtility.CopySerializedIfDifferent(previous, baked);
        }

        public static string SlugFromTemplateDisplayName(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return "turret";

            string s = displayName.Trim();
            if (s.StartsWith("Turret ", System.StringComparison.OrdinalIgnoreCase))
                s = s.Substring(7).Trim();
            if (string.IsNullOrEmpty(s))
                return "turret";

            char[] chars = s.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (char.IsLetterOrDigit(chars[i]))
                    chars[i] = char.ToLowerInvariant(chars[i]);
                else
                    chars[i] = '_';
            }

            string slug = new string(chars).Trim('_');
            while (slug.Contains("__"))
                slug = slug.Replace("__", "_");
            return string.IsNullOrEmpty(slug) ? "turret" : slug;
        }

        public static GameObject AddPlaceableToScene(int templateIndex, Vector3 position, Quaternion rotation)
        {
            GameObject root = CreateBakedInstance(templateIndex, sanitizeName: true);
            if (root == null)
                return null;

            root.transform.SetPositionAndRotation(position, rotation);
            Undo.RegisterCreatedObjectUndo(root, "Add placeable turret");
            Selection.activeGameObject = root;
            return root;
        }

        public static GameObject BakePrefabFromTemplateIndex(
            int templateIndex,
            string assetPath,
            string objectName = null)
        {
            GameObject root = CreateBakedInstance(templateIndex, sanitizeName: false);
            if (root == null)
                return null;

            try
            {
                if (!string.IsNullOrEmpty(objectName))
                    root.name = objectName;
                EnsureFolderForAsset(assetPath);
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, assetPath);
                if (prefab != null)
                    Debug.Log("[DM Turret Bake] Saved " + assetPath);
                return prefab;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        public static GameObject BakePrefabFromSceneHierarchy(GameObject sceneRoot, string assetPath)
        {
            if (sceneRoot == null || !sceneRoot.scene.IsValid())
            {
                Debug.LogWarning("[DM Turret Bake] Select a scene turret root (with F3DTurret or F3DTurretConstructor).");
                return null;
            }

            F3DTurretConstructor constructor = sceneRoot.GetComponentInParent<F3DTurretConstructor>();
            GameObject source = constructor != null ? constructor.gameObject : sceneRoot;
            GameObject duplicate = Object.Instantiate(source);
            try
            {
                duplicate.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                PrepareBakedRoot(duplicate, runConstructorFromIndex: constructor != null ? constructor.turretIndex : -1);
                EnsureFolderForAsset(assetPath);
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(duplicate, assetPath);
                if (prefab != null)
                    Debug.Log("[DM Turret Bake] Saved scene turret → " + assetPath);
                return prefab;
            }
            finally
            {
                Object.DestroyImmediate(duplicate);
            }
        }

        static GameObject CreateBakedInstance(int templateIndex, bool sanitizeName)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(ConstructorBasePath);
            if (source == null)
            {
                Debug.LogWarning("[DM Turret Bake] Missing " + ConstructorBasePath);
                return null;
            }

            TryGetTemplateInfo(templateIndex, out string displayName, out string slug);
            string objectName = "SI_turret_" + slug;

            GameObject root = (GameObject)PrefabUtility.InstantiatePrefab(source);
            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            root.name = sanitizeName ? objectName : "SI_turret";
            PrepareBakedRoot(root, runConstructorFromIndex: templateIndex);
            return root;
        }

        public static void PrepareBakedRoot(GameObject root, int runConstructorFromIndex)
        {
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;

            F3DTurretConstructor constructor = root.GetComponent<F3DTurretConstructor>();
            F3DTurretScriptable db = AssetDatabase.LoadAssetAtPath<F3DTurretScriptable>(DatabasePath);
            if (constructor != null && db != null && db.Turrets != null && db.Turrets.Count > 0)
            {
                int index = runConstructorFromIndex >= 0
                    ? Mathf.Clamp(runConstructorFromIndex, 0, db.Turrets.Count - 1)
                    : Mathf.Clamp(constructor.turretIndex, 0, db.Turrets.Count - 1);
                constructor.turretIndex = index;
                constructor.UpdateFullTurret(db.Turrets[index]);
            }

            if (root.transform.childCount == 0)
            {
                GameObject skeleton = AssetDatabase.LoadAssetAtPath<GameObject>(SkeletonTemplatePath);
                if (skeleton != null)
                {
                    GameObject model = Object.Instantiate(skeleton);
                    model.name = "Model";
                    model.transform.SetParent(root.transform, false);
                }
            }

            StripDemoInput(root);
            EnsureForgeAndDmComponents(root);
            if (constructor != null)
                Object.DestroyImmediate(constructor);

            RecenterVisualToRoot(root);
            FitRootBoxCollider(root);
            WireRuntimeReferences(root);

            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
        }

        /// <summary>Fix pivot on a live scene instance without moving the mesh in the world.</summary>
        public static void RecenterPlacedInstance(GameObject root)
        {
            if (root == null || !TryGetRendererBounds(root, out Bounds bounds))
                return;

            Vector3 pivotWorld = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            Vector3 delta = pivotWorld - root.transform.position;
            ShiftDirectChildren(root.transform, -delta);
            root.transform.position = pivotWorld;
            FitRootBoxCollider(root);
        }

        static void StripDemoInput(GameObject root)
        {
            F3DPlayerTurretController demo = root.GetComponent<F3DPlayerTurretController>();
            if (demo != null)
                Object.DestroyImmediate(demo);
            F3DTurretUI ui = root.GetComponent<F3DTurretUI>();
            if (ui != null)
                Object.DestroyImmediate(ui);
            F3DPlayerController legacy = root.GetComponent<F3DPlayerController>();
            if (legacy != null)
                Object.DestroyImmediate(legacy);
        }

        static void EnsureForgeAndDmComponents(GameObject root)
        {
            if (root.GetComponent<F3DTurret>() == null)
                root.AddComponent<F3DTurret>();
            F3DFXController fx = root.GetComponent<F3DFXController>();
            if (fx == null)
                fx = root.AddComponent<F3DFXController>();
            WireFx(fx, root.transform);

            DMTurretRuntime runtime = root.GetComponent<DMTurretRuntime>();
            if (runtime == null)
                runtime = root.AddComponent<DMTurretRuntime>();
            SerializedObject so = new SerializedObject(runtime);
            so.FindProperty("faction").enumValueIndex = (int)DMTurretFaction.Player;
            so.FindProperty("controlMode").enumValueIndex = (int)DMTurretControlMode.Auto;
            so.FindProperty("placedByPlayer").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (root.GetComponent<DMTurretHealth>() == null)
                root.AddComponent<DMTurretHealth>();
            if (root.GetComponent<DMTurretBrain>() == null)
                root.AddComponent<DMTurretBrain>();
            if (root.GetComponent<DMTurretInteract>() == null)
                root.AddComponent<DMTurretInteract>();

            WireRuntimeReferences(root);
        }

        static void WireFx(F3DFXController fx, Transform root)
        {
            fx.DefaultFXType = F3DFXType.Vulcan;
            fx.VulcanFireRate = 0.11f;
            fx.vulcanProjectile = LoadFxTransform("e496166a742ada640b89650ebebbd052");
            fx.vulcanMuzzle = LoadFxTransform("ca6dec307275d294ba519247d04b6733");
            fx.vulcanImpact = LoadFxTransform("bfa7781c328a236468eda51a8ae2be55");
            fx.soloGunProjectile = LoadFxTransform("440fe57f42038354092415c07a4d55a9");
            fx.soloGunMuzzle = LoadFxTransform("b16d97c287e513344af2399ff138cd27");
            fx.soloGunImpact = LoadFxTransform("fd953838f1872d042a334b466e298967");
            fx.sniperBeam = LoadFxTransform("12e7c1b739173c946b6fa3d40e9f62d1");
            fx.sniperMuzzle = LoadFxTransform("43a8e47b31adb9a47b81c4653b412e5c");
            fx.sniperImpact = LoadFxTransform("a980d2b7788c0ef489f3c79f44da79e1");
            fx.shotGunProjectile = LoadFxTransform("0072b525437a5ed4891ebd255ec94fe6");
            fx.shotGunMuzzle = LoadFxTransform("ca6dec307275d294ba519247d04b6733");
            fx.shotGunImpact = LoadFxTransform("5f10243d9632db04bb987838fb3b36b7");
            fx.seekerProjectile = LoadFxTransform("49ca46ce7082ccf4c91329cd9b7c4b61");
            fx.seekerMuzzle = LoadFxTransform("43a8e47b31adb9a47b81c4653b412e5c");
            fx.seekerImpact = LoadFxTransform("762a17aff08aa1f40ac72b27c15fe2d1");
            fx.railgunBeam = LoadFxTransform("ec0fb8bef2fb8a5459c55c3700521b70");
            fx.railgunMuzzle = LoadFxTransform("b16d97c287e513344af2399ff138cd27");
            fx.railgunImpact = LoadFxTransform("2e92e894e0311c14c87f9544b2c88693");
            fx.plasmagunProjectile = LoadFxTransform("e7b84dec6cdbfb646a33fd06b85cf64e");
            fx.plasmagunMuzzle = LoadFxTransform("e4aff98840a58c94ea1cec28789d55d6");
            fx.plasmagunImpact = LoadFxTransform("ec543be1efaafca46aeac6f66e7a5ff0");
            fx.plasmaBeam = LoadFxTransform("66b50793fa1c780409218a1a1db5b1d8");
            fx.plasmaBeamHeavy = LoadFxTransform("a5251bb5b54465f43aebe1ddc2724566");
            fx.lightningGunBeam = LoadFxTransform("497cce4f043498049994c48e6e47f0ae");
            fx.flameRed = LoadFxTransform("bd9162be33a5ce54aa35d8e984e6d39d");
            fx.laserImpulseProjectile = LoadFxTransform("64703ac190f718b4abd5d10cb23c96ce");
            fx.laserImpulseMuzzle = LoadFxTransform("45116fb1512dc704b99d70267628129a");
            fx.laserImpulseImpact = LoadFxTransform("ce59e020d922ccb43866dee1ba26a673");
            fx.ShellParticles = System.Array.Empty<ParticleSystem>();

            Transform socket = FindNamed(root, "SOCKET_WEAPON", "MOUNT");
            fx.TurretSocket = socket != null ? new[] { socket } : new[] { root };
        }

        static Transform LoadFxTransform(string guid)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
                return null;
            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return go != null ? go.transform : null;
        }

        static Transform FindNamed(Transform root, string a, string b)
        {
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            Transform fallback = null;
            for (int i = 0; i < all.Length; i++)
            {
                string n = all[i].name;
                if (n.IndexOf(a, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return all[i];
                if (fallback == null && n.IndexOf(b, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    fallback = all[i];
            }

            return fallback;
        }

        static void RecenterVisualToRoot(GameObject root)
        {
            if (!TryGetRendererBounds(root, out Bounds bounds))
                return;

            Vector3 pivotWorld = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            ShiftDirectChildren(root.transform, -pivotWorld);
        }

        static void ShiftDirectChildren(Transform root, Vector3 worldDelta)
        {
            for (int i = 0; i < root.childCount; i++)
                root.GetChild(i).position += worldDelta;
        }

        static bool TryGetRendererBounds(GameObject root, out Bounds bounds)
        {
            bounds = default;
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            bool any = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null || renderers[i] is ParticleSystemRenderer)
                    continue;
                if (!any)
                {
                    bounds = renderers[i].bounds;
                    any = true;
                }
                else
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
            }

            return any;
        }

        static void FitRootBoxCollider(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            Bounds local = default;
            bool any = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null || renderers[i] is ParticleSystemRenderer)
                    continue;
                Bounds world = renderers[i].bounds;
                Vector3[] corners =
                {
                    world.min,
                    new Vector3(world.min.x, world.min.y, world.max.z),
                    new Vector3(world.min.x, world.max.y, world.min.z),
                    new Vector3(world.max.x, world.min.y, world.min.z),
                    world.max,
                    new Vector3(world.max.x, world.max.y, world.min.z),
                    new Vector3(world.max.x, world.min.y, world.max.z),
                    new Vector3(world.min.x, world.max.y, world.max.z),
                };
                for (int c = 0; c < corners.Length; c++)
                {
                    Vector3 p = root.transform.InverseTransformPoint(corners[c]);
                    if (!any)
                    {
                        local = new Bounds(p, Vector3.zero);
                        any = true;
                    }
                    else
                    {
                        local.Encapsulate(p);
                    }
                }
            }

            if (!any)
                return;

            BoxCollider box = root.GetComponent<BoxCollider>();
            if (box == null)
                box = root.AddComponent<BoxCollider>();
            box.center = local.center;
            box.size = local.size;
        }

        static string SanitizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "Turret";
            char[] chars = name.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (!char.IsLetterOrDigit(chars[i]) && chars[i] != '_')
                    chars[i] = '_';
            }

            return new string(chars).Trim('_');
        }

        static void EnsureFolderForAsset(string assetPath)
        {
            string dir = System.IO.Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(dir) || AssetDatabase.IsValidFolder(dir))
                return;

            string[] parts = dir.Split('/');
            string build = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = build + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(build, parts[i]);
                build = next;
            }
        }
    }
}
