using Invector.vMelee;
using Invector.vShooter;
using Project.Combat;
using Project.Interaction;
using UnityEngine;

namespace Project.AI.Invector
{
    /// <summary>
    /// Fits a single root hit capsule and disables ragdoll/child colliders so humanoid enemies
    /// are hittable where they appear and do not launch from projectile force.
    /// </summary>
    public static class EnemyInvectorHitSetup
    {
        public static void Apply(
            GameObject root,
            float targetRadius = 0.45f,
            float targetHeight = 2f,
            Vector3 targetCenter = default,
            bool fitToRenderers = true)
        {
            if (root == null)
                return;

            if (targetCenter == default)
                targetCenter = new Vector3(0f, 1f, 0f);

            StabilizeRigidbodies(root);
            DisableChildSolidColliders(root);
            FitRootCapsule(root, targetRadius, targetHeight, targetCenter, fitToRenderers);
            EnsureRootDamageReceiver(root);
            EnsureRagdollBoneDamageProxies(root);
        }

        public static void StabilizeRigidbodies(GameObject root)
        {
            if (root == null)
                return;

            GetOrCreateCache(root).StabilizeAllRigidbodies();
        }

        /// <summary>
        /// Ragdoll bones must use a terrain-colliding layer. Bootstrap assigns Enemy to the full hierarchy,
        /// which can prevent corpses and dropped weapons from colliding with the ground.
        /// </summary>
        public static void RestoreRagdollPhysicsLayers(GameObject root)
        {
            if (root == null)
                return;

            GetOrCreateCache(root).RestoreBonePhysicsLayers();
        }

        /// <summary>
        /// Unlocks bone rigidbodies so Invector ragdoll can take over on death.
        /// Also ensures bone damage proxies exist so melee hits still route while down.
        /// </summary>
        public static void ReleaseForRagdoll(GameObject root)
        {
            if (root == null)
                return;

            EnsureRagdollBoneDamageProxies(root);
            GetOrCreateCache(root).ReleaseBonesForRagdoll();
        }

        public static void EnsureRootCapsule(
            GameObject root,
            float targetRadius = 0.45f,
            float targetHeight = 2f,
            Vector3 targetCenter = default,
            bool fitToRenderers = true)
        {
            if (root == null)
                return;

            if (targetCenter == default)
                targetCenter = new Vector3(0f, 1f, 0f);

            FitRootCapsule(root, targetRadius, targetHeight, targetCenter, fitToRenderers);
        }

        private static EnemyInvectorPhysicsCache GetOrCreateCache(GameObject root)
        {
            EnemyInvectorPhysicsCache cache = root.GetComponent<EnemyInvectorPhysicsCache>();
            if (cache == null)
                cache = root.AddComponent<EnemyInvectorPhysicsCache>();

            cache.Refresh();
            return cache;
        }

        private static void DisableChildSolidColliders(GameObject root)
        {
            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null || collider.transform == root.transform)
                    continue;

                // Keep melee/ranged hit volumes — vMeleeManager enables vHitBox during attack windows.
                if (IsOutgoingWeaponHitCollider(collider))
                    continue;

                collider.enabled = false;
            }
        }

        private static bool IsOutgoingWeaponHitCollider(Collider collider)
        {
            if (collider == null)
                return false;

            if (collider.GetComponentInParent<vHitBox>() != null)
                return true;
            if (collider.GetComponentInParent<vMeleeWeapon>() != null)
                return true;
            if (collider.GetComponentInParent<vShooterWeapon>() != null)
                return true;
            if (collider.GetComponentInParent<WeaponHitbox>() != null)
                return true;

            Transform node = collider.transform;
            while (node != null)
            {
                string name = node.name;
                if (name.StartsWith("Drawn_", System.StringComparison.Ordinal) ||
                    name.StartsWith("Holstered_", System.StringComparison.Ordinal) ||
                    name.Equals("WeaponHitbox", System.StringComparison.Ordinal) ||
                    name.Equals("hitBox", System.StringComparison.OrdinalIgnoreCase))
                    return true;
                node = node.parent;
            }

            return false;
        }

        private static void FitRootCapsule(
            GameObject root,
            float targetRadius,
            float targetHeight,
            Vector3 targetCenter,
            bool fitToRenderers)
        {
            CapsuleCollider capsule = root.GetComponent<CapsuleCollider>();
            if (capsule == null)
                capsule = root.AddComponent<CapsuleCollider>();

            if (fitToRenderers && TryFitCapsuleToRenderers(root, capsule))
                return;

            capsule.isTrigger = false;
            capsule.radius = targetRadius;
            capsule.height = Mathf.Max(targetHeight, targetRadius * 2f);
            capsule.center = targetCenter;
        }

        private static Transform ResolveVisualRoot(Transform root)
        {
            Transform visual = root.Find("scene");
            if (visual != null)
                return visual;

            visual = root.Find("Visual");
            if (visual != null)
                return visual;

            return root;
        }

        private static bool TryFitCapsuleToRenderers(GameObject root, CapsuleCollider capsule)
        {
            Transform visualRoot = ResolveVisualRoot(root.transform);
            Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
                return false;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    bounds.Encapsulate(renderers[i].bounds);
            }

            Vector3 localCenter = root.transform.InverseTransformPoint(bounds.center);
            float height = Mathf.Clamp(Mathf.Max(bounds.size.y, 0.5f), 0.5f, 3.5f);
            float radius = Mathf.Max(bounds.extents.x, bounds.extents.z) * 0.35f;
            radius = Mathf.Clamp(radius, 0.25f, 0.75f);

            float centerY = Mathf.Clamp(localCenter.y, height * 0.5f - 0.05f, 2f);

            capsule.isTrigger = false;
            capsule.center = new Vector3(0f, centerY, 0f);
            capsule.height = height;
            capsule.radius = radius;
            capsule.direction = 1;
            return true;
        }

        public static void EnsureRootDamageReceiver(GameObject root)
        {
            Collider rootCollider = root.GetComponent<Collider>();
            if (rootCollider == null)
                return;

            if (root.GetComponent<PioneerInvectorDamageReceiver>() == null)
                root.AddComponent<PioneerInvectorDamageReceiver>();
        }

        /// <summary>
        /// Adds <see cref="PioneerRagdollBoneDamageProxy"/> on every bone that has a Rigidbody + Collider
        /// so Invector melee <c>ApplyDamage</c> (hit-GO only) still reaches the root receiver during ragdoll.
        /// </summary>
        public static void EnsureRagdollBoneDamageProxies(GameObject root)
        {
            if (root == null)
                return;

            PioneerInvectorDamageReceiver rootReceiver = root.GetComponent<PioneerInvectorDamageReceiver>();
            if (rootReceiver == null)
                rootReceiver = root.AddComponent<PioneerInvectorDamageReceiver>();

            Rigidbody rootBody = root.GetComponent<Rigidbody>();
            Rigidbody[] bodies = root.GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < bodies.Length; i++)
            {
                Rigidbody body = bodies[i];
                if (body == null || body == rootBody)
                    continue;

                Collider boneCollider = body.GetComponent<Collider>();
                if (boneCollider == null)
                    continue;

                // Skip outgoing weapon volumes that sit under the enemy hierarchy.
                if (IsOutgoingWeaponHitCollider(boneCollider))
                    continue;

                PioneerRagdollBoneDamageProxy proxy = body.GetComponent<PioneerRagdollBoneDamageProxy>();
                if (proxy == null)
                    proxy = body.gameObject.AddComponent<PioneerRagdollBoneDamageProxy>();

                proxy.Configure(rootReceiver);
            }
        }
    }
}
