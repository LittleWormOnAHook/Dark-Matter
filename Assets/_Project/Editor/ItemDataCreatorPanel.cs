using Project.Data;
using Project.Interaction;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Shared UI + create logic for Item Data Creator (embedded in Blueprint and Crafting Manager).
    /// </summary>
    public sealed class ItemDataCreatorPanel
    {
        private enum CreatorKind
        {
            HealConsumable,
            Resource,
            ThrowableGrenade
        }

        private string itemName = "New Item";
        private int maxStack = 64;
        private float healthRestore;
        private float energyRestore;
        private float staminaRestore;
        private float oxygenRestore;
        private bool isAcInfused;
        private int acValue;
        private string tooltipDescription = string.Empty;
        private CreatorKind creatorKind = CreatorKind.HealConsumable;

        private GameObject worldPrefabTemplate;
        private bool addResourceNode;
        private bool addCraftingComponent;
        private GameObject gatherVFXPrefab;

        private Vector2 scroll;

        public void Draw()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);

            EditorGUILayout.LabelField("Item Data Creator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Create ItemData + optional world prefab. Resources can get ResourceNode (gatherable); " +
                "consumables/throwables get ItemPickup. Grenades prefer the dedicated Grenade / Throwable creator.",
                MessageType.None);
            EditorGUILayout.Space(8f);

            creatorKind = (CreatorKind)EditorGUILayout.EnumPopup("Kind", creatorKind);
            itemName = EditorGUILayout.TextField("Item Name", itemName);
            maxStack = EditorGUILayout.IntField("Max Stack", Mathf.Max(1, maxStack));
            tooltipDescription = EditorGUILayout.TextField("Tooltip", tooltipDescription);

            if (creatorKind == CreatorKind.HealConsumable)
            {
                EditorGUILayout.Space(10f);
                EditorGUILayout.LabelField("Survival Restore", EditorStyles.boldLabel);
                healthRestore = EditorGUILayout.FloatField("Health Restore", healthRestore);
                energyRestore = EditorGUILayout.FloatField("Energy Restore", energyRestore);
                staminaRestore = EditorGUILayout.FloatField("Stamina Restore", staminaRestore);
                oxygenRestore = EditorGUILayout.FloatField("Oxygen Restore (display sec)", oxygenRestore);

                EditorGUILayout.Space(10f);
                EditorGUILayout.LabelField("Aether Credits", EditorStyles.boldLabel);
                isAcInfused = EditorGUILayout.Toggle("Grants AC On Pickup", isAcInfused);
                if (isAcInfused)
                    acValue = EditorGUILayout.IntField("AC Value", acValue);
            }
            else
            {
                healthRestore = energyRestore = staminaRestore = oxygenRestore = 0f;
                isAcInfused = false;
                acValue = 0;
            }

            EditorGUILayout.Space(15f);
            EditorGUILayout.LabelField("World Prefab", EditorStyles.boldLabel);
            worldPrefabTemplate = (GameObject)EditorGUILayout.ObjectField(
                "World Prefab", worldPrefabTemplate, typeof(GameObject), false);

            if (creatorKind == CreatorKind.Resource)
            {
                EditorGUILayout.Space(10f);
                EditorGUILayout.LabelField("Auto Components", EditorStyles.boldLabel);
                addResourceNode = EditorGUILayout.Toggle("Add ResourceNode (Gatherable)", addResourceNode);
                addCraftingComponent = EditorGUILayout.Toggle("Add Crafting Component", addCraftingComponent);
                gatherVFXPrefab = (GameObject)EditorGUILayout.ObjectField(
                    "Gather VFX (Optional)", gatherVFXPrefab, typeof(GameObject), false);
            }
            else
            {
                addResourceNode = false;
                addCraftingComponent = false;
            }

            EditorGUILayout.Space(20f);
            if (GUILayout.Button("Create ItemData + Prefab", GUILayout.Height(50f)))
                CreateFullItem();

            EditorGUILayout.EndScrollView();
        }

        private void CreateFullItem()
        {
            if (string.IsNullOrEmpty(itemName))
            {
                EditorUtility.DisplayDialog("Item Data Creator", "Item Name is required!", "OK");
                return;
            }

            string safeName = CraftingEditorUtility.SanitizeAssetName(itemName);
            ItemData newItem = ScriptableObject.CreateInstance<ItemData>();
            newItem.itemName = itemName.Trim();
            newItem.maxStack = maxStack;
            newItem.tooltipDescription = tooltipDescription;

            string folder;
            switch (creatorKind)
            {
                case CreatorKind.Resource:
                    newItem.itemType = ItemType.Resource;
                    folder = ProjectAssetPaths.ItemsResources;
                    break;
                case CreatorKind.ThrowableGrenade:
                    newItem.itemType = ItemType.Consumable;
                    folder = ProjectAssetPaths.ItemsThrowables;
                    break;
                default:
                    newItem.itemType = ItemType.Consumable;
                    newItem.healthRestore = healthRestore;
                    newItem.energyRestore = energyRestore;
                    newItem.staminaRestore = staminaRestore;
                    newItem.oxygenRestore = oxygenRestore;
                    newItem.isAcInfused = isAcInfused;
                    newItem.acValue = acValue;
                    folder = ProjectAssetPaths.ItemsConsumables;
                    break;
            }

            CraftingEditorUtility.EnsureFolder(folder);
            string dataPath = $"{folder}/{safeName}.asset";
            AssetDatabase.CreateAsset(newItem, dataPath);

            ItemDataInspectorCategory category = ItemDataInspectorCategoryResolver.Resolve(newItem);
            ItemDataPruneUtility.Prune(newItem, category);

            if (worldPrefabTemplate != null)
            {
                GameObject instance = Object.Instantiate(worldPrefabTemplate);
                instance.name = safeName + "_World";

                if (addResourceNode)
                {
                    ResourceNode rn = instance.AddComponent<ResourceNode>();
                    rn.resourceItem = newItem;
                }

                if (addCraftingComponent)
                    instance.AddComponent<BoxCollider>();

                if (creatorKind != CreatorKind.Resource || !addResourceNode)
                {
                    ItemPickup pickup = instance.GetComponent<ItemPickup>();
                    if (pickup == null)
                        pickup = instance.AddComponent<ItemPickup>();
                    pickup.itemData = newItem;
                    pickup.amount = 1;
                }

                CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsItemsWorld);
                string prefabPath = $"{ProjectAssetPaths.PrefabsItemsWorld}/{safeName}_World.prefab";
                PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                Object.DestroyImmediate(instance);

                newItem.worldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                EditorUtility.SetDirty(newItem);
            }

            CraftingEditorUtility.AddItemToRegistry(newItem);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog(
                "Item Data Creator",
                $"Item '{itemName}' created.\n\nItemData: {dataPath}\nPrefab: {(worldPrefabTemplate != null ? "Created" : "None")}",
                "OK");

            itemName = "New Item";
            worldPrefabTemplate = null;
            Selection.activeObject = newItem;
        }
    }
}
