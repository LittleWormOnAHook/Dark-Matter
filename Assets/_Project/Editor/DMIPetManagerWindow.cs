using MalbersAnimations.PathCreation;
using Project.EditorTools;
using Project.Pet;
using Project.World;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Pet Manager — create pet prefabs / definitions and author melee + ranged combat settings
    /// (Creature Manager–style sections). Apply updates existing pets such as Brimmy.
    /// </summary>
    public class DMIPetManagerWindow : EditorWindow
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

        private GameObject targetPetPrefab;

        private DMIPetRangedAttackKind rangedAttackKind = DMIPetRangedAttackKind.None;
        private GameObject rangedProjectilePrefab;
        private GameObject rangedImpactVfxPrefab;
        private float rangedMinDamage = 5f;
        private float rangedMaxDamage = 10f;
        private float rangedDamageBonusPerLevel = 0.05f;
        private float rangedMinInterval = 3f;
        private float rangedMaxInterval = 8f;
        private float rangedMaxAttackRange = 22f;
        private float rangedOwnerLeashDistance = 16f;
        private float rangedAbandonAfterSeconds = 6f;
        private float rangedProjectileSpeed = 14f;

        private bool meleeEnabled;
        private float meleeEngageRange = 2.2f;
        private float meleeDamage = 8f;
        private float meleeDamageRandomRange = 4f;
        private float meleeDamageBonusPerLevel = 0.05f;
        private float meleeAttackCooldown = 1.4f;
        private float meleeIntervalVariation = 0.35f;
        private float meleeOwnerLeashDistance = 12f;
        private float meleeAbandonAfterSeconds = 6f;

        private bool pathFollowEnabled;
        private PathCreator petPatrolPath;
        private DMIPathPatrolMode pathPatrolMode = DMIPathPatrolMode.Loop;
        private float pathPatrolWaitDuration = 2f;
        private static readonly string[] PathPatrolModeLabels =
        {
            "Loop (ordered)",
            "Ping Pong (random)"
        };

        private Vector2 scroll;
        private string statusMessage = string.Empty;
        private MessageType statusType = MessageType.Info;

        [MenuItem(SurvivalPioneerEditorMenus.PetManager, false, 20)]
        public static void ShowWindow()
        {
            DMIPetManagerWindow window = GetWindow<DMIPetManagerWindow>("Pet Manager");
            window.minSize = new Vector2(460f, 640f);
        }

        [MenuItem(SurvivalPioneerEditorMenus.PetManagerFromSelection, false, 21)]
        private static void OpenFromSelection()
        {
            DMIPetManagerWindow window = GetWindow<DMIPetManagerWindow>("Pet Manager");
            window.minSize = new Vector2(460f, 640f);
            window.UseSelectionAsSource();
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);

            EditorGUILayout.LabelField("Pet Manager", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Create pet world prefabs, Resources copies, PetDefinition assets, and combat tuning. " +
                "Melee / Ranged sections mirror Creature Manager: edit fields, then Create or Apply to Prefab.",
                MessageType.Info);

            DrawIdentitySection();
            DrawSourceSection();
            DrawIconSection();
            DrawMeleeSection();
            DrawRangedSection();
            DrawActions();

            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.Space(8f);
                EditorGUILayout.HelpBox(statusMessage, statusType);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawIdentitySection()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
            petId = EditorGUILayout.TextField(
                new GUIContent("Pet Id", "Stable id used for definition / Resources path."),
                petId);
            displayName = EditorGUILayout.TextField("Display Name", displayName);
            prefabName = EditorGUILayout.TextField("Prefab Name", prefabName);
            description = EditorGUILayout.TextField("Description", description, GUILayout.MinHeight(40f));
        }

        private void DrawSourceSection()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Source / Target Prefab", EditorStyles.boldLabel);
            sourcePrefab = (GameObject)EditorGUILayout.ObjectField(
                new GUIContent("Source Prefab", "Mesh / visual source used when creating a new pet."),
                sourcePrefab,
                typeof(GameObject),
                false);
            animatorController = (RuntimeAnimatorController)EditorGUILayout.ObjectField(
                "Animator Controller",
                animatorController,
                typeof(RuntimeAnimatorController),
                false);
            targetPetPrefab = (GameObject)EditorGUILayout.ObjectField(
                new GUIContent(
                    "Existing Pet Prefab",
                    "Optional. Load settings from / Apply combat to this prefab (e.g. Brimmy)."),
                targetPetPrefab,
                typeof(GameObject),
                false);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Use Selection"))
                UseSelectionAsSource();
            if (GUILayout.Button("Load Fox Cub Preset"))
                ApplyFoxCubPreset();
            if (GUILayout.Button("Load From Prefab"))
                LoadCombatFromTargetPrefab();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawIconSection()
        {
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
        }

        private void DrawMeleeSection()
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Threat / Melee", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Melee Interval (sec) = time between hits (DMIPetMeleeAttack.meleeAttackCooldown). " +
                "Uses the player's current combat target, same assist rules as ranged.",
                MessageType.None);

            meleeEnabled = EditorGUILayout.Toggle(
                new GUIContent("Enable Melee", "Adds/configures DMIPetMeleeAttack on build/apply."),
                meleeEnabled);

            using (new EditorGUI.DisabledScope(!meleeEnabled))
            {
                meleeEngageRange = EditorGUILayout.FloatField(
                    new GUIContent("Melee Engage Range", "Horizontal distance at which swings are allowed."),
                    meleeEngageRange);
                meleeDamage = EditorGUILayout.FloatField(
                    new GUIContent("Melee Damage", "Base damage before level scaling."),
                    meleeDamage);
                meleeDamageRandomRange = EditorGUILayout.FloatField(
                    new GUIContent("Melee Damage Random", "Extra random damage on top of Melee Damage."),
                    meleeDamageRandomRange);
                meleeDamageBonusPerLevel = EditorGUILayout.FloatField(
                    new GUIContent("Damage Bonus / Level", "+% damage per player level above 1 (0.05 = +5%)."),
                    meleeDamageBonusPerLevel);
                meleeAttackCooldown = EditorGUILayout.FloatField(
                    new GUIContent(
                        "Melee Interval (sec)",
                        "Seconds between melee hits."),
                    meleeAttackCooldown);
                if (meleeAttackCooldown < 0.05f)
                    meleeAttackCooldown = 0.05f;

                meleeIntervalVariation = EditorGUILayout.Slider(
                    new GUIContent(
                        "Melee Interval Variation",
                        "Extra random delay: wait = Melee Interval + Random(0, this). Clamp 0–10s."),
                    meleeIntervalVariation,
                    0f,
                    10f);

                meleeOwnerLeashDistance = EditorGUILayout.FloatField(
                    new GUIContent("Owner Leash Distance", "Abandon melee when owner is farther than this from the target."),
                    meleeOwnerLeashDistance);
                meleeAbandonAfterSeconds = EditorGUILayout.FloatField(
                    new GUIContent("Abandon After (sec)", "Seconds beyond leash before stopping melee."),
                    meleeAbandonAfterSeconds);
            }
        }

        private void DrawRangedSection()
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Path Follow / Patrol", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Assign a Path Creator / Path Creator Variant. Edit anchors with Path Creator's native Scene tools only. " +
                "Loop (ordered) = next anchor in sequence. Ping Pong (random) = random next anchor. " +
                "Wait / Idle is seconds spent at each anchor. Pet follows until CallToOwner / combat clears path. " +
                "Create / Apply also writes these settings; path updates refresh anchors at runtime.",
                MessageType.None);
            pathFollowEnabled = EditorGUILayout.Toggle(
                new GUIContent("Enable Path Follow", "When on, pet registers with the Path Creator on Start."),
                pathFollowEnabled);
            using (new EditorGUI.DisabledScope(!pathFollowEnabled))
            {
                petPatrolPath = (PathCreator)EditorGUILayout.ObjectField(
                    new GUIContent("Path Creator", "Path Creator or Path Creator Variant. Apply writes to selected pets / prefab assets when path is persistent."),
                    petPatrolPath,
                    typeof(PathCreator),
                    true);

                int modeIndex = pathPatrolMode == DMIPathPatrolMode.PingPong ? 1 : 0;
                modeIndex = EditorGUILayout.Popup(
                    new GUIContent(
                        "Path Mode",
                        "Loop (ordered): travel anchors 0→1→2→…→0. Ping Pong (random): pick a random different anchor after each wait."),
                    modeIndex,
                    PathPatrolModeLabels);
                pathPatrolMode = modeIndex == 1 ? DMIPathPatrolMode.PingPong : DMIPathPatrolMode.Loop;

                pathPatrolWaitDuration = EditorGUILayout.FloatField(
                    new GUIContent(
                        "Wait / Idle At Anchors (sec)",
                        "Seconds the pet idles at each Path Creator anchor before moving on."),
                    pathPatrolWaitDuration);
                if (pathPatrolWaitDuration < 0f)
                    pathPatrolWaitDuration = 0f;

                if (GUILayout.Button("Apply Path Creator To Selected Pet"))
                    ApplyPetPatrolPathToSelection();
            }

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Ranged Attack", EditorStyles.boldLabel);

            rangedAttackKind = (DMIPetRangedAttackKind)EditorGUILayout.EnumPopup(
                new GUIContent("Ranged Attack", "None strips DMIPetRangedAttack. Fireball uses FireBall Lite Variant."),
                rangedAttackKind);

            using (new EditorGUI.DisabledScope(rangedAttackKind == DMIPetRangedAttackKind.None))
            {
                rangedProjectilePrefab = (GameObject)EditorGUILayout.ObjectField(
                    new GUIContent("Projectile Prefab", "Traveling projectile visual. Empty Fireball → FireBall Lite Variant."),
                    rangedProjectilePrefab,
                    typeof(GameObject),
                    false);
                rangedImpactVfxPrefab = (GameObject)EditorGUILayout.ObjectField(
                    new GUIContent(
                        "Impact VFX",
                        "Explosion/hit VFX via CombatHitResolver on every collider hit. Empty Fireball → Malbers Explosion."),
                    rangedImpactVfxPrefab,
                    typeof(GameObject),
                    false);

                if (rangedAttackKind == DMIPetRangedAttackKind.Fireball && rangedProjectilePrefab == null)
                {
                    EditorGUILayout.HelpBox(
                        "Fireball defaults to FireBall Lite Variant when Projectile Prefab is empty.",
                        MessageType.None);
                }

                rangedMinDamage = EditorGUILayout.FloatField(
                    new GUIContent("Damage Min", "Inclusive min base damage before level scaling."),
                    rangedMinDamage);
                rangedMaxDamage = EditorGUILayout.FloatField(
                    new GUIContent("Damage Max", "Inclusive max base damage before level scaling."),
                    rangedMaxDamage);
                rangedDamageBonusPerLevel = EditorGUILayout.FloatField(
                    new GUIContent("Damage Bonus / Level", "+% damage per player level above 1 (0.05 = +5%)."),
                    rangedDamageBonusPerLevel);
                rangedMinInterval = EditorGUILayout.FloatField(
                    new GUIContent("Interval Min (sec)", "Minimum seconds between shots."),
                    rangedMinInterval);
                rangedMaxInterval = EditorGUILayout.FloatField(
                    new GUIContent("Interval Max (sec)", "Maximum seconds between shots."),
                    rangedMaxInterval);
                rangedMaxAttackRange = EditorGUILayout.FloatField(
                    new GUIContent("Max Attack Range", "Pet will not fire beyond this horizontal distance."),
                    rangedMaxAttackRange);
                rangedProjectileSpeed = EditorGUILayout.FloatField(
                    new GUIContent("Projectile Speed", "Travel speed override for CombatProjectile."),
                    rangedProjectileSpeed);
                rangedOwnerLeashDistance = EditorGUILayout.FloatField(
                    new GUIContent("Owner Leash Distance", "Abandon ranged when owner is farther than this from the target."),
                    rangedOwnerLeashDistance);
                rangedAbandonAfterSeconds = EditorGUILayout.FloatField(
                    new GUIContent("Abandon After (sec)", "Seconds beyond leash before stopping ranged."),
                    rangedAbandonAfterSeconds);
            }
        }

        private void DrawActions()
        {
            EditorGUILayout.Space(16f);
            using (new EditorGUI.DisabledScope(!CanCreate()))
            {
                if (GUILayout.Button("Create Pet Prefab", GUILayout.Height(40f)))
                    CreatePet();
            }

            using (new EditorGUI.DisabledScope(targetPetPrefab == null && sourcePrefab == null))
            {
                if (GUILayout.Button("Apply Combat To Prefab", GUILayout.Height(34f)))
                    ApplyCombatToExisting();
            }

            EditorGUILayout.Space(8f);
            if (GUILayout.Button("Create Fox Cub Demo Preset", GUILayout.Height(28f)))
            {
                ApplyFoxCubPreset();
                CreatePet();
            }
        }

        private void UseSelectionAsSource()
        {
            if (Selection.activeObject is GameObject selected)
            {
                if (EditorUtility.IsPersistent(selected))
                {
                    targetPetPrefab = selected;
                    sourcePrefab = selected;
                    LoadCombatFromTargetPrefab();
                }
                else
                {
                    sourcePrefab = selected;
                }

                SetStatus($"Selection applied: {selected.name}", MessageType.Info);
            }
        }

        private void ApplyFoxCubPreset()
        {
            PetPrefabBuildSettings preset = PetPrefabBuilder.CreateFoxCubPreset();
            ApplyBuildSettingsToUi(preset);
            SetStatus("Loaded Fox Cub preset.", MessageType.Info);
        }

        private void ApplyBuildSettingsToUi(PetPrefabBuildSettings preset)
        {
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
            ApplyCombatSettingsToUi(preset);
        }

        private void ApplyCombatSettingsToUi(PetPrefabBuildSettings settings)
        {
            rangedAttackKind = settings.RangedAttackKind;
            rangedProjectilePrefab = settings.RangedProjectilePrefab;
            rangedImpactVfxPrefab = settings.RangedImpactVfxPrefab;
            rangedMinDamage = settings.RangedMinDamage > 0f ? settings.RangedMinDamage : 5f;
            rangedMaxDamage = settings.RangedMaxDamage > 0f ? settings.RangedMaxDamage : 10f;
            rangedDamageBonusPerLevel = settings.RangedDamageBonusPerLevel > 0f ? settings.RangedDamageBonusPerLevel : 0.05f;
            rangedMinInterval = settings.RangedMinInterval > 0f ? settings.RangedMinInterval : 3f;
            rangedMaxInterval = settings.RangedMaxInterval > 0f ? settings.RangedMaxInterval : 8f;
            rangedMaxAttackRange = settings.RangedMaxAttackRange > 0f ? settings.RangedMaxAttackRange : 22f;
            rangedOwnerLeashDistance = settings.RangedOwnerLeashDistance > 0f ? settings.RangedOwnerLeashDistance : 16f;
            rangedAbandonAfterSeconds = settings.RangedAbandonAfterSeconds > 0f ? settings.RangedAbandonAfterSeconds : 6f;
            rangedProjectileSpeed = settings.RangedProjectileSpeed > 0f ? settings.RangedProjectileSpeed : 14f;

            meleeEnabled = settings.MeleeEnabled;
            meleeEngageRange = settings.MeleeEngageRange > 0f ? settings.MeleeEngageRange : 2.2f;
            meleeDamage = settings.MeleeDamage > 0f ? settings.MeleeDamage : 8f;
            meleeDamageRandomRange = Mathf.Max(0f, settings.MeleeDamageRandomRange);
            meleeDamageBonusPerLevel = settings.MeleeDamageBonusPerLevel > 0f ? settings.MeleeDamageBonusPerLevel : 0.05f;
            meleeAttackCooldown = settings.MeleeAttackCooldown > 0f ? settings.MeleeAttackCooldown : 1.4f;
            meleeIntervalVariation = Mathf.Clamp(settings.MeleeIntervalVariation, 0f, 10f);
            meleeOwnerLeashDistance = settings.MeleeOwnerLeashDistance > 0f ? settings.MeleeOwnerLeashDistance : 12f;
            meleeAbandonAfterSeconds = settings.MeleeAbandonAfterSeconds > 0f ? settings.MeleeAbandonAfterSeconds : 6f;

            pathFollowEnabled = settings.PathFollowEnabled;
            petPatrolPath = settings.PatrolPath;
            pathPatrolMode = settings.PathPatrolMode;
            pathPatrolWaitDuration = settings.PathPatrolWaitDuration >= 0f ? settings.PathPatrolWaitDuration : 2f;
        }

        private void LoadCombatFromTargetPrefab()
        {
            GameObject prefab = targetPetPrefab != null ? targetPetPrefab : sourcePrefab;
            if (prefab == null)
            {
                SetStatus("Assign an Existing Pet Prefab first.", MessageType.Warning);
                return;
            }

            PetPrefabBuildSettings settings = PetPrefabBuilder.CreateDefaultCombatSettings();
            DMIPetRangedAttack ranged = prefab.GetComponent<DMIPetRangedAttack>();
            if (ranged != null)
            {
                SerializedObject so = new SerializedObject(ranged);
                settings.RangedAttackKind = (DMIPetRangedAttackKind)so.FindProperty("attackKind").enumValueIndex;
                settings.RangedProjectilePrefab = so.FindProperty("projectilePrefab").objectReferenceValue as GameObject;
                settings.RangedImpactVfxPrefab = so.FindProperty("impactVfxPrefab").objectReferenceValue as GameObject;
                settings.RangedMinDamage = so.FindProperty("minBaseDamage").floatValue;
                settings.RangedMaxDamage = so.FindProperty("maxBaseDamage").floatValue;
                settings.RangedDamageBonusPerLevel = so.FindProperty("damageBonusPerLevel").floatValue;
                settings.RangedMinInterval = so.FindProperty("minAttackInterval").floatValue;
                settings.RangedMaxInterval = so.FindProperty("maxAttackInterval").floatValue;
                settings.RangedMaxAttackRange = so.FindProperty("maxAttackRange").floatValue;
                settings.RangedOwnerLeashDistance = so.FindProperty("ownerLeashDistance").floatValue;
                settings.RangedAbandonAfterSeconds = so.FindProperty("abandonAfterSeconds").floatValue;
                settings.RangedProjectileSpeed = so.FindProperty("projectileSpeed").floatValue;
            }

            DMIPetMeleeAttack melee = prefab.GetComponent<DMIPetMeleeAttack>();
            if (melee != null)
            {
                SerializedObject so = new SerializedObject(melee);
                settings.MeleeEnabled = so.FindProperty("meleeEnabled").boolValue;
                settings.MeleeEngageRange = so.FindProperty("meleeEngageRange").floatValue;
                settings.MeleeDamage = so.FindProperty("meleeDamage").floatValue;
                settings.MeleeDamageRandomRange = so.FindProperty("meleeDamageRandomRange").floatValue;
                settings.MeleeDamageBonusPerLevel = so.FindProperty("damageBonusPerLevel").floatValue;
                settings.MeleeAttackCooldown = so.FindProperty("meleeAttackCooldown").floatValue;
                settings.MeleeIntervalVariation = so.FindProperty("meleeIntervalVariation").floatValue;
                settings.MeleeOwnerLeashDistance = so.FindProperty("ownerLeashDistance").floatValue;
                settings.MeleeAbandonAfterSeconds = so.FindProperty("abandonAfterSeconds").floatValue;
            }

            PetController pet = prefab.GetComponent<PetController>()
                ?? prefab.GetComponentInChildren<PetController>(true);
            if (pet != null)
            {
                SerializedObject so = new SerializedObject(pet);
                settings.PathFollowEnabled = so.FindProperty("pathFollowEnabled").boolValue;
                settings.PatrolPath = so.FindProperty("patrolPath").objectReferenceValue as PathCreator;
                SerializedProperty modeProp = so.FindProperty("pathPatrolMode");
                if (modeProp != null)
                    settings.PathPatrolMode = (DMIPathPatrolMode)modeProp.enumValueIndex;
                SerializedProperty waitProp = so.FindProperty("pathPatrolWaitDuration");
                if (waitProp != null)
                    settings.PathPatrolWaitDuration = waitProp.floatValue;
            }

            ApplyCombatSettingsToUi(settings);
            targetPetPrefab = prefab;
            SetStatus($"Loaded combat settings from {prefab.name}.", MessageType.Info);
        }

        private bool CanCreate()
        {
            return sourcePrefab != null && !string.IsNullOrWhiteSpace(petId);
        }

        private PetPrefabBuildSettings BuildSettingsFromUi()
        {
            return new PetPrefabBuildSettings
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
                AccentColor = accentColor,
                RangedAttackKind = rangedAttackKind,
                RangedProjectilePrefab = rangedProjectilePrefab,
                RangedImpactVfxPrefab = rangedImpactVfxPrefab,
                RangedMinDamage = rangedMinDamage,
                RangedMaxDamage = rangedMaxDamage,
                RangedDamageBonusPerLevel = rangedDamageBonusPerLevel,
                RangedMinInterval = rangedMinInterval,
                RangedMaxInterval = rangedMaxInterval,
                RangedMaxAttackRange = rangedMaxAttackRange,
                RangedOwnerLeashDistance = rangedOwnerLeashDistance,
                RangedAbandonAfterSeconds = rangedAbandonAfterSeconds,
                RangedProjectileSpeed = rangedProjectileSpeed,
                MeleeEnabled = meleeEnabled,
                MeleeEngageRange = meleeEngageRange,
                MeleeDamage = meleeDamage,
                MeleeDamageRandomRange = meleeDamageRandomRange,
                MeleeDamageBonusPerLevel = meleeDamageBonusPerLevel,
                MeleeAttackCooldown = meleeAttackCooldown,
                MeleeIntervalVariation = meleeIntervalVariation,
                MeleeOwnerLeashDistance = meleeOwnerLeashDistance,
                MeleeAbandonAfterSeconds = meleeAbandonAfterSeconds,
                PathFollowEnabled = pathFollowEnabled,
                PatrolPath = petPatrolPath,
                PathPatrolMode = pathPatrolMode,
                PathPatrolWaitDuration = pathPatrolWaitDuration
            };
        }

        private void ApplyPetPatrolPathToSelection(GameObject extraRoot = null)
        {
            if (!pathFollowEnabled)
            {
                SetStatus("Enable Path Follow first.", MessageType.Warning);
                return;
            }

            if (petPatrolPath == null)
            {
                SetStatus("Assign a Path Creator first.", MessageType.Warning);
                return;
            }

            int applied = DMIPathFollowEditorUtility.ApplyToPets(
                petPatrolPath,
                enablePathFollow: true,
                pathPatrolMode,
                pathPatrolWaitDuration,
                extraRoot);
            SetStatus(
                applied > 0
                    ? $"Assigned Path Creator ({PathPatrolModeLabels[pathPatrolMode == DMIPathPatrolMode.PingPong ? 1 : 0]}, wait {pathPatrolWaitDuration:0.##}s) to {applied} pet(s)."
                    : "Select a scene pet (or persistent path + prefab asset), then apply.",
                applied > 0 ? MessageType.Info : MessageType.Warning);
        }

        private void CreatePet()
        {
            if (PetPrefabBuilder.Build(BuildSettingsFromUi(), out string message))
            {
                if (pathFollowEnabled && petPatrolPath != null)
                {
                    int applied = DMIPathFollowEditorUtility.ApplyToPets(
                        petPatrolPath,
                        enablePathFollow: true,
                        pathPatrolMode,
                        pathPatrolWaitDuration);
                    message += applied > 0
                        ? $"\nAssigned Path Creator to {applied} pet target(s)."
                        : "\nPath Follow enabled on prefab; select a scene pet to assign a scene Path Creator.";
                }

                SetStatus(message, MessageType.Info);
            }
            else
                SetStatus(message, MessageType.Error);
        }

        private void ApplyCombatToExisting()
        {
            GameObject prefab = targetPetPrefab != null ? targetPetPrefab : sourcePrefab;
            if (prefab == null)
            {
                SetStatus("Assign an Existing Pet Prefab (or Source Prefab) to apply combat.", MessageType.Warning);
                return;
            }

            string path = AssetDatabase.GetAssetPath(prefab);
            if (string.IsNullOrEmpty(path))
            {
                SetStatus("Target must be a project prefab asset.", MessageType.Error);
                return;
            }

            PetPrefabBuildSettings settings = BuildSettingsFromUi();
            if (PetPrefabBuilder.ApplyCombatToPrefab(path, settings, out string message))
            {
                // Keep Resources twin / Prefabs twin in sync for Brimmy-style duplicates.
                string alt = path.Contains("/Resources/Pets/")
                    ? path.Replace("/Resources/Pets/", "/Prefabs/Pets/")
                    : path.Replace("/Prefabs/Pets/", "/Resources/Pets/");
                if (!string.Equals(alt, path, System.StringComparison.OrdinalIgnoreCase) &&
                    AssetDatabase.LoadAssetAtPath<GameObject>(alt) != null)
                {
                    PetPrefabBuilder.ApplyCombatToPrefab(alt, settings, out _);
                }

                if (pathFollowEnabled && petPatrolPath != null)
                    ApplyPetPatrolPathToSelection();

                SetStatus(message, MessageType.Info);
            }
            else
            {
                SetStatus(message, MessageType.Error);
            }
        }

        private void SetStatus(string message, MessageType type)
        {
            statusMessage = message;
            statusType = type;
            if (type == MessageType.Error)
                Debug.LogError(message);
            else
                Debug.Log(message);
        }
    }
}
