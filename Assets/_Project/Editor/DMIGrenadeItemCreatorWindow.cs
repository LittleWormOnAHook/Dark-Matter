using Project.Data;
using Project.Interaction;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Creates identity-only throwable consumable ItemData under Throwables (keeps itemType = Consumable).
    /// </summary>
    public class DMIGrenadeItemCreatorWindow : EditorWindow
    {
        private string itemName = "Frag Grenade";
        private string assetFileName = "DM_Frag_Grenade";
        private int maxStack = 6;
        private string tooltipDescription =
            "Fragmentation grenade. Hold G to aim, click to throw. Hold LT (or RMB/LCtrl) while aiming to cook.";
        private Sprite icon;
        private GameObject worldPrefabTemplate;
        private bool createWorldPrefab;
        private bool addToItemRegistry = true;

        [MenuItem(DarkMatterGenesisEditorMenus.GrenadeItemCreator, false, 2)]
        public static void Open()
        {
            GetWindow<DMIGrenadeItemCreatorWindow>("Grenade Item Creator").minSize = new Vector2(420f, 420f);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Grenade / Throwable Item Creator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Creates Consumable ItemData under Data/Items/Throwables with identity fields only.\n" +
                "Combat throw prefab stays under Prefabs/Combat/Throwables — do not put it here.",
                MessageType.Info);

            itemName = EditorGUILayout.TextField("Item Name", itemName);
            assetFileName = EditorGUILayout.TextField(
                "Asset File Name",
                string.IsNullOrEmpty(assetFileName)
                    ? CraftingEditorUtility.SanitizeAssetName(itemName)
                    : assetFileName);
            maxStack = EditorGUILayout.IntField("Max Stack", Mathf.Max(1, maxStack));
            icon = (Sprite)EditorGUILayout.ObjectField("Icon", icon, typeof(Sprite), false);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Tooltip", EditorStyles.boldLabel);
            tooltipDescription = EditorGUILayout.TextArea(tooltipDescription, GUILayout.MinHeight(56f));

            EditorGUILayout.Space(6f);
            createWorldPrefab = EditorGUILayout.Toggle("Create World Pickup Prefab", createWorldPrefab);
            using (new EditorGUI.DisabledScope(!createWorldPrefab))
            {
                worldPrefabTemplate = (GameObject)EditorGUILayout.ObjectField(
                    "Mesh Template",
                    worldPrefabTemplate,
                    typeof(GameObject),
                    false);
            }

            addToItemRegistry = EditorGUILayout.Toggle("Add To Item Registry", addToItemRegistry);

            EditorGUILayout.Space(14f);
            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(itemName)))
            {
                if (GUILayout.Button("Create Throwable ItemData", GUILayout.Height(40f)))
                    CreateItem();
            }
        }

        private void CreateItem()
        {
            string safeName = CraftingEditorUtility.SanitizeAssetName(
                string.IsNullOrWhiteSpace(assetFileName) ? itemName : assetFileName);
            if (string.IsNullOrEmpty(safeName))
            {
                EditorUtility.DisplayDialog("Grenade Item Creator", "Enter a valid asset file name.", "OK");
                return;
            }

            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.ItemsThrowables);
            string dataPath = $"{ProjectAssetPaths.ItemsThrowables}/{safeName}.asset";

            if (AssetDatabase.LoadAssetAtPath<ItemData>(dataPath) != null
                && !EditorUtility.DisplayDialog(
                    "Grenade Item Creator",
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
            item.itemType = ItemType.Consumable;
            item.maxStack = maxStack;
            item.icon = icon;
            item.tooltipDescription = tooltipDescription;
            ItemDataPruneUtility.Prune(item, ItemDataInspectorCategory.ThrowableConsumable);
            EditorUtility.SetDirty(item);

            if (createWorldPrefab && worldPrefabTemplate != null)
            {
                CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsItemsWorld);
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
            EditorUtility.DisplayDialog(
                "Grenade Item Creator",
                $"Created throwable '{item.itemName}' at\n{dataPath}\n(itemType = Consumable).",
                "OK");
        }
    }
}
