using System.Collections.Generic;
using System.Text;
using Project.Crafting;
using Project.Data;
using Project.UI;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    [InitializeOnLoad]
    public static class DMGameIconAutoImport
    {
        static DMGameIconAutoImport()
        {
            EditorApplication.delayCall += DMGameIconRegistryEditor.ImportGameIconsAsSprites;
        }
    }

    public static class DMGameIconRegistryEditor
    {
        private const string GameIconFolder = "Assets/_Project/Resources/UI/Game Icons";

        [MenuItem("Dark Matter Genesis/UI/Game Icons/Import as Sprites")]
        public static void ImportGameIconsAsSprites()
        {
            ImportFolder(GameIconFolder);
        }

        [MenuItem("Dark Matter Genesis/UI/Game Icons/Rewire Item + Recipe Icons")]
        public static void RewireItemAndRecipeIcons()
        {
            ImportFolder(GameIconFolder);

            Dictionary<string, Sprite> byKey = LoadSprites(GameIconFolder);
            if (byKey.Count == 0)
            {
                Debug.LogWarning("Game Icons: no sprites in " + GameIconFolder + ".");
                return;
            }

            int itemsWired = 0;
            ItemData[] items = ItemRegistry.GetAllItems();
            for (int i = 0; i < items.Length; i++)
            {
                ItemData item = items[i];
                if (item == null)
                    continue;

                Sprite found = FindSprite(item.itemName, item.name, byKey, recipe: false);
                if (found == null || item.icon == found)
                    continue;

                Undo.RecordObject(item, "Rewire Game Icon");
                item.icon = found;
                EditorUtility.SetDirty(item);
                itemsWired++;
            }

            int recipesWired = 0;
            string[] recipeGuids = AssetDatabase.FindAssets("t:RecipeDefinition", new[] { "Assets/_Project/Data" });
            for (int i = 0; i < recipeGuids.Length; i++)
            {
                var recipe = AssetDatabase.LoadAssetAtPath<RecipeDefinition>(
                    AssetDatabase.GUIDToAssetPath(recipeGuids[i]));
                if (recipe == null)
                    continue;

                Sprite found = FindSprite(recipe.displayName, recipe.name, byKey, recipe: true);
                if (found == null && recipe.outputItem != null)
                    found = FindSprite(recipe.outputItem.itemName, recipe.outputItem.name, byKey, recipe: false);
                if (found == null || recipe.icon == found)
                    continue;

                Undo.RecordObject(recipe, "Rewire Recipe Game Icon");
                recipe.icon = found;
                EditorUtility.SetDirty(recipe);
                recipesWired++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Game Icons: rewired {itemsWired} items and {recipesWired} recipes from {GameIconFolder}.");
        }

        private static void ImportFolder(string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder))
                return;

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
            int changed = 0;
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (path.EndsWith(".psd", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                    continue;

                bool dirty = importer.textureType != TextureImporterType.Sprite
                    || importer.spriteImportMode != SpriteImportMode.Single
                    || importer.mipmapEnabled
                    || !importer.alphaIsTransparency
                    || importer.npotScale != TextureImporterNPOTScale.None
                    || importer.wrapMode != TextureWrapMode.Clamp
                    || DMTextureImporterEditorUtility.GetSpriteGenerateFallbackPhysicsShape(importer);

                if (!dirty)
                    continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.wrapMode = TextureWrapMode.Clamp;
                DMTextureImporterEditorUtility.SetSpriteGenerateFallbackPhysicsShape(importer, false);
                importer.SaveAndReimport();
                changed++;
            }

            if (changed > 0)
                Debug.Log($"Game Icons: imported {changed} UITK sprites from {folder}.");
        }

        private static Dictionary<string, Sprite> LoadSprites(string folder)
        {
            var byKey = new Dictionary<string, Sprite>();
            if (!AssetDatabase.IsValidFolder(folder))
                return byKey;

            string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { folder });
            for (int i = 0; i < guids.Length; i++)
            {
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (sprite == null)
                    continue;
                string key = Normalize(sprite.name);
                if (key.Length == 0 || byKey.ContainsKey(key))
                    continue;
                byKey[key] = sprite;
            }

            return byKey;
        }

        private static Sprite FindSprite(string primary, string secondary, Dictionary<string, Sprite> byKey, bool recipe)
        {
            string[] candidates = recipe
                ? new[] { Alias(primary, true), Alias(secondary, true), primary, secondary }
                : new[] { primary, secondary, Alias(primary, false), Alias(secondary, false) };

            for (int i = 0; i < candidates.Length; i++)
            {
                string key = Normalize(candidates[i]);
                if (key.Length == 0)
                    continue;
                if (byKey.TryGetValue(key, out Sprite sprite))
                    return sprite;
            }

            return null;
        }

        private static string Alias(string value, bool recipe)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;

            string n = value.Trim();
            if (recipe)
            {
                if (n.IndexOf("forest stew", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return "DM_Blueprint_Forest_Stew";
                if (n.IndexOf("plasma fuel", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return "DM_Blueprint_Plasma_Fuel";
                if (n.IndexOf("bio", System.StringComparison.OrdinalIgnoreCase) >= 0
                    && n.IndexOf("gel", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return "DM_Blueprint_Bio_Gel";
                if (n.IndexOf("storage", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return "Dm_Blueprint_Storage_Module";
                if (n.IndexOf("standard", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return "Dm_Blueprint_Standard_Ammo";
                if (n.IndexOf("pistol", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return "DM_Blueprint_Sci_Fi_Pistol";
                if (n.IndexOf("plasma", System.StringComparison.OrdinalIgnoreCase) >= 0
                    && n.IndexOf("ammo", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return "DM_Blueprint_Plasma_Ammo";
                if (n.IndexOf("ice", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return "DM_Blueprint_Ice_Ammo";
                if (n.IndexOf("fire", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return "DM_Blueprint_Fire_Ammo";
                if (n.IndexOf("electric", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return "DM_Blueprint_Electricity_Ammo";
                if (n.IndexOf("explosive", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return "DM_Blueprint_Explosive_Ammo";
                if (n.IndexOf("ion", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return "DM_Blueprint_Ion_Ammo";
                if (n.IndexOf("resonance", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return "DM_Blueprint_Resonance_Stabilizer";
                if (n.IndexOf("rifle", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return "DM_Blueprint_Survival_Rifle";
                if (n.IndexOf("quora", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return "DM_Blueprint_Quora_Shelter";
                if (n.IndexOf("mushroom", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("grilled", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return "DM_Blueprint_Grilled_Mushroom";
                if (n.IndexOf("medpack", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("herbal", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return "DM_Blueprint_Herbal_Medpack";
                if (n.IndexOf("pimican", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("pemican", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return "DM_Blueprint_Pimican";
            }

            if (n.IndexOf("binocular", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return "Binnoculars";
            if (n.IndexOf("scan", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return "Multi-Purpose Scan Tool";
            if (n.Equals("Standard", System.StringComparison.OrdinalIgnoreCase))
                return "Standard Ammo";
            if (n.IndexOf("laser", System.StringComparison.OrdinalIgnoreCase) >= 0
                && n.IndexOf("ammo", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return "Laser Ammo";
            if (n.Equals("Ice", System.StringComparison.OrdinalIgnoreCase))
                return "Ice";
            if (n.Equals("Fire", System.StringComparison.OrdinalIgnoreCase))
                return "Fire";
            if (n.Equals("Electricity", System.StringComparison.OrdinalIgnoreCase))
                return "Electricity";
            if (n.Equals("Explosive", System.StringComparison.OrdinalIgnoreCase))
                return "Explosive";
            if (n.Equals("Ion", System.StringComparison.OrdinalIgnoreCase))
                return "Ion";
            if (n.IndexOf("resonance", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return "Resonance Stabilizer";
            if (n.IndexOf("pistol", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return "Pistol";
            if (n.IndexOf("grenade", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return "Grenade";
            if (n.IndexOf("hover", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return "Hover Vehicle";
            if (n.IndexOf("mining", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return "Mining Drill";
            if (n.IndexOf("storage", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return "Storage Module";
            if (n.IndexOf("silicate", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return "Silicon";
            if (n.IndexOf("oxygen", System.StringComparison.OrdinalIgnoreCase) >= 0
                && n.IndexOf("mini", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return "Oxygen Mini Tank";
            if (n.IndexOf("fear", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return "Sword Of Fear";
            if (n.IndexOf("2 hander", System.StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("two-handed", System.StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("two handed", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return "Two Handed Sword";
            return n;
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var sb = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (char.IsLetterOrDigit(c))
                    sb.Append(char.ToLowerInvariant(c));
            }

            string key = sb.ToString();
            if (key.StartsWith("dm"))
                key = key.Substring(2);
            return key
                .Replace("binnoculars", "binoculars")
                .Replace("brimestone", "brimstone")
                .Replace("forrest", "forest")
                .Replace("oxygenminitank", "oxygentankmini");
        }
    }
}
