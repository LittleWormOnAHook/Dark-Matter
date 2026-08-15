using System.Text;
using Project.Crafting;
using Project.Data;
using Project.Inventory;
using Project.Progression;
using Project.Vehicles;
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
            AppendVehicleLines(text, item);
            AppendAcLine(text, item);
            AppendProgressionLines(text, item);
            AppendCraftedItemLine(text, item);

            if (!string.IsNullOrWhiteSpace(item.tooltipDescription))
            {
                text.AppendLine();
                text.Append(SanitizeForTmp(item.tooltipDescription.Trim()));
            }

            return SanitizeForTmp(text.ToString().TrimEnd());
        }

        /// <summary>
        /// Rajdhani SDF lacks Unicode subscripts (e.g. O₂). Map common chemistry glyphs to ASCII.
        /// </summary>
        public static string SanitizeForTmp(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            return value
                .Replace('\u2080', '0')
                .Replace('\u2081', '1')
                .Replace('\u2082', '2')
                .Replace('\u2083', '3')
                .Replace('\u2084', '4')
                .Replace('\u2085', '5')
                .Replace('\u2086', '6')
                .Replace('\u2087', '7')
                .Replace('\u2088', '8')
                .Replace('\u2089', '9');
        }

        private static string FormatTypeLine(ItemData item)
        {
            string typeLabel = item.itemType switch
            {
                ItemType.Consumable => "Consumable",
                ItemType.Resource => "Resource",
                ItemType.MeleeWeapon => "Melee Weapon",
                ItemType.RangedWeapon => "Ranged Weapon",
                ItemType.Ammo => "Ammo",
                ItemType.Tool => "Tool",
                ItemType.Quest => "Quest Item",
                ItemType.Vehicle => "Vehicle",
                _ => item.itemType.ToString()
            };

            string color = item.itemType switch
            {
                ItemType.MeleeWeapon => "#E8C547",
                ItemType.RangedWeapon => "#C02E7A",
                ItemType.Ammo => "#D4A017",
                ItemType.Tool => "#6EC1FF",
                ItemType.Consumable => "#7DDA7D",
                ItemType.Resource => "#C8A2FF",
                ItemType.Quest => "#FF9F6E",
                ItemType.Vehicle => "#6EE7B7",
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
            if (item.itemType == ItemType.MeleeWeapon)
            {
                AppendMeleeWeaponLines(text, item);
                return;
            }

            if (item.IsRangedWeapon)
                AppendRangedWeaponLines(text, item);
        }

        private static void AppendMeleeWeaponLines(StringBuilder text, ItemData item)
        {
            text.AppendLine("<color=#A0A8B8>Base Stats:</color>");
            text.AppendLine($"  Grip: {(item.IsTwoHanded ? "Two-Handed" : "One-Handed")}");
            text.AppendLine($"  Damage: {Mathf.RoundToInt(item.meleeDamage)}-{Mathf.RoundToInt(item.meleeDamage + item.meleeDamageRandomRange)}");
            float effective = item.GetAverageMeleeDamage();
            text.AppendLine($"  <color=#8C7F75>Effective: ~{Mathf.RoundToInt(effective)} (skills + level)</color>");
            text.AppendLine($"  Crit Chance: {item.criticalChance * 100f:0.#}%");
            text.AppendLine($"  Crit Multiplier: x{item.criticalDamageMultiplier:0.#}");
            text.AppendLine($"  Range: {item.meleeRange:0.#}m");
            text.AppendLine($"  Cooldown: {item.meleeCooldown:0.##}s");
            if (item.meleeStaminaCost > 0f)
                text.AppendLine($"  Stamina Cost: {item.meleeStaminaCost:0.#}");
            if (item.meleeKnockback > 0f)
                text.AppendLine($"  Knockback: {item.meleeKnockback:0.#}");
            text.AppendLine($"  Gather Power: {item.gatherPower}");
        }

        private static void AppendRangedWeaponLines(StringBuilder text, ItemData item)
        {
            text.AppendLine("<color=#A0A8B8>Base Stats:</color>");
            text.AppendLine($"  Grip: {(item.weaponGrip == WeaponGrip.TwoHanded ? "Two-Handed" : "One-Handed")}");
            text.AppendLine($"  Damage: {Mathf.RoundToInt(item.rangedDamage)}-{Mathf.RoundToInt(item.rangedDamage + item.rangedDamageRandomRange)}");
            float effective = item.GetAverageRangedDamage();
            text.AppendLine($"  <color=#8C7F75>Effective: ~{Mathf.RoundToInt(effective)} (skills + level)</color>");
            text.AppendLine($"  Accuracy: {item.weaponAccuracy:0.#}");
            float effectiveAcc = item.GetEffectiveAccuracy();
            if (!Mathf.Approximately(effectiveAcc, item.weaponAccuracy))
                text.AppendLine($"  <color=#8C7F75>Effective Acc: {effectiveAcc:0.#}</color>");
            text.AppendLine($"  Spread: {item.projectileSpreadDegrees:0.##}°");
            text.AppendLine($"  Fire Rate: {item.fireRate:0.#}/s");
            text.AppendLine($"  Range: {item.rangedRange:0.#}m");
            text.AppendLine($"  Magazine: {item.magazineSize}");
            if (item.reloadTimeSeconds > 0f)
                text.AppendLine($"  Reload: {item.reloadTimeSeconds:0.##}s");
            if (item.recoilVertical > 0.01f || item.recoilHorizontal > 0.01f)
                text.AppendLine($"  Recoil: {item.recoilVertical:0.##}↑ / ±{item.recoilHorizontal:0.##}");
            text.AppendLine($"  Projectile Speed: {item.projectileSpeed:0.#}");
            text.AppendLine($"  <color=#8C7F75>Ammo modifies speed/spread/VFX/status — not base damage.</color>");
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
                string openHint = item.toolType == ToolType.Scanner
                    ? "[N] Use tool  |  [RMB] Toggle optics"
                    : "[Hold B] Binoculars  |  [B] Blueprints  |  [RMB] Close optics";
                text.AppendLine($"  Toolbar: {openHint}");
                text.AppendLine("  [Scroll] Zoom while optics are open");
            }
        }

        private static void AppendVehicleLines(StringBuilder text, ItemData item)
        {
            if (!item.IsVehicle)
                return;

            text.AppendLine("<color=#A0A8B8>Vehicle:</color>");
            text.AppendLine("  Type: Hovercraft");
            text.AppendLine($"  Fuel Capacity: {Mathf.RoundToInt(HovercraftStorageState.DefaultMaxFuel)}");
            text.AppendLine($"  Fuel Remaining: {Mathf.RoundToInt(HovercraftStorageState.StoredFuel)}");
            text.AppendLine("<color=#8890A0><i>Right-click to Refuel or Deploy.</i></color>");
        }

        private static void AppendAcLine(StringBuilder text, ItemData item)
        {
            if (!item.isAcInfused || item.acValue <= 0)
                return;

            text.AppendLine($"<color=#FFD966>AC Value: {item.acValue}</color>");
        }

        private static void AppendProgressionLines(StringBuilder text, ItemData item)
        {
            int equip = item.requiredLevelToEquip;
            int craft = item.requiredLevelToCraft;
            int use = item.requiredLevelToUse;
            int pickup = item.requiredLevelToPickup;

            bool anyGate = LevelUnlockUtility.IsGateActive(equip)
                || LevelUnlockUtility.IsGateActive(craft)
                || LevelUnlockUtility.IsGateActive(use)
                || LevelUnlockUtility.IsGateActive(pickup);

            if (!anyGate && !item.grantsXp)
                return;

            text.AppendLine();
            text.AppendLine("<color=#A0A8B8>Progression:</color>");
            if (LevelUnlockUtility.IsGateActive(equip))
                text.AppendLine($"  <color=#D4A017>Equip Lv {equip}+</color>");
            if (LevelUnlockUtility.IsGateActive(use))
                text.AppendLine($"  <color=#D4A017>Use Lv {use}+</color>");
            if (LevelUnlockUtility.IsGateActive(craft))
                text.AppendLine($"  <color=#D4A017>Craft Lv {craft}+</color>");
            if (LevelUnlockUtility.IsGateActive(pickup))
                text.AppendLine($"  <color=#D4A017>Pickup Lv {pickup}+</color>");
            if (item.grantsXp && item.xpAmount > 0)
                text.AppendLine($"  <color=#C02E7A>+{item.xpAmount} XP</color>");
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
            text.AppendLine($"<color=#C8A2FF><b>{stationLabel} Blueprint</b></color>");

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
                int craftLevel = LevelUnlockUtility.GetEffectiveCraftRequiredLevel(
                    recipe.requiredPlayerLevel,
                    recipe.outputItem);
                if (craftLevel > 1)
                    text.AppendLine($"  <color=#D4A017>Requires level {craftLevel}</color>");
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
