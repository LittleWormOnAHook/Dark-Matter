using System.Collections.Generic;
using System.IO;
using Project.Crafting;
using Project.Data;
using Project.EditorTools;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Workbench recipes for Phase C ranged weapons and gunpowder ammo.
/// </summary>
public static class RangedCraftingSetup
{
    private const string ItemsFolder = ProjectAssetPaths.ItemsData;
    private const string RecipesFolder = ProjectAssetPaths.RecipesData;

    private const string MetalScrapPath = ProjectAssetPaths.ItemsResources + "/metal_scrap.asset";
    private const string ElectronicScrapPath = ProjectAssetPaths.ItemsResources + "/electronic_scrap.asset";

    private const string RifleItemPath = ProjectAssetPaths.ItemsRanged + "/survival_rifle.asset";
    private const string PistolItemPath = ProjectAssetPaths.ItemsRanged + "/sci_fi_pistol.asset";
    private const string GunpowderAmmoPath = ProjectAssetPaths.ItemsAmmo + "/ammo_gunpowder_rounds.asset";

    private static readonly (string id, string file, string name, string desc, (string item, int amount)[] ingredients, string output, int outputAmount)[] RecipeSpecs =
    {
        (
            "craft_survival_rifle",
            "craft_survival_rifle",
            "Survival Rifle",
            "Assemble a two-handed survival rifle from salvaged parts.",
            new[] { ("Metal Scrap", 12), ("Electronic Scrap", 6) },
            "Survival Rifle",
            1),
        (
            "craft_sci_fi_pistol",
            "craft_sci_fi_pistol",
            "Sci-Fi Pistol",
            "Fabricate a compact sci-fi sidearm from scrap.",
            new[] { ("Metal Scrap", 6), ("Electronic Scrap", 4) },
            "Sci-Fi Pistol",
            1),
        (
            "craft_gunpowder_rounds",
            "craft_gunpowder_rounds",
            "Gunpowder Rounds",
            "Press scrap into a batch of ballistic rounds.",
            new[] { ("Metal Scrap", 2), ("Electronic Scrap", 1) },
            "Gunpowder Rounds",
            20)
    };

    [MenuItem(SurvivalPioneerEditorMenus.Combat + "Setup Phase C Ranged Crafting", false, 1)]
    public static void SetupPhaseCRangedCraftingMenu()
    {
        int changes = EnsureRangedCraftingRecipes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        CraftingSetup.SyncRecipeRegistryFromDataFolder();

        EditorUtility.DisplayDialog(
            "Phase C Ranged Crafting",
            changes > 0
                ? $"Created/updated {changes} ranged crafting assets and synced the recipe registry."
                : "Ranged crafting assets are already up to date.",
            "OK");
    }

    public static int EnsureRangedCraftingRecipes()
    {
        EnsureFolder(ProjectAssetPaths.Data + "/Crafting");
        EnsureFolder(RecipesFolder);
        EnsureFolder(ProjectAssetPaths.RecipesWeapons);
        EnsureFolder(ProjectAssetPaths.RecipesAmmo);
        EnsureFolder(ProjectAssetPaths.ItemsResources);
        EnsureFolder(ProjectAssetPaths.ItemsRanged);
        EnsureFolder(ProjectAssetPaths.ItemsAmmo);

        int changes = 0;
        if (EnsureScrapItem("Metal Scrap", MetalScrapPath, ComponentCategory.MetalScrap) != null)
            changes++;
        if (EnsureScrapItem("Electronic Scrap", ElectronicScrapPath, ComponentCategory.ElectronicScrap) != null)
            changes++;

        Dictionary<string, ItemData> items = LoadItemLookup();
        changes += EnsureRecipeAssets(items);
        return changes;
    }

    private static ItemData EnsureScrapItem(string displayName, string path, ComponentCategory category)
    {
        ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
        if (item == null)
        {
            item = ScriptableObject.CreateInstance<ItemData>();
            AssetDatabase.CreateAsset(item, path);
        }

        item.itemName = displayName;
        item.itemType = ItemType.Resource;
        item.componentCategory = category;
        item.maxStack = 99;
        item.tooltipDescription = category == ComponentCategory.MetalScrap
            ? "Salvaged metal used in weapon fabrication."
            : "Recovered electronics used in weapon fabrication.";

        EditorUtility.SetDirty(item);
        WeaponPrefabBuilder.TryRegisterInItemRegistry(item);
        return item;
    }

    private static Dictionary<string, ItemData> LoadItemLookup()
    {
        Dictionary<string, ItemData> lookup = new Dictionary<string, ItemData>();
        string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { ItemsFolder });
        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);
            if (item != null && !lookup.ContainsKey(item.itemName))
                lookup[item.itemName] = item;
        }

        return lookup;
    }

    private static int EnsureRecipeAssets(Dictionary<string, ItemData> items)
    {
        int changes = 0;

        for (int i = 0; i < RecipeSpecs.Length; i++)
        {
            (string id, string file, string name, string desc, (string item, int amount)[] ingredients, string output, int outputAmount) spec =
                RecipeSpecs[i];

            string path = $"{ProjectAssetPaths.RecipesWeapons}/{spec.file}.asset";
            if (spec.id.Contains("gunpowder") || spec.id.Contains("Ammo") || spec.id.Contains("rounds"))
                path = $"{ProjectAssetPaths.RecipesAmmo}/{spec.file}.asset";
            RecipeDefinition recipe = AssetDatabase.LoadAssetAtPath<RecipeDefinition>(path);
            if (recipe == null)
            {
                // Prefer category folder; fall back to legacy flat Recipes path.
                string legacy = $"{RecipesFolder}/{spec.file}.asset";
                recipe = AssetDatabase.LoadAssetAtPath<RecipeDefinition>(legacy);
                if (recipe != null)
                    path = legacy;
            }
            if (recipe == null)
            {
                CraftingEditorUtility.EnsureFolder(System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/'));
                recipe = ScriptableObject.CreateInstance<RecipeDefinition>();
                AssetDatabase.CreateAsset(recipe, path);
                changes++;
            }

            recipe.recipeId = spec.id;
            recipe.displayName = spec.name;
            recipe.description = spec.desc;
            recipe.stationType = CraftingStationType.Workbench;
            recipe.outputAmount = spec.outputAmount;
            recipe.requiredPlayerLevel = spec.id.Contains("rifle") ? 3 : 1;
            recipe.recipeTier = spec.id.Contains("rifle") ? 2 : 1;
            recipe.ingredients = new List<RecipeIngredient>();

            for (int j = 0; j < spec.ingredients.Length; j++)
            {
                (string itemName, int amount) ingredientSpec = spec.ingredients[j];
                if (!items.TryGetValue(ingredientSpec.itemName, out ItemData ingredientItem))
                {
                    Debug.LogWarning(
                        $"RangedCraftingSetup: Missing ingredient item '{ingredientSpec.itemName}' for recipe '{spec.id}'.");
                    continue;
                }

                recipe.ingredients.Add(new RecipeIngredient
                {
                    item = ingredientItem,
                    amount = ingredientSpec.amount
                });
            }

            if (!items.TryGetValue(spec.output, out ItemData outputItem))
            {
                Debug.LogWarning($"RangedCraftingSetup: Missing output item '{spec.output}' for recipe '{spec.id}'.");
            }
            else
            {
                recipe.outputItem = outputItem;
                if (recipe.icon == null)
                    recipe.icon = outputItem.icon;
            }

            EditorUtility.SetDirty(recipe);
        }

        return changes;
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
}
