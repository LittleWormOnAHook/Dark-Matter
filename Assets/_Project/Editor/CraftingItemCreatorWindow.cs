using Project.Data;

using Project.Interaction;

using UnityEditor;

using UnityEngine;



namespace Project.EditorTools

{

    /// <summary>

    /// Creates consumable and resource ItemData assets used as crafting ingredients or outputs.

    /// </summary>

    public class CraftingItemCreatorWindow : EditorWindow

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



        [MenuItem(SurvivalPioneerEditorMenus.Crafting + "Crafting Item Creator")]

        public static void Open()

        {

            GetWindow<CraftingItemCreatorWindow>("Crafting Item Creator").minSize = new Vector2(420f, 560f);

        }



        private void OnGUI()

        {

            EditorGUILayout.LabelField("Crafting Item Creator", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(

                "Create stackable consumables or resources for recipe ingredients and crafted outputs.",

                MessageType.Info);

            EditorGUILayout.Space(8f);



            itemName = EditorGUILayout.TextField("Item Name", itemName);

            assetFileName = EditorGUILayout.TextField(

                "Asset File Name",

                string.IsNullOrEmpty(assetFileName) ? CraftingEditorUtility.SanitizeAssetName(itemName) : assetFileName);

            itemType = (ItemType)EditorGUILayout.EnumPopup("Item Type", itemType);



            if (itemType != ItemType.Consumable && itemType != ItemType.Resource)

            {

                EditorGUILayout.HelpBox("Use Craftable Equipment Recipe Creator for weapons and tools.", MessageType.Warning);

                itemType = ItemType.Consumable;

            }



            maxStack = EditorGUILayout.IntField("Max Stack", Mathf.Max(1, maxStack));

            icon = (Sprite)EditorGUILayout.ObjectField("Icon", icon, typeof(Sprite), false);



            EditorGUILayout.Space(8f);

            EditorGUILayout.LabelField("Survival Restore", EditorStyles.boldLabel);

            healthRestore = EditorGUILayout.FloatField("Health", healthRestore);

            energyRestore = EditorGUILayout.FloatField("Energy", energyRestore);

            staminaRestore = EditorGUILayout.FloatField("Stamina", staminaRestore);

            oxygenRestore = EditorGUILayout.FloatField("Oxygen (display sec)", oxygenRestore);



            EditorGUILayout.Space(8f);

            EditorGUILayout.LabelField("Tooltip", EditorStyles.boldLabel);

            tooltipDescription = EditorGUILayout.TextArea(tooltipDescription, GUILayout.MinHeight(48f));



            EditorGUILayout.Space(8f);

            EditorGUILayout.LabelField("World Pickup", EditorStyles.boldLabel);

            createWorldPrefab = EditorGUILayout.Toggle("Create World Prefab", createWorldPrefab);

            using (new EditorGUI.DisabledScope(!createWorldPrefab))

            {

                worldPrefabTemplate = (GameObject)EditorGUILayout.ObjectField("Mesh Template", worldPrefabTemplate, typeof(GameObject), false);

            }



            addToItemRegistry = EditorGUILayout.Toggle("Add To Item Registry", addToItemRegistry);



            EditorGUILayout.Space(16f);

            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(itemName)))

            {

                if (GUILayout.Button("Create Crafting Item", GUILayout.Height(42f)))

                    CreateItem();

            }

        }



        private void CreateItem()

        {

            string safeName = CraftingEditorUtility.SanitizeAssetName(string.IsNullOrWhiteSpace(assetFileName) ? itemName : assetFileName);

            if (string.IsNullOrEmpty(safeName))

            {

                EditorUtility.DisplayDialog("Crafting Item Creator", "Enter a valid item or file name.", "OK");

                return;

            }



            string dataPath = $"{CraftingEditorUtility.ItemsFolder}/{safeName}.asset";

            if (AssetDatabase.LoadAssetAtPath<ItemData>(dataPath) != null &&

                !EditorUtility.DisplayDialog("Crafting Item Creator", $"Item asset '{safeName}' already exists. Overwrite?", "Overwrite", "Cancel"))

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

            item.itemType = itemType;

            item.maxStack = maxStack;

            item.icon = icon;

            item.healthRestore = healthRestore;

            item.energyRestore = energyRestore;

            item.staminaRestore = staminaRestore;

            item.oxygenRestore = oxygenRestore;

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

            EditorUtility.DisplayDialog("Crafting Item Creator", $"Created crafting item '{item.itemName}'.", "OK");

        }

    }

}

