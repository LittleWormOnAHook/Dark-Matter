using System.Collections.Generic;
using System.IO;
using Project.Crafting;
using Project.Data;
using Project.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.EditorTools
{
    public static class CraftingSetup
    {
        private const string RecipesFolder = ProjectAssetPaths.RecipesData;
        private const string ItemsFolder = ProjectAssetPaths.ItemsData;
        private const string ResourcesFolder = ProjectAssetPaths.ResourcesCrafting;
        private const string RegistryPath = ProjectAssetPaths.RecipeRegistry;
        private const string PlayerPrefabPath = ProjectAssetPaths.PlayerPrefab;

        private static readonly (string id, string file, CraftingStationType station, string name, string desc, (string item, int amount)[] ingredients, string output, int outputAmount)[] RecipeSpecs =
        {
            ("grilled_mushroom", "grilled_mushroom", CraftingStationType.Cooking, "Grilled Mushroom", "Cook mushrooms over the fire.",
                new[] { ("Mushroom", 2) }, "Cooked Mushroom", 1),
            ("forest_stew", "forest_stew", CraftingStationType.Cooking, "Forest Stew", "A hearty stew from foraged ingredients.",
                new[] { ("Mushroom", 2), ("Red Lilly", 1) }, "Forest Stew", 1),
            ("herbal_medpack", "herbal_medpack", CraftingStationType.Workbench, "Herbal Medpack", "Combine herbs into a medpack.",
                new[] { ("Red Lilly", 1), ("Mushroom", 2) }, "Medpack", 1),
            ("stone_salve", "stone_salve", CraftingStationType.Workbench, "Stone Salve", "Crush rocks into a crude salve.",
                new[] { ("Rock", 3) }, "Medpack", 1)
        };

        [MenuItem(SurvivalPioneerEditorMenus.Crafting + "Wire Scene Stations")]
        public static void WireCraftingStations()
        {
            EnsureFolders();
            ItemData cookedMushroom = EnsureCookedMushroomItem();
            ItemData forestStew = EnsureForestStewItem();
            ItemData oxygenTank = EnsureOxygenTankItem();
            Dictionary<string, ItemData> items = LoadItemLookup();
            items["Cooked Mushroom"] = cookedMushroom;
            items["Forest Stew"] = forestStew;
            items["Oxygen Tank"] = oxygenTank;

            List<RecipeDefinition> recipes = EnsureRecipeAssets(items);
            EnsureRecipeRegistry(recipes);
            WireStation("Cooking", CraftingStationType.Cooking);
            WireStation("Workbench", CraftingStationType.Workbench);
            WireRecipePickups("Recipe Book", recipes);
            EnsurePlayerCraftingManager();
            EnsureCraftingUi();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            Debug.Log("Crafting setup complete. Press E at Cooking/Workbench stations, find recipe scrolls near the Recipe Book, and use Journal > Craft tab.");
        }

        [MenuItem(SurvivalPioneerEditorMenus.Crafting + "Seed Starter Recipes")]
        public static void CreateCraftingContentOnly()
        {
            EnsureFolders();
            ItemData cookedMushroom = EnsureCookedMushroomItem();
            ItemData forestStew = EnsureForestStewItem();
            ItemData oxygenTank = EnsureOxygenTankItem();
            Dictionary<string, ItemData> items = LoadItemLookup();
            items["Cooked Mushroom"] = cookedMushroom;
            items["Forest Stew"] = forestStew;
            items["Oxygen Tank"] = oxygenTank;

            List<RecipeDefinition> recipes = EnsureRecipeAssets(items);
            EnsureRecipeRegistry(recipes);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Created {recipes.Count} recipes and RecipeRegistry at {RegistryPath}.");
        }

        private static void EnsureFolders()
        {
            EnsureFolder(ProjectAssetPaths.Data + "/Crafting");
            EnsureFolder(RecipesFolder);
            EnsureFolder(ResourcesFolder);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folderName = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(folderName))
                AssetDatabase.CreateFolder(parent, folderName);
        }

        private static Dictionary<string, ItemData> LoadItemLookup()
        {
            Dictionary<string, ItemData> lookup = new Dictionary<string, ItemData>();
            string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { ItemsFolder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
                if (item != null && !lookup.ContainsKey(item.itemName))
                    lookup[item.itemName] = item;
            }

            return lookup;
        }

        private static ItemData EnsureCookedMushroomItem()
        {
            string path = ItemsFolder + "/Cooked Mushroom.asset";
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            ItemData mushroom = AssetDatabase.LoadAssetAtPath<ItemData>(ItemsFolder + "/Mushroom.asset");

            if (item == null)
            {
                item = ScriptableObject.CreateInstance<ItemData>();
                AssetDatabase.CreateAsset(item, path);
            }

            item.itemName = "Cooked Mushroom";
            item.itemType = ItemType.Consumable;
            item.maxStack = 64;
            item.energyRestore = 35f;
            item.staminaRestore = 15f;
            item.tooltipDescription = "A warm grilled mushroom. Restores energy and stamina.";
            if (item.icon == null && mushroom != null)
                item.icon = mushroom.icon;
            if (item.worldPrefab == null && mushroom != null)
                item.worldPrefab = mushroom.worldPrefab;

            EditorUtility.SetDirty(item);
            return item;
        }

        private static ItemData EnsureForestStewItem()
        {
            string path = ItemsFolder + "/Forest Stew.asset";
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            ItemData mushroom = AssetDatabase.LoadAssetAtPath<ItemData>(ItemsFolder + "/Mushroom.asset");

            if (item == null)
            {
                item = ScriptableObject.CreateInstance<ItemData>();
                AssetDatabase.CreateAsset(item, path);
            }

            item.itemName = "Forest Stew";
            item.itemType = ItemType.Consumable;
            item.maxStack = 32;
            item.energyRestore = 70f;
            item.staminaRestore = 5f;
            item.healthRestore = 10f;
            item.tooltipDescription = "A nourishing forest stew.";
            if (item.icon == null && mushroom != null)
                item.icon = mushroom.icon;

            EditorUtility.SetDirty(item);
            return item;
        }

        private static ItemData EnsureOxygenTankItem()
        {
            string path = ItemsFolder + "/Oxygen Tank.asset";
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);

            if (item == null)
            {
                item = ScriptableObject.CreateInstance<ItemData>();
                AssetDatabase.CreateAsset(item, path);
            }

            item.itemName = "Oxygen Tank";
            item.itemType = ItemType.Consumable;
            item.maxStack = 16;
            item.oxygenRestore = 600f;
            item.tooltipDescription = "Portable oxygen supply. Restores 10 minutes of breathable air.";
            EditorUtility.SetDirty(item);
            return item;
        }

        private static List<RecipeDefinition> EnsureRecipeAssets(Dictionary<string, ItemData> items)
        {
            List<RecipeDefinition> recipes = new List<RecipeDefinition>();

            foreach (var spec in RecipeSpecs)
            {
                string path = $"{RecipesFolder}/{spec.file}.asset";
                RecipeDefinition recipe = AssetDatabase.LoadAssetAtPath<RecipeDefinition>(path);
                if (recipe == null)
                {
                    recipe = ScriptableObject.CreateInstance<RecipeDefinition>();
                    AssetDatabase.CreateAsset(recipe, path);
                }

                recipe.recipeId = spec.id;
                recipe.displayName = spec.name;
                recipe.description = spec.desc;
                recipe.stationType = spec.station;
                recipe.outputAmount = spec.outputAmount;
                recipe.ingredients = new List<RecipeIngredient>();

                foreach ((string itemName, int amount) ingredientSpec in spec.ingredients)
                {
                    if (!items.TryGetValue(ingredientSpec.itemName, out ItemData ingredientItem))
                    {
                        Debug.LogWarning($"CraftingSetup: Missing ingredient item '{ingredientSpec.itemName}' for recipe '{spec.id}'.");
                        continue;
                    }

                    recipe.ingredients.Add(new RecipeIngredient
                    {
                        item = ingredientItem,
                        amount = ingredientSpec.amount
                    });
                }

                if (!items.TryGetValue(spec.output, out ItemData outputItem))
                    Debug.LogWarning($"CraftingSetup: Missing output item '{spec.output}' for recipe '{spec.id}'.");
                else
                    recipe.outputItem = outputItem;

                EditorUtility.SetDirty(recipe);
                recipes.Add(recipe);
            }

            return recipes;
        }

        [MenuItem(SurvivalPioneerEditorMenus.Crafting + "Sync Recipe Registry")]
        public static void SyncRecipeRegistryFromDataFolder()
        {
            EnsureFolders();
            List<RecipeDefinition> recipes = LoadAllRecipeDefinitionsFromFolder();
            EnsureRecipeRegistry(recipes);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Synced {recipes.Count} recipes to RecipeRegistry at {RegistryPath}.");
        }

        private static List<RecipeDefinition> LoadAllRecipeDefinitionsFromFolder()
        {
            List<RecipeDefinition> recipes = new List<RecipeDefinition>();
            string[] guids = AssetDatabase.FindAssets("t:RecipeDefinition", new[] { RecipesFolder });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                RecipeDefinition recipe = AssetDatabase.LoadAssetAtPath<RecipeDefinition>(path);
                if (recipe != null)
                    recipes.Add(recipe);
            }

            recipes.Sort((a, b) => string.CompareOrdinal(a?.ResolvedId, b?.ResolvedId));
            return recipes;
        }

        private static void EnsureRecipeRegistry(List<RecipeDefinition> recipes)
        {
            List<RecipeDefinition> registryRecipes = LoadAllRecipeDefinitionsFromFolder();
            if (registryRecipes.Count == 0)
                registryRecipes = recipes;

            RecipeRegistry registry = AssetDatabase.LoadAssetAtPath<RecipeRegistry>(RegistryPath);
            if (registry == null)
            {
                registry = ScriptableObject.CreateInstance<RecipeRegistry>();
                AssetDatabase.CreateAsset(registry, RegistryPath);
            }

            SerializedObject serialized = new SerializedObject(registry);
            SerializedProperty recipesProperty = serialized.FindProperty("recipes");
            recipesProperty.arraySize = registryRecipes.Count;
            for (int i = 0; i < registryRecipes.Count; i++)
                recipesProperty.GetArrayElementAtIndex(i).objectReferenceValue = registryRecipes[i];

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(registry);
        }

        private static void WireStation(string objectName, CraftingStationType stationType)
        {
            GameObject target = CraftingEditorUtility.FindSceneObject(objectName);
            if (target == null)
            {
                Debug.LogWarning($"CraftingSetup: Could not find scene object '{objectName}'. Open Pioneer.unity and try again.");
                return;
            }

            if (!target.scene.IsValid())
            {
                Debug.LogWarning($"CraftingSetup: '{objectName}' is not part of the active scene.");
                return;
            }

            CraftingStation station = target.GetComponent<CraftingStation>();
            if (station == null)
                station = Undo.AddComponent<CraftingStation>(target);
            if (station == null)
                station = target.AddComponent<CraftingStation>();

            if (station == null)
            {
                Debug.LogWarning($"CraftingSetup: Failed to add CraftingStation to '{objectName}'.");
                return;
            }

            station.Configure(stationType);
            EditorUtility.SetDirty(station);
            PrefabUtility.RecordPrefabInstancePropertyModifications(station);

            Debug.Log($"Wired CraftingStation ({stationType}) on '{objectName}'.");
        }

        private static void WireRecipePickups(string anchorName, List<RecipeDefinition> recipes)
        {
            GameObject anchor = CraftingEditorUtility.FindSceneObject(anchorName);
            if (anchor == null)
            {
                Debug.LogWarning($"CraftingSetup: Could not find '{anchorName}' for recipe pickups.");
                return;
            }

            Transform host = anchor.transform.Find("RecipePickups");
            if (host == null)
            {
                GameObject hostObject = new GameObject("RecipePickups");
                Undo.RegisterCreatedObjectUndo(hostObject, "Create Recipe Pickups");
                hostObject.transform.SetParent(anchor.transform, false);
                host = hostObject.transform;
            }

            Vector3 basePosition = anchor.transform.position + anchor.transform.forward * 1.2f + Vector3.up * 0.35f;
            Vector3 right = anchor.transform.right;

            for (int i = 0; i < recipes.Count; i++)
            {
                RecipeDefinition recipe = recipes[i];
                if (recipe == null)
                    continue;

                string childName = $"RecipePickup_{recipe.ResolvedId}";
                Transform existing = host.Find(childName);
                GameObject pickupObject = existing != null ? existing.gameObject : null;

                if (pickupObject == null)
                {
                    string prefabPath = CraftingEditorUtility.GetRecipePickupPrefabPath(recipe.ResolvedId);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    if (prefab != null)
                    {
                        pickupObject = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                        if (pickupObject != null)
                        {
                            pickupObject.name = childName;
                            Undo.RegisterCreatedObjectUndo(pickupObject, "Create Recipe Pickup");
                        }
                    }

                    if (pickupObject == null)
                    {
                        pickupObject = CraftingEditorUtility.BuildRecipePickupInstance(
                            recipe.ResolvedId,
                            CraftingEditorUtility.LoadDefaultBookVisual());
                        pickupObject.name = childName;
                        Undo.RegisterCreatedObjectUndo(pickupObject, "Create Recipe Pickup");
                    }

                    pickupObject.transform.SetParent(host, false);
                }

                pickupObject.transform.position = basePosition + right * (i - 1.5f) * 0.65f;

                RecipePickup pickup = pickupObject.GetComponent<RecipePickup>();
                if (pickup == null)
                    pickup = pickupObject.GetComponentInChildren<RecipePickup>();

                if (pickup != null)
                {
                    pickup.Configure(recipe.ResolvedId);
                    EditorUtility.SetDirty(pickup);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(pickup);
                }
                else
                {
                    Debug.LogWarning($"CraftingSetup: Recipe pickup prefab for '{recipe.ResolvedId}' is missing RecipePickup.");
                }

                pickupObject.SetActive(true);
            }

            Debug.Log($"Placed {recipes.Count} recipe pickups near '{anchorName}'.");
        }

        private static void EnsureTriggerCollider(GameObject target, Vector3 boxSize, bool isBox = true)
        {
            if (target == null)
                return;

            Collider[] colliders = target.GetComponents<Collider>();
            Collider trigger = null;
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null && colliders[i].isTrigger)
                {
                    trigger = colliders[i];
                    break;
                }
            }

            if (trigger == null)
            {
                if (isBox)
                {
                    BoxCollider box = Undo.AddComponent<BoxCollider>(target) ?? target.AddComponent<BoxCollider>();
                    box.size = boxSize;
                    box.isTrigger = true;
                }
                else
                {
                    SphereCollider sphere = Undo.AddComponent<SphereCollider>(target) ?? target.AddComponent<SphereCollider>();
                    sphere.radius = boxSize.x;
                    sphere.isTrigger = true;
                }
            }
            else
            {
                trigger.isTrigger = true;
                if (trigger is BoxCollider boxCollider)
                    boxCollider.size = boxSize;
            }
        }

        private static void EnsurePlayerCraftingManager()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning("CraftingSetup: No Player tagged object in scene. Attempting prefab wiring.");
                WirePlayerPrefabCraftingManager();
                return;
            }

            if (player.GetComponent<CraftingManager>() == null)
                Undo.AddComponent<CraftingManager>(player);

            Debug.Log("Ensured CraftingManager on Player.");
        }

        private static void WirePlayerPrefabCraftingManager()
        {
            GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (prefabRoot == null)
                return;

            GameObject prefabInstance = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            if (prefabInstance.GetComponent<CraftingManager>() == null)
            {
                prefabInstance.AddComponent<CraftingManager>();
                PrefabUtility.SaveAsPrefabAsset(prefabInstance, PlayerPrefabPath);
                Debug.Log("Added CraftingManager to Player prefab.");
            }

            PrefabUtility.UnloadPrefabContents(prefabInstance);
        }

        private static void EnsureCraftingUi()
        {
            UIManager uiManager = Object.FindAnyObjectByType<UIManager>();
            if (uiManager == null)
            {
                Debug.LogWarning("CraftingSetup: UIManager not found. CraftingUI will be created at runtime.");
                return;
            }

            if (uiManager.GetComponent<CraftingUI>() == null)
                Undo.AddComponent<CraftingUI>(uiManager.gameObject);

            if (uiManager.GetComponent<JournalPanelUI>() == null)
                Undo.AddComponent<JournalPanelUI>(uiManager.gameObject);

            Debug.Log("Ensured CraftingUI on UIManager.");
        }
    }
}
