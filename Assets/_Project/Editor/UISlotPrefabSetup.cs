using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using Project.EditorTools;
using Project.UI;

public class UISlotPrefabSetup : EditorWindow
{
    [MenuItem(SurvivalPioneerEditorMenus.Ui + "Inventory Slot Prefab", false, 20)]
    public static void CreateSlotPrefab()
    {
        // Create the Slot GameObject
        GameObject slotObj = new GameObject("InventorySlot");
        
        // RectTransform
        RectTransform rt = slotObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(80, 80);

        // Background Image
        Image bgImage = slotObj.AddComponent<Image>();
        bgImage.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);

        // Icon Image
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(slotObj.transform);
        RectTransform iconRT = iconObj.AddComponent<RectTransform>();
        iconRT.sizeDelta = new Vector2(65, 65);
        Image iconImage = iconObj.AddComponent<Image>();
        iconImage.color = Color.white;

        // Amount Text
        GameObject textObj = new GameObject("Amount");
        textObj.transform.SetParent(slotObj.transform);
        RectTransform textRT = textObj.AddComponent<RectTransform>();
        textRT.anchorMin = new Vector2(1, 0);
        textRT.anchorMax = new Vector2(1, 0);
        textRT.anchoredPosition = new Vector2(-6, 6);
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "";
        tmp.fontSize = 18;
        tmp.alignment = TextAlignmentOptions.Right;
        tmp.color = Color.white;

        // Add InventorySlotUI component
        InventorySlotUI slotUI = slotObj.AddComponent<InventorySlotUI>();
        slotUI.iconImage = iconImage;
        slotUI.amountText = tmp;

        // Save as Prefab
        string folderPath = "Assets/_Project/Prefabs/UI";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "UI");
        }

        string prefabPath = folderPath + "/InventorySlot.prefab";
        PrefabUtility.SaveAsPrefabAsset(slotObj, prefabPath);

        DestroyImmediate(slotObj);

        Debug.Log("✅ InventorySlot.prefab created successfully at: " + prefabPath);
    }
}