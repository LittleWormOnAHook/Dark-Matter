using System;
using Project.Data;
using Project.EditorTools;
using Project.Interaction;
using Project.Inventory;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Bakes held-item grip offsets from the live weapon transform parented to the hand.
/// Use while paused in Play mode after positioning the equipped weapon where you want it.
/// </summary>
public static class HeldGripPlacementUtility
{
    private const string DefaultSwordAssetPath = "Assets/_Project/Data/Items/weap2_sword.asset";

    [InitializeOnLoadMethod]
    private static void RegisterPlayModeSaveHook()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
            AssetDatabase.SaveAssets();
    }

    [MenuItem(SurvivalPioneerEditorMenus.Equipment + "Bake Sheathed Grip From Player Back")]
    private static void BakeFromPlayerBackMenu()
    {
        if (!EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Sheathed Grip Baker",
                "Enter Play mode first.\n\n1. Put the weapon on a weapon hotbar slot (1–4)\n2. Sheathe it (press the same weapon key again so it holsters on your back)\n3. Pause the editor\n4. Adjust the weapon under Spine if needed\n5. Run this bake again",
                "OK");
            return;
        }

        ItemData item = ResolveTargetItemDataForSheathed();
        if (item == null)
            return;

        string assetPath = AssetDatabase.GetAssetPath(item);
        if (TryBakeFromPlayerBack(assetPath, "Spine", item))
        {
            EditorUtility.DisplayDialog(
                "Sheathed Grip Baker",
                $"Saved sheathed grip to {item.name}.\n\nValues are written to disk and will persist after exiting Play mode.",
                "OK");
        }
    }

    [MenuItem(SurvivalPioneerEditorMenus.Equipment + "Bake Sheathed Grip From Selected Transform")]
    private static void BakeSheathedFromSelectedMenu()
    {
        Transform selected = Selection.activeTransform;
        if (selected == null)
        {
            EditorUtility.DisplayDialog(
                "Sheathed Grip Baker",
                "Select the positioned holstered weapon in the Hierarchy first.",
                "OK");
            return;
        }

        Transform spine = FindAncestorNamed(selected, "Spine");
        if (spine == null)
        {
            EditorUtility.DisplayDialog(
                "Sheathed Grip Baker",
                "Selected object must be parented under Spine (move the holstered weapon on the player's back, not a scene copy).",
                "OK");
            return;
        }

        ItemData item = ResolveItemDataForTransform(selected);
        if (item == null)
            item = ResolveTargetItemDataForSheathed();

        if (item == null)
        {
            EditorUtility.DisplayDialog(
                "Sheathed Grip Baker",
                "Could not determine which ItemData asset to update. Holster the weapon in Play mode or select its ItemData asset in the Project window.",
                "OK");
            return;
        }

        string assetPath = AssetDatabase.GetAssetPath(item);
        Transform sheathedRoot = GetHeldWeaponRoot(spine, selected);
        if (TryBakeSheathedTransformToItem(assetPath, spine, sheathedRoot, "Spine"))
        {
            EditorUtility.DisplayDialog("Sheathed Grip Baker", $"Saved sheathed grip to {item.name}.", "OK");
        }
    }

    [MenuItem(SurvivalPioneerEditorMenus.Equipment + "Bake Sheathed Grip From Clipboard JSON")]
    private static void BakeSheathedFromClipboard()
    {
        if (!TryParseWorldPlacement(EditorGUIUtility.systemCopyBuffer, out WorldPlacement placement))
        {
            EditorUtility.DisplayDialog(
                "Sheathed Grip Baker",
                "Clipboard does not contain a valid UnityEditor.TransformWorldPlacementJSON payload.",
                "OK");
            return;
        }

        string assetPath = EditorUtility.OpenFilePanel("Select ItemData asset", "Assets/_Project/Data/Items", "asset");
        if (string.IsNullOrWhiteSpace(assetPath))
            return;

        if (assetPath.StartsWith(Application.dataPath, StringComparison.OrdinalIgnoreCase))
            assetPath = "Assets" + assetPath.Substring(Application.dataPath.Length);

        if (TryBakeSheathedWorldPlacementToItem(assetPath, placement, "Spine"))
            EditorUtility.DisplayDialog("Sheathed Grip Baker", "Sheathed grip values were written to the selected ItemData asset.", "OK");
    }

    [MenuItem(SurvivalPioneerEditorMenus.Equipment + "Bake Held Grip From Player Hand")]
    private static void BakeFromPlayerHandMenu()
    {
        if (!EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Held Grip Baker",
                "Enter Play mode first.\n\n1. Equip the weapon on your hotbar\n2. Pause (Play button or Editor Pause)\n3. Move the weapon root under RightHand in the Hierarchy\n4. Run this bake again",
                "OK");
            return;
        }

        ItemData item = ResolveTargetItemData();
        if (item == null)
            return;

        string assetPath = AssetDatabase.GetAssetPath(item);
        if (TryBakeFromPlayerHand(assetPath, "RightHand", item))
        {
            EditorUtility.DisplayDialog(
                "Held Grip Baker",
                $"Saved held grip to {item.name}.\n\nValues are written to disk and will persist after exiting Play mode.",
                "OK");
        }
    }

    [MenuItem(SurvivalPioneerEditorMenus.Equipment + "Bake Held Grip From Selected Transform")]
    private static void BakeFromSelectedMenu()
    {
        Transform selected = Selection.activeTransform;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("Held Grip Baker", "Select the positioned held weapon in the Hierarchy first.", "OK");
            return;
        }

        Transform hand = FindAncestorNamed(selected, "RightHand");
        if (hand == null)
        {
            EditorUtility.DisplayDialog(
                "Held Grip Baker",
                "Selected object must be parented under RightHand (move the equipped sword, not a scene copy).",
                "OK");
            return;
        }

        ItemData item = ResolveItemDataForTransform(selected);
        if (item == null)
            item = ResolveTargetItemData();

        if (item == null)
        {
            EditorUtility.DisplayDialog(
                "Held Grip Baker",
                "Could not determine which ItemData asset to update. Equip a weapon in Play mode or select an ItemData asset in the Project window.",
                "OK");
            return;
        }

        string assetPath = AssetDatabase.GetAssetPath(item);
        Transform heldRoot = GetHeldWeaponRoot(hand, selected);
        if (TryBakeLocalTransformToItem(assetPath, hand, heldRoot, item.equipSocketName))
        {
            EditorUtility.DisplayDialog("Held Grip Baker", $"Saved grip to {item.name}.", "OK");
        }
    }

    [MenuItem(SurvivalPioneerEditorMenus.Equipment + "Bake Held Grip From Clipboard JSON")]
    private static void BakeFromClipboard()
    {
        if (!TryParseWorldPlacement(EditorGUIUtility.systemCopyBuffer, out WorldPlacement placement))
        {
            EditorUtility.DisplayDialog(
                "Held Grip Baker",
                "Clipboard does not contain a valid UnityEditor.TransformWorldPlacementJSON payload.",
                "OK");
            return;
        }

        string assetPath = EditorUtility.OpenFilePanel("Select ItemData asset", "Assets/_Project/Data/Items", "asset");
        if (string.IsNullOrWhiteSpace(assetPath))
            return;

        if (assetPath.StartsWith(Application.dataPath, StringComparison.OrdinalIgnoreCase))
            assetPath = "Assets" + assetPath.Substring(Application.dataPath.Length);

        if (TryBakeWorldPlacementToItem(assetPath, placement, "RightHand"))
            EditorUtility.DisplayDialog("Held Grip Baker", "Held grip values were written to the selected ItemData asset.", "OK");
    }

    private static bool TryBakeFromPlayerHand(string assetPath, string socketName, ItemData item)
    {
        if (item == null)
            item = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);

        if (item == null)
        {
            Debug.LogWarning($"HeldGripPlacementUtility: could not load ItemData at {assetPath}");
            return false;
        }

        Transform hand = FindSceneHandSocket(socketName);
        if (hand == null)
        {
            EditorUtility.DisplayDialog(
                "Held Grip Baker",
                "Could not find Player/RightHand. Enter Play mode, equip the weapon, pause, position the weapon root under RightHand, then run this again.",
                "OK");
            return false;
        }

        Transform held = ResolveHeldTransformForBake(hand, item);
        if (held == null)
        {
            EditorUtility.DisplayDialog(
                "Held Grip Baker",
                "No equipped weapon found under RightHand.\n\nDraw the weapon on your hotbar, pause, then move the weapon root object (not an internal mesh child) under RightHand.",
                "OK");
            return false;
        }

        if (held.parent != hand)
        {
            EditorUtility.DisplayDialog(
                "Held Grip Baker",
                $"Move the weapon root '{held.name}' (direct child of RightHand), not an internal mesh part. Baking using root transform.",
                "OK");
        }

        return TryBakeLocalTransformToItem(assetPath, hand, held, socketName);
    }

    private static bool TryBakeFromPlayerBack(string assetPath, string socketName, ItemData item)
    {
        if (item == null)
            item = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);
        if (item == null)
        {
            Debug.LogWarning($"HeldGripPlacementUtility: could not load ItemData at {assetPath}");
            return false;
        }

        Transform spine = FindSceneHandSocket(socketName);
        if (spine == null)
        {
            EditorUtility.DisplayDialog(
                "Sheathed Grip Baker",
                "Could not find Player/Spine. Enter Play mode, select a non-weapon hotbar slot so the weapon sheathes on your back, pause, reposition it, then run this again.",
                "OK");
            return false;
        }

        Transform sheathed = ResolveHeldTransformForBake(spine, item);
        if (sheathed == null)
        {
            EditorUtility.DisplayDialog(
                "Sheathed Grip Baker",
                "No sheathed weapon found under Spine. Select a non-weapon hotbar slot (or another weapon slot) so the weapon moves to your back, pause, reposition it, then rebake.",
                "OK");
            return false;
        }

        return TryBakeSheathedTransformToItem(assetPath, spine, sheathed, socketName);
    }

    private static ItemData ResolveTargetItemDataForSheathed()
    {
        if (Selection.activeObject is ItemData selectedItem)
            return selectedItem;

        ItemData holstered = GetHolsteredItemInPlayMode();
        if (holstered != null)
            return holstered;

        EditorUtility.DisplayDialog(
            "Sheathed Grip Baker",
            "No sheathed weapon found.\n\nEnter Play mode, put the weapon on a weapon hotbar slot, select a non-weapon hotbar slot so it sheathes on your back, pause, reposition it, or select its ItemData asset in the Project window.",
            "OK");
        return null;
    }

    private static ItemData GetHolsteredItemInPlayMode()
    {
        if (!EditorApplication.isPlaying)
            return null;

        Transform spine = FindSceneHandSocket("Spine");
        Transform selected = Selection.activeTransform;
        if (selected != null && spine != null && IsDescendantOf(selected, spine))
        {
            ItemData fromSelection = ResolveItemDataForTransform(selected);
            if (fromSelection != null)
                return fromSelection;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return null;

        EquipmentController equipment = player.GetComponent<EquipmentController>();
        if (equipment == null)
            return null;

        int activeHotbar = equipment.ActiveWeaponHotbarSlot;
        bool activeWeaponSelected = equipment.IsWeaponHotbarSlot(equipment.SelectedHotbarSlot);
        ItemData backCandidate = null;

        equipment.ForEachWeaponHotbarSlot(hotbarIndex =>
        {
            if (backCandidate != null)
                return;

            if (equipment.IsWeaponDrawn && activeWeaponSelected && hotbarIndex == activeHotbar)
                return;

            ItemData weapon = equipment.GetHotbarItem(hotbarIndex);
            if (weapon != null && weapon.IsEquippable)
                backCandidate = weapon;
        });

        if (backCandidate != null)
            return backCandidate;

        if (spine != null)
        {
            Transform visual = FindHeldWeaponRootUnderSocket(spine, null);
            if (visual != null)
                return ResolveItemDataForTransform(visual, allowDefaultFallback: false);
        }

        return null;
    }

    private static ItemData ResolveTargetItemData()
    {
        ItemData equipped = GetEquippedItemInPlayMode();
        if (equipped != null)
            return equipped;

        if (Selection.activeObject is ItemData selectedItem)
            return selectedItem;

        EditorUtility.DisplayDialog(
            "Grip Baker",
            "No equipped weapon found.\n\nEnter Play mode with a weapon hotbar slot selected (weapon in hand), or select an ItemData asset in the Project window.",
            "OK");
        return null;
    }

    private static ItemData GetEquippedItemInPlayMode()
    {
        if (!EditorApplication.isPlaying)
            return null;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return null;

        EquipmentController equipment = player.GetComponent<EquipmentController>();
        return equipment != null && equipment.HasActiveMeleeWeapon() ? equipment.SelectedHotbarItem : null;
    }

    private static Transform ResolveHeldTransformForBake(Transform socket, ItemData item)
    {
        Transform selected = Selection.activeTransform;
        if (selected != null && IsDescendantOf(selected, socket) && !IsFingerBone(selected.name))
        {
            Transform root = GetHeldWeaponRoot(socket, selected);
            if (root != null)
                return root;
        }

        return FindHeldWeaponRootUnderSocket(socket, item);
    }

    private static Transform GetHeldWeaponRoot(Transform socket, Transform anyDescendant)
    {
        Transform current = anyDescendant;
        Transform root = anyDescendant;

        while (current != null && current != socket)
        {
            if (current.parent == socket)
                return current;

            root = current;
            current = current.parent;
        }

        return root;
    }

    private static bool IsDescendantOf(Transform candidate, Transform ancestor)
    {
        Transform current = candidate;
        while (current != null)
        {
            if (current == ancestor)
                return true;
            current = current.parent;
        }

        return false;
    }

    private static bool TryBakeSheathedTransformToItem(
        string assetPath,
        Transform socket,
        Transform sheathed,
        string socketName)
    {
        ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);
        if (item == null)
            return false;

        Vector3 localPosition = socket.InverseTransformPoint(sheathed.position);
        Quaternion localRotation = Quaternion.Inverse(socket.rotation) * sheathed.rotation;
        Vector3 localScale = sheathed.localScale;
        if (sheathed.parent == socket)
        {
            localPosition = sheathed.localPosition;
            localRotation = sheathed.localRotation;
            localScale = sheathed.localScale;
        }

        ApplySheathedGripToItem(item, localPosition, localRotation, localScale, socketName);
        ApplySheathedGripToLiveVisual(item, localPosition, localRotation, localScale);

        Debug.Log(
            $"HeldGripPlacementUtility: baked {item.name} sheathed grip — " +
            $"pos {localPosition}, rot {localRotation.eulerAngles}, scale {localScale}");
        return true;
    }

    private static bool TryBakeSheathedWorldPlacementToItem(string assetPath, WorldPlacement placement, string socketName)
    {
        ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);
        if (item == null)
            return false;

        Transform socket = FindSceneHandSocket(socketName);
        if (socket == null)
        {
            EditorUtility.DisplayDialog(
                "Sheathed Grip Baker",
                $"Could not find Player/{socketName} in the current scene. Use Bake Sheathed Grip From Player Back while paused in Play mode instead.",
                "OK");
            return false;
        }

        Vector3 localPosition = socket.InverseTransformPoint(placement.position);
        Quaternion localRotation = Quaternion.Inverse(socket.rotation) * placement.rotation;
        Vector3 localScale = placement.scale == Vector3.zero ? Vector3.one : placement.scale;

        ApplySheathedGripToItem(item, localPosition, localRotation, localScale, socketName);
        ApplySheathedGripToLiveVisual(item, localPosition, localRotation, localScale);
        return true;
    }

    private static bool TryBakeLocalTransformToItem(
        string assetPath,
        Transform hand,
        Transform held,
        string socketName)
    {
        ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);
        if (item == null)
            return false;

        if (held.parent != hand)
            Debug.LogWarning("HeldGripPlacementUtility: baking weapon root transform relative to hand socket.");

        Vector3 localPosition = hand.InverseTransformPoint(held.position);
        Quaternion localRotation = Quaternion.Inverse(hand.rotation) * held.rotation;
        Vector3 localScale = held.lossyScale;
        if (held.parent == hand)
        {
            localPosition = held.localPosition;
            localRotation = held.localRotation;
            localScale = held.localScale;
        }

        ApplyGripToItem(item, localPosition, localRotation, localScale, socketName);
        ApplyGripToLiveVisual(item, localPosition, localRotation, localScale);

        Debug.Log(
            $"HeldGripPlacementUtility: baked {item.name} grip from live transform — " +
            $"pos {localPosition}, rot {localRotation.eulerAngles}, scale {localScale}");
        return true;
    }

    private static bool TryBakeWorldPlacementToItem(string assetPath, WorldPlacement placement, string socketName)
    {
        ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);
        if (item == null)
            return false;

        Transform hand = FindSceneHandSocket(socketName);
        if (hand == null)
        {
            EditorUtility.DisplayDialog(
                "Held Grip Baker",
                "Could not find Player/RightHand in the current scene. Use Bake Held Grip From Player Hand while paused in Play mode instead.",
                "OK");
            return false;
        }

        Vector3 localPosition = hand.InverseTransformPoint(placement.position);
        Quaternion localRotation = Quaternion.Inverse(hand.rotation) * placement.rotation;
        Vector3 localScale = placement.scale == Vector3.zero ? Vector3.one : placement.scale;

        ApplyGripToItem(item, localPosition, localRotation, localScale, socketName);
        ApplyGripToLiveVisual(item, localPosition, localRotation, localScale);
        return true;
    }

    private static void ApplySheathedGripToItem(
        ItemData item,
        Vector3 localPosition,
        Quaternion localRotation,
        Vector3 localScale,
        string socketName)
    {
        Undo.RecordObject(item, "Bake Sheathed Grip");
        item.sheathedLocalPosition = localPosition;
        item.sheathedLocalRotation = localRotation;
        item.useSheathedLocalRotation = true;
        item.sheathedLocalEuler = localRotation.eulerAngles;
        item.sheatheSocketName = socketName;
        item.sheathedLocalScale = localScale == Vector3.zero ? Vector3.one : localScale;
        PersistItemData(item);
    }

    private static void ApplyGripToLiveVisual(
        ItemData item,
        Vector3 localPosition,
        Quaternion localRotation,
        Vector3 localScale)
    {
        if (!EditorApplication.isPlaying)
            return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return;

        EquippedItemVisual visual = player.GetComponent<EquippedItemVisual>();
        visual?.ApplyBakedHandGrip(item, localPosition, localRotation, localScale);
    }

    private static void ApplySheathedGripToLiveVisual(
        ItemData item,
        Vector3 localPosition,
        Quaternion localRotation,
        Vector3 localScale)
    {
        if (!EditorApplication.isPlaying)
            return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return;

        EquippedItemVisual visual = player.GetComponent<EquippedItemVisual>();
        visual?.ApplyBakedSheathedGrip(item, localPosition, localRotation, localScale);
    }

    private static void ApplyGripToItem(
        ItemData item,
        Vector3 localPosition,
        Quaternion localRotation,
        Vector3 localScale,
        string socketName)
    {
        Undo.RecordObject(item, "Bake Held Grip");
        item.heldLocalPosition = localPosition;
        item.heldLocalRotation = localRotation;
        item.useHeldLocalRotation = true;
        item.heldLocalEuler = localRotation.eulerAngles;
        item.equipSocketName = socketName;
        item.heldLocalScale = localScale == Vector3.zero ? Vector3.one : localScale;
        PersistItemData(item);
    }

    private static void PersistItemData(ItemData item)
    {
        if (item == null)
            return;

        EditorUtility.SetDirty(item);
        AssetDatabase.SaveAssets();

        string path = AssetDatabase.GetAssetPath(item);
        if (!string.IsNullOrEmpty(path))
            AssetDatabase.ImportAsset(path);
    }

    private static Transform FindHeldWeaponRootUnderSocket(Transform socket, ItemData item)
    {
        if (socket == null)
            return null;

        string prefabName = item != null && item.heldPrefab != null ? item.heldPrefab.name : null;
        Transform nameMatch = null;
        Transform firstVisual = null;

        for (int i = 0; i < socket.childCount; i++)
        {
            Transform child = socket.GetChild(i);
            if (IsFingerBone(child.name) || !HasRendererHierarchy(child))
                continue;

            if (!string.IsNullOrEmpty(prefabName) &&
                child.name.StartsWith(prefabName, StringComparison.Ordinal))
                nameMatch = child;

            if (firstVisual == null)
                firstVisual = child;
        }

        return nameMatch != null ? nameMatch : firstVisual;
    }

    private static bool HasRendererHierarchy(Transform root)
    {
        return root.GetComponentInChildren<Renderer>() != null;
    }

    private static Transform FindSceneHandSocket(string socketName)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return null;

        return FindDeepChild(player.transform, socketName);
    }

    private static Transform FindHeldWeaponUnderHand(Transform hand, ItemData item)
    {
        return FindHeldWeaponRootUnderSocket(hand, item);
    }

    private static bool IsFingerBone(string boneName)
    {
        return boneName.StartsWith("Right", StringComparison.Ordinal) &&
               (boneName.Contains("Thumb") ||
                boneName.Contains("Index") ||
                boneName.Contains("Middle") ||
                boneName.Contains("Ring") ||
                boneName.Contains("Pinky"));
    }

    private static ItemData ResolveItemDataForTransform(Transform held, bool allowDefaultFallback = true)
    {
        string objectName = held.name.Replace("(Clone)", string.Empty).Trim();
        string[] guids = AssetDatabase.FindAssets($"{objectName} t:ItemData");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (item != null && item.itemName == objectName)
                return item;
        }

        return allowDefaultFallback
            ? AssetDatabase.LoadAssetAtPath<ItemData>(DefaultSwordAssetPath)
            : null;
    }

    private static Transform FindAncestorNamed(Transform transform, string name)
    {
        Transform current = transform;
        while (current != null)
        {
            if (current.name == name)
                return current;
            current = current.parent;
        }

        return null;
    }

    private static Transform FindDeepChild(Transform parent, string childName)
    {
        if (parent.name == childName)
            return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindDeepChild(parent.GetChild(i), childName);
            if (found != null)
                return found;
        }

        return null;
    }

    private static bool TryParseWorldPlacement(string raw, out WorldPlacement placement)
    {
        placement = default;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        const string prefix = "UnityEditor.TransformWorldPlacementJSON:";
        string json = raw.Trim();
        if (json.StartsWith(prefix, StringComparison.Ordinal))
            json = json.Substring(prefix.Length);

        try
        {
            placement = JsonUtility.FromJson<WorldPlacement>(json);
            return placement.rotation != default || placement.position != default;
        }
        catch
        {
            return false;
        }
    }

    [Serializable]
    private struct WorldPlacement
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
    }
}
