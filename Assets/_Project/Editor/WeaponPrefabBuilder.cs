using System;
using System.IO;
using Project.Data;
using Project.EditorTools;
using Project.Interaction;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds world pickup and held weapon prefabs from a mesh/model source.
/// </summary>
public static class WeaponPrefabBuilder
{
    public struct PickupOptions
    {
        public int Layer;
        public bool AutoFitCollider;
        public bool CanRespawn;
        public string PromptText;
    }

    public static PickupOptions DefaultPickupOptions => new PickupOptions
    {
        Layer = 7,
        AutoFitCollider = true,
        CanRespawn = false,
        PromptText = "Press E to pick up"
    };

    public static GameObject CreateHeldPrefab(
        GameObject source,
        string prefabName,
        string savePath,
        ItemData itemData = null,
        bool configureHitbox = true)
    {
        EnsureFolder(Path.GetDirectoryName(savePath)?.Replace('\\', '/'));

        GameObject instance = BuildHeldInstance(source, prefabName, itemData, configureHitbox);
        GameObject prefab = SavePrefab(instance, savePath);
        UnityEngine.Object.DestroyImmediate(instance);
        return prefab;
    }

    public static GameObject CreateWorldPickupPrefab(
        GameObject source,
        string prefabName,
        string savePath,
        ItemData itemData,
        PickupOptions options,
        bool configureHitbox = false)
    {
        EnsureFolder(Path.GetDirectoryName(savePath)?.Replace('\\', '/'));

        GameObject instance = BuildWorldInstance(source, prefabName, itemData, options, configureHitbox);
        GameObject prefab = SavePrefab(instance, savePath);
        UnityEngine.Object.DestroyImmediate(instance);
        return prefab;
    }

    public static GameObject CreateWorldPickupPrefab(
        GameObject source,
        string prefabName,
        string savePath,
        ItemData itemData,
        PickupOptions options)
    {
        return CreateWorldPickupPrefab(source, prefabName, savePath, itemData, options, configureHitbox: false);
    }

    public static GameObject BuildWorldInstance(
        GameObject source,
        string instanceName,
        ItemData itemData,
        PickupOptions options,
        bool configureHitbox = false)
    {
        GameObject instance = InstantiateSource(source);
        instance.name = instanceName;
        instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        RemoveExistingPickupComponents(instance);
        ConfigurePickupComponents(instance, itemData, options);

        if (configureHitbox && ShouldConfigureMeleeHitbox(itemData))
            ConfigureWeaponHitbox(instance, itemData);

        return instance;
    }

    public static GameObject BuildHeldInstance(
        GameObject source,
        string instanceName,
        ItemData itemData = null,
        bool configureHitbox = true)
    {
        GameObject instance = InstantiateSource(source);
        instance.name = instanceName;
        instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        RemoveExistingPickupComponents(instance);
        StripForHeld(instance);

        if (configureHitbox)
            ConfigureWeaponHitbox(instance, itemData);

        return instance;
    }

    public static void ConfigureWeaponHitbox(GameObject root, ItemData itemData = null)
    {
        if (root == null)
            return;

        WeaponHitbox.SetupPrefabRoot(root, itemData);
    }

    public static bool ShouldConfigureMeleeHitbox(ItemData itemData)
    {
        if (itemData == null)
            return true;

        return itemData.itemType == ItemType.MeleeWeapon;
    }

    public static ItemData FindItemDataForPrefab(GameObject prefab)
    {
        if (prefab == null)
            return null;

        string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { "Assets/_Project/Data/Items" });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (item == null)
                continue;

            if (item.heldPrefab == prefab || item.worldPrefab == prefab)
                return item;
        }

        return null;
    }

    public static int RefreshWeaponHitboxesInFolder(string folderPath, bool includeWorldPrefabs = false)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !AssetDatabase.IsValidFolder(folderPath))
            return 0;

        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
        int updated = 0;

        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabAsset == null)
                continue;

            ItemData itemData = FindItemDataForPrefab(prefabAsset);
            bool isHeldPrefab = prefabPath.Contains("/Held/", StringComparison.OrdinalIgnoreCase);

            if (isHeldPrefab)
            {
                if (itemData != null && !ShouldConfigureMeleeHitbox(itemData))
                    continue;
            }
            else if (!includeWorldPrefabs)
            {
                continue;
            }
            else if (itemData == null || !ShouldConfigureMeleeHitbox(itemData))
            {
                continue;
            }

            if (prefabAsset.GetComponentInChildren<Renderer>(true) == null)
                continue;

            if (RefreshWeaponHitboxPrefabAtPath(prefabPath, itemData))
                updated++;
        }

        return updated;
    }

    public static bool RefreshWeaponHitboxPrefabAtPath(string prefabPath, ItemData itemData = null)
    {
        if (string.IsNullOrWhiteSpace(prefabPath))
            return false;

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        if (prefabRoot == null)
            return false;

        try
        {
            if (itemData == null)
            {
                GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                itemData = FindItemDataForPrefab(prefabAsset);
            }

            ConfigureWeaponHitbox(prefabRoot, itemData);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    public static void WirePickupItemData(string prefabPath, ItemData itemData)
    {
        if (itemData == null || string.IsNullOrWhiteSpace(prefabPath))
            return;

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            ItemPickup pickup = prefabRoot.GetComponent<ItemPickup>();
            if (pickup == null)
                pickup = prefabRoot.GetComponentInChildren<ItemPickup>();

            if (pickup != null)
            {
                pickup.itemData = itemData;
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    public static void ApplyGripTemplate(ItemData item, ItemData template)
    {
        if (item == null || template == null)
            return;

        item.equipSocketName = template.equipSocketName;
        item.heldLocalPosition = template.heldLocalPosition;
        item.heldLocalEuler = template.heldLocalEuler;
        item.useHeldLocalRotation = template.useHeldLocalRotation;
        item.heldLocalRotation = template.heldLocalRotation;
        item.heldLocalScale = template.heldLocalScale;
        item.swingEulerAngles = template.swingEulerAngles;

        item.sheatheSocketName = template.sheatheSocketName;
        item.sheathedLocalPosition = template.sheathedLocalPosition;
        item.sheathedLocalEuler = template.sheathedLocalEuler;
        item.useSheathedLocalRotation = template.useSheathedLocalRotation;
        item.sheathedLocalRotation = template.sheathedLocalRotation;
        item.sheathedLocalScale = template.sheathedLocalScale;
    }

    public static void ApplyWeaponStatsPreset(ItemData item, WeaponGrip grip)
    {
        if (item == null)
            return;

        item.itemType = ItemType.MeleeWeapon;
        item.weaponGrip = grip;
        item.maxStack = 1;

        if (grip == WeaponGrip.TwoHanded)
        {
            item.meleeDamage = 28f;
            item.meleeDamageRandomRange = 12f;
            item.criticalDamageMultiplier = 3.5f;
            item.meleeRange = 3.4f;
            item.meleeCooldown = 0.85f;
            item.gatherPower = 2;
            item.swingEulerAngles = Vector3.zero;
            return;
        }

        item.meleeDamage = 18f;
        item.meleeDamageRandomRange = 8f;
        item.criticalDamageMultiplier = 2.5f;
        item.meleeRange = 2.6f;
        item.meleeCooldown = 0.55f;
        item.gatherPower = 1;
        item.swingEulerAngles = new Vector3(-90f, 0f, 0f);
    }

    public static bool TryRegisterInItemRegistry(ItemData item)
    {
        if (item == null)
            return false;

        ItemRegistry registry = AssetDatabase.LoadAssetAtPath<ItemRegistry>("Assets/_Project/Resources/ItemRegistry.asset");
        if (registry == null)
            return false;

        SerializedObject serialized = new SerializedObject(registry);
        SerializedProperty itemsProperty = serialized.FindProperty("items");
        if (itemsProperty == null)
            return false;

        for (int i = 0; i < itemsProperty.arraySize; i++)
        {
            SerializedProperty entry = itemsProperty.GetArrayElementAtIndex(i);
            if (entry.objectReferenceValue == item)
                return true;
        }

        itemsProperty.InsertArrayElementAtIndex(itemsProperty.arraySize);
        itemsProperty.GetArrayElementAtIndex(itemsProperty.arraySize - 1).objectReferenceValue = item;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(registry);
        return true;
    }

    public static GameObject InstantiateSource(GameObject source)
    {
        GameObject instance = PrefabUtility.InstantiatePrefab(source) as GameObject;
        if (instance == null)
            instance = UnityEngine.Object.Instantiate(source);

        return instance;
    }

    public static void StripForHeld(GameObject root)
    {
        foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            UnityEngine.Object.DestroyImmediate(collider);

        foreach (Rigidbody body in root.GetComponentsInChildren<Rigidbody>(true))
            UnityEngine.Object.DestroyImmediate(body);

        foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour is ItemPickup || behaviour is ResourceNode)
                UnityEngine.Object.DestroyImmediate(behaviour);
        }
    }

    public static string SanitizeAssetName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
            return string.Empty;

        char[] invalid = Path.GetInvalidFileNameChars();
        string sanitized = rawName.Trim();
        foreach (char c in invalid)
            sanitized = sanitized.Replace(c, '_');

        return sanitized.Replace('/', '_').Replace('\\', '_');
    }

    public static void EnsureFolder(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || AssetDatabase.IsValidFolder(folderPath))
            return;

        folderPath = folderPath.Replace('\\', '/');
        string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
        if (!string.IsNullOrEmpty(parent))
            EnsureFolder(parent);

        string folderName = Path.GetFileName(folderPath);
        if (string.IsNullOrEmpty(folderName) || string.IsNullOrEmpty(parent))
            return;

        if (!AssetDatabase.IsValidFolder(folderPath))
            AssetDatabase.CreateFolder(parent, folderName);
    }

    private static GameObject SavePrefab(GameObject instance, string path)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            AssetDatabase.DeleteAsset(path);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, path, out bool success);
        if (!success || prefab == null)
            throw new InvalidOperationException($"Failed to save prefab at {path}");

        return prefab;
    }

    private static void ConfigurePickupComponents(GameObject instance, ItemData itemData, PickupOptions options)
    {
        instance.layer = options.Layer;

        Collider collider = instance.GetComponentInChildren<Collider>();
        if (collider == null)
            collider = instance.AddComponent<BoxCollider>();

        collider.isTrigger = true;

        if (options.AutoFitCollider && collider is BoxCollider boxCollider)
            FitBoxCollider(instance, boxCollider);

        ItemPickup pickup = instance.GetComponent<ItemPickup>();
        if (pickup == null)
            pickup = instance.AddComponent<ItemPickup>();

        pickup.itemData = itemData;
        pickup.amount = 1;
        pickup.promptText = string.IsNullOrWhiteSpace(options.PromptText)
            ? "Press E to pick up"
            : options.PromptText;
        pickup.canRespawn = options.CanRespawn;
    }

    private static void FitBoxCollider(GameObject root, BoxCollider boxCollider)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        Transform transform = boxCollider.transform;
        boxCollider.center = transform.InverseTransformPoint(bounds.center);
        Vector3 lossy = transform.lossyScale;
        boxCollider.size = new Vector3(
            SafeDivide(bounds.size.x, Mathf.Abs(lossy.x)),
            SafeDivide(bounds.size.y, Mathf.Abs(lossy.y)),
            SafeDivide(bounds.size.z, Mathf.Abs(lossy.z)));
    }

    private static float SafeDivide(float value, float divisor)
    {
        return Mathf.Approximately(divisor, 0f) ? value : value / divisor;
    }

    private static void RemoveExistingPickupComponents(GameObject root)
    {
        foreach (ItemPickup pickup in root.GetComponentsInChildren<ItemPickup>(true))
            UnityEngine.Object.DestroyImmediate(pickup);

        foreach (ResourceNode node in root.GetComponentsInChildren<ResourceNode>(true))
            UnityEngine.Object.DestroyImmediate(node);

        MapMarkerEditorUtility.RemoveMapMarkers(root);
    }

    [MenuItem(SurvivalPioneerEditorMenus.AddWeaponHitboxToSelectedPrefab, false, 14)]
    public static void AddHitboxToSelectedPrefab()
    {
        UnityEngine.Object[] selected = Selection.objects;
        if (selected == null || selected.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "Weapon Hitbox",
                "Select one or more weapon prefabs in the Project window.",
                "OK");
            return;
        }

        int updated = 0;
        for (int i = 0; i < selected.Length; i++)
        {
            GameObject selectedObject = selected[i] as GameObject;
            if (selectedObject == null)
                continue;

            string path = AssetDatabase.GetAssetPath(selectedObject);
            if (string.IsNullOrWhiteSpace(path) || !path.EndsWith(".prefab"))
                continue;

            ItemData itemData = FindItemDataForPrefab(selectedObject);
            if (RefreshWeaponHitboxPrefabAtPath(path, itemData))
                updated++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Weapon Hitbox",
            updated > 0
                ? $"Updated hitbox setup on {updated} prefab(s)."
                : "No prefab assets were updated. Select .prefab assets in the Project window.",
            "OK");
    }

    [MenuItem(SurvivalPioneerEditorMenus.AddWeaponHitboxToSelectedPrefab, true)]
    private static bool ValidateAddHitboxToSelectedPrefab()
    {
        UnityEngine.Object[] selected = Selection.objects;
        if (selected == null || selected.Length == 0)
            return false;

        for (int i = 0; i < selected.Length; i++)
        {
            GameObject selectedObject = selected[i] as GameObject;
            if (selectedObject == null)
                continue;

            string path = AssetDatabase.GetAssetPath(selectedObject);
            if (!string.IsNullOrWhiteSpace(path) && path.EndsWith(".prefab"))
                return true;
        }

        return false;
    }

    [MenuItem(SurvivalPioneerEditorMenus.RefreshAllWeaponHitboxes, false, 15)]
    public static void RefreshAllWeaponHitboxes()
    {
        if (!EditorUtility.DisplayDialog(
                "Weapon Hitbox",
                "Rebuild WeaponHitbox components and capsule colliders on all held weapon prefabs " +
                "and melee world prefabs referenced by ItemData?",
                "Refresh",
                "Cancel"))
        {
            return;
        }

        const string itemsPrefabFolder = "Assets/_Project/Prefabs/Items";
        const string heldPrefabFolder = "Assets/_Project/Prefabs/Items/Held";

        int heldUpdated = RefreshWeaponHitboxesInFolder(heldPrefabFolder);
        int itemsUpdated = RefreshWeaponHitboxesInFolder(itemsPrefabFolder, includeWorldPrefabs: true);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Weapon Hitbox",
            $"Updated {heldUpdated} held prefab(s) and {itemsUpdated} item prefab(s).",
            "OK");
    }
}
