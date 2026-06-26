using Project.EditorTools;
using Project.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Wires PlayerInput unity events for journal tab keyboard shortcuts.
/// </summary>
public static class JournalInputSetupUtility
{
    private const string InventoryActionId = "23757435-cd6f-4ea0-83b0-70108500f300";
    private const string PetsActionId = "a4b8c2d1-5e6f-4789-a0b1-c2d3e4f56789";
    private const string MapActionId = "e8f9a0b1-c2d3-4456-a789-012345678abc";
    private const string JournalActionId = "b1c2d3e4-f5a6-4789-b012-3456789abcde";
    private const string CraftActionId = "a1a1a1a1-b1b1-4c1c-8d1d-111111111001";
    private const string RecipesActionId = "a1a1a1a1-b1b1-4c1c-8d1d-111111111002";
    private const string PioneersActionId = "a1a1a1a1-b1b1-4c1c-8d1d-111111111003";
    private const string SkillsActionId = "a1a1a1a1-b1b1-4c1c-8d1d-111111111004";
    private const string EchoesActionId = "a1a1a1a1-b1b1-4c1c-8d1d-111111111005";

    [MenuItem(SurvivalPioneerEditorMenus.Scene + "Journal Input Shortcuts", false, 1)]
    public static void SetupJournalInput()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            EditorUtility.DisplayDialog("Journal Input", "Open a scene first.", "OK");
            return;
        }

        UIManager uiManager = Object.FindAnyObjectByType<UIManager>();
        InventoryUI inventoryUi = Object.FindAnyObjectByType<InventoryUI>();
        PetUI petUi = Object.FindAnyObjectByType<PetUI>();
        MapUI mapUi = Object.FindAnyObjectByType<MapUI>();
        PlayerInput playerInput = Object.FindAnyObjectByType<PlayerInput>();

        if (playerInput == null)
        {
            EditorUtility.DisplayDialog("Journal Input", "No PlayerInput found in the scene.", "OK");
            return;
        }

        SerializedObject serializedPlayerInput = new SerializedObject(playerInput);
        SerializedProperty actionEvents = serializedPlayerInput.FindProperty("m_ActionEvents");
        if (actionEvents == null || !actionEvents.isArray)
        {
            EditorUtility.DisplayDialog("Journal Input", "PlayerInput has no action events array.", "OK");
            return;
        }

        int changes = 0;
        if (inventoryUi != null && WireActionEvent(actionEvents, InventoryActionId, inventoryUi, "Project.UI.InventoryUI, Assembly-CSharp", "OnToggleInventory"))
            changes++;
        if (petUi != null && WireActionEvent(actionEvents, PetsActionId, petUi, "Project.UI.PetUI, Assembly-CSharp", "OnTogglePets"))
            changes++;
        if (mapUi != null && WireActionEvent(actionEvents, MapActionId, mapUi, "Project.UI.MapUI, Assembly-CSharp", "OnToggleMap"))
            changes++;

        if (uiManager != null)
        {
            if (WireActionEvent(actionEvents, JournalActionId, uiManager, "Project.UI.UIManager, Assembly-CSharp", "OnToggleJournal"))
                changes++;
            if (WireActionEvent(actionEvents, CraftActionId, uiManager, "Project.UI.UIManager, Assembly-CSharp", "OnToggleCraft"))
                changes++;
            if (WireActionEvent(actionEvents, RecipesActionId, uiManager, "Project.UI.UIManager, Assembly-CSharp", "OnToggleRecipes"))
                changes++;
            if (WireActionEvent(actionEvents, PioneersActionId, uiManager, "Project.UI.UIManager, Assembly-CSharp", "OnTogglePioneers"))
                changes++;
            if (WireActionEvent(actionEvents, SkillsActionId, uiManager, "Project.UI.UIManager, Assembly-CSharp", "OnToggleSkills"))
                changes++;
            if (WireActionEvent(actionEvents, EchoesActionId, uiManager, "Project.UI.UIManager, Assembly-CSharp", "OnToggleEchoes"))
                changes++;
        }

        if (uiManager != null && uiManager.GetComponent<JournalPanelUI>() == null)
        {
            Undo.AddComponent<JournalPanelUI>(uiManager.gameObject);
            changes++;
        }

        if (changes > 0)
            serializedPlayerInput.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log(changes > 0
            ? $"Journal input wired ({changes} change(s)). Keys: J Journal, I Inventory, M Map, K Pet, P Pioneers, C Craft, R Recipes, T Skills, L Echoes."
            : "Journal input already wired.");
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
