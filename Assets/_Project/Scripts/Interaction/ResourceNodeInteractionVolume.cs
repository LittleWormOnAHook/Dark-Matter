using UnityEngine;

namespace Project.Interaction
{
    /// <summary>
    /// Runtime-fitted root BoxCollider that tracks child renderer AABBs so non-uniform
    /// boulder scales and Visual-only plant scales keep a reliable interaction volume.
    /// Existing mesh colliders are left alone (walk collision). The box is trigger.
    /// Added at play by ResourceNode.OnEnable so placed scene nodes do not need YAML edits.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-50)]
    public class ResourceNodeInteractionVolume : MonoBehaviour
    {
        private const float SizePadding = 0.05f;
        private const float ScaleChangeSqrEpsilon = 0.0001f;

        private BoxCollider box;
        private Vector3 lastLossyScale;

        private void OnEnable()
        {
            Refit();
        }

        private void OnValidate()
        {
            Refit();
        }

        private void LateUpdate()
        {
            Vector3 scale = transform.lossyScale;
            if ((scale - lastLossyScale).sqrMagnitude > ScaleChangeSqrEpsilon)
                Refit();
        }

        public void Refit()
        {
            lastLossyScale = transform.lossyScale;

            if (!TryEncapsulateRenderers(out Bounds world))
                return;

            if (box == null)
                box = GetComponent<BoxCollider>();
            if (box == null)
                box = gameObject.AddComponent<BoxCollider>();

            // Plants and boulders: this box is the interaction collider. Trigger is OK.
            // Do not destroy mesh colliders on children (walk collision).
            box.isTrigger = true;
            box.center = transform.InverseTransformPoint(world.center);
            Vector3 lossy = transform.lossyScale;
            box.size = new Vector3(
                SafeDiv(world.size.x, lossy.x),
                SafeDiv(world.size.y, lossy.y),
                SafeDiv(world.size.z, lossy.z)) + Vector3.one * SizePadding;
        }

        private bool TryEncapsulateRenderers(out Bounds world)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            bool started = false;
            world = default;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer rend = renderers[i];
                if (rend == null || !rend.enabled)
                    continue;

                if (!started)
                {
                    world = rend.bounds;
                    started = true;
                }
                else
                {
                    world.Encapsulate(rend.bounds);
                }
            }

            return started && world.size.sqrMagnitude > 0.0001f;
        }

        private static float SafeDiv(float a, float b)
        {
            return Mathf.Abs(b) < 0.0001f ? a : a / b;
        }
    }
}
