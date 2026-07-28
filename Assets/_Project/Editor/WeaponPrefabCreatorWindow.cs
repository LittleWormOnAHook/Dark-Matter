using Project.Data;
using Project.EditorTools;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor window for creating weapon world/held prefabs and optional ItemData assets.
/// </summary>
public class WeaponPrefabCreatorWindow : EditorWindow
{
    private const string IconsFolder = ProjectAssetPaths.ArtIcons;
    private const string OneHandTemplatePath = ProjectAssetPaths.ItemsMelee + "/weap2_sword.asset";
    private const string TwoHandTemplatePath = ProjectAssetPaths.ItemsMelee + "/weap_two_handed.asset";

    private string weaponName = "New Weapon";
    private GameObject meshSource;
    private ItemType weaponItemType = ItemType.MeleeWeapon;
    private WeaponGrip weaponGrip = WeaponGrip.OneHanded;
    private ItemData gripTemplate;

    private bool createWorldPrefab = true;
    private bool createHeldPrefab = true;
    private bool createItemData = true;
    private bool registerInItemRegistry = true;
    private bool copyGripFromTemplate = true;
    private bool autoGenerateIcon = true;
    private bool addMeleeHitbox = true;
    private bool addHitboxToWorldPrefab = false;

    private float meleeDamage = 18f;
    private float meleeDamageRandomRange = 8f;
    private float criticalChance = 0.1f;
    private float criticalDamageMultiplier = 2.5f;
    private float meleeRange = 2.6f;
    private float meleeCooldown = 0.55f;
    private float meleeStaminaCost = 8f;
    private float meleeKnockback;
    private int gatherPower = 1;

    private float rangedDamage = 14f;
    private float rangedDamageRandomRange = 4f;
    private float rangedRange = 45f;
    private float projectileSpeed = 120f;
    private float projectileSpreadDegrees = 1.5f;
    private float weaponAccuracy = 75f;
    private float closeRangeFullAccuracyDistance = 12f;
    private float closeRangeSpreadScale = 0.2f;
    private float recoilVertical = 2.5f;
    private float recoilHorizontal = 0.7f;
    private float recoilFireRateScale = 4.5f;
    private float fireRate = 4f;
    private int magazineSize = 30;
    private float reloadTimeSeconds = 1.8f;
    private float hipFireMaxDeviationDegrees = 15f;
    private float hipFireSpreadMultiplier = 1f;

    private bool isMiningTool;
    private int miningPassesRequired = 2;
    private int miningDropMin = 1;
    private int miningDropMax = 5;
    private float miningLockBreakDegrees = 30f;
    private float miningPassDuration = 1.25f;

    private WeaponPrefabBuilder.PickupOptions pickupOptions = WeaponPrefabBuilder.DefaultPickupOptions;

    [MenuItem(SurvivalPioneerEditorMenus.WeaponPrefabCreator, false, 10)]
    public static void ShowWindow()
    {
        WeaponPrefabCreatorWindow window = GetWindow<WeaponPrefabCreatorWindow>("Weapon Prefabs");
        window.minSize = new Vector2(420, 620);
    }

    [MenuItem(SurvivalPioneerEditorMenus.WeaponPrefabCreatorFromSelection, false, 11)]
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
        bool isRangedToggle = EditorGUILayout.Toggle(
            new GUIContent("Ranged Weapon", "Enables velocity / accuracy / spread authoring on the ItemData."),
            weaponItemType == ItemType.RangedWeapon);
        weaponItemType = isRangedToggle ? ItemType.RangedWeapon : ItemType.MeleeWeapon;

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

        bool isRanged = weaponItemType == ItemType.RangedWeapon;

        if (!isRanged)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Melee Base Stats", EditorStyles.boldLabel);
            meleeDamage = EditorGUILayout.FloatField("Damage", meleeDamage);
            meleeDamageRandomRange = EditorGUILayout.FloatField("Damage Random Range", meleeDamageRandomRange);
            criticalChance = EditorGUILayout.Slider("Crit Chance", criticalChance, 0f, 1f);
            criticalDamageMultiplier = EditorGUILayout.FloatField("Critical Multiplier", criticalDamageMultiplier);
            meleeRange = EditorGUILayout.FloatField("Range", meleeRange);
            meleeCooldown = EditorGUILayout.FloatField("Cooldown", meleeCooldown);
            meleeStaminaCost = EditorGUILayout.FloatField("Stamina Cost", meleeStaminaCost);
            meleeKnockback = EditorGUILayout.FloatField("Knockback", meleeKnockback);
            gatherPower = EditorGUILayout.IntField("Gather Power", gatherPower);
        }
        else
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Ranged Base Stats", EditorStyles.boldLabel);
            rangedDamage = EditorGUILayout.FloatField("Damage", rangedDamage);
            rangedDamageRandomRange = EditorGUILayout.FloatField("Damage Random Range", rangedDamageRandomRange);
            rangedRange = EditorGUILayout.FloatField("Range", rangedRange);
            fireRate = EditorGUILayout.FloatField("Fire Rate", fireRate);
            magazineSize = EditorGUILayout.IntField("Magazine Size", magazineSize);
            reloadTimeSeconds = EditorGUILayout.FloatField("Reload Time (s)", reloadTimeSeconds);
            projectileSpeed = EditorGUILayout.FloatField(
                new GUIContent("Projectile Speed", "Fallback velocity when ammo has no override. Ammo.projectileSpeed is primary."),
                projectileSpeed);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Accuracy / Spread", EditorStyles.boldLabel);
            projectileSpreadDegrees = EditorGUILayout.FloatField("Spread Degrees", projectileSpreadDegrees);
            weaponAccuracy = EditorGUILayout.Slider("Weapon Accuracy", weaponAccuracy, 0f, 100f);
            closeRangeFullAccuracyDistance = EditorGUILayout.FloatField("Close Range Dist (m)", closeRangeFullAccuracyDistance);
            closeRangeSpreadScale = EditorGUILayout.Slider("Close Spread Scale", closeRangeSpreadScale, 0f, 1f);
            hipFireMaxDeviationDegrees = EditorGUILayout.FloatField("Hip Max Deviation", hipFireMaxDeviationDegrees);
            hipFireSpreadMultiplier = EditorGUILayout.FloatField("Hip Spread Multiplier", hipFireSpreadMultiplier);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Recoil", EditorStyles.boldLabel);
            recoilVertical = EditorGUILayout.FloatField("Recoil Vertical", recoilVertical);
            recoilHorizontal = EditorGUILayout.FloatField("Recoil Horizontal", recoilHorizontal);
            recoilFireRateScale = EditorGUILayout.FloatField("Recoil Fire-Rate Scale", recoilFireRateScale);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Mining Tool", EditorStyles.boldLabel);
            isMiningTool = EditorGUILayout.Toggle(
                new GUIContent("Mining Tool", "Hold Fire to mine ResourceNodes with a soft-locked laser. Skips combat hitscan and ammo."),
                isMiningTool);
            using (new EditorGUI.DisabledScope(!isMiningTool))
            {
                miningPassesRequired = EditorGUILayout.IntField("Passes Required", Mathf.Max(1, miningPassesRequired));
                miningDropMin = EditorGUILayout.IntField("Drop Min", Mathf.Max(1, miningDropMin));
                miningDropMax = EditorGUILayout.IntField("Drop Max", Mathf.Max(miningDropMin, miningDropMax));
                miningLockBreakDegrees = EditorGUILayout.FloatField("Lock Break Degrees", miningLockBreakDegrees);
                miningPassDuration = EditorGUILayout.FloatField("Pass Duration (s)", miningPassDuration);
            }
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Grip Template", EditorStyles.boldLabel);
        copyGripFromTemplate = EditorGUILayout.Toggle("Copy Hand/Back Grip", copyGripFromTemplate);
        using (new EditorGUI.DisabledScope(!copyGripFromTemplate))
        {
            gripTemplate = (ItemData)EditorGUILayout.ObjectField("Template ItemData", gripTemplate, typeof(ItemData), false);
        }

        autoGenerateIcon = EditorGUILayout.Toggle("Auto-generate Icon", autoGenerateIcon);

        if (!isRanged)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Melee Hitbox", EditorStyles.boldLabel);
            addMeleeHitbox = EditorGUILayout.Toggle("Add To Held Prefab", addMeleeHitbox);
            using (new EditorGUI.DisabledScope(!createHeldPrefab))
                addHitboxToWorldPrefab = EditorGUILayout.Toggle("Add To World Prefab", addHitboxToWorldPrefab);
            EditorGUILayout.HelpBox(
                "Adds WeaponHitbox + a child capsule collider fit to the blade. " +
                "Tune strikeEndBias and hitboxLocalOffset on the prefab after creation.",
                MessageType.None);
        }

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

        string itemsDataFolder = weaponItemType == ItemType.RangedWeapon
            ? ProjectAssetPaths.ItemsRanged
            : ProjectAssetPaths.ItemsMelee;
        string weaponsPrefabFolder = weaponItemType == ItemType.RangedWeapon
            ? ProjectAssetPaths.PrefabsWeaponsRanged
            : ProjectAssetPaths.PrefabsWeaponsMelee;
        string dataPath = $"{itemsDataFolder}/{safeName}.asset";
        string worldPath = $"{weaponsPrefabFolder}/{safeName}.prefab";
        string heldPath = $"{weaponsPrefabFolder}/{safeName}_Held.prefab";

        if (AssetExists(dataPath, worldPath, heldPath) &&
            !EditorUtility.DisplayDialog("Weapon Prefab Creator", $"Assets named '{safeName}' already exist. Overwrite?", "Overwrite", "Cancel"))
            return;

        WeaponPrefabBuilder.EnsureFolder(itemsDataFolder);
        WeaponPrefabBuilder.EnsureFolder(weaponsPrefabFolder);

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
            itemData.maxStack = 1;
            itemData.itemType = weaponItemType == ItemType.RangedWeapon
                ? ItemType.RangedWeapon
                : ItemType.MeleeWeapon;

            if (itemData.itemType == ItemType.MeleeWeapon)
            {
                itemData.meleeDamage = meleeDamage;
                itemData.meleeDamageRandomRange = meleeDamageRandomRange;
                itemData.criticalChance = criticalChance;
                itemData.criticalDamageMultiplier = criticalDamageMultiplier;
                itemData.meleeRange = meleeRange;
                itemData.meleeCooldown = meleeCooldown;
                itemData.meleeStaminaCost = meleeStaminaCost;
                itemData.meleeKnockback = meleeKnockback;
                itemData.gatherPower = gatherPower;
            }
            else
            {
                itemData.rangedDamage = rangedDamage;
                itemData.rangedDamageRandomRange = rangedDamageRandomRange;
                itemData.rangedRange = rangedRange;
                itemData.projectileSpeed = projectileSpeed;
                itemData.projectileSpreadDegrees = projectileSpreadDegrees;
                itemData.weaponAccuracy = weaponAccuracy;
                itemData.closeRangeFullAccuracyDistance = closeRangeFullAccuracyDistance;
                itemData.closeRangeSpreadScale = closeRangeSpreadScale;
                itemData.recoilVertical = recoilVertical;
                itemData.recoilHorizontal = recoilHorizontal;
                itemData.recoilFireRateScale = recoilFireRateScale;
                itemData.fireRate = fireRate;
                itemData.magazineSize = magazineSize;
                itemData.reloadTimeSeconds = reloadTimeSeconds;
                itemData.hipFireMaxDeviationDegrees = hipFireMaxDeviationDegrees;
                itemData.hipFireSpreadMultiplier = hipFireSpreadMultiplier;
                itemData.isMiningTool = isMiningTool;
                if (isMiningTool)
                {
                    itemData.isHitscanBeam = true;
                    itemData.miningPassesRequired = Mathf.Max(1, miningPassesRequired);
                    itemData.miningDropMin = Mathf.Max(1, miningDropMin);
                    itemData.miningDropMax = Mathf.Max(itemData.miningDropMin, miningDropMax);
                    itemData.miningLockBreakDegrees = Mathf.Max(5f, miningLockBreakDegrees);
                    itemData.miningPassDuration = Mathf.Max(0.1f, miningPassDuration);
                    itemData.magazineSize = 999;
                    itemData.rangedDamage = 0f;
                    itemData.fireRate = 12f;
                }
            }

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
        bool configureMeleeHitbox = addMeleeHitbox && weaponItemType != ItemType.RangedWeapon;

        if (createWorldPrefab)
        {
            worldPrefab = WeaponPrefabBuilder.CreateWorldPickupPrefab(
                meshSource,
                safeName,
                worldPath,
                itemData,
                pickupOptions,
                configureHitbox: addHitboxToWorldPrefab && configureMeleeHitbox);
        }

        if (createHeldPrefab)
        {
            heldPrefab = WeaponPrefabBuilder.CreateHeldPrefab(
                meshSource,
                safeName + "_Held",
                heldPath,
                itemData,
                configureHitbox: configureMeleeHitbox);
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
            BuildSummary(safeName, dataPath, worldPath, heldPath, itemData != null, configureMeleeHitbox && addMeleeHitbox),
            "OK");
    }

    private static bool AssetExists(string dataPath, string worldPath, string heldPath)
    {
        return AssetDatabase.LoadAssetAtPath<Object>(dataPath) != null ||
               AssetDatabase.LoadAssetAtPath<Object>(worldPath) != null ||
               AssetDatabase.LoadAssetAtPath<Object>(heldPath) != null;
    }

    private static string BuildSummary(
        string safeName,
        string dataPath,
        string worldPath,
        string heldPath,
        bool createdItemData,
        bool configuredMeleeHitbox)
    {
        string summary = $"Created weapon '{safeName}'.\n\n";
        if (AssetDatabase.LoadAssetAtPath<Object>(worldPath) != null)
            summary += $"World: {worldPath}\n";
        if (AssetDatabase.LoadAssetAtPath<Object>(heldPath) != null)
            summary += $"Held: {heldPath}\n";
        if (createdItemData)
            summary += $"ItemData: {dataPath}\n";

        summary += "\nTune grip in Play mode, then bake with Tools/Project grip bakers.";
        if (configuredMeleeHitbox)
            summary += "\nHeld hitbox baked on prefab; use Tools > Dark Matter Genesis > Combat > Refresh All Weapon Hitboxes to update existing weapons.";
        return summary;
    }
}
