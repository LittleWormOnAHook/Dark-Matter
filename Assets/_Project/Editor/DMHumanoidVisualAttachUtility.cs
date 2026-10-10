#if UNITY_EDITOR
using System.Collections.Generic;
using Project.AI.Invector;
using Project.EditorTools.Invector;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    public enum DMHumanoidVisualTarget
    {
        Player,
        Enemy
    }

    /// <summary>
    /// Shared Meshy/custom humanoid visual attach for player and enemy Invector prefabs.
    /// </summary>
    public static class DMHumanoidVisualAttachUtility
    {
        public struct AttachResult
        {
            public bool AppliedHumanoidAvatar;
            public bool HipsBound;
            public int BonesRenamed;
            public Avatar RebuiltAvatar;
            public EnemyModelAvatarUtility.ModelInspection Inspection;
        }

        public static AttachResult AttachVisualModel(
            GameObject root,
            GameObject visualSource,
            string visualChildName,
            DMHumanoidVisualTarget target,
            GameObject templateForBoneNames = null,
            string avatarPersistPath = null)
        {
            AttachResult result = default;
            if (root == null || visualSource == null)
                return result;

            string childName = string.IsNullOrWhiteSpace(visualChildName) ? "Visual" : visualChildName;
            ClearPreviousCustomVisual(root, childName);

            GameObject instantiateSource = EnemyModelAvatarUtility.ResolvePreferredVisualModel(visualSource);
            if (instantiateSource == null)
                instantiateSource = visualSource;
            if (EnemyModelAvatarUtility.LooksLikeInvectorCharacterPrefab(instantiateSource) &&
                instantiateSource == visualSource)
            {
                Debug.LogWarning(
                    "[DMHumanoidVisualAttach] Source looks like an Invector character prefab and no FBX was resolved. " +
                    "Refusing to nest the template under Visual. Assign Space_suit.fbx or a mesh prefab.",
                    root);
                return result;
            }

            Dictionary<HumanBodyBones, string> templateNames =
                DMHumanoidBoneRenameUtility.SnapshotTemplateBoneNames(root, target, templateForBoneNames);
            DMHumanoidBoneRenameUtility.PrefixStockTemplateBones(root, templateNames);

            GameObject visualInstance = InstantiateVisualSource(instantiateSource);
            if (visualInstance == null)
                return result;

            visualInstance.name = childName;
            visualInstance.transform.SetParent(root.transform, false);
            visualInstance.transform.localPosition = Vector3.zero;
            visualInstance.transform.localRotation = Quaternion.identity;
            visualInstance.transform.localScale = Vector3.one;

            SanitizeAttachedVisual(visualInstance);
            DMHumanoidVisualMaterialUtility.SyncMaterialsFromSource(visualInstance, visualSource, refreshImport: true);

            EnemyModelAvatarUtility.PrepareModelInstance(visualInstance, preferHumanoidAvatar: true);
            result.Inspection = EnemyModelAvatarUtility.Inspect(visualInstance);

            Avatar driveAvatar = result.Inspection.Avatar;
            if (result.Inspection.IsHumanoidAvatar && result.Inspection.IsAvatarValid &&
                result.Inspection.Avatar != null)
            {
                Dictionary<HumanBodyBones, Transform> sourceBones =
                    DMHumanoidBoneRenameUtility.SnapshotHumanoidBonesUnder(visualInstance, result.Inspection.Avatar);
                var renamed = new List<DMHumanoidBoneRenameUtility.RenameEntry>(sourceBones.Count);
                result.BonesRenamed = DMHumanoidBoneRenameUtility.RenameVisualBonesToTemplate(
                    sourceBones,
                    templateNames,
                    renamed);
                DMHumanoidBoneRenameUtility.LogRenameSummary(root, renamed);

                Avatar rebuilt = DMHumanoidBoneRenameUtility.RebuildHumanoidAvatar(
                    visualInstance,
                    result.Inspection.Avatar,
                    templateNames);
                if (rebuilt != null && !string.IsNullOrEmpty(avatarPersistPath))
                    rebuilt = DMHumanoidBoneRenameUtility.SaveAvatarAsset(rebuilt, avatarPersistPath);
                if (rebuilt != null)
                {
                    result.RebuiltAvatar = rebuilt;
                    driveAvatar = rebuilt;
                }

                IntegrateHumanoidVisual(root, visualInstance, driveAvatar, target);
                result.AppliedHumanoidAvatar = true;
                Animator rootAnimator = root.GetComponent<Animator>();
                result.HipsBound = rootAnimator != null && rootAnimator.GetBoneTransform(HumanBodyBones.Hips) != null;

                string logPrefix = target == DMHumanoidVisualTarget.Player
                    ? "[PlayerPrefabVisualSetup]"
                    : "[EnemyInvectorSetup]";
                string avatarName = driveAvatar != null ? driveAvatar.name : "null";
                Debug.Log(
                    $"{logPrefix} Humanoid visual '{visualSource.name}' bound to root Animator " +
                    $"(avatar={avatarName}, renamed={result.BonesRenamed}, {result.Inspection.Summary}).",
                    root);
            }
            else
            {
                HideStockBodyMeshes(root);
                string logPrefix = target == DMHumanoidVisualTarget.Player
                    ? "[PlayerPrefabVisualSetup]"
                    : "[EnemyInvectorSetup]";
                Debug.LogWarning(
                    $"{logPrefix} Visual '{visualSource.name}' is not a valid Humanoid avatar " +
                    $"({result.Inspection.Summary}). Nested under '{childName}' and stock body meshes were hidden. " +
                    result.Inspection.Recommendation,
                    root);
            }

            EnableVisualBodyRenderers(visualInstance, target == DMHumanoidVisualTarget.Player);
            if (target == DMHumanoidVisualTarget.Enemy)
                LeanEnemyRenderCost(root);
            return result;
        }

        public static GameObject InstantiateVisualSource(GameObject visualSource)
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

        public static void ClearPreviousCustomVisual(GameObject root, string childName)
        {
            if (root == null)
                return;

            Transform existing = root.transform.Find(childName);
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            Transform[] children = new Transform[root.transform.childCount];
            for (int i = 0; i < root.transform.childCount; i++)
                children[i] = root.transform.GetChild(i);

            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (child == null || IsProtectedTemplateRootChild(child.name))
                    continue;

                if (IsLeftoverRootVisualJunk(child.name))
                    Object.DestroyImmediate(child.gameObject);
            }
        }

        public static void SanitizeAttachedVisual(GameObject visual)
        {
            if (visual == null)
                return;

            StripNestedInvectorCharacters(visual);
            StripSourceCombatDummies(visual);
            StripEditorOnlyJunk(visual);
            SanitizeSourceLods(visual);
        }

        public static void HideStockBodyMeshes(GameObject root)
        {
            Transform stockModel = root.transform.Find("3D Model");
            if (stockModel == null)
                return;

            SkinnedMeshRenderer[] skinned = stockModel.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < skinned.Length; i++)
            {
                if (skinned[i] == null || IsWeaponVisualNode(skinned[i].transform))
                    continue;
                skinned[i].enabled = false;
                if (skinned[i].GetComponent<Collider>() == null)
                    skinned[i].gameObject.SetActive(false);
            }

            MeshRenderer[] meshes = stockModel.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < meshes.Length; i++)
            {
                if (meshes[i] == null || IsWeaponVisualNode(meshes[i].transform))
                    continue;

                meshes[i].enabled = false;
                if (meshes[i].GetComponent<Collider>() == null)
                    meshes[i].gameObject.SetActive(false);
            }

            DeactivateStockLodRoots(stockModel);
        }

        public static void EnableVisualBodyRenderers(GameObject visual, bool updateWhenOffscreen = true)
        {
            if (visual == null)
                return;

            visual.SetActive(true);

            SkinnedMeshRenderer[] skinned = visual.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < skinned.Length; i++)
            {
                SkinnedMeshRenderer renderer = skinned[i];
                if (renderer == null || IsWeaponVisualNode(renderer.transform))
                    continue;
                renderer.enabled = true;
                renderer.gameObject.SetActive(true);
                renderer.updateWhenOffscreen = updateWhenOffscreen;
            }

            MeshRenderer[] meshes = visual.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < meshes.Length; i++)
            {
                MeshRenderer renderer = meshes[i];
                if (renderer == null || IsWeaponVisualNode(renderer.transform))
                    continue;
                renderer.enabled = true;
                renderer.gameObject.SetActive(true);
            }
        }

        public static void EnableVisualUpdateWhenOffscreen(GameObject rootOrVisual)
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

        private static void IntegrateHumanoidVisual(
            GameObject root,
            GameObject visualInstance,
            Avatar avatar,
            DMHumanoidVisualTarget target)
        {
            HideStockBodyMeshes(root);

            Animator rootAnimator = root.GetComponent<Animator>();
            if (rootAnimator == null)
                rootAnimator = root.AddComponent<Animator>();

            RuntimeAnimatorController keepController = rootAnimator.runtimeAnimatorController;

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
            rootAnimator.cullingMode = target == DMHumanoidVisualTarget.Player
                ? AnimatorCullingMode.AlwaysAnimate
                : AnimatorCullingMode.CullUpdateTransforms;
            rootAnimator.Rebind();
            rootAnimator.Update(0f);

            bool updateOffscreen = target == DMHumanoidVisualTarget.Player;
            EnableVisualBodyRenderers(visualInstance, updateOffscreen);
            if (updateOffscreen)
                EnableVisualUpdateWhenOffscreen(visualInstance);
            if (target == DMHumanoidVisualTarget.Enemy)
                LeanEnemyRenderCost(root);

            if (target == DMHumanoidVisualTarget.Player)
                DMHumanoidVisualFinalizeUtility.ClearPlayerDeadFlag(root);
            else
                EnemyInvectorSetupUtility.ClearSerializedInvectorDeadFlagPublic(root);

            if (rootAnimator.GetBoneTransform(HumanBodyBones.Hips) == null)
            {
                string logPrefix = target == DMHumanoidVisualTarget.Player
                    ? "[PlayerPrefabVisualSetup]"
                    : "[EnemyInvectorSetup]";
                Debug.LogWarning(
                    logPrefix + " Root Animator did not bind Hips after avatar swap. " +
                    "Check FBX Humanoid mapping. Visual remains nested; stock body stays hidden.",
                    root);
            }
            else if (target == DMHumanoidVisualTarget.Enemy)
            {
                EnemyInvectorWeaponHolderRebind.RebindToAnimatorBones(root);
                EnemyInvectorBodySnapSetup.ApplyRuntime(root);
            }

            if (target == DMHumanoidVisualTarget.Enemy)
                EnemyInvectorSetupUtility.RepairWeaponSlotVisualsFromLoadout(root);
            else
                EnemyInvectorSetupUtility.RepairWeaponSlotVisuals(root);
        }

        /// <summary>
        /// Cheap humanoid-enemy render pass: hide leftover VBOT Mesh-LOD when Visual is bound,
        /// drop AlwaysAnimate / updateWhenOffscreen, and stop holstered weapon particles.
        /// </summary>
        public static void LeanEnemyRenderCost(GameObject root)
        {
            if (root == null)
                return;

            Transform visual = root.transform.Find("Visual");
            if (visual != null && HasEnabledBodyRenderer(visual.gameObject))
                HideStockBodyMeshes(root);

            Animator rootAnimator = root.GetComponent<Animator>();
            if (rootAnimator != null)
                rootAnimator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

            SkinnedMeshRenderer[] skinned = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < skinned.Length; i++)
            {
                if (skinned[i] != null)
                    skinned[i].updateWhenOffscreen = false;
            }

            DisableUnusedWeaponParticles(root);
        }

        public static int LeanHumanoidEnemyPrefabAsset(string prefabPath)
        {
            if (string.IsNullOrEmpty(prefabPath))
                return 0;

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null)
                return 0;

            try
            {
                LeanEnemyRenderCost(root);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                return 1;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        public static readonly string[] HumanoidEnemyOutputPrefabPaths =
        {
            "Assets/_Project/Prefabs/Combat/Enemies/Humadroid.prefab",
            "Assets/_Project/Prefabs/Combat/Enemies/Lisa_Hybrid.prefab",
            "Assets/_Project/Prefabs/Combat/Enemies/Axe_Droid.prefab",
            "Assets/_Project/Prefabs/Combat/Enemies/corrupt_patrol_android.prefab",
            "Assets/_Project/Prefabs/Combat/Enemies/Hybrid Droid.prefab",
            "Assets/_Project/Prefabs/Combat/Enemies/Humanoid.prefab",
            "Assets/_Project/Prefabs/Combat/Enemies/Robot.prefab",
            "Assets/_Project/Prefabs/Combat/Enemies/The_Evil_One.prefab",
            "Assets/_Project/Prefabs/Combat/Enemies/HumanoidEnemy_Invector.prefab"
        };

        public static string LeanHumanoidEnemyOutputs()
        {
            var sb = new System.Text.StringBuilder();
            int leaned = 0;
            for (int i = 0; i < HumanoidEnemyOutputPrefabPaths.Length; i++)
            {
                string path = HumanoidEnemyOutputPrefabPaths[i];
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                {
                    sb.AppendLine("missing " + path);
                    continue;
                }

                leaned += LeanHumanoidEnemyPrefabAsset(path);
                sb.AppendLine("leaned " + path);
            }

            sb.AppendLine("count=" + leaned);
            return sb.ToString();
        }

        static void DeactivateStockLodRoots(Transform stockModel)
        {
            if (stockModel == null)
                return;

            LODGroup[] groups = stockModel.GetComponentsInChildren<LODGroup>(true);
            for (int i = 0; i < groups.Length; i++)
            {
                LODGroup group = groups[i];
                if (group == null || IsWeaponVisualNode(group.transform))
                    continue;

                group.enabled = false;
                if (IsStockLodRootName(group.gameObject.name))
                    group.gameObject.SetActive(false);
            }

            Transform[] nodes = stockModel.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < nodes.Length; i++)
            {
                Transform node = nodes[i];
                if (node == null || node == stockModel || IsWeaponVisualNode(node))
                    continue;
                if (IsStockLodRootName(node.name))
                    node.gameObject.SetActive(false);
            }
        }

        static bool IsStockLodRootName(string name)
        {
            return name.Equals("Mesh_LOD", System.StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("Mesh-LOD", System.StringComparison.OrdinalIgnoreCase) ||
                   name.StartsWith("Mesh_LOD", System.StringComparison.OrdinalIgnoreCase) ||
                   name.StartsWith("Mesh-LOD", System.StringComparison.OrdinalIgnoreCase) ||
                   name.StartsWith("VBOT_LOD", System.StringComparison.OrdinalIgnoreCase) ||
                   name.IndexOf("VBOT_LOD", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static bool HasEnabledBodyRenderer(GameObject visual)
        {
            if (visual == null)
                return false;

            SkinnedMeshRenderer[] skinned = visual.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < skinned.Length; i++)
            {
                if (skinned[i] != null && skinned[i].enabled && !IsWeaponVisualNode(skinned[i].transform))
                    return true;
            }

            MeshRenderer[] meshes = visual.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < meshes.Length; i++)
            {
                if (meshes[i] != null && meshes[i].enabled && !IsWeaponVisualNode(meshes[i].transform))
                    return true;
            }

            return false;
        }

        static void DisableUnusedWeaponParticles(GameObject root)
        {
            ParticleSystem[] systems = root.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem ps = systems[i];
                if (ps == null)
                    continue;

                Transform node = ps.transform;
                bool holstered = false;
                bool drawnInactive = false;
                bool underStockLod = false;
                while (node != null && node != root.transform)
                {
                    string name = node.name;
                    if (name.StartsWith("Holstered_", System.StringComparison.Ordinal))
                    {
                        holstered = true;
                        break;
                    }

                    if (name.StartsWith("Drawn_", System.StringComparison.Ordinal))
                    {
                        drawnInactive = !node.gameObject.activeSelf;
                        break;
                    }

                    if (IsStockLodRootName(name))
                    {
                        underStockLod = true;
                        break;
                    }

                    node = node.parent;
                }

                if (!holstered && !drawnInactive && !underStockLod)
                    continue;

                ParticleSystem.MainModule main = ps.main;
                main.playOnAwake = false;
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
                if (renderer != null)
                    renderer.enabled = false;
            }
        }

        public static bool IsWeaponVisualNode(Transform node)
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

        static bool IsProtectedTemplateRootChild(string name)
        {
            return name.Equals("3D Model", System.StringComparison.Ordinal) ||
                   name.Equals("Humanoid", System.StringComparison.Ordinal) ||
                   name.Equals("BodySnaps", System.StringComparison.Ordinal) ||
                   name.Equals("InvectorComponents", System.StringComparison.Ordinal) ||
                   name.Equals("hitBox", System.StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("leftFoot_trigger", System.StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("rightFoot_trigger", System.StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("WeaponHolders", System.StringComparison.Ordinal);
        }

        static bool IsLeftoverRootVisualJunk(string name)
        {
            if (name.Equals("Armature", System.StringComparison.OrdinalIgnoreCase) ||
                name.Equals("char1", System.StringComparison.OrdinalIgnoreCase) ||
                name.Equals("rig", System.StringComparison.OrdinalIgnoreCase) ||
                name.Equals("HumanoidEnemy_Invector", System.StringComparison.OrdinalIgnoreCase) ||
                name.Equals("Player_Invector", System.StringComparison.OrdinalIgnoreCase))
                return true;

            return name.StartsWith("Mesh-LOD", System.StringComparison.OrdinalIgnoreCase) ||
                   name.StartsWith("Mesh_LOD", System.StringComparison.OrdinalIgnoreCase);
        }

        static void StripNestedInvectorCharacters(GameObject visual)
        {
            Transform[] nodes = visual.GetComponentsInChildren<Transform>(true);
            for (int i = nodes.Length - 1; i >= 0; i--)
            {
                Transform node = nodes[i];
                if (node == null || node.gameObject == visual)
                    continue;

                string name = node.name;
                if (name.Equals("HumanoidEnemy_Invector", System.StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("Player_Invector", System.StringComparison.OrdinalIgnoreCase) ||
                    (node.parent == visual.transform &&
                     EnemyModelAvatarUtility.LooksLikeInvectorCharacterPrefab(node.gameObject)))
                {
                    Object.DestroyImmediate(node.gameObject);
                }
            }
        }

        static void StripSourceCombatDummies(GameObject visual)
        {
            Transform[] nodes = visual.GetComponentsInChildren<Transform>(true);
            for (int i = nodes.Length - 1; i >= 0; i--)
            {
                Transform node = nodes[i];
                if (node == null || node.gameObject == visual || IsWeaponVisualNode(node))
                    continue;

                string name = node.name;
                bool namedHitVolume =
                    name.Equals("hitBox", System.StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("leftFoot_trigger", System.StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("rightFoot_trigger", System.StringComparison.OrdinalIgnoreCase) ||
                    name.EndsWith("_trigger", System.StringComparison.OrdinalIgnoreCase);

                if (namedHitVolume || IsUnityPrimitiveHitMesh(node))
                    Object.DestroyImmediate(node.gameObject);
            }
        }

        static void StripEditorOnlyJunk(GameObject visual)
        {
            Camera[] cameras = visual.GetComponentsInChildren<Camera>(true);
            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i] != null)
                    Object.DestroyImmediate(cameras[i].gameObject);
            }

            Light[] lights = visual.GetComponentsInChildren<Light>(true);
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] != null)
                    Object.DestroyImmediate(lights[i].gameObject);
            }

            AudioListener[] listeners = visual.GetComponentsInChildren<AudioListener>(true);
            for (int i = 0; i < listeners.Length; i++)
            {
                if (listeners[i] != null)
                    Object.DestroyImmediate(listeners[i]);
            }
        }

        static void SanitizeSourceLods(GameObject visual)
        {
            LODGroup[] groups = visual.GetComponentsInChildren<LODGroup>(true);
            if (groups != null && groups.Length > 0)
                return;

            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || IsWeaponVisualNode(renderer.transform))
                    continue;

                string name = renderer.gameObject.name;
                if (!IsLowLodName(name))
                    continue;

                if (HasPreferredLodSibling(renderer.transform, name))
                {
                    renderer.enabled = false;
                    renderer.gameObject.SetActive(false);
                }
            }
        }

        static bool IsLowLodName(string name)
        {
            return name.EndsWith("_LOW", System.StringComparison.OrdinalIgnoreCase) ||
                   name.EndsWith("_low", System.StringComparison.Ordinal) ||
                   name.EndsWith("LOW", System.StringComparison.Ordinal);
        }

        static bool HasPreferredLodSibling(Transform node, string lowName)
        {
            Transform parent = node.parent;
            if (parent == null)
                return false;

            string stem = StripLodSuffix(lowName);
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform sibling = parent.GetChild(i);
                if (sibling == null || sibling == node)
                    continue;

                string siblingName = sibling.name;
                if (siblingName.Equals(stem, System.StringComparison.OrdinalIgnoreCase) ||
                    siblingName.Equals(stem + "_HI", System.StringComparison.OrdinalIgnoreCase) ||
                    siblingName.Equals(stem + "_hi", System.StringComparison.OrdinalIgnoreCase) ||
                    siblingName.Equals(stem + "_HI", System.StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        static string StripLodSuffix(string name)
        {
            if (name.EndsWith("_LOW", System.StringComparison.OrdinalIgnoreCase))
                return name.Substring(0, name.Length - 4);
            if (name.EndsWith("_low", System.StringComparison.Ordinal))
                return name.Substring(0, name.Length - 4);
            if (name.EndsWith("LOW", System.StringComparison.Ordinal))
                return name.Substring(0, name.Length - 3);
            return name;
        }

        static bool IsUnityPrimitiveHitMesh(Transform node)
        {
            if (node == null)
                return false;

            MeshFilter filter = node.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
                return false;

            string meshName = filter.sharedMesh.name;
            if (!meshName.Equals("Sphere", System.StringComparison.Ordinal) &&
                !meshName.Equals("Capsule", System.StringComparison.Ordinal) &&
                !meshName.Equals("Cube", System.StringComparison.Ordinal))
                return false;

            return node.GetComponent<SkinnedMeshRenderer>() == null;
        }
    }
}
#endif
