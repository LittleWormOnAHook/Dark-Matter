using UnityEngine;
using UnityEditor;
using Project.Combat;
using Project.Data;
using Project.EditorTools;
using Project.Interaction;

/// <summary>
/// Authors a physical projectile prefab (CombatProjectile + optional visual/tracer) and a matching
/// ammo ItemData asset together in one action, wired to reference each other. Covers the sci-fi
/// ammo pipeline shared by the player, companions, and enemies: muzzle flash, tracer, impact VFX,
/// hitscan-beam option (lasers), splash/AoE, and elemental damage-over-time status effects.
/// </summary>
public class ProjectileAmmoCreatorWindow : EditorWindow
{
    // Identity
    private string ammoName = "New Ammo";
    private AmmoType ammoType = AmmoType.Gunpowder;
    private Sprite icon;
    private int maxStack = 999;
    private int ammoPerPickup = 20;
    private float ammoPickupGrant = 20f;
    private string tooltipDescription = "";

    // Ranged / projectile behavior
    private bool isHitscanBeam;
    private bool isContinuousLaser;
    private float rangedDamage = 14f;
    private float rangedDamageRandomRange = 4f;
    private float rangedRange = 45f;
    private float projectileSpeed = 120f;
    private float projectileSpreadDegrees = 1.5f;
    private float weaponAccuracy = 75f;
    private float closeRangeFullAccuracyDistance = 12f;
    private float closeRangeSpreadScale = 0.2f;
    private float projectileGravityScale;
    private float splashRadius;
    private float splashDamageFalloff = 0.25f;

    // VFX slots
    private GameObject muzzleFlashPrefab;
    private GameObject tracerPrefab;
    private GameObject impactVfxPrefab;
    private GameObject beamVfxPrefab;
    private GameObject projectileVisualModel;

    // Audio slots
    private AudioClip fireSound;
    private AudioClip projectileTravelSound;
    private AudioClip continuousLoopSound;
    private AudioClip continuousStartSound;
    private AudioClip continuousStopSound;

    // Elemental effect
    private StatusEffectType statusEffectOverride = StatusEffectType.None;
    private float statusEffectDamagePerTick;
    private float statusEffectTickInterval = 1f;
    private float statusEffectDuration;
    private GameObject statusEffectVfxPrefab;

    // Projectile prefab
    private bool createProjectilePrefab = true;
    private GameObject existingProjectilePrefab;

    // World pickup prefab
    private bool createPickupPrefab = true;
    private GameObject pickupVisualModel;
    private int pickupLayer = 7;
    private bool pickupAutoFitCollider = true;
    private bool pickupCanRespawn = true;
    private string pickupPromptText = "Press E to pick up";

    private Vector2 scroll;

    [MenuItem(SurvivalPioneerEditorMenus.ProjectileAmmoCreator, false, 20)]
    public static void ShowWindow()
    {
        GetWindow<ProjectileAmmoCreatorWindow>("Projectile + Ammo Creator").minSize = new Vector2(460, 640);
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        GUILayout.Label("Create Projectile Prefab + Ammo ItemData", EditorStyles.boldLabel);
        GUILayout.Space(6);

        ammoName = EditorGUILayout.TextField("Ammo Name", ammoName);
        ammoType = (AmmoType)EditorGUILayout.EnumPopup("Ammo Type", ammoType);
        icon = (Sprite)EditorGUILayout.ObjectField("Icon", icon, typeof(Sprite), false);
        maxStack = EditorGUILayout.IntField("Max Stack", maxStack);
        ammoPerPickup = EditorGUILayout.IntField("Ammo Per Pickup", ammoPerPickup);
        ammoPickupGrant = EditorGUILayout.FloatField("Ammo Pickup Grant", ammoPickupGrant);
        GUILayout.Label("Tooltip");
        tooltipDescription = EditorGUILayout.TextArea(tooltipDescription, GUILayout.Height(40));

        GUILayout.Space(10);
        GUILayout.Label("Ranged Behavior", EditorStyles.boldLabel);
        isHitscanBeam = EditorGUILayout.Toggle(
            new GUIContent(
                "Hitscan Laser Beam",
                "No traveling projectile. Instant muzzle particles + tracer/beam. For pulse or continuous laser weapons/tools."),
            isHitscanBeam);
        if (isHitscanBeam)
        {
            EditorGUILayout.HelpBox(
                "Laser ammo: use Ammo Type = Laser. Create separate Pulse (fireSound) and Continuous (continuousLoopSound) assets for different devices.",
                MessageType.Info);
            isContinuousLaser = EditorGUILayout.Toggle(
                new GUIContent("Continuous Laser", "Hold-fire tools use loop audio; pulse weapons use Fire Sound."),
                isContinuousLaser);
        }
        rangedDamage = EditorGUILayout.FloatField("Damage", rangedDamage);
        rangedDamageRandomRange = EditorGUILayout.FloatField("Damage Random Range", rangedDamageRandomRange);
        rangedRange = EditorGUILayout.FloatField("Range", rangedRange);
        using (new EditorGUI.DisabledScope(isHitscanBeam))
        {
            projectileSpeed = EditorGUILayout.FloatField(
                new GUIContent("Projectile Speed", "Travel velocity (m/s). Primary speed knob for traveling ammo."),
                projectileSpeed);
            projectileGravityScale = EditorGUILayout.FloatField(
                new GUIContent("Gravity Scale", "0 = perfectly straight flight. Sci-fi energy ammo usually stays at 0."),
                projectileGravityScale);
        }

        GUILayout.Space(6);
        GUILayout.Label("Accuracy / Spread", EditorStyles.boldLabel);
        projectileSpreadDegrees = EditorGUILayout.FloatField(
            new GUIContent("Spread Degrees", "Base cone before accuracy / close-range modifiers."),
            projectileSpreadDegrees);
        weaponAccuracy = EditorGUILayout.Slider(
            new GUIContent("Weapon Accuracy", "0-100. Higher reduces effective cone spread. Skill bonuses add on top."),
            weaponAccuracy, 0f, 100f);
        closeRangeFullAccuracyDistance = EditorGUILayout.FloatField(
            new GUIContent("Close Range Dist (m)", "Within this distance to aim point, spread scales toward Close Spread Scale."),
            closeRangeFullAccuracyDistance);
        closeRangeSpreadScale = EditorGUILayout.Slider(
            new GUIContent("Close Spread Scale", "Point-blank spread multiplier (0 = perfect, 1 = full)."),
            closeRangeSpreadScale, 0f, 1f);

        GUILayout.Space(10);
        GUILayout.Label("Splash / AoE", EditorStyles.boldLabel);
        splashRadius = EditorGUILayout.FloatField(
            new GUIContent("Splash Radius", "0 = single-target only."), splashRadius);
        if (splashRadius > 0f)
            splashDamageFalloff = EditorGUILayout.Slider("Edge Damage Falloff", splashDamageFalloff, 0f, 1f);

        GUILayout.Space(10);
        GUILayout.Label("VFX Slots", EditorStyles.boldLabel);
        muzzleFlashPrefab = (GameObject)EditorGUILayout.ObjectField(
            new GUIContent("Muzzle Flash", "Spawned at the firing socket every shot."), muzzleFlashPrefab, typeof(GameObject), false);
        tracerPrefab = (GameObject)EditorGUILayout.ObjectField(
            new GUIContent(
                isHitscanBeam ? "Tracer Burst" : "Tracer / Trail",
                isHitscanBeam
                    ? "Optional particle/tracer prefab stretched or played along the hitscan beam for pulse lasers."
                    : "TrailRenderer/particle prefab attached to the flying projectile."),
            tracerPrefab,
            typeof(GameObject),
            false);
        impactVfxPrefab = (GameObject)EditorGUILayout.ObjectField(
            new GUIContent("Impact VFX", "Spawned at the hit point on impact."), impactVfxPrefab, typeof(GameObject), false);
        if (isHitscanBeam)
        {
            beamVfxPrefab = (GameObject)EditorGUILayout.ObjectField(
                new GUIContent("Beam VFX", "Needs a LineRenderer; stretched between muzzle and hit point."), beamVfxPrefab, typeof(GameObject), false);
        }

        GUILayout.Space(10);
        GUILayout.Label("Audio", EditorStyles.boldLabel);
        if (!isHitscanBeam || !isContinuousLaser)
        {
            fireSound = (AudioClip)EditorGUILayout.ObjectField(
                new GUIContent("Fire Sound", "Pulse shot / gunshot played once per fire."), fireSound, typeof(AudioClip), false);
        }
        if (!isHitscanBeam)
        {
            projectileTravelSound = (AudioClip)EditorGUILayout.ObjectField(
                new GUIContent("Projectile Travel Sound", "Looping sound that follows the flying projectile and stops the instant it hits or expires."),
                projectileTravelSound, typeof(AudioClip), false);
        }
        if (isHitscanBeam && isContinuousLaser)
        {
            continuousLoopSound = (AudioClip)EditorGUILayout.ObjectField(
                new GUIContent("Continuous Loop", "Loops while hold-fire laser is active."), continuousLoopSound, typeof(AudioClip), false);
            continuousStartSound = (AudioClip)EditorGUILayout.ObjectField(
                new GUIContent("Continuous Start", "Optional chirp when the beam starts."), continuousStartSound, typeof(AudioClip), false);
            continuousStopSound = (AudioClip)EditorGUILayout.ObjectField(
                new GUIContent("Continuous Stop", "Optional chirp when the beam stops."), continuousStopSound, typeof(AudioClip), false);
        }

        GUILayout.Space(10);
        GUILayout.Label("Elemental Effect", EditorStyles.boldLabel);
        statusEffectOverride = (StatusEffectType)EditorGUILayout.EnumPopup(
            new GUIContent("Status Effect", "None uses the ammo type's sensible default (Fire->Burning, Ice->Frozen, Electricity->Shocked, Plasma->Corroded)."),
            statusEffectOverride);
        statusEffectDamagePerTick = EditorGUILayout.FloatField("Damage Per Tick", statusEffectDamagePerTick);
        statusEffectTickInterval = EditorGUILayout.FloatField("Tick Interval (sec)", statusEffectTickInterval);
        statusEffectDuration = EditorGUILayout.FloatField(
            new GUIContent("Duration (sec)", "0 disables the status effect entirely."), statusEffectDuration);
        statusEffectVfxPrefab = (GameObject)EditorGUILayout.ObjectField(
            new GUIContent("Status VFX", "Looping VFX attached to the target while the effect is active."), statusEffectVfxPrefab, typeof(GameObject), false);

        GUILayout.Space(10);
        GUILayout.Label("Projectile Prefab", EditorStyles.boldLabel);
        if (!isHitscanBeam)
        {
            createProjectilePrefab = EditorGUILayout.Toggle("Auto-Create Projectile Prefab", createProjectilePrefab);
            if (createProjectilePrefab)
            {
                projectileVisualModel = (GameObject)EditorGUILayout.ObjectField(
                    new GUIContent("Visual Model (Optional)", "Instantiated as a child of the new projectile prefab. Leave empty for a simple placeholder sphere."),
                    projectileVisualModel, typeof(GameObject), false);
            }
            else
            {
                existingProjectilePrefab = (GameObject)EditorGUILayout.ObjectField(
                    "Existing Projectile Prefab", existingProjectilePrefab, typeof(GameObject), false);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Hitscan beam ammo doesn't use a traveling projectile prefab.", MessageType.Info);
        }

        GUILayout.Space(10);
        GUILayout.Label("World Pickup", EditorStyles.boldLabel);
        createPickupPrefab = EditorGUILayout.Toggle(
            new GUIContent("Auto-Create Pickup Prefab", "Builds a world ItemPickup prefab for this ammo, wired to the new ItemData."),
            createPickupPrefab);
        if (createPickupPrefab)
        {
            pickupVisualModel = (GameObject)EditorGUILayout.ObjectField(
                new GUIContent("Visual Model (Optional)", "Instantiated as the pickup's visual. Falls back to the projectile visual model, then a placeholder cube."),
                pickupVisualModel, typeof(GameObject), false);
            pickupLayer = EditorGUILayout.LayerField("Pickup Layer", pickupLayer);
            pickupAutoFitCollider = EditorGUILayout.Toggle("Auto-Fit Collider", pickupAutoFitCollider);
            pickupCanRespawn = EditorGUILayout.Toggle("Can Respawn", pickupCanRespawn);
            pickupPromptText = EditorGUILayout.TextField("Prompt Text", pickupPromptText);
        }

        GUILayout.Space(20);
        if (GUILayout.Button("Create Ammo + Projectile", GUILayout.Height(44)))
            CreateAmmoAndProjectile();

        EditorGUILayout.EndScrollView();
    }

    private void CreateAmmoAndProjectile()
    {
        if (string.IsNullOrWhiteSpace(ammoName))
        {
            EditorUtility.DisplayDialog("Error", "Ammo Name is required.", "OK");
            return;
        }

        GameObject projectilePrefab = null;

        if (!isHitscanBeam)
        {
            projectilePrefab = createProjectilePrefab
                ? BuildProjectilePrefab()
                : existingProjectilePrefab;

            if (projectilePrefab == null)
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    "Either enable 'Auto-Create Projectile Prefab' or assign an existing one.",
                    "OK");
                return;
            }
        }

        ItemData ammoItem = ScriptableObject.CreateInstance<ItemData>();
        ammoItem.itemName = ammoName;
        ammoItem.itemType = ItemType.Ammo;
        ammoItem.icon = icon;
        ammoItem.maxStack = maxStack;
        ammoItem.ammoType = ammoType;
        ammoItem.ammoPerPickup = ammoPerPickup;
        ammoItem.ammoPickupGrant = ammoPickupGrant;
        ammoItem.tooltipDescription = tooltipDescription;

        ammoItem.rangedDamage = rangedDamage;
        ammoItem.rangedDamageRandomRange = rangedDamageRandomRange;
        ammoItem.rangedRange = rangedRange;
        ammoItem.projectileSpeed = projectileSpeed;
        ammoItem.projectileSpreadDegrees = projectileSpreadDegrees;
        ammoItem.weaponAccuracy = weaponAccuracy;
        ammoItem.closeRangeFullAccuracyDistance = closeRangeFullAccuracyDistance;
        ammoItem.closeRangeSpreadScale = closeRangeSpreadScale;
        ammoItem.projectileGravityScale = projectileGravityScale;
        ammoItem.isHitscanBeam = isHitscanBeam;
        ammoItem.isContinuousLaser = isHitscanBeam && isContinuousLaser;
        ammoItem.splashRadius = splashRadius;
        ammoItem.splashDamageFalloff = splashDamageFalloff;

        ammoItem.muzzleFlashPrefab = muzzleFlashPrefab;
        ammoItem.tracerPrefab = tracerPrefab;
        ammoItem.impactVfxPrefab = impactVfxPrefab;
        ammoItem.beamVfxPrefab = beamVfxPrefab;
        ammoItem.projectilePrefab = projectilePrefab;
        ammoItem.fireSound = fireSound;
        ammoItem.projectileTravelSound = projectileTravelSound;
        ammoItem.continuousLoopSound = continuousLoopSound;
        ammoItem.continuousStartSound = continuousStartSound;
        ammoItem.continuousStopSound = continuousStopSound;

        ammoItem.statusEffectOverride = statusEffectOverride;
        ammoItem.statusEffectDamagePerTick = statusEffectDamagePerTick;
        ammoItem.statusEffectTickInterval = Mathf.Max(0.1f, statusEffectTickInterval);
        ammoItem.statusEffectDuration = statusEffectDuration;
        ammoItem.statusEffectVfxPrefab = statusEffectVfxPrefab;

        string folder = ProjectAssetPaths.ItemsAmmo;
        EnsureFolder(folder);
        string safeName = MakeSafeFileName(ammoName);
        string dataPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{safeName}.asset");
        AssetDatabase.CreateAsset(ammoItem, dataPath);
        ItemDataPruneUtility.Prune(ammoItem, ItemDataInspectorCategory.Ammo);
        EditorUtility.SetDirty(ammoItem);

        GameObject pickupPrefab = null;
        if (createPickupPrefab)
        {
            pickupPrefab = BuildPickupPrefab(ammoItem, safeName);
            ammoItem.worldPrefab = pickupPrefab;
            EditorUtility.SetDirty(ammoItem);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Success",
            $"Ammo '{ammoName}' created.\n\nItemData: {dataPath}\n" +
            (projectilePrefab != null ? $"Projectile Prefab: {AssetDatabase.GetAssetPath(projectilePrefab)}\n" : "") +
            (pickupPrefab != null ? $"Pickup Prefab: {AssetDatabase.GetAssetPath(pickupPrefab)}\n\n" : "\n") +
            "Remember: add this ammo type to the 'Compatible Ammo Types' list on any weapon ItemData assets that should accept it.",
            "OK");

        Selection.activeObject = ammoItem;
        EditorGUIUtility.PingObject(ammoItem);
    }

    private GameObject BuildPickupPrefab(ItemData ammoItem, string safeName)
    {
        string folder = ProjectAssetPaths.PrefabsItemsAmmo;
        EnsureFolder(folder);

        GameObject source = pickupVisualModel != null ? pickupVisualModel : projectileVisualModel;
        GameObject instance;
        if (source != null)
        {
            instance = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (instance == null)
                instance = Instantiate(source);
        }
        else
        {
            instance = GameObject.CreatePrimitive(PrimitiveType.Cube);
            instance.transform.localScale = Vector3.one * 0.25f;
            DestroyImmediate(instance.GetComponent<Collider>());
        }

        instance.name = safeName + "_Pickup";
        instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        instance.layer = pickupLayer;

        Collider collider = instance.GetComponentInChildren<Collider>();
        if (collider == null)
            collider = instance.AddComponent<BoxCollider>();
        collider.isTrigger = true;

        if (pickupAutoFitCollider && collider is BoxCollider boxCollider)
            FitBoxCollider(instance, boxCollider);

        ItemPickup pickup = instance.GetComponent<ItemPickup>();
        if (pickup == null)
            pickup = instance.AddComponent<ItemPickup>();
        pickup.itemData = ammoItem;
        pickup.amount = Mathf.Max(1, ammoItem.ammoPerPickup);
        pickup.promptText = pickupPromptText;
        pickup.canRespawn = pickupCanRespawn;

        string prefabPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{safeName}_Pickup.prefab");
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        DestroyImmediate(instance);

        return savedPrefab;
    }

    private static void FitBoxCollider(GameObject root, BoxCollider boxCollider)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        Vector3 localCenter = root.transform.InverseTransformPoint(bounds.center);
        Vector3 lossyScale = root.transform.lossyScale;
        Vector3 localSize = new Vector3(
            lossyScale.x != 0f ? bounds.size.x / lossyScale.x : bounds.size.x,
            lossyScale.y != 0f ? bounds.size.y / lossyScale.y : bounds.size.y,
            lossyScale.z != 0f ? bounds.size.z / lossyScale.z : bounds.size.z);

        boxCollider.center = localCenter;
        boxCollider.size = localSize;
    }

    private GameObject BuildProjectilePrefab()
    {
        string folder = ProjectAssetPaths.PrefabsCombatProjectiles;
        EnsureFolder(folder);

        GameObject root = new GameObject(ammoName + "_Projectile");
        root.AddComponent<CombatProjectile>();

        if (projectileVisualModel != null)
        {
            GameObject visual = Instantiate(projectileVisualModel, root.transform);
            visual.name = "Visual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
        }
        else
        {
            GameObject placeholder = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            placeholder.name = "Visual_Placeholder";
            placeholder.transform.SetParent(root.transform, false);
            placeholder.transform.localScale = Vector3.one * 0.12f;
            Object.DestroyImmediate(placeholder.GetComponent<Collider>());
        }

        string safeName = MakeSafeFileName(ammoName);
        string prefabPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{safeName}_Projectile.prefab");
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        DestroyImmediate(root);

        return savedPrefab;
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

    private static string MakeSafeFileName(string name)
    {
        char[] invalid = System.IO.Path.GetInvalidFileNameChars();
        string safe = name;
        foreach (char c in invalid)
            safe = safe.Replace(c, '_');

        return safe.Trim();
    }
}
