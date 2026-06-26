using Project.Map;
using Project.EditorTools;
using Project.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Adds WorldMapProvider, MapUI, and PlayerInput wiring for the map system.
/// </summary>
public static class MapSystemSetupUtility
{
    private const string MapActionId = "e8f9a0b1-c2d3-4456-a789-012345678abc";
    private const string JournalActionId = "b1c2d3e4-f5a6-4789-b012-3456789abcde";

    [MenuItem(SurvivalPioneerEditorMenus.Scene + "Map System", false, 0)]
    public static void SetupMapSystem()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            EditorUtility.DisplayDialog("Map System", "Open a scene first.", "OK");
            return;
        }

        int changes = 0;
        changes += EnsureWorldMapProvider();
        changes += EnsureMapUi() ? 1 : 0;
        changes += WirePlayerInput();

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log(changes > 0
            ? $"Map system setup complete ({changes} change(s)). Minimap: top-right. Full map: M. Journal tabs: Survival Pioneer > Scene > Journal Input Shortcuts."
            : "Map system already set up.");
    }

    private static int EnsureWorldMapProvider()
    {
        if (Object.FindAnyObjectByType<WorldMapProvider>() != null)
            return 0;

        Terrain terrain = Object.FindAnyObjectByType<Terrain>();
        if (terrain != null)
        {
            Undo.AddComponent<WorldMapProvider>(terrain.gameObject);
            return 1;
        }

        GameObject providerObject = new GameObject("WorldMapProvider");
        Undo.RegisterCreatedObjectUndo(providerObject, "Create WorldMapProvider");
        providerObject.AddComponent<WorldMapProvider>();
        return 1;
    }

    private static bool EnsureMapUi()
    {
        GameObject canvasObject = GameObject.Find("MainCanvas");
        if (canvasObject == null)
            return false;

        if (canvasObject.GetComponent<MapUI>() != null)
            return false;

        Undo.AddComponent<MapUI>(canvasObject);
        return true;
    }

    private static int WirePlayerInput()
    {
        MapUI mapUi = Object.FindAnyObjectByType<MapUI>();
        UIManager uiManager = Object.FindAnyObjectByType<UIManager>();
        PlayerInput playerInput = Object.FindAnyObjectByType<PlayerInput>();
        if (playerInput == null)
            return 0;

        SerializedObject serializedPlayerInput = new SerializedObject(playerInput);
        SerializedProperty actionEvents = serializedPlayerInput.FindProperty("m_ActionEvents");
        if (actionEvents == null || !actionEvents.isArray)
            return 0;

        int changes = 0;
        if (mapUi != null && WireActionEvent(actionEvents, MapActionId, mapUi, "Project.UI.MapUI, Assembly-CSharp", "OnToggleMap"))
            changes++;

        if (uiManager != null && WireActionEvent(actionEvents, JournalActionId, uiManager, "Project.UI.UIManager, Assembly-CSharp", "OnToggleJournal"))
            changes++;

        if (changes > 0)
            serializedPlayerInput.ApplyModifiedProperties();

        return changes;
    }

    private static bool WireActionEvent(
        SerializedProperty actionEvents,
        string actionId,
        Object target,
        string targetTypeName,
        string methodName)
    {
        for (int i = 0; i < actionEvents.arraySize; i++)
        {
            SerializedProperty entry = actionEvents.GetArrayElementAtIndex(i);
            if (entry.FindPropertyRelative("m_ActionId").stringValue != actionId)
                continue;

            if (EntryTargetsComponent(entry, target, methodName))
                return false;

            AddInputCall(entry, target, targetTypeName, methodName);
            return true;
        }

        int newIndex = actionEvents.arraySize;
        actionEvents.InsertArrayElementAtIndex(newIndex);
        SerializedProperty newEntry = actionEvents.GetArrayElementAtIndex(newIndex);
        newEntry.FindPropertyRelative("m_ActionId").stringValue = actionId;
        AddInputCall(newEntry, target, targetTypeName, methodName);
        return true;
    }

    private static bool EntryTargetsComponent(SerializedProperty entry, Object target, string methodName)
    {
        SerializedProperty calls = entry.FindPropertyRelative("m_PersistentCalls.m_Calls");
        if (calls == null || !calls.isArray)
            return false;

        for (int i = 0; i < calls.arraySize; i++)
        {
            SerializedProperty call = calls.GetArrayElementAtIndex(i);
            if (call.FindPropertyRelative("m_Target").objectReferenceValue == target
                && call.FindPropertyRelative("m_MethodName").stringValue == methodName)
                return true;
        }

        return false;
    }

    private static void AddInputCall(SerializedProperty entry, Object target, string targetTypeName, string methodName)
    {
        SerializedProperty calls = entry.FindPropertyRelative("m_PersistentCalls.m_Calls");
        int callIndex = calls.arraySize;
        calls.InsertArrayElementAtIndex(callIndex);
        SerializedProperty call = calls.GetArrayElementAtIndex(callIndex);
        call.FindPropertyRelative("m_Target").objectReferenceValue = target;
        call.FindPropertyRelative("m_TargetAssemblyTypeName").stringValue = targetTypeName;
        call.FindPropertyRelative("m_MethodName").stringValue = methodName;
        call.FindPropertyRelative("m_Mode").enumValueIndex = 0;
        call.FindPropertyRelative("m_CallState").enumValueIndex = 2;
    }
}
