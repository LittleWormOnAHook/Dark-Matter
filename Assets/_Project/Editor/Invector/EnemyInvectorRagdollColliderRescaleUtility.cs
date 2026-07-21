#if UNITY_EDITOR
using System.Text;
using Project.AI.Invector;
using Project.EditorTools;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Invector
{
    /// <summary>
    /// Rescales a humanoid prefab's ragdoll bone colliders if — and only if — its raw bone Transform
    /// hierarchy genuinely disagrees with its rendered world-space size.
    ///
    /// This was originally written to fix VBOT_LOD.fbx-based enemy prefabs whose bone colliders looked
    /// wildly oversized (e.g. radius=13.93 on a thigh). That turned out to be a false alarm: those bones
    /// sit under an "Armature" wrapper Transform with localScale ~0.01 (a standard cm-to-meter
    /// conversion), so a raw local radius of 13.93 is actually a correct ~0.14m in world space once you
    /// apply that scale — see EnemyInvectorPhysicsCache.IsImplausiblyOversized, which now accounts for
    /// Transform.lossyScale before judging size. This tool is kept as a defensive measure for the rarer,
    /// genuine case: a rig whose bind pose really is baked at the wrong scale relative to its own render
    /// bounds (confirmed by comparing raw bone-position height against the render-bounds-derived root
    /// capsule height). It only touches flagged collider dimensions — never bone positions, joints,
    /// rigidbodies, the source FBX, or any other prefab — so running it is always safe even when, as with
    /// both prefabs it was built for, it correctly finds nothing to do.
    /// </summary>
    public static class EnemyInvectorRagdollColliderRescaleUtility
    {
        [MenuItem(SurvivalPioneerEditorMenus.Combat + "Rescale Oversized Ragdoll Colliders", false, 136)]
        public static void RescaleSelectedOversizedColliders()
        {
            GameObject[] selected = Selection.gameObjects;
            if (selected == null || selected.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "Rescale Ragdoll Colliders",
                    "Select a humanoid enemy prefab (in Prefab Mode) or instance.",
                    "OK");
                return;
            }

            StringBuilder summary = new StringBuilder();
            for (int i = 0; i < selected.Length; i++)
            {
                GameObject root = selected[i];
                if (root == null)
                    continue;

                summary.AppendLine(Rescale(root));
            }

            EditorUtility.DisplayDialog("Rescale Ragdoll Colliders", summary.ToString(), "OK");
        }

        private static string Rescale(GameObject root)
        {
            Animator animator = root.GetComponentInChildren<Animator>(true);
            if (animator == null || !animator.isHuman)
                return $"{root.name}: no humanoid Animator found, skipped.";

            Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
            Transform leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            Transform rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            if (head == null || leftFoot == null || rightFoot == null)
                return $"{root.name}: missing key bones (Head/LeftFoot/RightFoot), skipped.";

            // The root CapsuleCollider is fit from the SkinnedMeshRenderer's actual world-space render
            // bounds (see EnemyInvectorHitSetup.TryFitCapsuleToRenderers), which matches what you
            // actually see in game — proven correct, unlike the raw bone Transform positions. Use it as
            // the ground-truth reference height to measure how far off the raw skeleton really is.
            CapsuleCollider rootCapsule = root.GetComponent<CapsuleCollider>();
            if (rootCapsule == null || rootCapsule.height <= 0.01f)
                return $"{root.name}: no valid root CapsuleCollider to use as a size reference, skipped.";

            float feetY = 0.5f * (leftFoot.position.y + rightFoot.position.y);
            float rawSkeletonHeight = head.position.y - feetY;
            if (rawSkeletonHeight <= 0.01f)
                return $"{root.name}: could not measure raw skeleton height, skipped.";

            float correctHeight = rootCapsule.height;
            float ratio = rawSkeletonHeight / correctHeight;

            // A modest mismatch (e.g. a tall/short character) is normal and not what this tool is for.
            // Only correct genuine order-of-magnitude bind-pose scale problems.
            if (ratio < 2f)
            {
                return $"{root.name}: raw skeleton height ({rawSkeletonHeight:0.##}) is close enough to " +
                       $"the reference height ({correctHeight:0.##}); nothing to rescale.";
            }

            int rescaledCount = 0;
            Rigidbody rootBody = root.GetComponent<Rigidbody>();
            Rigidbody[] bodies = root.GetComponentsInChildren<Rigidbody>(true);
            for (int b = 0; b < bodies.Length; b++)
            {
                Rigidbody body = bodies[b];
                if (body == null || body == rootBody)
                    continue;

                Collider[] colliders = body.GetComponents<Collider>();
                for (int c = 0; c < colliders.Length; c++)
                {
                    Collider collider = colliders[c];
                    if (collider == null || !EnemyInvectorPhysicsCache.IsImplausiblyOversized(collider, out _))
                        continue;

                    Undo.RecordObject(collider, "Rescale Ragdoll Collider");
                    RescaleCollider(collider, ratio);
                    EditorUtility.SetDirty(collider);
                    rescaledCount++;
                }
            }

            return rescaledCount > 0
                ? $"{root.name}: measured scale mismatch {ratio:0.##}x, rescaled {rescaledCount} bone collider(s). Save the prefab and re-run the audit to confirm."
                : $"{root.name}: measured scale mismatch {ratio:0.##}x, but found no colliders currently flagged as oversized.";
        }

        private static void RescaleCollider(Collider collider, float ratio)
        {
            switch (collider)
            {
                case CapsuleCollider capsule:
                    capsule.radius /= ratio;
                    capsule.height /= ratio;
                    capsule.center /= ratio;
                    break;

                case BoxCollider box:
                    box.size /= ratio;
                    box.center /= ratio;
                    break;

                case SphereCollider sphere:
                    sphere.radius /= ratio;
                    sphere.center /= ratio;
                    break;
            }
        }
    }
}
#endif
