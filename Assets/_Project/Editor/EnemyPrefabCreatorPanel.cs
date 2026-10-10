#if UNITY_EDITOR
using System;
using MalbersAnimations.PathCreation;
using Project.AI;
using Project.Data;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    public sealed class EnemyPrefabCreatorPanelContext
    {
        public EnemyDefinition Definition;
        public string DefinitionAssetFileName;
        public PathCreator PatrolPathCreator;
        public bool ShowDefinitionAssetName = true;
        public bool ShowArchetype = true;
        public bool ShowIdentityFields = true;
        public bool ShowHumanoidInfoBox = true;
        public Action ApplyPatrolPath;
        public Action ApplyLoot;
    }

    /// <summary>
    /// Shared IMGUI for Enemy Prefab Creator and Genesis Studio Character Creator (Enemy tab).
    /// </summary>
    public static class EnemyPrefabCreatorPanel
    {
        public static void DrawIntroHelpBox(bool compact)
        {
            if (compact)
            {
                DMCharacterCreatorSharedUi.DrawWrappedHelpBox(
                "Clone HumanoidEnemy_Invector, paste a Humanoid mesh, then tune AI, loot, and combat below. " +
                DMHumanoidVisualRebuildUtility.BoneRenameHelp + " " +
                "Save Definition Asset links this setup to Combat → Enemy Types.",
                    MessageType.Info);
                return;
            }

            DMCharacterCreatorSharedUi.DrawWrappedHelpBox(
                "Humanoid: clone HumanoidEnemy_Invector → paste rigged Meshy FBX (tool can Humanoid-rig the import). " +
                DMHumanoidVisualRebuildUtility.BoneRenameHelp + " " +
                "Full AI/loot/combat unchanged. Generic creatures: LegacyCreature + Animation Pipeline in the full Enemy Prefab Creator window. " +
                "Genesis Studio → Player → Enemy Prefab hosts the same mesh-swap workflow.",
                MessageType.Info);
        }

        public static void DrawIdentity(EnemyPrefabCreatorPanelContext ctx)
        {
            EnemyDefinition def = ctx.Definition;
            if (def == null)
                return;

            using var _ = DMCharacterCreatorSharedUi.ScopedCreatorLabelWidth();
            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
            def.enemyId = EditorGUILayout.TextField("Enemy Id", def.enemyId);
            def.displayName = EditorGUILayout.TextField("Display Name", def.displayName);
            def.prefabFileName = EditorGUILayout.TextField("Prefab File Name", def.prefabFileName);
            if (ctx.ShowArchetype)
            {
                def.archetype = (EnemyArchetype)EditorGUILayout.EnumPopup("Archetype", def.archetype);
                if (def.archetype == EnemyArchetype.HumanoidInvector)
                    DrawHumanoidWeapons(def);
            }

            if (ctx.ShowDefinitionAssetName)
                ctx.DefinitionAssetFileName = EditorGUILayout.TextField("Definition Asset Name", ctx.DefinitionAssetFileName);
        }

        public static void DrawHumanoidWeapons(EnemyDefinition def)
        {
            if (def == null)
                return;

            using var _ = DMCharacterCreatorSharedUi.ScopedCreatorLabelWidth();
            def.meleeWeaponItem = (ItemData)EditorGUILayout.ObjectField(
                "Melee Weapon Item",
                def.meleeWeaponItem,
                typeof(ItemData),
                false);
            def.rangedWeaponItem = (ItemData)EditorGUILayout.ObjectField(
                "Ranged Weapon Item",
                def.rangedWeaponItem,
                typeof(ItemData),
                false);
            def.preferRangedWeapon = DMCharacterCreatorSharedUi.DrawPropertyToggle("Prefer Ranged", def.preferRangedWeapon);
        }

        /// <summary>Applies an Enemy Kind: body type (hit FX), threat kind and category defaults.</summary>
        public static void ApplyEnemyKind(EnemyDefinition def, Project.Combat.DMEnemyBodyType kind)
        {
            if (def == null)
                return;

            def.bodyType = kind;
            switch (kind)
            {
                case Project.Combat.DMEnemyBodyType.Android:
                    def.surfaceThreatKind = SurfaceThreatKind.Android;
                    def.enemyCategory = EnemyCategory.Hybrid;
                    break;
                case Project.Combat.DMEnemyBodyType.Robot:
                    def.surfaceThreatKind = SurfaceThreatKind.Android;
                    def.enemyCategory = EnemyCategory.Tank;
                    break;
                default:
                    def.surfaceThreatKind = SurfaceThreatKind.Lifeform;
                    def.enemyCategory = EnemyCategory.Grunt;
                    break;
            }
        }

        /// <summary>Enemy Kind + brain/profile overrides + XP. All of it lives on the EnemyDefinition (the authority).</summary>
        public static void DrawKindAndBrain(EnemyDefinition def)
        {
            if (def == null)
                return;

            using var _ = DMCharacterCreatorSharedUi.ScopedCreatorLabelWidth();
            EditorGUILayout.LabelField("Enemy Kind & Brain", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            var kind = (Project.Combat.DMEnemyBodyType)EditorGUILayout.EnumPopup("Enemy Kind (Humanoid / Android / Robot)", def.bodyType);
            if (EditorGUI.EndChangeCheck())
                ApplyEnemyKind(def, kind);

            def.enemyCategory = (EnemyCategory)EditorGUILayout.EnumPopup("Category", def.enemyCategory);
            def.surfaceThreatKind = (SurfaceThreatKind)EditorGUILayout.EnumPopup("Threat Kind", def.surfaceThreatKind);

            def.overrideBrain = DMCharacterCreatorSharedUi.DrawPropertyToggle("Override Brain (else profile default)", def.overrideBrain);
            if (def.overrideBrain)
            {
                def.brainArchetype = (DMEnemyArchetype)EditorGUILayout.EnumPopup("Brain Archetype", def.brainArchetype);
                def.primaryPersonality = (DMEnemyPersonality)EditorGUILayout.EnumPopup("Primary Personality", def.primaryPersonality);
                def.secondaryPersonality = (DMEnemyPersonality)EditorGUILayout.EnumPopup("Secondary Personality", def.secondaryPersonality);
            }

            def.brainProfileOverride = (DM_EnemyBrainProfile)EditorGUILayout.ObjectField(
                "Brain Profile Override", def.brainProfileOverride, typeof(DM_EnemyBrainProfile), false);
            def.engagementProfileOverride = (DM_EnemyEngagementProfile)EditorGUILayout.ObjectField(
                "Engagement Profile Override", def.engagementProfileOverride, typeof(DM_EnemyEngagementProfile), false);
            def.hitMarkProfileOverride = (Project.Combat.DM_EnemyHitMarkProfile)EditorGUILayout.ObjectField(
                "Hit Mark Profile Override", def.hitMarkProfileOverride, typeof(Project.Combat.DM_EnemyHitMarkProfile), false);
            def.xpReward = Mathf.Max(0, EditorGUILayout.IntField("XP Reward", def.xpReward));
        }

        public static void DrawHumanoidAnimatorNote(bool show)
        {
            if (!show)
                return;

            DMCharacterCreatorSharedUi.DrawWrappedHelpBox(
                "Humanoid Invector enemies use the Player_Invector animator controller with the assigned model Avatar. " +
                "Assign Model FBX (Meshy Humanoid), optional melee/ranged ItemData, then Create/Rebuild.",
                MessageType.Info);
        }

        public static void DrawBehaviorPreset(EnemyDefinition def)
        {
            if (def == null)
                return;

            using var _ = DMCharacterCreatorSharedUi.ScopedCreatorLabelWidth();
            EditorGUILayout.LabelField("AI Preset", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            def.behaviorPreset = (EnemyBehaviorPreset)EditorGUILayout.EnumPopup(
                "Behavior Preset",
                def.behaviorPreset);
            if (EditorGUI.EndChangeCheck() && def.behaviorPreset != EnemyBehaviorPreset.Custom)
                def.ApplyBehaviorPreset(def.behaviorPreset);

            if (GUILayout.Button("Apply Preset Values", GUILayout.Height(24f)) &&
                def.behaviorPreset != EnemyBehaviorPreset.Custom)
            {
                def.ApplyBehaviorPreset(def.behaviorPreset);
            }
        }

        public static void DrawMovementAndBehavior(EnemyPrefabCreatorPanelContext ctx)
        {
            EnemyDefinition def = ctx.Definition;
            if (def == null)
                return;

            using var _ = DMCharacterCreatorSharedUi.ScopedCreatorLabelWidth();
            EditorGUILayout.LabelField("Movement & Behavior", EditorStyles.boldLabel);
            def.movementMode = (EnemyMovementMode)EditorGUILayout.EnumPopup("Movement Mode", def.movementMode);
            def.patrolMode = (EnemyPatrolMode)EditorGUILayout.EnumPopup("Patrol Mode", def.patrolMode);
            def.investigateNoise = DMCharacterCreatorSharedUi.DrawPropertyToggle("Investigate Noise", def.investigateNoise);
            def.chasePlayer = DMCharacterCreatorSharedUi.DrawPropertyToggle("Chase Player", def.chasePlayer);
            def.returnToHomeAfterSearch = DMCharacterCreatorSharedUi.DrawPropertyToggle(
                "Return Home After Search",
                def.returnToHomeAfterSearch);
            def.chaseRadius = EditorGUILayout.FloatField("Chase Radius", def.chaseRadius);
            DMCharacterCreatorSharedUi.DrawWrappedHelpBox(
                "Max distance from spawn/home to pursue the player. Beyond this, the enemy gives up and returns home. 0 = unlimited.",
                MessageType.None);

            EnemyMovementMode mode = def.movementMode;
            if (mode == EnemyMovementMode.Wander)
            {
                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Wander Area", EditorStyles.miniBoldLabel);
                def.wanderRadius = EditorGUILayout.FloatField("Wander Radius", def.wanderRadius);
                def.wanderPauseMin = EditorGUILayout.FloatField("Wander Pause Min", def.wanderPauseMin);
                def.wanderPauseMax = EditorGUILayout.FloatField("Wander Pause Max", def.wanderPauseMax);
            }

            if (mode == EnemyMovementMode.Patrol)
            {
                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Patrol Route", EditorStyles.miniBoldLabel);
                ctx.PatrolPathCreator = (PathCreator)EditorGUILayout.ObjectField(
                    new GUIContent(
                        "Path Creator",
                        "Path Creator or Path Creator Variant. Apply writes onto selected scene AI / placed instance; persistent path assets also bake onto selected prefab assets."),
                    ctx.PatrolPathCreator,
                    typeof(PathCreator),
                    true);
                if (GUILayout.Button("Apply Patrol Path To Selection / Prefab", GUILayout.Height(24f)))
                    ctx.ApplyPatrolPath?.Invoke();

                def.patrolPointCount = EditorGUILayout.IntField("Patrol Point Count", def.patrolPointCount);
                def.patrolRadius = EditorGUILayout.FloatField("Patrol Radius", def.patrolRadius);
                def.patrolWaitDuration = EditorGUILayout.FloatField("Patrol Wait Duration", def.patrolWaitDuration);
                DMCharacterCreatorSharedUi.DrawWrappedHelpBox(
                    "Preferred: place Path Creator Variant, edit anchors with Path Creator's native Scene tools only, " +
                    "then assign here. Create/Rebuild with Place in Scene applies the path to the instance. " +
                    "Fallback: circle PatrolPoints when no path is set.",
                    MessageType.Info);
            }

            if (mode == EnemyMovementMode.Stationary)
            {
                DMCharacterCreatorSharedUi.DrawWrappedHelpBox(
                    "Stationary enemies hold position but can still chase, investigate noise, and return home when configured.",
                    MessageType.None);
            }
        }

        public static void DrawLoot(EnemyPrefabCreatorPanelContext ctx)
        {
            EnemyDefinition def = ctx.Definition;
            if (def == null)
                return;

            using var _ = DMCharacterCreatorSharedUi.ScopedCreatorLabelWidth();
            EditorGUILayout.LabelField("Loot", EditorStyles.boldLabel);
            DMCharacterCreatorSharedUi.DrawWrappedHelpBox(
                "Dead enemies pause respawn for the loot delay (or until fully looted). " +
                "Press E on the corpse to open the loot menu. Leave the item pool empty to roll random items from the item registry.",
                MessageType.Info);

            def.enableLoot = DMCharacterCreatorSharedUi.DrawPropertyToggle("Enable Loot", def.enableLoot);
            DMCharacterCreatorSharedUi.DrawIntMinMaxRow("AC Drop", ref def.acDropMin, ref def.acDropMax);
            DMCharacterCreatorSharedUi.DrawIntMinMaxRow(
                "Random Items",
                ref def.randomLootCountMin,
                ref def.randomLootCountMax);
            def.lootRespawnDelay = EditorGUILayout.FloatField("Loot Respawn Delay", def.lootRespawnDelay);
            def.lootInteractRange = EditorGUILayout.FloatField("Loot Interact Range", def.lootInteractRange);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Loot Item Pool (optional)", EditorStyles.miniBoldLabel);
            DrawItemPoolArray(ref def.lootItemPool);

            if (GUILayout.Button("Apply Loot To Existing Prefab", GUILayout.Height(26f)))
                ctx.ApplyLoot?.Invoke();
        }

        public static void DrawHealth(EnemyDefinition def)
        {
            if (def == null)
                return;

            using var _ = DMCharacterCreatorSharedUi.ScopedCreatorLabelWidth();
            EditorGUILayout.LabelField("Health", EditorStyles.boldLabel);
            def.maxHealth = EditorGUILayout.FloatField("Max Health", def.maxHealth);
            def.destroyOnDeath = DMCharacterCreatorSharedUi.DrawPropertyToggle("Destroy On Death", def.destroyOnDeath);
            def.destroyDelay = EditorGUILayout.FloatField("Destroy Delay", def.destroyDelay);
            def.respawnTime = EditorGUILayout.FloatField("Respawn Time", def.respawnTime);
            DMCharacterCreatorSharedUi.DrawWrappedHelpBox(
                "Respawn Time > 0 respawns the enemy at its spawn point after death. Destroy On Death is ignored while respawning is enabled.",
                MessageType.None);
        }

        public static void DrawHealthBar(EnemyDefinition def)
        {
            if (def == null)
                return;

            using var _ = DMCharacterCreatorSharedUi.ScopedCreatorLabelWidth();
            EditorGUILayout.LabelField("Health Bar", EditorStyles.boldLabel);
            def.showFloatingHealthBar = DMCharacterCreatorSharedUi.DrawPropertyToggle(
                "Show Floating Health Bar",
                def.showFloatingHealthBar);
            def.hideHealthBarUntilDamaged = DMCharacterCreatorSharedUi.DrawPropertyToggle(
                "Hide Until Damaged",
                def.hideHealthBarUntilDamaged);
            def.healthBarOffset = DMCharacterCreatorSharedUi.DrawVector3Field("Health Bar Offset", def.healthBarOffset);
        }

        public static void DrawSenses(EnemyDefinition def)
        {
            if (def == null)
                return;

            using var _ = DMCharacterCreatorSharedUi.ScopedCreatorLabelWidth();
            EditorGUILayout.LabelField("Senses", EditorStyles.boldLabel);
            def.visionRange = EditorGUILayout.FloatField("Vision Range", def.visionRange);
            def.visionFov = EditorGUILayout.FloatField("Vision Fov", def.visionFov);
            def.eyeHeight = EditorGUILayout.FloatField("Eye Height", def.eyeHeight);
            def.senseHearingEnabled = DMCharacterCreatorSharedUi.DrawPropertyToggle("Hearing Enabled", def.senseHearingEnabled);
            def.hearingRange = EditorGUILayout.FloatField("Hearing Range", def.hearingRange);
            def.hearingAggroChance = EditorGUILayout.Slider(
                "Hearing Aggro Chance",
                def.hearingAggroChance,
                0f,
                1f);
            def.hearingCooldown = EditorGUILayout.FloatField("Hearing Cooldown", def.hearingCooldown);
            def.aggroOnDamaged = DMCharacterCreatorSharedUi.DrawPropertyToggle("Aggro On Damaged", def.aggroOnDamaged);
            def.aggroOnHeardHit = DMCharacterCreatorSharedUi.DrawPropertyToggle("Aggro On Heard Hit", def.aggroOnHeardHit);
            def.proximityRange = EditorGUILayout.FloatField("Proximity Range", def.proximityRange);
        }

        public static void DrawCombatStats(EnemyDefinition def)
        {
            if (def == null)
                return;

            using var _ = DMCharacterCreatorSharedUi.ScopedCreatorLabelWidth();
            EditorGUILayout.LabelField("Melee Combat", EditorStyles.boldLabel);
            def.attackRange = EditorGUILayout.FloatField("Attack Range", def.attackRange);
            def.attackDamage = EditorGUILayout.FloatField("Attack Damage", def.attackDamage);
            def.attackCooldown = EditorGUILayout.FloatField("Attack Cooldown", def.attackCooldown);
            def.attackWindup = EditorGUILayout.FloatField("Attack Windup", def.attackWindup);
            def.meleeDuration = EditorGUILayout.FloatField("Melee Duration", def.meleeDuration);
            def.unarmedDuration = EditorGUILayout.FloatField("Unarmed Duration", def.unarmedDuration);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Ranged Combat", EditorStyles.boldLabel);
            def.rangedEngageRange = EditorGUILayout.FloatField("Engage Range", def.rangedEngageRange);
            def.rangedAttackCooldown = EditorGUILayout.FloatField("Shot Cooldown", def.rangedAttackCooldown);
            def.rangedDuration = EditorGUILayout.FloatField("Shot Duration", def.rangedDuration);
            def.aimHoldDuration = EditorGUILayout.FloatField("Aim Hold Duration", def.aimHoldDuration);
            def.missRate = EditorGUILayout.Slider("Miss Rate", def.missRate, 0f, 1f);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("AI Timing", EditorStyles.boldLabel);
            def.walkSpeed = EditorGUILayout.FloatField("Walk Speed", def.walkSpeed);
            def.runSpeed = EditorGUILayout.FloatField("Run Speed", def.runSpeed);
            def.turnSpeed = EditorGUILayout.FloatField("Turn Speed", def.turnSpeed);
            def.loseTargetDelay = EditorGUILayout.FloatField("Lose Target Delay", def.loseTargetDelay);
            def.searchDuration = EditorGUILayout.FloatField("Search Duration", def.searchDuration);
            def.searchRadius = EditorGUILayout.FloatField("Search Radius", def.searchRadius);
            def.idleDuration = EditorGUILayout.FloatField("Idle Duration", def.idleDuration);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Collider", EditorStyles.boldLabel);
            def.fitColliderToRenderers = DMCharacterCreatorSharedUi.DrawPropertyToggle(
                "Fit Collider To Renderers",
                def.fitColliderToRenderers);
            def.colliderCenter = DMCharacterCreatorSharedUi.DrawVector3Field("Collider Center", def.colliderCenter);
            def.colliderRadius = EditorGUILayout.FloatField("Collider Radius", def.colliderRadius);
            def.colliderHeight = EditorGUILayout.FloatField("Collider Height", def.colliderHeight);
        }

        public static void DrawSaveDefinitionRow(
            EnemyPrefabCreatorPanelContext ctx,
            Action saveDefinition,
            Action saveDefinitionAndCreatePrefab = null)
        {
            EnsureEnemyIdentityDefaults(ctx.Definition);

            DMCharacterCreatorActionValidation.DrawBlockersHelpBox(
                DMCharacterCreatorActionValidation.CollectEnemySaveBlockers(
                    ctx.Definition,
                    ctx.DefinitionAssetFileName));

            if (saveDefinitionAndCreatePrefab != null)
            {
                if (GUILayout.Button(
                        "Save Definition + Create Prefab + Apply Visual",
                        GUILayout.Height(30f),
                        GUILayout.ExpandWidth(true)))
                {
                    if (!DMCharacterCreatorActionValidation.TryValidateEnemySaveDefinition(
                            ctx.Definition,
                            ctx.DefinitionAssetFileName,
                            out string saveMessage))
                    {
                        DMCharacterCreatorActionValidation.ShowValidationDialog(
                            DMCharacterCreatorActionValidation.EnemyDialogTitle,
                            saveMessage);
                        return;
                    }

                    EnemyHumanoidPrefabCreatorPanelState createState = BuildEnemyCreateValidationState(ctx);
                    bool requireModel = createState != null && createState.humanoidMeshSource != null;
                    if (!DMCharacterCreatorActionValidation.TryValidateEnemyCreatePrefab(
                            createState,
                            requireModel,
                            out string createMessage))
                    {
                        DMCharacterCreatorActionValidation.ShowValidationDialog(
                            DMCharacterCreatorActionValidation.EnemyDialogTitle,
                            "Save checks passed, but Create Prefab cannot run:\n\n" + createMessage);
                        return;
                    }

                    saveDefinitionAndCreatePrefab.Invoke();
                }

                EditorGUILayout.Space(4f);
            }

            DMCharacterCreatorSharedUi.DrawResponsiveButtonRow(
                28f,
                ("Save Definition Asset", () =>
                {
                    EnsureEnemyIdentityDefaults(ctx.Definition);
                    if (!DMCharacterCreatorActionValidation.TryValidateEnemySaveDefinition(
                            ctx.Definition,
                            ctx.DefinitionAssetFileName,
                            out string message))
                    {
                        DMCharacterCreatorActionValidation.ShowValidationDialog(
                            DMCharacterCreatorActionValidation.EnemyDialogTitle,
                            message);
                        return;
                    }

                    saveDefinition?.Invoke();
                }, true),
                ("Ping Output Prefab", () => PingOutputPrefab(ctx.Definition), ctx.Definition != null));
        }

        static EnemyHumanoidPrefabCreatorPanelState BuildEnemyCreateValidationState(EnemyPrefabCreatorPanelContext ctx)
        {
            EnemyDefinition def = ctx?.Definition;
            if (def == null)
                return null;

            return new EnemyHumanoidPrefabCreatorPanelState
            {
                definition = def,
                displayName = def.displayName,
                prefabFileName = def.prefabFileName,
                visualChildName = string.IsNullOrWhiteSpace(def.visualChildName) ? "Visual" : def.visualChildName,
                templatePrefab = def.templatePrefab != null
                    ? def.templatePrefab
                    : EnemyPrefabVisualSetupUtility.LoadDefaultTemplate(),
                humanoidMeshSource = def.lastModelSource
            };
        }

        public static void EnsureEnemyIdentityDefaults(EnemyDefinition definition)
        {
            if (definition == null)
                return;

            if (string.IsNullOrWhiteSpace(definition.enemyId))
            {
                definition.enemyId = EnemyPrefabBuilder.SanitizeFileName(
                    definition.prefabFileName,
                    definition.displayName ?? "new_enemy");
            }

            if (string.IsNullOrWhiteSpace(definition.prefabFileName))
                definition.prefabFileName = EnemyPrefabBuilder.SanitizeFileName(definition.enemyId, "NewEnemy");

            if (string.IsNullOrWhiteSpace(definition.displayName))
                definition.displayName = definition.enemyId.Replace('_', ' ');
        }

        public static void PingOutputPrefab(EnemyDefinition def)
        {
            if (def == null)
                return;

            string prefabPath = EnemyPrefabVisualSetupUtility.ResolveOutputPrefabPath(def.prefabFileName, def.displayName);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                return;

            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
        }

        public static bool SaveDefinitionAsset(ref EnemyDefinition workingDefinition, string definitionAssetFileName)
        {
            EnsureEnemyIdentityDefaults(workingDefinition);

            if (!DMCharacterCreatorActionValidation.TryValidateEnemySaveDefinition(
                    workingDefinition,
                    definitionAssetFileName,
                    out string validationMessage))
            {
                DMCharacterCreatorActionValidation.ShowValidationDialog(
                    DMCharacterCreatorActionValidation.EnemyDialogTitle,
                    validationMessage);
                return false;
            }

            string prefabPath = EnemyPrefabVisualSetupUtility.ResolveOutputPrefabPath(workingDefinition);
            if (!DMCharacterCreatorDefinitionLink.TryEnsureSaved(workingDefinition, out EnemyDefinition saved, out string saveError))
            {
                DMCharacterCreatorActionValidation.ShowValidationDialog(
                    DMCharacterCreatorActionValidation.EnemyDialogTitle,
                    saveError);
                return false;
            }

            workingDefinition = saved;
            string path = AssetDatabase.GetAssetPath(saved);
            AssetDatabase.ImportAsset(path);
            EditorGUIUtility.PingObject(workingDefinition);
            Debug.Log($"Saved enemy definition to {path} (output prefab: {prefabPath})");
            return true;
        }

        public static void ApplyLootToExistingPrefab(EnemyDefinition definition)
        {
            if (definition == null)
                return;

            string prefabPath =
                $"{ProjectAssetPaths.PrefabsCombatEnemies}/{EnemyPrefabBuilder.SanitizeFileName(definition.prefabFileName, definition.displayName)}.prefab";

            GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabRoot == null)
            {
                EditorUtility.DisplayDialog(
                    "Enemy Prefab Creator",
                    $"Prefab not found at {prefabPath}. Create the prefab first.",
                    "OK");
                return;
            }

            GameObject instance = PrefabUtility.LoadPrefabContents(prefabPath);
            if (instance == null)
            {
                EditorUtility.DisplayDialog("Enemy Prefab Creator", "Could not open prefab for editing.", "OK");
                return;
            }

            EnemyPrefabBuilder.ApplyLootToPrefab(instance, definition);
            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            PrefabUtility.UnloadPrefabContents(instance);
            AssetDatabase.SaveAssets();
            Debug.Log($"Applied loot setup to {prefabPath}");
        }

        static void DrawItemPoolArray(ref ItemData[] items)
        {
            int count = EditorGUILayout.IntField("Pool Count", items?.Length ?? 0);
            if (count < 0)
                count = 0;

            if (items == null || items.Length != count)
                Array.Resize(ref items, count);

            for (int i = 0; i < count; i++)
            {
                items[i] = (ItemData)EditorGUILayout.ObjectField(
                    $"  Item {i + 1}",
                    items[i],
                    typeof(ItemData),
                    false);
            }
        }
    }
}
#endif
