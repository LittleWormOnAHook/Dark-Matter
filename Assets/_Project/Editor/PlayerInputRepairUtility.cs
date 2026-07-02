using System.Collections.Generic;
using System.Text;
using Project.EditorTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Removes stale UI-map and orphaned action events from PlayerInput components.
/// Fixes PlayerInputEditor NullReferenceException when inspecting PlayerInput.
/// </summary>
public static class PlayerInputRepairUtility
{
    private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Players/Player.prefab";
    private const string InputActionsPath = "Assets/_Project/Settings/Input/InputSystem_Actions.inputactions";
    private const string PlayerActionMapName = "Player";

    [MenuItem(SurvivalPioneerEditorMenus.Maintenance + "Repair PlayerInput Action Events", false, 15)]
    public static void RepairPlayerInputMenu()
    {
        int prefabChanges = RepairPlayerPrefab(resyncPlayerMap: false);
        int sceneChanges = RepairActiveScene(resyncPlayerMap: false);

        string summary = BuildSummary(prefabChanges, sceneChanges);
        if (prefabChanges + sceneChanges > 0)
            Debug.Log(summary);
        else
            Debug.Log("PlayerInput repair: no changes needed.");
    }

    [MenuItem(SurvivalPioneerEditorMenus.Maintenance + "Repair PlayerInput + Sync Player Map", false, 16)]
    public static void RepairAndSyncPlayerMapMenu()
    {
        int prefabChanges = RepairPlayerPrefab(resyncPlayerMap: true);
        int sceneChanges = RepairActiveScene(resyncPlayerMap: true);

        string summary = BuildSummary(prefabChanges, sceneChanges);
        if (prefabChanges + sceneChanges > 0)
            Debug.Log(summary);
        else
            Debug.Log("PlayerInput repair + sync: no changes needed.");
    }

    private static string BuildSummary(int prefabChanges, int sceneChanges)
    {
        var builder = new StringBuilder("PlayerInput repair complete.");
        if (prefabChanges > 0)
            builder.Append($" Player prefab: {prefabChanges} change(s).");
        if (sceneChanges > 0)
            builder.Append($" Active scene: {sceneChanges} change(s).");
        return builder.ToString();
    }

    private static int RepairPlayerPrefab(bool resyncPlayerMap)
    {
        GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (prefabRoot == null)
        {
            Debug.LogWarning($"PlayerInput repair: prefab not found at {PlayerPrefabPath}.");
            return 0;
        }

        PlayerInput playerInput = prefabRoot.GetComponent<PlayerInput>();
        if (playerInput == null)
        {
            Debug.LogWarning("PlayerInput repair: Player prefab has no PlayerInput component.");
            return 0;
        }

        int changes = RepairPlayerInput(playerInput, resyncPlayerMap, "Player prefab");
        if (changes > 0)
        {
            PrefabUtility.SavePrefabAsset(prefabRoot);
            AssetDatabase.SaveAssets();
        }

        return changes;
    }

    private static int RepairActiveScene(bool resyncPlayerMap)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
            return 0;

        var playerInputs = new List<PlayerInput>();
        foreach (GameObject root in scene.GetRootGameObjects())
            playerInputs.AddRange(root.GetComponentsInChildren<PlayerInput>(true));

        if (playerInputs.Count == 0)
            return 0;

        int totalChanges = 0;
        foreach (PlayerInput playerInput in playerInputs)
        {
            int changes = RepairPlayerInput(playerInput, resyncPlayerMap, $"{scene.name}/{playerInput.gameObject.name}");
            if (changes > 0)
                totalChanges += changes;
        }

        if (totalChanges > 0)
            EditorSceneManager.MarkSceneDirty(scene);

        return totalChanges;
    }

    private static int RepairPlayerInput(PlayerInput playerInput, bool resyncPlayerMap, string contextLabel)
    {
        InputActionAsset asset = playerInput.actions;
        if (asset == null)
            asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);

        if (asset == null)
        {
            Debug.LogWarning($"PlayerInput repair ({contextLabel}): no InputActionAsset assigned.");
            return 0;
        }

        HashSet<string> allActionIds = CollectActionIds(asset);
        HashSet<string> uiActionIds = CollectMapActionIds(asset, "UI");

        SerializedObject serialized = new SerializedObject(playerInput);
        SerializedProperty actionEvents = serialized.FindProperty("m_ActionEvents");
        if (actionEvents == null || !actionEvents.isArray)
            return 0;

        int removedUi = 0;
        int removedOrphan = 0;
        int addedPlayer = 0;

        for (int i = actionEvents.arraySize - 1; i >= 0; i--)
        {
            SerializedProperty entry = actionEvents.GetArrayElementAtIndex(i);
            string actionId = entry.FindPropertyRelative("m_ActionId").stringValue;
            string actionName = entry.FindPropertyRelative("m_ActionName").stringValue;
            string mapPrefix = GetMapPrefix(actionName);

            if (IsUiAction(mapPrefix, actionId, uiActionIds))
            {
                actionEvents.DeleteArrayElementAtIndex(i);
                removedUi++;
                continue;
            }

            if (!allActionIds.Contains(actionId))
            {
                Debug.LogWarning(
                    $"PlayerInput repair ({contextLabel}): removed orphaned action event id={actionId} name={actionName}");
                actionEvents.DeleteArrayElementAtIndex(i);
                removedOrphan++;
            }
        }

        if (resyncPlayerMap)
            addedPlayer = EnsurePlayerMapEvents(actionEvents, asset);

        int totalChanges = removedUi + removedOrphan + addedPlayer;
        if (totalChanges > 0)
        {
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(playerInput);
            Debug.Log(
                $"PlayerInput repair ({contextLabel}): removed {removedUi} UI event(s), " +
                $"{removedOrphan} orphaned event(s), added {addedPlayer} Player map event(s).");
        }

        return totalChanges;
    }

    private static int EnsurePlayerMapEvents(SerializedProperty actionEvents, InputActionAsset asset)
    {
        InputActionMap playerMap = asset.FindActionMap(PlayerActionMapName, throwIfNotFound: false);
        if (playerMap == null)
            return 0;

        var existingIds = new HashSet<string>();
        for (int i = 0; i < actionEvents.arraySize; i++)
        {
            SerializedProperty entry = actionEvents.GetArrayElementAtIndex(i);
            existingIds.Add(entry.FindPropertyRelative("m_ActionId").stringValue);
        }

        int added = 0;
        foreach (InputAction action in playerMap.actions)
        {
            string id = action.id.ToString();
            if (existingIds.Contains(id))
                continue;

            int index = actionEvents.arraySize;
            actionEvents.InsertArrayElementAtIndex(index);
            SerializedProperty newEntry = actionEvents.GetArrayElementAtIndex(index);
            newEntry.FindPropertyRelative("m_ActionId").stringValue = id;
            newEntry.FindPropertyRelative("m_ActionName").stringValue = $"{PlayerActionMapName}/{action.name}";
            added++;
        }

        return added;
    }

    private static HashSet<string> CollectActionIds(InputActionAsset asset)
    {
        var ids = new HashSet<string>();
        foreach (InputActionMap map in asset.actionMaps)
        {
            foreach (InputAction action in map.actions)
                ids.Add(action.id.ToString());
        }

        return ids;
    }

    private static HashSet<string> CollectMapActionIds(InputActionAsset asset, string mapName)
    {
        var ids = new HashSet<string>();
        InputActionMap map = asset.FindActionMap(mapName, throwIfNotFound: false);
        if (map == null)
            return ids;

        foreach (InputAction action in map.actions)
            ids.Add(action.id.ToString());

        return ids;
    }

    private static bool IsUiAction(string mapPrefix, string actionId, HashSet<string> uiActionIds)
    {
        if (mapPrefix == "UI")
            return true;

        return uiActionIds.Contains(actionId);
    }

    private static string GetMapPrefix(string actionName)
    {
        if (string.IsNullOrEmpty(actionName))
            return string.Empty;

        int slashIndex = actionName.IndexOf('/');
        if (slashIndex <= 0)
            return string.Empty;

        return actionName.Substring(0, slashIndex);
    }
}
