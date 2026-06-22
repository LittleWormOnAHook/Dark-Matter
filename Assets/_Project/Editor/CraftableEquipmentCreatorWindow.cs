using System.Collections.Generic;
using Project.Crafting;
using Project.Data;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Creates recipes whose output is an existing weapon or tool ItemData.
    /// </summary>
    public class CraftableEquipmentCreatorWindow : EditorWindow
    {
        private ItemData outputItem;
        private string recipeId = "craftable_equipment";
        private string displayName = "Craftable Equipment";
        private string description = string.Empty;
        private CraftingStationType stationType = CraftingStationType.Workbench;
        private int outputAmount = 1;
        private List<RecipeIngredient> ingredients = new List<RecipeIngredient>();
        private bool addToRecipeRegistry = true;

        private ItemData[] itemOptions = System.Array.Empty<ItemData>();

        [MenuItem(SurvivalPioneerEditorMenus.Crafting + "Craftable Equipment Recipe Creator")]
        public static void Open()
        {
            GetWindow<CraftableEquipmentCreatorWindow>("Craftable Equipment").minSize = new Vector2(460f, 620f);
        }

        private void OnEnable()
        {
            RefreshItemOptions();
        }

        private void RefreshItemOptions()
        {
            itemOptions = CraftingEditorUtility.LoadAllItems();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Craftable Equipment Recipe Creator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Attach a crafting recipe to an existing weapon or tool ItemData. " +
                "Create the equipment first with Content > Equipment Item Creator if needed.",
                MessageType.Info);

            EditorGUILayout.Space(8f);
            outputItem = (ItemData)EditorGUILayout.ObjectField("Output Equipment", outputItem, typeof(ItemData), false);

            if (outputItem != null && outputItem.itemType != ItemType.MeleeWeapon && outputItem.itemType != ItemType.Tool)
                EditorGUILayout.HelpBox("Output should be a melee weapon or tool.", MessageType.Warning);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Use Selected ItemData", GUILayout.Width(170f)))
                UseSelectedItemData();
            if (GUILayout.Button("Open Equipment Item Creator", GUILayout.Width(210f)))
                EditorApplication.ExecuteMenuItem(SurvivalPioneerEditorMenus.Content + "Equipment Item Creator");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8f);
            recipeId = EditorGUILayout.TextField("Recipe Id", recipeId);
            displayName = EditorGUILayout.TextField("Display Name", displayName);
            description = EditorGUILayout.TextArea(description, GUILayout.MinHeight(48f));
            stationType = (CraftingStationType)EditorGUILayout.EnumPopup("Station", stationType);
            outputAmount = Mathf.Max(1, EditorGUILayout.IntField("Output Amount", outputAmount));

            EditorGUILayout.Space(8f);
            CraftingEditorUtility.DrawIngredientListEditor(ref ingredients, itemOptions);
            addToRecipeRegistry = EditorGUILayout.Toggle("Add To Recipe Registry", addToRecipeRegistry);

            EditorGUILayout.Space(16f);
            using (new EditorGUI.DisabledScope(!CanCreate()))
            {
                if (GUILayout.Button("Create Equipment Recipe", GUILayout.Height(42f)))
                    CreateRecipe();
            }
        }

        private void UseSelectedItemData()
        {
            ItemData selected = Selection.activeObject as ItemData;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("Craftable Equipment", "Select an ItemData asset in the Project window.", "OK");
                return;
            }

            outputItem = selected;
            displayName = $"Craft {selected.itemName}";
            recipeId = CraftingEditorUtility.SanitizeAssetName(selected.itemName).ToLowerInvariant();
            Repaint();
        }

        private bool CanCreate()
        {
            return outputItem != null && !string.IsNullOrWhiteSpace(recipeId) && !string.IsNullOrWhiteSpace(displayName);
        }

        private void CreateRecipe()
        {
            string safeFileName = CraftingEditorUtility.SanitizeAssetName(recipeId);
            if (string.IsNullOrEmpty(safeFileName))
            {
                EditorUtility.DisplayDialog("Craftable Equipment", "Recipe id is invalid.", "OK");
                return;
            }

            string path = $"{CraftingEditorUtility.RecipesFolder}/{safeFileName}.asset";
            if (AssetDatabase.LoadAssetAtPath<RecipeDefinition>(path) != null &&
                !EditorUtility.DisplayDialog("Craftable Equipment", $"Recipe asset '{safeFileName}' already exists. Overwrite?", "Overwrite", "Cancel"))
            {
                return;
            }

            RecipeDefinition draft = ScriptableObject.CreateInstance<RecipeDefinition>();
            draft.recipeId = recipeId.Trim();
            draft.displayName = displayName.Trim();
            draft.description = description;
            draft.stationType = stationType;
            draft.outputItem = outputItem;
            draft.outputAmount = outputAmount;
            draft.icon = outputItem != null ? outputItem.icon : null;
            draft.ingredients = new List<RecipeIngredient>(ingredients);

            RecipeDefinition saved = CraftingEditorUtility.SaveRecipeAsset(draft, safeFileName);
            if (saved == null)
            {
                EditorUtility.DisplayDialog("Craftable Equipment", "Failed to save recipe asset.", "OK");
                return;
            }

            if (addToRecipeRegistry)
                CraftingEditorUtility.AddRecipeToRegistry(saved);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshItemOptions();

            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            EditorUtility.DisplayDialog("Craftable Equipment", $"Created recipe '{saved.displayName}'.", "OK");
        }
    }
}
