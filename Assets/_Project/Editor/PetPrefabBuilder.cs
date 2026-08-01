using System.IO;
using MalbersAnimations.PathCreation;
using Project.Pet;
using Project.World;
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

        public DMIPetRangedAttackKind RangedAttackKind;
        public GameObject RangedProjectilePrefab;
        public GameObject RangedImpactVfxPrefab;
        public float RangedMinDamage;
        public float RangedMaxDamage;
        public float RangedDamageBonusPerLevel;
        public float RangedMinInterval;
        public float RangedMaxInterval;
        public float RangedMaxAttackRange;
        public float RangedOwnerLeashDistance;
        public float RangedAbandonAfterSeconds;
        public float RangedProjectileSpeed;

        public bool MeleeEnabled;
        public float MeleeEngageRange;
        public float MeleeDamage;
        public float MeleeDamageRandomRange;
        public float MeleeDamageBonusPerLevel;
        public float MeleeAttackCooldown;
        public float MeleeIntervalVariation;
        public float MeleeOwnerLeashDistance;
        public float MeleeAbandonAfterSeconds;

        public bool PathFollowEnabled;
        public PathCreator PatrolPath;
        public DMIPathPatrolMode PathPatrolMode;
        public float PathPatrolWaitDuration;
    }

    public static class PetPrefabBuilder
    {
        private const string PrefabFolder = ProjectAssetPaths.PrefabsPets;
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

                ApplyRangedAttack(root, settings);
                ApplyMeleeAttack(root, settings);
                ApplyPathFollow(root, settings);

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

        /// <summary>
        /// Applies combat settings (melee + ranged) onto an existing pet prefab asset and its Resources twin.
        /// </summary>
        public static bool ApplyCombatToPrefab(string prefabPath, PetPrefabBuildSettings settings, out string message)
        {
            message = string.Empty;
            if (string.IsNullOrWhiteSpace(prefabPath))
            {
                message = "PetPrefabBuilder: Prefab path is required.";
                return false;
            }

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            if (prefabRoot == null)
            {
                message = $"PetPrefabBuilder: Failed to open prefab at {prefabPath}";
                return false;
            }

            try
            {
                ApplyRangedAttack(prefabRoot, settings);
                ApplyMeleeAttack(prefabRoot, settings);
                ApplyPathFollow(prefabRoot, settings);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);

                string fileName = Path.GetFileName(prefabPath);
                string resourcesPath = $"{ResourcesFolder}/{fileName}";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(resourcesPath) != null &&
                    !string.Equals(resourcesPath, prefabPath, System.StringComparison.OrdinalIgnoreCase))
                {
                    AssetDatabase.CopyAsset(prefabPath, resourcesPath);
                }

                message = $"PetPrefabBuilder: Applied combat settings to {prefabPath}";
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        public static PetPrefabBuildSettings CreateDefaultCombatSettings()
        {
            return new PetPrefabBuildSettings
            {
                RangedAttackKind = DMIPetRangedAttackKind.None,
                RangedMinDamage = 5f,
                RangedMaxDamage = 10f,
                RangedDamageBonusPerLevel = 0.05f,
                RangedMinInterval = 3f,
                RangedMaxInterval = 8f,
                RangedMaxAttackRange = 22f,
                RangedOwnerLeashDistance = 16f,
                RangedAbandonAfterSeconds = 6f,
                RangedProjectileSpeed = 14f,
                MeleeEnabled = false,
                MeleeEngageRange = 2.2f,
                MeleeDamage = 8f,
                MeleeDamageRandomRange = 4f,
                MeleeDamageBonusPerLevel = 0.05f,
                MeleeAttackCooldown = 1.4f,
                MeleeIntervalVariation = 0.35f,
                MeleeOwnerLeashDistance = 12f,
                MeleeAbandonAfterSeconds = 6f,
                PathFollowEnabled = false,
                PathPatrolMode = DMIPathPatrolMode.Loop,
                PathPatrolWaitDuration = 2f
            };
        }

        public static PetPrefabBuildSettings CreateFoxCubPreset()
        {
            PetPrefabBuildSettings settings = CreateDefaultCombatSettings();
            settings.PetId = "fox_cub";
            settings.DisplayName = "Fox Cub";
            settings.Description = "A loyal companion that gathers nearby items.";
            settings.SourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/Players/Fox Cub Variant.prefab");
            settings.AnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                "Assets/_Project/Animations/PetFoxController.controller");
            settings.AutoGenerateIcon = true;
            settings.FurColor = new Color(0.92f, 0.45f, 0.12f, 1f);
            settings.BellyColor = new Color(0.98f, 0.82f, 0.62f, 1f);
            settings.AccentColor = new Color(0.12f, 0.1f, 0.1f, 1f);
            settings.PrefabName = "FoxCub";
            return settings;
        }

        public static PetPrefabBuildSettings CreateRickyPreset()
        {
            PetPrefabBuildSettings settings = CreateDefaultCombatSettings();
            settings.PetId = "ricky";
            settings.DisplayName = "Ricky";
            settings.Description = "Ricky the Racoon, a troublesome but loyal companion that gathers nearby items.";
            settings.SourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Malbers Animations/Animals Packs/01 Forest Pack/Raccoon/Models/Raccoon PA.prefab");
            settings.AnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                "Assets/_Project/Animations/PetRaccoonController.controller");
            settings.AutoGenerateIcon = true;
            settings.FurColor = new Color(0.45f, 0.42f, 0.4f, 1f);
            settings.BellyColor = new Color(0.78f, 0.74f, 0.7f, 1f);
            settings.AccentColor = new Color(0.12f, 0.1f, 0.1f, 1f);
            settings.PrefabName = "Ricky";
            return settings;
        }

        /// <summary>
        /// Adds or strips <see cref="DMIPetRangedAttack"/> on an existing pet prefab asset (e.g. Brimmy).
        /// </summary>
        public static bool ApplyRangedAttackToPrefab(string prefabPath, DMIPetRangedAttackKind kind, out string message)
        {
            PetPrefabBuildSettings settings = CreateDefaultCombatSettings();
            settings.RangedAttackKind = kind;
            settings.RangedProjectilePrefab = kind == DMIPetRangedAttackKind.Fireball
                ? AssetDatabase.LoadAssetAtPath<GameObject>(DMIPetRangedAttack.FireballProjectilePath)
                : null;
            settings.RangedImpactVfxPrefab = kind == DMIPetRangedAttackKind.Fireball
                ? AssetDatabase.LoadAssetAtPath<GameObject>(DMIPetRangedAttack.FireballImpactVfxPath)
                : null;
            return ApplyCombatToPrefab(prefabPath, settings, out message);
        }

        private static void ApplyRangedAttack(GameObject root, PetPrefabBuildSettings settings)
        {
            DMIPetRangedAttack ranged = root.GetComponent<DMIPetRangedAttack>();

            if (settings.RangedAttackKind == DMIPetRangedAttackKind.None)
            {
                if (ranged != null)
                    Object.DestroyImmediate(ranged);
                return;
            }

            if (ranged == null)
                ranged = root.AddComponent<DMIPetRangedAttack>();

            GameObject projectile = settings.RangedProjectilePrefab;
            if (projectile == null && settings.RangedAttackKind == DMIPetRangedAttackKind.Fireball)
                projectile = AssetDatabase.LoadAssetAtPath<GameObject>(DMIPetRangedAttack.FireballProjectilePath);

            GameObject impact = settings.RangedImpactVfxPrefab;
            if (impact == null && settings.RangedAttackKind == DMIPetRangedAttackKind.Fireball)
                impact = AssetDatabase.LoadAssetAtPath<GameObject>(DMIPetRangedAttack.FireballImpactVfxPath);

            float minDamage = settings.RangedMinDamage > 0f ? settings.RangedMinDamage : 5f;
            float maxDamage = settings.RangedMaxDamage > 0f ? settings.RangedMaxDamage : 10f;
            float minInterval = settings.RangedMinInterval > 0f ? settings.RangedMinInterval : 3f;
            float maxInterval = settings.RangedMaxInterval > 0f ? settings.RangedMaxInterval : 8f;
            float range = settings.RangedMaxAttackRange > 0f ? settings.RangedMaxAttackRange : 22f;
            float leash = settings.RangedOwnerLeashDistance > 0f ? settings.RangedOwnerLeashDistance : 16f;
            float abandon = settings.RangedAbandonAfterSeconds > 0f ? settings.RangedAbandonAfterSeconds : 6f;
            float speed = settings.RangedProjectileSpeed > 0f ? settings.RangedProjectileSpeed : 14f;
            float levelBonus = settings.RangedDamageBonusPerLevel > 0f ? settings.RangedDamageBonusPerLevel : 0.05f;

            ranged.ConfigureSettings(
                settings.RangedAttackKind,
                projectile,
                impact,
                minDamage,
                maxDamage,
                levelBonus,
                minInterval,
                maxInterval,
                range,
                leash,
                abandon,
                speed);

            SerializedObject so = new SerializedObject(ranged);
            so.FindProperty("attackKind").enumValueIndex = (int)settings.RangedAttackKind;
            so.FindProperty("projectilePrefab").objectReferenceValue = projectile;
            so.FindProperty("impactVfxPrefab").objectReferenceValue = impact;
            so.FindProperty("minBaseDamage").floatValue = minDamage;
            so.FindProperty("maxBaseDamage").floatValue = maxDamage;
            so.FindProperty("damageBonusPerLevel").floatValue = levelBonus;
            so.FindProperty("minAttackInterval").floatValue = minInterval;
            so.FindProperty("maxAttackInterval").floatValue = maxInterval;
            so.FindProperty("maxAttackRange").floatValue = range;
            so.FindProperty("ownerLeashDistance").floatValue = leash;
            so.FindProperty("abandonAfterSeconds").floatValue = abandon;
            so.FindProperty("projectileSpeed").floatValue = speed;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ApplyMeleeAttack(GameObject root, PetPrefabBuildSettings settings)
        {
            DMIPetMeleeAttack melee = root.GetComponent<DMIPetMeleeAttack>();

            if (!settings.MeleeEnabled)
            {
                // Keep component if already present but disabled, so Pet Manager can re-enable without rebuild.
                if (melee != null)
                {
                    melee.ConfigureSettings(
                        false,
                        settings.MeleeEngageRange > 0f ? settings.MeleeEngageRange : 2.2f,
                        settings.MeleeDamage > 0f ? settings.MeleeDamage : 8f,
                        Mathf.Max(0f, settings.MeleeDamageRandomRange),
                        settings.MeleeDamageBonusPerLevel > 0f ? settings.MeleeDamageBonusPerLevel : 0.05f,
                        settings.MeleeAttackCooldown > 0f ? settings.MeleeAttackCooldown : 1.4f,
                        Mathf.Clamp(settings.MeleeIntervalVariation, 0f, 10f),
                        settings.MeleeOwnerLeashDistance > 0f ? settings.MeleeOwnerLeashDistance : 12f,
                        settings.MeleeAbandonAfterSeconds > 0f ? settings.MeleeAbandonAfterSeconds : 6f);
                    EditorUtility.SetDirty(melee);
                }

                return;
            }

            if (melee == null)
                melee = root.AddComponent<DMIPetMeleeAttack>();

            float engage = settings.MeleeEngageRange > 0f ? settings.MeleeEngageRange : 2.2f;
            float damage = settings.MeleeDamage > 0f ? settings.MeleeDamage : 8f;
            float damageRandom = Mathf.Max(0f, settings.MeleeDamageRandomRange);
            float levelBonus = settings.MeleeDamageBonusPerLevel > 0f ? settings.MeleeDamageBonusPerLevel : 0.05f;
            float interval = settings.MeleeAttackCooldown > 0f ? settings.MeleeAttackCooldown : 1.4f;
            float variation = Mathf.Clamp(settings.MeleeIntervalVariation, 0f, 10f);
            float leash = settings.MeleeOwnerLeashDistance > 0f ? settings.MeleeOwnerLeashDistance : 12f;
            float abandon = settings.MeleeAbandonAfterSeconds > 0f ? settings.MeleeAbandonAfterSeconds : 6f;

            melee.ConfigureSettings(
                true,
                engage,
                damage,
                damageRandom,
                levelBonus,
                interval,
                variation,
                leash,
                abandon);

            SerializedObject so = new SerializedObject(melee);
            so.FindProperty("meleeEnabled").boolValue = true;
            so.FindProperty("meleeEngageRange").floatValue = engage;
            so.FindProperty("meleeDamage").floatValue = damage;
            so.FindProperty("meleeDamageRandomRange").floatValue = damageRandom;
            so.FindProperty("damageBonusPerLevel").floatValue = levelBonus;
            so.FindProperty("meleeAttackCooldown").floatValue = interval;
            so.FindProperty("meleeIntervalVariation").floatValue = variation;
            so.FindProperty("ownerLeashDistance").floatValue = leash;
            so.FindProperty("abandonAfterSeconds").floatValue = abandon;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ApplyPathFollow(GameObject root, PetPrefabBuildSettings settings)
        {
            DMIPathFollowEditorUtility.TryWritePetPathOnPrefabRoot(
                root,
                settings.PatrolPath,
                settings.PathFollowEnabled,
                settings.PathPatrolMode,
                settings.PathPatrolWaitDuration);
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
