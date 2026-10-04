using Invector.vCharacterController;
using Project.AI;
using UnityEngine;

namespace Project.AI.Invector
{
    /// <summary>
    /// Clears Invector ragdoll flags without waking the root rigidbody (ResetRagdoll is player-oriented).
    /// </summary>
    public static class EnemyInvectorRagdollStateUtility
    {
        public static void RestoreAnimatedPhysics(GameObject root)
        {
            if (root == null)
                return;

            ClearRagdollWithoutResetRagdoll(root);

            EnemyInvectorBootstrap bootstrap = root.GetComponent<EnemyInvectorBootstrap>();
            bootstrap?.EnsureInvectorPhysicsReady();
            EnemyInvectorHitSetup.StabilizeRigidbodies(root);
            EnemyInvectorHitSetup.RestoreRagdollPhysicsLayers(root);
            Physics.SyncTransforms();
        }

        /// <summary>
        /// Clears ragdoll / controller flags without <see cref="vThirdPersonController.ResetRagdoll"/>,
        /// which wakes the root rigidbody with gravity and causes spawn / get-up launches.
        /// </summary>
        public static void ClearRagdollWithoutResetRagdoll(GameObject root)
        {
            if (root == null)
                return;

            vThirdPersonController controller = root.GetComponent<vThirdPersonController>();
            vRagdoll ragdoll = root.GetComponent<vRagdoll>();

            if (ragdoll != null)
            {
                ragdoll.startRagdolled = false;
                ragdoll.keepRagdolled = false;

                if (ragdoll.isActive || ragdoll.state != vRagdoll.RagdollState.animated)
                {
                    ragdoll.isActive = false;
                    ragdoll.state = vRagdoll.RagdollState.animated;
                }
            }

            ClearControllerRagdollFlag(controller);

            if (controller != null)
            {
                controller.disableCheckGround = true;
                controller.useSnapGround = false;
                controller.extraGravity = 0f;

                if (controller.animator != null)
                {
                    controller.animator.enabled = true;
                    controller.animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                }
            }

            ForceLivingRenderersVisible(root);
        }

        private static void ForceLivingRenderersVisible(GameObject root)
        {
            if (root == null)
                return;

            HumanoidPerformanceController.ForceSpawnVisible(root);

            SkinnedMeshRenderer[] renderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                SkinnedMeshRenderer renderer = renderers[i];
                if (renderer == null)
                    continue;

                renderer.updateWhenOffscreen = true;
            }
        }

        /// <summary>
        /// Clears ragdolled state only — never calls <see cref="vThirdPersonController.ResetRagdoll"/>.
        /// </summary>
        public static void ClearControllerRagdollFlag(vThirdPersonController controller)
        {
            if (controller == null)
                return;

            controller.ragdolled = false;
            controller.verticalVelocity = 0f;
        }
    }
}
