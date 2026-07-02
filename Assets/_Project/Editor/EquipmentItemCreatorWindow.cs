using System;
using System.IO;
using Project.Data;
using Project.EditorTools;
using Project.Interaction;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Creates ItemData assets plus pickup/held prefabs for melee weapons and tools.
/// </summary>
public class EquipmentItemCreatorWindow : EditorWindow
{
    private enum EquipmentKind
    {
        MeleeWeapon,
        Tool
    }

    private enum IconSourceMode
    {
        GenerateFromMesh,
        UseImageFile
    }

    private const string ItemsDataFolder = "Assets/_Project/Data/Items";
    private const string ItemsPrefabFolder = "Assets/_Project/Prefabs/Items";
    private const string HeldPrefabFolder = "Assets/_Project/Prefabs/Items/Held";
    private const string IconsFolder = "Assets/_Project/Art/Icons";
    private const string SwordTemplatePath = "Assets/_Project/Data/Items/weap2_sword.asset";

    private EquipmentKind equipmentKind = EquipmentKind.MeleeWeapon;
    private string itemName = "New Weapon";
    private GameObject meshSource;
    private Sprite icon;
    private ItemData gripTemplate;

    private IconSourceMode iconSourceMode = IconSourceMode.GenerateFromMesh;
    private UnityEngine.Object iconImageSource;
    private bool copyImageIntoProject = true;
    private int imageIconPixelsPerUnit = 100;
    private bool autoGenerateIcon = true;
    private int iconSize = 128;
    private Vector3 iconRotation = new Vector3(0f, 90f, 0f);
    private float iconPadding = 1.15f;
    private bool transparentIconBackground = true;
    private Texture2D iconPreviewTexture;

    private bool useSeparateHeldPrefab;
    private bool copyGripFromTemplate = true;
    private bool autoFitCollider = true;
    private bool canRespawn;
    private int pickupLayer = 7;

    private float meleeDamage = 15f;
    private float meleeDamageRandomRange = 4f;
    private float criticalDamageMultiplier = 2f;
    private float meleeRange = 2.4f;
    private float meleeCooldown = 0.6f;
    private int gatherPower = 1;
    private Vector3 swingEulerAngles = new Vector3(-90f, 0f, 0f);

    private ToolType toolType = ToolType.Scanner;
    private float toolRange = 8f;
    private float scanRange = 24f;

    [MenuItem(SurvivalPioneerEditorMenus.Content + "Equipment Item Creator")]
    public static void ShowWindow()
    {
        var window = GetWindow<EquipmentItemCreatorWindow>("Equipment Creator");
        window.minSize = new Vector2(460, 720);
    }

    [MenuItem(SurvivalPioneerEditorMenus.Content + "Equipment Item Creator From Selection")]
    private static void OpenFromSelection()
    {
        var window = GetWindow<EquipmentItemCreatorWindow>("Equipment Creator");
        window.minSize = new Vector2(460, 720);
        window.UseSelectionAsMeshSource();
    }

    private void OnDisable()
    {
        ClearIconPreview();
    }

    private void OnEnable()
    {
        if (gripTemplate == null)
            gripTemplate = AssetDatabase.LoadAssetAtPath<ItemData>(SwordTemplatePath);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Create Weapon or Tool", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Pick a mesh/model prefab, configure stats, then create an ItemData asset " +
            "and a world pickup prefab wired for hotbar equip and pickup.",
            MessageType.Info);

        EditorGUILayout.Space(8f);
        equipmentKind = (EquipmentKind)EditorGUILayout.EnumPopup("Type", equipmentKind);

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
        itemName = EditorGUILayout.TextField("Item Name", itemName);
        meshSource = (GameObject)EditorGUILayout.ObjectField(
            "Mesh / Model Source",
            meshSource,
            typeof(GameObject),
            false);

        EditorGUILayout.Space(6f);
        DrawIconSection();

        if (GUILayout.Button("Use Selected Object As Mesh Source"))
            UseSelectionAsMeshSource();

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Prefab Options", EditorStyles.boldLabel);
        useSeparateHeldPrefab = EditorGUILayout.Toggle("Separate Held Prefab", useSeparateHeldPrefab);
        autoFitCollider = EditorGUILayout.Toggle("Auto-fit Pickup Collider", autoFitCollider);
        canRespawn = EditorGUILayout.Toggle("Can Respawn", canRespawn);
        pickupLayer = EditorGUILayout.LayerField("Pickup Layer", pickupLayer);

        if (equipmentKind == EquipmentKind.MeleeWeapon)
            DrawWeaponFields();
        else
            DrawToolFields();

        EditorGUILayout.Space(16f);
        using (new EditorGUI.DisabledScope(!CanCreate()))
        {
            if (GUILayout.Button("Create Equipment Item", GUILayout.Height(44f)))
                CreateEquipmentItem();
        }
    }

    private void DrawIconSection()
    {
        EditorGUILayout.LabelField("Icon", EditorStyles.boldLabel);
        iconSourceMode = (IconSourceMode)EditorGUILayout.EnumPopup("Icon Source", iconSourceMode);

        if (iconSourceMode == IconSourceMode.GenerateFromMesh)
            DrawGeneratedIconFields();
        else
            DrawImageIconFields();

        icon = (Sprite)EditorGUILayout.ObjectField("Assigned Icon", icon, typeof(Sprite), false);
        DrawIconPreview();
    }

    private void DrawGeneratedIconFields()
    {
        autoGenerateIcon = EditorGUILayout.Toggle("Auto-generate On Create", autoGenerateIcon);
        iconSize = EditorGUILayout.IntSlider("Icon Size", iconSize, 64, 256);
        iconRotation = EditorGUILayout.Vector3Field("Icon Rotation", iconRotation);
        iconPadding = EditorGUILayout.Slider("Icon Padding", iconPadding, 1f, 1.8f);
        transparentIconBackground = EditorGUILayout.Toggle("Transparent Background", transparentIconBackground);

        using (new EditorGUI.DisabledScope(meshSource == null))
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Preview Icon"))
                RefreshIconPreview();
            if (GUILayout.Button("Save Icon Asset"))
                SaveGeneratedIconAsset();
            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawImageIconFields()
    {
        EditorGUILayout.HelpBox(
            "Assign a PNG or JPG from the project, or import one from disk. " +
            "Images are configured as Sprites for inventory use.",
            MessageType.None);

        EditorGUI.BeginChangeCheck();
        iconImageSource = EditorGUILayout.ObjectField(
            "Image (PNG / JPG)",
            iconImageSource,
            typeof(UnityEngine.Object),
            false);
        if (EditorGUI.EndChangeCheck())
            ApplyImageSourceSelection();

        copyImageIntoProject = EditorGUILayout.Toggle("Copy External Files Into Project", copyImageIntoProject);
        imageIconPixelsPerUnit = EditorGUILayout.IntField("Sprite Pixels Per Unit", imageIconPixelsPerUnit);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Browse PNG/JPG..."))
            BrowseForImageFile();
        if (GUILayout.Button("Apply Image As Icon"))
            ApplyImageAsIcon();
        EditorGUILayout.EndHorizontal();
    }

    private void ApplyImageSourceSelection()
    {
        if (iconImageSource == null)
            return;

        if (iconImageSource is Sprite sprite)
        {
            icon = sprite;
            RefreshIconPreview();
            return;
        }

        if (iconImageSource is Texture2D texture)
        {
            icon = EquipmentIconGenerator.EnsureSpriteFromTexture(texture, imageIconPixelsPerUnit);
            RefreshIconPreview();
            return;
        }

        EditorUtility.DisplayDialog(
            "Equipment Creator",
            "Assign a PNG/JPG Texture or an existing Sprite asset.",
            "OK");
        iconImageSource = null;
    }

    private void BrowseForImageFile()
    {
        string path = EditorUtility.OpenFilePanel(
            "Select Icon Image",
            Application.dataPath,
            "png,jpg,jpeg");

        if (string.IsNullOrWhiteSpace(path))
            return;

        if (copyImageIntoProject)
        {
            ApplyImportedImageFile(path);
            return;
        }

        if (!path.Replace('\\', '/').StartsWith(Application.dataPath.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase))
        {
            EditorUtility.DisplayDialog(
                "Equipment Creator",
                "The selected file is outside the project. Enable 'Copy External Files Into Project' or place the image under Assets/.",
                "OK");
            return;
        }

        string assetPath = "Assets" + path.Substring(Application.dataPath.Length).Replace('\\', '/');
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (texture == null)
        {
            EditorUtility.DisplayDialog("Equipment Creator", "Could not load the selected image from the project.", "OK");
            return;
        }

        iconImageSource = texture;
        ApplyImageSourceSelection();
    }

    private void ApplyImageAsIcon()
    {
        if (iconImageSource == null)
        {
            BrowseForImageFile();
            return;
        }

        ApplyImageSourceSelection();
        if (icon == null)
        {
            EditorUtility.DisplayDialog("Equipment Creator", "Could not use the selected image as an icon.", "OK");
            return;
        }

        EditorGUIUtility.PingObject(icon);
    }

    private void ApplyImportedImageFile(string sourceFilePath)
    {
        string safeName = SanitizeAssetName(itemName);
        if (string.IsNullOrEmpty(safeName))
        {
            EditorUtility.DisplayDialog("Equipment Creator", "Enter a valid item name before importing an icon.", "OK");
            return;
        }

        string extension = Path.GetExtension(sourceFilePath);
        if (string.IsNullOrEmpty(extension))
            extension = ".png";

        EnsureFolder(IconsFolder);
        string targetAssetPath = $"{IconsFolder}/{safeName}_Icon{extension.ToLowerInvariant()}";
        icon = EquipmentIconGenerator.ImportImageFileAsSprite(
            sourceFilePath,
            targetAssetPath,
            imageIconPixelsPerUnit);

        if (icon == null)
        {
            EditorUtility.DisplayDialog("Equipment Creator", "Could not import the selected image as an icon.", "OK");
            return;
        }

        iconImageSource = icon;
        RefreshIconPreview();
        EditorGUIUtility.PingObject(icon);
    }

    private void DrawWeaponFields()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Melee Stats", EditorStyles.boldLabel);
        meleeDamage = EditorGUILayout.FloatField("Damage", meleeDamage);
        meleeDamageRandomRange = EditorGUILayout.FloatField("Damage Random Range", meleeDamageRandomRange);
        criticalDamageMultiplier = EditorGUILayout.FloatField("Critical Damage Multiplier", criticalDamageMultiplier);
        meleeRange = EditorGUILayout.FloatField("Range", meleeRange);
        meleeCooldown = EditorGUILayout.FloatField("Cooldown", meleeCooldown);
        gatherPower = EditorGUILayout.IntField("Gather Power", gatherPower);
        swingEulerAngles = EditorGUILayout.Vector3Field("Swing Euler", swingEulerAngles);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Grip Template", EditorStyles.boldLabel);
        copyGripFromTemplate = EditorGUILayout.Toggle("Copy Hand/Back Grip", copyGripFromTemplate);
        using (new EditorGUI.DisabledScope(!copyGripFromTemplate))
        {
            gripTemplate = (ItemData)EditorGUILayout.ObjectField(
                "Template ItemData",
                gripTemplate,
                typeof(ItemData),
                false);
        }
    }

    private void DrawToolFields()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Tool Stats", EditorStyles.boldLabel);
        toolType = (ToolType)EditorGUILayout.EnumPopup("Tool Type", toolType);
        toolRange = EditorGUILayout.FloatField("Tool Range", toolRange);
        scanRange = EditorGUILayout.FloatField("Scan Range", scanRange);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Held Grip", EditorStyles.boldLabel);
        copyGripFromTemplate = EditorGUILayout.Toggle("Copy Hand Grip From Template", copyGripFromTemplate);
        using (new EditorGUI.DisabledScope(!copyGripFromTemplate))
        {
            gripTemplate = (ItemData)EditorGUILayout.ObjectField(
                "Template ItemData",
                gripTemplate,
                typeof(ItemData),
                false);
        }
    }

    private void UseSelectionAsMeshSource()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("Equipment Creator", "Select a mesh or prefab instance in the Hierarchy first.", "OK");
            return;
        }

        meshSource = selected;

        if (string.IsNullOrWhiteSpace(itemName) || itemName == "New Weapon" || itemName == "New Tool")
            itemName = selected.name.Replace("(Clone)", string.Empty).Trim();

        if (iconSourceMode == IconSourceMode.GenerateFromMesh)
            RefreshIconPreview();

        Repaint();
    }

    private EquipmentIconGenerator.Settings BuildIconSettings()
    {
        return new EquipmentIconGenerator.Settings
        {
            Size = iconSize,
            ModelRotation = iconRotation,
            Padding = iconPadding,
            TransparentBackground = transparentIconBackground
        };
    }

    private void RefreshIconPreview()
    {
        ClearIconPreview();

        if (icon != null && icon.texture != null)
        {
            iconPreviewTexture = icon.texture;
            Repaint();
            return;
        }

        if (iconSourceMode != IconSourceMode.GenerateFromMesh || meshSource == null)
            return;

        iconPreviewTexture = EquipmentIconGenerator.RenderPreview(meshSource, BuildIconSettings());
        Repaint();
    }

    private void SaveGeneratedIconAsset()
    {
        string safeName = SanitizeAssetName(itemName);
        if (string.IsNullOrEmpty(safeName))
        {
            EditorUtility.DisplayDialog("Equipment Creator", "Enter a valid item name before saving an icon.", "OK");
            return;
        }

        if (meshSource == null)
            return;

        EnsureFolder(IconsFolder);
        string iconPath = $"{IconsFolder}/{safeName}_Icon.png";
        Sprite generated = EquipmentIconGenerator.SaveSpriteAsset(meshSource, iconPath, BuildIconSettings());
        if (generated == null)
        {
            EditorUtility.DisplayDialog("Equipment Creator", "Could not generate an icon from the current mesh source.", "OK");
            return;
        }

        icon = generated;
        RefreshIconPreview();
        EditorGUIUtility.PingObject(generated);
    }

    private void DrawIconPreview()
    {
        if (iconPreviewTexture == null)
            return;

        EditorGUILayout.Space(4f);
        float previewSize = Mathf.Min(128f, position.width - 24f);
        Rect rect = GUILayoutUtility.GetRect(previewSize, previewSize, GUILayout.ExpandWidth(false));
        EditorGUI.DrawPreviewTexture(rect, iconPreviewTexture, null, ScaleMode.ScaleToFit);
    }

    private void ClearIconPreview()
    {
        if (iconPreviewTexture == null)
            return;

        if (icon == null || icon.texture != iconPreviewTexture)
            DestroyImmediate(iconPreviewTexture);

        iconPreviewTexture = null;
    }

    private Sprite ResolveIconForCreate(string safeName)
    {
        if (icon != null)
            return icon;

        if (iconSourceMode == IconSourceMode.UseImageFile)
        {
            if (iconImageSource != null)
                ApplyImageSourceSelection();

            return icon;
        }

        if (!autoGenerateIcon || meshSource == null)
            return null;

        EnsureFolder(IconsFolder);
        string iconPath = $"{IconsFolder}/{safeName}_Icon.png";
        return EquipmentIconGenerator.SaveSpriteAsset(meshSource, iconPath, BuildIconSettings());
    }

    private string GetIconSummaryPath(string safeName)
    {
        if (icon == null)
            return string.Empty;

        string assetPath = AssetDatabase.GetAssetPath(icon);
        return string.IsNullOrEmpty(assetPath) ? $"{IconsFolder}/{safeName}_Icon" : assetPath;
    }

    private bool CanCreate()
    {
        return meshSource != null && !string.IsNullOrWhiteSpace(itemName);
    }

    private void CreateEquipmentItem()
    {
        string safeName = SanitizeAssetName(itemName);
        if (string.IsNullOrEmpty(safeName))
        {
            EditorUtility.DisplayDialog("Equipment Creator", "Item name is invalid after sanitization.", "OK");
            return;
        }

        string dataPath = $"{ItemsDataFolder}/{safeName}.asset";
        string worldPrefabPath = $"{ItemsPrefabFolder}/{safeName}.prefab";
        string heldPrefabPath = $"{HeldPrefabFolder}/{safeName}_Held.prefab";

        if (AssetDatabase.LoadAssetAtPath<ItemData>(dataPath) != null ||
            AssetDatabase.LoadAssetAtPath<GameObject>(worldPrefabPath) != null)
        {
            if (!EditorUtility.DisplayDialog(
                    "Equipment Creator",
                    $"An asset named '{safeName}' already exists. Overwrite?",
                    "Overwrite",
                    "Cancel"))
                return;
        }

        EnsureFolder(ItemsDataFolder);
        EnsureFolder(ItemsPrefabFolder);
        if (useSeparateHeldPrefab)
            EnsureFolder(HeldPrefabFolder);

        icon = ResolveIconForCreate(safeName);

        ItemData itemData = ScriptableObject.CreateInstance<ItemData>();
        itemData.itemName = safeName;
        itemData.icon = icon;
        itemData.maxStack = 1;
        itemData.itemType = equipmentKind == EquipmentKind.MeleeWeapon
            ? ItemType.MeleeWeapon
            : ItemType.Tool;

        ApplyEquipmentDefaults(itemData);

        AssetDatabase.CreateAsset(itemData, dataPath);

        GameObject worldInstance = BuildPickupInstance(meshSource, safeName, itemData, stripPickupComponents: false);
        GameObject worldPrefab = SavePrefab(worldInstance, worldPrefabPath);
        DestroyImmediate(worldInstance);

        GameObject heldPrefab = worldPrefab;
        if (useSeparateHeldPrefab)
        {
            GameObject heldInstance = BuildPickupInstance(meshSource, safeName + "_Held", itemData, stripPickupComponents: true);
            heldPrefab = SavePrefab(heldInstance, heldPrefabPath);
            DestroyImmediate(heldInstance);
        }

        itemData.worldPrefab = worldPrefab;
        itemData.heldPrefab = heldPrefab;

        WirePickupReference(worldPrefabPath, itemData);
        EditorUtility.SetDirty(itemData);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = itemData;
        EditorGUIUtility.PingObject(itemData);

        EditorUtility.DisplayDialog(
            "Equipment Creator",
            $"Created {equipmentKind} '{safeName}'.\n\n" +
            $"ItemData: {dataPath}\n" +
            $"World prefab: {worldPrefabPath}\n" +
            (useSeparateHeldPrefab ? $"Held prefab: {heldPrefabPath}\n" : string.Empty) +
            (icon != null ? $"Icon: {GetIconSummaryPath(safeName)}\n" : string.Empty) +
            "\nNext: tune hand/back grip in Play mode, then bake with Tools/Project grip bakers.",
            "OK");
    }

    private void ApplyEquipmentDefaults(ItemData itemData)
    {
        if (equipmentKind == EquipmentKind.MeleeWeapon)
        {
            itemData.meleeDamage = meleeDamage;
            itemData.meleeDamageRandomRange = meleeDamageRandomRange;
            itemData.criticalDamageMultiplier = criticalDamageMultiplier;
            itemData.meleeRange = meleeRange;
            itemData.meleeCooldown = meleeCooldown;
            itemData.gatherPower = gatherPower;
            itemData.swingEulerAngles = swingEulerAngles;
        }
        else
        {
            itemData.toolType = toolType;
            itemData.toolRange = toolRange;
            itemData.scanRange = scanRange;
        }

        if (!copyGripFromTemplate || gripTemplate == null)
            return;

        itemData.equipSocketName = gripTemplate.equipSocketName;
        itemData.heldLocalPosition = gripTemplate.heldLocalPosition;
        itemData.heldLocalEuler = gripTemplate.heldLocalEuler;
        itemData.useHeldLocalRotation = gripTemplate.useHeldLocalRotation;
        itemData.heldLocalRotation = gripTemplate.heldLocalRotation;
        itemData.heldLocalScale = gripTemplate.heldLocalScale;

        if (equipmentKind == EquipmentKind.MeleeWeapon)
        {
            itemData.sheatheSocketName = gripTemplate.sheatheSocketName;
            itemData.sheathedLocalPosition = gripTemplate.sheathedLocalPosition;
            itemData.sheathedLocalEuler = gripTemplate.sheathedLocalEuler;
            itemData.useSheathedLocalRotation = gripTemplate.useSheathedLocalRotation;
            itemData.sheathedLocalRotation = gripTemplate.sheathedLocalRotation;
            itemData.sheathedLocalScale = gripTemplate.sheathedLocalScale;
        }
    }

    private GameObject BuildPickupInstance(
        GameObject source,
        string instanceName,
        ItemData itemData,
        bool stripPickupComponents)
    {
        GameObject instance = InstantiateSource(source);
        instance.name = instanceName;
        instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        RemoveExistingPickupComponents(instance);

        if (stripPickupComponents)
        {
            RemoveAllColliders(instance);
            if (itemData != null && itemData.itemType == ItemType.MeleeWeapon)
                WeaponPrefabBuilder.ConfigureWeaponHitbox(instance, itemData);
            return instance;
        }

        ConfigurePickupComponents(instance, itemData);
        return instance;
    }

    private static GameObject InstantiateSource(GameObject source)
    {
        GameObject instance = PrefabUtility.InstantiatePrefab(source) as GameObject;
        if (instance == null)
            instance = Instantiate(source);

        return instance;
    }

    private void ConfigurePickupComponents(GameObject instance, ItemData itemData)
    {
        instance.layer = pickupLayer;

        Collider collider = instance.GetComponentInChildren<Collider>();
        if (collider == null)
            collider = instance.AddComponent<BoxCollider>();

        collider.isTrigger = true;

        if (autoFitCollider && collider is BoxCollider boxCollider)
            FitBoxCollider(instance, boxCollider);

        ItemPickup pickup = instance.GetComponent<ItemPickup>();
        if (pickup == null)
            pickup = instance.AddComponent<ItemPickup>();

        pickup.itemData = itemData;
        pickup.amount = 1;
        pickup.promptText = "Press E to pick up";
        pickup.canRespawn = canRespawn;
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
            DestroyImmediate(pickup);

        foreach (ResourceNode node in root.GetComponentsInChildren<ResourceNode>(true))
            DestroyImmediate(node);

        MapMarkerEditorUtility.RemoveMapMarkers(root);
    }

    private static void RemoveAllColliders(GameObject root)
    {
        foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            DestroyImmediate(collider);

        foreach (Rigidbody body in root.GetComponentsInChildren<Rigidbody>(true))
            DestroyImmediate(body);
    }

    private static GameObject SavePrefab(GameObject instance, string path)
    {
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, path, out bool success);
        if (!success || prefab == null)
            throw new InvalidOperationException($"Failed to save prefab at {path}");

        return prefab;
    }

    private static void WirePickupReference(string prefabPath, ItemData itemData)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        ItemPickup pickup = prefabRoot.GetComponent<ItemPickup>();
        if (pickup != null)
        {
            pickup.itemData = itemData;
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        }

        PrefabUtility.UnloadPrefabContents(prefabRoot);
    }

    private static string SanitizeAssetName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
            return string.Empty;

        char[] invalid = Path.GetInvalidFileNameChars();
        string sanitized = rawName.Trim();
        foreach (char c in invalid)
            sanitized = sanitized.Replace(c, '_');

        return sanitized.Replace('/', '_').Replace('\\', '_');
    }

    private static void EnsureFolder(string folderPath)
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
}
