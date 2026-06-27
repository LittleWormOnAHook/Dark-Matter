using Project.Data;
using Project.EditorTools;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor window for creating weapon world/held prefabs and optional ItemData assets.
/// </summary>
public class WeaponPrefabCreatorWindow : EditorWindow
{
    private const string ItemsDataFolder = "Assets/_Project/Data/Items";
    private const string WorldPrefabFolder = "Assets/_Project/Prefabs/Items";
    private const string HeldPrefabFolder = "Assets/_Project/Prefabs/Items/Held";
    private const string IconsFolder = "Assets/_Project/Art/Icons";
    private const string OneHandTemplatePath = "Assets/_Project/Data/Items/weap2_sword.asset";
    private const string TwoHandTemplatePath = "Assets/_Project/Data/Items/weap_two_handed.asset";

    private string weaponName = "New Weapon";
    private GameObject meshSource;
    private WeaponGrip weaponGrip = WeaponGrip.OneHanded;
    private ItemData gripTemplate;

    private bool createWorldPrefab = true;
    private bool createHeldPrefab = true;
    private bool createItemData = true;
    private bool registerInItemRegistry = true;
    private bool copyGripFromTemplate = true;
    private bool autoGenerateIcon = true;

    private float meleeDamage = 18f;
    private float meleeDamageRandomRange = 8f;
    private float criticalDamageMultiplier = 2.5f;
    private float meleeRange = 2.6f;
    private float meleeCooldown = 0.55f;
    private int gatherPower = 1;

    private WeaponPrefabBuilder.PickupOptions pickupOptions = WeaponPrefabBuilder.DefaultPickupOptions;

    [MenuItem(SurvivalPioneerEditorMenus.Combat + "Weapon Prefab Creator")]
    public static void ShowWindow()
    {
        WeaponPrefabCreatorWindow window = GetWindow<WeaponPrefabCreatorWindow>("Weapon Prefabs");
        window.minSize = new Vector2(420, 620);
    }

    [MenuItem(SurvivalPioneerEditorMenus.Combat + "Weapon Prefab Creator From Selection")]
    private static void OpenFromSelection()
    {
        WeaponPrefabCreatorWindow window = GetWindow<WeaponPrefabCreatorWindow>("Weapon Prefabs");
        window.minSize = new Vector2(420, 620);
        window.UseSelectionAsSource();
    }

    private void OnEnable()
    {
        ApplyGripTemplateDefaults();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Weapon Prefab Creator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Build world pickup and held weapon prefabs from a mesh or model. " +
            "Optionally create ItemData and register it for saves/pickups.",
            MessageType.Info);

        EditorGUILayout.Space(6f);
        weaponName = EditorGUILayout.TextField("Weapon Name", weaponName);
        meshSource = (GameObject)EditorGUILayout.ObjectField("Mesh / Model Source", meshSource, typeof(GameObject), false);

        EditorGUI.BeginChangeCheck();
        weaponGrip = (WeaponGrip)EditorGUILayout.EnumPopup("Grip", weaponGrip);
        if (EditorGUI.EndChangeCheck())
            ApplyGripTemplateDefaults();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Use Selection"))
            UseSelectionAsSource();
        if (GUILayout.Button("Apply Stat Preset"))
            ApplyStatPresetFromGrip();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
        createWorldPrefab = EditorGUILayout.Toggle("World Pickup Prefab", createWorldPrefab);
        createHeldPrefab = EditorGUILayout.Toggle("Held Prefab", createHeldPrefab);
        createItemData = EditorGUILayout.Toggle("ItemData Asset", createItemData);

        using (new EditorGUI.DisabledScope(!createItemData))
            registerInItemRegistry = EditorGUILayout.Toggle("Add To Item Registry", registerInItemRegistry);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Pickup", EditorStyles.boldLabel);
        pickupOptions.Layer = EditorGUILayout.LayerField("Layer", pickupOptions.Layer);
        pickupOptions.AutoFitCollider = EditorGUILayout.Toggle("Auto-fit Collider", pickupOptions.AutoFitCollider);
        pickupOptions.CanRespawn = EditorGUILayout.Toggle("Can Respawn", pickupOptions.CanRespawn);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Melee Stats", EditorStyles.boldLabel);
        meleeDamage = EditorGUILayout.FloatField("Damage", meleeDamage);
        meleeDamageRandomRange = EditorGUILayout.FloatField("Damage Random Range", meleeDamageRandomRange);
        criticalDamageMultiplier = EditorGUILayout.FloatField("Critical Multiplier", criticalDamageMultiplier);
        meleeRange = EditorGUILayout.FloatField("Range", meleeRange);
        meleeCooldown = EditorGUILayout.FloatField("Cooldown", meleeCooldown);
        gatherPower = EditorGUILayout.IntField("Gather Power", gatherPower);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Grip Template", EditorStyles.boldLabel);
        copyGripFromTemplate = EditorGUILayout.Toggle("Copy Hand/Back Grip", copyGripFromTemplate);
        using (new EditorGUI.DisabledScope(!copyGripFromTemplate))
        {
            gripTemplate = (ItemData)EditorGUILayout.ObjectField("Template ItemData", gripTemplate, typeof(ItemData), false);
        }

        autoGenerateIcon = EditorGUILayout.Toggle("Auto-generate Icon", autoGenerateIcon);

        EditorGUILayout.Space(16f);
        using (new EditorGUI.DisabledScope(!CanCreate()))
        {
            if (GUILayout.Button("Create Weapon Prefabs", GUILayout.Height(44f)))
                CreateWeaponPrefabs();
        }
    }

    private void UseSelectionAsSource()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("Weapon Prefab Creator", "Select a mesh or prefab in the Hierarchy first.", "OK");
            return;
        }

        meshSource = selected;
        if (string.IsNullOrWhiteSpace(weaponName) || weaponName == "New Weapon")
            weaponName = selected.name.Replace("(Clone)", string.Empty).Trim();

        Repaint();
    }

    private void ApplyGripTemplateDefaults()
    {
        string templatePath = weaponGrip == WeaponGrip.TwoHanded ? TwoHandTemplatePath : OneHandTemplatePath;
        gripTemplate = AssetDatabase.LoadAssetAtPath<ItemData>(templatePath);
        ApplyStatPresetFromGrip();
    }

    private void ApplyStatPresetFromGrip()
    {
        ItemData temp = ScriptableObject.CreateInstance<ItemData>();
        WeaponPrefabBuilder.ApplyWeaponStatsPreset(temp, weaponGrip);
        meleeDamage = temp.meleeDamage;
        meleeDamageRandomRange = temp.meleeDamageRandomRange;
        criticalDamageMultiplier = temp.criticalDamageMultiplier;
        meleeRange = temp.meleeRange;
        meleeCooldown = temp.meleeCooldown;
        gatherPower = temp.gatherPower;
        DestroyImmediate(temp);
    }

    private bool CanCreate()
    {
        if (meshSource == null || string.IsNullOrWhiteSpace(weaponName))
            return false;

        return createWorldPrefab || createHeldPrefab || createItemData;
    }

    private void CreateWeaponPrefabs()
    {
        string safeName = WeaponPrefabBuilder.SanitizeAssetName(weaponName);
        if (string.IsNullOrEmpty(safeName))
        {
            EditorUtility.DisplayDialog("Weapon Prefab Creator", "Weapon name is invalid.", "OK");
            return;
        }

        string dataPath = $"{ItemsDataFolder}/{safeName}.asset";
        string worldPath = $"{WorldPrefabFolder}/{safeName}.prefab";
        string heldPath = $"{HeldPrefabFolder}/{safeName}_Held.prefab";

        if (AssetExists(dataPath, worldPath, heldPath) &&
            !EditorUtility.DisplayDialog("Weapon Prefab Creator", $"Assets named '{safeName}' already exist. Overwrite?", "Overwrite", "Cancel"))
            return;

        WeaponPrefabBuilder.EnsureFolder(ItemsDataFolder);
        WeaponPrefabBuilder.EnsureFolder(WorldPrefabFolder);
        WeaponPrefabBuilder.EnsureFolder(HeldPrefabFolder);

        ItemData itemData = null;
        if (createItemData)
        {
            itemData = AssetDatabase.LoadAssetAtPath<ItemData>(dataPath);
            if (itemData == null)
            {
                itemData = ScriptableObject.CreateInstance<ItemData>();
                AssetDatabase.CreateAsset(itemData, dataPath);
            }
        }

        if (itemData != null)
        {
            itemData.itemName = weaponName.Trim();
            itemData.weaponGrip = weaponGrip;
            itemData.meleeDamage = meleeDamage;
            itemData.meleeDamageRandomRange = meleeDamageRandomRange;
            itemData.criticalDamageMultiplier = criticalDamageMultiplier;
            itemData.meleeRange = meleeRange;
            itemData.meleeCooldown = meleeCooldown;
            itemData.gatherPower = gatherPower;
            itemData.maxStack = 1;
            itemData.itemType = ItemType.MeleeWeapon;

            if (copyGripFromTemplate && gripTemplate != null)
                WeaponPrefabBuilder.ApplyGripTemplate(itemData, gripTemplate);

            if (autoGenerateIcon && meshSource != null)
            {
                WeaponPrefabBuilder.EnsureFolder(IconsFolder);
                Sprite icon = EquipmentIconGenerator.SaveSpriteAsset(
                    meshSource,
                    $"{IconsFolder}/{safeName}_Icon.png",
                    new EquipmentIconGenerator.Settings
                    {
                        Size = 128,
                        ModelRotation = new Vector3(0f, 90f, 0f),
                        Padding = 1.15f,
                        TransparentBackground = true
                    });
                if (icon != null)
                    itemData.icon = icon;
            }
        }

        GameObject worldPrefab = null;
        GameObject heldPrefab = null;

        if (createWorldPrefab)
        {
            worldPrefab = WeaponPrefabBuilder.CreateWorldPickupPrefab(
                meshSource,
                safeName,
                worldPath,
                itemData,
                pickupOptions);
        }

        if (createHeldPrefab)
        {
            heldPrefab = WeaponPrefabBuilder.CreateHeldPrefab(meshSource, safeName + "_Held", heldPath);
        }

        if (itemData != null)
        {
            if (worldPrefab != null)
                itemData.worldPrefab = worldPrefab;

            if (heldPrefab != null)
                itemData.heldPrefab = heldPrefab;
            else if (worldPrefab != null)
                itemData.heldPrefab = worldPrefab;

            if (worldPrefab != null)
                WeaponPrefabBuilder.WirePickupItemData(worldPath, itemData);

            EditorUtility.SetDirty(itemData);

            if (registerInItemRegistry)
                WeaponPrefabBuilder.TryRegisterInItemRegistry(itemData);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = itemData != null ? itemData : worldPrefab != null ? worldPrefab : heldPrefab;
        EditorGUIUtility.PingObject(Selection.activeObject);

        EditorUtility.DisplayDialog(
            "Weapon Prefab Creator",
            BuildSummary(safeName, dataPath, worldPath, heldPath, itemData != null),
            "OK");
    }

    private static bool AssetExists(string dataPath, string worldPath, string heldPath)
    {
        return AssetDatabase.LoadAssetAtPath<Object>(dataPath) != null ||
               AssetDatabase.LoadAssetAtPath<Object>(worldPath) != null ||
               AssetDatabase.LoadAssetAtPath<Object>(heldPath) != null;
    }

    private static string BuildSummary(string safeName, string dataPath, string worldPath, string heldPath, bool createdItemData)
    {
        string summary = $"Created weapon '{safeName}'.\n\n";
        if (AssetDatabase.LoadAssetAtPath<Object>(worldPath) != null)
            summary += $"World: {worldPath}\n";
        if (AssetDatabase.LoadAssetAtPath<Object>(heldPath) != null)
            summary += $"Held: {heldPath}\n";
        if (createdItemData)
            summary += $"ItemData: {dataPath}\n";

        summary += "\nTune grip in Play mode, then bake with Tools/Project grip bakers.";
        return summary;
    }
}
