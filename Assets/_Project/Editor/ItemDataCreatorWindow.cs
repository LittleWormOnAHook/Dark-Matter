using UnityEngine;
using UnityEditor;
using Project.Data;
using Project.EditorTools;
using Project.Interaction;

public class ItemDataCreatorWindow : EditorWindow
{
    private string itemName = "New Item";
    private int maxStack = 64;
    private float energyRestore = 0f;
    private float staminaRestore = 0f;
    private float oxygenRestore = 0f;
    private bool isAcInfused = false;
    private int acValue = 0;

    private GameObject worldPrefabTemplate;

    private bool addResourceNode = true;
    private bool addCraftingComponent = false;
    private GameObject gatherVFXPrefab;

    [MenuItem(SurvivalPioneerEditorMenus.ItemDataCreator, false, 0)]
    public static void ShowWindow()
    {
        GetWindow<ItemDataCreatorWindow>("Item Data Creator").minSize = new Vector2(450, 620);
    }

    private void OnGUI()
    {
        GUILayout.Label("Create New Item + Prefab", EditorStyles.boldLabel);
        GUILayout.Space(10);

        itemName = EditorGUILayout.TextField("Item Name", itemName);
        maxStack = EditorGUILayout.IntField("Max Stack", maxStack);

        GUILayout.Space(10);
        GUILayout.Label("Survival Restore", EditorStyles.boldLabel);
        energyRestore = EditorGUILayout.FloatField("Energy Restore", energyRestore);
        staminaRestore = EditorGUILayout.FloatField("Stamina Restore", staminaRestore);
        oxygenRestore = EditorGUILayout.FloatField("Oxygen Restore (display sec)", oxygenRestore);

        GUILayout.Space(10);
        GUILayout.Label("Currency (legacy fields)", EditorStyles.boldLabel);
        isAcInfused = EditorGUILayout.Toggle("Grants AC On Pickup", isAcInfused);
        if (isAcInfused)
            acValue = EditorGUILayout.IntField("AC Value", acValue);

        GUILayout.Space(15);
        GUILayout.Label("World Prefab", EditorStyles.boldLabel);
        worldPrefabTemplate = (GameObject)EditorGUILayout.ObjectField("World Prefab", worldPrefabTemplate, typeof(GameObject), false);

        GUILayout.Space(10);
        GUILayout.Label("Auto Components", EditorStyles.boldLabel);
        addResourceNode = EditorGUILayout.Toggle("Add ResourceNode (Gatherable)", addResourceNode);
        addCraftingComponent = EditorGUILayout.Toggle("Add Crafting Component", addCraftingComponent);

        GUILayout.Space(10);
        gatherVFXPrefab = (GameObject)EditorGUILayout.ObjectField("Gather VFX (Optional)", gatherVFXPrefab, typeof(GameObject), false);

        GUILayout.Space(20);

        if (GUILayout.Button("Create ItemData + Prefab", GUILayout.Height(50)))
        {
            CreateFullItem();
        }
    }

    private void CreateFullItem()
    {
        if (string.IsNullOrEmpty(itemName))
        {
            EditorUtility.DisplayDialog("Error", "Item Name is required!", "OK");
            return;
        }

        ItemData newItem = ScriptableObject.CreateInstance<ItemData>();
        newItem.itemName = itemName;
        newItem.maxStack = maxStack;
        newItem.energyRestore = energyRestore;
        newItem.staminaRestore = staminaRestore;
        newItem.oxygenRestore = oxygenRestore;
        newItem.isAcInfused = isAcInfused;
        newItem.acValue = acValue;

        string folder = addResourceNode ? ProjectAssetPaths.ItemsResources : ProjectAssetPaths.ItemsConsumables;
        CraftingEditorUtility.EnsureFolder(folder);
        string dataPath = $"{folder}/{itemName}.asset";
        AssetDatabase.CreateAsset(newItem, dataPath);

        if (worldPrefabTemplate != null)
        {
            GameObject instance = Instantiate(worldPrefabTemplate);
            instance.name = itemName + "_World";

            if (addResourceNode)
            {
                ResourceNode rn = instance.AddComponent<ResourceNode>();
                rn.resourceItem = newItem;
            }

            if (addCraftingComponent)
            {
                instance.AddComponent<BoxCollider>();
            }

            string prefabPath = $"{ProjectAssetPaths.PrefabsItemsWorld}/{itemName}_World.prefab";
            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            DestroyImmediate(instance);

            newItem.worldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            EditorUtility.SetDirty(newItem);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Success", 
            $"Item '{itemName}' created successfully!\n\n" +
            $"ItemData: {dataPath}\n" +
            $"Prefab: {(worldPrefabTemplate != null ? "Created" : "None")}", 
            "OK");

        itemName = "New Item";
        worldPrefabTemplate = null;
    }
}
