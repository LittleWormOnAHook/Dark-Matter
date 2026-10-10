#if UNITY_EDITOR
using System.Collections.Generic;
using Invector.vCharacterController;
using Project.AI.Invector;
using Project.EditorTools.Invector;
using Project.Player.Invector;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Shared post-attach visual repair for player and humanoid enemy Invector prefabs.
    /// </summary>
    public static class DMHumanoidVisualFinalizeUtility
    {
        public static void FinalizeVisualCommon(GameObject root)
        {
            FinalizeVisualCommon(root, InferVisualTarget(root));
        }

        public static void FinalizeVisualCommon(GameObject root, DMHumanoidVisualTarget target)
        {
            if (root == null)
                return;

            EnemyInvectorBodySnapSetupEditor.ConfigureEditor(root);
            EnemyInvectorWeaponHolderRebind.RebindToAnimatorBones(root);

            int remounted = PlayerInvectorRagdollSetup.RepairSeparatedRagdoll(root);
            if (remounted > 0)
            {
                Debug.Log(
                    $"[DMHumanoidVisualFinalize] Remounted orphan ragdoll onto avatar ({remounted} bone rigidbodies).",
                    root);
            }

            EnemyInvectorSetupUtility.RepairWeaponSlotVisuals(root);
            RepairEditModeAnimator(root, target);
            RemountTemplateCombatNodes(root);

            Transform visual = root.transform.Find("Visual");
            string visualName = visual != null ? visual.name : "Visual";
            Dictionary<HumanBodyBones, string> templateNames =
                DMHumanoidBoneRenameUtility.SnapshotTemplateBoneNames(
                    root,
                    DMHumanoidVisualTarget.Enemy,
                    null);
            if (templateNames == null || templateNames.Count == 0)
            {
                templateNames = DMHumanoidBoneRenameUtility.SnapshotTemplateBoneNames(
                    root,
                    DMHumanoidVisualTarget.Player,
                    null);
            }

            DMHumanoidBoneRenameUtility.RemountStockHitVolumesToVisual(root, visualName, templateNames);
            if (visual != null)
            {
                DMHumanoidVisualAttachUtility.EnableVisualBodyRenderers(
                    visual.gameObject,
                    updateWhenOffscreen: target == DMHumanoidVisualTarget.Player);
            }

            if (target == DMHumanoidVisualTarget.Enemy)
                DMHumanoidVisualAttachUtility.LeanEnemyRenderCost(root);
        }

        public static void RepairEditModeAnimator(GameObject root)
        {
            RepairEditModeAnimator(root, InferVisualTarget(root));
        }

        public static void RepairEditModeAnimator(GameObject root, DMHumanoidVisualTarget target)
        {
            if (root == null)
                return;

            ClearPlayerDeadFlag(root);

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
            rootAnimator.cullingMode = target == DMHumanoidVisualTarget.Player
                ? AnimatorCullingMode.AlwaysAnimate
                : AnimatorCullingMode.CullUpdateTransforms;

            RuntimeAnimatorController savedController = rootAnimator.runtimeAnimatorController;
            rootAnimator.runtimeAnimatorController = null;
            rootAnimator.enabled = true;
            if (rootAnimator.avatar != null && rootAnimator.avatar.isValid)
            {
                rootAnimator.Rebind();
                rootAnimator.Update(0f);
            }

            bool copiedFromFbx = TryCopyBindPoseFromAvatarSource(rootAnimator);

            rootAnimator.runtimeAnimatorController = savedController;
            rootAnimator.writeDefaultValuesOnDisable = !copiedFromFbx;
            if (!Application.isPlaying)
                rootAnimator.enabled = false;

            if (copiedFromFbx)
                TryCopyBindPoseFromAvatarSource(rootAnimator);

            if (target == DMHumanoidVisualTarget.Player)
                DMHumanoidVisualAttachUtility.EnableVisualUpdateWhenOffscreen(root);

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

        public static bool TryCopyBindPoseFromAvatarSource(Animator rootAnimator)
        {
            if (rootAnimator == null || rootAnimator.avatar == null)
                return false;

            string avatarPath = AssetDatabase.GetAssetPath(rootAnimator.avatar);
            if (string.IsNullOrEmpty(avatarPath))
                return false;

            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(avatarPath);
            if (fbx == null)
                return false;

            Transform visual = rootAnimator.transform.Find("Visual");
            if (visual == null)
            {
                Transform hips = rootAnimator.GetBoneTransform(HumanBodyBones.Hips);
                if (hips != null && hips.parent != null && hips.parent.parent != null)
                    visual = hips.parent.parent;
            }

            if (visual == null)
                return false;

            GameObject temp = Object.Instantiate(fbx);
            try
            {
                var srcRot = new System.Collections.Generic.Dictionary<string, Quaternion>(64);
                var srcPos = new System.Collections.Generic.Dictionary<string, Vector3>(64);
                var srcScl = new System.Collections.Generic.Dictionary<string, Vector3>(64);
                Transform[] srcTransforms = temp.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < srcTransforms.Length; i++)
                {
                    Transform t = srcTransforms[i];
                    if (t == null || srcRot.ContainsKey(t.name))
                        continue;
                    srcRot[t.name] = t.localRotation;
                    srcPos[t.name] = t.localPosition;
                    srcScl[t.name] = t.localScale;
                }

                int applied = 0;
                Transform[] dstTransforms = visual.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < dstTransforms.Length; i++)
                {
                    Transform d = dstTransforms[i];
                    if (d == null || d == visual || !srcRot.ContainsKey(d.name))
                        continue;
                    d.localPosition = srcPos[d.name];
                    d.localRotation = srcRot[d.name];
                    d.localScale = srcScl[d.name];
                    EditorUtility.SetDirty(d);
                    applied++;
                }

                return applied > 0;
            }
            finally
            {
                Object.DestroyImmediate(temp);
            }
        }

        /// <summary>
        /// Parents template hitBox / foot triggers onto the live Humanoid bones so they
        /// follow the new mesh instead of the hidden VBOT armature.
        /// </summary>
        public static void RemountTemplateCombatNodes(GameObject root)
        {
            if (root == null)
                return;

            Animator animator = root.GetComponent<Animator>();
            if (animator == null || animator.avatar == null || !animator.avatar.isHuman)
                return;

            if (animator.avatar.isValid)
            {
                animator.Rebind();
                animator.Update(0f);
            }

            Transform visual = root.transform.Find("Visual");
            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips) ??
                             FindVisualBoneByTemplateName(visual, animator, HumanBodyBones.Hips);
            Transform leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot) ??
                                 FindVisualBoneByTemplateName(visual, animator, HumanBodyBones.LeftFoot);
            Transform rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot) ??
                                  FindVisualBoneByTemplateName(visual, animator, HumanBodyBones.RightFoot);

            Transform[] nodes = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < nodes.Length; i++)
            {
                Transform node = nodes[i];
                if (node == null || node == root.transform)
                    continue;
                if (IsOutgoingWeaponSlot(node))
                    continue;

                string name = node.name;
                Transform destination = null;
                if (name.Equals("leftFoot_trigger", System.StringComparison.OrdinalIgnoreCase))
                    destination = leftFoot;
                else if (name.Equals("rightFoot_trigger", System.StringComparison.OrdinalIgnoreCase))
                    destination = rightFoot;
                else if (name.Equals("hitBox", System.StringComparison.OrdinalIgnoreCase))
                    destination = ResolveHitBoxDestination(animator, node, hips, visual);

                if (destination == null || node.parent == destination || node.IsChildOf(destination))
                    continue;

                node.SetParent(destination, true);
            }

            DeduplicateNamedChildren(leftFoot, "leftFoot_trigger");
            DeduplicateNamedChildren(rightFoot, "rightFoot_trigger");
        }

        static Transform ResolveHitBoxDestination(
            Animator animator,
            Transform hitBox,
            Transform hips,
            Transform visual)
        {
            if (visual != null && hitBox.IsChildOf(visual))
                return null;

            if (hitBox.parent != null && hitBox.parent != animator.transform)
            {
                Transform mapped = ResolveHumanoidBoneFromName(animator, hitBox.parent.name);
                if (mapped != null)
                    return mapped;
            }

            return hips;
        }

        static Transform FindVisualBoneByTemplateName(Transform visual, Animator animator, HumanBodyBones bone)
        {
            if (visual == null)
                return null;

            Avatar avatar = animator != null ? animator.avatar : null;
            if (avatar != null && avatar.isHuman)
            {
                HumanBone[] humans = avatar.humanDescription.human;
                if (humans != null)
                {
                    string want = HumanTrait.BoneName[(int)bone];
                    for (int i = 0; i < humans.Length; i++)
                    {
                        if (!humans[i].humanName.Equals(want, System.StringComparison.OrdinalIgnoreCase))
                            continue;
                        Transform named = DMHumanoidBoneRenameUtility.FindNamedUnder(visual, humans[i].boneName);
                        if (named != null)
                            return named;
                    }
                }
            }

            return DMHumanoidBoneRenameUtility.FindNamedUnder(visual, "VBOT_:" + bone) ??
                   DMHumanoidBoneRenameUtility.FindNamedUnder(visual, "VBOT_" + bone);
        }

        static Transform ResolveHumanoidBoneFromName(Animator animator, string sourceName)
        {
            if (animator == null || string.IsNullOrEmpty(sourceName))
                return null;

            string suffix = sourceName;
            const string vbotColon = "VBOT_:";
            if (suffix.StartsWith(vbotColon, System.StringComparison.OrdinalIgnoreCase))
                suffix = suffix.Substring(vbotColon.Length);
            else if (suffix.StartsWith("VBOT_", System.StringComparison.OrdinalIgnoreCase))
                suffix = suffix.Substring("VBOT_".Length);

            HumanBodyBones bone;
            if (suffix.Equals("Hips", System.StringComparison.OrdinalIgnoreCase))
                bone = HumanBodyBones.Hips;
            else if (suffix.Equals("LeftUpLeg", System.StringComparison.OrdinalIgnoreCase))
                bone = HumanBodyBones.LeftUpperLeg;
            else if (suffix.Equals("LeftLeg", System.StringComparison.OrdinalIgnoreCase))
                bone = HumanBodyBones.LeftLowerLeg;
            else if (suffix.Equals("LeftFoot", System.StringComparison.OrdinalIgnoreCase))
                bone = HumanBodyBones.LeftFoot;
            else if (suffix.Equals("RightUpLeg", System.StringComparison.OrdinalIgnoreCase))
                bone = HumanBodyBones.RightUpperLeg;
            else if (suffix.Equals("RightLeg", System.StringComparison.OrdinalIgnoreCase))
                bone = HumanBodyBones.RightLowerLeg;
            else if (suffix.Equals("RightFoot", System.StringComparison.OrdinalIgnoreCase))
                bone = HumanBodyBones.RightFoot;
            else if (suffix.Equals("LeftArm", System.StringComparison.OrdinalIgnoreCase))
                bone = HumanBodyBones.LeftUpperArm;
            else if (suffix.Equals("LeftForeArm", System.StringComparison.OrdinalIgnoreCase))
                bone = HumanBodyBones.LeftLowerArm;
            else if (suffix.Equals("LeftHand", System.StringComparison.OrdinalIgnoreCase))
                bone = HumanBodyBones.LeftHand;
            else if (suffix.Equals("RightArm", System.StringComparison.OrdinalIgnoreCase))
                bone = HumanBodyBones.RightUpperArm;
            else if (suffix.Equals("RightForeArm", System.StringComparison.OrdinalIgnoreCase))
                bone = HumanBodyBones.RightLowerArm;
            else if (suffix.Equals("RightHand", System.StringComparison.OrdinalIgnoreCase))
                bone = HumanBodyBones.RightHand;
            else if (suffix.Equals("Head", System.StringComparison.OrdinalIgnoreCase))
                bone = HumanBodyBones.Head;
            else if (suffix.Equals("Spine", System.StringComparison.OrdinalIgnoreCase) ||
                     suffix.Equals("Spine1", System.StringComparison.OrdinalIgnoreCase) ||
                     suffix.Equals("Chest", System.StringComparison.OrdinalIgnoreCase))
                bone = HumanBodyBones.Chest;
            else
                return null;

            return animator.GetBoneTransform(bone);
        }

        static bool IsOutgoingWeaponSlot(Transform node)
        {
            Transform cur = node;
            while (cur != null)
            {
                string name = cur.name;
                if (name.StartsWith("Drawn_", System.StringComparison.Ordinal) ||
                    name.StartsWith("Holstered_", System.StringComparison.Ordinal) ||
                    name.Equals("WeaponHolders", System.StringComparison.Ordinal) ||
                    name.Equals("RightHandlers", System.StringComparison.Ordinal) ||
                    name.Equals("LeftHandlers", System.StringComparison.Ordinal))
                    return true;
                cur = cur.parent;
            }

            return false;
        }

        static void DeduplicateNamedChildren(Transform parent, string childName)
        {
            if (parent == null)
                return;

            Transform keep = null;
            Transform[] children = parent.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (child == null || child == parent)
                    continue;
                if (!child.name.Equals(childName, System.StringComparison.OrdinalIgnoreCase))
                    continue;
                if (keep == null)
                {
                    keep = child;
                    continue;
                }

                Object.DestroyImmediate(child.gameObject);
            }
        }

        static DMHumanoidVisualTarget InferVisualTarget(GameObject root)
        {
            if (root != null && root.GetComponent<PioneerShooterMeleeInput>() != null)
                return DMHumanoidVisualTarget.Player;
            return DMHumanoidVisualTarget.Enemy;
        }

        public static void ClearPlayerDeadFlag(GameObject root)
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

            if (controller is global::Invector.vHealthController health && health.isDead)
            {
                health.isDead = false;
                health.ResetHealth();
            }
        }
    }
}
#endif
