using Project.Crafting;
using Project.Progression;

namespace Project.UI
{
    public static class LevelUnlockRegistry
    {
        public static int GetNextRecipeUnlockLevel(int playerLevel)
        {
            int next = int.MaxValue;
            foreach (RecipeDefinition recipe in RecipeRegistry.GetAllRecipes())
            {
                if (recipe == null || recipe.requiredPlayerLevel <= playerLevel)
                    continue;

                if (recipe.requiredPlayerLevel < next)
                    next = recipe.requiredPlayerLevel;
            }

            return next == int.MaxValue ? -1 : next;
        }

        public static string BuildUnlockSummary(int playerLevel)
        {
            int nextRecipe = GetNextRecipeUnlockLevel(playerLevel);
            if (nextRecipe < 0)
                return "All known recipes unlocked for your level.";

            return $"Next recipe tier unlocks at level {nextRecipe}.";
        }
    }
}
