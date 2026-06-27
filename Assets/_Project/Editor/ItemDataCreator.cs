using UnityEngine;
using UnityEditor;
using Project.Data;
using Project.EditorTools;

public class ItemDataCreator : EditorWindow
{
    [MenuItem(SurvivalPioneerEditorMenus.Content + "Create Starting ItemData Assets")]
    public static void CreateDefaultItems()
    {
        string folderPath = "Assets/_Project/Data/Items";
        
        // Create folder if it doesn't exist
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets/_Project/Data", "Items");
        }

        CreateItem("Wood", 64, 0, 5, 0, false, 0);
        CreateItem("Stone", 64, 0, 0, 0, false, 0);
        CreateItem("Berry", 32, 25, 5, 0, false, 0);
        CreateItem("Pi Crystal", 16, 16, 20, 0, true, 5);
        CreateItem("Pi Shard", 64, 0, 10, 0, true, 1);
        CreateItem("Log", 32, 0, 15, 0, false, 0);
        CreateItem("Leaf", 64, 13, 8, 0, false, 0);

        AssetDatabase.Refresh();
        Debug.Log("✅ 7 ItemData assets created successfully in: " + folderPath);
    }

    private static void CreateItem(string itemName, int maxStack, float energy, float stamina, float oxygen, bool isPiInfused, int piValue)
    {
        ItemData item = ScriptableObject.CreateInstance<ItemData>();
        
        item.itemName = itemName;
        item.maxStack = maxStack;
        item.energyRestore = energy;
        item.staminaRestore = stamina;
        item.oxygenRestore = oxygen;
        item.isPiInfused = isPiInfused;
        item.piValue = piValue;

        string path = $"Assets/_Project/Data/Items/{itemName}.asset";
        AssetDatabase.CreateAsset(item, path);
    }
}