using System.IO;
using Project.Combat;
using Project.Data;
using Project.EditorTools;
using Project.EditorTools.Player;
using Project.Interaction;
using Project.Inventory;
using Project.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Phase C setup: rifle/pistol ItemData, default projectile, player wiring, and catalog reseed.
/// </summary>
public static class RangedWeaponSetupUtility
{
    private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Players/Player.prefab";
    private const string ItemsFolder = "Assets/_Project/Data/Items";
    private const string HeldFolder = "Assets/_Project/Prefabs/Items/Held";
    private const string WorldFolder = "Assets/_Project/Prefabs/Items";
    private const string ProjectileFolder = "Assets/_Project/Prefabs/Combat/Projectiles";
    private const string DefaultBulletPath = "Assets/_Project/Prefabs/Combat/Projectiles/DefaultBullet.prefab";
    private const string IconsFolder = "Assets/_Project/Art/Icons";

    private const string RifleSourcePath =
        "Assets/Futuristic Weapons Pack #1/prefabs/futuristic_weapon_4_prefab.prefab";
    private const string PistolSourcePath =
        "Assets/HDRP SCI-FI Weapons/Prefabs/Standart/Sci-fi_Pistol.prefab";

    private const string RifleItemPath = ItemsFolder + "/survival_rifle.asset";
    private const string PistolItemPath = ItemsFolder + "/sci_fi_pistol.asset";
    private const string GunpowderAmmoPath = ItemsFolder + "/ammo_gunpowder_rounds.asset";
    private const string GunpowderAmmoWorldPath = WorldFolder + "/ammo_gunpowder_rounds_World.prefab";

    private const string RifleHeldPath = HeldFolder + "/survival_rifle_Held.prefab";
    private const string PistolHeldPath = HeldFolder + "/sci_fi_pistol_Held.prefab";
    private const string RifleWorldPath = WorldFolder + "/survival_rifle.prefab";
    private const string PistolWorldPath = WorldFolder + "/sci_fi_pistol.prefab";

    private const string AttackActionId = "6c2ab1b8-8984-453a-af3d-a3c78ae1679a";
    private const string BlockActionId = "c8d4e2f1-6a7b-4890-c1d2-e3f4a5b6c7d8";

    private const string SwordGripTemplatePath = "Assets/_Project/Data/Items/weap2_sword.asset";
    private const string TwoHandGripTemplatePath = "Assets/_Project/Data/Items/weap_two_handed.asset";

    [MenuItem(SurvivalPioneerEditorMenus.Combat + "Setup Phase C Ranged Weapons", false, 0)]
    public static void SetupPhaseCRangedWeaponsMenu()
    {
        int changes = SetupPhaseCRangedWeapons(showDialog: true);
        if (changes <= 0)
            Debug.Log("Phase C ranged setup: no changes were required.");
    }

    [MenuItem(SurvivalPioneerEditorMenus.CombatAnimations + "Reseed Ranged Action Catalog", false, 7)]
    public static void ReseedRangedActionCatalogMenu()
    {
        GkcActionCatalogExtractor.ReseedFromVerifiedIds();
    }

    [MenuItem(SurvivalPioneerEditorMenus.Equipment + "Reset Ranged Weapon Baked Grips", false, 25)]
    public static void ResetRangedBakedGripsMenu()
    {
        int changes = ResetRangedBakedGrips(showDialog: true);
        if (changes <= 0)
            EditorUtility.DisplayDialog("Reset Ranged Grips", "Rifle and pistol grips were already at defaults.", "OK");
    }

    public static int ResetRangedBakedGrips(bool showDialog = false)
    {
        int changes = 0;
        changes += ApplyDefaultRangedGrips(
            RifleItemPath,
            new Vector3(0.04f, -0.02f, 0.08f),
            new Vector3(-8f, 90f, 8f),
            new Vector3(0.85f, 0.85f, 0.85f),
            new Vector3(0.04f, 0.18f, -0.24f),
            new Vector3(75f, 90f, 90f));
        changes += ApplyDefaultRangedGrips(
            PistolItemPath,
            new Vector3(0.02f, 0.02f, 0.06f),
            new Vector3(-12f, 90f, 0f),
            new Vector3(1.17f, 1.17f, 1.17f),
            new Vector3(0.08f, 0.12f, -0.18f),
            new Vector3(70f, 90f, 90f));

        if (changes > 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        if (showDialog && changes > 0)
        {
            EditorUtility.DisplayDialog(
                "Reset Ranged Grips",
                "Reset held, holstered, and aim grips on Survival Rifle and Sci-Fi Pistol to Phase C defaults.\n\nRe-enter Play mode to see the changes.",
                "OK");
        }

        return changes;
    }

    private static int ApplyDefaultRangedGrips(
        string itemPath,
        Vector3 heldLocalPosition,
        Vector3 heldLocalEuler,
        Vector3 heldLocalScale,
        Vector3 sheathedLocalPosition,
        Vector3 sheathedLocalEuler)
    {
        ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(itemPath);
        if (item == null)
            return 0;

        Undo.RecordObject(item, "Reset Ranged Baked Grips");
        item.heldLocalPosition = heldLocalPosition;
        item.heldLocalEuler = heldLocalEuler;
        item.heldLocalScale = heldLocalScale;
        item.useHeldLocalRotation = false;
        item.heldLocalRotation = Quaternion.identity;

        item.sheathedLocalPosition = sheathedLocalPosition;
        item.sheathedLocalEuler = sheathedLocalEuler;
        item.sheathedLocalScale = Vector3.one;
        item.useSheathedLocalRotation = false;
        item.sheathedLocalRotation = Quaternion.identity;

        item.useAimHeldGrip = false;
        item.aimHeldLocalPosition = heldLocalPosition;
        item.aimHeldLocalEuler = heldLocalEuler;
        item.useAimHeldLocalRotation = false;
        item.aimHeldLocalRotation = Quaternion.identity;
        item.aimHeldLocalScale = heldLocalScale;

        EditorUtility.SetDirty(item);
        return 1;
    }

    public static int SetupPhaseCRangedWeapons(bool showDialog)
    {
        int changes = 0;
        WeaponPrefabBuilder.EnsureFolder(ItemsFolder);
        WeaponPrefabBuilder.EnsureFolder(HeldFolder);
        WeaponPrefabBuilder.EnsureFolder(WorldFolder);
        WeaponPrefabBuilder.EnsureFolder(ProjectileFolder);
        WeaponPrefabBuilder.EnsureFolder(IconsFolder);

        GameObject defaultBullet = EnsureDefaultBulletPrefab();
        if (defaultBullet != null)
            changes++;

        ItemData gunpowderAmmo = EnsureGunpowderAmmo(defaultBullet);
        if (gunpowderAmmo != null)
            changes++;

        ItemData rifle = EnsureRangedWeapon(
            "Survival Rifle",
            RifleItemPath,
            RifleHeldPath,
            RifleWorldPath,
            RifleSourcePath,
            WeaponGrip.TwoHanded,
            GkcWeaponKind.Rifle,
            defaultBullet,
            rangedDamage: 16f,
            magazineSize: 30,
            fireRate: 5.5f,
            projectileSpreadDegrees: 1.2f,
            heldLocalPosition: new Vector3(0.04f, -0.02f, 0.08f),
            heldLocalEuler: new Vector3(-8f, 90f, 8f),
            heldLocalScale: new Vector3(0.85f, 0.85f, 0.85f),
            sheathedLocalPosition: new Vector3(0.04f, 0.18f, -0.24f),
            sheathedLocalEuler: new Vector3(75f, 90f, 90f));
        if (rifle != null)
            changes++;

        ItemData pistol = EnsureRangedWeapon(
            "Sci-Fi Pistol",
            PistolItemPath,
            PistolHeldPath,
            PistolWorldPath,
            PistolSourcePath,
            WeaponGrip.OneHanded,
            GkcWeaponKind.Pistol,
            defaultBullet,
            rangedDamage: 12f,
            magazineSize: 12,
            fireRate: 3.8f,
            projectileSpreadDegrees: 2.2f,
            heldLocalPosition: new Vector3(0.02f, 0.02f, 0.06f),
            heldLocalEuler: new Vector3(-12f, 90f, 0f),
            heldLocalScale: new Vector3(1.17f, 1.17f, 1.17f),
            sheathedLocalPosition: new Vector3(0.08f, 0.12f, -0.18f),
            sheathedLocalEuler: new Vector3(70f, 90f, 90f));
        if (pistol != null)
            changes++;

        changes += WirePlayerPrefabForRangedCombat();
        changes += RangedCraftingSetup.EnsureRangedCraftingRecipes();
        GkcActionCatalogExtractor.ReseedFromVerifiedIds();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        CraftingSetup.SyncRecipeRegistryFromDataFolder();

        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "Phase C Ranged Weapons",
                "Created/updated:\n" +
                $"- Default bullet: {DefaultBulletPath}\n" +
                $"- Rifle ItemData: {RifleItemPath}\n" +
                $"- Pistol ItemData: {PistolItemPath}\n" +
                $"- Gunpowder ammo: {GunpowderAmmoPath}\n" +
                "- Player prefab wired with ranged components + input\n" +
                "- Workbench recipes for rifle, pistol, and gunpowder ammo\n" +
                "- GKC action catalog reseeded with rifle/pistol fire entries",
                "OK");
        }

        return changes;
    }

    private static GameObject EnsureDefaultBulletPrefab()
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultBulletPath);
        if (existing != null)
            return existing;

        GameObject bullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bullet.name = "DefaultBullet";
        bullet.transform.localScale = Vector3.one * 0.08f;

        UnityEngine.Object.DestroyImmediate(bullet.GetComponent<Collider>());
        CombatProjectile projectile = bullet.AddComponent<CombatProjectile>();

        SerializedObject serialized = new SerializedObject(projectile);
        serialized.FindProperty("speed").floatValue = 85f;
        serialized.FindProperty("radius").floatValue = 0.08f;
        serialized.FindProperty("maxLifetime").floatValue = 3f;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(bullet, DefaultBulletPath);
        UnityEngine.Object.DestroyImmediate(bullet);
        return prefab;
    }

    private static ItemData EnsureGunpowderAmmo(GameObject defaultBullet)
    {
        ItemData ammo = LoadOrCreateItem(GunpowderAmmoPath);
        ammo.itemName = "Gunpowder Rounds";
        ammo.itemType = ItemType.Ammo;
        ammo.ammoType = AmmoType.Gunpowder;
        ammo.ammoPerPickup = 20;
        ammo.ammoPickupGrant = 20f;
        ammo.maxStack = 120;
        ammo.rangedDamage = 8f;
        ammo.rangedDamageRandomRange = 2f;
        ammo.projectileSpeed = 85f;
        ammo.projectilePrefab = defaultBullet;
        ammo.componentCategory = ComponentCategory.None;
        ammo.tooltipDescription = "Standard ballistic rounds for rifles and pistols.";

        GameObject worldPrefab = EnsureGunpowderAmmoWorldPrefab(ammo);
        if (worldPrefab != null)
            ammo.worldPrefab = worldPrefab;

        EditorUtility.SetDirty(ammo);
        WeaponPrefabBuilder.TryRegisterInItemRegistry(ammo);
        return ammo;
    }

    private static GameObject EnsureGunpowderAmmoWorldPrefab(ItemData ammo)
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(GunpowderAmmoWorldPath);
        if (existing != null && existing.GetComponent<CombatProjectile>() == null)
            return existing;

        GameObject pickup = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pickup.name = "ammo_gunpowder_rounds_World";
        pickup.tag = "Ammo";
        pickup.layer = WeaponPrefabBuilder.DefaultPickupOptions.Layer;
        pickup.transform.localScale = Vector3.one * 0.35f;

        UnityEngine.Object.DestroyImmediate(pickup.GetComponent<Collider>());

        BoxCollider trigger = pickup.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = Vector3.one;

        ItemPickup itemPickup = pickup.AddComponent<ItemPickup>();
        itemPickup.itemData = ammo;
        itemPickup.amount = ammo.ammoPerPickup;
        itemPickup.promptText = "Press E to pick up";
        itemPickup.canRespawn = false;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(pickup, GunpowderAmmoWorldPath);
        UnityEngine.Object.DestroyImmediate(pickup);
        return prefab;
    }

    private static ItemData EnsureRangedWeapon(
        string displayName,
        string itemPath,
        string heldPath,
        string worldPath,
        string sourcePrefabPath,
        WeaponGrip grip,
        GkcWeaponKind weaponKind,
        GameObject defaultBullet,
        float rangedDamage,
        int magazineSize,
        float fireRate,
        float projectileSpreadDegrees,
        Vector3 heldLocalPosition,
        Vector3 heldLocalEuler,
        Vector3 heldLocalScale,
        Vector3 sheathedLocalPosition,
        Vector3 sheathedLocalEuler)
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePrefabPath);
        if (source == null)
        {
            Debug.LogWarning($"RangedWeaponSetupUtility: missing source prefab at {sourcePrefabPath}");
            return null;
        }

        ItemData template = AssetDatabase.LoadAssetAtPath<ItemData>(
            grip == WeaponGrip.TwoHanded ? TwoHandGripTemplatePath : SwordGripTemplatePath);

        ItemData item = LoadOrCreateItem(itemPath);
        GameObject heldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(heldPath);
        if (heldPrefab == null)
        {
            heldPrefab = WeaponPrefabBuilder.CreateHeldPrefab(
                source,
                Path.GetFileNameWithoutExtension(heldPath),
                heldPath,
                item,
                configureHitbox: false);
        }

        GameObject worldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(worldPath);
        if (worldPrefab == null)
        {
            worldPrefab = WeaponPrefabBuilder.CreateWorldPickupPrefab(
                source,
                Path.GetFileNameWithoutExtension(worldPath),
                worldPath,
                item,
                WeaponPrefabBuilder.DefaultPickupOptions);
        }

        item.itemName = displayName;
        item.itemType = ItemType.RangedWeapon;
        item.weaponGrip = grip;
        item.gkcWeaponKind = weaponKind;
        item.maxStack = 1;
        item.rangedDamage = rangedDamage;
        item.rangedDamageRandomRange = 4f;
        item.rangedRange = grip == WeaponGrip.TwoHanded ? 55f : 32f;
        item.projectileSpeed = 85f;
        item.projectileSpreadDegrees = projectileSpreadDegrees;
        item.fireRate = fireRate;
        item.magazineSize = magazineSize;
        item.defaultAmmoType = AmmoType.Gunpowder;
        item.compatibleAmmoTypes = new[] { AmmoType.Gunpowder };
        item.projectilePrefab = defaultBullet;
        item.muzzleSocketName = "Muzzle";
        item.aimFovMultiplier = grip == WeaponGrip.TwoHanded ? 0.76f : 0.84f;
        item.hipFireMaxDeviationDegrees = grip == WeaponGrip.TwoHanded ? 14f : 20f;
        item.hipFireSpreadMultiplier = grip == WeaponGrip.TwoHanded ? 1f : 1.15f;
        item.heldPrefab = heldPrefab;
        item.worldPrefab = worldPrefab;
        item.heldLocalPosition = heldLocalPosition;
        item.heldLocalEuler = heldLocalEuler;
        item.heldLocalScale = heldLocalScale;
        item.sheathedLocalPosition = sheathedLocalPosition;
        item.sheathedLocalEuler = sheathedLocalEuler;
        item.equipSocketName = "RightHand";
        item.sheatheSocketName = "Spine";
        item.tooltipDescription = grip == WeaponGrip.TwoHanded
            ? "Two-handed survival rifle. Hold RMB to aim, LMB to fire."
            : "One-handed sci-fi pistol. Hold RMB to aim, LMB to fire.";

        if (template != null)
        {
            if (string.IsNullOrEmpty(item.equipSocketName))
                item.equipSocketName = template.equipSocketName;
            if (string.IsNullOrEmpty(item.sheatheSocketName))
                item.sheatheSocketName = template.sheatheSocketName;
        }

        if (item.icon == null)
        {
            Sprite icon = EquipmentIconGenerator.SaveSpriteAsset(
                source,
                $"{IconsFolder}/{Path.GetFileNameWithoutExtension(itemPath)}_Icon.png",
                new EquipmentIconGenerator.Settings
                {
                    Size = 128,
                    ModelRotation = new Vector3(0f, 90f, 0f),
                    Padding = 1.15f,
                    TransparentBackground = true
                });
            if (icon != null)
                item.icon = icon;
        }

        EditorUtility.SetDirty(item);
        WeaponPrefabBuilder.WirePickupItemData(worldPath, item);
        WeaponPrefabBuilder.TryRegisterInItemRegistry(item);
        return item;
    }

    private static ItemData LoadOrCreateItem(string path)
    {
        ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
        if (item != null)
            return item;

        item = ScriptableObject.CreateInstance<ItemData>();
        AssetDatabase.CreateAsset(item, path);
        return item;
    }

    private static int WirePlayerPrefabForRangedCombat()
    {
        GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (prefabRoot == null)
        {
            Debug.LogWarning($"RangedWeaponSetupUtility: Player prefab not found at {PlayerPrefabPath}");
            return 0;
        }

        GameObject prefabInstance = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        int changes = 0;

        if (prefabInstance.GetComponent<RangedCombatController>() == null)
        {
            prefabInstance.AddComponent<RangedCombatController>();
            changes++;
        }

        if (prefabInstance.GetComponent<WeaponAmmoState>() == null)
        {
            prefabInstance.AddComponent<WeaponAmmoState>();
            changes++;
        }

        if (prefabInstance.GetComponent<RangedCombatHud>() == null)
        {
            prefabInstance.AddComponent<RangedCombatHud>();
            changes++;
        }

        PlayerInput playerInput = prefabInstance.GetComponent<PlayerInput>();
        RangedCombatController ranged = prefabInstance.GetComponent<RangedCombatController>();
        if (playerInput != null && ranged != null)
            changes += WireRangedInputEvents(playerInput, ranged);

        if (changes > 0)
            PrefabUtility.SaveAsPrefabAsset(prefabInstance, PlayerPrefabPath);

        PrefabUtility.UnloadPrefabContents(prefabInstance);
        return changes;
    }

    private static int WireRangedInputEvents(PlayerInput playerInput, RangedCombatController ranged)
    {
        SerializedObject serializedPlayerInput = new SerializedObject(playerInput);
        SerializedProperty actionEvents = serializedPlayerInput.FindProperty("m_ActionEvents");
        if (actionEvents == null || !actionEvents.isArray)
            return 0;

        int changes = 0;
        if (WireActionEvent(
                actionEvents,
                AttackActionId,
                ranged,
                "Project.Interaction.RangedCombatController, Assembly-CSharp",
                "OnAttack"))
            changes++;

        if (WireActionEvent(
                actionEvents,
                BlockActionId,
                ranged,
                "Project.Interaction.RangedCombatController, Assembly-CSharp",
                "OnBlock"))
            changes++;

        if (changes > 0)
            serializedPlayerInput.ApplyModifiedPropertiesWithoutUndo();

        return changes;
    }

    private static bool WireActionEvent(
        SerializedProperty actionEvents,
        string actionId,
        UnityEngine.Object target,
        string targetTypeName,
        string methodName)
    {
        for (int i = 0; i < actionEvents.arraySize; i++)
        {
            SerializedProperty entry = actionEvents.GetArrayElementAtIndex(i);
            if (entry.FindPropertyRelative("m_ActionId").stringValue != actionId)
                continue;

            if (EntryTargetsComponent(entry, target, methodName))
                return false;

            AddInputCall(entry, target, targetTypeName, methodName);
            return true;
        }

        return false;
    }

    private static bool EntryTargetsComponent(SerializedProperty entry, UnityEngine.Object target, string methodName)
    {
        SerializedProperty calls = entry.FindPropertyRelative("m_PersistentCalls.m_Calls");
        if (calls == null || !calls.isArray)
            return false;

        for (int i = 0; i < calls.arraySize; i++)
        {
            SerializedProperty call = calls.GetArrayElementAtIndex(i);
            if (call.FindPropertyRelative("m_Target").objectReferenceValue == target
                && call.FindPropertyRelative("m_MethodName").stringValue == methodName)
                return true;
        }

        return false;
    }

    private static void AddInputCall(SerializedProperty entry, UnityEngine.Object target, string targetTypeName, string methodName)
    {
        SerializedProperty calls = entry.FindPropertyRelative("m_PersistentCalls.m_Calls");
        int callIndex = calls.arraySize;
        calls.InsertArrayElementAtIndex(callIndex);
        SerializedProperty call = calls.GetArrayElementAtIndex(callIndex);
        call.FindPropertyRelative("m_Target").objectReferenceValue = target;
        call.FindPropertyRelative("m_TargetAssemblyTypeName").stringValue = targetTypeName;
        call.FindPropertyRelative("m_MethodName").stringValue = methodName;
        call.FindPropertyRelative("m_Mode").enumValueIndex = 0;
        call.FindPropertyRelative("m_CallState").enumValueIndex = 2;
    }
}
