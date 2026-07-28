using Project.Data;
using Project.Interaction;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Creates operational Resource ItemData assets (fuels, water, coolant, and similar
    /// crafting/operations materials) along with a world pickup prefab in one step. The generated
    /// prefab doubles as both the "pickup" and the "usable" object — ItemPickup already implements
    /// IWorldUsable, so a single prefab is both collectible and press-E interactable, matching the
    /// hand-authored Plasma Fuel setup earlier in this project.
    /// Also authors mining boulder nodes and hold-E plant harvest nodes.
    /// </summary>
    public class ResourceItemCreatorWindow : EditorWindow
    {
        private struct ResourcePreset
        {
            public string Name;
            public int MaxStack;
            public string Tooltip;

            public ResourcePreset(string name, int maxStack, string tooltip)
            {
                Name = name;
                MaxStack = maxStack;
                Tooltip = tooltip;
            }
        }

        private static readonly ResourcePreset[] Presets =
        {
            new ResourcePreset("Plasma Fuel", 500, "Refined plasma fuel cell. Refuels the hovercraft, powers building generators, and recharges the laser mining tool."),
            new ResourcePreset("Purified Water", 20, "Clean drinking water. Restores hydration and is used in several crafting recipes."),
            new ResourcePreset("Coolant Cell", 20, "Stabilized coolant. Keeps generators and machinery from overheating under sustained load."),
            new ResourcePreset("Hydrogen Canister", 20, "Compressed hydrogen. A lighter, faster-burning alternative fuel for specialized equipment."),
            new ResourcePreset("Refined Ore", 40, "Smelted metal ore, ready for advanced crafting recipes."),
        };

        private static readonly ResourcePreset[] MiningItemPresets =
        {
            new ResourcePreset("Silicate Ore", 80, "Io silicate rock fragments. Used in abrasives, ceramics, and structural crafting."),
            new ResourcePreset("Iron Ore", 80, "Dense iron-bearing ore. Smelted into metal components for weapons and modules."),
            new ResourcePreset("Sulfur Needle Tuft", 40, "Bristly sulfur-rich plant fiber. Antiseptic reagent for medpacks and salves."),
        };

        private const string BoulderTemplatePath = ProjectAssetPaths.BoulderNodeTemplate;
        private const string PlantGlbPath = ProjectAssetPaths.SulfurNeedleTuftGlb;
        private const string WorldResourcesFolder = ProjectAssetPaths.PrefabsWorldResources;

        private string itemName = "New Resource";
        private string assetFileName = string.Empty;
        private int maxStack = 20;
        private Sprite icon;
        private string tooltipDescription = string.Empty;

        private GameObject worldPrefabTemplate;
        private bool createWorldPrefab = true;
        private bool addToItemRegistry = true;
        private Vector2 scroll;

        private bool showMiningSection = true;
        private enum BoulderOreChoice { Silicate, Iron, Random }
        private BoulderOreChoice boulderOre = BoulderOreChoice.Random;
        private int boulderWaves = 1;
        private int boulderDropMin = 1;
        private int boulderDropMax = 3;
        private float boulderLastWaveScale = 0.6f;
        private float boulderPassDuration = 5f;
        private float plantHoldDuration = 4f;
        private int plantDropMin = 5;
        private int plantDropMax = 10;

        [MenuItem(SurvivalPioneerEditorMenus.ResourceItemCreator, false, 1)]
        public static void Open()
        {
            GetWindow<ResourceItemCreatorWindow>("Resource Item Creator").minSize = new Vector2(440f, 640f);
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);

            EditorGUILayout.LabelField("Resource Item Creator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Create operational Resource items — fuels, water, coolant, and similar materials used " +
                "by vehicles, generators, and crafting — as an ItemData asset plus a world pickup prefab.",
                MessageType.Info);

            EditorGUILayout.Space(8f);
            DrawPresetButtons();

            EditorGUILayout.Space(8f);
            itemName = EditorGUILayout.TextField("Item Name", itemName);
            assetFileName = EditorGUILayout.TextField(
                "Asset File Name",
                string.IsNullOrEmpty(assetFileName) ? CraftingEditorUtility.SanitizeAssetName(itemName) : assetFileName);
            maxStack = EditorGUILayout.IntField("Max Stack", Mathf.Max(1, maxStack));
            icon = (Sprite)EditorGUILayout.ObjectField("Icon", icon, typeof(Sprite), false);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Tooltip", EditorStyles.boldLabel);
            tooltipDescription = EditorGUILayout.TextArea(tooltipDescription, GUILayout.MinHeight(48f));

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("World Pickup / Usable Prefab", EditorStyles.boldLabel);
            createWorldPrefab = EditorGUILayout.Toggle("Create World Prefab", createWorldPrefab);
            using (new EditorGUI.DisabledScope(!createWorldPrefab))
            {
                worldPrefabTemplate = (GameObject)EditorGUILayout.ObjectField(
                    "Mesh Template (e.g. barrel, canister prop)", worldPrefabTemplate, typeof(GameObject), false);
                EditorGUILayout.HelpBox(
                    "The mesh template is instantiated with a trigger collider + ItemPickup added, then saved " +
                    "as a prefab. ItemPickup already implements IWorldUsable, so the result is both a pickup " +
                    "and a press-E usable object — no second prefab needed.",
                    MessageType.None);
            }

            addToItemRegistry = EditorGUILayout.Toggle("Add To Item Registry", addToItemRegistry);

            EditorGUILayout.Space(16f);
            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(itemName)))
            {
                if (GUILayout.Button("Create Resource Item", GUILayout.Height(42f)))
                    CreateItem();
            }

            EditorGUILayout.Space(20f);
            DrawMiningHarvestSection();

            EditorGUILayout.EndScrollView();
        }

        private void DrawMiningHarvestSection()
        {
            showMiningSection = EditorGUILayout.Foldout(showMiningSection, "Mining / Harvest Nodes", true);
            if (!showMiningSection)
                return;

            EditorGUILayout.HelpBox(
                "Create Silicate/Iron/Sulfur Needle items, laser-minable boulder nodes (5s/wave), " +
                "and hold-E plant harvest nodes from the starter boulder / Sulfur Needle Tuft assets.",
                MessageType.Info);

            EditorGUILayout.LabelField("Mining Resource Items", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < MiningItemPresets.Length; i++)
            {
                ResourcePreset preset = MiningItemPresets[i];
                if (GUILayout.Button(preset.Name))
                    ApplyPreset(preset);
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Create All Mining Resource Items", GUILayout.Height(28f)))
                CreateAllMiningItems();

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Boulder Node (Laser Mine)", EditorStyles.boldLabel);
            boulderOre = (BoulderOreChoice)EditorGUILayout.EnumPopup("Primary Ore", boulderOre);
            boulderWaves = EditorGUILayout.IntSlider("Waves", boulderWaves, 1, 3);
            boulderPassDuration = EditorGUILayout.FloatField("Pass Duration (s)", boulderPassDuration);
            boulderDropMin = EditorGUILayout.IntField("Drop Min / Wave", Mathf.Max(1, boulderDropMin));
            boulderDropMax = EditorGUILayout.IntField("Drop Max / Wave", Mathf.Max(boulderDropMin, boulderDropMax));
            boulderLastWaveScale = EditorGUILayout.Slider("Last Wave Scale", boulderLastWaveScale, 0.1f, 1f);

            if (GUILayout.Button("Create Boulder Mining Prefab", GUILayout.Height(32f)))
                CreateBoulderNodePrefab();

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Plant Node (Hold E Harvest)", EditorStyles.boldLabel);
            plantHoldDuration = EditorGUILayout.Slider("Hold Duration (s)", plantHoldDuration, 3f, 5f);
            plantDropMin = EditorGUILayout.IntField("Yield Min", Mathf.Max(1, plantDropMin));
            plantDropMax = EditorGUILayout.IntField("Yield Max", Mathf.Max(plantDropMin, plantDropMax));

            if (GUILayout.Button("Create Sulfur Needle Tuft Harvest Prefab", GUILayout.Height(32f)))
                CreatePlantNodePrefab();
        }

        private void DrawPresetButtons()
        {
            EditorGUILayout.LabelField("Quick Presets", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < Presets.Length; i++)
            {
                ResourcePreset preset = Presets[i];
                if (GUILayout.Button(preset.Name))
                    ApplyPreset(preset);

                if ((i + 1) % 3 == 0 && i != Presets.Length - 1)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void ApplyPreset(ResourcePreset preset)
        {
            itemName = preset.Name;
            assetFileName = CraftingEditorUtility.SanitizeAssetName(preset.Name);
            maxStack = preset.MaxStack;
            tooltipDescription = preset.Tooltip;
            GUI.FocusControl(null);
        }

        private void CreateAllMiningItems()
        {
            for (int i = 0; i < MiningItemPresets.Length; i++)
            {
                ApplyPreset(MiningItemPresets[i]);
                createWorldPrefab = true;
                if (worldPrefabTemplate == null)
                    worldPrefabTemplate = AssetDatabase.LoadAssetAtPath<GameObject>(BoulderTemplatePath);
                CreateItem(quiet: true);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Resource Item Creator", "Created Silicate Ore, Iron Ore, and Sulfur Needle Tuft.", "OK");
        }

        private void CreateItem(bool quiet = false)
        {
            string safeName = CraftingEditorUtility.SanitizeAssetName(string.IsNullOrWhiteSpace(assetFileName) ? itemName : assetFileName);
            if (string.IsNullOrEmpty(safeName))
            {
                if (!quiet)
                    EditorUtility.DisplayDialog("Resource Item Creator", "Enter a valid item or file name.", "OK");
                return;
            }

            string dataPath = $"{ProjectAssetPaths.ItemsResources}/{safeName}.asset";
            if (!quiet && AssetDatabase.LoadAssetAtPath<ItemData>(dataPath) != null &&
                !EditorUtility.DisplayDialog("Resource Item Creator", $"Item asset '{safeName}' already exists. Overwrite?", "Overwrite", "Cancel"))
            {
                return;
            }

            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.ItemsResources);
            CraftingEditorUtility.EnsureFolder(CraftingEditorUtility.ItemPrefabsFolder);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsItemsWorld);

            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(dataPath);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<ItemData>();
                AssetDatabase.CreateAsset(item, dataPath);
            }

            item.itemName = itemName.Trim();
            item.itemType = ItemType.Resource;
            item.maxStack = maxStack;
            item.icon = icon;
            item.tooltipDescription = tooltipDescription;
            EditorUtility.SetDirty(item);

            if (createWorldPrefab)
            {
                GameObject template = worldPrefabTemplate;
                if (template == null)
                    template = AssetDatabase.LoadAssetAtPath<GameObject>(BoulderTemplatePath);

                if (template != null)
                {
                    GameObject instance = Object.Instantiate(template);
                    instance.name = safeName + "_World";

                    // Strip ResourceNode from pickup clones — pickups should not be minable.
                    ResourceNode[] nodes = instance.GetComponentsInChildren<ResourceNode>(true);
                    for (int i = 0; i < nodes.Length; i++)
                        Object.DestroyImmediate(nodes[i]);

                    // Ensure a BoxCollider trigger first so RequireComponent(Collider) stays satisfied
                    // when MeshColliders are removed (concave MeshColliders cannot be triggers).
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

                    item.worldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    EditorUtility.SetDirty(item);
                }
            }

            if (addToItemRegistry)
                CraftingEditorUtility.AddItemToRegistry(item);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!quiet)
            {
                Selection.activeObject = item;
                EditorGUIUtility.PingObject(item);
                EditorUtility.DisplayDialog("Resource Item Creator", $"Created resource item '{item.itemName}'.", "OK");
            }
        }

        private ItemData EnsureMiningItem(string displayName, int stack, string tooltip)
        {
            string safe = CraftingEditorUtility.SanitizeAssetName(displayName);
            string dataPath = $"{ProjectAssetPaths.ItemsResources}/{safe}.asset";
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(dataPath);
            if (item != null)
                return item;

            itemName = displayName;
            assetFileName = safe;
            maxStack = stack;
            tooltipDescription = tooltip;
            createWorldPrefab = true;
            worldPrefabTemplate = AssetDatabase.LoadAssetAtPath<GameObject>(BoulderTemplatePath);
            CreateItem(quiet: true);
            return AssetDatabase.LoadAssetAtPath<ItemData>(dataPath);
        }

        private void CreateBoulderNodePrefab()
        {
            ItemData silicate = EnsureMiningItem(
                "Silicate Ore", 80, "Io silicate rock fragments. Used in abrasives, ceramics, and structural crafting.");
            ItemData iron = EnsureMiningItem(
                "Iron Ore", 80, "Dense iron-bearing ore. Smelted into metal components for weapons and modules.");

            ItemData primary = boulderOre switch
            {
                BoulderOreChoice.Silicate => silicate,
                BoulderOreChoice.Iron => iron,
                _ => Random.value < 0.5f ? silicate : iron
            };

            GameObject template = AssetDatabase.LoadAssetAtPath<GameObject>(BoulderTemplatePath);
            if (template == null)
            {
                EditorUtility.DisplayDialog("Resource Item Creator", $"Missing boulder template:\n{BoulderTemplatePath}", "OK");
                return;
            }

            CraftingEditorUtility.EnsureFolder(WorldResourcesFolder);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(template);
            instance.name = $"ResourceNode_Boulder_{primary.itemName.Replace(' ', '_')}";

            ResourceNode node = instance.GetComponent<ResourceNode>();
            if (node == null)
                node = instance.AddComponent<ResourceNode>();

            node.resourceItem = primary;
            node.interactionMode = ResourceNodeInteractionMode.LaserMine;
            node.passDuration = Mathf.Max(0.05f, boulderPassDuration);
            node.waves = Mathf.Clamp(boulderWaves, 1, 3);
            node.dropMin = Mathf.Max(1, boulderDropMin);
            node.dropMax = Mathf.Max(node.dropMin, boulderDropMax);
            node.lastWaveDropScale = Mathf.Clamp(boulderLastWaveScale, 0.1f, 1f);
            node.amountPerGather = 1;
            node.maxHits = 99;

            EnsureMineralMeshCollider(instance);

            string oreKey = primary.itemName.Replace(" ", string.Empty);
            string prefabPath = $"{WorldResourcesFolder}/ResourceNode_Boulder_{oreKey}.prefab";
            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Object.DestroyImmediate(instance);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            GameObject saved = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            EditorUtility.DisplayDialog("Resource Item Creator", $"Created boulder node:\n{prefabPath}", "OK");
        }

        private void CreatePlantNodePrefab()
        {
            ItemData tuft = EnsureMiningItem(
                "Sulfur Needle Tuft", 40, "Bristly sulfur-rich plant fiber. Antiseptic reagent for medpacks and salves.");

            GameObject plantSource = AssetDatabase.LoadAssetAtPath<GameObject>(PlantGlbPath);
            if (plantSource == null)
            {
                EditorUtility.DisplayDialog("Resource Item Creator", $"Missing plant source:\n{PlantGlbPath}", "OK");
                return;
            }

            CraftingEditorUtility.EnsureFolder(WorldResourcesFolder);
            GameObject root = new GameObject("ResourceNode_SulfurNeedleTuft");
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(plantSource);
            visual.name = "Visual";
            visual.transform.SetParent(root.transform, false);

            ResourceNode node = root.AddComponent<ResourceNode>();
            node.resourceItem = tuft;
            node.interactionMode = ResourceNodeInteractionMode.HoldHarvest;
            node.passDuration = plantHoldDuration;
            node.holdDurationSeconds = plantHoldDuration;
            node.waves = 1;
            node.dropMin = Mathf.Max(1, plantDropMin);
            node.dropMax = Mathf.Max(node.dropMin, plantDropMax);
            node.lastWaveDropScale = 1f;
            node.amountPerGather = plantDropMin;
            node.maxHits = 99;
            node.holdPromptText = "Hold E — Harvest";
            node.lootTint = new Color(0.75f, 0.82f, 0.28f, 1f);

            EnsurePlantTriggerBox(root);

            string prefabPath = $"{WorldResourcesFolder}/ResourceNode_SulfurNeedleTuft.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            GameObject saved = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Selection.activeObject = saved;
            EditorGUIUtility.PingObject(saved);
            EditorUtility.DisplayDialog("Resource Item Creator", $"Created plant harvest node:\n{prefabPath}", "OK");
        }

        /// <summary>
        /// Mineral nodes keep the authored MeshCollider for laser Raycast hits (non-trigger).
        /// Removes any extra BoxColliders that would steal hits.
        /// </summary>
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

        /// <summary>
        /// Plant harvest nodes use a trigger BoxCollider for Hold-E aim/use (no mesh collider required).
        /// </summary>
        private static void EnsurePlantTriggerBox(GameObject root)
        {
            MeshCollider[] meshes = root.GetComponentsInChildren<MeshCollider>(true);
            for (int i = 0; i < meshes.Length; i++)
                Object.DestroyImmediate(meshes[i]);

            BoxCollider box = root.GetComponent<BoxCollider>();
            if (box == null)
                box = root.AddComponent<BoxCollider>();

            box.isTrigger = true;
            Renderer rend = root.GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                Bounds b = rend.bounds;
                box.center = root.transform.InverseTransformPoint(b.center);
                Vector3 lossy = root.transform.lossyScale;
                box.size = new Vector3(
                    SafeDiv(b.size.x, lossy.x),
                    SafeDiv(b.size.y, lossy.y),
                    SafeDiv(b.size.z, lossy.z));
            }
            else
            {
                box.size = new Vector3(1f, 1.2f, 1f);
                box.center = new Vector3(0f, 0.6f, 0f);
            }
        }

        private static float SafeDiv(float a, float b) => Mathf.Abs(b) < 0.0001f ? a : a / b;
    }
}
