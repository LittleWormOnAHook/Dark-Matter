using Invector.vShooter;
using UnityEngine;

namespace Project.Player.Invector
{
    /// <summary>
    /// Faster aim IK/arm alignment for Meshy-swapped player prefabs (Visual/ + humanoid root Animator).
    /// Does not rename VBOT bones — only tunes vShooterManager smooth fields and pairs with
    /// <see cref="PioneerShooterMeleeInput"/> snap overrides.
    /// </summary>
    public static class PioneerInvectorMeshyAimSnapUtility
    {
        /// <summary>Stock Player_v7 used 20 — higher = faster IK offset convergence per fixed step.</summary>
        public const float MeshySnapIkAdjustSmooth = 72f;

        /// <summary>Arm-to-camera alignment follow speed (Invector armAlignmentWeight lerp factor).</summary>
        public const float MeshySnapArmWeight = 48f;

        /// <summary>vArmAimAlign bone rotation smooth.</summary>
        public const float MeshySnapArmIkRotation = 48f;

        public const float MeshySnapArmIkSmoothIn = 48f;
        public const float MeshySnapArmIkSmoothOut = 60f;

        public static bool HasMeshyVisualRoot(GameObject root)
        {
            if (root == null)
                return false;

            Transform visual = root.transform.Find("Visual");
            if (visual == null)
                return false;

            Animator animator = root.GetComponent<Animator>();
            return animator != null && animator.isHuman && animator.avatar != null && animator.avatar.isValid;
        }

        public static void ApplyShooterManagerSettings(GameObject root, vShooterManager manager)
        {
            if (manager == null || !HasMeshyVisualRoot(root))
                return;

            ApplyShooterManagerSettings(manager);
        }

        public static void ApplyShooterManagerSettings(vShooterManager manager)
        {
            if (manager == null)
                return;

            manager.ikAdjustSmooth = MeshySnapIkAdjustSmooth;
            manager.smoothArmWeight = MeshySnapArmWeight;
            manager.smoothArmIKRotation = MeshySnapArmIkRotation;
            manager.armIKSmoothIn = MeshySnapArmIkSmoothIn;
            manager.armIKSmoothOut = MeshySnapArmIkSmoothOut;
        }
    }
}
