#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Invector.vCharacterController;
using Invector.vShooter;
using Invector.vMelee;
using Project.Combat;
using Project.Data;
using Project.EditorTools;
using Project.Player.Invector;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ECM2;

namespace Project.EditorTools.Invector
{
    public static class PioneerInvectorPlayerSetupUtility
    {
        private const string InvectorPlayerPrefabPath =
            "Assets/Invector-3rdPersonController/Shooter/Prefabs/Player/vShooterMelee_NoInventory.prefab";

        private const string OutputPlayerPrefabPath = "Assets/_Project/Prefabs/Players/Player_Invector.prefab";

        private const string DefaultPistolPath =
            "Assets/Invector-3rdPersonController/Shooter/Prefabs/Weapon/_weapons_WITHOUT_Inventory/_weaponsPrefabs/vHandgun_NO_Inventory Variant.prefab";

        private const string DefaultRiflePath =
            "Assets/Invector-3rdPersonController/Shooter/Prefabs/Weapon/_weapons_WITHOUT_Inventory/_weaponsPrefabs/vAssaultRifle_NO_Inventory_v2 Variant.prefab";

        private const string DefaultMeleePath =
            "Assets/Invector-3rdPersonController/Melee Combat/Prefabs/Weapons/NoInventory/GreatSword_NOInventory.prefab";

        private const string DefaultTwoHandMeleePath =
            "Assets/Invector-3rdPersonController/Melee Combat/Prefabs/Weapons/NoInventory/GreatKatana_NOInventory.prefab";

        private const string PistolItemPath = ProjectAssetPaths.ItemsRanged + "/sci_fi_pistol.asset";
        private const string RifleItemPath = ProjectAssetPaths.ItemsRanged + "/survival_rifle.asset";
        private const string MiningToolItemPath = ProjectAssetPaths.ItemsRanged + "/DM_Mining_Tool.asset";
        private const string PreloadedMeleeSlotsRootName = "PreloadedMeleeWeaponSlots";
        private const string PreloadedRangedSlotsRootName = "PreloadedRangedWeaponSlots";

        [MenuItem(SurvivalPioneerEditorMenus.Combat + "Build Player_Invector Prefab", false, 120)]
        public static void BuildPlayerInvectorPrefab()
        {
            string sourcePath = System.IO.File.Exists(OutputPlayerPrefabPath)
                ? OutputPlayerPrefabPath
                : InvectorPlayerPrefabPath;

            GameObject invectorRoot = PrefabUtility.LoadPrefabContents(sourcePath);
            GameObject pioneerSourceRoot = null;
            bool loadedPioneerSource = false;

            if (sourcePath == InvectorPlayerPrefabPath && File.Exists(OutputPlayerPrefabPath))
            {
                pioneerSourceRoot = PrefabUtility.LoadPrefabContents(OutputPlayerPrefabPath);
                loadedPioneerSource = true;
            }

            try
            {
                invectorRoot.name = "Player_Invector";
                invectorRoot.tag = "Player";

                ReplaceShooterInput(invectorRoot);
                if (pioneerSourceRoot != null)
                    CopyPioneerComponents(pioneerSourceRoot, invectorRoot);
                DisableLegacyMotorComponents(invectorRoot);
                DisableInvectorStandaloneUi(invectorRoot);
                StripLegacyPlayerComponents(invectorRoot);
                ResetInvectorHealth(invectorRoot);
                EnsureBridgeComponents(invectorRoot);
                ConfigureWeaponBridge(invectorRoot);
                ConfigurePreloadedMeleeWeaponSlots(invectorRoot);
                ConfigurePreloadedRangedWeaponSlots(invectorRoot);
                ConfigurePlayerInput(invectorRoot);

                EnsureDirectory(OutputPlayerPrefabPath);
                PrefabUtility.SaveAsPrefabAsset(invectorRoot, OutputPlayerPrefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"Created {OutputPlayerPrefabPath}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(invectorRoot);
                if (loadedPioneerSource && pioneerSourceRoot != null)
                    PrefabUtility.UnloadPrefabContents(pioneerSourceRoot);
            }
        }

        [MenuItem(SurvivalPioneerEditorMenus.Combat + "Wire ItemData Invector Weapon Prefabs", false, 121)]
        public static void WireDefaultItemWeaponPrefabs()
        {
            AssignItemWeaponPrefab(PistolItemPath, LoadPrefab(DefaultPistolPath));
            AssignItemWeaponPrefab(RifleItemPath, LoadPrefab(DefaultRiflePath));
            AssetDatabase.SaveAssets();
            Debug.Log("Wired sci_fi_pistol and survival_rifle Invector weapon prefabs.");
        }

        [MenuItem(SurvivalPioneerEditorMenus.Combat + "Swap Pioneer Scene Player To Invector", false, 123)]
        public static void SwapScenePlayerToInvector()
        {
            BuildPlayerInvectorPrefab();
            WireDefaultItemWeaponPrefabs();
            EnemyInvectorSetupUtility.RepairAllHumanoidCombatPrefabs();

            GameObject outputPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(OutputPlayerPrefabPath);
            if (outputPrefab == null)
            {
                Debug.LogError("Player_Invector prefab missing.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Pioneer.unity", OpenSceneMode.Single);
            GameObject[] roots = scene.GetRootGameObjects();
            bool replaced = false;

            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].CompareTag("Player") || roots[i].name == "Player")
                {
                    Vector3 position = roots[i].transform.position;
                    Quaternion rotation = roots[i].transform.rotation;
                    UnityEngine.Object.DestroyImmediate(roots[i]);
                    GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(outputPrefab, scene);
                    instance.transform.SetPositionAndRotation(position, rotation);
                    replaced = true;
                    break;
                }
            }

            if (!replaced)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(outputPrefab, scene);
                instance.transform.position = Vector3.zero;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Pioneer.unity now uses Player_Invector.");
        }

        [MenuItem(SurvivalPioneerEditorMenus.Combat + "Refresh Player_Invector Melee Slots", false, 124)]
        public static void RefreshPlayerInvectorMeleeSlots()
        {
            GameObject invectorRoot = PrefabUtility.LoadPrefabContents(OutputPlayerPrefabPath);
            try
            {
                ConfigureWeaponBridge(invectorRoot);
                ConfigurePreloadedMeleeWeaponSlots(invectorRoot);
                PrefabUtility.SaveAsPrefabAsset(invectorRoot, OutputPlayerPrefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log("Refreshed Player_Invector preloaded melee weapon slots.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(invectorRoot);
            }
        }

        [MenuItem(SurvivalPioneerEditorMenus.Combat + "Refresh Player_Invector Ranged Slots", false, 125)]
        public static void RefreshPlayerInvectorRangedSlots()
        {
            GameObject invectorRoot = PrefabUtility.LoadPrefabContents(OutputPlayerPrefabPath);
            try
            {
                ConfigureWeaponBridge(invectorRoot);
                ConfigurePreloadedRangedWeaponSlots(invectorRoot);
                PrefabUtility.SaveAsPrefabAsset(invectorRoot, OutputPlayerPrefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log("Refreshed Player_Invector preloaded ranged weapon slots.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(invectorRoot);
            }
        }

        /// <summary>
        /// Configures default weapon bridge references and all preloaded melee/ranged slots on any Invector character root.
        /// </summary>
        public static void RefreshPreloadedWeaponSlotsOn(GameObject root)
        {
            if (root == null)
                return;

            ConfigureWeaponBridge(root);
            ConfigurePreloadedMeleeWeaponSlots(root);
            ConfigurePreloadedRangedWeaponSlots(root);
        }

        public static void ResetWeaponSlotTransformsFromItemData(GameObject root)
        {
            if (root == null)
                return;

            PioneerInvectorWeaponBridge bridge = root.GetComponent<PioneerInvectorWeaponBridge>();
            if (bridge == null)
                return;

            SerializedObject serialized = new SerializedObject(bridge);
            ResetWeaponSlotArrayTransforms(serialized.FindProperty("meleeWeaponSlots"));
            ResetWeaponSlotArrayTransforms(serialized.FindProperty("rangedWeaponSlots"));
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ResetWeaponSlotArrayTransforms(SerializedProperty slots)
        {
            if (slots == null || !slots.isArray)
                return;

            for (int i = 0; i < slots.arraySize; i++)
            {
                SerializedProperty slot = slots.GetArrayElementAtIndex(i);
                ItemData item = slot.FindPropertyRelative("item")?.objectReferenceValue as ItemData;
                if (item == null)
                    continue;

                GameObject drawn = slot.FindPropertyRelative("drawnInstance")?.objectReferenceValue as GameObject;
                if (drawn != null)
                    ApplyHeldTransform(drawn.transform, item);

                GameObject holstered = slot.FindPropertyRelative("holsteredInstance")?.objectReferenceValue as GameObject;
                if (holstered != null)
                    ApplySheathedTransform(holstered.transform, item);
            }
        }

        private static void ReplaceShooterInput(GameObject root)
        {
            vShooterMeleeInput legacyInput = root.GetComponent<vShooterMeleeInput>();
            if (legacyInput == null || legacyInput is PioneerShooterMeleeInput)
                return;

            PioneerShooterMeleeInput pioneerInput = root.AddComponent<PioneerShooterMeleeInput>();
            EditorUtility.CopySerialized(legacyInput, pioneerInput);
            UnityEngine.Object.DestroyImmediate(legacyInput, true);
        }

        private static void DisableLegacyMotorComponents(GameObject root)
        {
            RemoveIfPresent<CharacterMovement>(root);
            RemoveIfPresent<Character>(root);
        }

        private static void DisableInvectorStandaloneUi(GameObject root)
        {
            vCollectMeleeControl collectControl = root.GetComponent<vCollectMeleeControl>();
            if (collectControl != null)
            {
                SerializedObject serialized = new SerializedObject(collectControl);
                SerializedProperty displayPrefab = serialized.FindProperty("controlDisplayPrefab");
                if (displayPrefab != null)
                    displayPrefab.objectReferenceValue = null;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            vShooterManager shooterManager = root.GetComponent<vShooterManager>();
            if (shooterManager == null)
                return;

            SerializedObject shooterSerialized = new SerializedObject(shooterManager);
            SerializedProperty useAmmoDisplay = shooterSerialized.FindProperty("useAmmoDisplay");
            if (useAmmoDisplay != null)
                useAmmoDisplay.boolValue = false;

            LayerMask damageLayers = PioneerInvectorShooterLayers.ResolveShooterDamageLayers(shooterManager.damageLayer);
            SerializedProperty damageLayer = shooterSerialized.FindProperty("damageLayer");
            if (damageLayer != null)
                damageLayer.intValue = damageLayers.value;

            shooterSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void RemoveIfPresent<T>(GameObject root) where T : Component
        {
            T component = root.GetComponent<T>();
            if (component != null)
                UnityEngine.Object.DestroyImmediate(component, true);
        }

        private static void StripLegacyPlayerComponents(GameObject root)
        {
            RemoveMissingScriptsRecursive(root);
        }

        private static void RemoveMissingScriptsRecursive(GameObject root)
        {
            if (root == null)
                return;

            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] != null)
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(children[i].gameObject);
            }
        }

        private static void ResetInvectorHealth(GameObject root)
        {
            vThirdPersonController controller = root.GetComponent<vThirdPersonController>();
            if (controller == null)
                return;

            SerializedHealthFields(controller);
        }

        private static void SerializedHealthFields(vThirdPersonController controller)
        {
            var serialized = new UnityEditor.SerializedObject(controller);
            UnityEditor.SerializedProperty isDead = serialized.FindProperty("_isDead");
            UnityEditor.SerializedProperty currentHealth = serialized.FindProperty("_currentHealth");
            if (isDead != null)
                isDead.boolValue = false;
            if (currentHealth != null)
                currentHealth.floatValue = 0f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigurePlayerInput(GameObject root)
        {
            UnityEngine.InputSystem.PlayerInput playerInput = root.GetComponent<UnityEngine.InputSystem.PlayerInput>();
            if (playerInput == null)
                return;

            playerInput.notificationBehavior = UnityEngine.InputSystem.PlayerNotifications.InvokeCSharpEvents;
        }

        private static void CopyPioneerComponents(GameObject source, GameObject target)
        {
            Component[] components = source.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component == null)
                    continue;

                Type type = component.GetType();
                if (type == typeof(Transform) || type.Namespace != null && type.Namespace.StartsWith("ECM2", StringComparison.Ordinal))
                    continue;

                if (type == typeof(vShooterMeleeInput) || type == typeof(PioneerShooterMeleeInput))
                    continue;

                if (target.GetComponent(type) != null)
                    continue;

                Component clone = target.AddComponent(type);
                EditorUtility.CopySerialized(component, clone);
            }
        }

        private static void EnsureBridgeComponents(GameObject root)
        {
            if (root.GetComponent<PioneerInvectorBootstrap>() == null)
                root.AddComponent<PioneerInvectorBootstrap>();
            if (root.GetComponent<PioneerInvectorInputBridge>() == null)
                root.AddComponent<PioneerInvectorInputBridge>();
            if (root.GetComponent<PioneerInvectorWeaponBridge>() == null)
                root.AddComponent<PioneerInvectorWeaponBridge>();
            if (root.GetComponent<PioneerInvectorDamageBridge>() == null)
                root.AddComponent<PioneerInvectorDamageBridge>();
            if (root.GetComponent<PioneerInvectorSurvivalBridge>() == null)
                root.AddComponent<PioneerInvectorSurvivalBridge>();
            if (root.GetComponent<PioneerInvectorAmmoBridge>() == null)
                root.AddComponent<PioneerInvectorAmmoBridge>();
            if (root.GetComponent<PioneerPlayerInputBinder>() == null)
                root.AddComponent<PioneerPlayerInputBinder>();
            if (root.GetComponent<PioneerInvectorDeathRagdoll>() == null)
                root.AddComponent<PioneerInvectorDeathRagdoll>();
        }

        private static void ConfigureWeaponBridge(GameObject root)
        {
            PioneerInvectorWeaponBridge bridge = root.GetComponent<PioneerInvectorWeaponBridge>();
            if (bridge == null)
                return;

            SerializedObject serialized = new SerializedObject(bridge);
            serialized.FindProperty("defaultPistolPrefab").objectReferenceValue = LoadPrefab(DefaultPistolPath);
            serialized.FindProperty("defaultRiflePrefab").objectReferenceValue = LoadPrefab(DefaultRiflePath);
            serialized.FindProperty("defaultMeleeSwordPrefab").objectReferenceValue = LoadPrefab(DefaultMeleePath);
            serialized.FindProperty("defaultMeleeTwoHandPrefab").objectReferenceValue = LoadPrefab(DefaultTwoHandMeleePath);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigurePreloadedMeleeWeaponSlots(GameObject root)
        {
            if (root == null)
                return;

            PioneerInvectorWeaponBridge bridge = root.GetComponent<PioneerInvectorWeaponBridge>();
            if (bridge == null)
                return;

            Transform oldMeleeRoot = FindChildTransformByName(root.transform, PreloadedMeleeSlotsRootName);
            if (oldMeleeRoot != null)
                UnityEngine.Object.DestroyImmediate(oldMeleeRoot.gameObject, true);

            Transform oldGenericRoot = FindChildTransformByName(root.transform, "PreloadedWeaponSlots");
            if (oldGenericRoot != null)
                UnityEngine.Object.DestroyImmediate(oldGenericRoot.gameObject, true);

            Transform drawnParent = FindRightMeleeHandler(root);
            if (drawnParent == null)
                drawnParent = root.transform;

            SerializedObject serialized = new SerializedObject(bridge);
            SerializedProperty slots = serialized.FindProperty("meleeWeaponSlots");
            slots.ClearArray();

            List<ItemData> items = FindMeleeItems();
            HashSet<string> validSlotNames = new HashSet<string>(StringComparer.Ordinal);
            AddRangedSlotNames(validSlotNames);

            for (int i = 0; i < items.Count; i++)
            {
                ItemData item = items[i];
                GameObject drawnPrefab = ResolveMeleeWeaponPrefab(item);
                if (item == null || drawnPrefab == null)
                    continue;

                string safeName = MakeSafeObjectName(item.itemName, item.name);
                string drawnName = $"Drawn_{safeName}";
                string holsteredName = $"Holstered_{safeName}";
                validSlotNames.Add(drawnName);
                validSlotNames.Add(holsteredName);

                // Reuse existing slots so hand-authored prefab transforms are preserved.
                // ItemData held/sheathed values only seed brand-new slots as a starting point.
                Transform existingDrawn = FindChildTransformByName(root.transform, drawnName);
                GameObject drawn;
                if (existingDrawn != null)
                {
                    drawn = existingDrawn.gameObject;
                }
                else
                {
                    drawn = InstantiatePrefabChild(drawnPrefab, drawnParent, drawnName);
                    ApplyHeldTransform(drawn.transform, item);
                }

                PioneerInvectorWeaponBridge.PrepareDrawnMeleeSlot(drawn, item, drawnPrefab);
                drawn.SetActive(false);

                Transform existingHolstered = FindChildTransformByName(root.transform, holsteredName);
                GameObject holstered;
                if (existingHolstered != null)
                {
                    holstered = existingHolstered.gameObject;
                }
                else
                {
                    EnsureDefaultHolsterSocket(item);
                    Transform holsterParent = FindChildTransformByName(root.transform, PioneerInvectorWeaponBridge.ResolveHolsterSocketName(item));
                    if (holsterParent == null)
                        holsterParent = root.transform;

                    GameObject holsterPrefab = item.heldPrefab != null ? item.heldPrefab : item.worldPrefab;
                    if (holsterPrefab == null)
                        holsterPrefab = drawnPrefab;

                    holstered = InstantiatePrefabChild(holsterPrefab, holsterParent, holsteredName);
                    ApplySheathedTransform(holstered.transform, item);
                }

                PioneerInvectorWeaponBridge.PrepareHolsteredVisualSlot(holstered, item);
                holstered.SetActive(false);

                int index = slots.arraySize;
                slots.InsertArrayElementAtIndex(index);
                SerializedProperty slot = slots.GetArrayElementAtIndex(index);
                slot.FindPropertyRelative("slotId").stringValue = safeName;
                slot.FindPropertyRelative("item").objectReferenceValue = item;
                slot.FindPropertyRelative("drawnInstance").objectReferenceValue = drawn;
                slot.FindPropertyRelative("holsteredInstance").objectReferenceValue = holstered;
            }

            RemoveOrphanedWeaponSlots(root.transform, validSlotNames);

            serialized.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log($"Configured {slots.arraySize} preloaded Player_Invector melee slot pair(s) (existing slot transforms preserved).");
        }

        private static void ConfigurePreloadedRangedWeaponSlots(GameObject root)
        {
            if (root == null)
                return;

            PioneerInvectorWeaponBridge bridge = root.GetComponent<PioneerInvectorWeaponBridge>();
            if (bridge == null)
                return;

            Transform oldRangedRoot = FindChildTransformByName(root.transform, PreloadedRangedSlotsRootName);
            if (oldRangedRoot != null)
                UnityEngine.Object.DestroyImmediate(oldRangedRoot.gameObject, true);

            Transform drawnParent = FindRightRangedHandler(root);
            if (drawnParent == null)
                drawnParent = root.transform;

            SerializedObject serialized = new SerializedObject(bridge);
            SerializedProperty slots = serialized.FindProperty("rangedWeaponSlots");
            slots.ClearArray();

            List<ItemData> items = FindDefaultRangedItems();
            HashSet<string> validSlotNames = new HashSet<string>(StringComparer.Ordinal);
            AddMeleeSlotNames(validSlotNames);

            for (int i = 0; i < items.Count; i++)
            {
                ItemData item = items[i];
                GameObject drawnPrefab = ResolveRangedWeaponPrefab(item);
                if (item == null || drawnPrefab == null)
                    continue;

                string safeName = MakeSafeObjectName(item.itemName, item.name);
                string drawnName = $"Drawn_{safeName}";
                string holsteredName = $"Holstered_{safeName}";
                validSlotNames.Add(drawnName);
                validSlotNames.Add(holsteredName);

                Transform existingDrawn = FindChildTransformByName(root.transform, drawnName);
                GameObject drawn;
                if (existingDrawn != null)
                {
                    drawn = existingDrawn.gameObject;
                }
                else
                {
                    drawn = InstantiatePrefabChild(drawnPrefab, drawnParent, drawnName);
                    ApplyHeldTransform(drawn.transform, item);
                }

                PioneerInvectorWeaponBridge.PrepareDrawnRangedSlot(drawn, item, drawnPrefab);
                drawn.SetActive(false);

                Transform existingHolstered = FindChildTransformByName(root.transform, holsteredName);
                GameObject holstered;
                if (existingHolstered != null)
                {
                    holstered = existingHolstered.gameObject;
                }
                else
                {
                    Transform holsterParent = FindRangedHolsterParent(root, item);
                    if (holsterParent == null)
                        holsterParent = root.transform;

                    GameObject holsterPrefab = item.heldPrefab != null ? item.heldPrefab : item.worldPrefab;
                    if (holsterPrefab == null)
                        holsterPrefab = drawnPrefab;

                    holstered = InstantiatePrefabChild(holsterPrefab, holsterParent, holsteredName);
                    ApplySheathedTransform(holstered.transform, item);
                }

                PioneerInvectorWeaponBridge.PrepareHolsteredVisualSlot(holstered, item);
                holstered.SetActive(false);

                int index = slots.arraySize;
                slots.InsertArrayElementAtIndex(index);
                SerializedProperty slot = slots.GetArrayElementAtIndex(index);
                slot.FindPropertyRelative("slotId").stringValue = safeName;
                slot.FindPropertyRelative("item").objectReferenceValue = item;
                slot.FindPropertyRelative("drawnInstance").objectReferenceValue = drawn;
                slot.FindPropertyRelative("holsteredInstance").objectReferenceValue = holstered;
            }

            RemoveOrphanedWeaponSlots(root.transform, validSlotNames);

            serialized.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log($"Configured {slots.arraySize} preloaded Player_Invector ranged slot pair(s) (existing slot transforms preserved).");
        }


        private static void EnsureDefaultHolsterSocket(ItemData item)
        {
            if (item == null)
                return;

            if (!string.IsNullOrWhiteSpace(item.sheatheSocketName) &&
                !item.sheatheSocketName.Equals("Spine", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            item.sheatheSocketName = PioneerInvectorWeaponBridge.ResolveDefaultMeleeHolsterSocketName(item);
            EditorUtility.SetDirty(item);
        }

        private static List<ItemData> FindMeleeItems()
        {
            List<ItemData> items = new List<ItemData>();
            HashSet<ItemData> seen = new HashSet<ItemData>();

            ItemRegistry registry = AssetDatabase.LoadAssetAtPath<ItemRegistry>(ProjectAssetPaths.ItemRegistry);
            if (registry != null)
            {
                SerializedObject serialized = new SerializedObject(registry);
                SerializedProperty registryItems = serialized.FindProperty("items");
                if (registryItems != null)
                {
                    for (int i = 0; i < registryItems.arraySize; i++)
                        AddMeleeItem(registryItems.GetArrayElementAtIndex(i).objectReferenceValue as ItemData, items, seen);
                }
            }

            string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { ProjectAssetPaths.ItemsData });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                AddMeleeItem(AssetDatabase.LoadAssetAtPath<ItemData>(path), items, seen);
            }

            return items;
        }

        private static List<ItemData> FindDefaultRangedItems()
        {
            List<ItemData> items = new List<ItemData>();
            AddDefaultRangedItem(PistolItemPath, items);
            AddDefaultRangedItem(RifleItemPath, items);
            // OneHanded pistol base + existing-transform preserve — do not convert to TwoHanded rifle base.
            AddDefaultRangedItem(MiningToolItemPath, items);
            return items;
        }

        private static void AddDefaultRangedItem(string path, List<ItemData> items)
        {
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (item != null && item.itemType == ItemType.RangedWeapon && !items.Contains(item))
                items.Add(item);
        }

        private static void AddMeleeItem(ItemData item, List<ItemData> items, HashSet<ItemData> seen)
        {
            if (item == null || item.itemType != ItemType.MeleeWeapon || seen.Contains(item))
                return;

            seen.Add(item);
            items.Add(item);
        }

        public static GameObject ResolveMeleeWeaponPrefab(ItemData item)
        {
            if (item == null)
                return null;

            if (item.invectorWeaponPrefab != null)
                return item.invectorWeaponPrefab;

            return item.IsTwoHanded ? LoadPrefab(DefaultTwoHandMeleePath) : LoadPrefab(DefaultMeleePath);
        }

        public static GameObject ResolveRangedWeaponPrefab(ItemData item)
        {
            if (item == null)
                return null;

            if (item.invectorWeaponPrefab != null)
                return item.invectorWeaponPrefab;

            return item.weaponGrip == WeaponGrip.TwoHanded ? LoadPrefab(DefaultRiflePath) : LoadPrefab(DefaultPistolPath);
        }

        private static Transform FindRightMeleeHandler(GameObject root)
        {
            vCollectMeleeControl collectControl = root.GetComponent<vCollectMeleeControl>();
            if (collectControl != null && collectControl.rightHandler.customHandlers != null)
            {
                for (int i = 0; i < collectControl.rightHandler.customHandlers.Count; i++)
                {
                    Transform handler = collectControl.rightHandler.customHandlers[i];
                    if (handler != null && handler.name.Equals("meleeHandler", StringComparison.OrdinalIgnoreCase))
                        return handler;
                }
            }

            return FindChildTransformByName(root.transform, "meleeHandler");
        }

        private static Transform FindRightRangedHandler(GameObject root)
        {
            vCollectMeleeControl collectControl = root.GetComponent<vCollectMeleeControl>();
            if (collectControl != null && collectControl.rightHandler.defaultHandler != null)
                return collectControl.rightHandler.defaultHandler;

            Transform handler = FindChildTransformByName(root.transform, "defaultHandler");
            if (handler != null)
                return handler;

            return FindChildTransformByName(root.transform, "RightHand");
        }

        private static Transform FindRangedHolsterParent(GameObject root, ItemData item)
        {
            if (root == null)
                return null;

            // Mining tools share the back rifle holster (per design), then fall back by grip.
            string holderName = item != null && item.weaponGrip == WeaponGrip.TwoHanded
                ? "RifleHolder"
                : "HandgunHolder";

            // Prefer the live VBOT_ bone holder over BodySnaps proxy (proxy has tiny scale).
            Transform preferred = null;
            Transform fallback = null;
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                Transform t = all[i];
                if (t == null || t.name != holderName)
                    continue;

                string path = GetTransformPath(t);
                if (path.IndexOf("VBOT_", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    preferred = t;
                    break;
                }

                fallback ??= t;
            }

            if (preferred != null)
                return preferred;
            if (fallback != null)
                return fallback;

            string socketName = PioneerInvectorWeaponBridge.ResolveHolsterSocketName(item);
            return FindChildTransformByName(root.transform, socketName);
        }

        private static string GetTransformPath(Transform t)
        {
            string path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }

            return path;
        }

        private static GameObject InstantiatePrefabChild(GameObject prefab, Transform parent, string name)
        {
            // Plain Instantiate (not PrefabUtility.InstantiatePrefab) so the slot copy is NOT a
            // linked prefab instance. Slot preparation destroys child objects/components, which
            // Unity forbids inside linked prefab instances.
            GameObject instance = UnityEngine.Object.Instantiate(prefab, parent, false);

            instance.name = name;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            return instance;
        }

        private static void AddMeleeSlotNames(HashSet<string> validSlotNames)
        {
            if (validSlotNames == null)
                return;

            List<ItemData> items = FindMeleeItems();
            for (int i = 0; i < items.Count; i++)
                AddSlotPairNames(validSlotNames, items[i]);
        }

        private static void AddRangedSlotNames(HashSet<string> validSlotNames)
        {
            if (validSlotNames == null)
                return;

            List<ItemData> items = FindDefaultRangedItems();
            for (int i = 0; i < items.Count; i++)
                AddSlotPairNames(validSlotNames, items[i]);
        }

        private static void AddSlotPairNames(HashSet<string> validSlotNames, ItemData item)
        {
            if (validSlotNames == null || item == null)
                return;

            string safeName = MakeSafeObjectName(item.itemName, item.name);
            validSlotNames.Add($"Drawn_{safeName}");
            validSlotNames.Add($"Holstered_{safeName}");
        }

        private static void RemoveOrphanedWeaponSlots(Transform root, HashSet<string> validSlotNames)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);
                RemoveOrphanedWeaponSlots(child, validSlotNames);
                bool isGeneratedSlot =
                    child.name.StartsWith("Drawn_", StringComparison.Ordinal) ||
                    child.name.StartsWith("Holstered_", StringComparison.Ordinal);
                if (isGeneratedSlot && !validSlotNames.Contains(child.name))
                    UnityEngine.Object.DestroyImmediate(child.gameObject, true);
            }
        }

        private static Transform EnsureChild(Transform parent, string name)
        {
            Transform existing = FindChildTransformByName(parent, name);
            if (existing != null)
                return existing;

            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child.transform;
        }

        private static Transform FindChildTransformByName(Transform root, string name)
        {
            if (root == null || string.IsNullOrEmpty(name))
                return null;

            if (root.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindChildTransformByName(root.GetChild(i), name);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static void ApplyHeldTransform(Transform transform, ItemData item)
        {
            if (transform == null || item == null)
                return;

            transform.localPosition = item.heldLocalPosition;
            transform.localScale = item.heldLocalScale;
            transform.localRotation = item.useHeldLocalRotation ? item.heldLocalRotation : Quaternion.Euler(item.heldLocalEuler);
        }

        private static void ApplySheathedTransform(Transform transform, ItemData item)
        {
            if (transform == null || item == null)
                return;

            transform.localPosition = item.sheathedLocalPosition;
            transform.localScale = item.sheathedLocalScale == Vector3.zero ? Vector3.one : item.sheathedLocalScale;
            transform.localRotation = item.useSheathedLocalRotation ? item.sheathedLocalRotation : Quaternion.Euler(item.sheathedLocalEuler);
        }

        private static string MakeSafeObjectName(string preferred, string fallback)
        {
            string raw = string.IsNullOrWhiteSpace(preferred) ? fallback : preferred;
            if (string.IsNullOrWhiteSpace(raw))
                raw = "MeleeWeapon";

            char[] chars = raw.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (!char.IsLetterOrDigit(chars[i]) && chars[i] != '_')
                    chars[i] = '_';
            }

            return new string(chars);
        }

        private static void AssignItemWeaponPrefab(string itemPath, GameObject weaponPrefab)
        {
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(itemPath);
            if (item == null || weaponPrefab == null)
                return;

            item.invectorWeaponPrefab = weaponPrefab;
            EditorUtility.SetDirty(item);
        }

        private static GameObject LoadPrefab(string path) =>
            AssetDatabase.LoadAssetAtPath<GameObject>(path);

        private static void EnsureDirectory(string assetPath)
        {
            string directory = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(directory) && !AssetDatabase.IsValidFolder(directory))
            {
                string parent = "Assets/_Project/Prefabs";
                if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs/Players"))
                    AssetDatabase.CreateFolder(parent, "Players");
            }
        }
    }
}
#endif
