using System.Collections.Generic;
using Project.Data;
using Project.Interaction;
using Project.Progression;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Resource Manager — author mined / harvested ItemData assets plus Mining and Plant
    /// ResourceNode prefabs (yields, durations, required tools, node definitions).
    /// Rosters are built dynamically from Mining / Harvest item folders + linked node definitions.
    /// </summary>
    public class ResourceManagerWindow : EditorWindow
    {
        private struct HarvestRosterEntry
        {
            public string Name;
            public int MaxStack;
            public string Tooltip;
            public bool IsPlant;
            public string MeshPath;
            public float Duration;
            public int DropMin;
            public int DropMax;
            public int Waves;
            public float LastWaveScale;
            public Color LootTint;
            /// <summary>Mining / Harvesting skill rank required to identify via multi-tool F-scan.</summary>
            public int RequiredGatherSkillRank;

            public HarvestRosterEntry(
                string name,
                int maxStack,
                string tooltip,
                bool isPlant,
                string meshPath,
                float duration,
                int dropMin,
                int dropMax,
                int waves = 1,
                float lastWaveScale = 1f,
                Color lootTint = default,
                int requiredGatherSkillRank = 1)
            {
                Name = name;
                MaxStack = maxStack;
                Tooltip = tooltip;
                IsPlant = isPlant;
                MeshPath = meshPath;
                Duration = duration;
                DropMin = dropMin;
                DropMax = dropMax;
                Waves = waves;
                LastWaveScale = lastWaveScale;
                LootTint = lootTint.a > 0.01f
                    ? lootTint
                    : (isPlant
                        ? new Color(0.75f, 0.82f, 0.28f, 1f)
                        : new Color(0.82f, 0.72f, 0.35f, 1f));
                RequiredGatherSkillRank = Mathf.Max(1, requiredGatherSkillRank);
            }
        }

        /// <summary>
        /// Seed defaults for known canon resources (mesh / duration / yield) when no
        /// <see cref="ResourceNodeDefinition"/> is present yet. New items use generic defaults.
        /// </summary>
        private static readonly HarvestRosterEntry[] SeedDefaults =
        {
            new HarvestRosterEntry(
                "Silicate Ore", 80,
                "Io silicate rock fragments. Laser-mined from mineral boulders. Used in abrasives, ceramics, and structural crafting.",
                isPlant: false,
                meshPath: ProjectAssetPaths.BoulderNodeTemplate,
                duration: 5f, dropMin: 1, dropMax: 3, waves: 1, lastWaveScale: 0.6f,
                requiredGatherSkillRank: 1),
            new HarvestRosterEntry(
                "Iron Ore", 80,
                "Dense iron-bearing ore. Laser-mined from mineral boulders. Smelted into metal components for weapons and modules.",
                isPlant: false,
                meshPath: ProjectAssetPaths.BoulderNodeTemplate,
                duration: 5f, dropMin: 1, dropMax: 3, waves: 1, lastWaveScale: 0.6f,
                requiredGatherSkillRank: 2),
            new HarvestRosterEntry(
                "Sulfur Needle Tuft", 40,
                "Bristly sulfur-rich plant fiber. Hold-E harvest. Antiseptic reagent for medpacks and salves.",
                isPlant: true,
                meshPath: ProjectAssetPaths.SulfurNeedleTuftGlb,
                duration: 4f, dropMin: 5, dropMax: 10,
                lootTint: new Color(0.75f, 0.82f, 0.28f, 1f),
                requiredGatherSkillRank: 1),
            new HarvestRosterEntry(
                "Brimstone Blade", 20,
                "Fan-like Io plant fronds that seep a valuable brimstone goo. Hold-E harvest for crafting reagents.",
                isPlant: true,
                meshPath: ProjectAssetPaths.BrimstoneFanPlantPrefab,
                duration: 4.5f, dropMin: 2, dropMax: 5,
                lootTint: new Color(0.85f, 0.35f, 0.18f, 1f),
                requiredGatherSkillRank: 2),
        };

        private static readonly string[] MiningPreferredOrder = { "Silicate Ore", "Iron Ore" };
        private static readonly string[] PlantPreferredOrder = { "Sulfur Needle Tuft", "Brimstone Blade" };

        private List<HarvestRosterEntry> miningRosterCache;
        private List<HarvestRosterEntry> plantRosterCache;
        private bool rosterCacheDirty = true;

        private Vector2 scroll;
        private int tab;

        // Shared item authoring
        private string itemName = "New Resource";
        private string assetFileName = string.Empty;
        private int maxStack = 40;
        private Sprite icon;
        private string tooltipDescription = string.Empty;
        private bool createWorldPickupPrefab = true;
        private bool addToItemRegistry = true;
        private GameObject worldPrefabTemplate;
        private bool createNodeDefinition = true;

        // MineHarvestItemData gather fields (Create Item + node forms)
        private AudioClip lootYieldClip;
        private float lootYieldVolume = 0.9f;
        private AudioClip lootGrantClip;
        private float lootGrantVolume = 0.95f;
        private GameObject lootCompleteVfxPrefab;
        private bool grantsXp;
        private int xpAmount = 10;
        private XpSource xpSource = XpSource.SpecialItem;
        private int requiredGatherSkillRank = 1;
        private string unknownDisplayName = "Unknown Resource";

        // Mining node authoring
        private bool showMiningSection = true;
        private ItemData miningYieldItem;
        private ItemData miningRequiredTool;
        private bool miningRequireLaser = true;
        private GameObject miningMeshTemplate;
        private GameObject miningLootFlyModel;
        private int miningWaves = 1;
        private float miningPassDuration = 5f;
        private int miningDropMin = 1;
        private int miningDropMax = 3;
        private float miningLastWaveScale = 0.6f;
        private Color miningLootTint = new Color(0.82f, 0.72f, 0.35f, 1f);

        // Plant node authoring
        private bool showPlantSection = true;
        private ItemData plantYieldItem;
        private ItemData plantRequiredTool;
        private GameObject plantMeshTemplate;
        private GameObject plantLootFlyModel;
        private float plantHoldDuration = 4f;
        private int plantDropMin = 5;
        private int plantDropMax = 10;
        private float plantInteractRange = 3.5f;
        private string plantHoldPrompt = "Hold E — Harvest";
        private Color plantLootTint = new Color(0.75f, 0.82f, 0.28f, 1f);

        private int createItemCategory;
        private string statusMessage = string.Empty;
        private MessageType statusType = MessageType.Info;

        [MenuItem(SurvivalPioneerEditorMenus.ResourceManager, false, 1)]
        public static void Open()
        {
            GetWindow<ResourceManagerWindow>("Resource Manager").minSize = new Vector2(480f, 700f);
        }

        private void OnEnable()
        {
            rosterCacheDirty = true;
            EnsureItemFieldDefaults(isPlant: createItemCategory == 1);

            if (miningRequiredTool == null)
            {
                miningRequiredTool = AssetDatabase.LoadAssetAtPath<ItemData>(ProjectAssetPaths.MiningToolItem);
            }

            if (miningMeshTemplate == null)
                miningMeshTemplate = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectAssetPaths.BoulderNodeTemplate);

            if (plantMeshTemplate == null)
                plantMeshTemplate = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectAssetPaths.SulfurNeedleTuftGlb);
        }

        private void EnsureItemFieldDefaults(bool isPlant)
        {
            if (lootYieldClip == null)
            {
                lootYieldClip = AssetDatabase.LoadAssetAtPath<AudioClip>(
                    isPlant ? ProjectAssetPaths.AudioBreakWood : ProjectAssetPaths.AudioBreakStone);
            }

            if (lootGrantClip == null)
                lootGrantClip = AssetDatabase.LoadAssetAtPath<AudioClip>(ProjectAssetPaths.AudioPickUp);

            if (lootCompleteVfxPrefab == null)
            {
                lootCompleteVfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    ProjectAssetPaths.LootCompleteVfxPrefab);
            }
        }

        private void OnFocus()
        {
            rosterCacheDirty = true;
        }

        private void InvalidateRosterCache()
        {
            rosterCacheDirty = true;
            miningRosterCache = null;
            plantRosterCache = null;
        }

        private List<HarvestRosterEntry> GetMiningRoster()
        {
            EnsureRosterCaches();
            return miningRosterCache;
        }

        private List<HarvestRosterEntry> GetPlantRoster()
        {
            EnsureRosterCaches();
            return plantRosterCache;
        }

        private void EnsureRosterCaches()
        {
            if (!rosterCacheDirty && miningRosterCache != null && plantRosterCache != null)
                return;

            miningRosterCache = BuildRosterFromAssets(isPlant: false);
            plantRosterCache = BuildRosterFromAssets(isPlant: true);
            rosterCacheDirty = false;
        }

        /// <summary>
        /// Scans Mining or Harvest ItemData folders and merges linked ResourceNodeDefinition
        /// yield/duration data. Seed defaults fill gaps for canon resources.
        /// </summary>
        private static List<HarvestRosterEntry> BuildRosterFromAssets(bool isPlant)
        {
            string folder = isPlant
                ? ProjectAssetPaths.ItemsResourcesHarvest
                : ProjectAssetPaths.ItemsResourcesMining;
            CraftingEditorUtility.EnsureFolder(folder);

            Dictionary<string, ResourceNodeDefinition> defsByItemKey = IndexNodeDefinitions(isPlant);
            var byName = new Dictionary<string, HarvestRosterEntry>(System.StringComparer.OrdinalIgnoreCase);

            string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { folder });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
                if (item == null)
                    continue;

                if (item is MineHarvestItemData lean)
                {
                    bool leanIsPlant = lean.gatherKind == MineHarvestGatherKind.Harvest;
                    if (leanIsPlant != isPlant)
                        continue;
                }
                else if (item.itemType != ItemType.Resource)
                {
                    continue;
                }

                string key = string.IsNullOrWhiteSpace(item.itemName) ? item.name : item.itemName.Trim();
                if (string.IsNullOrEmpty(key))
                    continue;

                defsByItemKey.TryGetValue(NormalizeRosterKey(key), out ResourceNodeDefinition def);
                byName[key] = BuildEntryFromItem(item, isPlant, def);
            }

            // Include node definitions whose yield item lives elsewhere / is missing from the scan.
            foreach (KeyValuePair<string, ResourceNodeDefinition> pair in defsByItemKey)
            {
                ResourceNodeDefinition def = pair.Value;
                if (def == null || def.resourceItem == null)
                    continue;

                string key = string.IsNullOrWhiteSpace(def.resourceItem.itemName)
                    ? def.resourceItem.name
                    : def.resourceItem.itemName.Trim();
                if (string.IsNullOrEmpty(key) || byName.ContainsKey(key))
                    continue;

                byName[key] = BuildEntryFromItem(def.resourceItem, isPlant, def);
            }

            // Ensure seed canon entries appear even if assets were deleted mid-authoring.
            for (int i = 0; i < SeedDefaults.Length; i++)
            {
                HarvestRosterEntry seed = SeedDefaults[i];
                if (seed.IsPlant != isPlant)
                    continue;
                if (!byName.ContainsKey(seed.Name))
                    byName[seed.Name] = seed;
            }

            var list = new List<HarvestRosterEntry>(byName.Values);
            SortRoster(list, isPlant ? PlantPreferredOrder : MiningPreferredOrder);
            return list;
        }

        private static Dictionary<string, ResourceNodeDefinition> IndexNodeDefinitions(bool isPlant)
        {
            var map = new Dictionary<string, ResourceNodeDefinition>(System.StringComparer.OrdinalIgnoreCase);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.ItemsNodes);
            string[] guids = AssetDatabase.FindAssets("t:ResourceNodeDefinition", new[] { ProjectAssetPaths.ItemsNodes });
            ResourceNodeDefinition.NodeKind want = isPlant
                ? ResourceNodeDefinition.NodeKind.Plant
                : ResourceNodeDefinition.NodeKind.Mining;

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                ResourceNodeDefinition def = AssetDatabase.LoadAssetAtPath<ResourceNodeDefinition>(path);
                if (def == null || def.nodeKind != want)
                    continue;

                if (def.resourceItem != null)
                {
                    string itemKey = string.IsNullOrWhiteSpace(def.resourceItem.itemName)
                        ? def.resourceItem.name
                        : def.resourceItem.itemName.Trim();
                    if (!string.IsNullOrEmpty(itemKey))
                        map[NormalizeRosterKey(itemKey)] = def;
                }

                if (!string.IsNullOrWhiteSpace(def.displayName))
                    map[NormalizeRosterKey(def.displayName)] = def;
            }

            return map;
        }

        private static HarvestRosterEntry BuildEntryFromItem(
            ItemData item,
            bool isPlant,
            ResourceNodeDefinition def)
        {
            string name = string.IsNullOrWhiteSpace(item.itemName) ? item.name : item.itemName.Trim();
            bool hasSeed = TryGetSeedDefault(name, isPlant, out HarvestRosterEntry seed);

            int maxStack = item.maxStack > 0 ? item.maxStack : (hasSeed ? seed.MaxStack : (isPlant ? 40 : 80));
            string tooltip = !string.IsNullOrEmpty(item.tooltipDescription)
                ? item.tooltipDescription
                : (hasSeed ? seed.Tooltip : string.Empty);

            string meshPath = hasSeed ? seed.MeshPath : DefaultMeshPath(name, isPlant);
            float duration = hasSeed ? seed.Duration : (isPlant ? 4f : 5f);
            int dropMin = hasSeed ? seed.DropMin : (isPlant ? 5 : 1);
            int dropMax = hasSeed ? seed.DropMax : (isPlant ? 10 : 3);
            int waves = hasSeed ? seed.Waves : 1;
            float lastWave = hasSeed ? seed.LastWaveScale : (isPlant ? 1f : 0.6f);
            Color tint = hasSeed && seed.LootTint.a > 0.01f
                ? seed.LootTint
                : default;
            int scanRank = hasSeed ? seed.RequiredGatherSkillRank : 1;
            if (item is MineHarvestItemData leanItem)
                scanRank = Mathf.Max(1, leanItem.requiredGatherSkillRank);

            if (def != null)
            {
                if (def.itemMaxStack > 0)
                    maxStack = def.itemMaxStack;
                if (!string.IsNullOrEmpty(def.itemTooltip))
                    tooltip = def.itemTooltip;
                if (def.durationSeconds > 0.01f)
                    duration = def.durationSeconds;
                if (def.dropMin > 0)
                    dropMin = def.dropMin;
                if (def.dropMax > 0)
                    dropMax = Mathf.Max(dropMin, def.dropMax);
                waves = Mathf.Max(1, def.waves);
                lastWave = Mathf.Clamp(def.lastWaveDropScale, 0.1f, 1f);
                if (def.lootTint.a > 0.01f)
                    tint = def.lootTint;
                if (def.meshTemplate != null)
                {
                    string defMesh = AssetDatabase.GetAssetPath(def.meshTemplate);
                    if (!string.IsNullOrEmpty(defMesh))
                        meshPath = defMesh;
                }
            }

            return new HarvestRosterEntry(
                name, maxStack, tooltip, isPlant, meshPath,
                duration, dropMin, dropMax, waves, lastWave, tint, scanRank);
        }

        private static bool TryGetSeedDefault(string name, bool isPlant, out HarvestRosterEntry seed)
        {
            for (int i = 0; i < SeedDefaults.Length; i++)
            {
                if (SeedDefaults[i].IsPlant == isPlant &&
                    string.Equals(SeedDefaults[i].Name, name, System.StringComparison.OrdinalIgnoreCase))
                {
                    seed = SeedDefaults[i];
                    return true;
                }
            }

            seed = default;
            return false;
        }

        private static string DefaultMeshPath(string name, bool isPlant)
        {
            if (!isPlant)
                return ProjectAssetPaths.BoulderNodeTemplate;

            if (string.Equals(name, "Brimstone Blade", System.StringComparison.OrdinalIgnoreCase))
                return ProjectAssetPaths.BrimstoneFanPlantPrefab;
            if (string.Equals(name, "Sulfur Needle Tuft", System.StringComparison.OrdinalIgnoreCase))
                return ProjectAssetPaths.SulfurNeedleTuftGlb;

            return ProjectAssetPaths.SulfurNeedleTuftGlb;
        }

        private static string NormalizeRosterKey(string name)
        {
            return CraftingEditorUtility.SanitizeAssetName(name ?? string.Empty)
                .Replace(" ", string.Empty)
                .ToLowerInvariant();
        }

        private static void SortRoster(List<HarvestRosterEntry> list, string[] preferredOrder)
        {
            list.Sort((a, b) =>
            {
                int ai = IndexOfPreferred(preferredOrder, a.Name);
                int bi = IndexOfPreferred(preferredOrder, b.Name);
                if (ai >= 0 && bi >= 0)
                    return ai.CompareTo(bi);
                if (ai >= 0)
                    return -1;
                if (bi >= 0)
                    return 1;
                return string.Compare(a.Name, b.Name, System.StringComparison.OrdinalIgnoreCase);
            });
        }

        private static int IndexOfPreferred(string[] preferred, string name)
        {
            for (int i = 0; i < preferred.Length; i++)
            {
                if (string.Equals(preferred[i], name, System.StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return -1;
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);

            EditorGUILayout.LabelField("Resource Manager", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Manage mined / harvested resources only.\n" +
                $"ItemData → {ProjectAssetPaths.ItemsResources}/Mining or /Harvest\n" +
                $"Node definitions → {ProjectAssetPaths.ItemsNodes}\n" +
                $"World nodes → {ProjectAssetPaths.PrefabsWorldResources}\n\n" +
                "Multi-tool F-scan (aim mode, locked on node) identifies resource types before mine/harvest. " +
                "Required Gather Skill Rank gates Mining / Harvesting skill unlocks.",
                MessageType.Info);

            tab = GUILayout.Toolbar(tab, new[] { "Mining Nodes", "Plant Nodes", "Create Item" });
            EditorGUILayout.Space(8f);

            switch (tab)
            {
                case 0:
                    DrawMiningTab();
                    break;
                case 1:
                    DrawPlantTab();
                    break;
                default:
                    DrawCreateItemTab();
                    break;
            }

            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.Space(8f);
                EditorGUILayout.HelpBox(statusMessage, statusType);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawMiningTab()
        {
            showMiningSection = EditorGUILayout.Foldout(showMiningSection, "Mining Resource Roster", true);
            if (showMiningSection)
            {
                EditorGUILayout.HelpBox(
                    "Laser-mined mineral yields from Assets/.../Resources/Mining (+ linked node definitions). " +
                    "Requires the DM Mining Tool (drawn + aim mode). Identify with Hold F scan before mining. " +
                    "Skill: Mining (player level 5+).",
                    MessageType.None);

                List<HarvestRosterEntry> roster = GetMiningRoster();
                if (roster.Count == 0)
                {
                    EditorGUILayout.HelpBox(
                        "No mining items found. Create one below or use Create Item.",
                        MessageType.Warning);
                }

                for (int i = 0; i < roster.Count; i++)
                {
                    HarvestRosterEntry entry = roster[i];
                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.LabelField(entry.Name, EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(
                        $"Stack {entry.MaxStack}  ·  {entry.Duration:0.#}s/wave  ·  yield {entry.DropMin}-{entry.DropMax}  ·  waves {entry.Waves}  ·  scan rank {entry.RequiredGatherSkillRank}",
                        EditorStyles.miniLabel);
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Load Into Form"))
                        ApplyRosterToMiningForm(entry);
                    if (GUILayout.Button("Ensure Item + Definition + Node"))
                        EnsureFullPipeline(entry, isPlant: false);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Refresh Roster"))
                {
                    InvalidateRosterCache();
                    SetStatus("Mining roster refreshed from disk.", MessageType.Info);
                }

                if (GUILayout.Button("Ensure All Mining Roster Assets", GUILayout.Height(28f)))
                {
                    roster = GetMiningRoster();
                    for (int i = 0; i < roster.Count; i++)
                        EnsureFullPipeline(roster[i], isPlant: false, quiet: true);
                    InvalidateRosterCache();
                    SetStatus("Mining roster items, definitions, and boulder nodes ensured.", MessageType.Info);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Create / Update Mining Node", EditorStyles.boldLabel);

            EditorGUILayout.LabelField("Looted Item → Inventory", EditorStyles.boldLabel);
            miningYieldItem = (ItemData)EditorGUILayout.ObjectField(
                new GUIContent("Looted Item", "ItemData added to the player inventory after each completed mine wave."),
                miningYieldItem, typeof(ItemData), false);
            icon = (Sprite)EditorGUILayout.ObjectField(
                new GUIContent("Item Icon", "Inventory / hotbar icon for the looted resource."),
                icon, typeof(Sprite), false);
            itemName = EditorGUILayout.TextField("New Item Name (if creating)", itemName);
            maxStack = EditorGUILayout.IntField("Max Stack", Mathf.Max(1, maxStack));
            EditorGUILayout.LabelField("Tooltip");
            tooltipDescription = EditorGUILayout.TextArea(tooltipDescription ?? string.Empty, GUILayout.MinHeight(36f));
            DrawMineHarvestItemDataFields(isPlant: false);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Tool + World Node", EditorStyles.boldLabel);
            miningRequiredTool = (ItemData)EditorGUILayout.ObjectField(
                new GUIContent("Tool To Mine", "Defaults to DM Mining Tool. Leave empty to accept any isMiningTool."),
                miningRequiredTool, typeof(ItemData), false);
            miningRequireLaser = EditorGUILayout.Toggle("Require Mining Laser", miningRequireLaser);
            miningMeshTemplate = (GameObject)EditorGUILayout.ObjectField(
                new GUIContent("World Node Model", "Boulder / mineral mesh placed in the world."),
                miningMeshTemplate, typeof(GameObject), false);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Fly-To-Player Loot Visual", EditorStyles.boldLabel);
            miningLootFlyModel = (GameObject)EditorGUILayout.ObjectField(
                new GUIContent(
                    "Loot Fly Model",
                    "Model that moves from the node to the player before inventory grant. " +
                    "Empty = use looted item worldPrefab (created with the item)."),
                miningLootFlyModel, typeof(GameObject), false);
            miningLootTint = EditorGUILayout.ColorField("Loot Tint (orb fallback)", miningLootTint);

            EditorGUILayout.Space(6f);
            miningPassDuration = EditorGUILayout.FloatField("Mine Duration / Wave (s)", Mathf.Max(0.05f, miningPassDuration));
            miningWaves = EditorGUILayout.IntSlider("Waves", miningWaves, 1, 5);
            miningDropMin = EditorGUILayout.IntField("Yield Min / Wave", Mathf.Max(1, miningDropMin));
            miningDropMax = EditorGUILayout.IntField("Yield Max / Wave", Mathf.Max(miningDropMin, miningDropMax));
            miningLastWaveScale = EditorGUILayout.Slider("Last Wave Yield Scale", miningLastWaveScale, 0.1f, 1f);
            createNodeDefinition = EditorGUILayout.Toggle("Write Node Definition Asset", createNodeDefinition);
            createWorldPickupPrefab = EditorGUILayout.Toggle(
                new GUIContent("Create Item World Prefab", "Also builds Items/World pickup used as default fly model."),
                createWorldPickupPrefab);
            addToItemRegistry = EditorGUILayout.Toggle("Add To Item Registry", addToItemRegistry);

            EditorGUILayout.Space(6f);
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(miningYieldItem == null && string.IsNullOrWhiteSpace(itemName)))
            {
                if (GUILayout.Button("Create Mining ItemData", GUILayout.Height(32f)))
                {
                    if (miningYieldItem == null)
                        ApplyMiningFormToItemFields();
                    CreateHarvestItem(isPlant: false);
                }
            }

            using (new EditorGUI.DisabledScope(miningYieldItem == null && string.IsNullOrWhiteSpace(itemName)))
            {
                if (GUILayout.Button("Create Mining Node Prefab", GUILayout.Height(32f)))
                    CreateMiningNodePrefab();
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Create Item + Definition + Mining Node", GUILayout.Height(36f)))
            {
                if (miningYieldItem == null)
                {
                    ApplyMiningFormToItemFields();
                    miningYieldItem = CreateHarvestItem(isPlant: false, quiet: true);
                }

                CreateMiningNodePrefab();
            }
        }

        private void DrawPlantTab()
        {
            showPlantSection = EditorGUILayout.Foldout(showPlantSection, "Plant Resource Roster", true);
            if (showPlantSection)
            {
                EditorGUILayout.HelpBox(
                    "Hold-E harvested flora from Assets/.../Resources/Harvest (+ linked node definitions). " +
                    "Identify with multi-tool Hold F scan (aim mode) before harvest. Skill: Harvesting (player level 5+). " +
                    "Proximity dots + map markers after placement.",
                    MessageType.None);

                List<HarvestRosterEntry> roster = GetPlantRoster();
                if (roster.Count == 0)
                {
                    EditorGUILayout.HelpBox(
                        "No plant items found. Create one below or use Create Item.",
                        MessageType.Warning);
                }

                for (int i = 0; i < roster.Count; i++)
                {
                    HarvestRosterEntry entry = roster[i];
                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.LabelField(entry.Name, EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(
                        $"Stack {entry.MaxStack}  ·  hold {entry.Duration:0.#}s  ·  yield {entry.DropMin}-{entry.DropMax}  ·  scan rank {entry.RequiredGatherSkillRank}",
                        EditorStyles.miniLabel);
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Load Into Form"))
                        ApplyRosterToPlantForm(entry);
                    if (GUILayout.Button("Ensure Item + Definition + Node"))
                        EnsureFullPipeline(entry, isPlant: true);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Refresh Roster"))
                {
                    InvalidateRosterCache();
                    SetStatus("Plant roster refreshed from disk.", MessageType.Info);
                }

                if (GUILayout.Button("Ensure All Plant Roster Assets", GUILayout.Height(28f)))
                {
                    roster = GetPlantRoster();
                    for (int i = 0; i < roster.Count; i++)
                        EnsureFullPipeline(roster[i], isPlant: true, quiet: true);
                    InvalidateRosterCache();
                    SetStatus("Plant roster items, definitions, and harvest nodes ensured.", MessageType.Info);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Create / Update Plant Node", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Harvest input is Hold E (gamepad West). F is reserved for multi-tool resource scan. " +
                "Nearby plants show floating dots and register map markers (map-scanner gated). " +
                "Plant must be F-scanned / identified before Hold-E harvest.",
                MessageType.None);

            EditorGUILayout.LabelField("Looted Item → Inventory", EditorStyles.boldLabel);
            plantYieldItem = (ItemData)EditorGUILayout.ObjectField(
                new GUIContent("Looted Item", "ItemData added to the player inventory after Hold-E harvest completes."),
                plantYieldItem, typeof(ItemData), false);
            icon = (Sprite)EditorGUILayout.ObjectField(
                new GUIContent("Item Icon", "Inventory / hotbar icon for the looted resource."),
                icon, typeof(Sprite), false);
            itemName = EditorGUILayout.TextField("New Item Name (if creating)", itemName);
            maxStack = EditorGUILayout.IntField("Max Stack", Mathf.Max(1, maxStack));
            EditorGUILayout.LabelField("Tooltip");
            tooltipDescription = EditorGUILayout.TextArea(tooltipDescription ?? string.Empty, GUILayout.MinHeight(36f));
            DrawMineHarvestItemDataFields(isPlant: true);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Tool + World Plant", EditorStyles.boldLabel);
            plantRequiredTool = (ItemData)EditorGUILayout.ObjectField(
                new GUIContent("Tool To Harvest", "Leave empty for bare-hands Hold E harvest."),
                plantRequiredTool, typeof(ItemData), false);
            plantMeshTemplate = (GameObject)EditorGUILayout.ObjectField(
                new GUIContent("World Plant Model", "Plant mesh / prefab placed in the world."),
                plantMeshTemplate, typeof(GameObject), false);
            plantHoldPrompt = EditorGUILayout.TextField(
                new GUIContent(
                    "Harvest Label (legacy)",
                    "Stored on the node/definition for tooling. Player UX uses proximity dots + map markers, not this prompt."),
                plantHoldPrompt);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Fly-To-Player Loot Visual", EditorStyles.boldLabel);
            plantLootFlyModel = (GameObject)EditorGUILayout.ObjectField(
                new GUIContent(
                    "Loot Fly Model",
                    "Model that moves from the plant to the player before inventory grant. " +
                    "Empty = use looted item worldPrefab."),
                plantLootFlyModel, typeof(GameObject), false);
            plantLootTint = EditorGUILayout.ColorField("Loot Tint (orb fallback)", plantLootTint);

            EditorGUILayout.Space(6f);
            plantHoldDuration = EditorGUILayout.Slider("Hold Duration (s)", plantHoldDuration, 1f, 12f);
            plantDropMin = EditorGUILayout.IntField("Yield Min", Mathf.Max(1, plantDropMin));
            plantDropMax = EditorGUILayout.IntField("Yield Max", Mathf.Max(plantDropMin, plantDropMax));
            plantInteractRange = EditorGUILayout.FloatField("Interact Range", Mathf.Max(0.5f, plantInteractRange));
            createNodeDefinition = EditorGUILayout.Toggle("Write Node Definition Asset", createNodeDefinition);
            createWorldPickupPrefab = EditorGUILayout.Toggle(
                new GUIContent("Create Item World Prefab", "Also builds Items/World pickup used as default fly model."),
                createWorldPickupPrefab);
            addToItemRegistry = EditorGUILayout.Toggle("Add To Item Registry", addToItemRegistry);

            EditorGUILayout.Space(6f);
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(plantYieldItem == null && string.IsNullOrWhiteSpace(itemName)))
            {
                if (GUILayout.Button("Create Plant ItemData", GUILayout.Height(32f)))
                {
                    if (plantYieldItem == null)
                        ApplyPlantFormToItemFields();
                    CreateHarvestItem(isPlant: true);
                }
            }

            using (new EditorGUI.DisabledScope(plantYieldItem == null && string.IsNullOrWhiteSpace(itemName)))
            {
                if (GUILayout.Button("Create Plant Node Prefab", GUILayout.Height(32f)))
                    CreatePlantNodePrefab();
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Create Item + Definition + Plant Node", GUILayout.Height(36f)))
            {
                if (plantYieldItem == null)
                {
                    ApplyPlantFormToItemFields();
                    plantYieldItem = CreateHarvestItem(isPlant: true, quiet: true);
                }

                CreatePlantNodePrefab();
            }
        }

        private void DrawCreateItemTab()
        {
            EditorGUILayout.HelpBox(
                "Create a mined / harvested Resource ItemData only (no operational fuels or scrap).\n" +
                "Gather fields: icon, stack, tooltip, scan skill rank, unknown label, audio, complete VFX, XP.",
                MessageType.None);

            createItemCategory = EditorGUILayout.Popup(
                "Category / Gather Kind",
                createItemCategory,
                new[] { "Mining", "Plant / Harvest" });
            bool plantCategory = createItemCategory == 1;

            itemName = EditorGUILayout.TextField("Item Name", itemName);
            assetFileName = EditorGUILayout.TextField(
                "Asset File Name",
                string.IsNullOrEmpty(assetFileName) ? CraftingEditorUtility.SanitizeAssetName(itemName) : assetFileName);
            maxStack = EditorGUILayout.IntField("Max Stack", Mathf.Max(1, maxStack));
            icon = (Sprite)EditorGUILayout.ObjectField("Icon", icon, typeof(Sprite), false);
            EditorGUILayout.LabelField("Tooltip");
            tooltipDescription = EditorGUILayout.TextArea(tooltipDescription ?? string.Empty, GUILayout.MinHeight(48f));

            DrawMineHarvestItemDataFields(isPlant: plantCategory);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("World / Loot Prefab", EditorStyles.boldLabel);
            createWorldPickupPrefab = EditorGUILayout.Toggle("Create World Pickup Prefab", createWorldPickupPrefab);
            using (new EditorGUI.DisabledScope(!createWorldPickupPrefab))
            {
                worldPrefabTemplate = (GameObject)EditorGUILayout.ObjectField(
                    new GUIContent("Mesh Template", "Source mesh for Items/World pickup used as fly-to-player loot model."),
                    worldPrefabTemplate, typeof(GameObject), false);
            }

            addToItemRegistry = EditorGUILayout.Toggle("Add To Item Registry", addToItemRegistry);

            EditorGUILayout.Space(8f);
            if (GUILayout.Button("Create Resource ItemData", GUILayout.Height(40f)))
                CreateHarvestItem(isPlant: plantCategory);
        }

        /// <summary>
        /// Shared MineHarvestItemData gather fields (scan gate, audio, complete VFX, optional XP).
        /// </summary>
        private void DrawMineHarvestItemDataFields(bool isPlant)
        {
            EnsureItemFieldDefaults(isPlant);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Multi-Tool Resource Scan", EditorStyles.boldLabel);
            requiredGatherSkillRank = EditorGUILayout.IntField(
                new GUIContent(
                    "Required Gather Skill Rank",
                    isPlant
                        ? "Harvesting skill rank needed to identify this plant with Hold F scan."
                        : "Mining skill rank needed to identify this ore with Hold F scan."),
                Mathf.Max(1, requiredGatherSkillRank));
            unknownDisplayName = EditorGUILayout.TextField(
                new GUIContent(
                    "Unknown Display Name",
                    "Label shown on the node before the resource type is identified."),
                string.IsNullOrWhiteSpace(unknownDisplayName) ? "Unknown Resource" : unknownDisplayName);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Loot Attract / Harvest Audio", EditorStyles.boldLabel);
            lootYieldClip = (AudioClip)EditorGUILayout.ObjectField(
                new GUIContent("Yield Clip", "Played at the node when loot starts flying."),
                lootYieldClip, typeof(AudioClip), false);
            lootYieldVolume = EditorGUILayout.Slider("Yield Volume", lootYieldVolume, 0f, 1f);
            lootGrantClip = (AudioClip)EditorGUILayout.ObjectField(
                new GUIContent("Grant Clip", "Played when loot reaches the player. Empty = global pickup SFX."),
                lootGrantClip, typeof(AudioClip), false);
            lootGrantVolume = EditorGUILayout.Slider("Grant Volume", lootGrantVolume, 0f, 1f);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Harvest Complete VFX", EditorStyles.boldLabel);
            lootCompleteVfxPrefab = (GameObject)EditorGUILayout.ObjectField(
                new GUIContent(
                    "Complete VFX",
                    "One-shot at the player when loot arrives and inventory is granted."),
                lootCompleteVfxPrefab, typeof(GameObject), false);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Gather XP (optional)", EditorStyles.boldLabel);
            grantsXp = EditorGUILayout.Toggle(
                new GUIContent("Grants XP", "When true, inventory grant also awards XP (special gather yields)."),
                grantsXp);
            using (new EditorGUI.DisabledScope(!grantsXp))
            {
                xpAmount = EditorGUILayout.IntField("XP Amount", Mathf.Max(0, xpAmount));
                xpSource = (XpSource)EditorGUILayout.EnumPopup("XP Source", xpSource);
            }
        }

        private void ApplyMineHarvestFieldsFromItem(ItemData item, bool isPlant)
        {
            if (item == null)
            {
                EnsureItemFieldDefaults(isPlant);
                return;
            }

            icon = item.icon != null ? item.icon : icon;
            maxStack = item.maxStack;
            if (!string.IsNullOrEmpty(item.tooltipDescription))
                tooltipDescription = item.tooltipDescription;
            grantsXp = item.grantsXp;
            xpAmount = item.xpAmount;
            xpSource = item.xpSource;

            if (item is MineHarvestItemData lean)
            {
                requiredGatherSkillRank = Mathf.Max(1, lean.requiredGatherSkillRank);
                if (!string.IsNullOrWhiteSpace(lean.unknownDisplayName))
                    unknownDisplayName = lean.unknownDisplayName;
                if (lean.lootYieldClip != null)
                    lootYieldClip = lean.lootYieldClip;
                lootYieldVolume = lean.lootYieldVolume;
                if (lean.lootGrantClip != null)
                    lootGrantClip = lean.lootGrantClip;
                lootGrantVolume = lean.lootGrantVolume;
                if (lean.lootCompleteVfxPrefab != null)
                    lootCompleteVfxPrefab = lean.lootCompleteVfxPrefab;
            }

            EnsureItemFieldDefaults(isPlant);
        }

        private void ApplyRosterToMiningForm(HarvestRosterEntry entry)
        {
            itemName = entry.Name;
            assetFileName = CraftingEditorUtility.SanitizeAssetName(entry.Name);
            maxStack = entry.MaxStack;
            tooltipDescription = entry.Tooltip;
            miningPassDuration = entry.Duration;
            miningWaves = entry.Waves;
            miningDropMin = entry.DropMin;
            miningDropMax = entry.DropMax;
            miningLastWaveScale = entry.LastWaveScale;
            miningLootTint = entry.LootTint;
            miningMeshTemplate = AssetDatabase.LoadAssetAtPath<GameObject>(entry.MeshPath);
            miningYieldItem = LoadResourceItem(entry.Name, isPlant: false);
            requiredGatherSkillRank = Mathf.Max(1, entry.RequiredGatherSkillRank);
            unknownDisplayName = "Unknown Resource";

            ResourceNodeDefinition def = FindNodeDefinitionForItemName(entry.Name, isPlant: false);
            if (def != null)
            {
                if (def.durationSeconds > 0.01f)
                    miningPassDuration = def.durationSeconds;
                miningWaves = Mathf.Clamp(def.waves, 1, 5);
                if (def.dropMin > 0)
                    miningDropMin = def.dropMin;
                if (def.dropMax > 0)
                    miningDropMax = Mathf.Max(miningDropMin, def.dropMax);
                miningLastWaveScale = Mathf.Clamp(def.lastWaveDropScale, 0.1f, 1f);
                if (def.lootTint.a > 0.01f)
                    miningLootTint = def.lootTint;
                if (def.meshTemplate != null)
                    miningMeshTemplate = def.meshTemplate;
                if (def.lootFlyModel != null)
                    miningLootFlyModel = def.lootFlyModel;
                if (def.requiredTool != null)
                    miningRequiredTool = def.requiredTool;
                miningRequireLaser = def.requireMiningLaser;
                if (def.itemMaxStack > 0)
                    maxStack = def.itemMaxStack;
                if (!string.IsNullOrEmpty(def.itemTooltip))
                    tooltipDescription = def.itemTooltip;
                if (def.itemIcon != null)
                    icon = def.itemIcon;
                if (def.resourceItem != null)
                    miningYieldItem = def.resourceItem;
            }

            if (miningYieldItem != null)
            {
                ApplyMineHarvestFieldsFromItem(miningYieldItem, isPlant: false);
                if (miningLootFlyModel == null)
                    miningLootFlyModel = miningYieldItem.worldPrefab;
            }
            else if (def == null || def.lootFlyModel == null)
            {
                miningLootFlyModel = null;
                EnsureItemFieldDefaults(isPlant: false);
            }

            SetStatus($"Loaded mining roster '{entry.Name}'.", MessageType.Info);
            GUI.FocusControl(null);
        }

        private void ApplyRosterToPlantForm(HarvestRosterEntry entry)
        {
            itemName = entry.Name;
            assetFileName = CraftingEditorUtility.SanitizeAssetName(entry.Name);
            maxStack = entry.MaxStack;
            tooltipDescription = entry.Tooltip;
            plantHoldDuration = entry.Duration;
            plantDropMin = entry.DropMin;
            plantDropMax = entry.DropMax;
            plantLootTint = entry.LootTint;
            plantMeshTemplate = AssetDatabase.LoadAssetAtPath<GameObject>(entry.MeshPath);
            plantYieldItem = LoadResourceItem(entry.Name, isPlant: true);
            plantHoldPrompt = $"Hold E — Harvest {entry.Name}";
            requiredGatherSkillRank = Mathf.Max(1, entry.RequiredGatherSkillRank);
            unknownDisplayName = "Unknown Resource";

            ResourceNodeDefinition def = FindNodeDefinitionForItemName(entry.Name, isPlant: true);
            if (def != null)
            {
                if (def.durationSeconds > 0.01f)
                    plantHoldDuration = def.durationSeconds;
                if (def.dropMin > 0)
                    plantDropMin = def.dropMin;
                if (def.dropMax > 0)
                    plantDropMax = Mathf.Max(plantDropMin, def.dropMax);
                if (def.lootTint.a > 0.01f)
                    plantLootTint = def.lootTint;
                if (def.meshTemplate != null)
                    plantMeshTemplate = def.meshTemplate;
                if (def.lootFlyModel != null)
                    plantLootFlyModel = def.lootFlyModel;
                plantRequiredTool = def.requiredTool;
                if (!string.IsNullOrWhiteSpace(def.holdPromptText))
                    plantHoldPrompt = def.holdPromptText;
                if (def.holdInteractRange > 0.01f)
                    plantInteractRange = def.holdInteractRange;
                if (def.itemMaxStack > 0)
                    maxStack = def.itemMaxStack;
                if (!string.IsNullOrEmpty(def.itemTooltip))
                    tooltipDescription = def.itemTooltip;
                if (def.itemIcon != null)
                    icon = def.itemIcon;
                if (def.resourceItem != null)
                    plantYieldItem = def.resourceItem;
            }

            if (plantYieldItem != null)
            {
                ApplyMineHarvestFieldsFromItem(plantYieldItem, isPlant: true);
                if (plantLootFlyModel == null)
                    plantLootFlyModel = plantYieldItem.worldPrefab;
            }
            else if (def == null || def.lootFlyModel == null)
            {
                plantLootFlyModel = null;
                EnsureItemFieldDefaults(isPlant: true);
            }

            SetStatus($"Loaded plant roster '{entry.Name}'.", MessageType.Info);
            GUI.FocusControl(null);
        }

        private void ApplyMiningFormToItemFields()
        {
            if (string.IsNullOrWhiteSpace(itemName) && miningYieldItem != null)
                itemName = miningYieldItem.itemName;
        }

        private void ApplyPlantFormToItemFields()
        {
            if (string.IsNullOrWhiteSpace(itemName) && plantYieldItem != null)
                itemName = plantYieldItem.itemName;
        }

        private void EnsureFullPipeline(HarvestRosterEntry entry, bool isPlant, bool quiet = false)
        {
            if (isPlant)
                ApplyRosterToPlantForm(entry);
            else
                ApplyRosterToMiningForm(entry);

            ItemData item = CreateHarvestItem(isPlant, quiet: true);
            if (isPlant)
            {
                plantYieldItem = item;
                CreatePlantNodePrefab(quiet: true);
            }
            else
            {
                miningYieldItem = item;
                CreateMiningNodePrefab(quiet: true);
            }

            if (!quiet)
                SetStatus($"Ensured pipeline for '{entry.Name}'.", MessageType.Info);
        }

        private ItemData CreateHarvestItem(bool isPlant, bool quiet = false)
        {
            string safeName = CraftingEditorUtility.SanitizeAssetName(
                string.IsNullOrWhiteSpace(assetFileName) ? itemName : assetFileName);
            if (string.IsNullOrEmpty(safeName))
            {
                if (!quiet)
                    EditorUtility.DisplayDialog("Resource Manager", "Enter a valid item or file name.", "OK");
                return null;
            }

            string folder = isPlant ? ProjectAssetPaths.ItemsResourcesHarvest : ProjectAssetPaths.ItemsResourcesMining;
            CraftingEditorUtility.EnsureFolder(folder);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsItemsWorld);

            string dataPath = $"{folder}/{safeName}.asset";
            // Migrate from flat Resources folder if needed.
            string legacyPath = $"{ProjectAssetPaths.ItemsResources}/{safeName}.asset";
            MineHarvestItemData item = AssetDatabase.LoadAssetAtPath<MineHarvestItemData>(dataPath);
            if (item == null)
            {
                ItemData legacy = AssetDatabase.LoadAssetAtPath<ItemData>(legacyPath)
                                 ?? AssetDatabase.LoadAssetAtPath<ItemData>(dataPath);
                if (legacy != null)
                {
                    string legacyAssetPath = AssetDatabase.GetAssetPath(legacy);
                    if (!(legacy is MineHarvestItemData))
                        MineHarvestItemMigrator.MigrateAtPath(legacyAssetPath, isPlant);

                    if (legacyAssetPath != dataPath && AssetDatabase.LoadAssetAtPath<Object>(legacyAssetPath) != null)
                    {
                        string err = AssetDatabase.MoveAsset(legacyAssetPath, dataPath);
                        if (!string.IsNullOrEmpty(err))
                            Debug.LogWarning($"Resource Manager: move failed ({err}); loading legacy path.");
                    }

                    item = AssetDatabase.LoadAssetAtPath<MineHarvestItemData>(dataPath)
                           ?? AssetDatabase.LoadAssetAtPath<MineHarvestItemData>(legacyAssetPath);
                }
            }

            if (item == null)
            {
                if (!quiet && AssetDatabase.LoadAssetAtPath<ItemData>(dataPath) != null &&
                    !EditorUtility.DisplayDialog("Resource Manager", $"Item '{safeName}' exists. Overwrite?", "Overwrite", "Cancel"))
                {
                    return AssetDatabase.LoadAssetAtPath<ItemData>(dataPath);
                }

                item = ScriptableObject.CreateInstance<MineHarvestItemData>();
                AssetDatabase.CreateAsset(item, dataPath);
            }

            item.itemName = string.IsNullOrWhiteSpace(itemName) ? safeName : itemName.Trim();
            item.itemType = ItemType.Resource;
            item.gatherKind = isPlant ? MineHarvestGatherKind.Harvest : MineHarvestGatherKind.Mining;
            item.maxStack = maxStack;
            item.requiredGatherSkillRank = Mathf.Max(1, requiredGatherSkillRank);
            item.unknownDisplayName = string.IsNullOrWhiteSpace(unknownDisplayName)
                ? "Unknown Resource"
                : unknownDisplayName.Trim();
            if (icon != null)
                item.icon = icon;
            if (!string.IsNullOrWhiteSpace(tooltipDescription))
                item.tooltipDescription = tooltipDescription;

            EnsureItemFieldDefaults(isPlant);
            item.lootYieldClip = lootYieldClip;
            item.lootYieldVolume = Mathf.Clamp01(lootYieldVolume);
            item.lootGrantClip = lootGrantClip;
            item.lootGrantVolume = Mathf.Clamp01(lootGrantVolume);
            item.lootCompleteVfxPrefab = lootCompleteVfxPrefab;
            item.grantsXp = grantsXp;
            item.xpAmount = Mathf.Max(0, xpAmount);
            item.xpSource = xpSource;
            MineHarvestItemMigrator.AssignGatherDefaults(item, isPlant);
            // Keep form fields in sync with any defaults the migrator filled.
            lootYieldClip = item.lootYieldClip;
            lootGrantClip = item.lootGrantClip;
            lootCompleteVfxPrefab = item.lootCompleteVfxPrefab;
            item.PruneNonGatherFields();
            EditorUtility.SetDirty(item);

            if (createWorldPickupPrefab)
            {
                GameObject template = worldPrefabTemplate;
                if (template == null)
                {
                    template = isPlant
                        ? (plantMeshTemplate != null
                            ? plantMeshTemplate
                            : AssetDatabase.LoadAssetAtPath<GameObject>(PlantRosterMeshFor(item.itemName)))
                        : (miningMeshTemplate != null
                            ? miningMeshTemplate
                            : AssetDatabase.LoadAssetAtPath<GameObject>(ProjectAssetPaths.BoulderNodeTemplate));
                }

                if (template != null)
                    item.worldPrefab = BuildWorldPickupPrefab(item, safeName, template);
            }

            if (addToItemRegistry)
                CraftingEditorUtility.AddItemToRegistry(item);

            // Keep manager fly-model fields in sync with the item's world pickup mesh.
            if (isPlant)
            {
                plantYieldItem = item;
                if (plantLootFlyModel == null)
                    plantLootFlyModel = item.worldPrefab;
            }
            else
            {
                miningYieldItem = item;
                if (miningLootFlyModel == null)
                    miningLootFlyModel = item.worldPrefab;
            }

            AssetDatabase.SaveAssets();
            InvalidateRosterCache();
            if (!quiet)
            {
                Selection.activeObject = item;
                EditorGUIUtility.PingObject(item);
                SetStatus($"Created / updated resource item '{item.itemName}' at {dataPath}.", MessageType.Info);
            }

            return item;
        }

        private static string PlantRosterMeshFor(string name)
        {
            if (TryGetSeedDefault(name, isPlant: true, out HarvestRosterEntry seed) &&
                !string.IsNullOrEmpty(seed.MeshPath))
                return seed.MeshPath;

            ResourceNodeDefinition def = FindNodeDefinitionForItemName(name, isPlant: true);
            if (def != null && def.meshTemplate != null)
            {
                string path = AssetDatabase.GetAssetPath(def.meshTemplate);
                if (!string.IsNullOrEmpty(path))
                    return path;
            }

            return DefaultMeshPath(name, isPlant: true);
        }

        private static ResourceNodeDefinition FindNodeDefinitionForItemName(string displayName, bool isPlant)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return null;

            string safe = CraftingEditorUtility.SanitizeAssetName(displayName).Replace(" ", string.Empty);
            ResourceNodeDefinition def = AssetDatabase.LoadAssetAtPath<ResourceNodeDefinition>(
                $"{ProjectAssetPaths.ItemsNodes}/ResourceNode_{safe}.asset");
            if (def != null)
            {
                ResourceNodeDefinition.NodeKind want = isPlant
                    ? ResourceNodeDefinition.NodeKind.Plant
                    : ResourceNodeDefinition.NodeKind.Mining;
                if (def.nodeKind == want)
                    return def;
            }

            Dictionary<string, ResourceNodeDefinition> map = IndexNodeDefinitions(isPlant);
            map.TryGetValue(NormalizeRosterKey(displayName), out def);
            return def;
        }

        private GameObject BuildWorldPickupPrefab(ItemData item, string safeName, GameObject template)
        {
            GameObject instance = Object.Instantiate(template);
            instance.name = safeName + "_World";

            ResourceNode[] nodes = instance.GetComponentsInChildren<ResourceNode>(true);
            for (int i = 0; i < nodes.Length; i++)
                Object.DestroyImmediate(nodes[i]);

            MeshCollider[] meshCols = instance.GetComponentsInChildren<MeshCollider>(true);
            BoxCollider box = instance.GetComponent<BoxCollider>();
            if (box == null)
                box = instance.AddComponent<BoxCollider>();
            box.isTrigger = true;
            Renderer rend = instance.GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                Bounds b = rend.bounds;
                box.center = instance.transform.InverseTransformPoint(b.center);
                Vector3 lossy = instance.transform.lossyScale;
                box.size = new Vector3(
                    SafeDiv(b.size.x, lossy.x),
                    SafeDiv(b.size.y, lossy.y),
                    SafeDiv(b.size.z, lossy.z));
            }

            for (int i = 0; i < meshCols.Length; i++)
                Object.DestroyImmediate(meshCols[i]);

            ItemPickup pickup = instance.GetComponent<ItemPickup>();
            if (pickup == null)
                pickup = instance.AddComponent<ItemPickup>();
            pickup.itemData = item;
            pickup.amount = 1;
            pickup.promptText = "Press E to pick up";

            string prefabPath = $"{ProjectAssetPaths.PrefabsItemsWorld}/{safeName}_World.prefab";
            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Object.DestroyImmediate(instance);
            return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }

        private void CreateMiningNodePrefab(bool quiet = false)
        {
            ItemData yield = miningYieldItem;
            if (yield == null)
            {
                ApplyMiningFormToItemFields();
                yield = CreateHarvestItem(isPlant: false, quiet: true);
                miningYieldItem = yield;
            }

            if (yield == null)
            {
                if (!quiet)
                    EditorUtility.DisplayDialog("Resource Manager", "Assign or create a mining yield item first.", "OK");
                return;
            }

            GameObject template = miningMeshTemplate != null
                ? miningMeshTemplate
                : AssetDatabase.LoadAssetAtPath<GameObject>(ProjectAssetPaths.BoulderNodeTemplate);
            if (template == null)
            {
                if (!quiet)
                    EditorUtility.DisplayDialog("Resource Manager", $"Missing boulder template:\n{ProjectAssetPaths.BoulderNodeTemplate}", "OK");
                return;
            }

            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsWorldResources);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(template);
            if (instance == null)
                instance = Object.Instantiate(template);

            string oreKey = CraftingEditorUtility.SanitizeAssetName(yield.itemName).Replace(" ", string.Empty);
            instance.name = $"ResourceNode_Boulder_{oreKey}";

            ResourceNode node = instance.GetComponent<ResourceNode>();
            if (node == null)
                node = instance.AddComponent<ResourceNode>();

            ApplyMiningFieldsToNode(node, yield);
            EnsureMineralMeshCollider(instance);

            string prefabPath = $"{ProjectAssetPaths.PrefabsWorldResources}/ResourceNode_Boulder_{oreKey}.prefab";
            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Object.DestroyImmediate(instance);

            GameObject saved = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (createNodeDefinition)
                WriteNodeDefinition(yield, isPlant: false, saved);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            InvalidateRosterCache();

            if (!quiet)
            {
                Selection.activeObject = saved;
                EditorGUIUtility.PingObject(saved);
                SetStatus($"Created mining node:\n{prefabPath}", MessageType.Info);
            }
        }

        private void CreatePlantNodePrefab(bool quiet = false)
        {
            ItemData yield = plantYieldItem;
            if (yield == null)
            {
                ApplyPlantFormToItemFields();
                yield = CreateHarvestItem(isPlant: true, quiet: true);
                plantYieldItem = yield;
            }

            if (yield == null)
            {
                if (!quiet)
                    EditorUtility.DisplayDialog("Resource Manager", "Assign or create a plant yield item first.", "OK");
                return;
            }

            GameObject plantSource = plantMeshTemplate != null
                ? plantMeshTemplate
                : AssetDatabase.LoadAssetAtPath<GameObject>(PlantRosterMeshFor(yield.itemName));
            if (plantSource == null)
            {
                if (!quiet)
                    EditorUtility.DisplayDialog("Resource Manager", "Missing plant mesh / prefab template.", "OK");
                return;
            }

            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsWorldResources);
            string key = CraftingEditorUtility.SanitizeAssetName(yield.itemName).Replace(" ", string.Empty);
            GameObject root = new GameObject($"ResourceNode_{key}");
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(plantSource);
            if (visual == null)
                visual = Object.Instantiate(plantSource);
            visual.name = "Visual";
            visual.transform.SetParent(root.transform, false);

            ResourceNode node = root.AddComponent<ResourceNode>();
            ApplyPlantFieldsToNode(node, yield);
            EnsurePlantTriggerBox(root);
            MapMarkerEditorUtility.EnsureMapMarker(root, yield);

            string prefabPath = $"{ProjectAssetPaths.PrefabsWorldResources}/ResourceNode_{key}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);

            GameObject saved = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (createNodeDefinition)
                WriteNodeDefinition(yield, isPlant: true, saved);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            InvalidateRosterCache();

            if (!quiet)
            {
                Selection.activeObject = saved;
                EditorGUIUtility.PingObject(saved);
                SetStatus($"Created plant harvest node:\n{prefabPath}", MessageType.Info);
            }
        }

        private void ApplyMiningFieldsToNode(ResourceNode node, ItemData yield)
        {
            node.resourceItem = yield;
            node.interactionMode = ResourceNodeInteractionMode.LaserMine;
            node.passDuration = Mathf.Max(0.05f, miningPassDuration);
            node.waves = Mathf.Clamp(miningWaves, 1, 5);
            node.dropMin = Mathf.Max(1, miningDropMin);
            node.dropMax = Mathf.Max(node.dropMin, miningDropMax);
            node.lastWaveDropScale = Mathf.Clamp(miningLastWaveScale, 0.1f, 1f);
            node.amountPerGather = 1;
            node.maxHits = 99;
            node.requiredTool = miningRequiredTool;
            node.requireMiningLaser = miningRequireLaser;
            node.lootTint = miningLootTint;
            node.lootAttractPrefab = ResolveLootFlyModel(miningLootFlyModel, yield);
        }

        private void ApplyPlantFieldsToNode(ResourceNode node, ItemData yield)
        {
            node.resourceItem = yield;
            node.interactionMode = ResourceNodeInteractionMode.HoldHarvest;
            node.passDuration = plantHoldDuration;
            node.holdDurationSeconds = plantHoldDuration;
            node.waves = 1;
            node.dropMin = Mathf.Max(1, plantDropMin);
            node.dropMax = Mathf.Max(node.dropMin, plantDropMax);
            node.lastWaveDropScale = 1f;
            node.amountPerGather = plantDropMin;
            node.maxHits = 99;
            node.holdPromptText = string.IsNullOrWhiteSpace(plantHoldPrompt) ? "Hold E — Harvest" : plantHoldPrompt;
            node.holdInteractRange = Mathf.Max(0.5f, plantInteractRange);
            node.requiredTool = plantRequiredTool;
            node.requireMiningLaser = false;
            node.lootTint = plantLootTint;
            node.lootAttractPrefab = ResolveLootFlyModel(plantLootFlyModel, yield);
        }

        private static GameObject ResolveLootFlyModel(GameObject explicitModel, ItemData yield)
        {
            if (explicitModel != null)
                return explicitModel;
            return yield != null ? yield.worldPrefab : null;
        }

        private ResourceNodeDefinition WriteNodeDefinition(ItemData yield, bool isPlant, GameObject nodePrefab)
        {
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.ItemsNodes);
            string safe = CraftingEditorUtility.SanitizeAssetName(yield.itemName).Replace(" ", string.Empty);
            string path = $"{ProjectAssetPaths.ItemsNodes}/ResourceNode_{safe}.asset";

            ResourceNodeDefinition def = AssetDatabase.LoadAssetAtPath<ResourceNodeDefinition>(path);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<ResourceNodeDefinition>();
                AssetDatabase.CreateAsset(def, path);
            }

            def.displayName = yield.itemName;
            def.nodeKind = isPlant ? ResourceNodeDefinition.NodeKind.Plant : ResourceNodeDefinition.NodeKind.Mining;
            def.resourceItem = yield;
            def.interactionMode = isPlant
                ? ResourceNodeInteractionMode.HoldHarvest
                : ResourceNodeInteractionMode.LaserMine;
            def.requiredTool = isPlant ? plantRequiredTool : miningRequiredTool;
            def.requireMiningLaser = !isPlant && miningRequireLaser;
            def.durationSeconds = isPlant ? plantHoldDuration : miningPassDuration;
            def.waves = isPlant ? 1 : miningWaves;
            def.dropMin = isPlant ? plantDropMin : miningDropMin;
            def.dropMax = isPlant ? plantDropMax : miningDropMax;
            def.lastWaveDropScale = isPlant ? 1f : miningLastWaveScale;
            def.holdPromptText = plantHoldPrompt;
            def.holdInteractRange = plantInteractRange;
            def.lootTint = isPlant ? plantLootTint : miningLootTint;
            def.meshTemplate = isPlant ? plantMeshTemplate : miningMeshTemplate;
            def.lootFlyModel = isPlant
                ? ResolveLootFlyModel(plantLootFlyModel, yield)
                : ResolveLootFlyModel(miningLootFlyModel, yield);
            def.nodePrefab = nodePrefab;
            def.itemMaxStack = yield.maxStack;
            def.itemIcon = icon != null ? icon : yield.icon;
            def.itemTooltip = yield.tooltipDescription;
            if (yield is MineHarvestItemData leanAudio)
            {
                def.lootYieldClip = leanAudio.lootYieldClip;
                def.lootGrantClip = leanAudio.lootGrantClip;
            }
            EditorUtility.SetDirty(def);
            return def;
        }

        private static ItemData LoadResourceItem(string displayName, bool isPlant)
        {
            string safe = CraftingEditorUtility.SanitizeAssetName(displayName);
            string folder = isPlant ? ProjectAssetPaths.ItemsResourcesHarvest : ProjectAssetPaths.ItemsResourcesMining;
            ItemData item = AssetDatabase.LoadAssetAtPath<MineHarvestItemData>($"{folder}/{safe}.asset");
            if (item != null)
                return item;
            item = AssetDatabase.LoadAssetAtPath<ItemData>($"{folder}/{safe}.asset");
            if (item != null)
                return item;
            return AssetDatabase.LoadAssetAtPath<ItemData>($"{ProjectAssetPaths.ItemsResources}/{safe}.asset");
        }

        private static void EnsureMineralMeshCollider(GameObject root)
        {
            BoxCollider[] boxes = root.GetComponentsInChildren<BoxCollider>(true);
            for (int i = 0; i < boxes.Length; i++)
                Object.DestroyImmediate(boxes[i]);

            MeshCollider mesh = root.GetComponentInChildren<MeshCollider>(true);
            if (mesh == null)
            {
                MeshFilter filter = root.GetComponentInChildren<MeshFilter>(true);
                if (filter != null && filter.sharedMesh != null)
                {
                    mesh = filter.gameObject.AddComponent<MeshCollider>();
                    mesh.sharedMesh = filter.sharedMesh;
                }
            }

            if (mesh != null)
            {
                mesh.convex = false;
                mesh.isTrigger = false;
            }
        }

        private static void EnsurePlantTriggerBox(GameObject root)
        {
            MeshCollider[] meshes = root.GetComponentsInChildren<MeshCollider>(true);
            for (int i = 0; i < meshes.Length; i++)
                Object.DestroyImmediate(meshes[i]);

            // Recenter visual children so the mesh sits on the node root (fixes offset colliders / dots).
            Renderer rend = root.GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                Vector3 localCenter = root.transform.InverseTransformPoint(rend.bounds.center);
                if (localCenter.sqrMagnitude > 0.0001f)
                {
                    for (int i = 0; i < root.transform.childCount; i++)
                        root.transform.GetChild(i).localPosition -= localCenter;
                    rend = root.GetComponentInChildren<Renderer>();
                }
            }

            BoxCollider box = root.GetComponent<BoxCollider>();
            if (box == null)
                box = root.AddComponent<BoxCollider>();

            box.isTrigger = true;
            if (rend != null)
            {
                Bounds b = rend.bounds;
                box.center = root.transform.InverseTransformPoint(b.center);
                Vector3 lossy = root.transform.lossyScale;
                box.size = new Vector3(
                    SafeDiv(b.size.x, lossy.x),
                    SafeDiv(b.size.y, lossy.y),
                    SafeDiv(b.size.z, lossy.z)) + Vector3.one * 0.05f;
            }
            else
            {
                box.size = new Vector3(1f, 1.2f, 1f);
                box.center = new Vector3(0f, 0.6f, 0f);
            }
        }

        private void SetStatus(string message, MessageType type)
        {
            statusMessage = message;
            statusType = type;
        }

        private static float SafeDiv(float a, float b) => Mathf.Abs(b) < 0.0001f ? a : a / b;
    }
}
