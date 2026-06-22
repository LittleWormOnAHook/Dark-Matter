using System.Collections.Generic;
using Project.Crafting;
using Project.Data;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Author, edit, and register RecipeDefinition assets.
    /// </summary>
    public class RecipeCreatorWindow : EditorWindow
    {
        private RecipeDefinition[] recipeAssets = System.Array.Empty<RecipeDefinition>();
        private ItemData[] itemOptions = System.Array.Empty<ItemData>();
        private int selectedRecipeIndex = -1;

        private string recipeId = "new_recipe";
        private string displayName = "New Recipe";
        private string description = string.Empty;
        private CraftingStationType stationType = CraftingStationType.Cooking;
        private ItemData outputItem;
        private int outputAmount = 1;
        private Sprite recipeIcon;
        private List<RecipeIngredient> ingredients = new List<RecipeIngredient>();
        private string assetFileName = "new_recipe";
        private bool addToRecipeRegistry = true;

        private GameObject pickupVisualTemplate;
        private float pickupInteractRange = 2.5f;
        private Vector3 pickupColliderSize = new Vector3(0.5f, 0.5f, 0.5f);
        private bool autoFitPickupCollider = true;
        private bool createPickupPrefabOnSave = true;

        private Vector2 listScroll;
        private Vector2 editorScroll;

        [MenuItem(SurvivalPioneerEditorMenus.Crafting + "Recipe Creator")]
        public static void Open()
        {
            GetWindow<RecipeCreatorWindow>("Recipe Creator").minSize = new Vector2(760f, 520f);
        }

        private void OnEnable()
        {
            RefreshLists();
            if (pickupVisualTemplate == null)
                pickupVisualTemplate = CraftingEditorUtility.LoadDefaultBookVisual();
        }

        private void RefreshLists()
        {
            recipeAssets = CraftingEditorUtility.LoadAllRecipeAssets();
            itemOptions = CraftingEditorUtility.LoadAllItems();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Recipe Creator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Create or edit recipes, then register them for in-game discovery and crafting.", MessageType.Info);
            EditorGUILayout.Space(6f);

            EditorGUILayout.BeginHorizontal();

            DrawRecipeListPanel();
            DrawRecipeEditorPanel();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawRecipeListPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(240f));
            EditorGUILayout.LabelField("Recipes", EditorStyles.boldLabel);

            listScroll = EditorGUILayout.BeginScrollView(listScroll, GUILayout.ExpandHeight(true));
            for (int i = 0; i < recipeAssets.Length; i++)
            {
                RecipeDefinition recipe = recipeAssets[i];
                if (recipe == null)
                    continue;

                string label = string.IsNullOrEmpty(recipe.displayName) ? recipe.name : recipe.displayName;
                bool selected = i == selectedRecipeIndex;
                if (GUILayout.Toggle(selected, label, "Button"))
                {
                    if (selectedRecipeIndex != i)
                        LoadRecipe(recipe, i);
                }
            }
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("New Recipe", GUILayout.Height(28f)))
                StartNewRecipe();

            if (GUILayout.Button("Refresh List", GUILayout.Height(24f)))
                RefreshLists();

            EditorGUILayout.EndVertical();
        }

        private void DrawRecipeEditorPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            editorScroll = EditorGUILayout.BeginScrollView(editorScroll);

            recipeId = EditorGUILayout.TextField("Recipe Id", recipeId);
            assetFileName = EditorGUILayout.TextField("Asset File Name", assetFileName);
            displayName = EditorGUILayout.TextField("Display Name", displayName);
            description = EditorGUILayout.TextArea(description, GUILayout.MinHeight(56f));
            stationType = (CraftingStationType)EditorGUILayout.EnumPopup("Station", stationType);
            outputItem = (ItemData)EditorGUILayout.ObjectField("Output Item", outputItem, typeof(ItemData), false);
            outputAmount = Mathf.Max(1, EditorGUILayout.IntField("Output Amount", outputAmount));

            EditorGUILayout.BeginHorizontal();
            recipeIcon = (Sprite)EditorGUILayout.ObjectField("Recipe Icon", recipeIcon, typeof(Sprite), false);
            if (GUILayout.Button("Use Output", GUILayout.Width(90f)))
            {
                recipeIcon = outputItem != null ? outputItem.icon : null;
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();

            if (recipeIcon != null)
            {
                Rect preview = GUILayoutUtility.GetRect(48f, 48f, GUILayout.Width(48f));
                EditorGUI.DrawPreviewTexture(preview, recipeIcon.texture);
            }

            EditorGUILayout.Space(8f);
            CraftingEditorUtility.DrawIngredientListEditor(ref ingredients, itemOptions);
            addToRecipeRegistry = EditorGUILayout.Toggle("Add To Recipe Registry", addToRecipeRegistry);

            EditorGUILayout.Space(10f);
            DrawPickupPrefabSection();

            EditorGUILayout.Space(12f);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Recipe", GUILayout.Height(34f)))
                SaveCurrentRecipe();

            using (new EditorGUI.DisabledScope(selectedRecipeIndex < 0 || selectedRecipeIndex >= recipeAssets.Length))
            {
                if (GUILayout.Button("Delete Recipe", GUILayout.Height(34f)))
                    DeleteSelectedRecipe();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Pickup Prefab", GUILayout.Height(34f)))
                SavePickupPrefab();

            if (GUILayout.Button("Place In Scene", GUILayout.Height(34f)))
                PlacePickupInScene();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawPickupPrefabSection()
        {
            EditorGUILayout.LabelField("Recipe Pickup Prefab", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                $"Saves to {CraftingEditorUtility.CraftingPrefabsFolder}/RecipePickup_<id>.prefab using a book, scroll, or custom mesh.",
                MessageType.None);

            pickupVisualTemplate = (GameObject)EditorGUILayout.ObjectField(
                "Visual Template",
                pickupVisualTemplate,
                typeof(GameObject),
                false);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Use Open Book", GUILayout.Width(120f)))
                pickupVisualTemplate = CraftingEditorUtility.LoadDefaultBookVisual();
            if (GUILayout.Button("Use Crafting Book", GUILayout.Width(140f)))
                pickupVisualTemplate = CraftingEditorUtility.LoadDefaultCraftingBookVisual();
            if (GUILayout.Button("Use Selection", GUILayout.Width(110f)))
                UseSelectedVisualTemplate();
            EditorGUILayout.EndHorizontal();

            pickupInteractRange = EditorGUILayout.FloatField("Interact Range", pickupInteractRange);
            autoFitPickupCollider = EditorGUILayout.Toggle("Auto-fit Collider To Mesh", autoFitPickupCollider);
            using (new EditorGUI.DisabledScope(autoFitPickupCollider))
            {
                pickupColliderSize = EditorGUILayout.Vector3Field("Collider Size", pickupColliderSize);
            }

            createPickupPrefabOnSave = EditorGUILayout.Toggle("Create Pickup Prefab On Save", createPickupPrefabOnSave);
        }

        private void UseSelectedVisualTemplate()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("Recipe Creator", "Select a mesh or prefab in the Hierarchy or Project window.", "OK");
                return;
            }

            GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(selected);
            pickupVisualTemplate = source != null ? source : selected;
            Repaint();
        }

        private void StartNewRecipe()
        {
            selectedRecipeIndex = -1;
            recipeId = "new_recipe";
            assetFileName = "new_recipe";
            displayName = "New Recipe";
            description = string.Empty;
            stationType = CraftingStationType.Cooking;
            outputItem = null;
            outputAmount = 1;
            recipeIcon = null;
            ingredients = new List<RecipeIngredient>();
            addToRecipeRegistry = true;
            createPickupPrefabOnSave = true;
            if (pickupVisualTemplate == null)
                pickupVisualTemplate = CraftingEditorUtility.LoadDefaultBookVisual();
            Repaint();
        }

        private void LoadRecipe(RecipeDefinition recipe, int index)
        {
            selectedRecipeIndex = index;
            recipeId = recipe.ResolvedId;
            assetFileName = recipe.name;
            displayName = recipe.displayName;
            description = recipe.description;
            stationType = recipe.stationType;
            outputItem = recipe.outputItem;
            outputAmount = recipe.outputAmount;
            recipeIcon = recipe.icon;
            ingredients = recipe.ingredients != null
                ? new List<RecipeIngredient>(recipe.ingredients)
                : new List<RecipeIngredient>();
            Repaint();
        }

        private void SaveCurrentRecipe()
        {
            if (string.IsNullOrWhiteSpace(recipeId) || string.IsNullOrWhiteSpace(displayName))
            {
                EditorUtility.DisplayDialog("Recipe Creator", "Recipe id and display name are required.", "OK");
                return;
            }

            string safeFileName = CraftingEditorUtility.SanitizeAssetName(string.IsNullOrWhiteSpace(assetFileName) ? recipeId : assetFileName);
            if (string.IsNullOrEmpty(safeFileName))
            {
                EditorUtility.DisplayDialog("Recipe Creator", "Asset file name is invalid.", "OK");
                return;
            }

            RecipeDefinition draft = ScriptableObject.CreateInstance<RecipeDefinition>();
            draft.recipeId = recipeId.Trim();
            draft.displayName = displayName.Trim();
            draft.description = description;
            draft.stationType = stationType;
            draft.outputItem = outputItem;
            draft.outputAmount = outputAmount;
            draft.icon = recipeIcon != null ? recipeIcon : (outputItem != null ? outputItem.icon : null);
            draft.ingredients = new List<RecipeIngredient>(ingredients);

            RecipeDefinition saved = CraftingEditorUtility.SaveRecipeAsset(draft, safeFileName);
            if (saved == null)
            {
                EditorUtility.DisplayDialog("Recipe Creator", "Failed to save recipe.", "OK");
                return;
            }

            if (addToRecipeRegistry)
                CraftingEditorUtility.AddRecipeToRegistry(saved);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshLists();

            for (int i = 0; i < recipeAssets.Length; i++)
            {
                if (recipeAssets[i] == saved)
                {
                    selectedRecipeIndex = i;
                    break;
                }
            }

            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);

            if (createPickupPrefabOnSave)
            {
                CraftingEditorUtility.CreateRecipePickupPrefab(
                    recipeId.Trim(),
                    pickupVisualTemplate,
                    pickupInteractRange,
                    pickupColliderSize,
                    autoFitPickupCollider,
                    confirmOverwrite: false);
            }
        }

        private void DeleteSelectedRecipe()
        {
            if (selectedRecipeIndex < 0 || selectedRecipeIndex >= recipeAssets.Length)
                return;

            RecipeDefinition recipe = recipeAssets[selectedRecipeIndex];
            if (recipe == null)
                return;

            if (!EditorUtility.DisplayDialog("Recipe Creator", $"Delete recipe asset '{recipe.name}'?", "Delete", "Cancel"))
                return;

            string path = AssetDatabase.GetAssetPath(recipe);
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.SaveAssets();
            RefreshLists();
            StartNewRecipe();
        }

        private void SavePickupPrefab(bool showSuccessDialog = true)
        {
            if (string.IsNullOrWhiteSpace(recipeId))
            {
                EditorUtility.DisplayDialog("Recipe Creator", "Set a recipe id before saving a pickup prefab.", "OK");
                return;
            }

            GameObject prefab = CraftingEditorUtility.CreateRecipePickupPrefab(
                recipeId.Trim(),
                pickupVisualTemplate,
                pickupInteractRange,
                pickupColliderSize,
                autoFitPickupCollider,
                confirmOverwrite: true);

            if (prefab == null)
                return;

            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);

            if (showSuccessDialog)
            {
                EditorUtility.DisplayDialog(
                    "Recipe Creator",
                    $"Saved pickup prefab to\n{AssetDatabase.GetAssetPath(prefab)}",
                    "OK");
            }
        }

        private void PlacePickupInScene()
        {
            if (string.IsNullOrWhiteSpace(recipeId))
            {
                EditorUtility.DisplayDialog("Recipe Creator", "Set a recipe id before placing a pickup.", "OK");
                return;
            }

            Transform parent = Selection.activeTransform;
            GameObject instance = CraftingEditorUtility.PlaceRecipePickupInScene(
                recipeId.Trim(),
                pickupVisualTemplate,
                parent,
                pickupInteractRange,
                pickupColliderSize,
                autoFitPickupCollider,
                savePrefabIfMissing: true);

            if (instance == null)
            {
                EditorUtility.DisplayDialog("Recipe Creator", "Could not place recipe pickup.", "OK");
                return;
            }

            Selection.activeGameObject = instance;
            EditorGUIUtility.PingObject(instance);
        }
    }
}
