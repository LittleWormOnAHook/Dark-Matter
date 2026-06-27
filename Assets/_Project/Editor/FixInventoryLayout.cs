using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using Project.EditorTools;

public class FixInventoryLayout : EditorWindow
{
    [MenuItem(SurvivalPioneerEditorMenus.Ui + "Fix Inventory Grid Layout")]
    public static void FixLayout()
    {
        GameObject gridObj = GameObject.Find("MainInventoryGrid");
        if (gridObj == null)
        {
            Debug.LogError("MainInventoryGrid not found!");
            return;
        }

        GridLayoutGroup grid = gridObj.GetComponent<GridLayoutGroup>();
        if (grid == null)
            grid = gridObj.AddComponent<GridLayoutGroup>();

        grid.cellSize = new Vector2(80, 80);
        grid.spacing = new Vector2(12, 12);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 8;

        Debug.Log("✅ Inventory Grid Layout fixed!");
        EditorApplication.ExecuteMenuItem("Edit/Play");
    }
}