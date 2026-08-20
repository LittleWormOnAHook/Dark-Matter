#if UNITY_EDITOR
using Project.Data;
using Project.EditorTools;
using Project.Inventory;
using Project.Player.Invector;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Invector
{
    /// <summary>
    /// Play-mode grip baker for Player_Invector weapons. Captures local offsets on Invector handler
    /// sockets (drawn) and holster bones (sheathed) into ItemData.
    /// </summary>
    public static class InvectorWeaponGripBakeUtility
    {
        [MenuItem(DarkMatterGenesisEditorMenus.OpenInvectorWeaponGripWindow, false, 10)]
        public static void OpenWindow()
        {
            InvectorWeaponGripBakeWindow.Open();
        }

        [MenuItem(DarkMatterGenesisEditorMenus.BakeInvectorDrawnGrip, false, 11)]
        public static void BakeDrawnGripMenu()
        {
            BakeDrawnGrip(showDialog: true);
        }

        [MenuItem(DarkMatterGenesisEditorMenus.BakeInvectorHolsteredGrip, false, 12)]
        public static void BakeHolsteredGripMenu()
        {
            BakeHolsteredGrip(showDialog: true);
        }

        [MenuItem(DarkMatterGenesisEditorMenus.PreviewInvectorHolsteredWeapon, false, 13)]
        public static void PreviewHolsteredMenu()
        {
            if (!TryResolveContext(out PioneerInvectorWeaponBridge bridge, out ItemData item, out string error))
            {
                EditorUtility.DisplayDialog("Invector Weapon Grip", error, "OK");
                return;
            }

            bridge.BeginHolsterPreview(item);
            Selection.activeTransform = bridge.TryGetHolsteredWeaponInstance(item)?.transform;
            Debug.Log($"InvectorWeaponGripBakeUtility: holster preview active for '{item.name}'. Adjust the hip/back visual slot, pause, then bake holstered grip.");
        }

        [MenuItem(DarkMatterGenesisEditorMenus.EndInvectorHolsterPreview, false, 14)]
        public static void EndHolsterPreviewMenu()
        {
            PioneerInvectorWeaponBridge bridge = FindPlayerBridge();
            if (bridge == null)
            {
                EditorUtility.DisplayDialog("Invector Weapon Grip", "No Player_Invector bridge found in the scene.", "OK");
                return;
            }

            bridge.EndHolsterPreview();
            Debug.Log("InvectorWeaponGripBakeUtility: holster preview ended.");
        }

        [MenuItem(DarkMatterGenesisEditorMenus.ResetInvectorWeaponGrips, false, 15)]
        public static void ResetGripsOnSelectedItem()
        {
            ItemData item = Selection.activeObject as ItemData;
            if (item == null)
            {
                EditorUtility.DisplayDialog("Invector Weapon Grip", "Select an ItemData asset to reset.", "OK");
                return;
            }

            Undo.RecordObject(item, "Reset Invector Weapon Grips");
            item.equipSocketName = "RightHand";
            item.heldLocalPosition = Vector3.zero;
            item.heldLocalEuler = Vector3.zero;
            item.useHeldLocalRotation = false;
            item.heldLocalRotation = Quaternion.identity;
            item.heldLocalScale = Vector3.one;
            item.sheatheSocketName = "Spine";
            item.sheathedLocalPosition = new Vector3(0.02f, 0.18f, -0.22f);
            item.sheathedLocalEuler = new Vector3(75f, 90f, 90f);
            item.useSheathedLocalRotation = false;
            item.sheathedLocalRotation = Quaternion.identity;
            item.sheathedLocalScale = Vector3.one;
            EditorUtility.SetDirty(item);
            AssetDatabase.SaveAssets();
            Debug.Log($"InvectorWeaponGripBakeUtility: reset grip fields on '{item.name}'.");
        }

        [MenuItem(DarkMatterGenesisEditorMenus.BakeInvectorDrawnGrip, true)]
        [MenuItem(DarkMatterGenesisEditorMenus.BakeInvectorHolsteredGrip, true)]
        [MenuItem(DarkMatterGenesisEditorMenus.PreviewInvectorHolsteredWeapon, true)]
        [MenuItem(DarkMatterGenesisEditorMenus.EndInvectorHolsterPreview, true)]
        private static bool ValidatePlayMode()
        {
            return EditorApplication.isPlaying;
        }

        public static bool BakeDrawnGrip(bool showDialog)
        {
            if (!TryResolveContext(out PioneerInvectorWeaponBridge bridge, out ItemData item, out string error))
            {
                if (showDialog)
                    EditorUtility.DisplayDialog("Invector Weapon Grip", error, "OK");
                else
                    Debug.LogWarning(error);
                return false;
            }

            GameObject instance = bridge.TryGetWeaponInstance(item);
            if (instance == null || !instance.activeInHierarchy)
            {
                const string message = "Draw the weapon on the hotbar first (weapon key), then pause and bake.";
                if (showDialog)
                    EditorUtility.DisplayDialog("Invector Weapon Grip", message, "OK");
                else
                    Debug.LogWarning(message);
                return false;
            }

            Transform weaponRoot = instance.transform;
            Transform socket = weaponRoot.parent;
            Undo.RecordObject(item, "Bake Drawn Invector Grip");
            item.heldLocalPosition = weaponRoot.localPosition;
            item.heldLocalEuler = weaponRoot.localEulerAngles;
            item.heldLocalScale = weaponRoot.localScale;
            item.useHeldLocalRotation = false;
            ApplySocketName(item, socket, drawn: true);
            EditorUtility.SetDirty(item);
            AssetDatabase.SaveAssets();
            bridge.RefreshEquippedWeapon();

            string summary =
                $"Baked drawn grip for '{item.name}' on socket '{socket?.name}' — " +
                $"pos {FormatVector(item.heldLocalPosition)}, rot {FormatVector(item.heldLocalEuler)}";
            Debug.Log(summary);
            if (showDialog)
                EditorUtility.DisplayDialog("Invector Weapon Grip", summary, "OK");
            return true;
        }

        public static bool BakeHolsteredGrip(bool showDialog)
        {
            if (!TryResolveContext(out PioneerInvectorWeaponBridge bridge, out ItemData item, out string error))
            {
                if (showDialog)
                    EditorUtility.DisplayDialog("Invector Weapon Grip", error, "OK");
                else
                    Debug.LogWarning(error);
                return false;
            }

            if (!bridge.IsHolsterPreviewActive)
                bridge.BeginHolsterPreview(item);

            GameObject instance = bridge.TryGetHolsteredWeaponInstance(item);
            if (instance == null || !instance.activeInHierarchy)
            {
                const string message = "Start holster preview, adjust the hip/back visual slot, pause, then bake.";
                if (showDialog)
                    EditorUtility.DisplayDialog("Invector Weapon Grip", message, "OK");
                else
                    Debug.LogWarning(message);
                return false;
            }

            Transform weaponRoot = instance.transform;
            Transform socket = weaponRoot.parent;
            Undo.RecordObject(item, "Bake Holstered Invector Grip");
            item.sheathedLocalPosition = weaponRoot.localPosition;
            item.sheathedLocalEuler = weaponRoot.localEulerAngles;
            item.sheathedLocalScale = weaponRoot.localScale == Vector3.zero ? Vector3.one : weaponRoot.localScale;
            item.useSheathedLocalRotation = false;
            ApplySocketName(item, socket, drawn: false);
            EditorUtility.SetDirty(item);
            AssetDatabase.SaveAssets();
            PioneerInvectorWeaponBridge.ApplySheathedTransformToInstance(item, instance);

            string summary =
                $"Baked holstered grip for '{item.name}' on socket '{socket?.name}' — " +
                $"pos {FormatVector(item.sheathedLocalPosition)}, rot {FormatVector(item.sheathedLocalEuler)}";
            Debug.Log(summary);
            if (showDialog)
                EditorUtility.DisplayDialog("Invector Weapon Grip", summary, "OK");
            return true;
        }

        private static void ApplySocketName(ItemData item, Transform socket, bool drawn)
        {
            if (socket == null || string.IsNullOrWhiteSpace(socket.name))
                return;

            if (drawn)
            {
                if (socket.name.Equals("defaultHandler", System.StringComparison.OrdinalIgnoreCase))
                    item.equipSocketName = "RightHand";
                else
                    item.equipSocketName = socket.name;
                return;
            }

            item.sheatheSocketName = socket.name;
        }

        private static bool TryResolveContext(
            out PioneerInvectorWeaponBridge bridge,
            out ItemData item,
            out string error)
        {
            bridge = null;
            item = null;
            error = null;

            if (!EditorApplication.isPlaying)
            {
                error = "Enter Play mode with Player_Invector, draw or preview the weapon, then pause before baking.";
                return false;
            }

            bridge = FindPlayerBridge();
            if (bridge == null)
            {
                error = "No PioneerInvectorWeaponBridge found. Use Player_Invector in the scene.";
                return false;
            }

            item = ResolveTargetItem(bridge);
            if (item == null)
            {
                error = "Select an equippable ItemData asset, or draw a weapon on the hotbar.";
                return false;
            }

            if (item.itemType == ItemType.MeleeWeapon)
            {
                error =
                    $"'{item.name}' is a melee weapon. Melee positions are authored directly in the " +
                    "Player_Invector prefab: open the prefab and move its Drawn_/Holstered_ child objects. " +
                    "Baking only applies to ranged weapons.";
                return false;
            }

            if (item.invectorWeaponPrefab == null && item.itemType != ItemType.RangedWeapon)
            {
                error = $"'{item.name}' is not a weapon ItemData.";
                return false;
            }

            return true;
        }

        private static ItemData ResolveTargetItem(PioneerInvectorWeaponBridge bridge)
        {
            if (Selection.activeObject is ItemData selected && selected.IsEquippable)
                return selected;

            EquipmentController equipment = bridge.GetComponent<EquipmentController>();
            if (equipment == null)
                return null;

            if (equipment.IsWeaponDrawn)
                return equipment.DrawnWeaponItem;

            ItemData hotbarItem = equipment.GetHotbarItem(equipment.SelectedHotbarSlot);
            if (hotbarItem != null && hotbarItem.IsEquippable)
                return hotbarItem;

            return null;
        }

        private static PioneerInvectorWeaponBridge FindPlayerBridge()
        {
            PioneerInvectorWeaponBridge[] bridges =
                Object.FindObjectsByType<PioneerInvectorWeaponBridge>(FindObjectsInactive.Exclude);
            return bridges.Length > 0 ? bridges[0] : null;
        }

        private static string FormatVector(Vector3 value) =>
            $"({value.x:F3}, {value.y:F3}, {value.z:F3})";
    }
}
#endif
