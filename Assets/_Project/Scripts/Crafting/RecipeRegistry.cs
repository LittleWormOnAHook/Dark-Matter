using System.Collections.Generic;
using UnityEngine;

namespace Project.Crafting
{
    [CreateAssetMenu(menuName = "Project/Crafting/Blueprint Registry", fileName = "BlueprintRegistry")]
    public class RecipeRegistry : ScriptableObject
    {
        private static RecipeRegistry cached;

        [SerializeField] private RecipeDefinition[] recipes;

        public static RecipeRegistry Instance
        {
            get
            {
                if (cached == null)
                {
                    cached = Resources.Load<RecipeRegistry>("Crafting/BlueprintRegistry");
                    if (cached == null)
                        cached = Resources.Load<RecipeRegistry>("Crafting/RecipeRegistry");
                }

                return cached;
            }
        }

        public static IReadOnlyList<RecipeDefinition> GetAllBlueprints()
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

        /// <summary>Obsolete alias for <see cref="GetAllBlueprints"/>.</summary>
        public static IReadOnlyList<RecipeDefinition> GetAllRecipes() => GetAllBlueprints();

        public static RecipeDefinition Resolve(string recipeId)
        {
            if (string.IsNullOrEmpty(recipeId))
                return null;

            foreach (RecipeDefinition recipe in GetAllBlueprints())
            {
                if (recipe == null)
                    continue;

                if (recipe.ResolvedId == recipeId)
                    return recipe;
            }

            // Fallback: match asset name when serialized id drifted after Recipes→Blueprints rename.
            foreach (RecipeDefinition recipe in GetAllBlueprints())
            {
                if (recipe != null && recipe.name == recipeId)
                    return recipe;
            }

            return null;
        }
    }
}
