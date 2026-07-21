#if UNITY_EDITOR
using Project.Data;
using Project.EditorTools;
using Project.Inventory;
using Project.Player.Invector;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Invector
{
    public class InvectorWeaponGripBakeWindow : EditorWindow
    {
        private ItemData itemOverride;
        private Vector2 scroll;

        public static void Open()
        {
            GetWindow<InvectorWeaponGripBakeWindow>("Invector Weapon Grip");
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);

            EditorGUILayout.LabelField("Invector Weapon Grip Baker", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "MELEE weapons: no baking needed. Open the Player_Invector prefab and move the " +
                "Drawn_<Item> (right hand) and Holstered_<Item> (hip/back) child objects directly — " +
                "the game uses those prefab transforms as-is.\n\n" +
                "RANGED weapons (runtime-spawned) can still be baked:\n" +
                "1. Enter Play mode and START GAME.\n" +
                "2. Put the weapon on a hotbar slot.\n" +
                "3. Drawn: draw weapon, pause, nudge it, Bake Drawn.\n" +
                "4. Holstered: Preview Holstered, pause, nudge it, Bake Holstered.\n" +
                "5. End preview when finished.",
                MessageType.Info);

            EditorGUILayout.Space(8f);
            itemOverride = (ItemData)EditorGUILayout.ObjectField("Item Override", itemOverride, typeof(ItemData), false);

            PioneerInvectorWeaponBridge bridge = EditorApplication.isPlaying
                ? Object.FindAnyObjectByType<PioneerInvectorWeaponBridge>()
                : null;

            ItemData resolvedItem = ResolveItem(bridge);
            EditorGUILayout.LabelField("Target Item", resolvedItem != null ? resolvedItem.name : "(none)");
            EditorGUILayout.LabelField("Play Mode", EditorApplication.isPlaying ? "Active" : "Inactive");
            EditorGUILayout.LabelField("Paused", EditorApplication.isPaused ? "Yes" : "No");

            if (bridge != null)
            {
                EditorGUILayout.LabelField("Holster Preview", bridge.IsHolsterPreviewActive ? "Active" : "Inactive");
                GameObject instance = resolvedItem != null ? bridge.TryGetWeaponInstance(resolvedItem) : null;
                GameObject holstered = resolvedItem != null ? bridge.TryGetHolsteredWeaponInstance(resolvedItem) : null;
                EditorGUILayout.LabelField("Live Weapon Slot", instance != null ? instance.name : "(none)");
                EditorGUILayout.LabelField("Holster Preview Slot", holstered != null ? holstered.name : "(none)");
            }

            EditorGUILayout.Space(12f);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                if (GUILayout.Button("Refresh Player_Invector Melee Slots", GUILayout.Height(28f)))
                    PioneerInvectorPlayerSetupUtility.RefreshPlayerInvectorMeleeSlots();
            }
            if (EditorApplication.isPlaying)
                EditorGUILayout.HelpBox("Slot refresh is available outside Play mode. It adds slots for new melee items and keeps your authored slot positions.", MessageType.None);

            EditorGUILayout.Space(6f);
            using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
            {
                if (GUILayout.Button("Preview Holstered On Player", GUILayout.Height(28f)))
                {
                    if (itemOverride != null)
                        Selection.activeObject = itemOverride;
                    InvectorWeaponGripBakeUtility.PreviewHolsteredMenu();
                }

                if (GUILayout.Button("End Holster Preview", GUILayout.Height(24f)))
                    InvectorWeaponGripBakeUtility.EndHolsterPreviewMenu();

                EditorGUILayout.Space(6f);

                if (GUILayout.Button("Bake Drawn Grip (Live Player)", GUILayout.Height(32f)))
                {
                    if (itemOverride != null)
                        Selection.activeObject = itemOverride;
                    InvectorWeaponGripBakeUtility.BakeDrawnGrip(showDialog: true);
                }

                if (GUILayout.Button("Bake Holstered Grip (Live Player)", GUILayout.Height(32f)))
                {
                    if (itemOverride != null)
                        Selection.activeObject = itemOverride;
                    InvectorWeaponGripBakeUtility.BakeHolsteredGrip(showDialog: true);
                }
            }

            EditorGUILayout.Space(8f);
            if (GUILayout.Button("Reset Grips On Selected ItemData"))
                InvectorWeaponGripBakeUtility.ResetGripsOnSelectedItem();

            EditorGUILayout.EndScrollView();
        }

        private ItemData ResolveItem(PioneerInvectorWeaponBridge bridge)
        {
            if (itemOverride != null)
                return itemOverride;

            if (Selection.activeObject is ItemData selected)
                return selected;

            if (bridge == null)
                return null;

            EquipmentController equipment = bridge.GetComponent<EquipmentController>();
            if (equipment == null)
                return null;

            if (equipment.IsWeaponDrawn)
                return equipment.DrawnWeaponItem;

            return equipment.GetHotbarItem(equipment.SelectedHotbarSlot);
        }
    }
}
#endif
