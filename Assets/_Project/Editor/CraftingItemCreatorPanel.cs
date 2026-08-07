using Project.Data;
using Project.Interaction;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Shared UI + create logic for Crafting Item Creator (embedded in Blueprint and Crafting Manager).
    /// </summary>
    public sealed class CraftingItemCreatorPanel
    {
        private string itemName = "New Crafting Item";
        private string assetFileName = string.Empty;
        private ItemType itemType = ItemType.Consumable;
        private int maxStack = 64;
        private float healthRestore;
        private float energyRestore;
        private float staminaRestore;
        private float oxygenRestore;
        private string tooltipDescription = string.Empty;
        private Sprite icon;
        private GameObject worldPrefabTemplate;
        private bool createWorldPrefab = true;
        private bool addToItemRegistry = true;

        private Vector2 scroll;

        public void Draw()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);

            EditorGUILayout.LabelField("Crafting Item Creator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Create stackable consumables or resources for blueprint ingredients and crafted outputs.\n" +
                "For grenades use Grenade / Throwable Item Creator (Throwables folder).",
                MessageType.None);
            EditorGUILayout.Space(8f);

            itemName = EditorGUILayout.TextField("Item Name", itemName);
            assetFileName = EditorGUILayout.TextField(
                "Asset File Name",
                string.IsNullOrEmpty(assetFileName) ? CraftingEditorUtility.SanitizeAssetName(itemName) : assetFileName);
            itemType = (ItemType)EditorGUILayout.EnumPopup("Item Type", itemType);

            if (itemType != ItemType.Consumable && itemType != ItemType.Resource)
            {
                EditorGUILayout.HelpBox("Use Craftable Equipment / Weapon / Ammo creators for other types.", MessageType.Warning);
                itemType = ItemType.Consumable;
            }

            maxStack = EditorGUILayout.IntField("Max Stack", Mathf.Max(1, maxStack));
            icon = (Sprite)EditorGUILayout.ObjectField("Icon", icon, typeof(Sprite), false);

            if (itemType == ItemType.Consumable)
            {
                EditorGUILayout.Space(8f);
                EditorGUILayout.LabelField("Survival Restore", EditorStyles.boldLabel);
                healthRestore = EditorGUILayout.FloatField("Health", healthRestore);
                energyRestore = EditorGUILayout.FloatField("Energy", energyRestore);
                staminaRestore = EditorGUILayout.FloatField("Stamina", staminaRestore);
                oxygenRestore = EditorGUILayout.FloatField("Oxygen (display sec)", oxygenRestore);
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Tooltip", EditorStyles.boldLabel);
            tooltipDescription = EditorGUILayout.TextArea(tooltipDescription, GUILayout.MinHeight(48f));

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("World Pickup", EditorStyles.boldLabel);
            createWorldPrefab = EditorGUILayout.Toggle("Create World Prefab", createWorldPrefab);
            using (new EditorGUI.DisabledScope(!createWorldPrefab))
            {
                worldPrefabTemplate = (GameObject)EditorGUILayout.ObjectField(
                    "Mesh Template", worldPrefabTemplate, typeof(GameObject), false);
            }

            addToItemRegistry = EditorGUILayout.Toggle("Add To Item Registry", addToItemRegistry);

            EditorGUILayout.Space(16f);
            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(itemName)))
            {
                if (GUILayout.Button("Create Crafting Item", GUILayout.Height(42f)))
                    CreateItem();
            }

            EditorGUILayout.EndScrollView();
        }

        private void CreateItem()
        {
            string safeName = CraftingEditorUtility.SanitizeAssetName(
                string.IsNullOrWhiteSpace(assetFileName) ? itemName : assetFileName);
            if (string.IsNullOrEmpty(safeName))
            {
                EditorUtility.DisplayDialog("Crafting Item Creator", "Enter a valid item or file name.", "OK");
                return;
            }

            string folder = CraftingEditorUtility.GetItemCategoryFolder(itemType);
            CraftingEditorUtility.EnsureFolder(folder);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsItemsWorld);

            string dataPath = $"{folder}/{safeName}.asset";
            if (AssetDatabase.LoadAssetAtPath<ItemData>(dataPath) != null
                && !EditorUtility.DisplayDialog(
                    "Crafting Item Creator",
                    $"Item asset '{safeName}' already exists. Overwrite?",
                    "Overwrite",
                    "Cancel"))
            {
                return;
            }

            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(dataPath);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<ItemData>();
                AssetDatabase.CreateAsset(item, dataPath);
            }

            item.itemName = itemName.Trim();
            item.itemType = itemType;
            item.maxStack = maxStack;
            item.icon = icon;
            item.tooltipDescription = tooltipDescription;

            if (itemType == ItemType.Consumable)
            {
                item.healthRestore = healthRestore;
                item.energyRestore = energyRestore;
                item.staminaRestore = staminaRestore;
                item.oxygenRestore = oxygenRestore;
            }
            else
            {
                item.healthRestore = 0f;
                item.energyRestore = 0f;
                item.staminaRestore = 0f;
                item.oxygenRestore = 0f;
            }

            ItemDataPruneUtility.Prune(item);
            EditorUtility.SetDirty(item);

            if (createWorldPrefab && worldPrefabTemplate != null)
            {
                GameObject instance = Object.Instantiate(worldPrefabTemplate);
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

                string prefabPath = $"{ProjectAssetPaths.PrefabsItemsWorld}/{safeName}_World.prefab";
                PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                Object.DestroyImmediate(instance);

                item.worldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                EditorUtility.SetDirty(item);
            }

            if (addToItemRegistry)
                CraftingEditorUtility.AddItemToRegistry(item);

            AssetDatabase.SaveAssets();

            Selection.activeObject = item;
            EditorGUIUtility.PingObject(item);
            EditorUtility.DisplayDialog("Crafting Item Creator", $"Created crafting item '{item.itemName}'.", "OK");
        }
    }
}
