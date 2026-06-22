using System.Collections.Generic;
using Project.Data;
using UnityEngine;

namespace Project.Crafting
{
    [CreateAssetMenu(menuName = "Project/Crafting/Recipe Definition", fileName = "NewRecipe")]
    public class RecipeDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string recipeId;
        public string displayName;
        [TextArea(2, 4)]
        public string description;
        [Tooltip("Icon shown in crafting and scroll slots. Falls back to the output item icon when empty.")]
        public Sprite icon;

        [Header("Station")]
        public CraftingStationType stationType = CraftingStationType.Cooking;

        [Header("Ingredients")]
        public List<RecipeIngredient> ingredients = new List<RecipeIngredient>();

        [Header("Output")]
        public ItemData outputItem;
        public int outputAmount = 1;

        public string ResolvedId => string.IsNullOrEmpty(recipeId) ? name : recipeId;

        public Sprite DisplayIcon => icon != null ? icon : (outputItem != null ? outputItem.icon : null);
    }
}
