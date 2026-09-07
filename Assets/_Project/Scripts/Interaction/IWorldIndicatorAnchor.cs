using UnityEngine;

namespace Project.Interaction
{
    /// <summary>
    /// World interactables that expose a stable indicator attach point (bounds center or marker).
    /// UITK world chrome reads this instead of guessing <see cref="Transform.position"/>.
    /// Pickup stems grow world-up from this anchor; the proximity dot stays on the tip.
    /// </summary>
    public interface IWorldIndicatorAnchor
    {
        /// <summary>True when this instance should show a proximity/interaction indicator.</summary>
        bool IsIndicatorAvailable { get; }

        /// <summary>World-space stem base - visual/bounds center, or an explicit marker.</summary>
        Vector3 GetIndicatorWorldAnchor();

        /// <summary>World-up stem length (meters) at lock-on / far range.</summary>
        float IndicatorStemMinHeight { get; }

        /// <summary>World-up stem length (meters) when planar distance is within near reach.</summary>
        float IndicatorStemMaxHeight { get; }
    }

    /// <summary>
    /// Stable pickup-local center. World AABB centers drift as the camera/LOD/rotation
    /// changes; mesh localBounds stay welded to the item.
    /// </summary>
    public static class WorldIndicatorAnchorUtil
    {
        public static Vector3 ResolveStableLocalCenter(Transform root, Renderer[] renderers, Collider[] colliders)
        {
            if (root == null)
                return Vector3.up * 0.2f;

            if (TryMeshLocalCenter(root, renderers, out Vector3 local))
                return local;

            if (TryColliderLocalCenter(root, colliders, out local))
                return local;

            return Vector3.up * 0.2f;
        }

        private static bool TryMeshLocalCenter(Transform root, Renderer[] renderers, out Vector3 localCenter)
        {
            localCenter = Vector3.zero;
            if (renderers == null)
                return false;

            Vector3 sum = Vector3.zero;
            int count = 0;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer rend = renderers[i];
                if (rend == null || !rend.enabled || !rend.gameObject.activeInHierarchy)
                    continue;
                if (rend is ParticleSystemRenderer)
                    continue;

                Vector3 world = rend.transform.TransformPoint(rend.localBounds.center);
                sum += root.InverseTransformPoint(world);
                count++;
            }

            if (count == 0)
                return false;

            localCenter = sum / count;
            return true;
        }

        private static bool TryColliderLocalCenter(Transform root, Collider[] colliders, out Vector3 localCenter)
        {
            localCenter = Vector3.zero;
            if (colliders == null)
                return false;

            Vector3 sum = Vector3.zero;
            int count = 0;
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider col = colliders[i];
                if (col == null || !col.enabled || !col.gameObject.activeInHierarchy)
                    continue;

                Vector3 local = GetColliderLocalCenter(col);
                Vector3 world = col.transform.TransformPoint(local);
                sum += root.InverseTransformPoint(world);
                count++;
            }

            if (count == 0)
                return false;

            localCenter = sum / count;
            return true;
        }

        private static Vector3 GetColliderLocalCenter(Collider col)
        {
            if (col is SphereCollider sphere)
                return sphere.center;
            if (col is BoxCollider box)
                return box.center;
            if (col is CapsuleCollider capsule)
                return capsule.center;
            return Vector3.zero;
        }
    }
}
