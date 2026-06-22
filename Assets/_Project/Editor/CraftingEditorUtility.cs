using System.Collections.Generic;
using System.IO;
using Project.Crafting;
using Project.Data;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    public static class CraftingEditorUtility
    {
        public const string ItemsFolder = ProjectAssetPaths.ItemsData;
        public const string ItemPrefabsFolder = ProjectAssetPaths.PrefabsItems;
        public const string RecipesFolder = ProjectAssetPaths.RecipesData;
        public const string CraftingResourcesFolder = ProjectAssetPaths.ResourcesCrafting;
        public const string CraftingPrefabsFolder = ProjectAssetPaths.PrefabsCrafting;
        public const string CraftingStationsFolder = ProjectAssetPaths.PrefabsCraftingStations;
        public const string DefaultBookVisualPath = "Assets/PolygonTown/Prefabs/Props/SM_Prop_BookOpen_01.prefab";
        public const string DefaultCraftingBookVisualPath = "Assets/Synty/PolygonIcons/Prefabs/SM_Icon_Crafting_Book_01.prefab";
        public const string RecipeRegistryPath = ProjectAssetPaths.RecipeRegistry;
        public const string ItemRegistryPath = ProjectAssetPaths.ItemRegistry;

        public static void EnsureFolder(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || AssetDatabase.IsValidFolder(folderPath))
                return;

            folderPath = folderPath.Replace('\\', '/');
            string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent))
                EnsureFolder(parent);

            string folderName = Path.GetFileName(folderPath);
            if (string.IsNullOrEmpty(folderName) || string.IsNullOrEmpty(parent))
                return;

            if (!AssetDatabase.IsValidFolder(folderPath))
                AssetDatabase.CreateFolder(parent, folderName);
        }

        public static string SanitizeAssetName(string rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName))
                return string.Empty;

            char[] invalid = Path.GetInvalidFileNameChars();
            string sanitized = rawName.Trim();
            foreach (char character in invalid)
                sanitized = sanitized.Replace(character, '_');

            return sanitized.Replace('/', '_').Replace('\\', '_');
        }

        public static ItemData[] LoadAllItems()
        {
            EnsureFolder(ItemsFolder);
            string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { ItemsFolder });
            List<ItemData> items = new List<ItemData>(guids.Length);

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
                if (item != null)
                    items.Add(item);
            }

            items.Sort((a, b) => string.Compare(a != null ? a.itemName : string.Empty, b != null ? b.itemName : string.Empty, System.StringComparison.OrdinalIgnoreCase));
            return items.ToArray();
        }

        public static RecipeDefinition[] LoadAllRecipeAssets()
        {
            EnsureFolder(RecipesFolder);
            string[] guids = AssetDatabase.FindAssets("t:RecipeDefinition", new[] { RecipesFolder });
            List<RecipeDefinition> recipes = new List<RecipeDefinition>(guids.Length);

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                RecipeDefinition recipe = AssetDatabase.LoadAssetAtPath<RecipeDefinition>(path);
                if (recipe != null)
                    recipes.Add(recipe);
            }

            recipes.Sort((a, b) => string.Compare(a != null ? a.displayName : string.Empty, b != null ? b.displayName : string.Empty, System.StringComparison.OrdinalIgnoreCase));
            return recipes.ToArray();
        }

        public static ItemData FindItemByName(string itemName)
        {
            if (string.IsNullOrEmpty(itemName))
                return null;

            foreach (ItemData item in LoadAllItems())
            {
                if (item != null && item.itemName == itemName)
                    return item;
            }

            return null;
        }

        public static void AddItemToRegistry(ItemData item)
        {
            if (item == null)
                return;

            ItemRegistry registry = AssetDatabase.LoadAssetAtPath<ItemRegistry>(ItemRegistryPath);
            if (registry == null)
            {
                Debug.LogWarning($"CraftingEditorUtility: ItemRegistry not found at {ItemRegistryPath}.");
                return;
            }

            SerializedObject serialized = new SerializedObject(registry);
            SerializedProperty itemsProperty = serialized.FindProperty("items");
            if (itemsProperty == null)
                return;

            for (int i = 0; i < itemsProperty.arraySize; i++)
            {
                if (itemsProperty.GetArrayElementAtIndex(i).objectReferenceValue == item)
                    return;
            }

            int insertIndex = itemsProperty.arraySize;
            itemsProperty.InsertArrayElementAtIndex(insertIndex);
            itemsProperty.GetArrayElementAtIndex(insertIndex).objectReferenceValue = item;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(registry);
        }

        public static void AddRecipeToRegistry(RecipeDefinition recipe)
        {
            if (recipe == null)
                return;

            RecipeRegistry registry = AssetDatabase.LoadAssetAtPath<RecipeRegistry>(RecipeRegistryPath);
            if (registry == null)
            {
                EnsureFolder(CraftingResourcesFolder);
                registry = ScriptableObject.CreateInstance<RecipeRegistry>();
                AssetDatabase.CreateAsset(registry, RecipeRegistryPath);
            }

            SerializedObject serialized = new SerializedObject(registry);
            SerializedProperty recipesProperty = serialized.FindProperty("recipes");
            if (recipesProperty == null)
                return;

            string recipeId = recipe.ResolvedId;
            for (int i = 0; i < recipesProperty.arraySize; i++)
            {
                RecipeDefinition existing = recipesProperty.GetArrayElementAtIndex(i).objectReferenceValue as RecipeDefinition;
                if (existing == null)
                    continue;

                if (existing == recipe || existing.ResolvedId == recipeId)
                {
                    recipesProperty.GetArrayElementAtIndex(i).objectReferenceValue = recipe;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(registry);
                    return;
                }
            }

            int insertIndex = recipesProperty.arraySize;
            recipesProperty.InsertArrayElementAtIndex(insertIndex);
            recipesProperty.GetArrayElementAtIndex(insertIndex).objectReferenceValue = recipe;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(registry);
        }

        public static RecipeDefinition SaveRecipeAsset(RecipeDefinition source, string assetFileName)
        {
            if (source == null)
                return null;

            string safeName = SanitizeAssetName(assetFileName);
            if (string.IsNullOrEmpty(safeName))
                return null;

            EnsureFolder(RecipesFolder);
            string path = $"{RecipesFolder}/{safeName}.asset";

            RecipeDefinition existing = AssetDatabase.LoadAssetAtPath<RecipeDefinition>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(source, path);
                existing = source;
            }
            else
            {
                existing.recipeId = source.recipeId;
                existing.displayName = source.displayName;
                existing.description = source.description;
                existing.stationType = source.stationType;
                existing.outputItem = source.outputItem;
                existing.outputAmount = source.outputAmount;
                existing.icon = source.icon;
                existing.ingredients = source.ingredients != null
                    ? new List<RecipeIngredient>(source.ingredients)
                    : new List<RecipeIngredient>();
            }

            EditorUtility.SetDirty(existing);
            return existing;
        }

        public static void DrawIngredientListEditor(ref List<RecipeIngredient> ingredients, ItemData[] itemOptions)
        {
            if (ingredients == null)
                ingredients = new List<RecipeIngredient>();

            EditorGUILayout.LabelField("Ingredients", EditorStyles.boldLabel);

            for (int i = 0; i < ingredients.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                RecipeIngredient ingredient = ingredients[i];
                if (ingredient == null)
                {
                    ingredient = new RecipeIngredient { amount = 1 };
                    ingredients[i] = ingredient;
                }

                ingredient.item = (ItemData)EditorGUILayout.ObjectField(ingredient.item, typeof(ItemData), false, GUILayout.MinWidth(160f));
                ingredient.amount = Mathf.Max(1, EditorGUILayout.IntField(ingredient.amount, GUILayout.Width(52f)));

                if (GUILayout.Button("X", GUILayout.Width(24f)))
                {
                    ingredients.RemoveAt(i);
                    EditorGUILayout.EndHorizontal();
                    break;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Ingredient", GUILayout.Width(140f)))
                ingredients.Add(new RecipeIngredient { amount = 1 });

            if (itemOptions != null && itemOptions.Length > 0 && GUILayout.Button("Quick Add From List", GUILayout.Width(160f)))
                ShowItemPickerMenu(ingredients);

            EditorGUILayout.EndHorizontal();
        }

        private static void ShowItemPickerMenu(List<RecipeIngredient> ingredients)
        {
            GenericMenu menu = new GenericMenu();
            ItemData[] items = LoadAllItems();

            for (int i = 0; i < items.Length; i++)
            {
                ItemData item = items[i];
                if (item == null)
                    continue;

                menu.AddItem(new GUIContent(item.itemName), false, () =>
                {
                    ingredients.Add(new RecipeIngredient { item = item, amount = 1 });
                });
            }

            if (items.Length == 0)
                menu.AddDisabledItem(new GUIContent("No items found"));

            menu.ShowAsContext();
        }

        public static GameObject FindSceneObject(string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
                return null;

            GameObject activeMatch = GameObject.Find(objectName);
            if (activeMatch != null)
                return activeMatch;

            Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform transform = transforms[i];
                if (transform != null && transform.name == objectName)
                    return transform.gameObject;
            }

            return null;
        }

        public static string GetRecipePickupPrefabPath(string recipeId)
        {
            string safeId = SanitizeAssetName(recipeId);
            if (string.IsNullOrEmpty(safeId))
                return string.Empty;

            return $"{CraftingPrefabsFolder}/RecipePickup_{safeId}.prefab";
        }

        public static GameObject LoadDefaultBookVisual()
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(DefaultBookVisualPath);
        }

        public static GameObject LoadDefaultCraftingBookVisual()
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(DefaultCraftingBookVisualPath);
        }

        public static GameObject CreateRecipePickupPrefab(
            string recipeId,
            GameObject visualTemplate,
            float interactRange = 2.5f,
            Vector3? colliderSize = null,
            bool autoFitCollider = true,
            bool confirmOverwrite = true)
        {
            if (string.IsNullOrWhiteSpace(recipeId))
                return null;

            EnsureFolder(CraftingPrefabsFolder);

            string prefabPath = GetRecipePickupPrefabPath(recipeId);
            if (string.IsNullOrEmpty(prefabPath))
                return null;

            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existingPrefab != null && confirmOverwrite &&
                !EditorUtility.DisplayDialog(
                    "Recipe Pickup Prefab",
                    $"Prefab already exists at\n{prefabPath}\n\nOverwrite it?",
                    "Overwrite",
                    "Cancel"))
            {
                return existingPrefab;
            }

            GameObject instance = BuildRecipePickupInstance(recipeId.Trim(), visualTemplate, interactRange, colliderSize, autoFitCollider);
            if (instance == null)
                return null;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Object.DestroyImmediate(instance);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return prefab;
        }

        public static GameObject PlaceRecipePickupInScene(
            string recipeId,
            GameObject visualTemplate,
            Transform parent = null,
            float interactRange = 2.5f,
            Vector3? colliderSize = null,
            bool autoFitCollider = true,
            bool savePrefabIfMissing = true)
        {
            if (string.IsNullOrWhiteSpace(recipeId))
                return null;

            string prefabPath = GetRecipePickupPrefabPath(recipeId);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null && savePrefabIfMissing)
                prefab = CreateRecipePickupPrefab(recipeId, visualTemplate, interactRange, colliderSize, autoFitCollider);

            GameObject instance;
            if (prefab != null)
            {
                instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                if (instance == null)
                    instance = Object.Instantiate(prefab);
            }
            else
            {
                instance = BuildRecipePickupInstance(recipeId.Trim(), visualTemplate, interactRange, colliderSize, autoFitCollider);
            }

            if (instance == null)
                return null;

            Undo.RegisterCreatedObjectUndo(instance, "Place Recipe Pickup");

            if (parent != null)
            {
                instance.transform.SetParent(parent, false);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
            }

            return instance;
        }

        public static GameObject BuildRecipePickupInstance(
            string recipeId,
            GameObject visualTemplate,
            float interactRange = 2.5f,
            Vector3? colliderSize = null,
            bool autoFitCollider = true)
        {
            if (string.IsNullOrWhiteSpace(recipeId))
                return null;

            string safeId = SanitizeAssetName(recipeId);
            GameObject root = new GameObject($"RecipePickup_{safeId}");

            if (visualTemplate != null)
            {
                GameObject visual = PrefabUtility.InstantiatePrefab(visualTemplate) as GameObject;
                if (visual == null)
                    visual = Object.Instantiate(visualTemplate);

                visual.name = visualTemplate.name;
                visual.transform.SetParent(root.transform, false);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
            }

            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.isTrigger = true;

            if (autoFitCollider && root.transform.childCount > 0)
                FitBoxColliderToRenderers(root, collider);
            else
                collider.size = colliderSize ?? new Vector3(0.5f, 0.5f, 0.5f);

            RecipePickup pickup = root.AddComponent<RecipePickup>();
            pickup.Configure(recipeId, "Press E to collect recipe", interactRange);
            return root;
        }

        private static void FitBoxColliderToRenderers(GameObject root, BoxCollider boxCollider)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                boxCollider.size = new Vector3(0.5f, 0.5f, 0.5f);
                return;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            Transform transform = boxCollider.transform;
            boxCollider.center = transform.InverseTransformPoint(bounds.center);
            Vector3 lossy = transform.lossyScale;
            boxCollider.size = new Vector3(
                SafeDivide(bounds.size.x, Mathf.Abs(lossy.x)),
                SafeDivide(bounds.size.y, Mathf.Abs(lossy.y)),
                SafeDivide(bounds.size.z, Mathf.Abs(lossy.z)));
        }

        private static float SafeDivide(float value, float divisor)
        {
            return Mathf.Approximately(divisor, 0f) ? value : value / divisor;
        }

        [MenuItem(SurvivalPioneerEditorMenus.Crafting + "Sync Recipe Icons From Output")]
        public static void SyncRecipeIconsFromOutput()
        {
            RecipeDefinition[] recipes = LoadAllRecipeAssets();
            int updated = 0;

            for (int i = 0; i < recipes.Length; i++)
            {
                RecipeDefinition recipe = recipes[i];
                if (recipe == null || recipe.outputItem == null || recipe.outputItem.icon == null)
                    continue;

                if (recipe.icon == recipe.outputItem.icon)
                    continue;

                recipe.icon = recipe.outputItem.icon;
                EditorUtility.SetDirty(recipe);
                updated++;
            }

            if (updated > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            EditorUtility.DisplayDialog(
                "Sync Recipe Icons",
                updated > 0
                    ? $"Assigned output icons to {updated} recipe(s)."
                    : "All recipes already have matching icons (or are missing output items).",
                "OK");
        }
    }
}
