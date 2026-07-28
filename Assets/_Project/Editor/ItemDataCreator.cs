using UnityEngine;
using UnityEditor;
using Project.Data;
using Project.EditorTools;

public class ItemDataCreator : EditorWindow
{
    [MenuItem(SurvivalPioneerEditorMenus.Content + "Create Starting ItemData Assets")]
    public static void CreateDefaultItems()
    {
        string folderPath = ProjectAssetPaths.ItemsConsumables;
        
        // Create folder if it doesn't exist
        CraftingEditorUtility.EnsureFolder(folderPath);

        CreateItem("Wood", 64, 0, 5, 0, false, 0);
        CreateItem("Stone", 64, 0, 0, 0, false, 0);
        CreateItem("Berry", 32, 25, 5, 0, false, 0);
        CreateItem("AC Crystal", 16, 16, 20, 0, true, 5);
        CreateItem("AC Shard", 64, 0, 10, 0, true, 1);
        CreateItem("Log", 32, 0, 15, 0, false, 0);
        CreateItem("Leaf", 64, 13, 8, 0, false, 0);

        AssetDatabase.Refresh();
        Debug.Log("✅ 7 ItemData assets created successfully in: " + folderPath);
    }

    private static void CreateItem(
        string itemName,
        int maxStack,
        float energy,
        float stamina,
        float oxygen,
        bool isAcInfused,
        int acValue)
    {
        ItemData item = ScriptableObject.CreateInstance<ItemData>();
        
        item.itemName = itemName;
        item.maxStack = maxStack;
        item.energyRestore = energy;
        item.staminaRestore = stamina;
        item.oxygenRestore = oxygen;
        item.isAcInfused = isAcInfused;
        item.acValue = acValue;

        string path = $"{ProjectAssetPaths.ItemsConsumables}/{itemName}.asset";
        AssetDatabase.CreateAsset(item, path);
    }
}
