using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using Project.EditorTools;

public class InventoryPanelSetup : EditorWindow
{
    [MenuItem(SurvivalPioneerEditorMenus.Ui + "Inventory Panel", false, 10)]
    public static void CreateInventoryPanel()
    {
        // Find or create MainCanvas
        GameObject canvasObj = GameObject.Find("MainCanvas");
        if (canvasObj == null)
        {
            Debug.LogError("MainCanvas not found! Run 'Setup Full UI Canvas' first.");
            return;
        }

        // Create InventoryPanel
        GameObject inventoryPanel = new GameObject("InventoryPanel");
        inventoryPanel.transform.SetParent(canvasObj.transform);

        RectTransform panelRT = inventoryPanel.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(1000, 700);
        panelRT.anchoredPosition = Vector2.zero;

        Image panelImage = inventoryPanel.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        // Create Main Inventory Grid inside it
        GameObject mainGrid = new GameObject("MainInventoryGrid");
        mainGrid.transform.SetParent(inventoryPanel.transform);

        RectTransform gridRT = mainGrid.AddComponent<RectTransform>();
        gridRT.anchorMin = new Vector2(0.5f, 0.5f);
        gridRT.anchorMax = new Vector2(0.5f, 0.5f);
        gridRT.sizeDelta = new Vector2(900, 580);
        gridRT.anchoredPosition = new Vector2(0, 20);

        GridLayoutGroup gridLayout = mainGrid.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(80, 80);
        gridLayout.spacing = new Vector2(12, 12);
        gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 8;

        Image gridBg = mainGrid.AddComponent<Image>();
        gridBg.color = new Color(0.2f, 0.2f, 0.2f, 0.6f);

        // Disable by default
        inventoryPanel.SetActive(false);

        Debug.Log("✅ InventoryPanel + MainInventoryGrid created successfully!");
        Selection.activeGameObject = inventoryPanel;
    }
}