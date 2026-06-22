using System.Collections.Generic;
using UnityEngine;

namespace Project.Crafting
{
    [CreateAssetMenu(menuName = "Project/Crafting/Recipe Registry", fileName = "RecipeRegistry")]
    public class RecipeRegistry : ScriptableObject
    {
        private static RecipeRegistry cached;

        [SerializeField] private RecipeDefinition[] recipes;

        public static RecipeRegistry Instance
        {
            get
            {
                if (cached == null)
                    cached = Resources.Load<RecipeRegistry>("Crafting/RecipeRegistry");

                return cached;
            }
        }

        public static IReadOnlyList<RecipeDefinition> GetAllRecipes()
        {
            List<RecipeDefinition> result = new List<RecipeDefinition>();

            RecipeRegistry registry = Instance;
            if (registry != null && registry.recipes != null)
            {
                foreach (RecipeDefinition recipe in registry.recipes)
                {
                    if (recipe != null)
                        result.Add(recipe);
                }
            }

            return result;
        }

        public static RecipeDefinition Resolve(string recipeId)
        {
            if (string.IsNullOrEmpty(recipeId))
                return null;

            foreach (RecipeDefinition recipe in GetAllRecipes())
            {
                if (recipe != null && recipe.ResolvedId == recipeId)
                    return recipe;
            }

            return null;
        }
    }
}
