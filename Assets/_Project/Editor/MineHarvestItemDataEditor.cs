using Project.Data;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Pruned Inspector for mine/harvest yield items — identity, stack, loot visual,
    /// loot-attract / harvest audio, tooltip, and optional XP.
    /// </summary>
    [CustomEditor(typeof(MineHarvestItemData))]
    public class MineHarvestItemDataEditor : Editor
    {
        private SerializedProperty itemName;
        private SerializedProperty icon;
        private SerializedProperty worldPrefab;
        private SerializedProperty maxStack;
        private SerializedProperty gatherKind;
        private SerializedProperty requiredGatherSkillRank;
        private SerializedProperty unknownDisplayName;
        private SerializedProperty lootYieldClip;
        private SerializedProperty lootYieldVolume;
        private SerializedProperty lootGrantClip;
        private SerializedProperty lootGrantVolume;
        private SerializedProperty lootCompleteVfxPrefab;
        private SerializedProperty tooltipDescription;
        private SerializedProperty grantsXp;
        private SerializedProperty xpAmount;
        private SerializedProperty xpSource;
        private SerializedProperty grantXpEveryPickupOrUse;
        private SerializedProperty requiredLevelToEquip;
        private SerializedProperty requiredLevelToCraft;
        private SerializedProperty requiredLevelToUse;
        private SerializedProperty requiredLevelToPickup;

        private void OnEnable()
        {
            itemName = serializedObject.FindProperty("itemName");
            icon = serializedObject.FindProperty("icon");
            worldPrefab = serializedObject.FindProperty("worldPrefab");
            maxStack = serializedObject.FindProperty("maxStack");
            gatherKind = serializedObject.FindProperty("gatherKind");
            requiredGatherSkillRank = serializedObject.FindProperty("requiredGatherSkillRank");
            unknownDisplayName = serializedObject.FindProperty("unknownDisplayName");
            lootYieldClip = serializedObject.FindProperty("lootYieldClip");
            lootYieldVolume = serializedObject.FindProperty("lootYieldVolume");
            lootGrantClip = serializedObject.FindProperty("lootGrantClip");
            lootGrantVolume = serializedObject.FindProperty("lootGrantVolume");
            lootCompleteVfxPrefab = serializedObject.FindProperty("lootCompleteVfxPrefab");
            tooltipDescription = serializedObject.FindProperty("tooltipDescription");
            grantsXp = serializedObject.FindProperty("grantsXp");
            xpAmount = serializedObject.FindProperty("xpAmount");
            xpSource = serializedObject.FindProperty("xpSource");
            grantXpEveryPickupOrUse = serializedObject.FindProperty("grantXpEveryPickupOrUse");
            requiredLevelToEquip = serializedObject.FindProperty("requiredLevelToEquip");
            requiredLevelToCraft = serializedObject.FindProperty("requiredLevelToCraft");
            requiredLevelToUse = serializedObject.FindProperty("requiredLevelToUse");
            requiredLevelToPickup = serializedObject.FindProperty("requiredLevelToPickup");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "Mine / Harvest resource item — inventory yield, loot attract audio, complete VFX, optional XP.\n" +
                "Node timing, waves, tools, and plant hold settings live on Resource Node Definition / ResourceNode prefabs.",
                MessageType.Info);

            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(itemName);
            EditorGUILayout.PropertyField(gatherKind);
            EditorGUILayout.PropertyField(requiredGatherSkillRank);
            EditorGUILayout.PropertyField(unknownDisplayName);
            EditorGUILayout.PropertyField(icon);
            EditorGUILayout.PropertyField(worldPrefab, new GUIContent(
                "Loot / World Prefab",
                "Pickup mesh and fly-to-player loot model."));
            EditorGUILayout.PropertyField(maxStack);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Loot Attract / Harvest Audio", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(lootYieldClip, new GUIContent(
                "Yield Clip",
                "Played at the node when loot starts flying (mine break / plant harvest)."));
            EditorGUILayout.PropertyField(lootYieldVolume);
            EditorGUILayout.PropertyField(lootGrantClip, new GUIContent(
                "Grant Clip",
                "Played when loot reaches the player. Empty uses GameAudioManager item pickup."));
            EditorGUILayout.PropertyField(lootGrantVolume);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Harvest Complete VFX", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(lootCompleteVfxPrefab, new GUIContent(
                "Complete VFX",
                "Spawned at the player when loot arrives and inventory is granted."));

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Tooltip", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(tooltipDescription, GUIContent.none);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Progression", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(grantsXp);
            using (new EditorGUI.DisabledScope(!grantsXp.boolValue))
            {
                EditorGUILayout.PropertyField(xpAmount);
                EditorGUILayout.PropertyField(xpSource);
                EditorGUILayout.PropertyField(
                    grantXpEveryPickupOrUse,
                    new GUIContent(
                        "XP Every Pickup / Use",
                        "On: grant XP each gather/pickup. Off: one-time XP for this item asset."));
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Level Gates", EditorStyles.boldLabel);
            DrawSuggestedXpFromGates();

            if (serializedObject.ApplyModifiedProperties())
            {
                foreach (Object obj in targets)
                {
                    if (obj is MineHarvestItemData item)
                    {
                        item.PruneNonGatherFields();
                        EditorUtility.SetDirty(item);
                    }
                }
            }

            EditorGUILayout.Space(10f);
            if (GUILayout.Button("Prune Non-Gather Fields Now"))
            {
                foreach (Object obj in targets)
                {
                    if (obj is MineHarvestItemData item)
                    {
                        Undo.RecordObject(item, "Prune MineHarvest Item");
                        item.PruneNonGatherFields();
                        EditorUtility.SetDirty(item);
                    }
                }

                serializedObject.Update();
            }
        }

        private void DrawSuggestedXpFromGates()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(
                requiredLevelToEquip,
                new GUIContent("Required Level To Equip", "0 or 1 = no gate."));
            EditorGUILayout.PropertyField(
                requiredLevelToCraft,
                new GUIContent("Required Level To Craft", "0 or 1 = no gate."));
            EditorGUILayout.PropertyField(
                requiredLevelToUse,
                new GUIContent("Required Level To Use", "0 or 1 = no gate."));
            EditorGUILayout.PropertyField(
                requiredLevelToPickup,
                new GUIContent("Required Level To Pickup", "World pickup / loot claim. 0 or 1 = no gate."));
            bool gatesChanged = EditorGUI.EndChangeCheck();

            if (gatesChanged)
            {
                ItemDataXpAuthoringHints.ApplySuggestedXpFromGates(
                    xpAmount,
                    requiredLevelToEquip.intValue,
                    requiredLevelToCraft.intValue,
                    requiredLevelToUse.intValue,
                    requiredLevelToPickup.intValue,
                    grantXpEveryPickupOrUse.boolValue);
            }

            int gate = ItemDataXpAuthoringHints.GetAuthoringGateLevel(
                requiredLevelToEquip.intValue,
                requiredLevelToCraft.intValue,
                requiredLevelToUse.intValue,
                requiredLevelToPickup.intValue);
            bool continuous = grantXpEveryPickupOrUse.boolValue;
            int suggested = ItemDataXpAuthoringHints.GetSuggestedXpAmount(gate, continuous);
            int current = xpAmount.intValue;

            EditorGUILayout.Space(2f);
            EditorGUILayout.HelpBox(
                ItemDataXpAuthoringHints.FormatPreviewLabel(gate, continuous, suggested, current)
                + "\nGate edits auto-write xpAmount. Use Apply to resync after manual XP edits.",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(suggested == current))
                {
                    if (GUILayout.Button("Apply Suggested XP", GUILayout.Width(150f)))
                    {
                        xpAmount.intValue = suggested;
                        if (!grantsXp.boolValue)
                            grantsXp.boolValue = true;
                    }
                }
            }
        }
    }
}
