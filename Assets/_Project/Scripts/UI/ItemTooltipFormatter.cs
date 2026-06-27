using System.Text;
using Project.Crafting;
using Project.Data;
using Project.Inventory;
using UnityEngine;

namespace Project.UI
{
    public static class ItemTooltipFormatter
    {
        public static string BuildTitle(ItemData item)
        {
            return item == null ? string.Empty : item.itemName;
        }

        public static string BuildBody(ItemData item, int amount)
        {
            if (item == null)
                return string.Empty;

            StringBuilder text = new StringBuilder();

            text.AppendLine(FormatTypeLine(item));
            text.AppendLine($"<color=#A0A8B8>ID:</color> {item.name}");

            if (amount > 1 || item.maxStack > 1)
                text.AppendLine($"<color=#A0A8B8>Stack:</color> {amount} / {item.maxStack}");

            AppendConsumableLines(text, item);
            AppendWeaponLines(text, item);
            AppendToolLines(text, item);
            AppendAcLine(text, item);
            AppendCraftedItemLine(text, item);

            if (!string.IsNullOrWhiteSpace(item.tooltipDescription))
            {
                text.AppendLine();
                text.Append(item.tooltipDescription.Trim());
            }

            return text.ToString().TrimEnd();
        }

        private static string FormatTypeLine(ItemData item)
        {
            string typeLabel = item.itemType switch
            {
                ItemType.Consumable => "Consumable",
                ItemType.Resource => "Resource",
                ItemType.MeleeWeapon => "Melee Weapon",
                ItemType.Tool => "Tool",
                ItemType.Quest => "Quest Item",
                _ => item.itemType.ToString()
            };

            string color = item.itemType switch
            {
                ItemType.MeleeWeapon => "#E8C547",
                ItemType.Tool => "#6EC1FF",
                ItemType.Consumable => "#7DDA7D",
                ItemType.Resource => "#C8A2FF",
                ItemType.Quest => "#FF9F6E",
                _ => "#D0D4DC"
            };

            return $"<color={color}><b>{typeLabel}</b></color>";
        }

        private static void AppendConsumableLines(StringBuilder text, ItemData item)
        {
            if (!item.IsConsumable)
                return;

            text.AppendLine("<color=#A0A8B8>Restores:</color>");
            AppendRestoreLine(text, "Health", item.healthRestore, "#FF7070");
            AppendRestoreLine(text, "Energy", item.energyRestore, "#E8A045");
            AppendRestoreLine(text, "Stamina", item.staminaRestore, "#B6E067");
            AppendOxygenRestoreLine(text, item.oxygenRestore);
        }

        private static void AppendOxygenRestoreLine(StringBuilder text, float oxygenRestore)
        {
            if (oxygenRestore <= 0f)
                return;

            int totalSeconds = Mathf.CeilToInt(oxygenRestore);
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            text.AppendLine($"  <color=#6EC1FF>+{minutes:00}:{seconds:00} Oxygen</color>");
        }

        private static void AppendRestoreLine(StringBuilder text, string label, float value, string color)
        {
            if (value <= 0f)
                return;

            text.AppendLine($"  <color={color}>+{Mathf.RoundToInt(value)} {label}</color>");
        }

        private static void AppendWeaponLines(StringBuilder text, ItemData item)
        {
            if (item.itemType != ItemType.MeleeWeapon)
                return;

            text.AppendLine("<color=#A0A8B8>Combat:</color>");
            text.AppendLine($"  Grip: {(item.IsTwoHanded ? "Two-Handed" : "One-Handed")}");
            text.AppendLine($"  Damage: {Mathf.RoundToInt(item.meleeDamage)}-{Mathf.RoundToInt(item.meleeDamage + item.meleeDamageRandomRange)}");
            text.AppendLine($"  Crit Multiplier: x{item.criticalDamageMultiplier:0.#}");
            text.AppendLine($"  Range: {item.meleeRange:0.#}m");
            text.AppendLine($"  Cooldown: {item.meleeCooldown:0.##}s");
            text.AppendLine($"  Gather Power: {item.gatherPower}");
        }

        private static void AppendToolLines(StringBuilder text, ItemData item)
        {
            if (item.itemType != ItemType.Tool)
                return;

            text.AppendLine("<color=#A0A8B8>Tool:</color>");
            if (item.toolType != ToolType.None)
                text.AppendLine($"  Type: {item.toolType}");

            text.AppendLine($"  Range: {item.toolRange:0.#}m");

            if (item.toolType == ToolType.Scanner)
                text.AppendLine($"  Scan Range: {item.scanRange:0.#}m");

            if (item.toolType == ToolType.Binoculars)
                text.AppendLine($"  Zoom FOV: {item.opticsZoomFov:0.#}°");

            if (item.IsOpticsTool)
            {
                string keyHint = item.toolType == ToolType.Scanner ? "[N]" : "[B]";
                text.AppendLine($"  Toolbar: {keyHint} Use tool  |  [RMB] Toggle optics");
                text.AppendLine("  [Scroll] Zoom while optics are open");
            }
        }

        private static void AppendAcLine(StringBuilder text, ItemData item)
        {
            if (!item.isPiInfused || item.piValue <= 0)
                return;

            text.AppendLine($"<color=#FFD966>AC Value: {item.piValue}</color>");
        }

        private static void AppendCraftedItemLine(StringBuilder text, ItemData item)
        {
            string craftedLine = RecipeTooltipFormatter.BuildCraftedItemLine(item);
            if (string.IsNullOrEmpty(craftedLine))
                return;

            text.AppendLine();
            text.AppendLine(craftedLine);
        }
    }

    public static class RecipeTooltipFormatter
    {
        public static string BuildTitle(RecipeDefinition recipe)
        {
            if (recipe == null)
                return string.Empty;

            return string.IsNullOrEmpty(recipe.displayName) ? recipe.ResolvedId : recipe.displayName;
        }

        public static string BuildBody(RecipeDefinition recipe, InventorySystem inventory, bool pendingScroll = false)
        {
            if (recipe == null)
                return string.Empty;

            StringBuilder text = new StringBuilder();
            string stationLabel = recipe.stationType == CraftingStationType.Cooking ? "Cooking" : "Workbench";
            text.AppendLine($"<color=#C8A2FF><b>{stationLabel} Recipe</b></color>");

            if (pendingScroll)
                text.AppendLine("<color=#FF9F6E>Right-click to learn</color>");

            if (!string.IsNullOrWhiteSpace(recipe.description))
            {
                text.AppendLine();
                text.AppendLine(recipe.description.Trim());
            }

            if (recipe.ingredients != null && recipe.ingredients.Count > 0)
            {
                text.AppendLine();
                text.AppendLine("<color=#A0A8B8>Ingredients:</color>");
                for (int i = 0; i < recipe.ingredients.Count; i++)
                {
                    RecipeIngredient ingredient = recipe.ingredients[i];
                    if (ingredient == null || ingredient.item == null)
                        continue;

                    int have = inventory != null ? inventory.CountItem(ingredient.item) : 0;
                    string color = have >= ingredient.amount ? "#7DDA7D" : "#FF9F6E";
                    text.AppendLine($"  <color={color}>{ingredient.item.itemName} {have}/{ingredient.amount}</color>");
                }
            }

            if (recipe.outputItem != null)
            {
                text.AppendLine();
                text.AppendLine("<color=#A0A8B8>Creates:</color>");
                text.AppendLine($"  {recipe.outputAmount}x {recipe.outputItem.itemName}");
                AppendItemEffectSummary(text, recipe.outputItem);
                text.AppendLine("<color=#8890A0><i>Left-click to craft when ingredients are ready.</i></color>");
            }

            return text.ToString().TrimEnd();
        }

        public static string BuildScrollBody(RecipeDefinition recipe)
        {
            return BuildBody(recipe, null, pendingScroll: true);
        }

        public static string BuildCraftedItemLine(ItemData item)
        {
            RecipeDefinition recipe = FindRecipeForOutput(item);
            if (recipe == null)
                return string.Empty;

            bool cooking = recipe.stationType == CraftingStationType.Cooking;
            string stationLabel = cooking ? "Cooked" : "Crafted";
            string stationName = cooking ? "Cooking Pot" : "Workbench";
            return $"<color=#A0A8B8>{stationLabel} at {stationName}:</color> {BuildTitle(recipe)}";
        }

        public static RecipeDefinition FindRecipeForOutput(ItemData item)
        {
            if (item == null)
                return null;

            foreach (RecipeDefinition recipe in RecipeRegistry.GetAllRecipes())
            {
                if (recipe?.outputItem == item)
                    return recipe;
            }

            return null;
        }

        private static void AppendItemEffectSummary(StringBuilder text, ItemData item)
        {
            if (item == null || !item.IsConsumable)
                return;

            bool hasRestore = item.healthRestore > 0f || item.energyRestore > 0f
                || item.staminaRestore > 0f || item.oxygenRestore > 0f;
            if (!hasRestore)
                return;

            text.Append("  <color=#8890A0>Restores ");
            bool first = true;
            AppendRestore(text, ref first, "HP", item.healthRestore);
            AppendRestore(text, ref first, "Energy", item.energyRestore);
            AppendRestore(text, ref first, "Stamina", item.staminaRestore);
            AppendOxygenRestore(text, ref first, item.oxygenRestore);
            text.AppendLine("</color>");
        }

        private static void AppendOxygenRestore(StringBuilder text, ref bool first, float oxygenRestore)
        {
            if (oxygenRestore <= 0f)
                return;

            if (!first)
                text.Append(", ");

            int totalSeconds = Mathf.CeilToInt(oxygenRestore);
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            text.Append($"{minutes:00}:{seconds:00} Oxygen");
            first = false;
        }

        private static void AppendRestore(StringBuilder text, ref bool first, string label, float value)
        {
            if (value <= 0f)
                return;

            if (!first)
                text.Append(", ");
            first = false;
            text.Append($"+{Mathf.RoundToInt(value)} {label}");
        }
    }
}
