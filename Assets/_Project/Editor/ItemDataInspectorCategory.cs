using Project.Data;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Inspector / prune category for <see cref="ItemData"/>. Driven by itemType, flags, and folder.
    /// </summary>
    public enum ItemDataInspectorCategory
    {
        ThrowableConsumable,
        HealConsumable,
        GenericConsumable,
        Ammo,
        RangedWeapon,
        MiningTool,
        MeleeWeapon,
        OpticsTool,
        GenericTool,
        Resource,
        Component,
        Module,
        Operations,
        Vehicle,
        Quest
    }

    public static class ItemDataInspectorCategoryResolver
    {
        public static ItemDataInspectorCategory Resolve(ItemData item)
        {
            if (item == null)
                return ItemDataInspectorCategory.GenericConsumable;

            string path = AssetDatabase.GetAssetPath(item) ?? string.Empty;
            path = path.Replace('\\', '/');

            if (item.unlocksInventoryStorageRow || PathContains(path, "/Modules/"))
                return ItemDataInspectorCategory.Module;

            if (PathContains(path, "/Operations/"))
                return ItemDataInspectorCategory.Operations;

            if (item.componentCategory != ComponentCategory.None || PathContains(path, "/Components/"))
                return ItemDataInspectorCategory.Component;

            switch (item.itemType)
            {
                case ItemType.Ammo:
                    return ItemDataInspectorCategory.Ammo;

                case ItemType.MeleeWeapon:
                    return ItemDataInspectorCategory.MeleeWeapon;

                case ItemType.RangedWeapon:
                    return item.isMiningTool
                        ? ItemDataInspectorCategory.MiningTool
                        : ItemDataInspectorCategory.RangedWeapon;

                case ItemType.Tool:
                    return item.IsOpticsTool
                        ? ItemDataInspectorCategory.OpticsTool
                        : ItemDataInspectorCategory.GenericTool;

                case ItemType.Vehicle:
                    return ItemDataInspectorCategory.Vehicle;

                case ItemType.Quest:
                    return ItemDataInspectorCategory.Quest;

                case ItemType.Resource:
                    return ItemDataInspectorCategory.Resource;

                case ItemType.Consumable:
                    if (IsThrowableConsumable(item, path))
                        return ItemDataInspectorCategory.ThrowableConsumable;
                    if (HasAnyRestore(item))
                        return ItemDataInspectorCategory.HealConsumable;
                    return ItemDataInspectorCategory.GenericConsumable;

                default:
                    return ItemDataInspectorCategory.GenericConsumable;
            }
        }

        public static bool IsThrowableConsumable(ItemData item, string assetPath = null)
        {
            if (item == null || item.itemType != ItemType.Consumable)
                return false;
            if (HasAnyRestore(item))
                return false;

            string path = assetPath;
            if (string.IsNullOrEmpty(path))
                path = AssetDatabase.GetAssetPath(item) ?? string.Empty;
            path = path.Replace('\\', '/');

            if (PathContains(path, "/Throwables/"))
                return true;

            if (ContainsIgnoreCase(item.itemName, "Grenade")
                || ContainsIgnoreCase(item.name, "Grenade")
                || ContainsIgnoreCase(item.StableItemId, "Grenade"))
                return true;

            return false;
        }

        public static bool HasAnyRestore(ItemData item)
        {
            if (item == null)
                return false;
            return item.healthRestore > 0f
                || item.energyRestore > 0f
                || item.staminaRestore > 0f
                || item.oxygenRestore > 0f;
        }

        private static bool PathContains(string path, string fragment)
        {
            return !string.IsNullOrEmpty(path)
                && path.IndexOf(fragment, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool ContainsIgnoreCase(string value, string fragment)
        {
            return !string.IsNullOrEmpty(value)
                && value.IndexOf(fragment, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
