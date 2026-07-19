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
            new ResourcePreset("Plasma Fuel", 20, "Refined plasma fuel cell. Refuels the hovercraft and powers building generators."),
            new ResourcePreset("Purified Water", 20, "Clean drinking water. Restores hydration and is used in several crafting recipes."),
            new ResourcePreset("Coolant Cell", 20, "Stabilized coolant. Keeps generators and machinery from overheating under sustained load."),
            new ResourcePreset("Hydrogen Canister", 20, "Compressed hydrogen. A lighter, faster-burning alternative fuel for specialized equipment."),
            new ResourcePreset("Refined Ore", 40, "Smelted metal ore, ready for advanced crafting recipes."),
        };

        private string itemName = "New Resource";
        private string assetFileName = string.Empty;
        private int maxStack = 20;
        private Sprite icon;
        private string tooltipDescription = string.Empty;

        private GameObject worldPrefabTemplate;
        private bool createWorldPrefab = true;
        private bool addToItemRegistry = true;
        private Vector2 scroll;

        [MenuItem(SurvivalPioneerEditorMenus.ResourceItemCreator, false, 1)]
        public static void Open()
        {
            GetWindow<ResourceItemCreatorWindow>("Resource Item Creator").minSize = new Vector2(440f, 560f);
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

            EditorGUILayout.EndScrollView();
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

        private void CreateItem()
        {
            string safeName = CraftingEditorUtility.SanitizeAssetName(string.IsNullOrWhiteSpace(assetFileName) ? itemName : assetFileName);
            if (string.IsNullOrEmpty(safeName))
            {
                EditorUtility.DisplayDialog("Resource Item Creator", "Enter a valid item or file name.", "OK");
                return;
            }

            string dataPath = $"{CraftingEditorUtility.ItemsFolder}/{safeName}.asset";
            if (AssetDatabase.LoadAssetAtPath<ItemData>(dataPath) != null &&
                !EditorUtility.DisplayDialog("Resource Item Creator", $"Item asset '{safeName}' already exists. Overwrite?", "Overwrite", "Cancel"))
            {
                return;
            }

            CraftingEditorUtility.EnsureFolder(CraftingEditorUtility.ItemsFolder);
            CraftingEditorUtility.EnsureFolder(CraftingEditorUtility.ItemPrefabsFolder);

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

            if (createWorldPrefab && worldPrefabTemplate != null)
            {
                GameObject instance = Instantiate(worldPrefabTemplate);
                instance.name = safeName + "_World";

                Collider collider = instance.GetComponentInChildren<Collider>();
                if (collider == null)
                {
                    BoxCollider box = instance.AddComponent<BoxCollider>();
                    box.isTrigger = true;
                }
                else
                {
                    collider.isTrigger = true;
                }

                ItemPickup pickup = instance.GetComponent<ItemPickup>();
                if (pickup == null)
                    pickup = instance.AddComponent<ItemPickup>();

                pickup.itemData = item;
                pickup.amount = 1;
                pickup.promptText = "Press E to pick up";

                string prefabPath = $"{CraftingEditorUtility.ItemPrefabsFolder}/{safeName}_World.prefab";
                PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                DestroyImmediate(instance);

                item.worldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                EditorUtility.SetDirty(item);
            }

            if (addToItemRegistry)
                CraftingEditorUtility.AddItemToRegistry(item);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = item;
            EditorGUIUtility.PingObject(item);
            EditorUtility.DisplayDialog("Resource Item Creator", $"Created resource item '{item.itemName}'.", "OK");
        }
    }
}
