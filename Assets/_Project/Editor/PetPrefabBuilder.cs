using System.IO;
using Project.Pet;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    public struct PetPrefabBuildSettings
    {
        public string PetId;
        public string DisplayName;
        public string Description;
        public GameObject SourcePrefab;
        public RuntimeAnimatorController AnimatorController;
        public Sprite InventoryIcon;
        public bool AutoGenerateIcon;
        public Color FurColor;
        public Color BellyColor;
        public Color AccentColor;
        public string PrefabName;
    }

    public static class PetPrefabBuilder
    {
        private const string PrefabFolder = "Assets/_Project/Prefabs/Pets";
        private const string ResourcesFolder = "Assets/_Project/Resources/Pets";
        private const string DefinitionFolder = "Assets/_Project/Resources/Pets/Definitions";
        private const string IconFolder = "Assets/_Project/Resources/Pets/Icons";

        public static bool Build(PetPrefabBuildSettings settings, out string message)
        {
            message = string.Empty;
            if (settings.SourcePrefab == null)
            {
                message = "PetPrefabBuilder: Source prefab is required.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(settings.PetId))
            {
                message = "PetPrefabBuilder: Pet id is required.";
                return false;
            }

            string petId = SanitizePetId(settings.PetId);
            string prefabName = string.IsNullOrWhiteSpace(settings.PrefabName)
                ? ToPascalCase(petId)
                : settings.PrefabName.Trim();

            EnsureFolder(PrefabFolder);
            EnsureFolder(ResourcesFolder);
            EnsureFolder(DefinitionFolder);
            EnsureFolder(IconFolder);

            Sprite icon = settings.InventoryIcon;
            if (icon == null && settings.AutoGenerateIcon)
                icon = EnsureGeneratedIcon(petId, settings.FurColor, settings.BellyColor, settings.AccentColor);

            if (icon == null)
            {
                message = "PetPrefabBuilder: Inventory icon is required (assign one or enable auto-generate).";
                return false;
            }

            PetDefinition definition = EnsurePetDefinition(
                petId,
                string.IsNullOrWhiteSpace(settings.DisplayName) ? prefabName : settings.DisplayName,
                string.IsNullOrWhiteSpace(settings.Description)
                    ? "A loyal companion."
                    : settings.Description,
                icon);

            string outputPrefabPath = $"{PrefabFolder}/{prefabName}.prefab";
            string resourcesPrefabPath = $"{ResourcesFolder}/{prefabName}.prefab";

            GameObject root = PrefabUtility.InstantiatePrefab(settings.SourcePrefab) as GameObject;
            if (root == null)
            {
                message = "PetPrefabBuilder: Failed to instantiate source prefab.";
                return false;
            }

            try
            {
                root.name = prefabName;

                PetController controller = root.GetComponent<PetController>();
                if (controller == null)
                    controller = root.AddComponent<PetController>();

                if (root.GetComponent<PetAnimationController>() == null)
                    root.AddComponent<PetAnimationController>();

                PetWorldAdoptable adoptable = root.GetComponent<PetWorldAdoptable>();
                if (adoptable == null)
                    adoptable = root.AddComponent<PetWorldAdoptable>();

                Animator animator = root.GetComponentInChildren<Animator>(true);
                if (animator != null && settings.AnimatorController != null)
                {
                    animator.runtimeAnimatorController = settings.AnimatorController;
                    animator.applyRootMotion = false;
                }

                EnsureTriggerCollider(root);

                PetAnimationController animationController = root.GetComponent<PetAnimationController>();
                if (animationController != null && settings.AnimatorController != null)
                {
                    SerializedObject animationSo = new SerializedObject(animationController);
                    animationSo.FindProperty("petAnimatorController").objectReferenceValue = settings.AnimatorController;
                    animationSo.FindProperty("idleState").stringValue = "Idle";
                    animationSo.FindProperty("walkState").stringValue = "Walk";
                    animationSo.FindProperty("runState").stringValue = "Run";
                    animationSo.ApplyModifiedPropertiesWithoutUndo();
                }

                SerializedObject controllerSo = new SerializedObject(controller);
                controllerSo.FindProperty("definition").objectReferenceValue = definition;
                controllerSo.FindProperty("petId").stringValue = definition.petId;
                controllerSo.FindProperty("displayName").stringValue = definition.displayName;
                controllerSo.FindProperty("description").stringValue = definition.description;
                controllerSo.FindProperty("inventoryIcon").objectReferenceValue = definition.inventoryIcon;
                controllerSo.FindProperty("isOwned").boolValue = false;
                controllerSo.FindProperty("companionActive").boolValue = false;
                controllerSo.ApplyModifiedPropertiesWithoutUndo();

                SavePrefab(root, outputPrefabPath);
                if (AssetDatabase.LoadAssetAtPath<GameObject>(resourcesPrefabPath) != null)
                    AssetDatabase.DeleteAsset(resourcesPrefabPath);
                AssetDatabase.CopyAsset(outputPrefabPath, resourcesPrefabPath);

                definition.worldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(resourcesPrefabPath);
                EditorUtility.SetDirty(definition);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                message =
                    $"Pet '{definition.displayName}' created.\n" +
                    $"Prefab: {outputPrefabPath}\n" +
                    $"Resources: {resourcesPrefabPath}\n" +
                    $"Definition: {DefinitionFolder}/{petId}.asset";
                return true;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        public static PetPrefabBuildSettings CreateFoxCubPreset()
        {
            return new PetPrefabBuildSettings
            {
                PetId = "fox_cub",
                DisplayName = "Fox Cub",
                Description = "A loyal companion that gathers nearby items.",
                SourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/_Project/Prefabs/Players/Fox Cub Variant.prefab"),
                AnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                    "Assets/_Project/Animations/PetFoxController.controller"),
                AutoGenerateIcon = true,
                FurColor = new Color(0.92f, 0.45f, 0.12f, 1f),
                BellyColor = new Color(0.98f, 0.82f, 0.62f, 1f),
                AccentColor = new Color(0.12f, 0.1f, 0.1f, 1f),
                PrefabName = "FoxCub"
            };
        }

        public static PetPrefabBuildSettings CreateRickyPreset()
        {
            return new PetPrefabBuildSettings
            {
                PetId = "ricky",
                DisplayName = "Ricky",
                Description = "Ricky the Racoon, a troublesome but loyal companion that gathers nearby items.",
                SourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Malbers Animations/Animals Packs/01 Forest Pack/Raccoon/Models/Raccoon PA.prefab"),
                AnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                    "Assets/_Project/Animations/PetRaccoonController.controller"),
                AutoGenerateIcon = true,
                FurColor = new Color(0.45f, 0.42f, 0.4f, 1f),
                BellyColor = new Color(0.78f, 0.74f, 0.7f, 1f),
                AccentColor = new Color(0.12f, 0.1f, 0.1f, 1f),
                PrefabName = "Ricky"
            };
        }

        private static PetDefinition EnsurePetDefinition(string petId, string displayName, string description, Sprite icon)
        {
            string definitionPath = $"{DefinitionFolder}/{petId}.asset";
            PetDefinition definition = AssetDatabase.LoadAssetAtPath<PetDefinition>(definitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<PetDefinition>();
                AssetDatabase.CreateAsset(definition, definitionPath);
            }

            definition.petId = petId;
            definition.displayName = displayName;
            definition.description = description;
            definition.inventoryIcon = icon;
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static Sprite EnsureGeneratedIcon(string petId, Color fur, Color belly, Color accent)
        {
            string iconPath = $"{IconFolder}/{petId}_icon.png";
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            if (existing != null)
                return existing;

            Texture2D texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    float nx = (x - 32f) / 32f;
                    float ny = (y - 28f) / 32f;
                    float head = nx * nx + ny * ny;
                    Color pixel = Color.clear;
                    if (head <= 1f)
                        pixel = ny < -0.05f ? belly : fur;
                    if (x > 18 && x < 24 && y > 40 && y < 52)
                        pixel = fur;
                    if (x > 40 && x < 46 && y > 40 && y < 52)
                        pixel = fur;
                    if (x > 28 && x < 36 && y > 22 && y < 30)
                        pixel = accent;
                    texture.SetPixel(x, y, pixel);
                }
            }

            texture.Apply();
            File.WriteAllBytes(iconPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(iconPath, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(iconPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 64f;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
        }

        private static void EnsureTriggerCollider(GameObject root)
        {
            Collider collider = root.GetComponent<Collider>();
            if (collider == null)
            {
                CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
                capsule.height = 1.2f;
                capsule.radius = 0.35f;
                capsule.center = new Vector3(0f, 0.55f, 0f);
                capsule.isTrigger = true;
                return;
            }

            collider.isTrigger = true;
        }

        private static void SavePrefab(GameObject source, string assetPath)
        {
            if (source == null)
                return;

            PrefabUtility.SaveAsPrefabAsset(source, assetPath);
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            string[] parts = folderPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static string SanitizePetId(string raw)
        {
            string value = raw.Trim().ToLowerInvariant().Replace(' ', '_');
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (!char.IsLetterOrDigit(c) && c != '_')
                    value = value.Replace(c, '_');
            }

            return value;
        }

        private static string ToPascalCase(string petId)
        {
            string[] parts = petId.Split('_');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length == 0)
                    continue;
                parts[i] = char.ToUpperInvariant(parts[i][0]) + parts[i].Substring(1);
            }

            return string.Concat(parts);
        }
    }
}
