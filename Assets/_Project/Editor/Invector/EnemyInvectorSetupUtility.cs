#if UNITY_EDITOR
using System.IO;
using Invector;
using Invector.vCharacterController;
using Invector.vMelee;
using Project.AI;
using Project.AI.Invector;
using Project.Combat;
using Project.Data;
using Project.EditorTools;
using Project.Player.Invector;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Invector
{
    public static class EnemyInvectorSetupUtility
    {
        private const string SourcePlayerPrefabPath = "Assets/_Project/Prefabs/Players/Player_Invector.prefab";
        private const string HumanoidBasePrefabPath = ProjectAssetPaths.HumanoidEnemyPrefab;
        private const string HumanoidEnemyDefinitionPath = "Assets/_Project/Data/Enemies/Humanoid_Enemy.asset";
        private const string DefaultMeleeItemPath = ProjectAssetPaths.ItemsMelee + "/weap2_sword.asset";
        private const string DefaultRangedItemPath = ProjectAssetPaths.ItemsRanged + "/sci_fi_pistol.asset";

        [MenuItem(SurvivalPioneerEditorMenus.RepairAllHumanoidCombatPrefabs, false, 130)]
        public static void RepairAllHumanoidCombatPrefabs()
        {
            EnsureHumanoidEnemyDefinitionAsset();

            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { ProjectAssetPaths.PrefabsCombat });
            int repaired = 0;
            for (int i = 0; i < prefabGuids.Length; i++)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefabAsset == null || prefabAsset.GetComponent<EnemyInvectorBootstrap>() == null)
                    continue;

                if (RepairGameplayAtPath(prefabPath))
                    repaired++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Repaired spawn-ready gameplay on {repaired} humanoid combat prefab(s).");
        }

        public static GameObject BuildHumanoidEnemyPrefab(
            EnemyDefinition definition,
            GameObject visualSource,
            string outputPrefabPath)
        {
            if (definition == null)
                return null;

            EnsureHumanoidBaseExists();
            GameObject root = PrefabUtility.LoadPrefabContents(HumanoidBasePrefabPath);
            if (root == null)
                return null;

            try
            {
                root.name = Path.GetFileNameWithoutExtension(outputPrefabPath);
                if (visualSource != null)
                    AttachVisualModel(root, visualSource, definition.visualChildName);

                RepairHumanoidRoot(root, definition);

                CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsCombat);
                return PrefabUtility.SaveAsPrefabAsset(root, outputPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        public static bool RebuildHumanoidEnemyAtPath(
            string prefabPath,
            EnemyDefinition definition,
            GameObject visualSource = null)
        {
            if (string.IsNullOrEmpty(prefabPath) || definition == null)
                return false;

            if (!File.Exists(prefabPath))
                return BuildHumanoidEnemyPrefab(definition, visualSource, prefabPath) != null;

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null)
                return false;

            try
            {
                if (visualSource != null)
                    AttachVisualModel(root, visualSource, definition.visualChildName);

                RepairHumanoidRoot(root, definition);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        public static void RepairHumanoidRoot(GameObject root, EnemyDefinition definition)
        {
            if (root == null || definition == null)
                return;

            EnemyInvectorComponentStripper.StripEditor(root);
            EnemyPrefabBuilder.ApplyGameplayComponents(root, definition);

            EnemyAnimationController legacyAnim = root.GetComponent<EnemyAnimationController>();
            if (legacyAnim != null)
                Object.DestroyImmediate(legacyAnim, true);

            EnsureHumanoidEnemyComponents(root);
            ConfigureHumanoidLoadout(root, definition);
            WireBootstrapDefinition(root, definition);
            EnemyInvectorWeaponHolderRebind.RebindToAnimatorBones(root);
            EnemyInvectorBodySnapSetupEditor.EnsurePresentEditor(root);
            RepairWeaponSlotVisuals(root, definition.meleeWeaponItem, definition.rangedWeaponItem);
            EnemyInvectorBodySnapSetupEditor.ConfigureEditor(root);
            EnemyInvectorTargetLayers.Apply(root);
            EnemyInvectorRagdollAudit.Repair(root);
            ApplyHumanoidPhysics(root, definition);
            AddDamageReceiversToRoot(root);
            EnemyNavMeshStripUtility.StripNavMeshFromRoot(root);
            RepairHumanoidLocomotionAnimator(root);
            TuneMeshyMeleeEngagement(root);
            ExtendMeshyMeleeHitBoxes(root);
        }

        /// <summary>
        /// Keeps Creator / repair path from shipping Meshy humanoids with CullUpdateTransforms,
        /// serialized isDead, applyRootMotion, or nested Animators that cause chase glides.
        /// </summary>
        public static void RepairHumanoidLocomotionAnimator(GameObject root)
        {
            if (root == null)
                return;

            ClearSerializedInvectorDeadFlag(root);

            Animator rootAnimator = root.GetComponent<Animator>();
            if (rootAnimator == null)
                rootAnimator = root.AddComponent<Animator>();

            Animator[] nested = root.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < nested.Length; i++)
            {
                if (nested[i] != null && nested[i] != rootAnimator)
                    Object.DestroyImmediate(nested[i], true);
            }

            rootAnimator.applyRootMotion = false;
            rootAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            // AlwaysAnimate avoids intermittent chase glides when CullUpdateTransforms skips bone writes.
            // Do NOT SampleAnimation Idle/attack clips here — that froze mid-pose and floated weapons.
            RuntimeAnimatorController savedController = rootAnimator.runtimeAnimatorController;
            rootAnimator.runtimeAnimatorController = null;
            rootAnimator.enabled = true;
            if (rootAnimator.avatar != null && rootAnimator.avatar.isValid)
            {
                rootAnimator.Rebind();
                rootAnimator.Update(0f);
            }

            // Keep controller assigned for play mode, but leave Animator disabled in edit mode so
            // Scene view does not sample ShooterMelee states. Bootstrap re-enables at runtime.
            rootAnimator.runtimeAnimatorController = savedController;
            rootAnimator.writeDefaultValuesOnDisable = true;
            if (!Application.isPlaying)
                rootAnimator.enabled = false;

            EnableVisualUpdateWhenOffscreen(root);

            vThirdPersonController controller = root.GetComponent<vThirdPersonController>();
            if (controller != null)
            {
                SerializedObject so = new SerializedObject(controller);
                SerializedProperty disableAnim = so.FindProperty("disableAnimations");
                SerializedProperty useRoot = so.FindProperty("useRootMotion");
                if (disableAnim != null)
                    disableAnim.boolValue = false;
                if (useRoot != null)
                    useRoot.boolValue = false;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        /// <summary>
        /// Closes combat ring so Meshy-proportion arms/weapons connect; pairs with hitbox extend.
        /// </summary>
        public static void TuneMeshyMeleeEngagement(GameObject root)
        {
            if (root == null)
                return;

            EnemyAiController ai = root.GetComponent<EnemyAiController>();
            if (ai != null)
            {
                SerializedObject so = new SerializedObject(ai);
                SetFloatIfPresent(so, "minCombatSeparation", 0.95f);
                SetFloatIfPresent(so, "attackStandoffFraction", 0.62f);
                SetFloatIfPresent(so, "playerStandoffBonus", 0.15f);
                SetFloatIfPresent(so, "stopDistance", 0.35f);
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EnemyCombat combat = root.GetComponent<EnemyCombat>();
            if (combat != null)
            {
                SerializedObject so = new SerializedObject(combat);
                SerializedProperty attackRange = so.FindProperty("attackRange");
                if (attackRange != null && attackRange.floatValue < 2f)
                    attackRange.floatValue = 2f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        /// <summary>
        /// Extends Drawn_* melee vHitBox volumes to compensate for Meshy arm length vs VBOT authoring.
        /// Scales local box size against lossyScale so world reach is ~1.1m along the blade axis.
        /// </summary>
        public static void ExtendMeshyMeleeHitBoxes(GameObject root)
        {
            if (root == null)
                return;

            const float targetWorldReach = 1.15f;
            const float targetWorldThickness = 0.42f;
            const float targetWorldWidth = 0.55f;

            vHitBox[] hitBoxes = root.GetComponentsInChildren<vHitBox>(true);
            int extended = 0;
            for (int i = 0; i < hitBoxes.Length; i++)
            {
                vHitBox hitBox = hitBoxes[i];
                if (hitBox == null || !IsDrawnMeleeHitBox(hitBox.transform))
                    continue;

                BoxCollider box = hitBox.GetComponent<BoxCollider>();
                if (box == null)
                    continue;

                Vector3 lossy = hitBox.transform.lossyScale;
                float sx = Mathf.Max(0.001f, Mathf.Abs(lossy.x));
                float sy = Mathf.Max(0.001f, Mathf.Abs(lossy.y));
                float sz = Mathf.Max(0.001f, Mathf.Abs(lossy.z));

                // Blade/reach follows the parent scale axis with the largest magnitude (Meshy hand
                // chains crush X/Y and leave Z ≈ 1.2). Do not use current box local-max — a prior
                // bad extend can make the wrong axis look longest in world space.
                float[] scale = { sx, sy, sz };
                int reachAxis = 0;
                if (scale[1] > scale[reachAxis]) reachAxis = 1;
                if (scale[2] > scale[reachAxis]) reachAxis = 2;

                int widthAxis = (reachAxis + 1) % 3;
                int thickAxis = (reachAxis + 2) % 3;
                if (scale[thickAxis] < scale[widthAxis])
                {
                    int swap = widthAxis;
                    widthAxis = thickAxis;
                    thickAxis = swap;
                }

                Vector3 size = box.size;
                float[] local = { size.x, size.y, size.z };
                float[] targetWorld = { targetWorldWidth, targetWorldThickness, targetWorldReach };

                local[reachAxis] = (targetWorld[2] / scale[reachAxis]) *
                                   Mathf.Sign(local[reachAxis] == 0f ? 1f : local[reachAxis]);
                local[widthAxis] = (targetWorld[0] / scale[widthAxis]) *
                                   Mathf.Sign(local[widthAxis] == 0f ? 1f : local[widthAxis]);
                local[thickAxis] = (targetWorld[1] / scale[thickAxis]) *
                                   Mathf.Sign(local[thickAxis] == 0f ? 1f : local[thickAxis]);

                box.size = new Vector3(local[0], local[1], local[2]);

                // Nudge center along reach axis so the volume extends past the tip, not into the handle.
                Vector3 center = box.center;
                float reachExtent = Mathf.Abs(local[reachAxis]) * 0.5f;
                float[] c = { center.x, center.y, center.z };
                float tipSign = local[reachAxis] >= 0f ? 1f : -1f;
                if (Mathf.Abs(c[reachAxis]) > 0.001f)
                    tipSign = Mathf.Sign(c[reachAxis]);
                c[reachAxis] = tipSign * reachExtent * 0.35f;
                box.center = new Vector3(c[0], c[1], c[2]);

                extended++;
            }

            // Match Invector weapon attack distance to the closer engagement band.
            vMeleeWeapon[] weapons = root.GetComponentsInChildren<vMeleeWeapon>(true);
            for (int i = 0; i < weapons.Length; i++)
            {
                if (weapons[i] == null || !IsDrawnMeleeHitBox(weapons[i].transform))
                    continue;

                SerializedObject so = new SerializedObject(weapons[i]);
                SerializedProperty dist = so.FindProperty("distanceToAttack");
                if (dist != null && dist.floatValue < 1.35f)
                {
                    dist.floatValue = 1.35f;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            if (extended > 0)
                Debug.Log($"[EnemyInvectorSetup] Extended {extended} Meshy melee hitbox(es) on '{root.name}'.", root);
        }

        private static bool IsDrawnMeleeHitBox(Transform node)
        {
            Transform cur = node;
            while (cur != null)
            {
                string name = cur.name;
                if (name.StartsWith("Drawn_", System.StringComparison.Ordinal) &&
                    (name.IndexOf("Axe", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                     name.IndexOf("Sword", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                     name.IndexOf("Spear", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                     name.IndexOf("2_Hander", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                     name.IndexOf("weap", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                     name.IndexOf("melee", System.StringComparison.OrdinalIgnoreCase) >= 0))
                    return true;

                if (name.Equals("meleeHandler", System.StringComparison.OrdinalIgnoreCase) &&
                    node.GetComponentInParent<vMeleeWeapon>() != null)
                {
                    // Accept any Drawn_ under meleeHandler.
                    Transform p = node;
                    while (p != null)
                    {
                        if (p.name.StartsWith("Drawn_", System.StringComparison.Ordinal))
                            return true;
                        p = p.parent;
                    }
                }

                cur = cur.parent;
            }

            return false;
        }

        private static void SetFloatIfPresent(SerializedObject so, string propertyName, float value)
        {
            if (so == null)
                return;

            SerializedProperty prop = so.FindProperty(propertyName);
            if (prop != null && prop.propertyType == SerializedPropertyType.Float)
                prop.floatValue = value;
        }

        private static void EnableVisualUpdateWhenOffscreen(GameObject rootOrVisual)
        {
            if (rootOrVisual == null)
                return;

            SkinnedMeshRenderer[] renderers = rootOrVisual.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                SkinnedMeshRenderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled)
                    continue;

                renderer.updateWhenOffscreen = true;
            }
        }

        private static void ClearSerializedInvectorDeadFlag(GameObject root)
        {
            vThirdPersonController controller = root != null
                ? root.GetComponent<vThirdPersonController>()
                : null;
            if (controller == null)
                return;

            SerializedObject so = new SerializedObject(controller);
            SerializedProperty isDead = so.FindProperty("_isDead");
            if (isDead != null && isDead.boolValue)
            {
                isDead.boolValue = false;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            if (controller is global::Invector.vHealthController health)
            {
                health.isDead = false;
                health.ResetHealth();
                health.isImmortal = true;
            }
        }

        public static void ApplyHumanoidToExistingRoot(GameObject root, EnemyDefinition definition, GameObject visualSource)
        {
            if (root == null || definition == null)
                return;

            if (visualSource != null)
                AttachVisualModel(root, visualSource, definition.visualChildName);

            RepairHumanoidRoot(root, definition);
        }

        public static bool SwapVisualOnPrefab(string prefabPath, GameObject newVisualSource, EnemyDefinition definition)
        {
            return RebuildHumanoidEnemyAtPath(prefabPath, definition, newVisualSource);
        }

        private static bool RepairGameplayAtPath(string prefabPath)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null)
                return false;

            try
            {
                EnemyDefinition definition = ResolveDefinitionForPrefab(prefabPath);
                RepairHumanoidRoot(root, definition);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        public static EnemyDefinition ResolveDefinitionForPrefab(string prefabPath)
        {
            string fileName = Path.GetFileNameWithoutExtension(prefabPath);
            if (fileName == "HumanoidEnemy_Invector")
                return EnsureHumanoidEnemyDefinitionAsset();

            if (fileName == "The_Evil_One")
            {
                EnemyDefinition evilOne = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(
                    $"{ProjectAssetPaths.EnemiesData}/The_Evil_One.asset");
                if (evilOne != null)
                    return evilOne;
            }

            string[] guids = AssetDatabase.FindAssets("t:EnemyDefinition", new[] { ProjectAssetPaths.EnemiesData });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                EnemyDefinition definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
                if (definition == null)
                    continue;

                if (string.Equals(definition.prefabFileName, fileName, System.StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(definition.displayName.Replace(' ', '_'), fileName, System.StringComparison.OrdinalIgnoreCase))
                    return definition;
            }

            return EnsureHumanoidEnemyDefinitionAsset();
        }

        public static EnemyDefinition EnsureHumanoidEnemyDefinitionAsset()
        {
            EnemyDefinition existing = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(HumanoidEnemyDefinitionPath);
            if (existing != null)
                return existing;

            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.EnemiesData);
            EnemyDefinition definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            definition.enemyId = "humanoid_enemy";
            definition.displayName = "Humanoid Enemy";
            definition.prefabFileName = "HumanoidEnemy_Invector";
            definition.archetype = EnemyArchetype.HumanoidInvector;
            definition.movementMode = EnemyMovementMode.Wander;
            definition.visualChildName = "Visual";
            definition.meleeWeaponItem = AssetDatabase.LoadAssetAtPath<ItemData>(DefaultMeleeItemPath);
            definition.rangedWeaponItem = AssetDatabase.LoadAssetAtPath<ItemData>(DefaultRangedItemPath);
            definition.preferRangedWeapon = false;

            AssetDatabase.CreateAsset(definition, HumanoidEnemyDefinitionPath);
            AssetDatabase.SaveAssets();
            return definition;
        }

        private static void WireBootstrapDefinition(GameObject root, EnemyDefinition definition)
        {
            EnemyInvectorBootstrap bootstrap = root.GetComponent<EnemyInvectorBootstrap>();
            if (bootstrap == null)
                return;

            SerializedObject serialized = new SerializedObject(bootstrap);
            SerializedProperty definitionProperty = serialized.FindProperty("enemyDefinition");
            if (definitionProperty != null)
                definitionProperty.objectReferenceValue = definition;

            SerializedProperty infiniteAmmo = serialized.FindProperty("infiniteAmmo");
            if (infiniteAmmo != null)
                infiniteAmmo.boolValue = true;

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject BuildHumanoidBaseRoot(string rootName)
        {
            if (!File.Exists(SourcePlayerPrefabPath))
            {
                Debug.LogError($"Missing {SourcePlayerPrefabPath}. Run Build Player_Invector Prefab first.");
                return null;
            }

            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsCombat);
            GameObject root = PrefabUtility.LoadPrefabContents(SourcePlayerPrefabPath);
            try
            {
                root.name = rootName;
                PioneerInvectorPlayerSetupUtility.RefreshPreloadedWeaponSlotsOn(root);

                EnemyDefinition defaultDefinition = EnsureHumanoidEnemyDefinitionAsset();
                RepairHumanoidRoot(root, defaultDefinition);

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, HumanoidBasePrefabPath);
                AssetDatabase.SaveAssets();
                return prefab;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void EnsureHumanoidBaseExists()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(HumanoidBasePrefabPath) == null)
                BuildHumanoidBaseRoot("HumanoidEnemy_Invector");
        }

        private static void EnsureHumanoidEnemyComponents(GameObject root)
        {
            if (root.GetComponent<EnemyInvectorBootstrap>() == null)
                root.AddComponent<EnemyInvectorBootstrap>();
            if (root.GetComponent<EnemyInvectorLoadoutBridge>() == null)
                root.AddComponent<EnemyInvectorLoadoutBridge>();
            if (root.GetComponent<EnemyInvectorMotorBridge>() == null)
                root.AddComponent<EnemyInvectorMotorBridge>();
            if (root.GetComponent<EnemyInvectorCombatBridge>() == null)
                root.AddComponent<EnemyInvectorCombatBridge>();
            if (root.GetComponent<EnemyInvectorOutgoingDamageBridge>() == null)
                root.AddComponent<EnemyInvectorOutgoingDamageBridge>();
            if (root.GetComponent<EnemyTerrainRescue>() == null)
                root.AddComponent<EnemyTerrainRescue>();

            EnemyInvectorRagdollSetup.EnsurePresent(root);
            if (root.GetComponent<EnemyInvectorRagdollBridge>() == null)
                root.AddComponent<EnemyInvectorRagdollBridge>();
            if (root.GetComponent<EnemyInvectorDeathPresenter>() == null)
                root.AddComponent<EnemyInvectorDeathPresenter>();
            if (root.GetComponent<EnemyInvectorPhysicsCache>() == null)
                root.AddComponent<EnemyInvectorPhysicsCache>();
            if (root.GetComponent<HumanoidPerformanceController>() == null)
                root.AddComponent<HumanoidPerformanceController>();
            EnemyInvectorBodySnapSetup.ApplyRuntime(root);

            root.tag = "Enemy";
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0)
                root.layer = enemyLayer;
        }

        private static void ConfigureHumanoidLoadout(GameObject root, EnemyDefinition definition)
        {
            EnemyInvectorLoadoutBridge loadout = root.GetComponent<EnemyInvectorLoadoutBridge>();
            if (loadout == null || definition == null)
                return;

            SerializedObject serialized = new SerializedObject(loadout);
            SerializedProperty melee = serialized.FindProperty("meleeWeaponItem");
            SerializedProperty ranged = serialized.FindProperty("rangedWeaponItem");
            SerializedProperty prefer = serialized.FindProperty("preferRangedAtRange");
            SerializedProperty startMelee = serialized.FindProperty("startWithMeleeWeapon");

            if (melee != null)
                melee.objectReferenceValue = definition.meleeWeaponItem;
            if (ranged != null)
                ranged.objectReferenceValue = definition.rangedWeaponItem;
            if (prefer != null)
                prefer.boolValue = definition.preferRangedWeapon;
            if (startMelee != null)
                startMelee.boolValue = !definition.preferRangedWeapon;

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void AddDamageReceiversToRoot(GameObject root)
        {
            if (root == null)
                return;

            Collider rootCollider = root.GetComponent<Collider>();
            if (rootCollider == null || rootCollider.isTrigger)
                return;

            if (root.GetComponent<PioneerInvectorDamageReceiver>() == null)
                root.AddComponent<PioneerInvectorDamageReceiver>();
        }

        private static void ApplyHumanoidPhysics(GameObject root, EnemyDefinition definition)
        {
            if (root == null)
                return;

            float radius = definition != null ? definition.colliderRadius : 0.45f;
            float height = definition != null ? definition.colliderHeight : 2f;
            Vector3 center = definition != null ? definition.colliderCenter : new Vector3(0f, 1f, 0f);
            bool fit = definition == null || definition.fitColliderToRenderers;
            EnemyInvectorHitSetup.Apply(root, radius, height, center, fit);
            EnemyInvectorHitSetup.RestoreRagdollPhysicsLayers(root);
        }

        /// <summary>
        /// Attaches a Meshy / custom character FBX (or prefab) onto a humanoid Invector enemy root.
        /// Valid humanoid avatars replace the root Animator avatar and hide the stock VBOT body;
        /// generic meshes nest as an overlay under <paramref name="visualChildName"/>.
        /// </summary>
        public static void AttachVisualModel(GameObject root, GameObject visualSource, string visualChildName)
        {
            if (root == null || visualSource == null)
                return;

            string childName = string.IsNullOrWhiteSpace(visualChildName) ? "Visual" : visualChildName;
            ClearPreviousCustomVisual(root, childName);

            GameObject visualInstance = InstantiateVisualSource(visualSource);
            if (visualInstance == null)
                return;

            visualInstance.name = childName;
            visualInstance.transform.SetParent(root.transform, false);
            visualInstance.transform.localPosition = Vector3.zero;
            visualInstance.transform.localRotation = Quaternion.identity;
            visualInstance.transform.localScale = Vector3.one;

            EnemyModelAvatarUtility.PrepareModelInstance(visualInstance, preferHumanoidAvatar: true);
            EnemyModelAvatarUtility.ModelInspection inspection = EnemyModelAvatarUtility.Inspect(visualInstance);

            if (inspection.IsHumanoidAvatar && inspection.IsAvatarValid && inspection.Avatar != null)
            {
                IntegrateHumanoidVisual(root, visualInstance, inspection.Avatar);
                Debug.Log(
                    $"[EnemyInvectorSetup] Humanoid visual '{visualSource.name}' bound to root Animator " +
                    $"(avatar={inspection.Avatar.name}, {inspection.Summary}).",
                    root);
            }
            else
            {
                HideStockBodyMeshes(root);
                Debug.LogWarning(
                    $"[EnemyInvectorSetup] Visual '{visualSource.name}' is not a valid Humanoid avatar " +
                    $"({inspection.Summary}). Nested under '{childName}' and stock VBOT meshes were hidden. " +
                    inspection.Recommendation,
                    root);
            }
        }

        private static GameObject InstantiateVisualSource(GameObject visualSource)
        {
            GameObject visualInstance = PrefabUtility.InstantiatePrefab(visualSource) as GameObject;
            if (visualInstance != null)
            {
                if (PrefabUtility.IsPartOfPrefabInstance(visualInstance))
                    PrefabUtility.UnpackPrefabInstance(
                        visualInstance,
                        PrefabUnpackMode.Completely,
                        InteractionMode.AutomatedAction);
                return visualInstance;
            }

            return Object.Instantiate(visualSource);
        }

        private static void ClearPreviousCustomVisual(GameObject root, string childName)
        {
            Transform existing = root.transform.Find(childName);
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            // Prior Meshy flatten left Armature/char1 on the root — remove only when stock 3D Model is present.
            Transform stockModel = root.transform.Find("3D Model");
            if (stockModel == null)
                return;

            Transform leftoverArmature = root.transform.Find("Armature");
            if (leftoverArmature != null)
                Object.DestroyImmediate(leftoverArmature.gameObject);

            Transform leftoverMesh = root.transform.Find("char1");
            if (leftoverMesh != null)
                Object.DestroyImmediate(leftoverMesh.gameObject);
        }

        private static void IntegrateHumanoidVisual(GameObject root, GameObject visualInstance, Avatar avatar)
        {
            HideStockBodyMeshes(root);

            Animator rootAnimator = root.GetComponent<Animator>();
            if (rootAnimator == null)
                rootAnimator = root.AddComponent<Animator>();

            RuntimeAnimatorController keepController = rootAnimator.runtimeAnimatorController;

            // Strip nested Animators so only the root drives the humanoid.
            Animator[] nestedAnimators = visualInstance.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < nestedAnimators.Length; i++)
            {
                if (nestedAnimators[i] != null && nestedAnimators[i].gameObject != root)
                    Object.DestroyImmediate(nestedAnimators[i]);
            }

            rootAnimator.avatar = avatar;
            if (keepController != null)
                rootAnimator.runtimeAnimatorController = keepController;
            rootAnimator.applyRootMotion = false;
            rootAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            rootAnimator.Rebind();
            rootAnimator.Update(0f);

            EnableVisualUpdateWhenOffscreen(visualInstance);
            ClearSerializedInvectorDeadFlag(root);

            if (rootAnimator.GetBoneTransform(HumanBodyBones.Hips) == null)
            {
                Debug.LogWarning(
                    "[EnemyInvectorSetup] Root Animator did not bind Hips after avatar swap. " +
                    "Check FBX Humanoid mapping. Visual remains nested; stock body stays hidden.",
                    root);
            }
            else
            {
                // Holders / Drawn_ slots were authored on VBOT bones — move them onto Meshy bones.
                EnemyInvectorWeaponHolderRebind.RebindToAnimatorBones(root);
                EnemyInvectorBodySnapSetup.ApplyRuntime(root);
            }

            // Remount ItemData visuals after VBOT hide so GreatSword leftovers stay component-disabled.
            EnemyInvectorLoadoutBridge loadout = root.GetComponent<EnemyInvectorLoadoutBridge>();
            ItemData preferredMelee = null;
            ItemData preferredRanged = null;
            if (loadout != null)
            {
                SerializedObject so = new SerializedObject(loadout);
                preferredMelee = so.FindProperty("meleeWeaponItem")?.objectReferenceValue as ItemData;
                preferredRanged = so.FindProperty("rangedWeaponItem")?.objectReferenceValue as ItemData;
            }

            RepairWeaponSlotVisuals(root, preferredMelee, preferredRanged);
        }

        /// <summary>
        /// Syncs every Drawn_/Holstered_ slot to ItemData held/invector visuals and arms the preferred
        /// melee (or ranged) Drawn_ slot. Prevents VBOT GreatSword/handgun leftovers from showing.
        /// </summary>
        public static void RepairWeaponSlotVisuals(
            GameObject root,
            ItemData preferredMelee = null,
            ItemData preferredRanged = null)
        {
            if (root == null)
                return;

            string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { ProjectAssetPaths.ItemsData });
            int synced = 0;
            for (int i = 0; i < guids.Length; i++)
            {
                ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (item == null)
                    continue;

                bool isMelee = item.itemType == ItemType.MeleeWeapon;
                bool isRanged = item.IsRangedWeapon;
                if (!isMelee && !isRanged)
                    continue;

                GameObject drawn = PioneerInvectorWeaponBridge.FindPreloadedDrawnSlot(root.transform, item);
                GameObject holstered = PioneerInvectorWeaponBridge.FindPreloadedHolsteredSlot(root.transform, item);
                if (drawn == null && holstered == null)
                    continue;

                GameObject invectorPrefab = isMelee
                    ? PioneerInvectorPlayerSetupUtility.ResolveMeleeWeaponPrefab(item)
                    : PioneerInvectorPlayerSetupUtility.ResolveRangedWeaponPrefab(item);

                if (drawn != null)
                {
                    PioneerInvectorWeaponBridge.SyncPreloadedSlotVisuals(drawn, item, invectorPrefab, holstered: false);
                    drawn.SetActive(false);
                    synced++;
                }

                if (holstered != null)
                {
                    PioneerInvectorWeaponBridge.SyncPreloadedSlotVisuals(holstered, item, invectorPrefab, holstered: true);
                    holstered.SetActive(false);
                    synced++;
                }
            }

            ItemData preferred = preferredMelee != null ? preferredMelee : preferredRanged;
            if (preferred != null)
            {
                GameObject armed = PioneerInvectorWeaponBridge.FindPreloadedDrawnSlot(root.transform, preferred);
                if (armed != null)
                {
                    GameObject invectorPrefab = preferred.itemType == ItemType.MeleeWeapon
                        ? PioneerInvectorPlayerSetupUtility.ResolveMeleeWeaponPrefab(preferred)
                        : PioneerInvectorPlayerSetupUtility.ResolveRangedWeaponPrefab(preferred);
                    PioneerInvectorWeaponBridge.SyncPreloadedSlotVisuals(armed, preferred, invectorPrefab, holstered: false);
                    armed.SetActive(true);
                }
            }

            Debug.Log($"[EnemyInvectorSetup] Synced weapon slot visuals on '{root.name}' ({synced} slot(s)).", root);
        }

        private static void HideStockBodyMeshes(GameObject root)
        {
            Transform stockModel = root.transform.Find("3D Model");
            if (stockModel == null)
                return;

            // Keep the GameObject for weapon-holder hierarchy references, but hide body renderers.
            // Weapons live under VBOT_* bones before rebind — never treat "VBOT_" path alone as body.
            SkinnedMeshRenderer[] skinned = stockModel.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < skinned.Length; i++)
            {
                if (skinned[i] == null || IsWeaponVisualNode(skinned[i].transform))
                    continue;
                skinned[i].enabled = false;
                skinned[i].gameObject.SetActive(false);
            }

            MeshRenderer[] meshes = stockModel.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < meshes.Length; i++)
            {
                if (meshes[i] == null || IsWeaponVisualNode(meshes[i].transform))
                    continue;

                // Only hide LOD / VBOT body meshes — never Drawn_/Holstered_/weapon meshes.
                string path = AnimationUtility.CalculateTransformPath(meshes[i].transform, stockModel);
                if (path.IndexOf("Mesh_LOD", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    path.IndexOf("VBOT_", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    meshes[i].enabled = false;
                    meshes[i].gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Weapon slots are authored under VBOT bones, so stock-body hide must skip them
        /// (and PioneerVisual mounts / melee hit volumes) before holders rebind to Meshy.
        /// </summary>
        private static bool IsWeaponVisualNode(Transform node)
        {
            Transform cur = node;
            while (cur != null)
            {
                string name = cur.name;
                if (name.Equals("3D Model", System.StringComparison.Ordinal))
                    return false;

                if (name.StartsWith("Drawn_", System.StringComparison.Ordinal) ||
                    name.StartsWith("Holstered_", System.StringComparison.Ordinal) ||
                    name.StartsWith("PioneerVisual_", System.StringComparison.Ordinal) ||
                    name.Equals("WeaponHolders", System.StringComparison.Ordinal) ||
                    name.Equals("RightHandlers", System.StringComparison.Ordinal) ||
                    name.Equals("LeftHandlers", System.StringComparison.Ordinal) ||
                    name.Equals("HandgunHolder", System.StringComparison.Ordinal) ||
                    name.Equals("RifleHolder", System.StringComparison.Ordinal) ||
                    name.Equals("meleeHandler", System.StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("defaultHandler", System.StringComparison.OrdinalIgnoreCase))
                    return true;

                if (cur.GetComponent<global::Invector.vMelee.vMeleeWeapon>() != null ||
                    cur.GetComponent<global::Invector.vShooter.vShooterWeapon>() != null ||
                    cur.GetComponent<global::Invector.vMelee.vHitBox>() != null)
                    return true;

                cur = cur.parent;
            }

            return false;
        }

        public static GameObject ExtractVisualSource(GameObject enemyPrefab, string visualChildName)
        {
            if (enemyPrefab == null)
                return null;

            string childName = string.IsNullOrWhiteSpace(visualChildName) ? "Visual" : visualChildName;
            Transform visual = enemyPrefab.transform.Find(childName);
            if (visual != null)
                return visual.gameObject;

            if (enemyPrefab.transform.childCount > 0)
                return enemyPrefab.transform.GetChild(0).gameObject;

            return enemyPrefab;
        }

        public static string SuggestVisualChildName(GameObject model)
        {
            if (model == null)
                return "Visual";

            string modelName = model.name;
            if (modelName == "scene" || modelName == "Visual")
                return modelName;

            return "Visual";
        }
    }
}
#endif
