using System;
using System.Collections.Generic;
using System.Text;
using Project.Combat;
using Project.Data;
using Project.Interaction;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Blueprint + Crafting tab: create a full ammo type
    /// (ItemData + DMAmmoFxProfile + prefabs under Prefabs/Items/Ammo).
    /// </summary>
    public sealed class DMAmmoCreatorPanel
    {
        private enum PresetKind
        {
            Blank = 0,
            Standard = 1,
            Plasma = 2,
            Laser = 3
        }

        private string ammoName = "New Ammo";
        private AmmoType ammoType = AmmoType.Gunpowder;
        private bool registerNewType = true;
        private string newTypeName = string.Empty;
        private StatusEffectType newTypeStatus = StatusEffectType.None;
        private bool addNewTypeToWeapons = true;
        private PresetKind preset = PresetKind.Standard;
        private DMAmmoFxProfile working;
        private bool createFxProfile = true;
        private bool createProjectilePrefab = true;
        private GameObject projectileVisualModel;
        private GameObject existingProjectilePrefab;
        private bool createPickupPrefab = true;
        private GameObject pickupVisualModel;
        private int pickupLayer = 7;
        private bool pickupAutoFitCollider = true;
        private bool pickupCanRespawn = true;
        private string pickupPromptText = "Press E to pick up";
        private bool addToRegistry = true;
        private bool showProfileDetails = true;
        private Vector2 scroll;
        private Vector2 existingScroll;
        private string status = string.Empty;
        private ItemData[] existingAmmo = Array.Empty<ItemData>();
        private const HideFlags WorkingHideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;

        public void Draw()
        {
            EnsureWorking();
            RefreshExistingAmmo();

            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("Create Ammo Type", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Register a new AmmoType (Acid, CryoCells, …) or reuse an existing one. " +
                "Create writes one DMAmmoFxProfile (the inventory item) plus prefabs in Prefabs/Items/Ammo.",
                MessageType.Info);

            DrawExistingAmmo();

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("New Ammo Type", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            preset = (PresetKind)EditorGUILayout.EnumPopup(
                new GUIContent("Start From Preset", "Copies stats / FX from an existing profile, then you change the type."),
                preset);
            if (EditorGUI.EndChangeCheck())
            {
                ApplyPreset(preset);
                ApplyAmmoType(ammoType);
            }

            registerNewType = EditorGUILayout.Toggle(
                new GUIContent("Create New AmmoType", "Adds a new value to the AmmoType enum, then creates the assets."),
                registerNewType);
            if (registerNewType)
            {
                newTypeName = EditorGUILayout.TextField(
                    new GUIContent("New Type Id", "C# enum name. Acid, CryoCells, Slug. Spaces become PascalCase."),
                    newTypeName);
                newTypeStatus = (StatusEffectType)EditorGUILayout.EnumPopup("Default Status", newTypeStatus);
                addNewTypeToWeapons = EditorGUILayout.Toggle(
                    new GUIContent("Add To Multi-Ammo Weapons", "Append this type to ranged weapons that already list several ammo types."),
                    addNewTypeToWeapons);
                if (IsDefaultAmmoName(ammoName) && !string.IsNullOrWhiteSpace(newTypeName)
                    && DMAmmoTypeEnumUtility.TryMakeIdentifier(newTypeName, out string previewId, out _))
                    ammoName = previewId;
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                ammoType = (AmmoType)EditorGUILayout.EnumPopup(
                    new GUIContent("Existing Ammo Type", "Weapon compatibility key (Gunpowder, Plasma, Ice, …)."),
                    ammoType);
                if (EditorGUI.EndChangeCheck())
                    ApplyAmmoType(ammoType);
            }

            ammoName = EditorGUILayout.TextField("Ammo Name", ammoName);
            addToRegistry = EditorGUILayout.Toggle("Add To Item Registry", addToRegistry);
            createFxProfile = EditorGUILayout.Toggle("Create DMAmmoFxProfile", createFxProfile);

            EditorGUILayout.Space(8f);
            if (GUILayout.Button(
                    registerNewType
                        ? "Create New AmmoType + Profile + Prefabs"
                        : "Create Ammo + Profile + Prefabs",
                    GUILayout.Height(42f)))
                Create();

            EditorGUILayout.Space(6f);
            if (GUILayout.Button("Create Missing Ammo Types (Ice, Fire, Electric, …)", GUILayout.Height(28f)))
                CreateMissingTypes();

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Combat Prefabs", EditorStyles.boldLabel);
            if (!working.isHitscanBeam)
            {
                createProjectilePrefab = EditorGUILayout.Toggle("Create Projectile Prefab", createProjectilePrefab);
                if (createProjectilePrefab)
                {
                    projectileVisualModel = (GameObject)EditorGUILayout.ObjectField(
                        "Visual Model", projectileVisualModel, typeof(GameObject), false);
                }
                else
                {
                    existingProjectilePrefab = (GameObject)EditorGUILayout.ObjectField(
                        "Existing Projectile", existingProjectilePrefab, typeof(GameObject), false);
                }
            }
            else
            {
                createProjectilePrefab = false;
                EditorGUILayout.HelpBox("Hitscan / laser ammo does not create a traveling projectile.", MessageType.None);
            }

            createPickupPrefab = EditorGUILayout.Toggle("Create World Pickup Prefab", createPickupPrefab);
            if (createPickupPrefab)
            {
                pickupVisualModel = (GameObject)EditorGUILayout.ObjectField(
                    "Pickup Visual", pickupVisualModel, typeof(GameObject), false);
                pickupLayer = EditorGUILayout.LayerField("Pickup Layer", pickupLayer);
                pickupAutoFitCollider = EditorGUILayout.Toggle("Auto-Fit Collider", pickupAutoFitCollider);
                pickupCanRespawn = EditorGUILayout.Toggle("Can Respawn", pickupCanRespawn);
                pickupPromptText = EditorGUILayout.TextField("Prompt", pickupPromptText);
            }

            EditorGUILayout.Space(10f);
            showProfileDetails = EditorGUILayout.Foldout(showProfileDetails, "Tune Profile (stats, FX, per-ammo hit marks)", true);
            if (showProfileDetails)
            {
                SerializedObject so = new SerializedObject(working);
                so.Update();
                DrawProfileFields(so);
                so.ApplyModifiedProperties();
                ammoType = working.ammoType;
            }

            if (!string.IsNullOrEmpty(status))
            {
                EditorGUILayout.Space(8f);
                EditorGUILayout.HelpBox(status, MessageType.Info);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawExistingAmmo()
        {
            EditorGUILayout.LabelField($"Existing ammo ({existingAmmo.Length})", EditorStyles.boldLabel);
            existingScroll = EditorGUILayout.BeginScrollView(existingScroll, GUILayout.Height(120f));
            for (int i = 0; i < existingAmmo.Length; i++)
            {
                ItemData item = existingAmmo[i];
                if (item == null)
                    continue;

                string profileName = item.fxProfile != null ? item.fxProfile.name : "no profile";
                if (GUILayout.Button($"{item.itemName}  —  {item.ammoType}  —  {profileName}", EditorStyles.miniButton))
                {
                    Selection.activeObject = item;
                    EditorGUIUtility.PingObject(item);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawProfileFields(SerializedObject so)
        {
            EditorGUILayout.PropertyField(so.FindProperty("itemName"));
            EditorGUILayout.PropertyField(so.FindProperty("ammoType"));
            EditorGUILayout.PropertyField(so.FindProperty("icon"));
            EditorGUILayout.PropertyField(so.FindProperty("worldPrefab"));
            EditorGUILayout.PropertyField(so.FindProperty("maxStack"));
            EditorGUILayout.PropertyField(so.FindProperty("ammoPerPickup"));
            EditorGUILayout.PropertyField(so.FindProperty("ammoPickupGrant"));
            EditorGUILayout.PropertyField(so.FindProperty("tooltipDescription"));
            EditorGUILayout.PropertyField(so.FindProperty("grantsXp"));
            EditorGUILayout.PropertyField(so.FindProperty("xpAmount"));
            EditorGUILayout.PropertyField(so.FindProperty("xpSource"));
            EditorGUILayout.PropertyField(so.FindProperty("grantXpEveryPickupOrUse"));
            EditorGUILayout.PropertyField(so.FindProperty("requiredLevelToPickup"));
            EditorGUILayout.PropertyField(so.FindProperty("requiredLevelToUse"));
            EditorGUILayout.PropertyField(so.FindProperty("requiredLevelToEquip"));
            EditorGUILayout.PropertyField(so.FindProperty("requiredLevelToCraft"));
            EditorGUILayout.PropertyField(so.FindProperty("isAcInfused"));
            EditorGUILayout.PropertyField(so.FindProperty("acValue"));

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Ranged Behavior", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(so.FindProperty("isHitscanBeam"));
            if (so.FindProperty("isHitscanBeam").boolValue)
                EditorGUILayout.PropertyField(so.FindProperty("isContinuousLaser"));
            EditorGUILayout.PropertyField(so.FindProperty("rangedDamage"));
            EditorGUILayout.PropertyField(so.FindProperty("rangedDamageRandomRange"));
            EditorGUILayout.PropertyField(so.FindProperty("rangedRange"));
            EditorGUILayout.PropertyField(so.FindProperty("projectileSpeed"));
            EditorGUILayout.PropertyField(so.FindProperty("projectileSpreadDegrees"));
            EditorGUILayout.PropertyField(so.FindProperty("weaponAccuracy"));
            EditorGUILayout.PropertyField(so.FindProperty("closeRangeFullAccuracyDistance"));
            EditorGUILayout.PropertyField(so.FindProperty("closeRangeSpreadScale"));
            EditorGUILayout.PropertyField(so.FindProperty("projectileGravityScale"));
            EditorGUILayout.PropertyField(so.FindProperty("splashRadius"));
            EditorGUILayout.PropertyField(so.FindProperty("splashDamageFalloff"));
            EditorGUILayout.PropertyField(so.FindProperty("recoilVertical"));
            EditorGUILayout.PropertyField(so.FindProperty("recoilHorizontal"));
            EditorGUILayout.PropertyField(so.FindProperty("recoilFireRateScale"));
            EditorGUILayout.PropertyField(so.FindProperty("ammoRecoilProfile"), true);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Muzzle / Tracer / Projectile", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(so.FindProperty("projectilePrefab"));
            EditorGUILayout.PropertyField(so.FindProperty("muzzleFlashPrefab"));
            EditorGUILayout.PropertyField(so.FindProperty("tracerPrefab"));
            EditorGUILayout.PropertyField(so.FindProperty("beamVfxPrefab"));

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Audio", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(so.FindProperty("fireSound"));
            EditorGUILayout.PropertyField(so.FindProperty("projectileTravelSound"));
            EditorGUILayout.PropertyField(so.FindProperty("continuousLoopSound"));
            EditorGUILayout.PropertyField(so.FindProperty("continuousStartSound"));
            EditorGUILayout.PropertyField(so.FindProperty("continuousStopSound"));

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Elemental", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(so.FindProperty("statusEffectOverride"));
            EditorGUILayout.PropertyField(so.FindProperty("statusEffectDamagePerTick"));
            EditorGUILayout.PropertyField(so.FindProperty("statusEffectTickInterval"));
            EditorGUILayout.PropertyField(so.FindProperty("statusEffectDuration"));
            EditorGUILayout.PropertyField(so.FindProperty("statusEffectVfxPrefab"));

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Hit VFX + Hit Marks (this ammo)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(so.FindProperty("defaultImpactVfxPrefab"));
            EditorGUILayout.PropertyField(so.FindProperty("spawnLaserBurn"));
            EditorGUILayout.PropertyField(so.FindProperty("useHitMarks"));
            EditorGUILayout.PropertyField(so.FindProperty("defaultDecals"), true);
            EditorGUILayout.PropertyField(so.FindProperty("defaultHitEffects"), true);
            EditorGUILayout.PropertyField(so.FindProperty("surfaces"), true);
            EditorGUILayout.PropertyField(so.FindProperty("fallBackToCatalog"));
            EditorGUILayout.PropertyField(so.FindProperty("catalog"));
        }

        private void EnsureWorking()
        {
            if (working != null)
            {
                working.hideFlags = WorkingHideFlags;
                return;
            }

            working = CreateWorkingProfile();
            ApplyPreset(preset);
            ApplyAmmoType(ammoType);
        }

        private static DMAmmoFxProfile CreateWorkingProfile()
        {
            DMAmmoFxProfile profile = ScriptableObject.CreateInstance<DMAmmoFxProfile>();
            profile.hideFlags = WorkingHideFlags;
            return profile;
        }

        private void ApplyPreset(PresetKind kind)
        {
            if (working == null)
                return;

            string path = kind switch
            {
                PresetKind.Standard => ProjectAssetPaths.ItemsAmmo + "/DMAmmoFxProfile_Standard.asset",
                PresetKind.Plasma => ProjectAssetPaths.ItemsAmmo + "/DMAmmoFxProfile_Plasma.asset",
                PresetKind.Laser => ProjectAssetPaths.ItemsAmmo + "/DMAmmoFxProfile_Laser.asset",
                _ => null
            };

            if (string.IsNullOrEmpty(path))
            {
                UnityEngine.Object.DestroyImmediate(working);
                working = CreateWorkingProfile();
                working.itemName = ammoName;
                working.catalog = AssetDatabase.LoadAssetAtPath<DMAmmoFxCatalog>(
                    ProjectAssetPaths.ItemsAmmo + "/DMAmmoFxCatalog.asset");
                working.useHitMarks = true;
                working.fallBackToCatalog = true;
                working.SeedSurfacesFromCatalog();
                return;
            }

            DMAmmoFxProfile template = AssetDatabase.LoadAssetAtPath<DMAmmoFxProfile>(path);
            if (template != null)
                working.CopyFrom(template);
            if (!string.IsNullOrWhiteSpace(ammoName))
                working.itemName = ammoName;
        }

        private void ApplyAmmoType(AmmoType type)
        {
            if (working == null)
                return;

            working.ammoType = type;
            working.statusEffectOverride = type.DefaultStatusEffectFor();
            if (type.DefaultStatusEffectFor() != StatusEffectType.None && working.statusEffectDuration <= 0f)
                working.statusEffectDuration = 3f;

            if (type == AmmoType.Laser)
            {
                working.isHitscanBeam = true;
                working.spawnLaserBurn = true;
                working.useHitMarks = false;
            }
            else if (type == AmmoType.Explosive && working.splashRadius <= 0f)
            {
                working.splashRadius = 2.5f;
            }

            if (IsDefaultAmmoName(ammoName))
                ammoName = DefaultNameFor(type);
        }

        private void Create()
        {
            if (!createFxProfile)
            {
                EditorUtility.DisplayDialog("Ammo Creator", "Enable DMAmmoFxProfile.", "OK");
                return;
            }

            AmmoType typeToCreate = ammoType;
            string typeNote = string.Empty;
            if (registerNewType)
            {
                if (!DMAmmoTypeEnumUtility.TryAddType(
                        newTypeName,
                        newTypeStatus,
                        addNewTypeToWeapons,
                        out typeToCreate,
                        out string typeError))
                {
                    EditorUtility.DisplayDialog("Ammo Creator", typeError, "OK");
                    return;
                }

                ammoType = typeToCreate;
                working.ammoType = typeToCreate;
                working.statusEffectOverride = newTypeStatus;
                if (newTypeStatus != StatusEffectType.None && working.statusEffectDuration <= 0f)
                    working.statusEffectDuration = 3f;
                typeNote = $"\nRegistered AmmoType.{typeToCreate} = {(int)typeToCreate}. Ctrl+R so the new type appears in dropdowns.";
            }

            CreatedAmmo created = CreateAmmoType(ammoName, typeToCreate, working, confirmName: true);
            if (created == null)
                return;

            status = created.Describe() + typeNote;
            existingAmmo = Array.Empty<ItemData>();
            RefreshExistingAmmo();
            if (created.item != null)
            {
                Selection.activeObject = created.item;
                EditorGUIUtility.PingObject(created.item);
            }
            else if (created.profile != null)
            {
                Selection.activeObject = created.profile;
                EditorGUIUtility.PingObject(created.profile);
            }
        }

        private void CreateMissingTypes()
        {
            RefreshExistingAmmo();
            HashSet<AmmoType> have = new HashSet<AmmoType>();
            for (int i = 0; i < existingAmmo.Length; i++)
            {
                if (existingAmmo[i] != null)
                    have.Add(existingAmmo[i].ammoType);
            }

            List<string> createdNames = new List<string>();
            foreach (AmmoType type in (AmmoType[])Enum.GetValues(typeof(AmmoType)))
            {
                if (have.Contains(type))
                    continue;

                PresetKind kind = PresetFor(type);
                ApplyPreset(kind);
                ApplyAmmoType(type);
                string name = DefaultNameFor(type);
                ammoName = name;
                ammoType = type;
                CreatedAmmo created = CreateAmmoType(name, type, working, confirmName: false);
                if (created != null)
                    createdNames.Add(created.DescribeOneLine());
            }

            existingAmmo = Array.Empty<ItemData>();
            RefreshExistingAmmo();
            status = createdNames.Count == 0
                ? "Every AmmoType already has an ItemData."
                : "Created missing ammo types:\n" + string.Join("\n", createdNames);
            EditorUtility.DisplayDialog("Ammo Creator", status, "OK");
        }

        private CreatedAmmo CreateAmmoType(string name, AmmoType type, DMAmmoFxProfile source, bool confirmName)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                if (confirmName)
                    EditorUtility.DisplayDialog("Ammo Creator", "Ammo Name is required.", "OK");
                return null;
            }

            if (source == null)
                return null;

            source.ammoType = type;
            source.itemName = name;

            string safeName = CraftingEditorUtility.SanitizeAssetName(name);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.ItemsAmmo);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsItemsAmmo);

            GameObject projectile = null;
            if (!source.isHitscanBeam && createProjectilePrefab)
            {
                projectile = BuildProjectilePrefab(name, safeName);
            }
            else if (!source.isHitscanBeam && !createProjectilePrefab)
            {
                projectile = existingProjectilePrefab;
                if (projectile == null && createFxProfile)
                {
                    if (confirmName)
                    {
                        EditorUtility.DisplayDialog(
                            "Ammo Creator",
                            "Create a projectile prefab or assign an existing one.",
                            "OK");
                    }
                    return null;
                }
            }

            CreatedAmmo result = new CreatedAmmo { name = name };

            if (createFxProfile)
            {
                DMAmmoFxProfile profile = UnityEngine.Object.Instantiate(source);
                profile.hideFlags = HideFlags.None;
                profile.itemName = name;
                profile.ammoType = type;
                if (projectile != null)
                    profile.projectilePrefab = projectile;
                string profilePath = AssetDatabase.GenerateUniqueAssetPath(
                    $"{ProjectAssetPaths.ItemsAmmo}/DMAmmoFxProfile_{safeName}.asset");
                AssetDatabase.CreateAsset(profile, profilePath);
                result.profile = profile;
                result.profilePath = profilePath;
            }

            if (result.profile != null)
            {
                result.profile.itemType = ItemType.Ammo;
                result.profile.itemName = name;
                result.profile.ammoType = type;
                result.profile.fxProfile = result.profile;
                if (projectile != null)
                    result.profile.projectilePrefab = projectile;

                if (createPickupPrefab)
                {
                    GameObject pickup = BuildPickupPrefab(result.profile, safeName);
                    result.profile.worldPrefab = pickup;
                    result.pickup = pickup;
                }

                if (addToRegistry)
                    CraftingEditorUtility.AddItemToRegistry(result.profile);

                EditorUtility.SetDirty(result.profile);
                result.item = result.profile;
                result.itemPath = result.profilePath;
            }

            result.projectile = projectile;
            AssetDatabase.SaveAssets();
            return result;
        }

        private void RefreshExistingAmmo()
        {
            if (existingAmmo != null && existingAmmo.Length > 0)
                return;

            List<ItemData> list = new List<ItemData>();
            ItemData[] all = CraftingEditorUtility.LoadAllItems();
            for (int i = 0; i < all.Length; i++)
            {
                ItemData item = all[i];
                if (item != null && item.itemType == ItemType.Ammo)
                    list.Add(item);
            }

            list.Sort((a, b) => string.CompareOrdinal(a.itemName, b.itemName));
            existingAmmo = list.ToArray();
        }

        private GameObject BuildProjectilePrefab(string displayName, string safeName)
        {
            GameObject root = new GameObject(displayName + "_Projectile");
            root.AddComponent<CombatProjectile>();

            if (projectileVisualModel != null)
            {
                GameObject visual = UnityEngine.Object.Instantiate(projectileVisualModel, root.transform);
                visual.name = "Visual";
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
            }
            else
            {
                GameObject placeholder = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                placeholder.name = "Visual_Placeholder";
                placeholder.transform.SetParent(root.transform, false);
                placeholder.transform.localScale = Vector3.one * 0.12f;
                UnityEngine.Object.DestroyImmediate(placeholder.GetComponent<Collider>());
            }

            string prefabPath = AssetDatabase.GenerateUniqueAssetPath(
                $"{ProjectAssetPaths.PrefabsItemsAmmo}/{safeName}_Projectile.prefab");
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return saved;
        }

        private GameObject BuildPickupPrefab(ItemData ammoItem, string safeName)
        {
            GameObject source = pickupVisualModel != null
                ? pickupVisualModel
                : AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/_Project/Models/Ammo Crate/Meshy_AI_Futuristic_Cyberpunk__0806000643_texture.fbx");
            GameObject instance;
            if (source != null)
            {
                instance = PrefabUtility.InstantiatePrefab(source) as GameObject;
                if (instance == null)
                    instance = UnityEngine.Object.Instantiate(source);
            }
            else
            {
                instance = GameObject.CreatePrimitive(PrimitiveType.Cube);
                instance.transform.localScale = Vector3.one * 0.25f;
                UnityEngine.Object.DestroyImmediate(instance.GetComponent<Collider>());
            }

            Material crateMat = ResolveCrateMaterial(ammoItem);
            if (crateMat != null)
            {
                MeshRenderer[] renderers = instance.GetComponentsInChildren<MeshRenderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                    renderers[i].sharedMaterial = crateMat;
            }

            instance.name = safeName + "_Pickup";
            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            instance.layer = pickupLayer;

            Collider collider = instance.GetComponentInChildren<Collider>();
            if (collider == null)
                collider = instance.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            if (pickupAutoFitCollider && collider is BoxCollider box)
                FitBoxCollider(instance, box);

            ItemPickup pickup = instance.GetComponent<ItemPickup>();
            if (pickup == null)
                pickup = instance.AddComponent<ItemPickup>();
            pickup.itemData = ammoItem;
            pickup.amount = Mathf.Max(1, ammoItem.ammoPerPickup);
            pickup.promptText = pickupPromptText;
            pickup.canRespawn = pickupCanRespawn;

            string prefabPath = AssetDatabase.GenerateUniqueAssetPath(
                $"{ProjectAssetPaths.PrefabsItemsAmmo}/{safeName}_Pickup.prefab");
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            UnityEngine.Object.DestroyImmediate(instance);
            return saved;
        }

        private static Material ResolveCrateMaterial(ItemData ammoItem)
        {
            const string folder = "Assets/_Project/Models/Ammo Crate/Materials";
            string name = ammoItem != null ? ammoItem.ammoType.ToString() : string.Empty;
            string path = name switch
            {
                "Gunpowder" => folder + "/Ammo.mat",
                "Laser" => folder + "/Ammo 1.mat",
                "Plasma" => folder + "/Ammo 2.mat",
                "Ice" => folder + "/Ammo Ice.mat",
                "Fire" => folder + "/Ammo Fire.mat",
                "Electricity" => folder + "/Ammo Electricity.mat",
                "Explosive" => folder + "/Ammo Explosive.mat",
                "Ion" => folder + "/Ammo Ion.mat",
                "ResonanceStabilizer" => folder + "/Ammo Resonance.mat",
                _ => folder + "/Ammo.mat"
            };
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            return mat != null ? mat : AssetDatabase.LoadAssetAtPath<Material>(folder + "/Ammo.mat");
        }

        private static void FitBoxCollider(GameObject root, BoxCollider box)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            Vector3 lossy = root.transform.lossyScale;
            box.center = root.transform.InverseTransformPoint(bounds.center);
            box.size = new Vector3(
                lossy.x != 0f ? bounds.size.x / lossy.x : bounds.size.x,
                lossy.y != 0f ? bounds.size.y / lossy.y : bounds.size.y,
                lossy.z != 0f ? bounds.size.z / lossy.z : bounds.size.z);
        }

        private static bool IsDefaultAmmoName(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name == "New Ammo")
                return true;
            foreach (AmmoType type in (AmmoType[])Enum.GetValues(typeof(AmmoType)))
            {
                if (string.Equals(name, DefaultNameFor(type), StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static string DefaultNameFor(AmmoType type)
        {
            return type switch
            {
                AmmoType.Gunpowder => "Standard",
                AmmoType.ResonanceStabilizer => "Resonance Stabilizer",
                _ => type.ToString()
            };
        }

        private static PresetKind PresetFor(AmmoType type)
        {
            return type switch
            {
                AmmoType.Plasma => PresetKind.Plasma,
                AmmoType.Laser => PresetKind.Laser,
                AmmoType.Ion => PresetKind.Plasma,
                _ => PresetKind.Standard
            };
        }

        private sealed class CreatedAmmo
        {
            public string name;
            public ItemData item;
            public string itemPath;
            public DMAmmoFxProfile profile;
            public string profilePath;
            public GameObject projectile;
            public GameObject pickup;

            public string Describe()
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("Created ammo type '").Append(name).Append("'.");
                if (!string.IsNullOrEmpty(itemPath))
                    sb.Append("\nItemData: ").Append(itemPath);
                if (!string.IsNullOrEmpty(profilePath))
                    sb.Append("\nProfile: ").Append(profilePath);
                if (projectile != null)
                    sb.Append("\nProjectile: ").Append(AssetDatabase.GetAssetPath(projectile));
                if (pickup != null)
                    sb.Append("\nPickup: ").Append(AssetDatabase.GetAssetPath(pickup));
                return sb.ToString();
            }

            public string DescribeOneLine()
            {
                return $"{name} → {itemPath}";
            }
        }
    }
}
