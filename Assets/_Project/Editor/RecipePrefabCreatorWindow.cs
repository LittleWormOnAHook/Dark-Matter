using Project.Crafting;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Builds world recipe-scroll pickup prefabs (RecipePickup_*.prefab) for existing recipes.
    /// </summary>
    public class RecipePrefabCreatorWindow : EditorWindow
    {
        private RecipeDefinition[] recipeAssets = System.Array.Empty<RecipeDefinition>();
        private int selectedRecipeIndex;
        private string recipeId = string.Empty;

        private GameObject pickupVisualTemplate;
        private float pickupInteractRange = 2.5f;
        private Vector3 pickupColliderSize = new Vector3(0.5f, 0.5f, 0.5f);
        private bool autoFitPickupCollider = true;

        private Vector2 listScroll;

        [MenuItem(SurvivalPioneerEditorMenus.RecipePrefabCreator, false, 14)]
        public static void Open()
        {
            GetWindow<RecipePrefabCreatorWindow>("Recipe Prefab Creator").minSize = new Vector2(520f, 480f);
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
            if (selectedRecipeIndex >= recipeAssets.Length)
                selectedRecipeIndex = recipeAssets.Length > 0 ? 0 : -1;

            if (selectedRecipeIndex >= 0 && recipeAssets[selectedRecipeIndex] != null)
                recipeId = recipeAssets[selectedRecipeIndex].ResolvedId;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Recipe Prefab Creator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                $"Creates world pickup prefabs at {CraftingEditorUtility.CraftingPrefabsFolder}/RecipePickup_<id>.prefab. " +
                "Link each prefab to an existing RecipeDefinition by recipe id.",
                MessageType.Info);
            EditorGUILayout.Space(6f);

            DrawRecipePicker();
            EditorGUILayout.Space(8f);
            DrawPickupSettings();
            EditorGUILayout.Space(12f);
            DrawActions();
        }

        private void DrawRecipePicker()
        {
            EditorGUILayout.LabelField("Recipe", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh Recipes", GUILayout.Width(120f)))
                RefreshLists();

            using (new EditorGUI.DisabledScope(recipeAssets.Length == 0))
            {
                if (GUILayout.Button("Create Missing Prefabs", GUILayout.Width(160f)))
                    CreateMissingPrefabs();
            }
            EditorGUILayout.EndHorizontal();

            listScroll = EditorGUILayout.BeginScrollView(listScroll, GUILayout.Height(Mathf.Min(160f, recipeAssets.Length * 24f + 8f)));
            for (int i = 0; i < recipeAssets.Length; i++)
            {
                RecipeDefinition recipe = recipeAssets[i];
                if (recipe == null)
                    continue;

                string label = string.IsNullOrEmpty(recipe.displayName) ? recipe.name : $"{recipe.displayName} ({recipe.ResolvedId})";
                bool hasPrefab = PrefabExists(recipe.ResolvedId);
                if (!hasPrefab)
                    label += "  [no prefab]";

                bool selected = i == selectedRecipeIndex;
                if (GUILayout.Toggle(selected, label, "Button"))
                {
                    if (selectedRecipeIndex != i)
                    {
                        selectedRecipeIndex = i;
                        recipeId = recipe.ResolvedId;
                    }
                }
            }
            EditorGUILayout.EndScrollView();

            recipeId = EditorGUILayout.TextField("Recipe Id", recipeId);

            if (!string.IsNullOrWhiteSpace(recipeId))
            {
                string path = CraftingEditorUtility.GetRecipePickupPrefabPath(recipeId.Trim());
                bool exists = !string.IsNullOrEmpty(path) && AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
                EditorGUILayout.LabelField("Output", exists ? path : $"{path} (new)");
            }
        }

        private void DrawPickupSettings()
        {
            EditorGUILayout.LabelField("Pickup Visual", EditorStyles.boldLabel);

            pickupVisualTemplate = (GameObject)EditorGUILayout.ObjectField(
                "Visual Template",
                pickupVisualTemplate,
                typeof(GameObject),
                false);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Open Book", GUILayout.Width(100f)))
                pickupVisualTemplate = CraftingEditorUtility.LoadDefaultBookVisual();
            if (GUILayout.Button("Crafting Book", GUILayout.Width(110f)))
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
        }

        private void DrawActions()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create / Overwrite Prefab", GUILayout.Height(36f)))
                SavePickupPrefab();

            if (GUILayout.Button("Place In Scene", GUILayout.Height(36f)))
                PlacePickupInScene();
            EditorGUILayout.EndHorizontal();
        }

        private void UseSelectedVisualTemplate()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("Recipe Prefab Creator", "Select a mesh or prefab in the Hierarchy or Project window.", "OK");
                return;
            }

            GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(selected);
            pickupVisualTemplate = source != null ? source : selected;
            Repaint();
        }

        private void SavePickupPrefab()
        {
            if (string.IsNullOrWhiteSpace(recipeId))
            {
                EditorUtility.DisplayDialog("Recipe Prefab Creator", "Select or enter a recipe id.", "OK");
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
            EditorUtility.DisplayDialog(
                "Recipe Prefab Creator",
                $"Saved pickup prefab to\n{AssetDatabase.GetAssetPath(prefab)}",
                "OK");
        }

        private void PlacePickupInScene()
        {
            if (string.IsNullOrWhiteSpace(recipeId))
            {
                EditorUtility.DisplayDialog("Recipe Prefab Creator", "Select or enter a recipe id.", "OK");
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
                EditorUtility.DisplayDialog("Recipe Prefab Creator", "Could not place recipe pickup.", "OK");
                return;
            }

            Selection.activeGameObject = instance;
            EditorGUIUtility.PingObject(instance);
        }

        private void CreateMissingPrefabs()
        {
            int created = 0;
            for (int i = 0; i < recipeAssets.Length; i++)
            {
                RecipeDefinition recipe = recipeAssets[i];
                if (recipe == null || string.IsNullOrWhiteSpace(recipe.ResolvedId))
                    continue;

                if (PrefabExists(recipe.ResolvedId))
                    continue;

                GameObject prefab = CraftingEditorUtility.CreateRecipePickupPrefab(
                    recipe.ResolvedId,
                    pickupVisualTemplate,
                    pickupInteractRange,
                    pickupColliderSize,
                    autoFitPickupCollider,
                    confirmOverwrite: false);

                if (prefab != null)
                    created++;
            }

            EditorUtility.DisplayDialog(
                "Recipe Prefab Creator",
                created > 0 ? $"Created {created} missing recipe pickup prefab(s)." : "All recipes already have pickup prefabs.",
                "OK");
            RefreshLists();
        }

        private static bool PrefabExists(string recipeId)
        {
            string path = CraftingEditorUtility.GetRecipePickupPrefabPath(recipeId);
            return !string.IsNullOrEmpty(path) && AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
        }
    }
}
