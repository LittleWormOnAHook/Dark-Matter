using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using Project.EditorTools;

public class UICanvasSetup : EditorWindow
{
    [MenuItem(SurvivalPioneerEditorMenus.Ui + "Full UI Canvas + Inventory", false, 0)]
    public static void CreateFullUI()
    {
        GameObject canvasObj = GameObject.Find("MainCanvas") ?? new GameObject("MainCanvas");
        Canvas canvas = canvasObj.GetComponent<Canvas>() ?? canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasObj.GetComponent<CanvasScaler>() ?? canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // Inventory Panel
        GameObject inventoryPanel = new GameObject("InventoryPanel");
        inventoryPanel.transform.SetParent(canvasObj.transform);
        RectTransform panelRT = inventoryPanel.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(1000, 700);
        panelRT.anchoredPosition = Vector2.zero;

        Image panelBg = inventoryPanel.AddComponent<Image>();
        panelBg.color = new Color(0.05f, 0.05f, 0.05f, 0.95f);

        // Main Inventory Grid
        GameObject mainGrid = new GameObject("MainInventoryGrid");
        mainGrid.transform.SetParent(inventoryPanel.transform);
        RectTransform gridRT = mainGrid.AddComponent<RectTransform>();
        gridRT.anchorMin = new Vector2(0.5f, 0.5f);
        gridRT.anchorMax = new Vector2(0.5f, 0.5f);
        gridRT.sizeDelta = new Vector2(920, 580);
        gridRT.anchoredPosition = new Vector2(0, 10);

        GridLayoutGroup grid = mainGrid.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(80, 80);
        grid.spacing = new Vector2(12, 12);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 8;

        Image gridBg = mainGrid.AddComponent<Image>();
        gridBg.color = new Color(0.15f, 0.15f, 0.15f, 0.7f);

        inventoryPanel.SetActive(false);

        Debug.Log("✅ Full Inventory Panel + Grid created successfully!");
        Selection.activeGameObject = inventoryPanel;
    }
}