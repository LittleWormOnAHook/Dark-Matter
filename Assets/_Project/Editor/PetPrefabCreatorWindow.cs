using Project.EditorTools;
using UnityEditor;
using UnityEngine;

public class PetPrefabCreatorWindow : EditorWindow
{
    private string petId = "fox_cub";
    private string displayName = "Fox Cub";
    private string description = "A loyal companion that gathers nearby items.";
    private string prefabName = "FoxCub";
    private GameObject sourcePrefab;
    private RuntimeAnimatorController animatorController;
    private Sprite inventoryIcon;
    private bool autoGenerateIcon = true;
    private Color furColor = new Color(0.92f, 0.45f, 0.12f, 1f);
    private Color bellyColor = new Color(0.98f, 0.82f, 0.62f, 1f);
    private Color accentColor = new Color(0.12f, 0.1f, 0.1f, 1f);

    [MenuItem(SurvivalPioneerEditorMenus.Content + "Pet Prefab Creator", false, 25)]
    public static void ShowWindow()
    {
        PetPrefabCreatorWindow window = GetWindow<PetPrefabCreatorWindow>("Pet Prefabs");
        window.minSize = new Vector2(420, 560);
    }

    [MenuItem(SurvivalPioneerEditorMenus.Content + "Pet Prefab Creator From Selection", false, 26)]
    private static void OpenFromSelection()
    {
        PetPrefabCreatorWindow window = GetWindow<PetPrefabCreatorWindow>("Pet Prefabs");
        window.minSize = new Vector2(420, 560);
        window.UseSelectionAsSource();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Pet Prefab Creator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Create pet world prefabs, Resources copies, PetDefinition assets, and optional icons. " +
            "Use presets for quick starts or configure custom pets for the expanding pet system.",
            MessageType.Info);

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
        petId = EditorGUILayout.TextField("Pet Id", petId);
        displayName = EditorGUILayout.TextField("Display Name", displayName);
        prefabName = EditorGUILayout.TextField("Prefab Name", prefabName);
        description = EditorGUILayout.TextField("Description", description, GUILayout.MinHeight(40f));

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Source", EditorStyles.boldLabel);
        sourcePrefab = (GameObject)EditorGUILayout.ObjectField("Source Prefab", sourcePrefab, typeof(GameObject), false);
        animatorController = (RuntimeAnimatorController)EditorGUILayout.ObjectField(
            "Animator Controller",
            animatorController,
            typeof(RuntimeAnimatorController),
            false);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Use Selection"))
            UseSelectionAsSource();
        if (GUILayout.Button("Load Fox Cub Preset"))
            ApplyFoxCubPreset();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Icon", EditorStyles.boldLabel);
        autoGenerateIcon = EditorGUILayout.Toggle("Auto-generate Icon", autoGenerateIcon);
        using (new EditorGUI.DisabledScope(autoGenerateIcon))
            inventoryIcon = (Sprite)EditorGUILayout.ObjectField("Inventory Icon", inventoryIcon, typeof(Sprite), false);

        using (new EditorGUI.DisabledScope(!autoGenerateIcon))
        {
            furColor = EditorGUILayout.ColorField("Fur Color", furColor);
            bellyColor = EditorGUILayout.ColorField("Belly Color", bellyColor);
            accentColor = EditorGUILayout.ColorField("Accent Color", accentColor);
        }

        EditorGUILayout.Space(16f);
        using (new EditorGUI.DisabledScope(!CanCreate()))
        {
            if (GUILayout.Button("Create Pet Prefab", GUILayout.Height(44f)))
                CreatePet();
        }

        EditorGUILayout.Space(8f);
        if (GUILayout.Button("Create Fox Cub Demo Preset", GUILayout.Height(32f)))
        {
            ApplyFoxCubPreset();
            CreatePet();
        }
    }

    private void UseSelectionAsSource()
    {
        if (Selection.activeObject is GameObject selected)
            sourcePrefab = selected;
    }

    private void ApplyFoxCubPreset()
    {
        PetPrefabBuildSettings preset = PetPrefabBuilder.CreateFoxCubPreset();
        petId = preset.PetId;
        displayName = preset.DisplayName;
        description = preset.Description;
        prefabName = preset.PrefabName;
        sourcePrefab = preset.SourcePrefab;
        animatorController = preset.AnimatorController;
        autoGenerateIcon = preset.AutoGenerateIcon;
        furColor = preset.FurColor;
        bellyColor = preset.BellyColor;
        accentColor = preset.AccentColor;
        inventoryIcon = null;
    }

    private bool CanCreate()
    {
        return sourcePrefab != null && !string.IsNullOrWhiteSpace(petId);
    }

    private void CreatePet()
    {
        PetPrefabBuildSettings settings = new PetPrefabBuildSettings
        {
            PetId = petId,
            DisplayName = displayName,
            Description = description,
            PrefabName = prefabName,
            SourcePrefab = sourcePrefab,
            AnimatorController = animatorController,
            InventoryIcon = inventoryIcon,
            AutoGenerateIcon = autoGenerateIcon,
            FurColor = furColor,
            BellyColor = bellyColor,
            AccentColor = accentColor
        };

        if (PetPrefabBuilder.Build(settings, out string message))
            Debug.Log(message);
        else
            Debug.LogError(message);
    }
}
