using Project.Data;
using Project.EditorTools;
using UnityEditor;
using UnityEngine;

public class ItemDataCreator : EditorWindow
{
    [MenuItem(SurvivalPioneerEditorMenus.Content + "Create Starting ItemData Assets")]
    public static void CreateDefaultItems()
    {
        CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.ItemsConsumables);
        CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.ItemsResources);

        CreateConsumable("Berry", 32, 0, 5, 0, false, 0);
        CreateConsumable("AC Crystal", 16, 16, 20, 0, true, 5);
        CreateConsumable("AC Shard", 64, 0, 10, 0, true, 1);
        CreateConsumable("Leaf", 64, 13, 8, 0, false, 0);
        CreateResource("Wood", 64);
        CreateResource("Stone", 64);
        CreateResource("Log", 32);

        AssetDatabase.SaveAssets();
        Debug.Log("Starting ItemData assets created under Consumables / Resources.");
    }

    private static void CreateConsumable(
        string itemName,
        int maxStack,
        float energy,
        float stamina,
        float oxygen,
        bool isAcInfused,
        int acValue)
    {
        string path = $"{ProjectAssetPaths.ItemsConsumables}/{itemName}.asset";
        ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
        if (item == null)
        {
            item = ScriptableObject.CreateInstance<ItemData>();
            AssetDatabase.CreateAsset(item, path);
        }

        item.itemName = itemName;
        item.itemType = ItemType.Consumable;
        item.maxStack = maxStack;
        item.energyRestore = energy;
        item.staminaRestore = stamina;
        item.oxygenRestore = oxygen;
        item.isAcInfused = isAcInfused;
        item.acValue = acValue;
        ItemDataPruneUtility.Prune(item, ItemDataInspectorCategory.HealConsumable);
        EditorUtility.SetDirty(item);
    }

    private static void CreateResource(string itemName, int maxStack)
    {
        string path = $"{ProjectAssetPaths.ItemsResources}/{itemName}.asset";
        ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
        if (item == null)
        {
            item = ScriptableObject.CreateInstance<ItemData>();
            AssetDatabase.CreateAsset(item, path);
        }

        item.itemName = itemName;
        item.itemType = ItemType.Resource;
        item.maxStack = maxStack;
        ItemDataPruneUtility.Prune(item, ItemDataInspectorCategory.Resource);
        EditorUtility.SetDirty(item);
    }
}
