#if UNITY_EDITOR
using Project.Data;
using Project.Interaction;
using Project.Shelter;
using Project.Survival.Exposure;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    public static class QuoraShelterContentSetup
    {
        private const string DeployedPrefabPath = "Assets/_Project/Prefabs/Environment/Shelter/Quora_Shelter_Deployed.prefab";
        private const string WorldVisualPrefabPath = "Assets/_Project/Prefabs/Items/World/Quora Shelter_World.prefab";
        private const string ShelterSafeZonePrefabPath = "Assets/_Project/Prefabs/Environment/Exposure/Shelter_Safe_Zone.prefab";
        private const string InventoryItemPath = "Assets/_Project/Data/Items/Resources/Quora Shelter.asset";
        private const string LegacyConsumableItemPath = "Assets/_Project/Data/Items/Consumables/Quora Shelter.asset";

        [MenuItem("Tools/Dark Matter Genesis/Shelter/Convert Selected To Enterable Shelter")]
        public static void ConvertSelectedToEnterableShelter()
        {
            GameObject[] selected = Selection.gameObjects;
            if (selected == null || selected.Length == 0)
            {
                Debug.LogWarning("Select one or more Quora Shelter objects in the scene or project.");
                return;
            }

            GameObject safeZonePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShelterSafeZonePrefabPath);
            ItemData shelterItem = AssetDatabase.LoadAssetAtPath<ItemData>(InventoryItemPath);
            int converted = 0;

            for (int i = 0; i < selected.Length; i++)
            {
                GameObject target = selected[i];
                if (target == null)
                    continue;

                if (!target.name.Contains("Quora") && !target.name.Contains("Shelter"))
                    continue;

                Undo.RegisterFullObjectHierarchyUndo(target, "Convert Quora Shelter");

                ItemPickup pickup = target.GetComponent<ItemPickup>();
                if (pickup != null)
                    Undo.DestroyObjectImmediate(pickup);

                QuoraShelterController controller = target.GetComponent<QuoraShelterController>();
                if (controller == null)
                    controller = Undo.AddComponent<QuoraShelterController>(target);

                SerializedObject serializedController = new SerializedObject(controller);
                serializedController.FindProperty("shelterItem").objectReferenceValue = shelterItem;
                serializedController.FindProperty("remainingLifetimeSeconds").floatValue = QuoraShelterStorageState.DefaultLifetimeSeconds;
                serializedController.ApplyModifiedPropertiesWithoutUndo();

                if (safeZonePrefab != null && target.transform.Find("Shelter Safe Zone") == null)
                {
                    GameObject safeZone = (GameObject)PrefabUtility.InstantiatePrefab(safeZonePrefab, target.transform);
                    safeZone.name = "Shelter Safe Zone";
                    safeZone.transform.localPosition = Vector3.zero;
                    safeZone.transform.localRotation = Quaternion.identity;
                    safeZone.transform.localScale = Vector3.one;
                }

                converted++;
            }

            Debug.Log(converted > 0
                ? $"Converted {converted} Quora Shelter object(s) to enterable shelters."
                : "No Quora Shelter objects were converted. Select objects named with Quora or Shelter.");
        }

        [MenuItem("Tools/Dark Matter Genesis/Shelter/Setup Quora Shelter Deploy Prefab")]
        public static void SetupQuoraShelterDeployPrefab()
        {
            GameObject worldVisual = AssetDatabase.LoadAssetAtPath<GameObject>(WorldVisualPrefabPath);
            GameObject safeZonePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShelterSafeZonePrefabPath);
            ItemData shelterItem = AssetDatabase.LoadAssetAtPath<ItemData>(InventoryItemPath);
            ItemData legacyConsumable = AssetDatabase.LoadAssetAtPath<ItemData>(LegacyConsumableItemPath);

            if (worldVisual == null || safeZonePrefab == null || shelterItem == null)
            {
                Debug.LogError("Quora Shelter setup failed — missing world visual, safe zone prefab, or inventory item asset.");
                return;
            }

            EnsureFolder("Assets/_Project/Prefabs/Environment/Shelter");

            GameObject root = Object.Instantiate(worldVisual);
            root.name = "Quora_Shelter_Deployed";

            QuoraShelterController controller = root.GetComponent<QuoraShelterController>();
            if (controller == null)
                controller = root.AddComponent<QuoraShelterController>();

            ItemPickup pickup = root.GetComponent<ItemPickup>();
            if (pickup != null)
                Object.DestroyImmediate(pickup);

            GameObject safeZone = (GameObject)PrefabUtility.InstantiatePrefab(safeZonePrefab, root.transform);
            safeZone.name = "Shelter Safe Zone";
            safeZone.transform.localPosition = Vector3.zero;
            safeZone.transform.localRotation = Quaternion.identity;
            safeZone.transform.localScale = Vector3.one;

            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("shelterItem").objectReferenceValue = shelterItem;
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefabAsset = SaveOrReplacePrefab(root, DeployedPrefabPath);
            Object.DestroyImmediate(root);

            SerializedObject serializedItem = new SerializedObject(shelterItem);
            serializedItem.FindProperty("deployedPrefab").objectReferenceValue = prefabAsset;
            serializedItem.ApplyModifiedPropertiesWithoutUndo();

            if (legacyConsumable != null)
            {
                SerializedObject legacyItem = new SerializedObject(legacyConsumable);
                legacyItem.FindProperty("deployedPrefab").objectReferenceValue = prefabAsset;
                legacyItem.ApplyModifiedPropertiesWithoutUndo();
            }

            WireWorldPickupItemData(worldVisual, shelterItem);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Quora Shelter deploy prefab ready at {DeployedPrefabPath} and wired to {InventoryItemPath}.");
        }

        private static void WireWorldPickupItemData(GameObject worldPickupPrefab, ItemData shelterItem)
        {
            if (worldPickupPrefab == null || shelterItem == null)
                return;

            ItemPickup pickup = worldPickupPrefab.GetComponent<ItemPickup>();
            if (pickup == null)
                return;

            SerializedObject serializedPickup = new SerializedObject(pickup);
            serializedPickup.FindProperty("itemData").objectReferenceValue = shelterItem;
            serializedPickup.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(worldPickupPrefab);
        }

        private static GameObject SaveOrReplacePrefab(GameObject root, string path)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
                return PrefabUtility.SaveAsPrefabAsset(root, path);

            return PrefabUtility.SaveAsPrefabAsset(root, path);
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            string parent = System.IO.Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(folderPath);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif
