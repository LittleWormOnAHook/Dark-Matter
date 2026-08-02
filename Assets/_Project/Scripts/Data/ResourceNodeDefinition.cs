using Project.Interaction;
using UnityEngine;

namespace Project.Data
{
    /// <summary>
    /// Authoring data for a world mining or plant harvest node.
    /// Runtime behaviour lives on <see cref="ResourceNode"/> prefabs;
    /// this asset is the roster / factory source of truth for yields, durations, and tools.
    /// </summary>
    [CreateAssetMenu(menuName = "Project/Survival/Resource Node Definition", fileName = "ResourceNode_")]
    public class ResourceNodeDefinition : ScriptableObject
    {
        public enum NodeKind
        {
            Mining = 0,
            Plant = 1
        }

        [Header("Identity")]
        public string displayName = "New Resource Node";
        public NodeKind nodeKind = NodeKind.Mining;
        [TextArea(2, 4)]
        public string designerNotes;

        [Header("Looted Item (inventory)")]
        [Tooltip("Item granted to the player when mining / harvesting this node.")]
        public ItemData resourceItem;
        [Tooltip("Inventory icon for the looted item (written onto ItemData when created).")]
        public Sprite itemIcon;

        [Header("Interaction")]
        public ResourceNodeInteractionMode interactionMode = ResourceNodeInteractionMode.LaserMine;
        [Tooltip("Optional specific tool ItemData. Leave empty for mode defaults (laser mining tool / bare hands).")]
        public ItemData requiredTool;
        [Tooltip("When true, laser-mine nodes only accept equipped isMiningTool weapons.")]
        public bool requireMiningLaser = true;
        [Tooltip("Legacy plant harvest prompt string. Runtime uses proximity dots + map markers; Hold E / F still harvests.")]
        public string holdPromptText = "Hold E — Harvest";

        [Header("Timing")]
        [Tooltip("Seconds of continuous laser work per wave, or Hold-E harvest duration for plant nodes.")]
        public float durationSeconds = 5f;
        [Tooltip("Laser mining waves / passes. Plant harvest uses 1.")]
        [Min(1)]
        public int waves = 1;

        [Header("Yields")]
        [Min(1)]
        public int dropMin = 1;
        [Min(1)]
        public int dropMax = 3;
        [Range(0.1f, 1f)]
        [Tooltip("Multiplies yield on the final laser wave only.")]
        public float lastWaveDropScale = 0.6f;

        [Header("Plant Hold")]
        [Min(0.5f)]
        public float holdInteractRange = 3.5f;

        [Header("World Node + Fly-to-Player Visual")]
        [Tooltip("Mesh / prefab used for the planted ResourceNode in the world (boulder / plant).")]
        public GameObject meshTemplate;
        [Tooltip("Model that flies toward the player before inventory grant. Defaults to resourceItem.worldPrefab.")]
        public GameObject lootFlyModel;
        public Color lootTint = new Color(0.82f, 0.72f, 0.35f, 1f);
        public GameObject nodePrefab;

        [Header("Item Factory Defaults")]
        [Min(1)]
        public int itemMaxStack = 40;
        [TextArea(2, 4)]
        public string itemTooltip;

        [Header("Loot Attract / Harvest Audio")]
        [Tooltip("Written onto MineHarvestItemData.lootYieldClip when creating/updating items.")]
        public AudioClip lootYieldClip;
        [Tooltip("Written onto MineHarvestItemData.lootGrantClip (empty = global item pickup SFX).")]
        public AudioClip lootGrantClip;
    }
}
